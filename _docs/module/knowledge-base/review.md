# Knowledge Base & Teaching Content Intake — Verification & Review

## Open Issues — all phases

| Issue | Phase | Routes to | Blocking | Rounds |
|---|---|---|---|---:|
| R-2 latency measurement (3-namespace query) still has no deployed traffic to measure. This is not a code defect. | Phase 2 (`review/phase-1-6.md`) | devops, after deployment | No | 0 |
| No security audit has run on this module. Phases 2, 3, 4, 6, 7, 8, 10, 11, and 12 carry the Security gate and cannot reach devops until security runs. | Multiple phases | security | Yes, for gated phases | 0 |
| DS-3 cross-company category-id rejection still lacks a dedicated two-company category test. | Phase 7 (`review/phase-7.md`) | backend-engineer | No | 0 |
| `RenderPdfPreviewPageAsync` does not wrap `PdfSlidesRenderer.RenderPagePng` in the Phase 10-specific 4xx conversion path. | Phase 10 (`review/phase-10.md`) | security | No | 0 |
| EX-8 hard-floor exclusion count can transiently overcount by one before an un-reconciled legacy duplicate is committed; safe-direction and self-healing. | Phase 11 (`review/phase-11.md`) | backend-engineer | No | 0 |
| CD-5 points 2/3 have no error-surfacing path on API failure; prior-round observation, not proven as a Phase 13 regression. | Phase 13 (`review/phase-13.md`) | frontend-engineer | No | 0 |
| Phase 12 is 31/31 functionally verified after Round 19 TARGETED closed tasks 528/531 on top of Round 18 FULL. Its `🔒 Security gate` remains open and blocks devops. | Phase 12 (`review/phase-12.md`) | security | Yes, for devops handoff | 0 |

## Verification Summary (current round)

**Round 20 — Mode: FULL — Phase 13 / Module M. All 14 tasks were re-inspected from scratch against NR-20..NR-24 and CD-1..CD-10, including the protected-dialog/shared-export watchlist and project-wide browser-dialog sweep.**

**Result: 14/14 ✅ Verified, 0 Partial, 0 Failed. Phase 13 is QA-closed on a FULL round and carries no Security gate.** No checkbox change was needed because Round 17 already checked all 14; this round establishes final FULL eligibility.

Evidence:

- NR-20: `commitModalOpen` is independent state; only `commit()` opens it and the permitted step-1 “กลับไปแก้ข้อมูลบทเรียน” action closes it. It is never derived from `lessonId`, `commitStarted`, or `committing`.
- NR-21: `flushNarrations()` and `handleRetryFailedNarrations()` contain no navigation. The only `router.push` in the component is `handleConfirmSuccess()` → `/admin/lessons`, called by the succeeded-state button.
- NR-22: the modal renders exactly four checklist rows from the existing lesson/document/link/narration state and preserves the existing `completedSteps`/`totalSteps` progress calculation.
- NR-23/NR-24: `onOpenChange={() => {}}`, `showCloseButton={false}`, no buttons while running, exactly one succeeded action, and the required per-step failed actions/links are all present; step-3 retry still rebuilds the save input with `excludedSlideObjectIds`.
- CD-1..CD-8: all five replacements use inline `AlertDialog`, visible payload state rather than promise/ref closures, exact confirmed copy, default cancel, and `variant="destructive"`. Existing permission, busy, refresh, and error paths remain where they were.
- CD-9/CD-10: `LessonPermanentDeleteDialog`, `lesson-editor-pdf-replace-dialog`, `pdf-content-phase-warning-dialog`, and `CategoryMovePreviewDialog` match the earlier manifest; the four shared `DocumentUploadList` exports and their three consumers remain intact. Project-wide search finds no live `window.confirm`/`window.alert`/`window.prompt` call—only comments describing replacements.

Automated checks:

- Frontend typecheck ✅.
- Frontend lint ✅.
- Frontend tests ✅ 82/82, 12 files.
- Frontend production build ✅ compiled and generated 24 routes.
- Backend blast-radius checks were already run immediately before this phase: Release build 0/0, non-integration 351/351, EF model clean.

### Verified file manifest — Round 20 FULL

