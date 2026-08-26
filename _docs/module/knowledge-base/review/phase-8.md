# Phase 8: Knowledge library view (R7) — extends `/admin/documents` 🔒 Security gate

## Verification Summary (Round 6)

**Round 6 — Mode: FULL (Phase 8, first documented QA round for this phase).** All 37 Phase 8 checkboxes were inspected from code, contracts, migrations, and tests. Result: **36/37 checked, 1/37 Partial, 0 Failed, 0 Critical**. The 36 boxes that appeared before Phase 10 recorded its checkbox-integrity anomaly were set during this authorized qa-engineer run; this round now supplies the missing manifest, evidence, commands, findings, and Change Log entry. The remaining unchecked item is the shared /admin/documents layout/filter task because the R7.3 status selector is absent.

Security-focused result:

- **Company isolation:** DocumentResource and KnowledgeQnA both have CompanyId + !IsDelete global query filters in ApplicationDbContext.cs:115-122 and :169-173. Their Phase 8 repositories implement GetAllInCompany as FindBy(_ => true) at IDocumentResourceRepository.cs:36 and IKnowledgeQnARepository.cs:30. Neither method uses IgnoreQueryFilters. Repository-wide review found IgnoreQueryFilters only in unrelated or explicitly scoped paths, including deleted-document recovery with an explicit CompanyId predicate; none is present in the Phase 8 active list/search path.
- **SQL injection:** the actual document content search uses captured pattern + EF.Functions.ILike at IDocumentResourceService.cs:213-220; Q&A search uses the same parameterized EF translation at IKnowledgeQnAService.cs:133-138. No FromSqlRaw or string-built SQL exists in either path. No Critical leak or injection finding was found.
- **Residual proof gap:** design.md R-16 explicitly requires two companies to be seeded and both document/Q&A list visibility to be asserted. The runtime implementation is correct by direct inspection, but that exact regression proof is absent; see P8-02.

Checks executed:

- Frontend npm run lint — passed.
- Frontend npm run typecheck — passed.
- Frontend npm run test — **69/69 passed across 9 files**.
- Frontend npm run build — passed; Next.js 15.5.22 built 19 static pages.
- Backend dotnet build SupportRoom.slnx -c Release — passed, **0 errors, 2 existing CA1416 warnings**. The first Debug attempt was blocked by an already-running SupportRoom.Api process holding Debug DLLs; Release compilation proved the source clean.
- Backend dotnet test SupportRoom.slnx -c Release --no-restore --filter "Category!=Integration" — **269/269 passed**: Application 218, Providers 41, Api Integration 10.
- Backend dotnet ef migrations has-pending-model-changes --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api --configuration Release --no-build — **No changes have been made to the model since the last migration.**
- Browser smoke was attempted against localhost, but the in-app browser harness could not initialize its trusted RPC dependency. No visual/manual browser claim is made; this limitation does not replace the code, component-test, build, and contract evidence above.

## Verified File Manifest — knowledge-base (Phase 8, Round 6)

