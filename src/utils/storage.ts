export function isBrowser(): boolean {
  return typeof window !== "undefined";
}

export function readJson<T>(key: string, fallback: T): T {
  if (!isBrowser()) {
    return fallback;
  }
  try {
    const raw = window.localStorage.getItem(key);
    if (!raw) {
      return fallback;
    }
    return JSON.parse(raw) as T;
  } catch {
    return fallback;
  }
}

export function writeJson<T>(key: string, value: T): void {
  if (!isBrowser()) {
    return;
  }
  try {
    window.localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Storage unavailable (e.g. private browsing quota) - fail silently in this mock demo.
  }
}

export function removeKey(key: string): void {
  if (!isBrowser()) {
    return;
  }
  try {
    window.localStorage.removeItem(key);
  } catch {
    // ignore
  }
}
