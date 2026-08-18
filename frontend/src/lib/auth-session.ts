"use client";

import type { SignedInUser } from "@/types/domain";

/**
 * Where the back office keeps "who is signed in" and "which customer am I looking at".
 *
 * These are two deliberately different mechanisms, and the difference matters:
 *
 *   token          -> localStorage. Identity is the same in every tab; signing in once and
 *                     having every tab work is what people expect.
 *   active company -> the URL (?company=). NOT storage, and NOT the token.
 *
 * The company lives in the URL because it is a view, not an identity. Putting it in shared
 * storage or baking it into the token would mean switching customers in one tab silently
 * changes what another tab is showing while its heading still names the old one - wrong data
 * under a right-looking label. From the URL it also survives refresh, works with the back
 * button, can be bookmarked, and can be sent to a colleague who then sees the same screen.
 *
 * ⚠️ The value here is never trusted. The server re-checks on every request that this user may
 * act on that company (IAuthorizationGuard) - editing the URL by hand earns a 403, not data.
 *
 * A token in localStorage is readable by any script running on this origin. That is the accepted
 * cost of bearer tokens; the alternative (an httpOnly cookie) needs SameSite=None plus credentialed
 * CORS, which this split-origin setup deliberately avoids - see TD-014.
 */

const TOKEN_KEY = "supportroom.admin.token";
const USER_KEY = "supportroom.admin.user";

/**
 * Mirrors localStorage so api-client can read the token synchronously during a request without
 * touching storage on every call, and so server-side rendering has a safe default.
 */
let cachedToken: string | null = null;
let activeCompanyId: string | null = null;

export function loadSession(): { token: string | null; user: SignedInUser | null } {
  if (typeof window === "undefined") return { token: null, user: null };

  cachedToken = window.localStorage.getItem(TOKEN_KEY);
  const rawUser = window.localStorage.getItem(USER_KEY);

  let user: SignedInUser | null = null;
  if (rawUser) {
    try {
      user = JSON.parse(rawUser) as SignedInUser;
    } catch {
      // A corrupt entry must not lock someone out of the app forever - drop it and let them
      // sign in again.
      window.localStorage.removeItem(USER_KEY);
    }
  }

  return { token: cachedToken, user };
}

export function saveSession(token: string, user: SignedInUser): void {
  cachedToken = token;
  window.localStorage.setItem(TOKEN_KEY, token);
  window.localStorage.setItem(USER_KEY, JSON.stringify(user));
}

/**
 * Refreshes the cached profile without replacing the bearer token. The profile is persisted as
 * well as returned by React state: otherwise a password change clears mustChangePassword only
 * until the next full-page load, where the stale localStorage copy sends the user straight back
 * to the forced-change screen.
 */
export function saveUser(user: SignedInUser): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearSession(): void {
  cachedToken = null;
  activeCompanyId = null;
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(TOKEN_KEY);
  window.localStorage.removeItem(USER_KEY);
}

export function getAccessToken(): string | null {
  return cachedToken;
}

/**
 * Set from the URL by AdminSessionProvider on every navigation, so api-client can attach it
 * without every caller having to pass it down.
 */
export function setActiveCompanyId(companyId: string | null): void {
  activeCompanyId = companyId;
}

export function getActiveCompanyId(): string | null {
  // The URL is the source of truth and is available synchronously in the browser. Reading it
  // here closes a subtle first-render race: child page effects can issue their first request
  // before AdminSessionProvider's effect has mirrored ?company= into this module.
  if (typeof window !== "undefined") {
    const fromUrl = new URLSearchParams(window.location.search).get("company");
    if (fromUrl) return fromUrl;
  }
  return activeCompanyId;
}