| File | Bytes | Lines | Review scope |
|---|---:|---:|---|
| backend/src/SupportRoom.Domain/Entities/DocumentResource.cs | 2,311 | 45 | Full |
| backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs | 10,704 | 194 | Full; filters and index |
| backend/src/SupportRoom.Providers.Data/Migrations/20260825121033_AddDocumentContentHash.cs | 1,174 | 37 | Full |
| backend/src/SupportRoom.Providers.Data/Migrations/20260825121033_AddDocumentContentHash.Designer.cs | 34,033 | 957 | Generated model checked |
| backend/src/SupportRoom.Providers.Data/Migrations/ApplicationDbContextModelSnapshot.cs | 33,919 | 954 | Model parity checked |
| backend/src/SupportRoom.Providers.Data/Repository/IDocumentResourceRepository.cs | 2,031 | 40 | Full |
| backend/src/SupportRoom.Providers.Data/Repository/IKnowledgeQnARepository.cs | 1,413 | 31 | Full |
| backend/src/SupportRoom.Application/Common/KnowledgeLibrarySearch.cs | 644 | 22 | Full |
| backend/src/SupportRoom.Application/Services/IDocumentResourceService.cs | 24,926 | 493 | Full |
| backend/src/SupportRoom.Application/Services/IKnowledgeQnAService.cs | 17,113 | 370 | Full |
| backend/src/SupportRoom.Application/Dto/UploadDocumentDto.cs | 992 | 20 | Full |
| backend/src/SupportRoom.Application/Dto/KnowledgeQnADto.cs | 1,903 | 51 | Full |
| backend/src/SupportRoom.Application/ViewModel/DocumentResourceViewModel.cs | 2,241 | 44 | Full |
| backend/src/SupportRoom.Application/ViewModel/KnowledgeQnAViewModel.cs | 2,781 | 62 | Full |
| backend/src/SupportRoom.Api/Controllers/DocumentsController.cs | 5,678 | 134 | Full |
| backend/src/SupportRoom.Api/Controllers/KnowledgeQnAController.cs | 1,428 | 38 | Full |
| backend/tests/SupportRoom.Application.Tests/DocumentResourceServiceTests.cs | 24,591 | 587 | Full |
| backend/tests/SupportRoom.Application.Tests/KnowledgeQnAServiceTests.cs | 13,300 | 326 | Full |
| backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs | 12,253 | 275 | Full |
| backend/tests/SupportRoom.Application.Tests/Fakes/ServiceTestFakes.cs | 24,554 | 451 | Relevant repositories/full isolation behavior |
| frontend/src/app/admin/documents/page.tsx | 4,736 | 107 | Full |
| frontend/src/app/admin/qna-queue/page.tsx | 6,199 | 139 | Full |
| frontend/src/components/admin/DocumentLibraryFilterBar.tsx | 6,445 | 146 | Full |
| frontend/src/components/admin/DocumentUploadList.tsx | 21,606 | 476 | Full |
| frontend/src/components/admin/DocumentDuplicateDialog.tsx | 4,533 | 92 | Full |
| frontend/src/components/admin/KnowledgeQnATable.tsx | 9,607 | 215 | Full |
| frontend/src/components/admin/KnowledgeQnAAnswerDialog.tsx | 15,703 | 359 | Full |
| frontend/src/lib/api-client.ts | 31,409 | 732 | Relevant API surface and callers |
| frontend/src/types/domain.ts | 25,431 | 639 | Relevant Phase 8 types |
| frontend/src/types/api.ts | 1,009 | 35 | ApiErrorCode parity |
| frontend/docs/API_CONTRACT.md | 17,064 | 225 | Relevant wire-contract sections |

Also inspected repository-wide IgnoreQueryFilters, EF.Functions.ILike, FromSqlRaw, migration filenames, call sites, API error mapping, and the Phase 8 contract sections in requirement.md, design.md, and plan.md.

## Per-Task Results — Phase 8 (Round 6)

Phase 8 has 37 real checkbox tasks: 22 backend, 14 frontend, and one backend-owned API-contract document task.

