# Phase 12: Lesson trash, restore & permanent purge (R9) — Module L

## Verification Summary (Round 14)

**Round 14 — Mode: FULL — Phase 12 / Module L, first QA round, all 31 tasks inspected from scratch against R9, DM-2/DM-11/DM-15/DM-16/DM-18, MG-L1, and LT-1..LT-24.**

**Result: 15/31 ✅ Verified, 4/31 ⚠️ Partial, 12/31 ❌ Failed. Phase 12 remains open and is not deploy-eligible.** Fifteen verified checkboxes were changed to `[x]` in `plan.md`; every Partial/Failed task remains `[ ]`. The standing `🔒 Security gate` remains open and was not run in this QA role.

Security-sensitive inspection found **no Critical cross-company leak** in the Module L repository bypasses: every scoped `IgnoreQueryFilters()` path re-applies `CompanyId` in the same predicate. `ITrainingLinkRepository.GetByToken` remains the explicit DM-16 public-credential exception and resolves the company immediately in service code. Module L raw SQL uses EF parameter placeholders; no interpolated SQL injection path was found. The external purge order, shared-PDF guard, and question-exclusion-before-Q&A deletion order match LT-15..LT-20.

Automated checks:

- Frontend `npm run typecheck` ✅ clean.
- Frontend `npm run lint` ✅ clean.
- Frontend `npm run test` ✅ 79/79, 11 files.
- Frontend `npm run build` ✅ Next.js 15.5.22, 24 routes generated.
- Backend `dotnet build SupportRoom.slnx -c Release --no-restore` ✅ 0 warnings, 0 errors.
- Backend FULL `dotnet test SupportRoom.slnx -c Release --no-build` ❌ 323 passed / 11 failed. All 11 failures are external integration/configuration failures (Google credential, TTS/network, Pinecone/network), not Module L assertions.
- Backend `dotnet test SupportRoom.slnx -c Release --no-build --filter "Category!=Integration"` ✅ 320/320.
- EF `dotnet ef migrations has-pending-model-changes --no-build --configuration Release ...` ✅ no pending model changes.

## Verified File Manifest — Phase 12 (Round 14)

