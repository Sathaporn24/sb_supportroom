# Knowledge Base & Teaching Content Intake — Verification & Review

## Open Issues — all phases

| Issue | Phase | Routes to | Blocking | Rounds |
|---|---|---|---|---|
| R-2 latency measurement (3-namespace query) still not taken — no traffic/deployment exists to measure against yet. Not a code defect; carried forward from the Phase 2 backend handoff in `status.md`. | Phase 2 (`review/phase-1-6.md`) | `devops` (once deployed with real traffic) | No | 0 |
| No `security` audit has run on any phase in this module yet. Phases 2, 3, 4, 6, 7 all carry `🔒 Security gate` in `plan.md` and cannot reach `devops` until `security` runs, independent of round mode. | Phases 2, 3, 4, 6, 7 | `security` | Yes, for the gated phases | 0 |
| `DS-3`'s "category id belongs to a different company" rejection case is proven correct by direct inspection of the unchanged, already-verified `KnowledgeCategory` `HasQueryFilter` and by a live test against the real running app with a globally-nonexistent id (behaves identically under the filter) — but not by a dedicated unit test with a company-scoped fake (`FakeKnowledgeCategoryRepository` in `ServiceTestFakes.cs` does not filter by `CompanyId` at all) or by a live test with a genuine second company (the local dev seed data has only one company). Not a functional defect — a test-coverage strengthening suggestion. | Phase 7 | `backend-engineer` (optional hardening, not blocking) | No | 0 |

## Verification Summary (current round)

**Round 3 — Mode: FULL (Phase 7, first-ever round for this phase; also the first-ever confirmation of two bugs found and fixed during today's manual testing).** Covers all 22 `plan.md` tasks in Phase 7 (14 `[backend]`, 8 `[frontend]`) from scratch, plus:

1. **Bug 2** — an owner with exactly one company could never sign in (stuck forever on "เลือกบริษัทก่อนเริ่มทำงาน" because `CompanySwitcher` renders no interactive control at all when `companies.length <= 1`, just a `<span>`). Fixed in `frontend/src/components/admin/AdminSessionProvider.tsx` with a new effect that auto-selects the single company via `router.replace` when `user.role === "owner"`, `companies.length === 1`, and nothing is resolved yet.
2. **Bug 3** — the category `Select` in the scope picker needed two clicks to register, a classic Base UI uncontrolled→controlled transition (`value={scopeId}` starts as `undefined`). Fixed in `frontend/src/components/admin/DocumentUploadList.tsx` (`CompanyOrCategoryScopeFields`) and `frontend/src/components/admin/KnowledgeQnAAnswerDialog.tsx` (the same bug, inherited from the Phase 6 original this component was copied from) by changing to `value={scopeId ?? ""}`.

Per the user's explicit instruction, this round did **not** re-check Bug 1 (the Phase 3 cross-company `GetDeleted()` leak/IDOR — already closed and confirmed by Round 2/TARGETED, see `review/phase-1-6.md`) beyond confirming via the file-manifest diff that Phase 7 did not touch the same code again (`IDocumentResourceRepository.cs` changed only by +0 bytes beyond Round 2's fix — confirmed identical to the Round 2 manifest row).

**Phase 7 tasks — all 22 verified by direct code read, not taken on the implementer's report:**