1. ✅ DocumentResource.ContentHash is nullable and init-only at DocumentResource.cs:44; the null semantics are documented.
2. ✅ ApplicationDbContext creates the non-unique CompanyId/ContentHash index at :121; no IsUnique configuration exists.
3. ✅ MG-H1 adds only nullable ContentHash plus the index; Down drops the index and column. No UPDATE/backfill/Pinecone operation exists, and model parity is clean.
4. ✅ UploadAsync hashes input.Content once before duplicate checking, DB insertion, storage, or queueing at IDocumentResourceService.cs:84-88, stores it at :128, and uses lowercase SHA-256 hex at :251.
5. ✅ Document GetAllInCompany is FindBy(_ => true), with no IgnoreQueryFilters, at IDocumentResourceRepository.cs:36.
6. ✅ GetByScope(null, null) reaches GetAllInCompany at IDocumentResourceService.cs:189; explicit scopes retain their filter path.
7. ✅ Q&A GetAllInCompany mirrors the document repository at IKnowledgeQnARepository.cs:30.
8. ✅ KnowledgeQnAService.GetAll accepts scopeType/scopeId/status/q and applies the shared rules without adding fields.
9. ✅ GET /api/knowledge-qna exists at KnowledgeQnAController.cs:17-21. GetAll calls EnsureAuthenticated at IKnowledgeQnAService.cs:124, uses the existing role guard, and sorts newest-first.
10. ✅ Category filtering expands to category-scoped rows plus lesson-scoped rows whose LessonConfig.CategoryId matches, for both documents and Q&A.
11. ✅ KL-11 uses parameterized EF.Functions.ILike in both real service paths; document results deduplicate by document id. No raw SQL exists in these paths.
12. ✅ KnowledgeLibrarySearch trims q, treats blank or under two characters as no search, and filter/search composition is AND with no pagination.
13. ✅ KL-19 compares non-null hashes inside the materialized same-company active list and keeps CompanyId in the predicate.
14. ✅ KL-20 compares trimmed filenames with OrdinalIgnoreCase in memory and reports hash/name matches separately.
15. ✅ UploadDocumentDto.CheckDuplicate is additive and defaults false.
16. ✅ CheckDuplicate=true performs the duplicate gate before writes and returns the existing CONFLICT envelope with DuplicateDocumentDto details; false preserves upload behavior while still storing ContentHash.
17. ✅ KL-23 performs the normalized-question duplicate gate before Add/source/queue/commit, supports ConfirmDuplicate, and returns DuplicateQnAResponse.
18. ✅ Duplicate DTOs and KnowledgeQnAFilter match the wire contract; ContentHash does not appear in DocumentResourceViewModel or any response.
19. ✅ Document duplicate tests include identical bytes across two companies at DocumentResourceServiceTests.cs:486-505.
20. ✅ Document duplicate tests cover name+content, content-only, name-only, and neither at :507-565.
21. ✅ Null ContentHash non-duplication is tested at :567-587.
22. ✅ KL-23 tests cover normalization/409, cross-company non-match, zero writes/jobs on conflict, and ConfirmDuplicate override at KnowledgeQnAServiceTests.cs:226-325.
23. ✅ /admin/documents derives its initial scopeType/scopeId from URL parameters and defaults unknown/missing input to all scopes at page.tsx:18-36.
24. ⚠️ **Partial.** The page has one shared bar and two tables at page.tsx:81-99, but R7.3 status filtering is absent: DocumentLibraryFilterBar.tsx:81-146 renders only scope and search; DocumentUploadList.tsx:194-197 and KnowledgeQnATable.tsx:59-62 omit filter.status from reload dependencies. See P8-01.
25. ✅ The shared scope select includes the fourth lesson option populated from listLessons at DocumentLibraryFilterBar.tsx:115-122.
26. ✅ Shared scopeLabel resolves real lesson/category names and explicit deleted-item labels without exposing raw ids.
27. ✅ The KL-7 slide-source badge is derived from lessons/PdfDocumentResourceId in the library list; no fixedScope/primaryDocumentId dependency remains.
28. ✅ Search q is shared by both tables, debounced at page.tsx:50-61, documents remain filename-searchable when indexing failed, and the UI states the two-character/indexing limitation.
29. ✅ KnowledgeQnATable is rendered below the document table at page.tsx:91-99 and exposes Question, Answer, scope, status, edit, and delete actions.
30. ✅ Q&A edit reuses KnowledgeQnAAnswerDialog in edit mode and calls updateKnowledgeQnA.
31. ✅ Q&A delete uses an explicit dialog with both required side-effect messages; success exposes the /admin/qna-queue link.
32. ✅ The Q&A duplicate 409 flow reads ApiClientError.response.error.details, keeps a list, offers confirmDuplicate=true, per-row in-place edit, cancel, and the required "not saved / queue remains" messaging; no forbidden /admin/documents?q= deep link exists.
33. ✅ Only the library upload submits checkDuplicate=true; the PDF lesson upload path remains false/default.
34. ✅ DocumentDuplicateDialog distinguishes hash/name/both, renders file/scope/date, offers upload-anyway and cancel, and explains the pre-MG-H1 limitation.
35. ✅ Frontend domain/API types contain checkDuplicate, confirmDuplicate, DuplicateDocumentDto, DuplicateQnAResponse, KnowledgeQnAFilter, and CONFLICT parity.
36. ✅ api-client list/upload/Q&A methods serialize scopeType/scopeId/status/q and preserve the shared ApiClientError 409 envelope.
37. ✅ frontend/docs/API_CONTRACT.md documents the all-scope GET /api/documents behavior, GET /api/knowledge-qna, checkDuplicate, and 409 payloads.

