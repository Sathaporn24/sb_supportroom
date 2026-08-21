# company-admin — Verification & Review

## Open Issues — all phases

| Issue | Phase | Route | Blocking | Re-check rounds |
|---|---|---|---|---:|
| SEC-01 · JWT ที่ออกแล้วไม่รับรู้การปิดบริษัท/ปิดบัญชี/เปลี่ยน role (`CurrentUserMiddleware.cs:37`) | Phase 1/2 (Module A) | backend-engineer | yes (before deploy) | 0 |
| SEC-02 · `MustChangePassword` บังคับเฉพาะ frontend, bypass ผ่าน API ได้ (`AuthenticationConfiguration.cs:72`) | Phase 1/2 (Module A) | backend-engineer | yes (before deploy) | 0 |
| SEC-03 · login ของ admin ไม่มี rate limiting (`Program.cs:75`) | Phase 1/2 (Module A) | backend-engineer | yes (before deploy) | 0 |
| Phase 1's most recent QA round was TARGETED — needs one more FULL round before `devops` eligibility, even though functionally 15/15 | Phase 1 | qa-engineer | yes (before deploy) | — |
| `🔒 Security gate` on Phase 3 has not been audited yet — `security` has only run SECURITY-1 covering Phase 1/2; Phase 3's UI changes (dropdown role-gating, auto-select, removed gate screens) still need review | Phase 3 | security | yes (before deploy) | 0 |

## Verification Summary (current round)

**FULL round** for **Phase 3: Company Switching — Owner UX**. All 6 `[frontend]` tasks were unchecked before this round; all 6 are now **✅ Verified, 6/6**, checked off in `plan.md`.