| File | Bytes | Lines |
|---|---:|---:|
| `frontend/src/components/admin/PdfLessonContentPhase.tsx` | 28417 | 628 |
| `frontend/src/components/admin/CategoryFormDialog.tsx` | 14762 | 391 |
| `frontend/src/components/admin/DocumentUploadList.tsx` | 23029 | 508 |
| `frontend/src/app/admin/lessons/page.tsx` | 15846 | 362 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.tsx` | 4774 | 122 |
| `frontend/src/components/admin/CategoryMovePreviewDialog.tsx` | 5327 | 138 |
| `frontend/src/components/admin/LessonForm.tsx` | 38315 | 881 |
| `frontend/src/components/admin/DocumentLibraryFilterBar.tsx` | 7816 | 182 |
| `frontend/src/components/admin/KnowledgeQnATable.tsx` | 9622 | 215 |
| `frontend/src/components/admin/KnowledgeQnAAnswerDialog.tsx` | 15703 | 359 |

## Review Outcome — Phase 13 (Round 20)

**Phase 13 is fully QA-closed: 14/14 ✅ on a FULL round.** It has no Security gate. The module still cannot proceed wholesale to devops because Phase 12 and other gated phases await the separately authorized security audit.

## Change Log (Round 20)

- 2026-08-27 — Round 20 FULL, Phase 13 / Module M: 14/14 verified from scratch; no issues, no checkbox changes, no application-code changes. Phase 13 is final-FULL QA eligible.

---

## Prior review — Round 19 / Phase 12

**Round 19 — Mode: TARGETED — Phase 12 / Module L, owner-authorized re-check of tasks 528 and 531 after Round 18 FULL.** Scope was limited to the two new tests, their test infrastructure, production paths they exercise, and the backend verification blast radius. Production behavior was unchanged.

**Result: both gaps are genuinely closed. Phase 12 is 31/31 ✅ Verified, 0 Partial, 0 Failed.** Tasks 528 and 531 changed back to `[x]` in `plan.md`. The functional phase is closed; the independent `🔒 Security gate` remains open.

1. **Task 528 ✅:** `ModuleLRepositoryIsolationTests.CompanyACannotReadOrMutateCompanyBsTrashJobsOrPurgeDependencies` runs against PostgreSQL using the real `ApplicationDbContext`, `LessonConfigRepository`, `BackgroundJobRepository`, and all Module L dependency repositories. It executes the production `TryArchive`, `TryClaimPurge`, `TryRestore`, `TryRestoreAndCancelPurge`, `CancelPendingLessonPurge`, and `AccelerateLessonPurge` paths with company A targeting company B; every call affects zero rows. It also executes every relevant `IgnoreQueryFilters()` read path and then reopens the graph under company B to prove no state changed. Fixture IDs/company IDs are GUID-scoped and cleanup deletes only those exact fixture companies.
2. **Task 531 ✅:** `ProcessAsync_CompleteDependencyGraph_RemovesOnlyPurgeDependentsAndRetainsLearnerHistory` seeds link/session/question, narration, `LessonExcludedSlide`, document/chunk, Q&A/source/conflict and the purge job. Its source-repository deletion callback asserts the permanent question exclusion already exists at the exact moment source deletion begins. Final assertions prove every purge-dependent row/vector/storage key is gone while link/session/question/exclusion/job history remains.

Automated checks run by QA:

- Backend Release build ✅ 0 warnings, 0 errors.
- Targeted `LessonPurgeWorkerTests` ✅ 17/17.
- PostgreSQL production-repository integration test ✅ 1/1.
- Backend non-integration suite ✅ 351/351 (10 API + 41 Providers + 300 Application).
- EF pending-model check ✅ clean.
- FULL suite remains characterized as 11 unrelated external-provider/config failures; the integration project itself is 12/12 and no new Module L test fails.

## Review Outcome — Phase 12 (Round 19)

**Phase 12 closes functionally at 31/31 ✅.** No application-code defect was found by this re-check. Security audit remains mandatory before devops. Per the project owner's authorized sequence, QA proceeds next to one FULL Phase 13 round; it does not dispatch devops.

## Change Log (Round 19)

- 2026-08-27 — Round 19 TARGETED, Phase 12 / Module L: owner authorized another re-check beyond the prior ceiling; production PostgreSQL tenant-isolation coverage and complete purge-graph coverage both passed. Tasks 528/531 checked; Phase 12 now 31/31. Security gate remains open.

---

## Prior review — Round 18 / Phase 12

**Round 18 — Mode: FULL — Phase 12 / Module L, final whole-phase re-check after Round 16's TARGETED closure. All 31 tasks were re-read against R9, DM-2/DM-11/DM-15/DM-16/DM-18, MG-L1, and LT-1..LT-24; production code, tests, migration, API/client types, UI, and documentation were inspected again rather than carried forward from the prior manifest.**

**Result: 29/31 ✅ Verified, 2/31 ⚠️ Partial, 0/31 ❌ Failed. Phase 12 is reopened and is not deploy-eligible.** Tasks 528 and 531 were changed back from `[x]` to `[ ]` in `plan.md`; all other 29 checkboxes remain verified. The separate `🔒 Security gate` is still open and was not performed by this QA role.

Security-sensitive result: no Critical/Important production leak was found. Every Module L `IgnoreQueryFilters()` path was read directly; scoped paths re-apply `CompanyId` in the same query/update, the worker resolves `job.CompanyId` before repository access, and raw SQL uses EF parameters rather than interpolation. The issue is that the mandatory automated proof is incomplete, not that the inspected production predicates are currently unsafe.

### Automated checks

- Frontend `npm run typecheck` ✅ clean.
- Frontend `npm run lint` ✅ clean.
- Frontend `npm run test` ✅ 82/82 passed, 12 files.
- Frontend `npm run build` ✅ clean, 24 routes generated.
- Backend `dotnet build SupportRoom.slnx -c Release --no-restore` ✅ 0 warnings, 0 errors.
- Backend `dotnet test SupportRoom.slnx -c Release --no-build --filter "Category!=Integration"` ✅ 350/350 (10 API + 41 Providers + 299 Application).
- Backend FULL `dotnet test SupportRoom.slnx -c Release --no-build` ⚠️ 353 passed / 11 failed; all 11 failures are existing external-provider tests requiring Google credentials/network (Slides, voice/TTS, embedding/provider), not Module L.
- EF `dotnet ef migrations has-pending-model-changes ... --no-build` ✅ no pending model changes.

### Per-task result

| Result | Phase 12 tasks |
|---|---|
| ✅ 29 Verified | 509–527, 529–530, 532–539 |
| ⚠️ 2 Partial | 528, 531 |
| ❌ Failed | none |

### Issues found — Round 18

1. **P12-11 Important — task 528, tenant-isolation test coverage.** `CompanyIsolationTests.cs:417-426` explicitly says the PostgreSQL-specific `TryArchive`/`TryClaimPurge`/`TryRestore*`/job-update methods cannot run on the InMemory provider and are “provable only by code inspection.” The four tests at `LessonTrashServiceTests.cs:470-525` invoke `FakeLessonConfigRepository`/`FakeBackgroundJobRepository`, not the production repositories or their SQL. They prove that the hand-written fakes reject company B, but they cannot detect a future or current mismatch in the real `WHERE CompanyId = ...` clauses. This does not satisfy task 528's explicit requirement to cover every trash/job/purge repository bypass. **Direct fix:** add a small PostgreSQL-backed repository test fixture and execute every production bypass/raw-SQL method with company A targeting company B, asserting zero mutations. Do not weaken the task to fake-level coverage.
2. **P12-12 Important — task 531, complete purge-finalization graph coverage.** `LessonPurgeWorkerTests.cs:348-424` proves retained link/session/question/job/exclusion rows and separately proves `LessonExcludedSlide` removal, but it never seeds and asserts deletion of narration, lesson-scoped Q&A, Q&A source/conflict, document resource, and chunk in the same successful finalization path. The missing-file test only proves one document resource is removed; the shared-PDF test proves preservation. Therefore the required assertion that finalization removes **every scoped dependent** is still absent. **Direct fix:** add one complete-graph purge test that seeds all LT-15 dependency types, records/validates exclusion creation before source removal, and asserts every removable dependent is gone while all LT-19 history rows remain.

These are the same coverage family previously grouped under P12-08. Round 15 was the first failed re-check, Round 16 marked them closed, and this FULL round found that closure was incomplete. Per the re-check ceiling, QA stops here for the project owner to decide routing; it does not auto-dispatch or fix application code.

### Verified file manifest — Round 18 FULL

| File | Bytes | Lines |
|---|---:|---:|
| `_docs/module/knowledge-base/design.md` | 650057 | 2547 |
| `_docs/module/knowledge-base/plan.md` | 217581 | 1049 |
| `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs` | 12839 | 223 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260826151755_AddLessonTrashLifecycle.cs` | 3605 | 84 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonConfigRepository.cs` | 9116 | 190 |
| `backend/src/SupportRoom.Providers.Data/Repository/IBackgroundJobRepository.cs` | 5402 | 111 |
| `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs` | 50528 | 964 |
| `backend/src/SupportRoom.Application/Services/IBackgroundJobProcessor.cs` | 43803 | 837 |
| `backend/src/SupportRoom.Application/Services/ITrainingLinkService.cs` | 11532 | 234 |
| `backend/src/SupportRoom.Application/Services/ILearningSessionService.cs` | 19964 | 405 |
| `backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs` | 11855 | 224 |
| `backend/tests/SupportRoom.Application.Tests/LessonTrashServiceTests.cs` | 21879 | 527 |
| `backend/tests/SupportRoom.Application.Tests/LessonPurgeWorkerTests.cs` | 26188 | 578 |
| `backend/tests/SupportRoom.Application.Tests/RevokedLinkPolicyTests.cs` | 11706 | 317 |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 20152 | 454 |
| `backend/tests/SupportRoom.Application.Tests/KnowledgeQnAServiceTests.cs` | 15368 | 369 |
| `frontend/src/lib/api-client.ts` | 34488 | 794 |
| `frontend/src/app/admin/lessons/page.tsx` | 15846 | 362 |
| `frontend/src/components/admin/LessonTrashList.tsx` | 7524 | 166 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.tsx` | 4774 | 122 |
| `frontend/src/components/admin/LessonTrashList.test.tsx` | 4254 | 84 |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.test.tsx` | 2114 | 58 |
| `frontend/src/types/domain.ts` | 28180 | 691 |

## Review Outcome — Phase 12 (Round 18)

**QA FULL completed but Phase 12 does not close: 29/31 ✅, 2 ⚠️, 0 ❌.** No production security defect was found, but tasks 528 and 531 cannot remain checked because their required tests do not exist at the strength/coverage stated in `plan.md`. Security gate remains open independently. Work stops here without dispatch.

## Change Log (Round 18)

- 2026-08-27 — Round 18 FULL, Phase 12 / Module L: re-read all 31 tasks; reopened tasks 528 and 531 after finding that Round 16's TARGETED closure accepted fake-only tenant tests and incomplete dependent-graph purge assertions. Result 29 Verified, 2 Partial, 0 Failed. No application code changed; only QA artifacts were updated. Security gate remains open.

---

## Prior current round — Round 17 / Phase 13

**Round 17 — Mode: TARGETED — Phase 13 / Module M, re-check of the last two tasks (CD-5 point 5 / `handleArchiveLesson`, and the final CD-1 sweep verification), now that Phase 12 closed QA at 31/31 (Round 16) and unblocked them. Round 13 was FULL with a recorded manifest, making TARGETED legitimate here per this module's convention.**

**Scope of this round, stated explicitly:** the two newly-implemented tasks themselves, checked to the same standard as a FULL round; the one file they touch (`frontend/src/app/admin/lessons/page.tsx`) and its blast radius (the other 3 dialog swaps living in the same file, and every file that calls into it); the shared-code watchlist (backend files that showed as modified in the working tree but turned out to be untouched since Phase 12's last round, confirmed by stat, not assumed); the full CD-1/CD-9/CD-10 contract re-check; and a whole-project typecheck/lint/test/build pass on both frontend and backend. **This round does NOT re-inspect the other 12 already-closed Phase 13 tasks (Group A's NR-20..NR-24, and CD-5 points 1–4) from scratch** — those were verified FULL in Round 13 and are not re-read here; only their manifest entries were stat-compared to confirm they weren't disturbed by this fix.

**Result: both tasks are genuinely done. Phase 13 is now 14/14 ✅ Verified, 0 Partial, 0 Failed — the phase is closed.** Both remaining checkboxes changed from `[ ]` to `[x]` in `plan.md`.

1. **CD-5 point 5 (`handleArchiveLesson` → `AlertDialog`)** — read `frontend/src/app/admin/lessons/page.tsx` in full (grew from 14574/335 bytes/lines in the Round 13 manifest to 15846/362, consistent with one new state variable, one new handler, and one new `AlertDialog` block being added). Confirmed against `design.md`'s CD-5 table row 5 and the CD-1..CD-10 rule table, field by field:
   - New state `pendingArchiveLesson: LessonConfig | null`, holding the row object itself (not a closure) — matches CD-4's payload requirement and the exact convention already used by `pendingDeleteParent`/`pendingResetDemoData` in the same file.
   - `confirmArchiveLesson()` is a clean split of the old blocking-`window.confirm` function into an opener (`setPendingArchiveLesson(lesson)`, wired from `CategoryTree`'s `onArchiveLesson` callback, itself unchanged) and a doer (`confirmArchiveLesson`) called only from the `AlertDialogAction` — no `Promise`/`ref` wrapping (CD-4's explicit prohibition), confirmed by reading the function body directly.
   - Dialog uses the established inline `AlertDialog` pattern (`open={pendingArchiveLesson !== null}`, `onOpenChange={(next) => !next && setPendingArchiveLesson(null)}`) — the same shape as the other 3 dispatched points in this file, **not** `LessonPermanentDeleteDialog`'s typed-confirmation `Dialog` pattern, which CD-2/CD-9 explicitly forbid here.
   - Title "ย้ายบทเรียนไปถังขยะ", body text `ต้องการย้ายบทเรียน "{lesson.title}" ไปถังขยะใช่หรือไม่? ลิงก์การสอนทั้งหมดของบทเรียนนี้จะถูกยกเลิกทันที`, confirm button "ย้ายไปถังขยะ" with `variant="destructive"`, `AlertDialogCancel` at its shared-component default (`variant="outline"`, "ยกเลิก") — compared character-for-character against `design.md`'s CD-5 table row 5 and the CD-5 rule text: exact match, no punctuation or wording drift.
   - **API call, permission, and post-action behaviour confirmed genuinely unchanged from before the swap:** `confirmArchiveLesson()` still calls `api.archiveLesson(lesson.id)` (same endpoint, same argument shape as `archiveLesson(id: string)` in `api-client.ts`, itself unmodified), still sets `busyLessonId` before the call and clears it in `finally`, still removes the lesson from local state and increments `trashRefreshToken` on success (the exact LT-3 bookkeeping CD-8 requires so the trash tab refreshes), and still routes a failed call to the same `setError(...)` used by every other handler in this file — no `toast`, no in-dialog error surface. The role gate itself (`CAN_ARCHIVE_LESSON.has(role)`) lives in `CategoryTree.tsx`, which is untouched (byte-for-byte identical to its last-verified state) — the new dialog does not become a new permission layer and does not remove the existing one, satisfying CD-8 directly.
2. **The final CD-1 sweep verification** — independently re-ran `Grep` for `window\.(confirm|alert|prompt)` across the whole `frontend/src` tree myself, not trusting the engineer's count: the only hits are comment references in `CategoryFormDialog.tsx`, `DocumentUploadList.tsx`, and `admin/lessons/page.tsx` documenting which `AlertDialog` block replaced which original call — zero live calls remain anywhere. Confirmed no sixth site exists and that this round's fix touched only `admin/lessons/page.tsx`: stat-compared `CategoryFormDialog.tsx` (14762/391), `DocumentUploadList.tsx` (23029/508), `LessonPermanentDeleteDialog.tsx` (4774/122), `CategoryMovePreviewDialog.tsx` (5327/138), and `LessonForm.tsx` (38315/881) against the Round 13 manifest — all five byte-for-byte and line-for-line identical, confirming the other 4 dispatched dialogs and all 4 of CD-9's named protected dialogs were not touched.

**Shared-code watchlist and blast-radius check.** The working tree shows several backend files as modified (`LearningSessionController.cs`, `TrainingLinkController.cs`, `TtsController.cs`, `ILearningSessionService.cs`, `ILessonConfigService.cs`, `IVoiceQuestionService.cs`, `ILessonConfigRepository.cs`, `ServiceTestFakes.cs`, `LessonTrashServiceTests.cs`) and one frontend file (`PdfLessonContentPhase.tsx`) beyond `admin/lessons/page.tsx` — every one of these was stat-compared against the Phase 12 Round 16 / Phase 13 Round 13 manifests and found byte-for-byte and line-for-line identical to their last-verified state. This confirms they are prior-round changes already accounted for (Phase 12's Module L work and Phase 13's Group A work), not new changes introduced by this round's fix, and this round's blast radius is genuinely confined to the one file. `Glob` of `frontend/src/app/admin/lessons/*` and `frontend/src/components/admin/*.test.tsx` found no new files beyond what the Phase 12 manifest already accounts for (`LessonPermanentDeleteDialog.test.tsx`, `LessonTrashList.test.tsx`) — no test file was added for this phase's dialog swaps, consistent with `plan.md` having no `[frontend] test` task for Phase 13's Group B.

**Automated checks, run project-wide:**

- Frontend `npm run typecheck` ✅ clean.
- Frontend `npm run lint` ✅ clean (`eslint .`, no output).
- Frontend `npm run test` ✅ 82/82 passed, 12 test files.
- Frontend `npm run build` ✅ compiled cleanly, 24/24 routes generated, no errors.
- Backend `dotnet build SupportRoom.slnx -c Release` ✅ 0 warnings, 0 errors.
- Backend `dotnet test SupportRoom.slnx -c Release --filter "Category!=Integration"` ✅ 350/350 (41 Providers.Tests + 10 Api.IntegrationTests + 299 Application.Tests) — identical to Phase 12 Round 16's count, confirming no backend regression from this frontend-only fix.

**What this round does not cover:** the other 12 Phase 13 tasks (Group A's NR-20..NR-24 and CD-5 points 1–4) were not re-inspected from scratch — that was a genuine FULL round in Round 13 and nothing in their manifest entries has moved since. This round also did not run a security audit; Phase 13 carries no `🔒 Security gate` (re-confirmed below), so none is owed here, but Phases 2/3/4/6/7/8/10/11/12 remain independently gated and unaudited regardless of this round's result.

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

## Verified File Manifest — Phase 13

Files re-checked this round (Round 17, TARGETED). This round did not re-inspect the 17 files already covered by Round 13's FULL manifest from scratch — it stat-compared every one of them against that manifest (all found byte-for-byte and line-for-line identical unless noted) and read in full only the one file this fix touched.

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `frontend/src/app/admin/lessons/page.tsx` | 15846 | 362 | Round 17 (CD-5 point 5 implemented: `pendingArchiveLesson` state, `confirmArchiveLesson()`, new `AlertDialog` block) |
| `frontend/src/components/admin/PdfLessonContentPhase.tsx` | 28417 | 628 | Round 13 (confirmed unchanged this round) |
| `frontend/src/components/admin/CategoryFormDialog.tsx` | 14762 | 391 | Round 13 (confirmed unchanged this round) |
| `frontend/src/components/admin/DocumentUploadList.tsx` | 23029 | 508 | Round 13 (confirmed unchanged this round) |
| `frontend/src/components/admin/LessonPermanentDeleteDialog.tsx` (CD-9, must stay untouched) | 4774 | 122 | Round 13 (confirmed unchanged this round) |
| `frontend/src/components/admin/CategoryMovePreviewDialog.tsx` (CD-9, must stay untouched) | 5327 | 138 | Round 13 (confirmed unchanged this round) |
| `frontend/src/components/admin/LessonForm.tsx` (CD-9's `lesson-editor-pdf-replace-dialog`, must stay untouched) | 38315 | 881 | Round 13 (confirmed unchanged this round) |
| `frontend/src/components/admin/CategoryTree.tsx` (role-gate/`onArchiveLesson` caller, not previously in this phase's manifest — added to the watchlist here) | — | — | Round 17 (read in full, confirmed unmodified in the working tree; `CAN_ARCHIVE_LESSON` role gate and lesson-object payload contract both intact) |
| `frontend/src/lib/api-client.ts` (`archiveLesson(id)`, shared-code watchlist) | 34488 | 794 | Round 13/15 (confirmed unchanged this round) |
| `backend/src/SupportRoom.Api/Controllers/LearningSessionController.cs` | 5520 | 113 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/src/SupportRoom.Api/Controllers/TrainingLinkController.cs` | 4380 | 96 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/src/SupportRoom.Api/Controllers/TtsController.cs` | 1592 | 38 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/src/SupportRoom.Application/Services/ILearningSessionService.cs` | 19964 | 405 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/src/SupportRoom.Application/Services/ILessonConfigService.cs` | 50528 | 964 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs` | 11855 | 224 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/src/SupportRoom.Providers.Data/Repository/ILessonConfigRepository.cs` | 9116 | 190 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs` | 38694 | 746 | Phase 12 Round 15/16 (confirmed unchanged this round) |
| `backend/tests/SupportRoom.Application.Tests/LessonTrashServiceTests.cs` | 21879 | 527 | Phase 12 Round 16 (confirmed unchanged this round) |

`Glob` of `frontend/src/app/admin/lessons/*` and `frontend/src/components/admin/*.test.tsx` found no files beyond what the Round 13/Phase 12 manifests already account for — no new test file exists for this phase's dialog swaps. `Grep` for `window\.(confirm|alert|prompt)` across all of `frontend/src` returned zero live calls, only comment references — confirming CD-1's scope closure independently, not from the engineer's report.

Phase 13 is now fully closed (14/14). The next re-check of this phase (the FULL round still owed before `devops`) should treat this manifest as its starting point.

## Per-Task Results — Phase 13 (Round 17, TARGETED — only the two previously-blocked rows changed; the other 12 rows are carried forward verbatim from Round 13, archived in `review/phase-13.md`, not re-inspected this round)

- ✅ [frontend] CD-5 point 5 (`handleArchiveLesson`) — replaced with the established inline `AlertDialog` pattern (`pendingArchiveLesson` state holding the `LessonConfig` row itself, opener/doer split, no `Promise`/`ref` wrapping); title/description/confirm-button-label/`variant="destructive"` match `design.md`'s CD-5 table row 5 character-for-character; `AlertDialogCancel` at shared-component default; API call (`api.archiveLesson(lesson.id)`), `setBusyLessonId`/`setTrashRefreshToken` bookkeeping, and `setError` failure routing all confirmed unchanged from the pre-swap behaviour; `CategoryTree.tsx`'s `CAN_ARCHIVE_LESSON` role gate and the `(lesson) => void` payload contract both confirmed untouched.
- ✅ [frontend] Final CD-1 sweep verification — its premise ("after all 5 points are done") is now true; independently re-grepped the whole `frontend/src` tree and confirmed zero live `window.confirm`/`alert`/`prompt` calls remain (only comment references), no 6th site exists, and none of CD-9's four protected dialogs or the other 4 dispatched CD-5 points were touched by this round's fix (all five files stat-identical to the Round 13 manifest).

## Design/requirement contract checks — Phase 13 (Round 17)

- **CD-5 row 5** — compared word-for-word against `design.md`'s CD-5 table and the CD-5/CD-6/CD-7/CD-8 rule rows: title, body copy (including the exact "ลิงก์การสอนทั้งหมดของบทเรียนนี้จะถูกยกเลิกทันที" clause), button label, and `variant="destructive"` all match; no wording, punctuation, or scope drift.
- **CD-8 (permission/condition/error-path preservation)** — verified the role gate (`CAN_ARCHIVE_LESSON.has(role)` in `CategoryTree.tsx`) and the `setBusyLessonId`/`setTrashRefreshToken`/`setError` bookkeeping are byte-for-byte unchanged from before the swap; the new dialog introduces no new permission layer and removes none.
- **CD-1/CD-9 (scope closure)** — re-verified by direct `Grep` of the whole `frontend/src` tree this round, independently of the engineer's report; zero live calls remain, all 4 of CD-9's named protected dialogs confirmed untouched by stat comparison.
- **Data Model** — unchanged; this round touched zero backend/schema files (confirmed via the manifest above), consistent with `design.md`'s Module M entry stating no migration/entity/endpoint for this phase.
- **🔒 Security gate** — re-confirmed `design.md`'s Module M reasoning still holds after this fix: no endpoint touched, no permission surface changed (the role gate lives in the untouched `CategoryTree.tsx`), no new server-sourced data displayed in the dialog, no change to verification-step count. Phase 13 correctly carries no gate.

## Unverified Behaviour — undeployed phases

This project has a real test suite (350 backend tests as of Phase 12 Round 16 — 41 `Providers.Tests` + 10 `Api.IntegrationTests` + 299 `Application.Tests` — and 82 frontend Vitest tests as of this round), so this section stays scoped to rules the suite cannot itself exercise. **This section had been dropped from `review.md` since a round before Phase 12 began** (its last carrier, Phase 13's Round 13, was archived to `review/phase-13.md` in full rather than kept live) — restored here from that archive, verbatim except for the Phase 13 block below, which is updated for this round's CD-5 point 5 completion. Existing undeployed-phase notes remain below until each phase deploys, per `conventions.md` §4.

### Phase 13 (Module M) — updated this round for CD-5 point 5's completion
- **The commit modal's 3-state machine and 3-part lock invariant** (NR-20/NR-23/NR-24) — established correct by direct code reading and tracing every `setCommitModalOpen`/`onOpenChange` call site, not by an executed test opening the dialog, triggering Esc/outside-click, and asserting it stays open; no component/integration test exists for `PdfLessonContentPhase.tsx`.
- **NR-21's "no automatic navigation" guarantee** — verified by reading both `flushNarrations()` call sites and confirming no `router.push` remains in either; not by a test that drives a full commit-then-observe-no-navigation cycle.
- **NR-22's checklist tick timing** ("ห้ามติ๊กก่อน API ตอบสำเร็จ") — verified by reading that each status derivation depends only on state set inside a `try` block after an `await` resolves; not by a test that stalls a mock API call and asserts the row is still "active," not "success," mid-flight.
- **All 5 confirm-dialog swaps' CD-7/CD-8 preserved-behaviour guarantees** (cancel semantics, error routing, `setBusyLessonId`/`setTrashRefreshToken` bookkeeping for point 5 specifically) — verified by reading the `onOpenChange`/confirm-handler code directly, including the newly-added point 5 (`handleArchiveLesson`) this round; no test exercises an actual open-dialog → cancel/confirm → assert-page-state cycle for any of the 5 points.

### Phase 11 (Module K) — closed in Round 12, still undeployed
- The frontend create-PDF orchestration still has no component/integration test. Round 10/12 confirm the P11-02 fix by tracing `touchedAndNotExcludedIds` through step-4 flush, progress totals and retry state, not by an executed browser assertion.
- `ProcessLessonIndexAsync` and `ProcessDocumentIndexAsync` implement the two-vector exclusion paths, but no automated test executes those private worker methods end to end against a knowledge provider; correctness was established by direct code inspection.
- `LessonExcludedSlideReconciler.ReconcileAndLoad`'s dedup/hard-delete correctness (P11-01, closed this round) was established by direct code and test inspection — the regression tests seed the corruption state directly into the fake repository's backing store rather than exercising a live PostgreSQL unique-constraint race; the actual EF `Remove`-then-`SaveChanges` hard delete against a real database was not executed by this round.
- An already-open learner tab retaining its original slide list while vectors disappear immediately (R4.7.10/Q-K2) is browser/session timing behaviour and was not exercised live in this round; it remains an explicitly accepted contract risk, not a defect.

### Phase 8 (Module H)
- **The shared filter bar's `status` wiring actually causing both tables to refetch** — confirmed by reading the `useEffect` dependency arrays in `DocumentUploadList.tsx`/`KnowledgeQnATable.tsx`, never driven by a running test that changes the status filter and asserts both tables re-fetch.
- **KL-19/KL-20's four duplicate-outcome classification** is unit-tested for its *logic* (`DocumentResourceServiceTests.cs`), but the *upload dialog's* handling of the 409 response — `DocumentDuplicateDialog.tsx` picking the right message per combination, "อัปโหลดต่อไป" correctly resending with `checkDuplicate: false` — is verified by code reading only.
- **KL-23/KL-26's Q&A duplicate gate UI** — the 409 catch in `KnowledgeQnAAnswerDialog.tsx`, the list state, "แก้ใบเดิมแทน" switching the same dialog to edit mode in place without a fetch — is unit-tested on the backend (`KnowledgeQnAServiceTests.cs`) but the frontend flow itself has zero test coverage.
- **KL-14/KL-15/KL-16's edit/delete-from-library UI**, including the delete confirmation copy and the queue-question re-opening becoming visible after a refresh, is verified by reading `KnowledgeQnATable.tsx` and the backend's `DeleteAsync`/QQ-1 logic separately — no test drives an actual delete-then-reload-the-queue cycle end to end.

### Phase 10 (Module J) — carried forward from Round 5, still accurate after the NR-13 fix
- **NR-12's 4-step commit ordering, end-to-end** — the backend's per-endpoint behaviour at each step is unit-tested in isolation (`LessonConfigServiceTests.cs`), but the *frontend's* orchestration of the 4 steps in the correct order, with the correct resume-from-failure behaviour (including the now-fixed NR-13 step-1 display), is exercised by zero automated tests — verified this round by reading `PdfLessonContentPhase.tsx` directly, the same way the original NR-13 gap was found.
- **NR-15/NR-16's client-only draft-state rules** (what counts as "touched", what counts as "empty after trim", silent-clear-on-replace) — correct by direct inspection of the relevant `useState`/computed-value logic, but never executed by a test.
- **NR-13's per-step retry semantics** (steps 2/3/4 skip already-succeeded work on retry via the `xRef.current` guards) — verified correct by reading `commit()`'s guard conditions, not by driving an actual failure-then-retry sequence through a test.

### Phase 2 (Module B)
- KS-7 ("when the two prompt blocks conflict, the model must yield to block 1") and KS-8 (ban on copying Q&A text verbatim) — the prompt text sent to the model is verified correct by inspection (`RagVoiceQuestionProvider.BuildAnswerPrompt`), but whether the model actually obeys these instructions in a live call is not, and cannot be, exercised by an automated test.

### Phase 3 (Module C)
- MG-A3/MG-A4's default-chain backfill correctness (exactly 2 flagged rows per company, chain linkage, `LessonConfig.CategoryId` pointing at the leaf) was verified by the implementer's manual `psql` rehearsal against an isolated PostgreSQL database (recorded in `status.md`), not by an automated test — EF Core's InMemory provider cannot execute the raw-SQL backfill.
- `IBackgroundJobRepository.ClaimNext`'s `FOR UPDATE SKIP LOCKED` concurrency behaviour was verified manually against a real PostgreSQL instance (per `status.md`), not by an automated test, for the same reason.

### Phase 6 (Module F)
- KS-9/R5.5 ("the model reports a conflict, and it is a genuine one") — the code path that records a reported conflict is unit-tested for its own logic (validation, try/catch isolation), but whether the model's own judgment of "conflicting" is sound is a prompt/model-quality question `requirement.md` itself says is out of scope for code to guarantee (R5.5's stated limitation).

### Phase 7 (Module G)
- **DS-3's cross-company category-id rejection**, specifically: proven correct at the architecture level (the unchanged, already-verified `KnowledgeCategory.HasQueryFilter` scopes `Get(id)` to the caller's company) and proven live with a globally-nonexistent id — but not proven with a genuine second company's real category id, because the local dev seed data has exactly one company. See the Open Issues row above — not blocking, since the runtime protection is the same unchanged mechanism already verified correct in the Phase 1 FULL round.
- **Bug 2's owner-login auto-redirect** — verified by a full render-cycle code trace and by confirming live (via the real backend) that the exact single-company data condition this fix targets is true for the given test account, but not by literally driving a browser this round.

**Not yet covered by a dedicated block:** Phase 12 (Module L) closed its QA round (31/31, Round 16) without an `Unverified Behaviour` block ever being written for it — its rounds relied heavily on new, real tests for nearly every finding, but items like the `End()`/`GetOwnSummary()` looser-gate split (P12-04) and the exact interleaving proof for `TryArchive`/`TryRestoreAndCancelPurge` (P12-01/P12-02) were established by code tracing, not by a concurrency test that actually races two requests. This is a gap in Phase 12's own QA record, not something this round's scope covers — flagged here so it isn't lost, and left for Phase 12's still-owed FULL round to fill in properly.

## Issues Found — Phase 13 (Round 17)

None. Both previously-blocked tasks are now genuinely done, verified by direct code inspection against `design.md`'s CD-5 table and the CD-1..CD-10 rule text, not by trusting the engineer's report. The two non-blocking Open Issues carried over from Round 13 remain open (CD-5 points 2/3's missing error-surfacing path) — unaffected by this round's fix, which touched only point 5.

## Review Outcome — Phase 13 (Round 17)

**Phase 13 is fully verified and closed: 14/14 ✅, 0 ⚠️, 0 ❌.** Both of Round 13's blocked-not-failed tasks (CD-5 point 5 and the final CD-1 sweep) are now genuinely implemented and verified against `design.md`'s CD-5 table and CD-1..CD-10 rules. No regression found in the other 12 tasks' blast radius, the shared-code watchlist, or the whole-project automated checks.

**Phase 13 is NOT yet deploy-eligible, for one reason:** this closing round (Round 17) was TARGETED, not FULL. Per this module's QA convention, deploy eligibility requires the phase's *most recent* round to be FULL — the same distinction Phase 12 just hit. One more FULL round over all 14 tasks is needed before handoff to `devops`, to close on settled code rather than carry a TARGETED round as the final word. Phase 13 carries no `🔒 Security gate` (re-confirmed this round, `design.md`'s Module M reasoning still holds and no new gate condition was triggered), so unlike Phase 12, security audit is not a second independent blocker here — the FULL round is the only thing standing between this phase and `devops`.

Recommended next step: run one more FULL QA round on Phase 13 (cheap now, since nothing is expected to have changed beyond what Round 13 and Round 17 already covered between them).

## Archived rounds

- Phases 1–6 → `review/phase-1-6.md`
- Phase 3 → `review/phase-3.md`
- Phase 7 → `review/phase-7.md`
- Phase 8 → `review/phase-8.md`
- Phase 9 → `review/phase-9.md`
- Phase 10 → `review/phase-10.md`
- Phase 11 → `review/phase-11.md`
- Phase 12 — Round 14 FULL (15/31), Round 15 FULL (28/31), Round 16 TARGETED (31/31, closes Phase 12 functionally; superseded by Phase 13 Round 17 above) → `review/phase-12.md`
- Phase 13 — Round 13 FULL, 12/14 verified + 2 blocked → `review/phase-13.md`

## Change Log

- 2026-08-27 — Round 14 FULL, Phase 12 / Module L, first QA round: 15/31 Verified, 4 Partial, 12 Failed; archived to `review/phase-12.md`.
- 2026-08-27 — Round 15 FULL, Phase 12 / Module L, second QA round: 28/31 Verified, 3 Partial, 0 Failed. Verified P12-01..P12-07/P12-09/P12-06/P12-10 closed by direct code/test inspection; P12-08 narrowed to three specific remaining test gaps (failed re-check round 1 of 2). Phase remains open, not deploy-eligible, Phase 13's last two tasks remain blocked, and the Security gate remains open. Archived to `review/phase-12.md`.
- 2026-08-27 — Round 16 TARGETED, Phase 12 / Module L, re-check of P12-08's three narrowed sub-gaps: all three genuinely closed by direct code/test inspection. Phase 12 now 31/31 Verified, 0 Partial, 0 Failed — no open functional issues remain. Phase 13's last two blocked tasks are unblocked. Phase 12 is still not deploy-eligible: this round was TARGETED (one more FULL round is required before `devops` per this module's convention), and the Security gate remains independently open and unrun. Archived to `review/phase-12.md`, superseded by Round 17.
- 2026-08-27 — **Round 17 TARGETED, Phase 13 / Module M — closes the phase, 14/14 ✅ Verified, 0 Partial, 0 Failed.** Re-checked the two tasks Round 13 left blocked (CD-5 point 5 / `handleArchiveLesson`, and the final CD-1 sweep), now that Phase 12 unblocked them. Verified CD-5 point 5 genuinely matches `design.md`'s CD-5 table row 5 character-for-character (title/body/button/`variant="destructive"`), uses the same inline `AlertDialog` pattern (not `LessonPermanentDeleteDialog`'s typed-confirmation pattern), and preserves the pre-swap API call/`setBusyLessonId`/`setTrashRefreshToken`/`setError` behaviour and the untouched `CategoryTree.tsx` role gate exactly, per CD-8. Independently re-ran the CD-1 `window.confirm/alert/prompt` sweep across all of `frontend/src` myself (zero live calls, only comment references) rather than trusting the engineer's count, and stat-compared all 5 previously-dispatched dialog files plus CD-9's 4 protected dialogs against the Round 13 manifest (all byte-for-byte identical, confirming no collateral edits). Checked the shared-code watchlist and every backend file the working tree showed as modified — all stat-identical to their last-verified Phase 12 state, confirming this round's blast radius is genuinely confined to `admin/lessons/page.tsx`. Ticked both remaining checkboxes in `plan.md` (14/14 `[x]`). Automated checks project-wide: frontend typecheck/lint clean, test 82/82 (12 files), build clean (24 routes); backend Release build 0 warnings/0 errors, non-integration test 350/350 — identical to Phase 12 Round 16's count, confirming no backend regression from this frontend-only fix. **Restored `review.md`'s `## Unverified Behaviour — undeployed phases` section**, which had been dropped since a round before Phase 12 began (last carried live in Phase 13's own Round 13, then lost when that round was archived in full) — recovered from the `review/phase-13.md` archive verbatim, with the Phase 13 block updated for this round's CD-5 point 5 completion, and flagged that Phase 12 (Module L) closed its QA round without ever getting its own such block. Phase 13 carries no `🔒 Security gate` (re-confirmed). **Phase 13 is not yet deploy-eligible: this closing round was TARGETED, so one more FULL round is owed before `devops`** — the same gate Phase 12 is independently sitting behind.
