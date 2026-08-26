# Knowledge Base & Teaching Content Intake — Verification & Review

## Open Issues — all phases

| Issue | Phase | Routes to | Blocking | Rounds |
|---|---|---|---|---|
| R-2 latency measurement (3-namespace query) still has no deployed traffic to measure. This is not a code defect. | Phase 2 (review/phase-1-6.md) | devops, after deployment | No | 0 |
| No security audit has run on this module. Phases 2, 3, 4, 6, 7, 8, 10, 11, and 12 carry the Security gate and cannot reach devops until security runs. Phase 11 closed its QA round in Round 12 — the Security gate is now its only remaining blocker. | Phases 2, 3, 4, 6, 7, 8, 10, 11, 12 | security | Yes, for gated phases | 0 |
| DS-3 cross-company category-id rejection is protected by the real KnowledgeCategory query filter, but still lacks a dedicated two-company category test. This remains optional hardening, not a demonstrated defect. | Phase 7 (review/phase-7.md) | backend-engineer | No | 0 |
| RenderPdfPreviewPageAsync does not wrap PdfSlidesRenderer.RenderPagePng in the Phase 10-specific 4xx conversion path. Re-confirmed present (unchanged) by direct code read in Round 8 — still an audit-time residual, not a failed Phase 10 task. | Phase 10 | security | No | 0 |
| EX-8's hard-floor exclusion count in `ToggleAsync` can transiently overcount by 1 within a single request if a *different* page of the same lesson still has an un-reconciled legacy duplicate row pair — the reconciler's hard-deletes aren't committed yet when the count query runs. Safe-direction only (over-conservative rejection near the floor, never permits going below it); self-heals the first time any endpoint touches that lesson. Optional hardening only. | Phase 11 (review/phase-11.md, Round 12) | backend-engineer | No | 0 |

## Verification Summary (current round)

**Round 12 — Mode: FULL — Phase 11 / Module K, all 37 tasks verified from scratch, closing the phase.** This is the third re-check of P11-01, whose fix architecture (a shared `LessonExcludedSlideReconciler.ReconcileAndLoad` helper called by both write paths before any per-page logic runs) the project owner approved directly, past the two-round escalation ceiling. The fix was verified genuinely, not taken on the owner's approval as a reason to relax scrutiny: traced the exact two-live-duplicate-row scenario that failed twice before through `ToggleAsync`, confirmed the reconciler's hard-deletes (`repository.Delete` → EF `_set.Remove`) commit in the same `UnitOfWork.Commit()` as the rest of the method (no premature commit), and confirmed `LessonExcludedSlideServiceTests.ToggleAsync_WithTwoLiveLegacyDuplicateRows_RestoringCollapsesThemToOneNonLiveRow` asserts the corrected end state (one surviving row, not live) rather than merely "no throw."

**Result: 37/37 ✅ Verified, 0/37 ⚠️ Partial, 0 ❌ Failed. Overall: ✅ Verified — Phase 11 closes.** All 37 `plan.md` checkboxes for Phase 11 are now `[x]`. Phase 12 (Module L) may now start per the Module K → Module L dependency.

**Automated checks run, project-wide:**