Files inspected in the first FULL round.

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `_docs/module/knowledge-base/requirement.md` | 186713 | 981 | Round 14 |
| `_docs/module/knowledge-base/design.md` | 648520 | 2544 | Round 14 |
| `_docs/module/knowledge-base/plan.md` | 217568 | 1049 | Round 14 |
| `frontend/docs/API_CONTRACT.md` | 23320 | 278 | Round 14 |
| `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` | 16009 | 413 | Round 14 |
| `backend/src/SupportRoom.Domain/Entities/LessonConfig.cs` | 3996 | 76 | Round 14 |
| `backend/src/SupportRoom.Domain/Entities/SessionQuestionReviewExclusion.cs` | 2087 | 39 | Round 14 |
| `backend/src/SupportRoom.Domain/Enums/BackgroundJobType.cs` | 847 | 16 | Round 14 |
| `backend/src/SupportRoom.Domain/Enums/BackgroundJobStatus.cs` | 691 | 16 | Round 14 |
| `backend/src/SupportRoom.Domain/Enums/LessonTrashStatus.cs` | 1053 | 28 | Round 14 |
| `backend/src/SupportRoom.Domain/Enums/QuestionReviewExclusionReason.cs` | 410 | 9 | Round 14 |
| `backend/src/SupportRoom.Domain/Configuration/ServerDefaults.cs` | 17253 | 370 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs` | 12839 | 223 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Data/UnitOfWork/UnitOfWork.cs` | 2469 | 44 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Data/UnitOfWork/IUnitOfWork.cs` | 160 | 7 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260826151755_AddLessonTrashLifecycle.cs` | 3605 | 84 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Migrations/ApplicationDbContextModelSnapshot.cs` | 37771 | 1061 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonConfigRepository.cs` | 4724 | 88 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/IBackgroundJobRepository.cs` | 5402 | 111 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/ISessionQuestionReviewExclusionRepository.cs` | 2819 | 64 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/ITrainingLinkRepository.cs` | 1710 | 34 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILearningSessionRepository.cs` | 3393 | 63 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/IDocumentResourceRepository.cs` | 3427 | 59 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/IDocumentChunkRepository.cs` | 2026 | 44 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonSlideNarrationRepository.cs` | 2479 | 51 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonExcludedSlideRepository.cs` | 3379 | 63 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeQnARepository.cs` | 2121 | 41 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeQnASourceRepository.cs` | 1922 | 36 | Round 14 |
| `backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeQnAConflictRepository.cs` | 1517 | 29 | Round 14 |
| `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs` | 51944 | 997 | Round 14 |
| `backend/src/SupportRoom.Application/Services/IBackgroundJobProcessor.cs` | 42256 | 811 | Round 14 |
| `backend/src/SupportRoom.Application/Services/ITrainingLinkService.cs` | 11532 | 234 | Round 14 |
| `backend/src/SupportRoom.Application/Services/ILearningSessionService.cs` | 18919 | 391 | Round 14 |
| `backend/src/SupportRoom.Application/Services/IKnowledgeQnAService.cs` | 19581 | 414 | Round 14 |
| `backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs` | 11846 | 224 | Round 14 |
| `backend/src/SupportRoom.Application/ViewModel/LessonTrashItemViewModel.cs` | 1436 | 31 | Round 14 |
| `backend/src/SupportRoom.Application/Dto/PermanentDeleteLessonDto.cs` | 635 | 15 | Round 14 |
| `backend/src/SupportRoom.Api/Controllers/LessonController.cs` | 9615 | 193 | Round 14 |
| `backend/src/SupportRoom.Api/Controllers/LearningSessionController.cs` | 5226 | 110 | Round 14 |
| `backend/src/SupportRoom.Api/Controllers/TrainingLinkController.cs` | 4034 | 90 | Round 14 |
| `backend/src/SupportRoom.Api/Controllers/TtsController.cs` | 1584 | 38 | Round 14 |
| `backend/tests/SupportRoom.Application.Tests/LessonTrashServiceTests.cs` | 16464 | 405 | Round 14 |
| `backend/tests/SupportRoom.Application.Tests/RevokedLinkPolicyTests.cs` | 8447 | 226 | Round 14 |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 17290 | 395 | Round 14 |
| `backend/tests/SupportRoom.Application.Tests/BackgroundJobProcessingTests.cs` | 2581 | 74 | Round 14 |
| `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` | 33008 | 619 | Round 14 |
| `frontend/src/types/domain.ts` | 28180 | 691 | Round 14 |
| `frontend/src/lib/api-client.ts` | 34340 | 793 | Round 14 |
| `frontend/src/app/admin/lessons/page.tsx` | 14574 | 335 | Round 14 |
| `frontend/src/components/admin/LessonTrashList.tsx` | 7524 | 166 | Round 14 |
| `frontend/src/components/admin/lesson-trash-display.ts` | 2289 | 50 | Round 14 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.tsx` | 4774 | 122 | Round 14 |
| `frontend/src/components/admin/LessonTrashList.test.tsx` | 3256 | 69 | Round 14 |
| `frontend/src/components/admin/lesson-trash-display.test.ts` | 2077 | 43 | Round 14 |

## Per-Task Results — Phase 12 (Round 14)

- ✅ [backend] Entity `LessonConfig` matches DM-2.
- ✅ [backend] `SessionQuestionReviewExclusion` matches DM-18.
- ✅ [backend] Module L constants match DM-11, including `red_today` display enum support.
- ✅ [backend] EF configuration/query filters/indexes match DM-15.
- ⚠️ [backend] MG-L1 migration — schema/Down are correct and EF model is clean, but LT-24 preflight has no script or recorded result (P12-07).
- ❌ [backend] Lesson trash/purge repository methods — scoped bypasses are tenant-safe, but the conditional archive required by the task is absent; `GetPurgeDependencies`/hard-finalization are spread through orchestration rather than the planned repository boundary (P12-01).
- ✅ [backend] Review-exclusion repository and UnitOfWork registration.
- ❌ [backend] Archive service — correct normal-path transaction and link revoke, but concurrent calls can create duplicate jobs (P12-01).
- ❌ [backend] Restore service — correct state fields and 409 normal path, but lesson restore and job cancel are separate transactions (P12-02).
- ❌ [backend] Lifecycle endpoints/DTO/ViewModel — routes and camelCase payloads exist, but trash GET has no `cs` 403 guard (P12-03).
- ✅ [backend] Trash projection and all four urgency bands, including `red_today` ≤24h.
- ✅ [backend] Owner-only manual permanent-delete server logic, trim + ordinal-exact, existing-job acceleration, 202.
- ❌ [backend] Public learner authorization — content gate itself is strict, but resume and progress still use raw-token resolution; token-only resume exposes trashed metadata (P12-04).
- ✅ [backend] Active Q&A queue derives trash state and applies permanent exclusions before source checks.
- ❌ [backend] Durable worker/retry — claim, stale generation, active-session delay, restart requeue are correct; failure 3 incorrectly becomes permanent `failed` (P12-05).
- ✅ [backend] Purge dependency snapshot and permanent question exclusions; no `IKnowledgeQnAService.DeleteAsync` loop.
- ✅ [backend] External deletion order, stored namespace keys, shared-PDF guard, and DB finalization/retention order.
- ✅ [backend] Hard-deleted lesson history fallback "บทเรียนที่ถูกลบ".
- ✅ [backend] Role/archive/restore service tests cover the stated normal state matrix.
- ❌ [backend] LT-23 tenant tests do not cover every trash/job/purge repository bypass and cross-company path (P12-08).
- ⚠️ [backend] LT-5/LT-6 tests cover join/restart and the content-gate helper, but not token-only resume/PDF and endpoint-level question/TTS/progress behavior (P12-08).
- ❌ [backend] Worker timing/reliability tests are absent; existing background-job tests only cover generic mapper/backoff constants (P12-08).
- ❌ [backend] Queue/purge retention/finalization integration tests are absent (P12-08).
- ⚠️ [backend] API/workflow documentation covers endpoints/model and basic workflow, but omits full role and indefinite retry semantics (P12-09).
- ❌ [frontend] Domain/api-client lockstep — types exist, but archive/restore response types do not match backend and empty 202 causes JSON parse failure (P12-06).
- ✅ [frontend] Active/trash tabs and read-only trash surface.
- ✅ [frontend] Countdown/purging UI and no notification UI.
- ✅ [frontend] Archive/restore controls and list refresh behavior.
- ❌ [frontend] Owner permanent-delete dialog — role/title UI is correct, but cannot handle backend's empty 202 because the shared request helper throws while parsing JSON (P12-06).
- ❌ [frontend] Public learner callers/UI — learnerKey is wired on content calls, but token-only resume still receives trashed metadata instead of rejection (P12-04).
- ⚠️ [frontend] Tests cover urgency and role actions, but omit forbidden-control assertions and permanent-delete submission/202 behavior (P12-10).

## Design/requirement contract checks — Phase 12 (Round 14)

- DM-2: `LessonConfig` uses existing soft-delete fields plus only `PurgeJobId`/`PurgeStartedAt`; no duplicate lifecycle enum/state column added.
- DM-18: exclusion entity has standard audit fields, `SessionQuestionId`, `LessonId`, and `Reason`; no physical FK/retention field.
- DM-15/MG-L1: entity, migration, snapshot, and generated model match; migration is additive and contains no backfill/cleanup. Required LT-24 operational preflight remains unproven.
- DM-16/LT-23: all Module L scoped `IgnoreQueryFilters()` methods inspected have same-query `CompanyId` predicates. `GetByToken` is the documented public-token exception. No Critical tenant leak found by code inspection.
- LT-3/LT-4: normal archive transaction is correct; archive concurrency and restore atomicity are not.
- LT-5/LT-6: strict content/PDF helper exists, but authorization is not applied consistently to resume/progress.
- LT-11..LT-14: conditional claim and one-hour active-session deferral are correct; indefinite retry is contradicted by the generic max-attempt failure branch.
- LT-15..LT-20: dependency IDs come from DB, company scope is retained, external deletion ordering/shared PDF guard are correct, and exclusions are inserted before Q&A/source hard delete.
- LT-9: `neutral`, `yellow`, `red`, and `red_today` thresholds match the amended contract on both backend and frontend.

## Issues Found — Phase 12 (Round 14)

All findings are implementation/test/documentation gaps against already-clear contracts; no schema or business decision is needed.

1. **P12-01 Important → backend-engineer:** implement an atomic/conditional archive boundary so concurrent archive requests cannot create duplicate jobs; reconcile repository boundary with the Phase 12 task.
2. **P12-02 Important → backend-engineer:** wrap conditional lesson restore and exact job cancellation in one DB transaction/statement boundary; do not treat cancellation as best-effort bookkeeping.
3. **P12-03 Important → backend-engineer:** enforce the Phase 12 role matrix server-side on the trash-list endpoint (or return to system-analyst only if the project owner intentionally wants to revise LT-2/task wording).
4. **P12-04 Important → backend-engineer:** apply the strict `(token, learnerKey, link id, IN_PROGRESS)` gate to resume/progress and prevent token-only resume from returning link/lesson metadata for revoked links; add endpoint-level regression tests.
5. **P12-05 Important → backend-engineer:** special-case `lesson_purge` after attempt 3 to remain pending with `NextAttemptAt = now + 24h` indefinitely.
6. **P12-06 Important → frontend-engineer:** make the permanent-delete client accept an empty 202 (and correct archive/restore result types); add a dialog/client test that proves success is shown once, not an error after enqueue.
7. **P12-07 Important → backend-engineer/devops:** add the LT-24 preflight query/script and retain evidence of its zero-row result before any environment applies MG-L1.
8. **P12-08 Important → backend-engineer:** add the exact tenant, public endpoint, worker timing/race/retry/shared-PDF, and queue/finalization tests listed in tasks 528–531.
9. **P12-09 Minor → backend-engineer:** complete lifecycle docs with role matrix and indefinite daily retry behavior.
10. **P12-10 Minor → frontend-engineer:** expand tests for forbidden controls and owner-only typed permanent delete.

## Review Outcome — Phase 12 (Round 14)

**Sent back for fixes; awaiting the project owner's routing decision.** Phase 12 is **15/31 ✅, 4/31 ⚠️, 12/31 ❌** on its first FULL round. It cannot close, cannot unblock Phase 13's last two tasks, and cannot reach devops. The Security gate remains independently open; this QA result does not replace it.

## Change Log (Round 14)

- 2026-08-27 — Round 14 FULL, Phase 12 / Module L, first QA round: 15/31 Verified, 4 Partial, 12 Failed; Phase remains open and Security gate remains open. Findings are parked pending the project owner's decision; no downstream role was dispatched.

## Verification Summary (Round 15)

**Round 15 — Mode: FULL — Phase 12 / Module L, second QA round, all 31 tasks re-inspected from scratch (not just the 8 backend findings that were fixed) against R9, DM-2/DM-11/DM-15/DM-16/DM-18, MG-L1, and LT-1..LT-24.**

**Result: 28/31 ✅ Verified, 3/31 ⚠️ Partial, 0/31 ❌ Failed. Phase 12 remains open and is not deploy-eligible.** Thirteen additional checkboxes were changed to `[x]` in `plan.md` (28/31 total `[x]`); tasks 528/530/531 remain `[ ]`. The standing `🔒 Security gate` remains open and was not run in this QA role.

Verified by direct code read (not by the engineer's report) that P12-01 through P12-07, P12-09, and both frontend findings (P12-06/P12-10) are genuinely closed:

- **P12-01/P12-02 (archive race / restore atomicity):** `ILessonConfigRepository.TryArchive` is a single `ExecuteUpdate(... WHERE ... AND !IsDelete)` inside one transaction with the `BackgroundJob` insert and link revoke — traced the interleaving case: PostgreSQL row-level locking means a second concurrent caller's `UPDATE` blocks until the first commits, then reads `IsDelete=true` and matches zero rows, so only one caller can ever win and the loser gets `NotFound` from `ArchiveAsync`, not a duplicate job. `TryRestoreAndCancelPurge` wraps the conditional lesson restore and the job-cancel `UPDATE` in one `BeginTransaction()`/`Commit()`, rolling back if either statement affects zero rows.
- **P12-03 (trash GET missing `cs` guard):** `LessonConfigService.GetTrash()` now calls `EnsureCanArchiveOrRestore()` before querying, confirmed by a new `GetTrash_Cs_IsForbidden` test.
- **P12-04 (revoked-link resume/TTS/progress):** read `ILearningSessionService.cs` and `ITrainingLinkService.cs` end to end — both files are structurally clean (no trace of the prior brace-merge bug), and `UpdateProgress`/`GetResumeState`/`ITtsController`/`IVoiceQuestionService.ResolveContextAsync` all route through `GetEntityByTokenForContentAccess`/`GetInProgressEntityByLearnerKey`, never the raw `GetEntityByToken`. `End()`/`GetOwnSummary()` deliberately keep the looser `GetEntityByLearnerKey` gate (recap of an already-existing session, not new content) — consistent with the code's own documented split, not a regression.
- **P12-05 (permanent-failed worker):** `HandleFailure`'s `if (job.JobType == BackgroundJobType.LessonPurge)` branch is scoped to that one job type only; a new contrast test (`ProcessAsync_NonLessonPurgeJobType_StillBecomesPermanentlyFailedAtMaxAttempts`) proves every other job type is untouched.
- **P12-06 (empty-202 crash):** `api-client.ts`'s `request()` now reads `response.text()` and only `JSON.parse`s a non-empty body; a dedicated test feeds a literal `new Response(null, { status: 202 })` through `requestLessonPermanentDelete` and asserts it resolves.
- **P12-07 (LT-24 preflight):** `backend/scripts/preflight-lt24-lesson-trash.sql` exists and its dev-DB result (6 archived rows, 0 with `PurgeJobId IS NULL`) is recorded in `design.md`'s LT-24 row with an explicit "re-run on staging/production before applying MG-L1" caveat.
- **P12-09 (docs):** `ER_DIAGRAM_AND_WORKFLOW.md` now has an LT-2 role matrix table and an LT-14 retry-semantics section; `API_CONTRACT.md` documents the trash endpoints/role/learnerKey requirements to match.
- **P12-10 (frontend tests):** `LessonTrashList.test.tsx` now asserts absence of every forbidden control (edit/upload/delete-file/move/create-link/bulk) in the trash view; `LessonPermanentDeleteDialog.test.tsx` proves the typed-title submission and empty-202 success path.
- A separate `FakeServiceProvider` bug (always resolving `ICompanyContext` to one fixed company regardless of the constructor argument) was fixed alongside P12-01, confirmed by reading the fake — it now takes and uses a `companyId` parameter, and `LessonTrashServiceTests.BuildService` passes it correctly for the new two-company tests.

**P12-08 (test coverage) is substantially improved but not fully closed — this is a failed re-check, round 1 of 2 before the escalation ceiling.** New, real (non-smoke) test files were added and read in full: `LessonPurgeWorkerTests.cs` (60-day schedule, stale/restored/generation-mismatch no-op, active-session one-hour deferral without spending an attempt, `TryClaimPurge` race, restart-requeue continuing an already-claimed purge, the P12-05 daily-retry contrast, and exclusion-created-before-source-delete-and-survives-purge), `RevokedLinkPolicyTests.cs` (every LT-5/LT-6 branch: active link, no key, wrong key, ended session, matching in-progress session, across `GetEntityByTokenForContentAccess`/`Join`/`Restart`/`GetResumeState`/`UpdateProgress`), and `CompanyIsolationTests.cs`'s new real-`DbContext` `GetTrash`/`GetIncludingDeleted` cross-company tests plus a documented, honest explanation of why the raw-SQL conditional-update methods can't run against EF InMemory (the same limitation already accepted for `ClaimNext`/`RequeueOrphanedRunning`). Three specific gaps remain, none touched by this round's fixes:

1. No test at any level (fake-repository or real-DB) exercises `TryClaimPurge`/`TryRestoreAndCancelPurge`/`CancelPendingLessonPurge`/`AccelerateLessonPurge` with two different companies — only `GetTrash`/`GetIncludingDeleted` got the real-DB two-company treatment, and only `RestoreAsync`/`RequestPermanentDeleteAsync`/`GetTrash` got it at the service level.
2. Task 530's shared-PDF-guard-during-purge scenario (two lessons referencing the same `PdfDocumentResourceId`, purging one must preserve the resource/chunks/vectors) and its idempotent-missing-external-delete scenario have no test — confirmed by grepping the whole test project for "preserv"/"shared"/"idempotent" and finding nothing.
3. Task 531's "trash hides a lesson's questions, restore returns them to the queue" (LT-8/QQ-1) has no test — `SessionQuestionServiceTests.cs` has no trash/`IsDelete` reference at all — and finalization's retention side is proven only for `SessionQuestion` survival; no assertion checks `TrainingLink`/`LearningSession`/`BackgroundJob` history survive or that `LessonExcludedSlide` rows are actually removed by purge (the removal code itself was read and is correct; only the test is missing).

The plan.md task wording for the repository-methods task (line 514) names a `GetPurgeDependencies(companyId, lessonId)` method that does not exist as a literal named method — checked `design.md`'s actual DM-16 amendment for `ILessonConfigRepository`, which does **not** list this method at all (it is a `project-manager` gloss in `plan.md`, not part of the confirmed contract). The functional requirement it stands in for (LT-15's fresh-from-DB, company-scoped, no-client-supplied-ids dependency snapshot) is met by `IBackgroundJobProcessor.PurgeLessonAsync`, which queries each already-existing scoped repository directly. Not treated as a gap since `design.md` is the authoritative contract and does not require this shape.

Security-sensitive re-check specifically for P12-03/P12-04 confirms both gates are enforced server-side with no client-controlled bypass: `EnsureCanArchiveOrRestore()` runs inside `LessonConfigService.GetTrash()` itself (not a controller-level check a client could route around), and `GetEntityByTokenForContentAccess`/`GetInProgressEntityByLearnerKey` are the only paths every recipient-side controller (`LearningSessionController`, `TrainingLinkController`, `TtsController`, `LessonController.GetPdfPage`, `IVoiceQuestionService`) uses to resolve a link — there is no remaining call site using the raw `GetEntityByToken`/`GetByToken` for content access.

Automated checks:

- Frontend `npm run typecheck` ✅ clean.
- Frontend `npm run lint` ✅ clean.
- Frontend `npm run test` ✅ 82/82, 12 files (+3 tests from the new dialog/api-client/trash-list assertions).
- Frontend `npm run build` ✅ Next.js 15.5.22, 25 routes generated.
- Killed a stale locked `SupportRoom.Api.exe` process (PID) before building.
- Backend `dotnet build SupportRoom.slnx -c Release` ✅ 0 warnings, 0 errors.
- Backend `dotnet test SupportRoom.slnx -c Release --no-build --filter "Category!=Integration"` ✅ 342/342 (+22 from the new Module L tests).
- Backend FULL `dotnet test SupportRoom.slnx -c Release --no-build` ❌ 9 failed (down from 11 in Round 14) — all 9 are external integration/configuration failures (Google service-account credentials, Gemini API key, Pinecone namespace/network), none touching Module L.
- EF `dotnet ef migrations has-pending-model-changes --no-build --configuration Release ...` ✅ no pending model changes.

## Verified File Manifest — Phase 12 (Round 15)

Files inspected in this Round 15 FULL round (superseding the Round 14 manifest above). Superseded in turn by Round 16's manifest — see `review.md`.

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `_docs/module/knowledge-base/design.md` | 650057 | 2547 | Round 15 |
| `_docs/module/knowledge-base/plan.md` | 217568+ | 1049 | Round 15 |
| `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` | 18096 | 436 | Round 15 |
| `frontend/docs/API_CONTRACT.md` | 24581 | 279 | Round 15 |
| `backend/scripts/preflight-lt24-lesson-trash.sql` | 892 | 22 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/LessonController.cs` | 9615 | 193 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/LearningSessionController.cs` | 5520 | 113 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/TrainingLinkController.cs` | 4380 | 96 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/TtsController.cs` | 1592 | 38 | Round 15 |
| `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs` | 50528 | 964 | Round 15 |
| `backend/src/SupportRoom.Application/Services/IBackgroundJobProcessor.cs` | 43178 | 827 | Round 15 |
| `backend/src/SupportRoom.Application/Services/ITrainingLinkService.cs` | 11532 | 234 | Round 15 |
| `backend/src/SupportRoom.Application/Services/ILearningSessionService.cs` | 19964 | 405 | Round 15 |
| `backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs` | 11855 | 224 | Round 15 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonConfigRepository.cs` | 9116 | 190 | Round 15 |
| `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` | 38694 | 746 | Round 15 |
| `backend/tests/SupportRoom.Application.Tests/LessonTrashServiceTests.cs` | 17018 | 418 | Round 15 |
| `backend/tests/SupportRoom.Application.Tests/LessonPurgeWorkerTests.cs` | 17695 | 396 | Round 15 |
| `backend/tests/SupportRoom.Application.Tests/RevokedLinkPolicyTests.cs` | 11706 | 317 | Round 15 |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 20152 | 454 | Round 15 |
| `frontend/src/lib/api-client.ts` | 34488 | 794 | Round 15 |
| `frontend/src/lib/api-client.test.ts` | 2258 | 71 | Round 15 |
| `frontend/src/app/admin/lessons/page.tsx` | 14574 | 335 | Round 15 |
| `frontend/src/components/admin/LessonTrashList.tsx` | 7524 | 166 | Round 15 |
| `frontend/src/components/admin/LessonTrashList.test.tsx` | 4254 | 84 | Round 15 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.tsx` | 4774 | 122 | Round 15 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.test.tsx` | 2114 | 58 | Round 15 |
| `frontend/src/types/domain.ts` | 28180 | 691 | Round 15 |

