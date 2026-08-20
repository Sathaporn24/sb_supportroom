# Phase 3: Durable indexing queue & failure reporting — Round 1 (FULL, 2026-08-20)

This is the archived record of the first-ever QA round's findings on Phase 3, before the
cross-company leak/IDOR it found was fixed and re-verified. Superseded by the TARGETED round
recorded in the current `review.md` (same date, later pass) — see that file for the closure
confirmation. Moved here verbatim per `.claude/shared/conventions.md` §4/§archiving so `review.md`
does not have to carry a resolved finding's full detail forever.

## Per-Task Results — Phase 3 (Module C, 🔒 gate) — Round 1 (FULL)

18/21 ✅ Verified, 3 ❌ Failed:
- ✅ `BackgroundJob` entity/enums/`OnModelCreating`/migration, `IBackgroundJobRepository` (`ClaimNext`'s `FOR UPDATE SKIP LOCKED`, `RequeueOrphanedRunning`), `IKnowledgeIndexProvider.DeleteVectorsAsync` + Pinecone implementation (1000-id batching, split request types), `BackgroundJobHostedService` (DI-11/DI-12: requeue-on-start, 5s polling), `IDocumentResourceService.UploadAsync` (DI-2 synchronous content-type check), DI-5's outcome mapper, DI-13's transactional delete (chunk-read-before-delete, per-namespace `vector_delete` jobs — confirmed as the real closure of the Phase-3→4 deviation), DI-14 (blocks deleting a lesson's PDF source), DI-16/DI-17, the `willRetryAt`/`hasPendingVectorDelete` ViewModel enrichment (correctly `CompanyId`-filtered against `BackgroundJob`, unlike the bug below), both DI-5/DI-9 unit tests, and all 3 frontend items (`DocumentUploadList.tsx`'s failure-reason mapping + `willRetryAt` line, the pending-vector-delete badge, `DeletedDocumentsList.tsx`'s restore UI).
- ❌ **`GET /api/documents/{id}/deleted`**, **`POST /api/documents/{id}/restore`**, and **DI-15's implementation** — the endpoints exist and the happy-path logic (soft-delete-clear, re-enqueue) is correct, but the underlying `GetDeleted()` repository call leaks and allows restoring other companies' documents.

## Open Issue — closed by the TARGETED round (originally found here)

**Cross-company data leak + IDOR**: `IDocumentResourceRepository.GetDeleted()` (`SupportRoom.Providers.Data/Repository/IDocumentResourceRepository.cs`) calls `Context.DocumentResource.IgnoreQueryFilters().Where(x => x.IsDelete)` — this strips the *entire* combined `HasQueryFilter(CompanyId == ... && !IsDelete)`, not just the delete half. Neither `IDocumentResourceService.GetDeleted()`/`RestoreAsync()` nor `DocumentsController` re-applies a `CompanyId` check. Result: any authenticated admin/cs user from Company A can see (`GET /api/documents/deleted`) and restore (`POST /api/documents/{id}/restore`) Company B's soft-deleted documents. Reachable from the real UI (`DeletedDocumentsList.tsx` under `/admin/documents`). The only unit test on this path (`GetDeleted_ReturnsOnlySoftDeletedDocuments`) ran against a flat in-memory fake with no query-filter concept, so it could not catch this — unlike `ITrainingLinkRepository.GetByToken`, which has a dedicated real-`ApplicationDbContext` cross-company test.

## Issue routed — Round 1

1. **[Phase 3, backend-engineer]** Fix `IDocumentResourceRepository.GetDeleted()` to filter by `CompanyId` as well as `IsDelete` (the `IgnoreQueryFilters()` call needs a `companyContext` dependency and an explicit `.Where(x => x.CompanyId == companyContext.CompanyId && x.IsDelete)`, matching the pattern the entity's own `HasQueryFilter` already expresses). Add a real-`ApplicationDbContext` test analogous to `CompanyIsolationTests.LookingUpALinkByTokenCrossesTheFilterAndSwitchesTheRequestToThatCompany`, proving `GetDeleted()` only ever returns the current company's rows even when two companies both have soft-deleted documents. This is a plain implementation bug — the fix doesn't require any design decision.

## Round 1 Review Outcome (Phase 3 portion)

Not accepted as-is — sent back to `backend-engineer`. One of two ❌/routing-worthy findings across the whole module that triggered the hard-stop (any ⚠️/❌ result asks the user rather than self-accepting).