- **DS-1** (`UploadDocumentDto`/`UploadDocumentRequest`) — `LessonSlug` fully removed, replaced by `required string ScopeType` + `string? ScopeId`, exact same shape as `KnowledgeQnADto`. `ScopeId` documented and used as `LessonConfig.Id` (not `Slug`) for lesson scope. Confirmed in `UploadDocumentDto.cs` and `DocumentsController.cs`.
- **DS-2** (`EnsureValidScope` before storage) — `DocumentResourceService.UploadAsync`'s first line is `namespaceResolver.EnsureValidScope(CurrentCompanyId, input.ScopeType, input.ScopeId)`, before `storageProvider.UploadAsync`. Confirmed by direct read of `IDocumentResourceService.cs`.
- **DS-3** (6 rejection cases) — all 6 confirmed **both** by the real unit tests (`DocumentResourceServiceTests.cs`: `UploadAsync_Rejects_UnknownScopeType`, `..._LessonScopeId_NotInThisCompany`, `..._CategoryThatIsALevel1Parent`, `..._CategoryScopeId_ThatDoesNotExist`, `..._CompanyScope_WithAScopeId`, `..._LessonOrCategoryScope_WithNoScopeId` (Theory×2)) **and** by hitting the real running backend (`dotnet run`, real Postgres, real JWT) directly with `curl`: company+scopeId → 400, unknown scopeType → 400, category→ghost id → 404, lesson→no scopeId → 400, lesson→ghost id → 404, category→Level-1-parent id → 400 (all 6 confirmed with the actual HTTP status code returned, using the real seeded `kbcat-backfill-parent-...`/`kbcat-backfill-child-...` default-chain ids). No second validation path exists — `GeneralException.NotFound`/`ValidationError` map to 404/400 via the unchanged `HttpStatusCodeExceptionHandler`.
- **DS-4** (`GET /api/documents` scope query) — `DocumentsController.GetAll(scopeType, scopeId)` calls the single `IDocumentResourceService.GetByScope`; `GetByLessonSlug`/`GetStandalone` no longer exist anywhere in the interface. Omitting the query defaults to `company`, confirmed by both the code (`string.IsNullOrEmpty(scopeType) ? KnowledgeScopeType.Company : scopeType`) and the existing `GetByScope_DefaultsToCompany_WhenNoQuerySent` test.
- **DS-5/DS-9** — `IDocumentResourceService.MoveScopeAsync(id, MoveDocumentScopeDto)`, `PATCH /api/documents/{id}/scope` wired to it, `MoveDocumentScopeDto` DTO exists with the exact `ScopeType`/`ScopeId` shape, `DocumentResourceViewModel` unchanged (`ScopeType`/`ScopeId` already existed).
- **DS-6** (the move itself) — confirmed transactional and matching the spec exactly: reads `DocumentChunk` rows, groups by `NamespaceKey`, creates one `vector_delete` `BackgroundJob` per group with `VectorDeleteJobPayload{NamespaceKey, VectorIds}` (identical shape to DI-13's delete path), soft-deletes the `DocumentChunk` rows, writes new `ScopeType`/`ScopeId` + `IndexingStatus = pending` + `IndexedChunkCount = 0` + `FailureReason = null` + `UpdateBy`/`UpdateDate`, enqueues `document_index` — all inside one `UnitOfWork.Commit()`. No new `BackgroundJobType` was added; the worker (`IBackgroundJobProcessor`) was not touched (confirmed unchanged via the file-manifest diff). Proven by `MoveScopeAsync_QueuesVectorDelete_AndReindex_WhenScopeActuallyChanges`.
- **DS-7** (edge cases) — all 5 confirmed by dedicated tests: same-scope move is a true no-op (`MoveScopeAsync_IsANoOp_WhenMovingToTheExactSameScope` — zero jobs enqueued, chunks untouched), a document with no `DocumentChunk` rows creates no `vector_delete` but still re-queues `document_index` (`..._DoesNotQueueVectorDelete_WhenDocumentHasNoPersistedChunks`), a soft-deleted document 404s (`..._ThrowsNotFound_WhenDocumentIsSoftDeleted`, relying on the query filter behind `_repository.Get()`), a document that's a lesson's PDF source can still be moved unlike delete (`MoveScopeAsync_Allowed_ForADocumentThatIsALessonsPdfSource`, explicitly contrasted with `DeleteAsync`'s block in the test's own comment), and permissions are the same as upload/delete (no new role check exists anywhere in `MoveScopeAsync`, confirmed by direct read).
- **DS-10** — confirmed `IKnowledgeCategoryService.cs` is byte-for-byte unchanged since the last QA round (manifest stat match), so TX-6/TX-10's counting code was correctly left untouched.
- **DS-11** (no migration) — confirmed three ways: `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration"; the `Migrations/` directory has no file dated after `20260819134222_AddKnowledgeQnA.cs` (Phase 6); `git status --porcelain` shows no untracked/modified migration file.
- **DS-12** (3 test groups) — confirmed present and correct: the category-scope-succeeds + 6-rejection-case group, the move-scope-creates-correct-vector_delete-payload group, and the no-op/no-chunks group — all read in full above.
- **API_CONTRACT.md** — updated to the new wire shapes for all three endpoints (`POST`/`GET /api/documents`, `PATCH /api/documents/{id}/scope`), confirmed by direct read.
- **Frontend**: `domain.ts`'s `DocumentScope`/`DocumentResource` types use `scopeType`/`scopeId` (no `lessonSlug` residue on this type — the unrelated `TrainingLink.lessonSlug`/`CreateTrainingLinkInput.lessonSlug` fields belong to `learning-session` and were correctly left alone). `api-client.ts`'s `uploadDocument`/`listDocuments` send/receive `scopeType`/`scopeId`, and a new `moveDocumentScope` calls `PATCH .../scope`. `DocumentUploadList.tsx` used inside the lesson editor (`fixedScope={{ scopeType: "lesson", scopeId: lesson.id }}`) has no picker at all (confirmed by the `libraryMode` conditional gating every scope-related control). Both `app/admin/lessons/[slug]/page.tsx` (line 166, 480–481) and `app/admin/lessons/new/page.tsx` (line 96, using `company` scope since the lesson row doesn't exist yet at upload time — a reasonable carry-over of the previous "standalone" behaviour, not a contract violation) call the new shape. `app/admin/documents/page.tsx` gained the RadioGroup+Select scope picker (`CompanyOrCategoryScopeFields`, copied from `KnowledgeQnAAnswerDialog.tsx` as instructed, not a second pattern), a "ขอบเขต" column + filter, a corrected page title/description (no longer claims "ใช้ได้ทุกบทเรียน"), and a per-row "ย้ายขอบเขต" dialog calling `moveDocumentScope`.

**Bug 2 (AdminSessionProvider single-company auto-select) — confirmed by code trace + live data, not by an automated test (none exists for this component):**

- Root cause confirmed by reading `CompanySwitcher.tsx`: when `companies.length <= 1` it renders only `<span>บริษัท: {name}</span>` — literally no control to pick a company, so an owner in that state (blocked by `AdminGuard`'s "เลือกบริษัทก่อนเริ่มทำงาน" screen, which requires `activeCompanyId`) had no way forward at all.
- The fix (`AdminSessionProvider.tsx`, new effect) traced render-cycle by render-cycle: only fires for `user?.role === "owner"`; only acts when nothing is resolved yet (`companyFromUrl ?? user?.companyId ?? getActiveCompanyId()` is falsy) **and** `companies.length === 1`; sets `?company=<id>` via `router.replace` and stops re-firing on the next render because `resolved` becomes truthy. No redirect loop, no interference with the existing owner-with-URL effect (guarded by `!companyFromUrl`, which becomes false once this effect's replace lands), no effect on non-owner roles (role-gated) or owners with >1 company (`companies.length !== 1` guard) — confirmed by reading every line of both effects and their interaction, not just the new one in isolation.
- **Live confirmation against the real running app** (not just reading code): logged in via `curl -X POST /api/auth/login` with the given credentials (`owner@local.test`) against the actual running backend (`dotnet run`, real Postgres) — response confirms `role: "owner"`, `companyId: null`. Called `GET /api/companies` (the endpoint `listSwitchableCompanies()` hits) with the resulting token — confirms exactly **one** company (`company-test`) is returned, i.e. the live data genuinely matches the single-company scenario this fix targets, not a hypothetical.
- `frontend/src/app/admin/layout.tsx` (unchanged, confirmed by direct read) wraps every `/admin/*` route in `AdminSessionProvider → TooltipProvider → AdminGuard`, so the fix applies uniformly across the whole admin app (users, links, lessons, documents, etc.), not just knowledge-base screens — this is the cross-module regression check the user asked for. `AdminGuard.tsx` and `CompanySwitcher.tsx` (both read in full, both unchanged) confirm no other code path depends on the old stuck behaviour.
- **What this round could not do**: no browser/computer-use tool was available in this session's toolset to literally click through the owner-login flow in a live browser. Compensating evidence used instead: the full effect-timing trace above, the live backend data confirmation above, and the user's own reported manual browser test (login → auto-redirect to `?company=company-test` → dashboard, confirmed by their own observation, not just an engineer's report). This is recorded explicitly rather than silently treated as equivalent to a browser click-test — see `## Unverified Behaviour` below.

**Bug 3 (Select controlled-value fix) — confirmed by direct diff read in both files:**

- `DocumentUploadList.tsx`'s `CompanyOrCategoryScopeFields` (line 93) and `KnowledgeQnAAnswerDialog.tsx` (line 145): both now read `value={scopeId ?? ""}` instead of `value={scopeId}`, so the Base UI `Select` is controlled from the very first render instead of transitioning from `undefined` to a real string once a category is picked (the exact shape of the uncontrolled→controlled warning/two-click bug). `frontend/src/components/ui/select.tsx` itself (the shared shadcn wrapper) is untouched — confirmed by direct read — so the fix is correctly scoped to the two call sites, not a primitive-level workaround.
- **Phase 6 regression check on `KnowledgeQnAAnswerDialog.tsx`** (explicitly requested, since this file already passed Phase 6 QA before today): read the entire file top to bottom. The one-line diff is the *only* change — scope prefill logic (`useEffect` on `open`/`primaryItem?.id`), `handleScopeTypeChange`, `handleSave`'s payload, the `canSave` guard, and the RadioGroup's three options are all byte-identical to what Round 1 verified for Phase 6. No regression to the Q&A answer flow.

**Automated checks — all re-run independently this round:**

- Backend: `dotnet build SupportRoom.slnx` → **0 Warning(s), 0 Error(s)**.
- Backend: `dotnet test SupportRoom.slnx --filter "Category!=Integration"` → **204/204 passed** (21 `SupportRoom.Providers.Tests` + 182 `SupportRoom.Application.Tests` + 1 `SupportRoom.Api.IntegrationTests`) — 14 more than Round 2's 190, exactly the new DS-12 tests (8 `UploadAsync` cases + 6 `MoveScopeAsync` cases), confirmed by an independent run.
- Backend: `dotnet ef migrations has-pending-model-changes` → **"No changes have been made to the model since the last migration."** — confirms DS-11 (no schema change in Phase 7).
- Frontend (Node 22 via nvm): `npm run typecheck` → clean. `npm run lint` → clean. `npm run test` (Vitest) → **36/36 passed**, unchanged from Round 2 (Phase 7's `plan.md` has no `[frontend]` test task; DS-12's 3 test tasks are all `[backend]`). `npm run build` → succeeds, same 21 admin/public routes compile (route count unchanged; `/admin/documents` grew in size, not in route count).
- **Live app tests against the real running backend** (`dotnet run --project src/SupportRoom.Api`, real Postgres via `supportroom-pg`): login, `GET /api/companies`, and all 6 DS-3 rejection cases (see above) hit directly with `curl` and an authenticated JWT, independent of both the unit tests and the frontend.

**File manifest — blast radius confirmed exactly as expected, nothing else in the codebase moved:**

Content-based stat/line comparison (not mtime, which is unreliable here) of all 75 files in the previous manifest (`review/phase-1-6.md`) found **exactly 10 changed**: `IDocumentResourceService.cs`, `DocumentsController.cs`, `DocumentResourceServiceTests.cs`, `Fakes/ServiceTestFakes.cs`, `domain.ts`, `api-client.ts`, `DocumentUploadList.tsx`, `lessons/new/page.tsx`, `KnowledgeQnAAnswerDialog.tsx`, `lessons/[slug]/page.tsx` — precisely the set Phase 7 + the two bugs should touch, no more, no less. The other 65 files, including the entire shared-code watchlist (`ApplicationDbContext.cs`, `IAuthorizationGuard.cs`, `IKnowledgeCategoryService.cs`, `IKnowledgeNamespaceResolver.cs`, `IBackgroundJobProcessor.cs` beyond the earlier Round 2 fix), are byte/line-identical to Round 2's recorded state. `AdminSessionProvider.tsx` was not in the prior manifest (never directly inspected in an earlier round) and is added here as newly-inspected shared code. `Glob`/`find` swept `backend/src`, `backend/tests`, `frontend/src` for anything not already in the manifest; the only genuinely new files are `MoveDocumentScopeDto.cs` (new DTO) and the frontend/backend files already listed above — no stray file was created outside the phase's declared scope.

**Data Model contract check — confirmed no change.** Phase 7's own contract (`design.md` DS-11) states no migration and no new/changed fields; `dotnet ef migrations has-pending-model-changes` and the migrations-directory listing both confirm this directly rather than taking the contract's word for it. `DocumentResourceViewModel` (the one entity-adjacent file Phase 7 could plausibly have touched) is unchanged — confirmed by the manifest diff (1405 bytes/26 lines, identical to Round 1's recording). No model in this module's `design.md` is missing from the codebase and no untracked model exists — same conclusion as Round 1/2, unchanged by this round.

## Verified File Manifest — knowledge-base (Phase 1–7)

Current as of this round (Round 3, FULL). Supersedes the manifest in `review/phase-1-6.md`. The 10 rows marked `[R3]` changed this round (new bytes/lines shown); everything else is unchanged since Round 2 (re-confirmed, not just carried over). Rows marked `[R3, new to manifest]` were not inspected in any earlier round.

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
| backend/src/SupportRoom.Providers.Data/Repository/IDocumentResourceRepository.cs | 1443 | 30 |
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
| backend/src/SupportRoom.Application/Services/IBackgroundJobProcessor.cs | 24240 | 500 |
| backend/src/SupportRoom.Application/Services/IDocumentResourceService.cs `[R3]` | 18398 | 374 |
| backend/src/SupportRoom.Application/Services/VectorDeleteJobPayload.cs `[R3, new to manifest]` | 1723 | 30 |
| backend/src/SupportRoom.Application/Services/ILessonSlideNarrationService.cs | 8380 | 191 |
| backend/src/SupportRoom.Application/Services/ILessonSlideNarrationResolver.cs | 2025 | 46 |
| backend/src/SupportRoom.Application/Services/ILessonConfigService.cs | 23873 | 487 |
| backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs | 8569 | 171 |
| backend/src/SupportRoom.Application/Services/IKnowledgeQnAService.cs | 11818 | 276 |
| backend/src/SupportRoom.Application/Services/IKnowledgeQnAConflictService.cs | 1951 | 46 |
| backend/src/SupportRoom.Application/Services/DocumentChunkTextAnalyzer.cs | 1249 | 33 |
| backend/src/SupportRoom.Application/Common/IAuthorizationGuard.cs | 4565 | 115 |
| backend/src/SupportRoom.Application/Exceptions/GeneralException.cs `[R3, new to manifest]` | 2000 | 38 |
| backend/src/SupportRoom.Application/Dto/KnowledgeCategoryDto.cs | 546 | 20 |
| backend/src/SupportRoom.Application/Dto/KnowledgeQnADto.cs | 1050 | 34 |
| backend/src/SupportRoom.Application/Dto/LessonSlideNarrationDto.cs | 544 | 13 |
| backend/src/SupportRoom.Application/Dto/MoveDocumentScopeDto.cs `[R3, new to manifest]` | 439 | 10 |
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
| backend/src/SupportRoom.Api/Controllers/DocumentsController.cs `[R3]` | 4150 | 113 |
| backend/src/SupportRoom.Api/Controllers/LessonController.cs | 5335 | 119 |
| backend/src/SupportRoom.Api/Controllers/KnowledgeQnAController.cs | 1059 | 31 |
| backend/src/SupportRoom.Api/Controllers/QnaQueueController.cs | 744 | 20 |
| backend/src/SupportRoom.Api/Controllers/KnowledgeQnAConflictsController.cs | 1163 | 26 |
| backend/src/SupportRoom.Api/BackgroundJobHostedService.cs | 3118 | 76 |
| backend/src/SupportRoom.Api/Configurations/AuthenticationConfiguration.cs | 4053 | 81 |
| backend/src/SupportRoom.Api/Configurations/ServiceConfiguration.cs | 5886 | 98 |
| backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs | 11485 | 252 |
| backend/tests/SupportRoom.Application.Tests/DocumentResourceServiceTests.cs `[R3]` | 18300 | 460 |
| backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs `[R3]` | 24163 | 450 |
| frontend/docs/API_CONTRACT.md `[R3, new to manifest]` | 8276 | 132 |
| frontend/src/types/domain.ts `[R3]` | 20083 | 550 |
| frontend/src/lib/api-client.ts `[R3]` | 24860 | 622 |
| frontend/src/app/admin/categories/page.tsx | 4019 | 103 |
| frontend/src/components/admin/CategoryTree.tsx | 5428 | 142 |
| frontend/src/components/admin/CategoryMovePreviewDialog.tsx | 4921 | 129 |
| frontend/src/components/admin/DocumentUploadList.tsx `[R3]` | 18546 | 422 |
| frontend/src/components/admin/DeletedDocumentsList.tsx | 4274 | 101 |
| frontend/src/app/admin/documents/[id]/chunks/page.tsx | 5339 | 103 |
| frontend/src/app/admin/documents/page.tsx `[R3, new to manifest]` | 1747 | 34 |
| frontend/src/app/admin/lessons/new/page.tsx `[R3]` | 11489 | 277 |
| frontend/src/app/admin/lessons/[slug]/narrations/page.tsx | 7654 | 164 |
| frontend/src/app/admin/qna-queue/page.tsx | 6112 | 137 |
| frontend/src/components/admin/KnowledgeQnAAnswerDialog.tsx `[R3]` | 6898 | 181 |
| frontend/src/app/admin/qna-conflicts/page.tsx | 4406 | 103 |
| frontend/src/app/admin/lessons/[slug]/page.tsx `[R3]` | 25180 | 564 |
| frontend/src/components/admin/AdminSessionProvider.tsx `[R3, new to manifest]` | 5979 | 159 |
| frontend/src/components/admin/AdminGuard.tsx `[R3, new to manifest]` | 3956 | 102 |
| frontend/src/components/admin/CompanySwitcher.tsx `[R3, new to manifest]` | 1614 | 41 |
| frontend/src/components/admin/AdminLink.tsx `[R3, new to manifest]` | 1147 | 26 |
| frontend/src/app/admin/layout.tsx `[R3, new to manifest]` | 1004 | 24 |
| frontend/src/components/ui/select.tsx `[R3, new to manifest]` | 6655 | 201 |

## Per-Task Results — Phase 7 (this round)

**Phase 7 (Module G, 🔒 gate)** — 22/22 ✅ Verified (all 14 `[backend]` + all 8 `[frontend]` tasks) — see `## Verification Summary` above for the per-task detail, each checked against `design.md`'s DS-1..DS-12 and confirmed by direct code read, real unit tests, and (for the security-relevant rejection paths) live testing against the real running app.

**Phase 1 (Module A) — the previously ⚠️ Partial task now closes**: "เพิ่ม validation TX-4/TX-5 สำหรับ `LessonConfig.CategoryId` และ `DocumentResource`" is now ✅ **Verified** — TX-5's code (`EnsureValidScope`'s category-Level-2 check) always existed and was correct, but was unreachable until Phase 7 wired the call site (`UploadAsync`/`MoveScopeAsync`). Confirmed reachable and correct both by the DS-12 unit tests and by a live `curl` test rejecting a real Level-1 category id with 400 "ต้องเลือกหมวดย่อย (ชั้นที่ 2) เท่านั้น". Phase 1 is now **15/15 ✅ Verified**, zero open items.

**Bug 2 and Bug 3** — see `## Verification Summary` above; both ✅ Verified, first confirmation.

## Design/requirement contract checks — Phase 7

Field-by-field: Phase 7 declares **no** new/changed fields (DS-11) — confirmed by `dotnet ef migrations has-pending-model-changes` (clean) and by the migrations directory (no file newer than Phase 6's `AddKnowledgeQnA`). `MoveDocumentScopeDto` and `VectorDeleteJobPayload.Kind = Document` are DTO/payload-shape additions, not schema changes — `design.md` explicitly allows `PayloadJson` to be free-form (DM-10). No model in this module's `design.md` is missing from the codebase; no untracked model exists in the codebase that this module doesn't declare. `requirement.md`'s R3 ("เอกสารที่วางระดับหมวดต้องตอบได้ทุกบทเรียนในหมวดนั้น") is now genuinely satisfiable end-to-end for the first time — confirmed by the live category-scope rejection/acceptance tests above, not just by the presence of the field.

## Unverified Behaviour — undeployed phases

This project has a real test suite (204 backend + 36 frontend tests as of this round), so this section stays scoped to rules a passing suite cannot itself exercise — not a blanket "no tests" disclaimer. Phases 2, 3, 4, 6's blocks are unchanged by this round (this round didn't touch their code, confirmed by the manifest diff) and stay here per convention until each phase is actually deployed.

### Phase 2 (Module B)
- KS-7 ("when the two prompt blocks conflict, the model must yield to block 1") and KS-8 (ban on copying Q&A text verbatim) — the prompt text sent to the model is verified correct by inspection (`RagVoiceQuestionProvider.BuildAnswerPrompt`), but whether the model actually obeys these instructions in a live call is not, and cannot be, exercised by an automated test.

### Phase 3 (Module C)
- MG-A3/MG-A4's default-chain backfill correctness (exactly 2 flagged rows per company, chain linkage, `LessonConfig.CategoryId` pointing at the leaf) was verified by the implementer's manual `psql` rehearsal against an isolated PostgreSQL database (recorded in `status.md`), not by an automated test — EF Core's InMemory provider cannot execute the raw-SQL backfill.
- `IBackgroundJobRepository.ClaimNext`'s `FOR UPDATE SKIP LOCKED` concurrency behaviour was verified manually against a real PostgreSQL instance (per `status.md`), not by an automated test, for the same reason.

### Phase 6 (Module F)
- KS-9/R5.5 ("the model reports a conflict, and it is a genuine one") — the code path that records a reported conflict is unit-tested for its own logic (validation, try/catch isolation), but whether the model's own judgment of "conflicting" is sound is a prompt/model-quality question `requirement.md` itself says is out of scope for code to guarantee (R5.5's stated limitation).

### Phase 7 (Module G)
- **DS-3's cross-company category-id rejection**, specifically: proven correct at the architecture level (the unchanged, already-verified `KnowledgeCategory.HasQueryFilter` scopes `Get(id)` to the caller's company) and proven live with a globally-nonexistent id (which behaves identically to a cross-company id under that filter, since both simply fail to resolve inside the caller's scoped query) — but not proven with a genuine second company's real category id, because the local dev seed data has exactly one company. The DS-12 unit tests for this case use `FakeKnowledgeCategoryRepository`, which is a flat in-memory list with no `CompanyId` filtering at all, so those specific tests prove "doesn't exist"/"is Level 1" but not "belongs to another company" as a distinct case. See the Open Issues row above — not blocking, since the runtime protection is the same unchanged mechanism already verified correct in the Phase 1 FULL round.
- **Bug 2's owner-login auto-redirect**, specifically the client-side `router.replace` effect actually firing and landing the browser on the dashboard: verified by a full render-cycle code trace and by confirming live (via the real backend) that the exact single-company data condition this fix targets is true for the given test account — but not by literally driving a browser this round (no browser/computer-use tool was available in this session's toolset). The user's own reported manual browser test (watched it happen) is the closest thing to a browser-level confirmation on record; this round adds the code-correctness and live-data layers underneath it, not a duplicate click-test.

## Issues Found — Phase 7

None. All 22 Phase 7 tasks, both bug fixes, and the now-closed Phase 1 TX-5 item are ✅ Verified. The one non-blocking observation (DS-3's fake-repository test-coverage gap for the cross-company case) is recorded in `## Open Issues — all phases` as an optional hardening suggestion, not a defect requiring a fix-and-recheck cycle.

## Review Outcome — Phase 7

**Accepted.** This was a FULL round (first-ever for Phase 7) and every item in scope — all 22 `plan.md` tasks, Bug 2, Bug 3, and the now-reachable Phase 1 TX-5 item — came back ✅ Verified, with real evidence (direct code read, 14 new real unit tests, an independent re-run of the whole automated-check suite on both sides, and live testing against the actual running application for the security-relevant rejection paths and the owner-login data condition). Per `.claude/shared/conventions.md` §6, a FULL round with every task ✅ Verified is eligible for the autonomous no-pause exception — but this session's own agent instructions are explicit that manual mode always asks even on an all-✅ FULL round, and nothing here indicated an explicit "run this whole pipeline unattended" request, so this is presented to the user as a normal accept/reject decision rather than self-accepted.

- **Phase 7 is now eligible for `devops`** on round-mode grounds (this was FULL) — but, like every other gated phase in this module, it still carries its own `🔒 Security gate` and `security` has not audited any phase in this module yet. `devops` cannot ship it until that runs.
- **Phase 1 is now 15/15 ✅ Verified, zero open items** — eligible for `devops` (Round 1 was already FULL; Phase 1 carries no gate).
- **Phases 2, 3, 4, 5, 6 are unchanged by this round** (confirmed via the manifest diff — none of their code was touched) — their status stands exactly as `review/phase-1-6.md` left it: Phase 2 still has the non-blocking R-2 latency item; Phase 3 is 21/21 ✅ but its last round was TARGETED, so it still needs a FULL round before `devops` eligibility on mode grounds (independent of the `security` blocker); Phases 4, 5, 6 remain fully ✅ Verified from their FULL Round 1.
- **Module-wide**: 139/140 `plan.md` tasks now `[x]` across all 7 phases (118 from Phases 1–6 + 22 from Phase 7); the one remaining unchecked task is the Phase 2 R-2 latency measurement, which stays unchecked because it's legitimately blocked on a real deployment with traffic, not because anything failed verification — zero ❌/⚠️ items anywhere in the module. The only thing blocking every gated phase from `devops` is the still-unrun `security` audit.

## Archived rounds

- Phases 1–6 (knowledge-base) — Round 1 (FULL) + Round 2 (TARGETED), closed by Phase 7's round becoming current → `review/phase-1-6.md`
- Phase 3 (knowledge-base) — Round 1 (FULL) findings on the cross-company leak, superseded by Round 2's (TARGETED) closure → `review/phase-3.md`

## Change Log

- 2026-08-20 — First-ever QA round for this module, **FULL**, covering all 6 phases (118 tasks) from scratch. Found a cross-company data leak/IDOR (Phase 3) and a half-built requirement (R3 document-category-scope). Sent back to the user per the hard-stop rule. *(Full entry archived to `review/phase-1-6.md`.)*
- 2026-08-20 — **TARGETED** re-check (Round 2) of the Phase 3 cross-company leak/IDOR fix. Confirmed genuinely closed by direct code read, a new real-`ApplicationDbContext` regression test, and independent re-runs of build/test/typecheck/lint/build. Phase 3 now 21/21 ✅ Verified. *(Full entry archived to `review/phase-1-6.md`.)*
- 2026-08-20 — **FULL** round (Round 3) on **Phase 7** (Document scope assignment — R3 write path, Module G, 🔒 gate), the module's newest phase, plus first-time confirmation of two bugs found and fixed during today's manual testing: an owner-with-one-company login dead end (`AdminSessionProvider.tsx`) and a Select-needs-two-clicks bug (`DocumentUploadList.tsx` + `KnowledgeQnAAnswerDialog.tsx`, the latter a Phase 6 regression check). All 22 `plan.md` Phase 7 tasks ✅ Verified by direct code read, 14 new real unit tests (204/204 backend, up from 190), and live testing against the actual running application (login, `GET /api/companies`, all 6 DS-3 rejection cases hit directly via `curl` with a real JWT). The previously ⚠️ Partial Phase 1 TX-5 item closes now that Phase 7 wired its only call site — Phase 1 is 15/15 ✅. Backend: build 0/0, test 204/204, `has-pending-model-changes` clean (confirms DS-11's no-migration claim). Frontend: typecheck/lint clean, test 36/36 (unchanged — no new frontend test task in Phase 7), build clean, 21 routes. File-manifest diff (content-based, not mtime) confirmed exactly the expected 10 files changed plus `MoveDocumentScopeDto.cs` newly created — nothing else in the codebase moved. Archived Round 1/2's Phase 1–6 detail to `review/phase-1-6.md`, carrying the R3 issue's closure note forward and keeping Phases 2/3/6's `Unverified Behaviour` blocks in this file since none of the six phases have deployed yet. Presented to the user as a plain accept/reject decision (all-✅ FULL round, manual mode) — see `## Review Outcome` above.