Unchanged since Round 14 (stats confirmed still match): `requirement.md`, all Domain entities/enums, `ApplicationDbContext.cs`, `UnitOfWork.cs`/`IUnitOfWork.cs`, the `AddLessonTrashLifecycle` migration + snapshot, `IBackgroundJobRepository.cs`, `ISessionQuestionReviewExclusionRepository.cs`, `ITrainingLinkRepository.cs`, `ILearningSessionRepository.cs`, `IDocumentResourceRepository.cs`, `IDocumentChunkRepository.cs`, `ILessonSlideNarrationRepository.cs`, `ILessonExcludedSlideRepository.cs`, `IKnowledgeQnA*Repository.cs`, `ILessonConfigService.cs`'s DM-2/LT-7/LT-9/LT-10/LT-15..20 sections (only the ArchiveAsync/RestoreAsync/GetTrash bodies changed, re-read above), `IKnowledgeQnAService.cs`, `LessonTrashItemViewModel.cs`, `PermanentDeleteLessonDto.cs`, `BackgroundJobProcessingTests.cs`, `lesson-trash-display.ts`/`.test.ts`.

## Per-Task Results — Phase 12 (Round 15)

- ✅ [backend] Entity `LessonConfig` matches DM-2. (unchanged since Round 14)
- ✅ [backend] `SessionQuestionReviewExclusion` matches DM-18. (unchanged)
- ✅ [backend] Module L constants match DM-11, including `red_today` display enum support. (unchanged)
- ✅ [backend] EF configuration/query filters/indexes match DM-15. (unchanged)
- ✅ [backend] MG-L1 migration — schema/Down correct, EF model clean, and LT-24 preflight now has a real script + recorded dev-DB result in `design.md` (P12-07 fixed).
- ✅ [backend] Lesson trash/purge repository methods — `TryArchive`/`TryClaimPurge`/`TryRestore`/`TryRestoreAndCancelPurge`/`GetTrash`/`GetIncludingDeleted` all present, all re-apply `CompanyId` in the same predicate as their `IgnoreQueryFilters()` call. `GetPurgeDependencies` as a literal method name is a `plan.md` gloss not required by `design.md`'s actual DM-16 text (see Verification Summary).
- ✅ [backend] Review-exclusion repository and UnitOfWork registration. (unchanged)
- ✅ [backend] Archive service — `TryArchive` is a single conditional `UPDATE` inside one transaction with the job insert and link revoke; traced the concurrent-caller interleaving and confirmed only one caller can win (P12-01 fixed).
- ✅ [backend] Restore service — `TryRestoreAndCancelPurge` wraps the conditional lesson restore and job-cancel `UPDATE` in one transaction, rolling back on either failing (P12-02 fixed).
- ✅ [backend] Lifecycle endpoints/DTO/ViewModel — trash GET now calls `EnsureCanArchiveOrRestore()` inside the service (P12-03 fixed), confirmed with a dedicated forbidden-role test.
- ✅ [backend] Trash projection and all four urgency bands, including `red_today` ≤24h. (unchanged)
- ✅ [backend] Owner-only manual permanent-delete server logic, trim + ordinal-exact, existing-job acceleration, 202. (unchanged)
- ✅ [backend] Public learner authorization — resume/progress/TTS/question/content/PDF all route through `GetEntityByTokenForContentAccess`/`GetInProgressEntityByLearnerKey`; read `ILearningSessionService.cs`/`ITrainingLinkService.cs` end to end, no logic left merged or dropped from the prior brace bug (P12-04 fixed).
- ✅ [backend] Active Q&A queue derives trash state and applies permanent exclusions before source checks. (unchanged; the LT-8/QQ-1 trash-hides/restore-reveals behavior itself still has no dedicated test — see P12-08 below)
- ✅ [backend] Durable worker/retry — `lesson_purge`'s post-third-attempt branch stays `pending`/retries every 24h, scoped to that job type only; a contrast test proves other job types still terminate at `failed` (P12-05 fixed).
- ✅ [backend] Purge dependency snapshot and permanent question exclusions; no `IKnowledgeQnAService.DeleteAsync` loop. (unchanged)
- ✅ [backend] External deletion order, stored namespace keys, shared-PDF guard, and DB finalization/retention order. (unchanged; shared-PDF-guard behavior itself still has no dedicated test — see P12-08 below)
- ✅ [backend] Hard-deleted lesson history fallback "บทเรียนที่ถูกลบ". (unchanged)
- ✅ [backend] Role/archive/restore service tests cover the stated normal state matrix, now including `GetTrash_Cs_IsForbidden`.
- ⚠️ [backend] LT-23 tenant tests — `GetTrash`/`GetIncludingDeleted` now covered at the real-`DbContext` level and `RestoreAsync`/`RequestPermanentDeleteAsync`/`GetTrash` at the service level, but `TryClaimPurge`/`TryRestoreAndCancelPurge`/`CancelPendingLessonPurge`/`AccelerateLessonPurge` still have no two-company test at any level (P12-08, narrowed).
- ✅ [backend] LT-5/LT-6 public policy tests — `RevokedLinkPolicyTests.cs` now exhaustively covers `GetEntityByTokenForContentAccess`, `Join`, `Restart`, `GetResumeState`, and `UpdateProgress` across every case (no key, wrong key, ended session, matching in-progress session).
- ⚠️ [backend] Worker timing/reliability tests — 60-day schedule, active-session deferral, claim race, stale-generation no-op, and the P12-05 daily-retry contrast are all now real tests in `LessonPurgeWorkerTests.cs`, but the shared-PDF-guard-preservation case and idempotent-missing-external-delete case from task 530 still have no test (P12-08, narrowed).
- ⚠️ [backend] Queue/purge data tests — exclusion-created-before-source-delete and exclusion-survives-purge are now tested, but "trash hides questions then restore returns them" (LT-8/QQ-1) has no test, and finalization retention is proven only for `SessionQuestion` (not `TrainingLink`/`LearningSession`/`BackgroundJob`/removed `LessonExcludedSlide`) (P12-08, narrowed).
- ✅ [backend] API/workflow documentation now states the full LT-2 role matrix and LT-14's indefinite 24-hour retry semantics in both `ER_DIAGRAM_AND_WORKFLOW.md` and `API_CONTRACT.md` (P12-09 fixed).
- ✅ [frontend] Domain/api-client lockstep — `request()` now handles an empty 202 body correctly; types match `LessonTrashItemViewModel`/`LessonConfigViewModel` field-for-field (P12-06 fixed).
- ✅ [frontend] Active/trash tabs and read-only trash surface. (unchanged)
- ✅ [frontend] Countdown/purging UI and no notification UI. (unchanged)
- ✅ [frontend] Archive/restore controls and list refresh behavior. (unchanged)
- ✅ [frontend] Owner permanent-delete dialog — now handles the empty 202 via the fixed `request()`, with a passing regression test (P12-06 fixed).
- ✅ [frontend] Public learner callers/UI — learnerKey was already wired on every content call; the remaining gap (token-only resume receiving trashed metadata) was purely a backend fix, now closed via `GetResumeState`'s content-access gate (P12-04 fixed).
- ✅ [frontend] Tests now assert absence of every forbidden control in the trash view and exercise the typed permanent-delete submission/202 path (P12-10 fixed).