- Frontend `npm run typecheck` ✅ clean; `npm run lint` ✅ clean; `npm run test` ✅ **69/69**; `npm run build` ✅ **19/19** routes generated, no errors.
- Backend `dotnet build SupportRoom.slnx -c Release` ✅ **0 Warning(s) / 0 Error(s)**.
- Backend `dotnet test SupportRoom.slnx -c Release --no-build --filter "Category!=Integration"` ✅ **289/289** (238 Application + 41 Providers + 10 Api.IntegrationTests).
- Backend `dotnet ef migrations has-pending-model-changes ... --no-build` ✅ no pending model changes.
- No locked `SupportRoom.Api.exe` process found before building (checked via `tasklist` first, per this phase's standing note).

Full per-task results, the file manifest (including a byte-diff against the Round 10 FULL baseline and a `Glob` for new files), the design/requirement contract checks, and the complete P11-01 third-re-check writeup are archived in [`review/phase-11.md`](review/phase-11.md) under **Round 12 — FULL (closes Phase 11)**, per this module's convention of moving a closed phase's full detail out of the live file. One non-blocking observation surfaced this round (EX-8's hard-floor count can transiently overcount by 1 in a narrow, self-healing, safe-direction edge case unrelated to P11-01) — recorded in Open Issues above and in the archive, not treated as blocking closure.

## Unverified Behaviour — undeployed phases

This project has a real test suite (289 backend tests in Round 12 — 238 `Application.Tests` + 41 `Providers.Tests` + 10 `Api.IntegrationTests` — and 69 frontend Vitest tests), so this section stays scoped to rules the suite cannot itself exercise. Existing undeployed-phase notes remain below until deployment as required by conventions §4.

### Phase 11 (Module K) — closed in Round 12, still undeployed
- The frontend create-PDF orchestration still has no component/integration test. Round 10/12 confirm the P11-02 fix by tracing `touchedAndNotExcludedIds` through step-4 flush, progress totals and retry state, not by an executed browser assertion.
- `ProcessLessonIndexAsync` and `ProcessDocumentIndexAsync` implement the two-vector exclusion paths, but no automated test executes those private worker methods end to end against a knowledge provider; correctness was established by direct code inspection.
- `LessonExcludedSlideReconciler.ReconcileAndLoad`'s dedup/hard-delete correctness (P11-01, closed this round) was established by direct code and test inspection — the regression tests seed the corruption state directly into the fake repository's backing store rather than exercising a live PostgreSQL unique-constraint race; the actual EF `Remove`-then-`SaveChanges` hard delete against a real database was not executed by this round.
- An already-open learner tab retaining its original slide list while vectors disappear immediately (R4.7.10/Q-K2) is browser/session timing behaviour and was not exercised live in this round; it remains an explicitly accepted contract risk, not a defect.

### Phase 8 (Module H) — new block, first time this section has covered this phase
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

## Issues Found — Round 12

None blocking. P11-01 is closed after its third re-check (the project-owner-approved shared-reconciler architecture, verified by direct code and test inspection rather than taken on trust). One non-blocking observation was recorded: EX-8's hard-floor exclusion count in `ToggleAsync` can transiently overcount by 1 within a single request when a *different* page of the same lesson still has an un-reconciled legacy duplicate pair, because the reconciler's hard-deletes haven't committed yet when the count query runs. This is safe-direction only (over-conservative, never permissive), self-heals the first time any endpoint touches the lesson, and is filed as optional hardening in Open Issues above, not as a blocking Issue.

## Review Outcome — Round 12

**Accepted — Phase 11 closes.** 37/37 ✅ Verified, 0 ⚠️ Partial, 0 ❌ Failed, on a genuine FULL round. All 37 Phase 11 checkboxes in `plan.md` are now `[x]`. This was a FULL round with an all-✅ result, so per the autonomous-mode exception (`.claude/shared/conventions.md` §6) this outcome does not require pausing for a user decision when running unattended; in manual mode, present this summary and let the user confirm acceptance explicitly.

Phase 11 is now deploy-eligible on QA-mode grounds (FULL round required before `devops`, now satisfied). The only remaining blocker before `devops` is the standing `🔒 Security gate`, which has not run for this phase yet — see Open Issues above. Per the explicit Module K → Module L dependency in `plan.md`/`design.md`, **Phase 12 (Module L — Lesson trash, restore & permanent purge) may now start.**

## Archived rounds

- Phases 1–6 — Round 1 FULL + Round 2 TARGETED → review/phase-1-6.md
- Phase 3 — superseded Round 1 finding → review/phase-3.md
- Phase 7 — Round 3 FULL → review/phase-7.md
- Phase 9 — Round 4 FULL → review/phase-9.md
- Phase 10 — Round 5 FULL (superseded by Round 7, then Round 8) → review/phase-10.md
- Phase 8 — Round 6 FULL (superseded by Round 7, then Round 8) → review/phase-8.md
- Phase 8 + Phase 10 — Round 7 TARGETED, covered together (superseded by Round 8 FULL) → full text in review/phase-10.md's `## Round 7` section; review/phase-8.md points to it
- Phase 8 + Phase 10 — Round 8 FULL, closing both → full text in `review/phase-10.md` under `## Round 8`
- Phase 11 — Round 9 FULL, 34/37 Partial (superseded by Round 10 FULL) → `review/phase-11.md`
- Phase 11 — Round 10 FULL, 36/37 Partial (superseded by Round 11 TARGETED) → `review/phase-11.md`
- Phase 11 — Round 11 TARGETED, 36/37 Partial, P11-01 failed re-check 2 (superseded by Round 12 FULL) → `review/phase-11.md`
- Phase 11 — Round 12 FULL, 37/37 ✅ — **phase closed** → `review/phase-11.md`

## Change Log

- 2026-08-20 — Round 2 TARGETED Phase 3 re-check; archived with Phases 1–6.
- 2026-08-20 — Round 3 FULL Phase 7; archived in review/phase-7.md.
- 2026-08-26 — Round 4 FULL Phase 9; archived in review/phase-9.md.
- 2026-08-26 — Round 5 FULL Phase 10; archived in review/phase-10.md.
- 2026-08-26 — Round 6 FULL Phase 8, first documented round for the phase. Inspected all 37 tasks against R7, KL-1..KL-26, DM-3/DM-15/MG-H1, implementation, migrations, security-sensitive query paths, and tests. Checked 36 tasks; left the shared layout/filter task unchecked because R7.3 indexing-status filtering is missing. Recorded Important P8-01 and Important P8-02. Frontend lint/typecheck/test/build passed; backend Release build and 269 tests passed; EF reported no pending model changes. No Critical tenant-isolation or SQL-injection finding. Result parked without dispatch per user instruction. Archived to review/phase-8.md, superseded by Round 7.
- 2026-08-26 — **Round 7 TARGETED, re-checking P8-01 (Phase 8), P8-02 (Phase 8), and NR-13 (Phase 10) fixes found already implemented in the working tree.** All three confirmed genuinely fixed by direct code inspection: DocumentLibraryFilterBar.tsx now has a working IndexingStatus filter wired to both tables (P8-01); CompanyIsolationTests.cs gained two real-ApplicationDbContext two-company tests proving R-16 for both DocumentResource and KnowledgeQnA GetAllInCompany (P8-02); PdfLessonContentPhase.tsx now renders the step-1 commit error outside the commitStarted gate (NR-13). Checked blast radius (every Phase 8/10 task touching the same files) and the shared-code watchlist (auth config, EF context, api-client.ts, shared components) — no regressions, one incidental-but-correct fix found and recorded (SlidesEmbed.tsx's double API-base-URL prefix, a learning-session-module concern, no knowledge-base checkbox). Ticked Phase 8's KL-1 layout task and Phase 10's implement-NR-13 task; closed the P8-01/P8-02/NR-13 Open Issues rows. Phase 8 now 37/37 [x], Phase 10 now 21/21 [x]. Automated checks re-run project-wide: frontend typecheck/lint clean, test 69/69, build clean (24 routes); backend build 0/0 (Release), test 271/271 (+2 from P8-02), EF has-pending-model-changes clean. Recorded both phases as accepted, pending one FULL round before devops eligibility (TARGETED rounds don't confer deploy eligibility) — Security gate on both remains independently open and unaudited. Archived Round 6's Phase 8 content verbatim to review/phase-8.md.
- 2026-08-26 — Round 9 FULL Phase 11 archived verbatim to `review/phase-11.md`; superseded by Round 10.
- 2026-08-26 — Round 10 FULL Phase 11 archived verbatim to `review/phase-11.md`; superseded by Round 11 TARGETED.
- 2026-08-26 — **Round 11 TARGETED, P11-01 failed re-check 2.** Verified both changed entry points and both new tests against EX-4/EX-8/EX-9/DM-17. `ApplyExcludedSlidesAsync` now cleans every duplicate group correctly, but direct restore still soft-deletes only one selected row and leaves a live sibling, so the page remains excluded after a successful response; the new toggle test explicitly asserts that incorrect state. Named tests pass 2/2, but no FULL expansion ran after the prerequisite failed. Result remains **36/37 ✅, 1/37 ⚠️ Partial**; Phase 11/12 remain blocked and the two-failed-re-check ceiling routes the next decision to the project owner/user. Archived verbatim to `review/phase-11.md`, superseded by Round 12.
- 2026-08-26 — **Round 12 FULL, Phase 11 / Module K — closes the phase.** Third re-check of P11-01, on an architecture the project owner approved directly past the two-failed-re-check ceiling: `LessonExcludedSlideReconciler.ReconcileAndLoad` (new shared helper) collapses every `(LessonId, SlideObjectId)` duplicate group to one representative via real EF hard-deletes, called by both `ApplyExcludedSlidesAsync` and `ToggleAsync` before any per-page logic, committed in the same transaction as the rest of each method — verified genuinely (not on trust): traced the exact two-live-duplicate restore scenario that failed twice before, confirmed no premature commit, and confirmed the regression test asserts the corrected end state. Re-verified all other 36 Phase 11 tasks from scratch. Automated checks project-wide: frontend typecheck/lint clean, test 69/69, build 19/19 routes; backend build 0/0 (Release), test 289/289 (+2 vs Round 10), `has-pending-model-changes` clean. Result **37/37 ✅ Verified, 0 Partial, 0 Failed**. Ticked the last Phase 11 checkbox (EX-9) in `plan.md`. Recorded one non-blocking observation (EX-8 hard-floor count transient overcount, self-healing, safe-direction) in Open Issues. **Phase 11 closed; Phase 12 (Module L) may now start; Security gate remains open and independently unaudited for both.** Archived Round 12's full detail to `review/phase-11.md`.
