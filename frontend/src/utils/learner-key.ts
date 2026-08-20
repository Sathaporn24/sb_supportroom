"use client";

/**
 * The browser's anonymous key across training links.
 *
 * One link is handed to a whole department, so the link token alone cannot say who is calling.
 * This key does both jobs the spec asks of it (CORE_FEATURE_SPEC §2.4): it lets someone resume
 * after a closed tab or a dropped connection, and it keeps two people on the same link from
 * landing in each other's session.
 *
 * Together with a link token this is a composite bearer credential. One global browser key is
 * intentional: the server still scopes it by TrainingLinkId, so using it across links never
 * combines sessions or decides company access.
 */

const LEARNER_KEY_STORAGE_KEY = "supportroom.learnerKey";
const NAME_PREFIX = "sb_learner_name:";

function readStorage(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    // Private mode / storage disabled. Callers fall back to a fresh in-memory key, which means
    // no resume - degraded, but the lesson still runs.
    return null;
  }
}

function writeStorage(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // See readStorage - losing persistence costs resume, not the session.
  }
}

function generateKey(): string {
  if (typeof crypto === "undefined" || typeof crypto.randomUUID !== "function") {
    throw new Error("เบราว์เซอร์นี้ไม่รองรับการสร้างกุญแจที่ปลอดภัย กรุณาเปิดผ่าน HTTPS และลองใหม่อีกครั้ง");
  }
  return crypto.randomUUID();
}

/** Returns the browser-wide key, creating and persisting one on first visit. */
export function getOrCreateLearnerKey(): string {
  const existing = readStorage(LEARNER_KEY_STORAGE_KEY);
  if (existing) {
    return existing;
  }
  const created = generateKey();
  writeStorage(LEARNER_KEY_STORAGE_KEY, created);
  return created;
}

/** Null on a first visit - that is exactly the signal to show the join screen. */
export function peekLearnerKey(): string | null {
  return readStorage(LEARNER_KEY_STORAGE_KEY);
}

/** Remembered so a returning learner never retypes their name, including on "เรียนอีกครั้ง". */
export function getLearnerName(token: string): string | null {
  return readStorage(`${NAME_PREFIX}${token}`);
}

export function setLearnerName(token: string, name: string): void {
  writeStorage(`${NAME_PREFIX}${token}`, name);
}

/**
 * One-shot permission to open the room, handed over when the learner answered the confirmation
 * question on the join screen.
 *
 * Deliberately NOT a "this browser is confirmed" flag: it is consumed the moment the room reads
 * it, so the next time anyone opens the link the question is asked again. Storing a lasting flag
 * - localStorage, a cookie, a query string - would be the silent resume the spec forbids, just
 * moved to a different shelf. sessionStorage also dies with the tab, so a shared computer cannot
 * inherit it from whoever sat down before.
 */
const ENTRY_PREFIX = "sb_room_entry:";

export function grantRoomEntry(token: string): void {
  try {
    window.sessionStorage.setItem(`${ENTRY_PREFIX}${token}`, "1");
  } catch {
    // Storage disabled: the room falls back to sending the learner through the join screen,
    // which is the safe direction to fail in.
  }
}

/** Reads and immediately clears the grant. Returns false when the room was opened directly. */
export function consumeRoomEntry(token: string): boolean {
  try {
    const key = `${ENTRY_PREFIX}${token}`;
    const granted = window.sessionStorage.getItem(key) === "1";
    window.sessionStorage.removeItem(key);
    return granted;
  } catch {
    return false;
  }
}
