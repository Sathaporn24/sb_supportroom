"use client";

/**
 * The browser's identity on one training link.
 *
 * One link is handed to a whole department, so the link token alone cannot say who is calling.
 * This key does both jobs the spec asks of it (CORE_FEATURE_SPEC §2.4): it lets someone resume
 * after a closed tab or a dropped connection, and it keeps two people on the same link from
 * landing in each other's session.
 *
 * Scoped per token rather than one key per browser: the same machine opening two different
 * links keeps two independent identities, and clearing one link's state never disturbs another.
 *
 * ⚠️ Not a credential. The link token is what authorizes a request - this only selects which row
 * within that link belongs to the caller.
 */

const KEY_PREFIX = "sb_learner_key:";
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
  // randomUUID needs a secure context; the fallback keeps http://<lan-ip> dev hosts working.
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}${Math.random().toString(36).slice(2)}`;
}

/** Returns the existing key for this link, creating and persisting one on first visit. */
export function getOrCreateLearnerKey(token: string): string {
  const storageKey = `${KEY_PREFIX}${token}`;
  const existing = readStorage(storageKey);
  if (existing) {
    return existing;
  }
  const created = generateKey();
  writeStorage(storageKey, created);
  return created;
}

/** Null on a first visit - that is exactly the signal to show the join screen. */
export function peekLearnerKey(token: string): string | null {
  return readStorage(`${KEY_PREFIX}${token}`);
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
