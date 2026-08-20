# Phases 1–6 — Round 1 (FULL) + Round 2 (TARGETED), 2026-08-20

This is the archived record of the first two QA rounds on this module, covering Phases 1–6
(118 `plan.md` tasks) before Phase 7 landed. Superseded by Phase 7's FULL round (same module,
later phase) becoming the current round in `review.md` — see that file for what's current now.
Moved here verbatim per `.claude/shared/conventions.md` §4 so `review.md` does not have to carry
a closed round's full per-task detail forever. Phase 3's own Round-1-superseded-by-Round-2 detail
was already archived separately before this move — see `review/phase-3.md`.

**Note on the R3 category-write-path issue this round found**: the ⚠️ Partial item on Phase 1
(TX-5 unreachable) and the routed-to-`system-analyst` Open Issue below were both closed by Phase 7
landing (`design.md`'s DS-1..DS-12 amendment, `plan.md`'s Phase 7, and the engineers' implementation)
— see the current `review.md` for the closure confirmation. They are kept here exactly as this
round wrote them, unedited, for history.

## Verification Summary (as written by Round 1 + Round 2)

**Round 2 — Mode: TARGETED (Phase 3 re-check, same date 2026-08-20).** Re-checks the fix for the cross-company data leak/IDOR that Round 1 (FULL, below) found in `IDocumentResourceRepository.GetDeleted()`. Scope per the TARGETED rules: the fix itself, every Phase 3 task touching the same files (`GetDeleted()`'s callers), the shared-code watchlist, the full Data Model contract, and a whole-project `typecheck`/`lint`/`build`/`test` run on both sides.

- **The fix, verified by direct code read (not taken on the implementer's word):**
  - `IDocumentResourceRepository.GetDeleted(string companyId)` (`backend/src/SupportRoom.Providers.Data/Repository/IDocumentResourceRepository.cs`) now takes `companyId` as a required parameter and reapplies `.Where(x => x.CompanyId == companyId && x.IsDelete)` after `IgnoreQueryFilters()`, with an XML doc explaining why the reapplication is necessary. Confirmed by reading the file directly.
  - `IDocumentResourceService.GetDeleted()` calls `_repository.GetDeleted(CurrentCompanyId)`. `RestoreAsync()` calls `_repository.GetDeleted(CurrentCompanyId).SingleOrDefault(x => x.Id == id)` and additionally calls `guard.EnsureCanAccessCompany(entity.CompanyId)` as defense-in-depth, with a comment explaining why the extra guard exists given this is exactly the check that was missing before. Confirmed by direct read of `IDocumentResourceService.cs` lines 137–230.
  - `IBackgroundJobProcessor.ProcessVectorDeleteAsync` now calls `documentRepository.GetDeleted(companyContext.CompanyId!)` instead of an unscoped call — confirmed `companyContext.Resolve(job.CompanyId)` (DI-4) runs earlier in `ProcessAsync`, before this method is ever reached, so `companyContext.CompanyId` here is genuinely the job's own company, not attacker-controlled or stale.
  - `FakeDocumentResourceRepository.GetDeleted` in `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` signature updated to match (`GetDeleted(string companyId)`), filtering by `CompanyId` — confirmed by direct read.
  - New test `GetDeletedOnlyReturnsTheCallersCompanysSoftDeletedDocuments` in `CompanyIsolationTests.cs` — confirmed it instantiates the **real** `DocumentResourceRepository(_db)` against the shared `ApplicationDbContext` fixture (`_db`), not `FakeDocumentResourceRepository`, seeds one soft-deleted document per company, and asserts `GetDeleted(CompanyA)` returns exactly `doc-a` and never a `CompanyB`-owned row. This is the same shape of proof `LookingUpALinkByTokenCrossesTheFilterAndSwitchesTheRequestToThatCompany` already used for the `TrainingLink` exception, so the regression is now covered by a test that actually exercises EF Core's query-filter mechanics, not fake in-memory list filtering.
  - `grep -rn "IgnoreQueryFilters"` across the whole backend confirms exactly four real call sites: `ITrainingLinkRepository.GetByToken` (intentional, documented — no company known yet at lookup time), `IBackgroundJobRepository.ClaimNext`/`RequeueOrphanedRunning` (intentional — `BackgroundJob` has no `HasQueryFilter` at all by design, confirmed again this round by reading `ApplicationDbContext.OnModelCreating`), and the now-fixed `IDocumentResourceRepository.GetDeleted`. No other instance of the same bug shape exists.
- **Blast radius**: every other Phase 3 task in `plan.md` touching `IDocumentResourceRepository`/`IDocumentResourceService`/`IBackgroundJobProcessor` was re-read in full (DI-2, DI-5, DI-13, DI-14, DI-16, DI-17, the ViewModel enrichment) — no regression, all still match `design.md` exactly as Round 1 found.
- **File manifest comparison** (see updated table below): stat/line-count re-check of every file in Round 1's manifest found exactly 4 changed (`IDocumentResourceRepository.cs`, `IDocumentResourceService.cs`, `IBackgroundJobProcessor.cs`, `CompanyIsolationTests.cs`) plus one file touched that wasn't in the original manifest (`Fakes/ServiceTestFakes.cs`, a test-only file). `find ... -newer review.md` (mtime-based) independently confirms the same 5 files and nothing else in `backend/src`, `backend/tests`, or `frontend/src` changed since Round 1. No new source files (`Glob`/`find` swept the module's directories).
- **Shared-code watchlist** (`ApplicationDbContext.cs`, `IAuthorizationGuard.cs`, `api-client.ts`, `domain.ts`, shared layouts) — all confirmed byte/line-identical to Round 1's manifest, i.e. untouched. `frontend/src/lib/api-client.ts`'s `getDeletedDocuments`/`restoreDocument` calls still target `/api/documents/deleted` and `/api/documents/{id}/restore`, matching `DocumentsController`'s routes exactly — wiring intact.
- **`plan.md` updated**: Phase 3's three previously-unchecked tasks (DI-15, `GET /api/documents/{id}/deleted`, `POST /api/documents/{id}/restore`) are now `[x]` — Phase 3 is 21/21 ✅ Verified.

**Automated checks — all re-run independently this round:**

- Backend: `dotnet build SupportRoom.slnx` → **0 Warning(s), 0 Error(s)**.
- Backend: `dotnet test SupportRoom.slnx --filter "Category!=Integration"` → **190/190 passed** (21 `SupportRoom.Providers.Tests` + 168 `SupportRoom.Application.Tests` + 1 `SupportRoom.Api.IntegrationTests`) — one more than Round 1's 189, exactly the new regression test, confirmed by an independent run.
- Backend: `dotnet ef migrations has-pending-model-changes --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api` → **"No changes have been made to the model since the last migration."** (unchanged — this fix is repository-level, no schema impact, consistent with the fix's own scope).
- Frontend (Node 22 via nvm): `npm run typecheck` → clean. `npm run lint` → clean. `npm run test` (Vitest) → **36/36 passed**. `npm run build` → succeeds, all 21 routes compile.

**Outcome: fix confirmed genuinely closed.** Item goes from ❌ to ✅ after 1 re-check round (see `Open Issues` table history in the archive). Per Sequencing Notes in `plan.md`, Phase 7 (which reuses this exact `BackgroundJob`/soft-delete pattern for DS-6) may now start.

---

**Round 1 — Mode: FULL.** First-ever QA round for this module — no prior `review.md`/`review/phase-N.md` existed. Covers all 6 phases (118 `plan.md` tasks) from scratch: every entity, migration, repository, service, controller, provider, and frontend page/component/type touched by the module was read against `design.md`'s Data Model and the five contract sections (Taxonomy, Knowledge Scope & Retrieval, PDF Narration, Q&A Queue, Document Intake & Job), and against `requirement.md`'s R1–R6/P1–P9. (Phase 3's per-task detail from this round has moved to `review/phase-3.md` now that the TARGETED round above supersedes it; everything below is unchanged for Phases 1, 2, 4, 5, 6.)

**Automated checks — all re-run independently by Round 1, not taken on the implementer's word:**

- Backend: `dotnet build SupportRoom.slnx` → **0 Warning(s), 0 Error(s)**.
- Backend: `dotnet test SupportRoom.slnx --filter "Category!=Integration"` → **189/189 passed** (21 `SupportRoom.Providers.Tests` + 167 `SupportRoom.Application.Tests` + 1 `SupportRoom.Api.IntegrationTests`) — matches the number the implementer reported in `status.md`, confirmed by an independent run, not copied from it. (Superseded by Round 2's 190/190 above.)
- Backend: `dotnet ef migrations has-pending-model-changes --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api` → **"No changes have been made to the model since the last migration."**
- Frontend (Node 22 via nvm, per project convention): `npm run typecheck` → clean. `npm run lint` → clean. `npm run test` (Vitest) → **36/36 passed** (4 files). `npm run build` → succeeds, all 21 routes compile including all 6 phases' new pages.

**This project has a real, non-trivial test suite** (190 backend + 36 frontend as of Round 2), so the `## Unverified Behaviour` framing for "no test suite" projects doesn't apply literally here — but the suite's own design (see `R-12` in `design.md` and the file-header comment on `CompanyIsolationTests.cs`) is explicit that it covers *pure logic* (namespace resolution, DI-5's outcome mapping, DI-9's backoff, QQ-1's queue definition) and *entity-level* query-filter configuration, not every repository method's actual filtering behavior — `SupportRoom.Api.IntegrationTests` is still the template project (1 trivial test), exactly as `CLAUDE.md`'s Known Baseline Issues already records. The cross-company bug Round 1 found is precisely the kind of gap that gap leaves open, and it is now covered by a dedicated real-`ApplicationDbContext` regression test — see `## Unverified Behaviour` (kept in the current `review.md`, not here, since these phases are still undeployed) for what else a reader should not assume is machine-verified.

**Data Model contract check — full, field by field, all 16 sections**, scoped per `.claude/shared/conventions.md` §7 (this project uses EF Core/PostgreSQL, so the comparison is entities + migrations vs. `design.md`'s Data Model, not `schema.prisma`):

- DM-1 `KnowledgeCategory`, DM-2 `LessonConfig.CategoryId`, DM-3 `DocumentResource` (incl. the three `init`→`set` changes), DM-4 `DocumentChunk`, DM-5 `LessonSlideNarration`, DM-6 `KnowledgeQnA`, DM-7 `KnowledgeQnASource`, DM-8 `KnowledgeQnAConflict` — all match field-for-field, including nullability, `required`, and doc-comment intent (self-default chain, soft-delete semantics, etc). DM-9 (`QuestionQueueDismissal`) correctly does not exist anywhere in code, matching its "cut" status.
- DM-10 `BackgroundJob`, DM-11 constants (`KnowledgeScopeType`, `DocumentFailureReason`, `BackgroundJobType`, `BackgroundJobStatus`, plus the additional `KnowledgeSourceType` the Phase 2 handoff added — consistent with the project's `static class` convention, not a contract violation) — match.
- DM-12 `KnowledgeNamespaces.ForCategory`, DM-13 `IKnowledgeIndexProvider.DeleteVectorsAsync` (plus `UpdateMetadataAsync`, a documented Phase 6 extension) — match, including the 1000-id-per-request batching and the split `DeleteAllRequest`/`DeleteByIdsRequest` types.
- DM-14 `KnowledgeSourceChunk.EmbedText` and the one-line `chunk.EmbedText ?? chunk.Text` change — match exactly.
- DM-15 `ApplicationDbContext.OnModelCreating` — every index, every `HasQueryFilter` (including the deliberate absence on `BackgroundJob` and the deliberate presence of `&& !x.IsDelete` only on the new tables) — matches line for line. `SessionQuestion`'s two new indexes are the *only* change to that `learning-session`-owned entity, confirmed no field was touched (R-9).
- DM-16 repositories — all 9 new/changed repositories exist, are registered in `UnitOfWork.Register`, and expose exactly the methods `design.md` specifies. `GetSystemDefault()` correctly filters `IsSystemDefault && Level == 2` before `SingleOrDefault()` (R-13's fail-fast requirement).

No model in `schema`/entities that `design.md` doesn't account for, and no model `design.md` declares that's missing from the entities — the two are in agreement except for the write-path gap noted in Open Issues (a missing *feature*, not a schema drift: the `category` value of `ScopeType` exists correctly on the entity, it's just never reachable). **This gap is closed now — see the current `review.md`.**

**Cross-module contract check confirmed**: `SessionQuestion` only gained two indexes, no field changes — `learning-session`'s own entity is untouched, matching what its `design.md`/`review.md` would need to know about.

**DI-13 deviation closure (flagged in `status.md` after Phase 3, claimed closed in Phase 4) — confirmed genuinely closed.** `IDocumentResourceService.DeleteAsync` now reads `DocumentChunk` rows (written by the Phase 3/4 worker) *before* soft-deleting them, groups by `NamespaceKey`, and writes `VectorId`s into `VectorDeleteJobPayload` — no re-download/re-extract fallback remains anywhere in the code. The `BackgroundJobProcessor` comment describing the old Phase-3-only workaround has been removed, matching the claim.

**QQ-1's split implementation (repository does the `NotFound`/`Incorrect` half, `KnowledgeQnAService.GetQueue()` does the "no `KnowledgeQnASource` yet" half via one batched `GetBySessionQuestionIds` call) — confirmed behaviourally correct**, not just built-and-tested-separately: `KnowledgeQnAService.GetQueue()` composes the two halves in the order the contract requires (repository filter → batched exclusion → QQ-4 join), and `KnowledgeQnAServiceTests.cs`'s 9 cases exercise the composed result, not each half in isolation.

**The Phase-6 DI bug** (`KnowledgeNamespaceResolver` constructor-injecting repositories directly instead of resolving them through `IUnitOfWork`, caught only by restarting the real app) **is confirmed fixed** — the class now takes `IUnitOfWork` and resolves both repositories in its constructor body, matching the project-wide pattern every other service uses. No other service in the 6 phases repeats this mistake (checked every `Services/I*.cs` file added this module — all take `IUnitOfWork unitOfWork` and call `.GetRepository<T>()`).

**Security-relevant items checked specifically because the user asked**:

- `GET /api/documents/{id}/chunks` (Module D's named gate concern) — correctly calls `guard.EnsureAuthenticated()` and `guard.EnsureCanAccessCompany(entity.CompanyId)` before returning any chunk content, and `IAuthorizationGuard.EnsureCanAccessCompany` is a real, functioning check (confirmed by reading `AuthorizationGuard`'s implementation) — not a no-op. ✅ **Verified**.
- `GET /api/documents/deleted` and `POST /api/documents/{id}/restore` — ❌ **Failed in Round 1**, ✅ **fixed and re-verified in Round 2** (TARGETED, see Verification Summary above). This was a bug Round 1 found; it was not one the design anticipated by name (Module C's gate reasoning is about `BackgroundJob`/`DeleteVectorsAsync`/`LastErrorDetail` specifically), but it was squarely inside Module C's territory and Phase 3's existing 🔒 gate covered it — no new gate flag was needed on `plan.md`.

## Verified File Manifest — knowledge-base (Phase 1–6), as of Round 2 (superseded — see current `review.md` for the Phase 1–7 manifest)

Files actually opened and read (not exhaustive of every file touched by the module — this lists the ones whose content directly drove a ✅/⚠️/❌ call). Originally populated by Round 1 (FULL, 2026-08-20). Round 2 (TARGETED, same date) re-stat'd every row and found exactly 5 files changed since Round 1 — those rows are marked `[R2]` below with their updated numbers; every other row is unchanged since Round 1 (re-confirmed by Round 2's stat sweep, not just carried over).

| File | Bytes | Lines |
|---|---:|---:|
| backend/src/SupportRoom.Domain/Entities/KnowledgeCategory.cs | 839 | 22 |
| backend/src/SupportRoom.Domain/Entities/LessonConfig.cs | 2505 | 58 |
| backend/src/SupportRoom.Domain/Entities/DocumentResource.cs | 1574 | 36 |
| backend/src/SupportRoom.Domain/Entities/DocumentChunk.cs | 5133 | 59 |
| backend/src/SupportRoom.Domain/Entities/LessonSlideNarration.cs | 2247 | 33 |
| backend/src/SupportRoom.Domain/Entities/KnowledgeQnA.cs | 2989 | 57 |
| backend/src/SupportRoom.Domain/Entities/KnowledgeQnASource.cs | 1296 | 29 |
| backend/src/SupportRoom.Domain/Entities/KnowledgeQnAConflict.cs | 2264 | 44 |
| backend/src/SupportRoom.Domain/Entities/BackgroundJob.cs | 2931 | 60 |
| backend/src/SupportRoom.Domain/Enums/KnowledgeScopeType.cs | 215 | 8 |
| backend/src/SupportRoom.Domain/Enums/DocumentFailureReason.cs | 363 | 10 |
| backend/src/SupportRoom.Domain/Enums/BackgroundJobType.cs | 588 | 12 |
| backend/src/SupportRoom.Domain/Enums/BackgroundJobStatus.cs | 389 | 11 |
| backend/src/SupportRoom.Domain/Enums/KnowledgeSourceType.cs | 611 | 13 |
| backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs | 10601 | 200 |
| backend/src/SupportRoom.Providers.Data/Migrations/20260819082956_AddKnowledgeTaxonomyAndScope.cs | 6832 | 114 |
| backend/src/SupportRoom.Providers.Data/Migrations/20260819122301_AddDurableIndexingJobs.cs | 3025 | 61 |
| backend/src/SupportRoom.Providers.Data/Migrations/20260819124738_AddDocumentChunks.cs | 2721 | 59 |
| backend/src/SupportRoom.Providers.Data/Migrations/20260819130857_AddLessonSlideNarrations.cs | 2366 | 54 |
| backend/src/SupportRoom.Providers.Data/Migrations/20260819134222_AddKnowledgeQnA.cs | 7273 | 144 |
| backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeCategoryRepository.cs | 1413 | 29 |
| backend/src/SupportRoom.Providers.Data/Repository/IDocumentResourceRepository.cs `[R2]` | 1443 | 30 |
| backend/src/SupportRoom.Providers.Data/Repository/IDocumentChunkRepository.cs | 1221 | 33 |
| backend/src/SupportRoom.Providers.Data/Repository/ILessonSlideNarrationRepository.cs | 1627 | 40 |
| backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeQnARepository.cs | 1026 | 24 |
| backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeQnASourceRepository.cs | 1222 | 26 |
| backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeQnAConflictRepository.cs | 816 | 19 |
| backend/src/SupportRoom.Providers.Data/Repository/IBackgroundJobRepository.cs | 3045 | 72 |
| backend/src/SupportRoom.Providers.Data/Data/UnitOfWork/UnitOfWork.cs | 2296 | 43 |
| backend/src/SupportRoom.Application/Services/IKnowledgeCategoryService.cs | 7121 | 154 |
| backend/src/SupportRoom.Application/Services/IKnowledgeNamespaceResolver.cs | 5219 | 88 |
| backend/src/SupportRoom.Application/Services/IKnowledgeIndexingService.cs | 5934 | 134 |
| backend/src/SupportRoom.Application/Services/IBackgroundJobProcessor.cs `[R2]` | 24240 | 500 |
| backend/src/SupportRoom.Application/Services/IDocumentResourceService.cs `[R2]` | 14798 | 310 |
| backend/src/SupportRoom.Application/Services/ILessonSlideNarrationService.cs | 8380 | 191 |
| backend/src/SupportRoom.Application/Services/ILessonSlideNarrationResolver.cs | 2025 | 46 |
| backend/src/SupportRoom.Application/Services/ILessonConfigService.cs | 23873 | 487 |
| backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs | 8569 | 171 |
| backend/src/SupportRoom.Application/Services/IKnowledgeQnAService.cs | 11818 | 276 |
| backend/src/SupportRoom.Application/Services/IKnowledgeQnAConflictService.cs | 1951 | 46 |
| backend/src/SupportRoom.Application/Services/DocumentChunkTextAnalyzer.cs | 1249 | 33 |
| backend/src/SupportRoom.Application/Common/IAuthorizationGuard.cs | 4565 | 115 |
| backend/src/SupportRoom.Application/Dto/KnowledgeCategoryDto.cs | 546 | 20 |
| backend/src/SupportRoom.Application/Dto/KnowledgeQnADto.cs | 1050 | 34 |
| backend/src/SupportRoom.Application/Dto/LessonSlideNarrationDto.cs | 544 | 13 |
| backend/src/SupportRoom.Application/Dto/DtoLimits.cs | 2178 | 41 |
| backend/src/SupportRoom.Application/ViewModel/KnowledgeCategoryViewModel.cs | 694 | 20 |
| backend/src/SupportRoom.Application/ViewModel/DocumentChunkViewModel.cs | 748 | 16 |
| backend/src/SupportRoom.Application/ViewModel/DocumentResourceViewModel.cs | 1405 | 26 |
| backend/src/SupportRoom.Application/ViewModel/KnowledgeQnAViewModel.cs | 2061 | 51 |
| backend/src/SupportRoom.Providers.Knowledge/IKnowledgeIndexProvider.cs | 3917 | 72 |
| backend/src/SupportRoom.Providers.Knowledge/PineconeKnowledgeIndexProvider.cs | 9333 | 246 |
| backend/src/SupportRoom.Providers.VoiceQuestion/IVoiceQuestionProvider.cs | 3520 | 74 |
| backend/src/SupportRoom.Providers.VoiceQuestion/RagVoiceQuestionProvider.cs | 22445 | 325 |
| backend/src/SupportRoom.Api/Controllers/KnowledgeCategoriesController.cs | 1635 | 45 |
| backend/src/SupportRoom.Api/Controllers/DocumentsController.cs | 3194 | 97 |
| backend/src/SupportRoom.Api/Controllers/LessonController.cs | 5335 | 119 |
| backend/src/SupportRoom.Api/Controllers/KnowledgeQnAController.cs | 1059 | 31 |
| backend/src/SupportRoom.Api/Controllers/QnaQueueController.cs | 744 | 20 |
| backend/src/SupportRoom.Api/Controllers/KnowledgeQnAConflictsController.cs | 1163 | 26 |
| backend/src/SupportRoom.Api/BackgroundJobHostedService.cs | 3118 | 76 |
| backend/src/SupportRoom.Api/Configurations/AuthenticationConfiguration.cs | 4053 | 81 |
| backend/src/SupportRoom.Api/Configurations/ServiceConfiguration.cs | 5886 | 98 |
| backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs `[R2]` | 11485 | 252 |
| backend/tests/SupportRoom.Application.Tests/DocumentResourceServiceTests.cs | 9228 | 256 |
| backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs `[R2, new to manifest]` | 23787 | 446 |
| frontend/src/types/domain.ts | 19664 | 541 |
| frontend/src/lib/api-client.ts | 24109 | 605 |
| frontend/src/app/admin/categories/page.tsx | 4019 | 103 |
| frontend/src/components/admin/CategoryTree.tsx | 5428 | 142 |
| frontend/src/components/admin/CategoryMovePreviewDialog.tsx | 4921 | 129 |
| frontend/src/components/admin/DocumentUploadList.tsx | 9681 | 203 |
| frontend/src/components/admin/DeletedDocumentsList.tsx | 4274 | 101 |
| frontend/src/app/admin/documents/[id]/chunks/page.tsx | 5339 | 103 |
| frontend/src/app/admin/lessons/new/page.tsx | 11455 | 277 |
| frontend/src/app/admin/lessons/[slug]/narrations/page.tsx | 7654 | 164 |
| frontend/src/app/admin/qna-queue/page.tsx | 6112 | 137 |
| frontend/src/components/admin/KnowledgeQnAAnswerDialog.tsx | 6892 | 181 |
| frontend/src/app/admin/qna-conflicts/page.tsx | 4406 | 103 |
| frontend/src/app/admin/lessons/[slug]/page.tsx | 25067 | 562 |

## Per-Task Results — Phase 1–6 (as written by Round 1/2)

**Phase 1 (Module A)** — 14/15 ✅ Verified, 1 ⚠️ Partial:
- ✅ `KnowledgeCategory` entity, `LessonConfig.CategoryId`, `DocumentResource` DM-3 changes, `KnowledgeScopeType`/`DocumentFailureReason` enums, `OnModelCreating` changes, `AddKnowledgeTaxonomyAndScope` migration (MG-A1..A7, including the default-chain backfill), rollback warning, `IKnowledgeCategoryRepository` (incl. `GetSystemDefault()` fail-fast), `IDocumentResourceRepository`/`ILessonConfigRepository` changes, `UnitOfWork.Register`, `IKnowledgeCategoryService` (TX-1/2/3/6/11), TX-7 slug validation — all match `design.md` exactly, code inspected directly.
- ✅ All 5 category endpoints, `PUT /api/lessons/{id}/category`, DTO/ViewModel, all 4 frontend items (`domain.ts` types, `api-client.ts` methods, `/admin/categories` page + `CategoryTree`/`CategoryFormDialog`, category dropdown + `CategoryMovePreviewDialog` in the lesson editor), unit tests — verified by direct read.
- ⚠️ **Partial** (**closed by Phase 7 — see current `review.md`**): "เพิ่ม validation TX-4/TX-5 สำหรับ `LessonConfig.CategoryId` และ `DocumentResource`" — TX-4 (lessons) is fully implemented and enforced (`ValidateCategory` in `ILessonConfigService`). TX-5 (documents) is not reachable: no code path ever sets `DocumentResource.ScopeType = "category"`, so the validation has nothing to guard. See Open Issues.

**Phase 2 (Module B, 🔒 gate)** — 11/12 ✅ Verified, 1 ⚠️ Partial (carried forward, not new):
- ✅ `KnowledgeNamespaces.ForCategory`, `IVoiceQuestionProvider.CategoryNamespace` (required), `RagVoiceQuestionProvider`'s 3-namespace `Task.WhenAll` query + `MergeTopK`, `VoiceQuestionService`'s resolver call, `IKnowledgeNamespaceResolver`/`EnsureValidScope` (KS-1/KS-2), `sourceType` metadata on document/slide indexing paths (moved into the Phase-3 worker + `IAdminService`'s reindex-all, confirmed present in both), the metadata reader's "no sourceType = document" fallback, KS-11's fallback-to-full-deck behaviour, the resolver unit tests — all verified against code and the 7 new resolver tests plus the merge/retrieval logic in `RagVoiceQuestionProvider.cs`.
- ⚠️ Latency measurement task — not a code gap, blocked on a real deployment with traffic (unchanged since the Phase 2 backend handoff). **Still open — see current `review.md`.**

**Phase 3 (Module C, 🔒 gate)** — 21/21 ✅ Verified (Round 2, TARGETED, 2026-08-20; Round 1's 18/21+3❌ detail archived to `review/phase-3.md`):
- ✅ Everything Round 1 already verified (`BackgroundJob` entity/enums/`OnModelCreating`/migration, `IBackgroundJobRepository`, `IKnowledgeIndexProvider.DeleteVectorsAsync` + Pinecone implementation, `BackgroundJobHostedService`, `IDocumentResourceService.UploadAsync`, DI-5's outcome mapper, DI-13's transactional delete, DI-14, DI-16/DI-17, the ViewModel enrichment, both DI-5/DI-9 unit tests, all 3 frontend items) — re-confirmed unchanged this round (file manifest stat/line match).
- ✅ **`GET /api/documents/deleted`, `POST /api/documents/{id}/restore`, and DI-15's implementation** — now genuinely correct. `IDocumentResourceRepository.GetDeleted(companyId)` reapplies `CompanyId` after `IgnoreQueryFilters()`; `IDocumentResourceService.GetDeleted()`/`RestoreAsync()` pass `CurrentCompanyId` and `RestoreAsync` adds an explicit `guard.EnsureCanAccessCompany` as defense-in-depth; `IBackgroundJobProcessor.ProcessVectorDeleteAsync` (the caller that was missed originally) now passes `companyContext.CompanyId` too. Proven by a new regression test (`GetDeletedOnlyReturnsTheCallersCompanysSoftDeletedDocuments` in `CompanyIsolationTests.cs`) that runs against the real `ApplicationDbContext`, not a fake, and seeds soft-deleted documents for two companies to prove only the caller's own company's rows come back. `grep -rn "IgnoreQueryFilters"` across the whole backend confirms no other instance of the same bug shape exists (the two remaining call sites — `TrainingLink.GetByToken`, `BackgroundJob.ClaimNext`/`RequeueOrphanedRunning` — are both intentional and unrelated, confirmed by reading their surrounding code again this round). `plan.md`'s three previously-unchecked Phase 3 tasks are now `[x]`.

**Phase 4 (Module D, 🔒 gate)** — 10/10 ✅ Verified:
- `DocumentChunk` entity/index/migration, `IDocumentChunkRepository`, the Phase-3 worker's DI-8 replace-whole-set write, `DocumentChunkTextAnalyzer.HasSuspectCharacters` (DI-6, matches the exact character-class definition), `GET /api/documents/{id}/chunks` with explicit `EnsureAuthenticated`/`EnsureCanAccessCompany` guards (confirmed the guard is a real, functioning check, not a no-op), `DocumentChunkViewModel`, and the frontend chunks-viewer page (suspect-row sort/highlight, "แปลงไม่ได้" empty state) — all verified by direct read, all match DI-6/DI-7/DI-8.

**Phase 5 (Module E, no gate)** — 15/15 ✅ Verified:
- `LessonSlideNarration` entity/index/migration, `ILessonSlideNarrationRepository`, `ILessonSlideNarrationResolver` (NR-1, one resolver used by both the tutor path and indexing), `GET`/`PUT /api/lessons/{id}/narrations[...]` with NR-2's exact "trim-and-compare-to-prefill" logic and NR-9's server-side reject, the `lesson_index` job enqueue (NR-6), NR-3's transactional wipe-on-PDF-replace (confirmed same-transaction with the `PdfDocumentResourceId` write), NR-7's newly-opened PDF-lesson index path, NR-8's sourceType separation, `POST /api/lessons` (P9/Q4), and all 3 frontend items (`/admin/lessons/[slug]/narrations` page with `isLikelyScanned` warning and `isOverridden` badge, the pre-upload confirm flow via `getLessonNarrationCount`, `/admin/lessons/new`) — all verified by direct read.

**Phase 6 (Module F, 🔒 gate)** — 30/30 ✅ Verified:
- `KnowledgeQnA`/`KnowledgeQnASource`/`KnowledgeQnAConflict` entities/index/migration, `KnowledgeSourceChunk.EmbedText` + the one-line `IndexChunksAsync` change, all 3 new repositories, `ISessionQuestionRepository.GetReviewQueue()` (QQ-1's first half), `IKnowledgeQnAService` (QQ-1's second half composed correctly in `GetQueue()`, `EnsureValidScope` call for KS-2/TX-5, `VectorId = Id`, QQ-6's re-embed-only-if-Question-changed, QQ-5's transactional source soft-delete, QQ-7/QQ-8/QQ-9), `RagVoiceQuestionProvider`'s KS-7/KS-8/KS-9 prompt (two labelled blocks, "yield to block 1" instruction, ban-on-copying instruction, `conflict` field), `VoiceQuestionService.TryRecordConflict` (KS-9/KS-10, validated against the company-scoped repository, isolated in its own try/catch), all 6 endpoints, DTOs/ViewModels, the QQ-1 unit test suite, and all 6 frontend items (types, api-client methods, the queue page, the answer dialog with QQ-7/QQ-8's editable-scope-prefill, the conflicts page) — all verified by direct read. `IKnowledgeIndexProvider.UpdateMetadataAsync` (the documented Phase-6 addition beyond the original DM-13) is used correctly and only for the "Question unchanged" QQ-6 branch.

## Design/requirement contract checks — Phase 1–6

Full field-by-field comparison performed per `.claude/shared/conventions.md` §7 — see `## Verification Summary` above for the section-by-section result. All 7 new tables and 3 modified entities in this module's `design.md` Data Model exist in the real entities/migrations and match. No entity in the codebase that this module owns is unaccounted for in `design.md`. `SessionQuestion` (owned by `learning-session`) was correctly treated as out-of-scope beyond its two new indexes, consistent with R-9 and `.claude/shared/conventions.md` §7's cross-module rule — a `Grep` for `model SessionQuestion`-equivalent (`class SessionQuestion`) confirms `learning-session/design.md` is the actual owner.

## Issues Found — Phase 1–6 (as written by Round 1/2)

~~1. **[Phase 3, backend-engineer]** Fix `IDocumentResourceRepository.GetDeleted()`...~~ **Closed by Round 2 (TARGETED, 2026-08-20)** — see `review/phase-3.md` for the original finding and the Verification Summary above for the fix confirmation.

~~1. **[Phase 1/cross-phase, system-analyst]** Decide and specify the concrete contract for assigning a `DocumentResource` to `ScopeType = "category"`...~~ **Closed by Phase 7 (FULL, 2026-08-20)** — `system-analyst` amended `design.md` with DS-1..DS-12, `project-manager` added Phase 7 to `plan.md`, and the engineers implemented it. See the current `review.md` for the closure confirmation.

## Review Outcome — Phase 1–6 (as written by Round 1/2)

**Round 2 (TARGETED, Phase 3 re-check) — accepted.** The cross-company leak/IDOR is confirmed genuinely fixed by direct code inspection, a real-`ApplicationDbContext` regression test, and independent re-runs of build/test/typecheck/lint/build on both sides. Phase 3 is now 21/21 ✅ Verified with zero open items. Per `.claude/shared/conventions.md` §6, since this TARGETED round found no ⚠️/❌ (everything in its scope came back ✅), and the module as a whole still carries Phase 1's open ⚠️ item, this outcome does not qualify for the autonomous all-✅ exception on its own — reported to the user as a plain accepted result for this round's scope.

- **Phase 3 is not yet eligible for `devops`.** Its most recent round was TARGETED, not FULL — per the deploy-eligibility rule, that makes Phase 3 "accepted, pending a FULL round before deploy," not cleared to hand to `devops` on this round's strength alone. It also still carries its own 🔒 gate — `security` has not run on any phase in this module yet, an independent, harder blocker than the round-mode one. Since Phase 3 is functionally closed now, the cheapest path is likely to fold its confirmation into whichever later FULL round closes the module (e.g. once Phase 1's R3 gap and Phase 7 land), rather than spending a dedicated FULL round on Phase 3 alone right now.
- **Phase 7 may now start** — the Sequencing Notes in `plan.md` blocked Phase 7's engineers from beginning DS-3/DS-6 (which reuse the same `BackgroundJob`/soft-delete pattern) until this fix passed QA TARGETED confirmation. That condition is now met.
- Phases 4, 5 remain fully ✅ Verified with no open items — Phase 4 still carries its own 🔒 gate (`security` has not run on any phase yet) and Phase 5 has no gate. Their last round was FULL (Round 1), so they remain deploy-eligible on mode grounds, still blocked only by the pending `security` audit where gated.
- Phases 1, 2, 6 are unchanged by this round: Phase 1 still has the open R3 document-category-scope ⚠️ (routed to `system-analyst`, now specified via `design.md`'s DS-1..DS-12 amendment and `plan.md`'s Phase 7 — awaiting Phase 7's code before it can close), Phase 2 still has the non-blocking latency-measurement item. Neither is closed by this round; neither was in this round's scope.
- **No phase in this module has had a `security` audit yet** — Phases 2, 3, 4, 6 all carry `🔒 Security gate` in `plan.md` (Phase 7 also carries one now) and `devops` cannot ship any of them until `security` runs, independent of round mode.
