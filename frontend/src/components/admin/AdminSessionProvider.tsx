"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams, usePathname } from "next/navigation";
import * as api from "@/lib/api-client";
import {
  clearSession,
  getActiveCompanyId,
  loadSession,
  saveSession,
  saveUser,
  setActiveCompanyId,
} from "@/lib/auth-session";
import type { Company, LoginResult, SignedInUser } from "@/types/domain";

/**
 * Holds the two things every back-office screen needs: who is signed in, and which customer they
 * are currently looking at.
 *
 * The active company is read from ?company= on every navigation rather than kept in state, which
 * is what makes the browser work normally - back, refresh, bookmark and "send this link to a
 * colleague" all land on the same customer. See auth-session.ts for why it is not in the token.
 */

type AdminSession = {
  user: SignedInUser | null;
  companies: Company[];
  activeCompanyId: string | null;
  ready: boolean;
  signIn: (result: LoginResult) => void;
  signOut: () => void;
  switchCompany: (companyId: string) => void;
  refreshUser: () => Promise<void>;
};

const AdminSessionContext = createContext<AdminSession | null>(null);

export function useAdminSession(): AdminSession {
  const context = useContext(AdminSessionContext);
  if (!context) throw new Error("useAdminSession ต้องอยู่ภายใต้ AdminSessionProvider");
  return context;
}

export function AdminSessionProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const [user, setUser] = useState<SignedInUser | null>(null);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [ready, setReady] = useState(false);

  const companyFromUrl = searchParams.get("company");
  // Auth transition pages never carry a company in their URL and must not be given one: signIn()
  // below already decides where to land (/admin or /admin/change-password) via router.replace, and
  // that navigation is not synchronous - usePathname() can still report the old "/admin/login" for
  // a render or two while it is in flight. An effect below that also called replace() during that
  // window would win the race and strand the browser on "/admin/login?company=...".
  const isAuthTransitionPage = pathname === "/admin/login" || pathname === "/admin/change-password";

  const signOut = useCallback(() => {
    clearSession();
    setUser(null);
    setCompanies([]);
    router.replace("/admin/login");
  }, [router]);

  // Restore from localStorage on first paint. Reading storage is synchronous, so this settles in
  // one tick and screens only ever see `ready: true` with a final answer.
  useEffect(() => {
    const { token, user: storedUser } = loadSession();
    if (token && storedUser) setUser(storedUser);
    setReady(true);
  }, []);

  // A 401 from anywhere means the token is gone or expired - drop the session rather than leaving
  // the user clicking around a shell that fails every request.
  useEffect(() => {
    api.setUnauthorizedHandler(signOut);
    return () => api.setUnauthorizedHandler(null);
  }, [signOut]);

  // Mirror the URL into the module the api-client reads, before any screen fires a request.
  // Falling back to the user's own company covers a company-scoped user who has no reason to ever
  // see ?company= in their URL - they have exactly one. For an owner, retain the current in-memory
  // selection during client navigation and immediately canonicalize a link that accidentally
  // dropped the query. A full reload without ?company= still asks the owner to choose, by design.
  useEffect(() => {
    const resolved = companyFromUrl ?? user?.companyId ?? getActiveCompanyId();
    setActiveCompanyId(resolved);

    if (user?.role === "owner" && !companyFromUrl && resolved && !isAuthTransitionPage) {
      const params = new URLSearchParams(searchParams.toString());
      params.set("company", resolved);
      router.replace(`${pathname}?${params.toString()}`);
    }
  }, [companyFromUrl, isAuthTransitionPage, pathname, router, searchParams, user]);

  useEffect(() => {
    if (!user) return;
    void api
      .listSwitchableCompanies()
      .then(({ companies: list }) => setCompanies(list))
      // Never fatal: the switcher is navigation, and a failure here must not blank the screen the
      // user actually came for.
      .catch(() => setCompanies([]));
  }, [user]);

  // An owner with nothing resolved yet and exactly one company to pick from has no real choice to
  // make - the "pick a company" screen above would otherwise strand them forever, since there is
  // no control that lets them make that pick. Waits on the companies fetch above, so this only
  // fires once the list is known.
  useEffect(() => {
    if (user?.role !== "owner" || isAuthTransitionPage) return;
    const resolved = companyFromUrl ?? user?.companyId ?? getActiveCompanyId();
    if (resolved || companies.length !== 1) return;

    const params = new URLSearchParams(searchParams.toString());
    params.set("company", companies[0].id);
    router.replace(`${pathname}?${params.toString()}`);
  }, [companies, companyFromUrl, isAuthTransitionPage, pathname, router, searchParams, user]);

  const signIn = useCallback(
    (result: LoginResult) => {
      saveSession(result.token, result.user);
      setUser(result.user);
      setActiveCompanyId(result.user.companyId ?? null);
      router.replace(result.user.mustChangePassword ? "/admin/change-password" : "/admin");
    },
    [router],
  );

  const switchCompany = useCallback(
    (companyId: string) => {
      // Navigate rather than set state: the URL is the source of truth, so changing it is the
      // switch. Everything re-reads from the new URL.
      const params = new URLSearchParams(searchParams.toString());
      params.set("company", companyId);
      router.push(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams],
  );

  const refreshUser = useCallback(async () => {
    const { user: fresh } = await api.getSignedInUser();
    saveUser(fresh);
    setUser(fresh);
  }, []);

  const value = useMemo<AdminSession>(
    () => ({
      user,
      companies,
      activeCompanyId: companyFromUrl ?? getActiveCompanyId(),
      ready,
      signIn,
      signOut,
      switchCompany,
      refreshUser,
    }),
    [user, companies, companyFromUrl, ready, signIn, signOut, switchCompany, refreshUser],
  );

  return <AdminSessionContext.Provider value={value}>{children}</AdminSessionContext.Provider>;
}
