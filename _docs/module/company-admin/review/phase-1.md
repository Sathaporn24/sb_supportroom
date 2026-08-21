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