## Design/requirement contract checks — Phase 12 (Round 15)

- DM-2: `LessonConfig` uses existing soft-delete fields plus only `PurgeJobId`/`PurgeStartedAt`; no duplicate lifecycle enum/state column added. (re-confirmed, unchanged)
- DM-18: exclusion entity has standard audit fields, `SessionQuestionId`, `LessonId`, and `Reason`; no physical FK/retention field. (re-confirmed, unchanged)
- DM-15/MG-L1: entity, migration, snapshot, and generated model match; migration is additive and contains no backfill/cleanup. `dotnet ef migrations has-pending-model-changes` confirms no drift. LT-24 preflight is now evidenced (P12-07).
- DM-16/LT-23: re-read the full `ILessonConfigRepository` and confirmed every `IgnoreQueryFilters()` method (`GetTrash`, `GetIncludingDeleted`, and the raw-SQL `TryArchive`/`TryClaimPurge`/`TryRestore`/`TryRestoreAndCancelPurge`) re-applies `CompanyId` in the same predicate/`WHERE` clause. `GetByToken` remains the documented public-token exception. No Critical tenant leak found. `GetPurgeDependencies` is not part of DM-16's actual text (see Verification Summary) and is not held against the phase.
- LT-1..LT-4: state machine and both transactions (archive, restore+cancel) are now genuinely atomic and idempotent — traced the concurrent-request case for both.
- LT-5/LT-6: the strict `(token, learnerKey, IN_PROGRESS)` gate is now applied consistently across resume/progress/question/TTS/content/PDF; no remaining call site uses the raw token-only lookup for content access.
- LT-11..LT-14: conditional claim, one-hour active-session deferral, and now the indefinite 24-hour retry are all correct and scoped correctly to `lesson_purge` only.
- LT-15..LT-20: dependency IDs come from DB, company scope is retained, external deletion ordering/shared PDF guard are correct in the code (though the shared-PDF-guard path itself is untested — see P12-08), and exclusions are inserted before Q&A/source hard delete.
- LT-9: `neutral`, `yellow`, `red`, and `red_today` thresholds match the amended contract on both backend and frontend. (re-confirmed, unchanged)
- LT-24: preflight script and its dev-DB result are now recorded directly in `design.md`'s own contract row, with an explicit caveat that staging/production must re-run it before MG-L1 is applied there.