Every task was inspected against the real code in `CompanySwitcher.tsx`, `AdminSessionProvider.tsx`, and `AdminGuard.tsx` (not the engineer's own summary), and cross-checked against `requirement.md` F4.0–F4.6. Two implementation notes worth recording: (1) the engineer merged what the plan described as two separate `AdminSessionProvider.tsx` tasks (F4.3 "auto-select whenever nothing resolved" and F4.6 "switch away when the current company disappears from the list") into a single effect — the unified condition (`resolved && companies.some(...)` returns early, otherwise falls through to `companies[0]`) correctly covers both the "nothing resolved yet" and "resolved company no longer active" cases, so this is accepted as satisfying both tasks rather than a gap; (2) after removing both `AdminGuard.tsx` gate screens, the wrapper condition that used to enclose them is gone too — confirmed by grep, no dead code or unused imports remain, and the unrelated `isOwnerOnlyPage` guard for `/admin/companies` is untouched and still functions.

Checks run: `npm run typecheck` (clean, no errors), `npm run lint` (clean, `eslint .`), `npm run test` (Vitest, 5 test files / 41 tests passed — same count as the prior round; Phase 3 added no new tests, consistent with the plan not requiring any here), `npm run build` (production build succeeded, all 23 routes compiled, including `/admin/companies` and `/admin/companies/new`). This project's `test` script is Vitest and does exist and run real assertions (not empty), but none of those 41 tests exercise `CompanySwitcher.tsx`/`AdminSessionProvider.tsx`/`AdminGuard.tsx` directly (confirmed by `Glob` — no test file matches these three components) — see `## Unverified Behaviour` below for what that leaves unexecuted.

No schema change in this phase (frontend-only, confirmed by inspection — no DTO/ViewModel/controller/EF file touched), so there is nothing to compare against `design.md`'s Data Model for Phase 3 itself. Phase 3 is also not described in `design.md` at all — this is expected and documented in `plan.md`'s own Change Log: F4 skipped `system-analyst` on purpose because it doesn't touch schema/endpoints.

## Verified File Manifest — Phase 3

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `frontend/src/components/admin/CompanySwitcher.tsx` | 1601 | 40 | FULL-1 |
| `frontend/src/components/admin/AdminSessionProvider.tsx` | 6939 | 166 | FULL-1 |
| `frontend/src/components/admin/AdminGuard.tsx` | 2145 | 51 | FULL-1 |

## Per-Task Results — Phase 3 (FULL-1)

- ✅ [frontend] `CompanySwitcher.tsx` — owner branch (`user?.role !== "owner"` false path) renders `Select`/`SelectTrigger`/`SelectContent`/`SelectItem` unconditionally, not gated by `companies.length` — matches F4.1. Confirmed it is the plain shadcn `Select`, not a Combobox (DC-7 deferral respected). `admin`/`cs` branch is untouched: plain-text `บริษัท: {only.name}`, matches F4.2 exactly.
- ✅ [frontend] `CompanySwitcher.tsx` — no link/route to `/admin/companies` anywhere inside the dropdown JSX; matches F4.4/DC-6.
- ✅ [frontend] `AdminSessionProvider.tsx` — the effect at lines 113–122 no longer has the `companies.length !== 1` restriction (confirmed absent via grep across `frontend/src`); it selects `companies[0].id` whenever `resolved` is falsy or not present in the current `companies` list, for any `companies.length > 0`. Matches F4.3.
- ✅ [frontend] `AdminSessionProvider.tsx` — the same effect's `companies.some((c) => c.id === resolved)` check means a resolved id that has dropped out of the active list (deactivated mid-session) falls through to the `companies[0].id` branch and updates `?company=` via `router.replace`, with no interstitial render anywhere in the component. Matches F4.6.
- ✅ [frontend] `AdminGuard.tsx` — "เลือกบริษัทก่อนเริ่มทำงาน" screen is gone; grep for the exact string across `frontend/src` returns no matches anywhere in the codebase (not just this file).
- ✅ [frontend] `AdminGuard.tsx` — "ยังไม่มีบริษัทในระบบ" screen is gone (same grep, no matches), the wrapper condition that used to gate both screens is also gone (no `activeCompanyId` reference left in this file at all), and there are no unused imports or dead variables — the file's remaining logic (`isLoginPage`/`isChangePasswordPage`/`isOwnerOnlyPage` redirects) is all still referenced and all still exercised by the build/typecheck/lint pass.

## Design/requirement contract checks — Phase 3

No `schema.prisma`/EF entity/migration/DTO/controller was touched by this phase (confirmed by inspection of the three changed files plus a repo-wide grep for other files importing them — none found besides expected consumers: `layout.tsx`, `AdminTopbar.tsx`, etc., none of which needed changes). There is therefore no Data Model comparison to perform for Phase 3 itself. Requirement F4.0 was explicitly "no work" per `requirement.md` and confirmed still true — `admin`/`cs` still fall back to `user.companyId` unchanged in `AdminSessionProvider.tsx`. F4.1–F4.6 were checked field-by-field above against the real code, not inferred from the plan's description.

Security-sensitive checks specific to this phase (requested alongside the routine gate flag, since Phase 3 inherits Module A's blanket gate):
- A non-owner (`admin`/`cs`) has no code path to the `Select` dropdown — the branch is a hard `user?.role !== "owner"` check with an early return; there is no state or prop that can put a non-owner into the owner branch.
- An owner with no `activeCompanyId` yet has no reachable screen: the removed gate screens are gone, and the auto-select effect fires as soon as `companies` is non-empty, immediately assigning a real company before any screen using `activeCompanyId` renders meaningfully. The one edge case — zero *active* companies system-wide — is the requirement's own accepted risk (documented in `requirement.md` §Constraints, "สถานะ 0 บริษัทจะไม่มี UI รองรับเลย"), not a Phase 3 gap; it produces a generic failed request rather than a company-switch dropdown, so it does not expose cross-company data.
- `AdminGuard.tsx`'s `isOwnerOnlyPage` check (protecting `/admin/companies` from non-owner) is untouched by this phase's edits — confirmed by reading the current file end to end; no accidental widening.

## Unverified Behaviour — undeployed phases

This project has a real Vitest suite (`npm run test`, 41/41 passing) covering other parts of the codebase, but none of its test files exercise `CompanySwitcher.tsx`, `AdminSessionProvider.tsx`, or `AdminGuard.tsx` (confirmed via `Glob` for test files in `frontend/src/components/admin/` and by name across the whole `frontend/` tree — none exist for these three components). For this phase specifically, that leaves the following read-but-not-executed:

### Phase 3: Company Switching — Owner UX
- The merged auto-select/auto-switch condition in `AdminSessionProvider.tsx` (`resolved && companies.some(...)` gate) that is meant to satisfy both F4.3 (nothing resolved yet) and F4.6 (resolved company deactivated mid-session) in one effect — its branching was read and reasoned through, not run against a browser session with a real `companies` list transition.
- The interaction between the URL-replace in that effect and the URL-replace in the earlier "mirror URL" effect (both call `router.replace` with a `URLSearchParams` built from the current `searchParams`) — no observed run confirms they don't race or overwrite each other's `?company=` value on the same render pass.
- `switchCompany`'s `router.push` behavior when an owner picks a company from the now-always-visible dropdown while `companies.length === 1` (previously unreachable UI state, now reachable per F4.1) — logic was read, not exercised.

## Issues Found — Phase 3

None. All 6 tasks passed inspection.

## Review Outcome — Phase 3

**Auto-accepted** (autonomous-mode exception per `conventions.md` §6): this was a FULL round and all 6 tasks came back ✅ Verified, so it did not need to pause for a manual accept/reject decision. Phase 3 is functionally accepted. It is **not yet eligible for `devops`**: Module A's `🔒 Security gate` covers Phase 3 (per `plan.md`'s own reasoning — the gate is blanket-per-module, not risk-per-phase), and `security` has not audited Phase 3's changes yet (SECURITY-1 only covered Phase 1/2). The user must call `security` by name before this phase can deploy, same as Phase 1/2.

Phase 1 and Phase 2's outcomes are unchanged from the prior round (see `## Open Issues` above and `review/phase-1.md`): Phase 1 is 15/15 functionally verified but TARGETED, still needs a FULL round before `devops`; Phase 2 is 7/7 FULL. Both remain blocked on the 3 open SEC-01/02/03 findings in `security.md`.

## Archived rounds

- Phase 1 (company-admin) — FULL-1, sent back for CH-2/CH-6 migration correction; TARGETED-1, corrective migration verified, 15/15 → `review/phase-1.md`

## Change Log

- 2026-08-21 — QA TARGETED-1: corrective timestamp migration verified; former CH-2/CH-6 finding resolved, Phase 1 is 15/15 verified (TARGETED). Security gates remain open. (archived)
- 2026-08-21 — QA FULL-1 for Phase 3 (Company Switching — Owner UX): all 6 `[frontend]` tasks verified against real code in `CompanySwitcher.tsx`/`AdminSessionProvider.tsx`/`AdminGuard.tsx`, checked off in `plan.md`. `typecheck`/`lint`/`test` (41/41)/`build` all pass. No schema/endpoint touched, nothing to compare against Data Model. Auto-accepted (all-✅ FULL round, autonomous mode). Still blocked on Module A's `🔒 Security gate` — `security` has not audited Phase 3, and SEC-01/02/03 from SECURITY-1 remain open against Phase 1/2. Phase 1 still needs a FULL round (last was TARGETED) before any phase in this module is `devops`-eligible.