## Design and requirement contract checks (Round 6)

- **R7.1/R7.2/R7.4/R7.5/R7.6:** implemented and evidenced above.
- **R7.3:** scope/category filtering works, including KL-5 category expansion, but the requested status filter is absent from the UI; therefore Phase 8 is Partial.
- **KL-1..KL-26:** all code paths pass except the status-control portion of the shared filter layout. KL-25 remains respected: /admin/documents does not create Q&A.
- **DM-3/DM-15/MG-H1:** ContentHash is string?, init-only; the index is non-unique; migration is additive and reversible; existing rows are not backfilled; no pending model change exists.
- **Wire contract:** ContentHash remains private, checkDuplicate and confirmDuplicate match both sides, and the only Phase 8 endpoint addition is GET /api/knowledge-qna as designed.
- **Security gate:** functional QA does not close the gate. No leak/injection was found, but P8-02 and the standing security audit remain open.

## Issues Found — Phase 8 (Round 6)

### P8-01 — Important — shared status filter missing

Requirement R7.3 explicitly includes indexing status. Although KnowledgeQnAFilter.status and api-client serialization already exist, DocumentLibraryFilterBar has no status Select, and both list effects ignore filter.status. The smallest fix is to add one shared status selector to DocumentLibraryFilterBar, preserve the existing scope/search state when changing it, add filter.status to both useEffect dependency lists, and cover both tables with a frontend test.

### P8-02 — Important — R-16 two-company list regression tests missing

The production repositories and EF model are correctly isolated, so this is not a demonstrated data leak and is not Critical. However, design.md R-16 requires a two-company proof for both document and Q&A lists. Add real-ApplicationDbContext/repository tests that seed active DocumentResource and KnowledgeQnA rows for two companies, resolve Company A, exercise both GetAllInCompany/list-service paths, and assert that no Company B row is returned. A query-translation assertion for KL-11 parameterization would be useful hardening but is not needed to reproduce the missing R-16 proof.

## Review Outcome — Phase 8 (Round 6)

**⚠️ Partial — 36/37 checked.** Two Important findings remain open; there are no Critical findings and no failed build/test/migration checks. Phase 8 is not accepted and cannot reach devops. Its Security gate also remains independently open.

Per the user's instruction, this result is **recorded only**. No next role was invoked or dispatched. Fix routing is documented above solely for ownership; work remains parked until the user explicitly decides to continue.

**Superseded by Round 7 (TARGETED, 2026-08-26)** — P8-01 and P8-02 were confirmed fixed, KL-1's layout checkbox was ticked, and Phase 8 reached 37/37 `[x]`. Round 7 was TARGETED, not FULL, so it did not by itself make Phase 8 deploy-eligible. **Round 7's full verbatim content (it covered Phase 8's P8-01/P8-02 fixes and Phase 10's NR-13 fix together, in one pass) is archived in `review/phase-10.md`'s `## Round 7` section** — not duplicated here to avoid two copies of the same text; that section documents the P8-01/P8-02 evidence, blast radius, and shared-code watchlist findings in full.

**Superseded again by Round 8 (FULL, 2026-08-26)** — Round 8 re-inspected all 37 Phase 8 tasks from scratch (not relying on Round 6/7's write-ups), confirmed every one ✅ Verified by direct code read, and closed the phase with a genuine FULL round. See `review.md`'s current round for the full Round 8 record. Phase 8 is now deploy-eligible on QA-mode grounds; only the standing, never-audited `🔒 Security gate` remains open.
