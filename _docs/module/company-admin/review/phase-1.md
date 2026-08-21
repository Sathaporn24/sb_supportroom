# company-admin — Phase 1 FULL-1 Archive

## Verification Summary

**FULL round 1** for Phase 1. Phase 1 was **⚠️ sent back for one corrective backend migration issue**; the other 14 tasks passed code/contract inspection.

Verified with: `dotnet build SupportRoom.slnx` (0 warnings, 0 errors); `dotnet test SupportRoom.slnx --filter "Category!=Integration"` (213/213). `dotnet ef migrations list` found `20260821054948_BackfillMissingDefaultCategoryChain`; a read-only query against the PostgreSQL instance configured for development found it applied and found zero companies violating CH-3.

## Verified File Manifest — Phase 1

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `backend/src/SupportRoom.Application/Services/ICompanyService.cs` | 5961 | 162 | FULL-1 |
| `backend/src/SupportRoom.Application/Services/IKnowledgeCategoryService.cs` | 8304 | 192 | FULL-1 |
| `backend/src/SupportRoom.Application/Dto/CompanyDto.cs` | 1775 | 37 | FULL-1 |
| `backend/src/SupportRoom.Api/Controllers/CompanyController.cs` | 1346 | 37 | FULL-1 |
| `backend/src/SupportRoom.Providers.Data/Repository/ICompanyRepository.cs` | 1287 | 34 | FULL-1 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260821054948_BackfillMissingDefaultCategoryChain.cs` | 4638 | 117 | FULL-1 |
| `backend/src/SupportRoom.Domain/Entities/Company.cs` | 1888 | 39 | FULL-1 |
| `backend/src/SupportRoom.Domain/Entities/AdminUser.cs` | 3017 | 64 | FULL-1 |
| `backend/src/SupportRoom.Domain/Entities/KnowledgeCategory.cs` | 839 | 22 | FULL-1 |
| `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs` | 10601 | 200 | FULL-1 |
| `backend/tests/SupportRoom.Application.Tests/CompanyServiceTests.cs` | 8866 | 232 | FULL-1 |
| `backend/tests/SupportRoom.Application.Tests/KnowledgeCategoryServiceTests.cs` | 8018 | 209 | FULL-1 |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 12738 | 284 | FULL-1 |

## Per-Task Results — Phase 1 (FULL-1)

- ✅ [backend] `CreateDefaultChain` is add-only on `IKnowledgeCategoryService`, stages the exact linked parent/leaf and neither queries nor commits.
- ✅ [backend] `CreateCompanyDto` has the three CP-2 fields and annotations, without `Role` or `CompanyId`.
- ✅ [backend] `Create` calls `EnsureOwner` first, then normalizes and validates slug, duplicate slug, then duplicate email.
- ✅ [backend] Company, hardcoded `AdminRole.Admin` user, and default chain stage before one terminal `Commit()`; no `IAdminUserService.Create` call.
- ✅ [backend] `POST /api/companies` returns 201 with only `{ company: CompanyViewModel }`.
- ✅ [backend] CP-11 information log contains only company/admin-user/actor IDs.
- ✅ [backend] owner-only `GET /api/companies/all` returns inactive rows name-ordered without changing switcher behavior.
- ❌ [backend] `BackfillMissingDefaultCategoryChain` violates CH-2/CH-6 when it inserts a leaf under an already-existing parent: it selects `parent."CreateDate"` instead of the insertion time.
- ✅ [backend] the original migration is applied on the local development database; read-only CH-3 query returned zero violating companies.
- ✅ [backend] CP-6 single-commit/no-`IAdminUserService.Create` test passes.
- ✅ [backend] CH-3/CH-4 default-chain shape, no-query, no-commit test passes.
- ✅ [backend] CP-4 active/inactive duplicate-slug tests assert both exact messages and do not reactivate existing data.
- ✅ [backend] CP-5 duplicate-email test asserts the exact non-enumerating message.
- ✅ [backend] CP-8 test proves unknown JSON `role` is ignored and the created user remains `admin`.
- ✅ [backend] company-context test resolves the new company before querying `KnowledgeCategory`, proving CP-12 without `IgnoreQueryFilters()`.

## Design/requirement contract checks — Phase 1

Module A declares no new model or field. The existing `Company`, `AdminUser`, and cross-module `KnowledgeCategory` entities match the fields used by the confirmed Data Model; `Company` and `AdminUser` remain deliberately without query filters, while `KnowledgeCategory` retains its company/is-delete filter. The EF migration is data-only and contains no DDL or `ON CONFLICT`; application code for provisioning contains no `IgnoreQueryFilters()`. CP-1 through CP-15 and CH-1 through CH-8 were inspected; the only mismatch is the migration `CreateDate` value recorded above.

## Issues Found — Phase 1

`backend-engineer`: create a corrective **data-only** migration (and focused coverage) for the CH-2/CH-6 `CreateDate` mismatch. The original migration has been applied locally, so it must not be edited retrospectively; the corrective migration must be locally applied and then re-verified. This is an implementation bug because the contract is explicit.

## Review Outcome — Phase 1

User decision: send Phase 1 back to `backend-engineer` for the corrective migration. The Phase 1 `🔒 Security gate` remained open.

## Change Log

- 2026-08-21 — QA FULL-1: Phase 1 14/15 verified with one CH-2/CH-6 migration finding sent to `backend-engineer`.

---

# company-admin — Phase 1 TARGETED-1 Archive

## Verification Summary

**TARGETED re-check 1** for Phase 1's prior CH-2/CH-6 finding. The new `20260821142529_CorrectDefaultCategoryChainLeafCreateDate` preserves the applied original migration, is data-only, updates only the known leaf rows created below pre-existing system-default parents, and safely has a no-op `Down()`. The prior finding is **✅ resolved**; Phase 1 is now **15/15 verified (TARGETED)**. Its previous FULL round is archived above, so it still needs a later FULL round before `devops` eligibility.

The re-check inspected the corrective migration, generated designer/EF discovery, focused coverage, and the shared provisioning/frontend watchlist. It did not re-read unchanged Phase 1 behaviour already covered by FULL-1; Phase 2 remains 7/7 verified in that FULL round and its whole-project checks passed again.

Checks actually run: `dotnet build SupportRoom.slnx` (0 warnings, 0 errors); `dotnet test SupportRoom.slnx --filter "Category!=Integration"` (215/215); focused `DefaultCategoryChainCorrectiveMigrationTests` (2/2); `dotnet ef migrations list --no-connect` discovers both migrations; `dotnet ef migrations has-pending-model-changes` reports no model changes. Node `v22.23.2`: typecheck, lint, Vitest (41/41), and production build all pass.

An unfiltered `dotnet test SupportRoom.slnx` was also attempted; 10 unrelated tests failed because this QA environment lacks Google/Gemini/Pinecone credentials and no PostgreSQL listener is currently available on ports 5432/55432. The migration-specific integration assertion consequently could not connect. This does not contradict the passing filtered regression suite or corrective migration tests, but a live database query was not repeated in this re-check.

## Per-Task Results — Phase 1 (TARGETED-1)

- ✅ [backend] `BackfillMissingDefaultCategoryChain` is now complete: the applied original migration remains immutable, and corrective migration `20260821142529_CorrectDefaultCategoryChainLeafCreateDate` fixes only leaf rows generated for existing parents with `SET "CreateDate" = now()`.
- ✅ [backend] corrective migration has no DDL/insert/delete, no `IgnoreQueryFilters()` or `ON CONFLICT`, and its no-op `Down()` does not risk deleting default-chain data.
- ✅ [backend] focused migration tests assert the update's target scope, data-only operation, and no-op `Down()`; existing CP-6/CP-8/CP-10/CP-12/CP-13 code paths remained intact under the filtered suite.

## Design/requirement contract checks — Phase 1 (TARGETED-1)

CH-2/CH-6 require the leaf repaired under a pre-existing parent to use its own creation time, rather than inheriting the parent's historical timestamp. The correction selects only the deterministic `kbcat-company-admin-leaf-` rows, requires the linked parent/leaf shape and system-default levels, and excludes the deterministic parent created by the original migration; therefore it does not change chains that were already created together. EF detects the migration and confirms no model/snapshot drift. CH-3 is unchanged by this timestamp-only update.

## Issues Found — Phase 1 (TARGETED-1)

None. The former CH-2/CH-6 issue is resolved. The unavailable local PostgreSQL process prevented repeating the live migration-history/timestamp query in this round; it is a verification-environment limitation, not an implementation finding.

## Review Outcome — Phase 1 and Phase 2 (TARGETED-1)

Phase 1 accepted functionally after TARGETED-1 (15/15), pending a future FULL QA round before `devops` and the existing Phase 1 security audit. Phase 2 remains accepted from FULL-1 (7/7), pending its existing security audit. No security role was invoked by QA.

## Change Log

- 2026-08-21 — QA TARGETED-1: corrective timestamp migration verified; former CH-2/CH-6 finding resolved, Phase 1 is 15/15 verified (TARGETED). Security gates remain open.