## Issues Found — Phase 12 (Round 15)

1. **P12-08 Important (narrowed) → backend-engineer, failed re-check round 1 of 2:** three specific, small remaining test gaps — (a) two-company tests for `TryClaimPurge`/`TryRestoreAndCancelPurge`/`CancelPendingLessonPurge`/`AccelerateLessonPurge` (fake-repository level is sufficient, matching the pattern already used for `TryArchive`/`GetTrash`); (b) a shared-PDF-guard-during-purge test (two lessons referencing one `PdfDocumentResourceId`, purge one, assert the resource/chunks/vectors survive) and an idempotent-missing-external-delete test in `LessonPurgeWorkerTests.cs`; (c) a test that archiving hides a lesson's questions from the review queue and restoring returns them (LT-8/QQ-1), plus explicit assertions in the existing purge-finalization test that `TrainingLink`/`LearningSession`/`BackgroundJob` rows survive and `LessonExcludedSlide` rows are removed. This is implementation/test work against an already-clear contract, not a design or business question.

All other Round 14 findings (P12-01 through P12-07, P12-09, P12-06, P12-10) are verified closed by direct code and test inspection this round — see Per-Task Results and Verification Summary above for what was checked and how.

## Review Outcome — Phase 12 (Round 15)

**Sent back for the remaining P12-08 items; awaiting the project owner's decision on how to proceed.** Phase 12 is **28/31 ✅, 3/31 ⚠️, 0/31 ❌** on this second FULL round — a large improvement from Round 14's 15/31 ✅. It still cannot close, still cannot unblock Phase 13's last two tasks (`handleArchiveLesson` confirm-dialog swap and the final CD-1 sweep — both remain correctly blocked per the Phase 13 header rule until Phase 12 closes at 31/31), and still cannot reach `devops`. The Security gate remains independently open and was not run in this QA role. P12-08 has now had one failed re-check (this round); per the escalation rule, one more failed re-check after a further fix would require escalating to the user instead of a third routine round.

## Change Log (Round 15)

- 2026-08-27 — Round 15 FULL, Phase 12 / Module L, second QA round: 28/31 Verified, 3 Partial, 0 Failed. Verified P12-01..P12-07/P12-09/P12-06/P12-10 closed by direct code/test inspection; P12-08 narrowed to three specific remaining test gaps (failed re-check round 1 of 2). Phase remains open, not deploy-eligible, Phase 13's last two tasks remain blocked, and the Security gate remains open.

