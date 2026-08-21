# company-admin — Verification & Review

## Open Issues — all phases

| Issue | Phase | Route | Blocking | Re-check rounds |
|---|---|---|---|---:|
| `🔒 Security gate` has not been audited: tenant provisioning, initial credential input, registry tables without query filters, and hardcoded role require security review before deploy. | Phase 1 | security | yes (before deploy) | 0 |
| `🔒 Security gate` has not been audited: the owner UI handles initial credentials and calls tenant-provisioning endpoints. | Phase 2 | security | yes (before deploy) | 0 |

## Verification Summary (current round)

**TARGETED re-check 1** for Phase 1's prior CH-2/CH-6 finding. The new `20260821142529_CorrectDefaultCategoryChainLeafCreateDate` preserves the applied original migration, is data-only, updates only the known leaf rows created below pre-existing system-default parents, and safely has a no-op `Down()`. The prior finding is **✅ resolved**; Phase 1 is now **15/15 verified (TARGETED)**. Its previous FULL round is archived below, so it still needs a later FULL round before `devops` eligibility.

The re-check inspected the corrective migration, generated designer/EF discovery, focused coverage, and the shared provisioning/frontend watchlist. It did not re-read unchanged Phase 1 behaviour already covered by FULL-1; Phase 2 remains 7/7 verified in that FULL round and its whole-project checks passed again.

Checks actually run: `dotnet build SupportRoom.slnx` (0 warnings, 0 errors); `dotnet test SupportRoom.slnx --filter "Category!=Integration"` (215/215); focused `DefaultCategoryChainCorrectiveMigrationTests` (2/2); `dotnet ef migrations list --no-connect` discovers both migrations; `dotnet ef migrations has-pending-model-changes` reports no model changes. Node `v22.23.2`: typecheck, lint, Vitest (41/41), and production build all pass.

An unfiltered `dotnet test SupportRoom.slnx` was also attempted; 10 unrelated tests failed because this QA environment lacks Google/Gemini/Pinecone credentials and no PostgreSQL listener is currently available on ports 5432/55432. The migration-specific integration assertion consequently could not connect. This does not contradict the passing filtered regression suite or corrective migration tests, but a live database query was not repeated in this re-check.

## Per-Task Results — Phase 1 (TARGETED-1)

- ✅ [backend] `BackfillMissingDefaultCategoryChain` is now complete: the applied original migration remains immutable, and corrective migration `20260821142529_CorrectDefaultCategoryChainLeafCreateDate` fixes only leaf rows generated for existing parents with `SET "CreateDate" = now()`.
- ✅ [backend] corrective migration has no DDL/insert/delete, no `IgnoreQueryFilters()` or `ON CONFLICT`, and its no-op `Down()` does not risk deleting default-chain data.
- ✅ [backend] focused migration tests assert the update's target scope, data-only operation, and no-op `Down()`; existing CP-6/CP-8/CP-10/CP-12/CP-13 code paths remained intact under the filtered suite.

## Design/requirement contract checks — Phase 1 (TARGETED-1)

CH-2/CH-6 require the leaf repaired under a pre-existing parent to use its own creation time, rather than inheriting the parent's historical timestamp. The correction selects only the deterministic `kbcat-company-admin-leaf-` rows, requires the linked parent/leaf shape and system-default levels, and excludes the deterministic parent created by the original migration; therefore it does not change chains that were already created together. EF detects the migration and confirms no model/snapshot drift. CH-3 is unchanged by this timestamp-only update.

## Issues Found — Phase 1

None. The former CH-2/CH-6 issue is resolved. The unavailable local PostgreSQL process prevented repeating the live migration-history/timestamp query in this round; it is a verification-environment limitation, not an implementation finding.

## Review Outcome — Phase 1 and Phase 2

Phase 1 accepted functionally after TARGETED-1 (15/15), pending a future FULL QA round before `devops` and the existing Phase 1 security audit. Phase 2 remains accepted from FULL-1 (7/7), pending its existing security audit. No security role was invoked by QA.

## Archived rounds

- Phase 1 (company-admin) — FULL-1, sent back for CH-2/CH-6 migration correction → `review/phase-1.md`

## Change Log

- 2026-08-21 — QA TARGETED-1: corrective timestamp migration verified; former CH-2/CH-6 finding resolved, Phase 1 is 15/15 verified (TARGETED). Security gates remain open.