## Verification Summary (Round 16)

**Round 16 — Mode: TARGETED — Phase 12 / Module L, re-check of P12-08's three narrowed gaps from Round 15 (a FULL round with a recorded manifest), plus their blast radius, the shared-code watchlist, and a whole-project build/test pass. Does NOT re-inspect the other 28 already-closed Phase 12 tasks from scratch — that was done in full in Round 15 above and is not repeated here.**

**Result: all three P12-08 sub-gaps are genuinely closed. Phase 12 is now 31/31 ✅ Verified, 0 Partial, 0 Failed.** Tasks 528, 530, and 531 changed from `[ ]` to `[x]` in `plan.md`. Phase 12 QA has no more open functional items. The `🔒 Security gate` remains independently open (never run) — this TARGETED round did not audit security depth, only the functional test-coverage gap.

Verified each of the three sub-gaps by reading the actual new test code and the production code it exercises, not by trusting the engineer's summary:

1. **Two-company tests for the four raw-SQL methods, at fake-repository level — confirmed genuinely sufficient, not just "the fakes were told the right answer."** Read `CompanyIsolationTests.cs` lines 417-426: the documented justification (EF Core InMemory throws `InvalidOperationException` on `ExecuteSqlRaw`/relational-specific calls, the same accepted gap already excluding `ClaimNext`/`RequeueOrphanedRunning` from real-DB coverage) is real, not invented. Then compared the four new tests in `LessonTrashServiceTests.cs` (`TryClaimPurge_CompanyACannotClaimCompanyBsPendingPurgeJob`, `TryRestoreAndCancelPurge_CompanyACannotRestoreOrCancelCompanyBsJob`, `CancelPendingLessonPurge_CompanyACannotCancelCompanyBsJob`, `AccelerateLessonPurge_CompanyACannotAccelerateCompanyBsJob`) against the real production `WHERE`/`ExecuteSqlRaw` predicates in `ILessonConfigRepository.cs` and `IBackgroundJobRepository.cs` field by field: every fake predicate (`Id`/`CompanyId`/`IsDelete`/`PurgeJobId`/`PurgeStartedAt IS NULL` for the lesson methods; `Id`/`CompanyId`/`JobType`/`TargetId`/`Status=Pending` for the job methods) matches the real SQL's `WHERE` clause exactly, column for column. Each test also proves the positive case (the owning company can still act) alongside the negative one, so the guard is shown to be scoped by `CompanyId` specifically, not just always failing. This is as strong a guarantee as a fake-level test can give without a real Postgres harness, and it matches the exact pattern already accepted for `TryArchive`/`GetTrash` in Round 15.
2. **Shared-PDF-guard and idempotent-missing-external-delete tests in `LessonPurgeWorkerTests.cs` — confirmed real, not smoke tests.** `ProcessAsync_DocumentStillReferencedByAnotherLesson_PreservesResourceBytesChunksAndVectors` seeds two lessons (`lesson-a` active, `lesson-b` trashed) both pointing at the same `PdfDocumentResourceId = "doc-shared"`, plus a `DocumentChunk` and its vector; purges `lesson-b` only, and asserts `lesson-a` survives and the shared `DocumentResource`/`DocumentChunk`/storage key/vector all survive. Read `IBackgroundJobProcessor.cs`'s purge-finalization code (the `candidateDocumentIds`/shared-PDF-guard block around line 649-670) and confirmed it is the exact code path the test exercises — the guard checks for other lessons still referencing the same document before adding it to the delete set. `ProcessAsync_DocumentBytesAlreadyMissingFromStorage_StillSucceeds_DoesNotRetry` uses the **real** `LocalDocumentStorageProvider` (not `RecordingDocumentStorageProvider`), against a storage key that was deliberately never uploaded (`documents/missing-{guid}/never-uploaded.pdf`), and asserts the job succeeds with `AttemptCount` unchanged at 0 (not a retry) — proving `DeleteAsync` no-ops on a missing file rather than throwing, using production code, not a mock's assumption.
3. **Trash-hides/restore-reveals queue test and expanded retention assertions — confirmed correct wiring and real code path.** `KnowledgeQnAServiceTests.cs`'s new `GetQueue_TrashedLessonHidesItsQuestions_RestoreBringsThemBack` correctly wires the trashed `LessonConfig.Id = "lesson-1"` to the existing `SeedLink`/`SeedQuestion` helpers (which always hardcode `TrainingLink.LessonId = "lesson-1"`), so the test's trash/restore toggle genuinely reaches the same lesson the question's session→link chain points at — not a coincidental no-op. Read `KnowledgeQnAService.GetQueue()`'s actual filter and confirmed it calls `_lessonConfigRepository.GetTrash(CurrentCompanyId)` to build the excluded-lesson set, the same method the fake correctly implements, so the test exercises real service logic. For finalization retention, `LessonPurgeWorkerTests.cs`'s existing `ProcessAsync_SuccessfulPurge_CreatesReviewExclusionsBeforeDeletingSourcesAndSurvivesAfterLessonIsGone` test now has three added assertions (`TrainingLink`, `LearningSession`, `BackgroundJob` all still present after purge) and a new dedicated test `ProcessAsync_SuccessfulPurge_HardDeletesLessonExcludedSlideRows` proves the row is actually removed from the repository (not just soft-deleted) — read `IBackgroundJobProcessor.cs`'s finalization block and confirmed it calls `excludedSlideRepository.Delete(excludedSlide)` (a real removal call, matching the fake's `Items.Remove`), matching the assertion.

No new test files were created — Glob/`ls` of `backend/tests/SupportRoom.Application.Tests/*.cs` and `Fakes/*.cs` shows the same 30 files as before; all fixes landed inside existing files (`LessonTrashServiceTests.cs`, `LessonPurgeWorkerTests.cs`, `KnowledgeQnAServiceTests.cs`). `ServiceTestFakes.cs`, `RevokedLinkPolicyTests.cs`, and `CompanyIsolationTests.cs` are byte-for-byte and line-for-line identical to the Round 15 manifest — untouched by this fix, confirmed by direct stat comparison, not by trusting a self-reported change list.

Shared-code watchlist (auth/role guard, `ApplicationDbContext`, `api-client.ts`, shared admin components) — all files are byte-for-byte identical to the Round 15 manifest; this fix touched only test files, so there is no blast radius into shared code this round.

Automated checks:

- Frontend `npm run typecheck` ✅ clean.
- Frontend `npm run lint` ✅ clean.
- Frontend production/test files unchanged since Round 15 (confirmed by byte/line stat comparison against the Round 15 manifest) — `npm run test`/`npm run build` not re-run since nothing in scope changed on that side; Round 15 already recorded them clean (82/82 tests, successful build).
- Killed/checked for a stale locked `SupportRoom.Api.exe` process before building — none found.
- Backend `dotnet build SupportRoom.slnx -c Release` ✅ 0 warnings, 0 errors.
- Backend `dotnet test SupportRoom.slnx -c Release --filter "Category!=Integration"` ✅ 350/350 (41 Providers.Tests + 10 Api.IntegrationTests + 299 Application.Tests) — up from Round 15's 342, consistent with the 8 new test methods added across the three sub-gaps (4 tenant-isolation tests, 2 worker tests, 1 queue test, 1 hard-delete test).

**What this round does not cover:** the other 28 Phase 12 tasks already verified ✅ in Round 15 were not re-inspected from scratch (no code they depend on changed — confirmed via the manifest/glob comparison above). This round also did not re-run the FULL backend suite including `Category=Integration` tests (external provider failures unrelated to Module L, already characterized in Round 15) or the frontend build, since no frontend file changed. It did not run a security audit — the Security gate remains open and unrun.

## Verified File Manifest — Phase 12 (Round 16)

Files inspected across Round 15 (FULL) and Round 16 (TARGETED). Round 16 only touched three test files (marked below); everything else is carried forward unchanged from Round 15 and was confirmed still matching by direct stat comparison, not assumed.

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `_docs/module/knowledge-base/design.md` | 650057 | 2547 | Round 15 |
| `_docs/module/knowledge-base/plan.md` | 217583 | 1049 | Round 16 (checkboxes only; text unchanged) |
| `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` | 18096 | 436 | Round 15 |
| `frontend/docs/API_CONTRACT.md` | 24581 | 279 | Round 15 |
| `backend/scripts/preflight-lt24-lesson-trash.sql` | 892 | 22 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/LessonController.cs` | 9615 | 193 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/LearningSessionController.cs` | 5520 | 113 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/TrainingLinkController.cs` | 4380 | 96 | Round 15 |
| `backend/src/SupportRoom.Api/Controllers/TtsController.cs` | 1592 | 38 | Round 15 |
| `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs` | 50528 | 964 | Round 15 |
| `backend/src/SupportRoom.Application/Services/IBackgroundJobProcessor.cs` | 43178 | 827 | Round 15 |
| `backend/src/SupportRoom.Application/Services/ITrainingLinkService.cs` | 11532 | 234 | Round 15 |
| `backend/src/SupportRoom.Application/Services/ILearningSessionService.cs` | 19964 | 405 | Round 15 |
| `backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs` | 11855 | 224 | Round 15 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonConfigRepository.cs` | 9116 | 190 | Round 15 |
| `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` | 38694 | 746 | Round 15 (confirmed unchanged in Round 16) |
| `backend/tests/SupportRoom.Application.Tests/LessonTrashServiceTests.cs` | 21879 | 527 | Round 16 (P12-08 gap 1: 4 two-company tests added) |
| `backend/tests/SupportRoom.Application.Tests/LessonPurgeWorkerTests.cs` | 26188 | 578 | Round 16 (P12-08 gap 2: shared-PDF-guard + idempotent-missing-delete tests added; gap 3's retention assertions also landed here) |
| `backend/tests/SupportRoom.Application.Tests/RevokedLinkPolicyTests.cs` | 11706 | 317 | Round 15 (confirmed unchanged in Round 16) |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 20152 | 454 | Round 15 (confirmed unchanged in Round 16) |
| `backend/tests/SupportRoom.Application.Tests/KnowledgeQnAServiceTests.cs` | 15368 | 369 | Round 16 (P12-08 gap 3: `GetQueue_TrashedLessonHidesItsQuestions_RestoreBringsThemBack` added; not previously in this phase's manifest — belongs to Module H's Phase 8 test file, added to the watchlist here since Round 16 touched it) |
| `frontend/src/lib/api-client.ts` | 34488 | 794 | Round 15 |
| `frontend/src/lib/api-client.test.ts` | 2258 | 71 | Round 15 |
| `frontend/src/app/admin/lessons/page.tsx` | 14574 | 335 | Round 15 |
| `frontend/src/components/admin/LessonTrashList.tsx` | 7524 | 166 | Round 15 |
| `frontend/src/components/admin/LessonTrashList.test.tsx` | 4254 | 84 | Round 15 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.tsx` | 4774 | 122 | Round 15 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.test.tsx` | 2114 | 58 | Round 15 |
| `frontend/src/types/domain.ts` | 28180 | 691 | Round 15 |

Unchanged since Round 14 (stats confirmed still match, listed in the Round 14 manifest above, not re-listed here): `requirement.md`, all Domain entities/enums, `ApplicationDbContext.cs`, `UnitOfWork.cs`/`IUnitOfWork.cs`, the `AddLessonTrashLifecycle` migration + snapshot, `IBackgroundJobRepository.cs`, `ISessionQuestionReviewExclusionRepository.cs`, `ITrainingLinkRepository.cs`, `ILearningSessionRepository.cs`, `IDocumentResourceRepository.cs`, `IDocumentChunkRepository.cs`, `ILessonSlideNarrationRepository.cs`, `ILessonExcludedSlideRepository.cs`, `IKnowledgeQnA*Repository.cs`, `ILessonConfigService.cs`'s DM-2/LT-7/LT-9/LT-10/LT-15..20 sections (only the ArchiveAsync/RestoreAsync/GetTrash bodies changed, re-read above), `IKnowledgeQnAService.cs`, `LessonTrashItemViewModel.cs`, `PermanentDeleteLessonDto.cs`, `BackgroundJobProcessingTests.cs`, `lesson-trash-display.ts`/`.test.ts`.

## Per-Task Results — Phase 12 (Round 16, TARGETED — only the three previously-Partial rows changed; the other 28 rows are carried forward verbatim from Round 15, not re-inspected this round)

- ✅ [backend] Entity `LessonConfig` matches DM-2. (unchanged since Round 14)
- ✅ [backend] `SessionQuestionReviewExclusion` matches DM-18. (unchanged)
- ✅ [backend] Module L constants match DM-11, including `red_today` display enum support. (unchanged)
- ✅ [backend] EF configuration/query filters/indexes match DM-15. (unchanged)
- ✅ [backend] MG-L1 migration — schema/Down correct, EF model clean, and LT-24 preflight now has a real script + recorded dev-DB result in `design.md` (P12-07 fixed).
- ✅ [backend] Lesson trash/purge repository methods — `TryArchive`/`TryClaimPurge`/`TryRestore`/`TryRestoreAndCancelPurge`/`GetTrash`/`GetIncludingDeleted` all present, all re-apply `CompanyId` in the same predicate as their `IgnoreQueryFilters()` call. `GetPurgeDependencies` as a literal method name is a `plan.md` gloss not required by `design.md`'s actual DM-16 text (see Verification Summary).
- ✅ [backend] Review-exclusion repository and UnitOfWork registration. (unchanged)
- ✅ [backend] Archive service — `TryArchive` is a single conditional `UPDATE` inside one transaction with the job insert and link revoke; traced the concurrent-caller interleaving and confirmed only one caller can win (P12-01 fixed).
- ✅ [backend] Restore service — `TryRestoreAndCancelPurge` wraps the conditional lesson restore and job-cancel `UPDATE` in one transaction, rolling back on either failing (P12-02 fixed).
- ✅ [backend] Lifecycle endpoints/DTO/ViewModel — trash GET now calls `EnsureCanArchiveOrRestore()` inside the service (P12-03 fixed), confirmed with a dedicated forbidden-role test.
- ✅ [backend] Trash projection and all four urgency bands, including `red_today` ≤24h. (unchanged)
- ✅ [backend] Owner-only manual permanent-delete server logic, trim + ordinal-exact, existing-job acceleration, 202. (unchanged)
- ✅ [backend] Public learner authorization — resume/progress/TTS/question/content/PDF all route through `GetEntityByTokenForContentAccess`/`GetInProgressEntityByLearnerKey`; read `ILearningSessionService.cs`/`ITrainingLinkService.cs` end to end, no logic left merged or dropped from the prior brace bug (P12-04 fixed).
- ✅ [backend] Active Q&A queue derives trash state and applies permanent exclusions before source checks. (unchanged; the LT-8/QQ-1 trash-hides/restore-reveals behavior now also has a dedicated passing test — P12-08 gap 3 fixed, Round 16)
- ✅ [backend] Durable worker/retry — `lesson_purge`'s post-third-attempt branch stays `pending`/retries every 24h, scoped to that job type only; a contrast test proves other job types still terminate at `failed` (P12-05 fixed).
- ✅ [backend] Purge dependency snapshot and permanent question exclusions; no `IKnowledgeQnAService.DeleteAsync` loop. (unchanged)
- ✅ [backend] External deletion order, stored namespace keys, shared-PDF guard, and DB finalization/retention order. (unchanged; the shared-PDF-guard behavior now also has a dedicated passing test — P12-08 gap 2 fixed, Round 16)
- ✅ [backend] Hard-deleted lesson history fallback "บทเรียนที่ถูกลบ". (unchanged)
- ✅ [backend] Role/archive/restore service tests cover the stated normal state matrix, now including `GetTrash_Cs_IsForbidden`.
- ✅ [backend] LT-23 tenant tests — `GetTrash`/`GetIncludingDeleted` covered at the real-`DbContext` level (Round 15) and now `TryClaimPurge`/`TryRestoreAndCancelPurge`/`CancelPendingLessonPurge`/`AccelerateLessonPurge` each have a two-company fake-repository test whose predicates were verified field-by-field against the real `ExecuteSqlRaw`/`ExecuteUpdate` `WHERE` clauses in `ILessonConfigRepository.cs`/`IBackgroundJobRepository.cs` (P12-08 gap 1 fixed, Round 16).
- ✅ [backend] LT-5/LT-6 public policy tests — `RevokedLinkPolicyTests.cs` now exhaustively covers `GetEntityByTokenForContentAccess`, `Join`, `Restart`, `GetResumeState`, and `UpdateProgress` across every case (no key, wrong key, ended session, matching in-progress session).
- ✅ [backend] Worker timing/reliability tests — 60-day schedule, active-session deferral, claim race, stale-generation no-op, and the P12-05 daily-retry contrast (Round 15), plus now the shared-PDF-guard-preservation test (two lessons sharing one `PdfDocumentResourceId`, purging one preserves the resource/chunk/storage key/vector — traced against the real `IBackgroundJobProcessor.cs` guard code) and the idempotent-missing-external-delete test using the real `LocalDocumentStorageProvider` against a never-uploaded key (P12-08 gap 2 fixed, Round 16).
- ✅ [backend] Queue/purge data tests — exclusion-created-before-source-delete and exclusion-survives-purge (Round 15), plus now `GetQueue_TrashedLessonHidesItsQuestions_RestoreBringsThemBack` in `KnowledgeQnAServiceTests.cs` (verified the trashed lesson id genuinely correlates to the seeded link/question via the shared test helpers, and that `GetQueue()`'s real filter calls `_lessonConfigRepository.GetTrash()`), and finalization retention now explicitly asserts `TrainingLink`/`LearningSession`/`BackgroundJob` survive plus a dedicated test proving `LessonExcludedSlide` rows are hard-deleted via `excludedSlideRepository.Delete()` (P12-08 gap 3 fixed, Round 16).
- ✅ [backend] API/workflow documentation now states the full LT-2 role matrix and LT-14's indefinite 24-hour retry semantics in both `ER_DIAGRAM_AND_WORKFLOW.md` and `API_CONTRACT.md` (P12-09 fixed).
- ✅ [frontend] Domain/api-client lockstep — `request()` now handles an empty 202 body correctly; types match `LessonTrashItemViewModel`/`LessonConfigViewModel` field-for-field (P12-06 fixed).
- ✅ [frontend] Active/trash tabs and read-only trash surface. (unchanged)
- ✅ [frontend] Countdown/purging UI and no notification UI. (unchanged)
- ✅ [frontend] Archive/restore controls and list refresh behavior. (unchanged)
- ✅ [frontend] Owner permanent-delete dialog — now handles the empty 202 via the fixed `request()`, with a passing regression test (P12-06 fixed).
- ✅ [frontend] Public learner callers/UI — learnerKey was already wired on every content call; the remaining gap (token-only resume receiving trashed metadata) was purely a backend fix, now closed via `GetResumeState`'s content-access gate (P12-04 fixed).
- ✅ [frontend] Tests now assert absence of every forbidden control in the trash view and exercise the typed permanent-delete submission/202 path (P12-10 fixed).

## Design/requirement contract checks — Phase 12 (Round 16)

Round 15's contract checks (DM-2, DM-18, DM-15/MG-L1, LT-1..LT-4, LT-5/LT-6, LT-11..LT-14, LT-15..LT-20, LT-9, LT-24) are unchanged and not re-run this round — no schema or contract-relevant code moved, only test files. One update from Round 16:

- DM-16/LT-23: now fully closed. In addition to Round 15's `GetTrash`/`GetIncludingDeleted`/`TryArchive`/`TryRestoreAndCancelPurge` re-read, the two remaining raw-SQL methods (`TryClaimPurge` in `ILessonConfigRepository`, `CancelPendingLessonPurge`/`AccelerateLessonPurge` in `IBackgroundJobRepository`) were compared column-by-column against their new fake-repository tests and found to match exactly. No Critical tenant leak found across any of the six `IgnoreQueryFilters()`/raw-SQL methods.
- LT-15..LT-20: the shared-PDF-guard code path (previously "correct in the code... though untested") is now directly exercised by a passing test; no longer a documentation-only claim.
- LT-8/QQ-1: now has a dedicated passing test proving trash hides a lesson's questions from the queue and restore reverses it, in addition to the already-existing code-level correctness.

## Issues Found — Phase 12 (Round 16)

None remaining. All Round 15 findings are closed:

- P12-01 through P12-07, P12-09, P12-06, P12-10 were verified closed in Round 15 (see above).
- **P12-08 is now closed (Round 16)** — all three narrowed sub-gaps (two-company tests for the four raw-SQL methods; shared-PDF-guard and idempotent-missing-delete tests; trash-hides/restore-reveals queue test plus expanded retention assertions) were verified genuinely fixed by direct code and test inspection, not by trusting the engineer's report. See Verification Summary above for exactly what was checked and how.

## Review Outcome — Phase 12 (Round 16)

**Phase 12 is fully verified: 31/31 ✅, 0 ⚠️, 0 ❌.** P12-08's three sub-gaps, which had one prior failed re-check (Round 15), are now genuinely closed on this second re-check — the escalation ceiling (two failed re-checks) was not reached. Phase 13's last two blocked tasks (`handleArchiveLesson` confirm-dialog swap and the final CD-1 sweep) are unblocked and may be dispatched to `frontend-engineer` now that Phase 12 has closed QA at 31/31.

**Phase 12 is NOT yet deploy-eligible, for two independent reasons, both of which must clear before `devops`:**
1. **Round mode.** Per this module's QA convention, deploy eligibility requires the phase's *most recent* round to be FULL. This round (Round 16) was TARGETED — legitimate for re-checking a named fix, but it does not itself confer deploy eligibility even though the phase is now all-✅. One more FULL round over all 31 tasks is needed before handoff to `devops`, to close on settled code rather than carry a TARGETED round as the final word.
2. **Security gate.** The phase carries `🔒 Security gate` in `plan.md`'s heading and no `security` audit has ever run on it (see Open Issues in `review.md`). This is independent of round mode and would block `devops` even after a FULL round.

Recommended next step: run one more FULL QA round on Phase 12 (cheap now, since nothing is expected to have changed beyond what Round 16 already touched), then run `security` before considering `devops`.

## Change Log (Round 16)

- 2026-08-27 — Round 16 TARGETED, Phase 12 / Module L, re-check of P12-08's three narrowed sub-gaps: all three genuinely closed by direct code/test inspection. Phase 12 now 31/31 Verified, 0 Partial, 0 Failed — no open functional issues remain. Phase 13's last two blocked tasks are unblocked. Phase 12 is still not deploy-eligible: this round was TARGETED (one more FULL round is required before `devops` per this module's convention), and the Security gate remains independently open and unrun.
