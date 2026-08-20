# 1 ลิงก์ = หลายการเรียนแยกคนละคน — Verification & Review

## Open Issues — all phases

| Issue | Phase | Route | Blocking | Re-check rounds |
|---|---|---|---|---:|
| LS-QA-08 — Phase 3, 4, 5 and 6 carry `🔒 Security gate` and none has had a `security` audit yet | [Phase 3–6](plan.md#sequencing-notes) | `security` — only on explicit user request | Yes | 1 |
| LS-QA-10 — the application layer now provably does not log token/`learnerKey`/query string and sends `Cache-Control: no-store` on every learner route (confirmed this round, code-level) — but there is still no reverse proxy, CI, or deployment artifact in the repo to audit for TLS termination / access-log behaviour, so the infrastructure half of CA-3 remains unconfirmed | [Phase 3](plan.md#phase-3-module-c--learning-lifecycle-ฝั่งผู้เรียน-api--security-gate) | `security` audits infra when it exists; `devops` confirms at deploy | Yes | 1 |

## Verification Summary (current round — FULL-3)

- **Mode: FULL.** Every task in Phase 1–6, the full `Data Model` section of `design.md` (DM-1..DM-8) against real `schema`/entities, and all four contract sets (Learning Lifecycle LR-1..LR-8/LR-3a, Progress & Stalled SR-1..SR-3, Review RR-1..RR-6, Isolation & Credential IC-1..IC-7) were inspected against real code, not taken from `plan.md`'s existing checkboxes or from `review.md`'s prior content.
- **Why FULL and not TARGETED:** the caller flagged that `review.md`, `deploy.md` and the `review/*.md` archive files present in the worktree were not produced by a real `qa-engineer`/`devops` run of this pipeline, so there was no trustworthy FULL-round manifest to diff against. Every claim in this round comes from a fresh read of the current file, not from the prior TARGETED-2 entry (now archived at [`review/phase-1-6-targeted-2.md`](review/phase-1-6-targeted-2.md)) — that entry's technical claims were independently re-derived here and, where checked, held up (migrations, build/test counts, isolation, `no-pending-model-changes`).
- **Result: the module is in much better shape than `plan.md`'s narrative suggested.** Reading `plan.md`'s header box literally would say most checkboxes are `[ ]`; in the actual file 48/53 tasks were already `[x]`. Independent inspection this round found **zero code-level contradictions with `design.md`/CA-1..CA-6** anywhere it checked — including every one of the 7 "drift" points `plan.md` asked this round to adjudicate. One previously-unchecked task (Phase 3, request logging/cache) was verified and checked off this round. The only tasks that remain open are three that explicitly require a running browser (Phase 4/5 manual tests) — this agent invocation has no browser/computer-use tool, so those could only be traced through source, not executed.
- **Data Model (DM-1..DM-8):** `TrainingLink`, `LearningSession`, `SessionQuestion`, `ChatMessage` entities read field-by-field against `design.md`'s DM-1..DM-4 (read together with CA-1's amended names, as `plan.md` and `design.md`'s own banner instruct) — exact match, including audit-field mutability (CA-4: `SessionQuestion.UpdateBy/UpdateDate` are `set`, delete-audit fields stay `init`; `ChatMessage` fully `init`). `SessionSummary` (DM-5) does not exist anywhere in `src/` or `tests/` (only in old, untouched migrations, which is correct). Status constants (DM-6) match exactly, old `SessionStatus.NotStarted/Expired` values are gone. `ApplicationDbContext.OnModelCreating` (DM-7) has `HasQueryFilter` on all four entities plus the composite `(TrainingLinkId, LearnerKey)` index. Repositories (DM-8) match CA-4 exactly — no `GetByIdAcrossCompanies` on the learner-facing `ILearningSessionRepository`, only `ITrainingLinkRepository.GetByToken()` bypasses the filter, with its explaining XML doc intact. `LessonConfig`/`DocumentResource` untouched, as required — these belong to the `knowledge-base` module and were only touched here as a scope check, not re-verified in depth.
- **`dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration."** This is the strongest available confirmation that entities, `ApplicationDbContext`, and the two real migrations (`20260813140603_SplitLinkAndAddAuth`, `20260818155126_AddTotalSlideCount`) all agree — read-only EF tooling check, no migration was applied or altered.
- **Automated checks run, all green:**
  - `dotnet build SupportRoom.slnx` → 0 Warning(s), 0 Error(s).
  - `dotnet test SupportRoom.slnx --filter "Category!=Integration"` → 21 (Providers) + 127 (Application) + 1 (Api.IntegrationTests, the still-template `UnitTest1.cs` — confirmed by reading the file, not real coverage) = 149 passed, 0 failed.
  - `npm run typecheck` (Node 22 via nvm) → clean.
  - `npm run lint` → clean.
  - `npm run test -- --run` (Vitest) → 4 files, 36/36 passed.
  - `npm run build` → succeeds; only the pre-existing "multiple lockfiles" Next.js warning, unrelated to this module.
- **This project has a real automated test suite** — it is not the "no tests" baseline. `LearningSessionServiceTests.cs` (574 lines) individually covers LR-1 expiry/validation, LR-3/LR-3a's "learnerKey empty → resumable:null, not an error" rule, ENDED rows never counting as resumable, LR-4/LR-5 idempotency and the one-way `CompletedAllSlides` flip, SR-2/SR-3 stalled derivation. `SessionQuestionServiceTests.cs` covers RR-2/RR-3/RR-6. `CompanyIsolationTests.EveryEntityIsCompanyScoped` is a live tripwire, not a per-entity assertion. See `## Unverified Behaviour` below for what this suite does **not** reach.
- **CA-2/CA-3 drift resolution (the 7 points `plan.md` asked this round to adjudicate) — all 7 read as "implementation variant that still satisfies the contract", not "wrong, must fix":**
  1. Route shape (`/api/learning-sessions/{token}/join|restart|progress|end|summary` etc.) — matches CA-2's table exactly, confirmed against `LearningSessionController.cs`.
  2. `learnerKey` as a body/query field, not `X-Learner-Key` header — `grep -rn "X-Learner-Key"` across `backend/src` and `frontend/src` returns **zero matches**; the header was fully replaced by CA-2's `(token, learnerKey)` server-resolution pattern everywhere, including `POST /api/voice-question` (multipart field `learnerKey`) and SignalR (`JoinSession(token, learnerKey)`).
  3. `RecipientName` vs `LearnerName` — entity field is `RecipientName` per CA-1; TS type, ViewModel, DTO, and every UI label agree.
  4. `SessionId` vs `LearningSessionId` — `SessionQuestion.SessionId`/`ChatMessage.SessionId` per CA-1; consistent end-to-end including the migration's `RenameColumn` step.
  5. Status-constant class names — `SessionStatus`, `LinkStatus`, `ReviewResult` in `Domain/Enums/SessionStatus.cs`, matching CA-1's table.
  6. SignalR method signatures — `JoinSession(token, learnerKey)` / `SendChatMessage(token, learnerKey, text)` on the hub match CA-2's table verbatim (not the baseline's original `JoinLearning(learningSessionId)` proposal, which CA-2 explicitly superseded).
  7. Migration file names/shape — `20260813140603_SplitLinkAndAddAuth` and `20260818155126_AddTotalSlideCount` match CA-5 exactly; no `SplitLessonLinkAndLearningSession` file exists.
  None of these needed a code change this round — CA-1..CA-6 already closed them as `system-analyst` decisions, and this round's job was to confirm the code actually matches the amendment text, which it does everywhere checked.
- **Entity name note (not a drift item):** the module uses `TrainingLink` throughout, per the standing instruction that Q2's original `LessonLink` rename proposal was withdrawn by the project owner before `design.md` was written. `TrainingLink.cs`, all controllers/services/DTOs, and `frontend/src/types/domain.ts` agree — confirmed, not re-litigated.

## Verified File Manifest — learning-session Phase 1–6 (FULL-3)

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `_docs/module/learning-session/requirement.md` | 66073 | 431 | FULL-3 |
| `_docs/module/learning-session/design.md` | 128571 | 1173 | FULL-3 |
| `_docs/module/learning-session/plan.md` | 35685 | 230 | FULL-3 |
| `backend/src/SupportRoom.Domain/Entities/TrainingLink.cs` | 2069 | 42 | FULL-3 |
| `backend/src/SupportRoom.Domain/Entities/LearningSession.cs` | 3621 | 72 | FULL-3 |
| `backend/src/SupportRoom.Domain/Entities/SessionQuestion.cs` | 1734 | 39 | FULL-3 |
| `backend/src/SupportRoom.Domain/Entities/ChatMessage.cs` | 962 | 25 | FULL-3 |
| `backend/src/SupportRoom.Domain/Enums/SessionStatus.cs` | 1650 | 44 | FULL-3 |
| `backend/src/SupportRoom.Domain/Configuration/ServerDefaults.cs` | 13416 | 298 | FULL-3 |
| `backend/src/SupportRoom.Application/Dto/DtoLimits.cs` | 1124 | 22 | FULL-3 |
| `backend/src/SupportRoom.Application/Dto/LearningSessionDto.cs` | 2451 | 60 | FULL-3 |
| `backend/src/SupportRoom.Application/Dto/CreateTrainingLinkDto.cs` | 1082 | 28 | FULL-3 |
| `backend/src/SupportRoom.Application/ViewModel/LearningSessionViewModel.cs` | 1715 | 33 | FULL-3 |
| `backend/src/SupportRoom.Application/ViewModel/TrainingLinkViewModel.cs` | 1459 | 32 | FULL-3 |
| `backend/src/SupportRoom.Application/ViewModel/SessionQuestionViewModel.cs` | 757 | 18 | FULL-3 |
| `backend/src/SupportRoom.Application/ViewModel/LearnerSessionQuestionViewModel.cs` | 641 | 16 | FULL-3 |
| `backend/src/SupportRoom.Application/ViewModel/SessionSummaryViewModel.cs` | 1263 | 26 | FULL-3 |
| `backend/src/SupportRoom.Application/ViewModel/LearnerSessionSummaryViewModel.cs` | 559 | 14 | FULL-3 |
| `backend/src/SupportRoom.Application/Services/ITrainingLinkService.cs` | 8891 | 189 | FULL-3 |
| `backend/src/SupportRoom.Application/Services/ILearningSessionService.cs` | 17396 | 370 | FULL-3 |
| `backend/src/SupportRoom.Application/Services/ISessionQuestionService.cs` | 6235 | 128 | FULL-3 |
| `backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs` | 5714 | 118 | FULL-3 |
| `backend/src/SupportRoom.Application/Services/IChatMessageService.cs` | 4376 | 94 | FULL-3 |
| `backend/src/SupportRoom.Application/Realtime/IRealtimeNotifier.cs` | 1042 | 20 | FULL-3 |
| `backend/src/SupportRoom.Api/Program.cs` | 8530 | 193 | FULL-3 |
| `backend/src/SupportRoom.Api/Configurations/AuthenticationConfiguration.cs` | 4053 | 81 | FULL-3 |
| `backend/src/SupportRoom.Api/Controllers/TrainingLinkController.cs` | 3479 | 86 | FULL-3 |
| `backend/src/SupportRoom.Api/Controllers/LearningSessionController.cs` | 4859 | 108 | FULL-3 |
| `backend/src/SupportRoom.Api/Controllers/SessionQuestionController.cs` | 1731 | 40 | FULL-3 |
| `backend/src/SupportRoom.Api/Controllers/ChatMessagesController.cs` | 1342 | 34 | FULL-3 |
| `backend/src/SupportRoom.Api/Controllers/VoiceQuestionController.cs` | 3019 | 75 | FULL-3 |
| `backend/src/SupportRoom.Api/Hubs/SessionHub.cs` | 5287 | 137 | FULL-3 |
| `backend/src/SupportRoom.Api/Realtime/SignalRRealtimeNotifier.cs` | 875 | 17 | FULL-3 |
| `backend/src/SupportRoom.Api/.env.example` | 6235 | 120 | FULL-3 |
| `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs` | 6213 | 128 | FULL-3 |
| `backend/src/SupportRoom.Providers.Data/Data/UnitOfWork/UnitOfWork.cs` | 1786 | 37 | FULL-3 |
| `backend/src/SupportRoom.Providers.Data/Repository/ITrainingLinkRepository.cs` | 1190 | 26 | FULL-3 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILearningSessionRepository.cs` | 2778 | 55 | FULL-3 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260813140603_SplitLinkAndAddAuth.cs` | 20781 | 321 | FULL-3 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260818155126_AddTotalSlideCount.cs` | 1146 | 40 | FULL-3 |
| `backend/src/SupportRoom.Providers.Data/Migrations/ApplicationDbContextModelSnapshot.cs` | 21753 | 632 | FULL-3 |
| `backend/tests/SupportRoom.Application.Tests/TrainingLinkServiceTests.cs` | 6998 | 196 | FULL-3 |
| `backend/tests/SupportRoom.Application.Tests/LearningSessionServiceTests.cs` | 23580 | 574 | FULL-3 |
| `backend/tests/SupportRoom.Application.Tests/SessionQuestionServiceTests.cs` | 8593 | 220 | FULL-3 |
| `backend/tests/SupportRoom.Application.Tests/VoiceQuestionServiceTests.cs` | 8797 | 174 | FULL-3 |
| `backend/tests/SupportRoom.Application.Tests/ChatMessageServiceTests.cs` | 7446 | 173 | FULL-3 |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 8889 | 198 | FULL-3 |
| `backend/tests/SupportRoom.Api.IntegrationTests/UnitTest1.cs` | — | 10 | FULL-3 (confirmed still a template, no real coverage) |
| `frontend/src/types/domain.ts` | 12710 | 365 | FULL-3 |
| `frontend/src/lib/api-client.ts` | 17427 | 452 | FULL-3 |
| `frontend/src/utils/learner-key.ts` | 3797 | 100 | FULL-3 |
| `frontend/src/utils/learner-key.test.ts` | 2074 | 77 | FULL-3 |
| `frontend/src/utils/session-status.ts` | 1683 | 48 | FULL-3 |
| `frontend/src/hooks/use-session-chat.ts` | 4993 | 128 | FULL-3 |
| `frontend/src/hooks/use-agent-session-chat.ts` | 4300 | 114 | FULL-3 |
| `frontend/src/hooks/use-tutor-session.ts` | 24367 | 542 | FULL-3 |
| `frontend/src/app/join/[token]/page.tsx` | 14604 | 328 | FULL-3 |
| `frontend/src/app/room/[token]/page.tsx` | 10680 | 255 | FULL-3 |
| `frontend/src/app/session-ended/[token]/page.tsx` | 3919 | 89 | FULL-3 |
| `frontend/src/app/admin/page.tsx` | 3268 | 87 | FULL-3 |
| `frontend/src/app/admin/links/[token]/page.tsx` | 7029 | 161 | FULL-3 |
| `frontend/src/app/admin/learning-sessions/[id]/page.tsx` | 9645 | 232 | FULL-3 |
| `frontend/src/components/admin/CreateTrainingLinkModal.tsx` | 6846 | 160 | FULL-3 |
| `frontend/src/components/admin/TrainingLinksTable.tsx` | 3716 | 76 | FULL-3 |
| `frontend/package.json` | 1253 | 50 | FULL-3 |

## Per-Task Results — learning-session Phase 1 (FULL-3, Module A)

All 9/9 tasks ✅ Verified:

- ✅ [backend] `20260813140603_SplitLinkAndAddAuth` inspected line-by-line: hand-edited to `RenameTable`+`RenameIndex` instead of the EF-generated `DropTable`+`CreateTable`; backfills `Company` from every legacy `CompanyId` source, splits each `TrainingSession` row with real activity into a `LearningSession` row (`learning_` prefix), repoints `SessionQuestion.SessionId`/`ChatMessage.SessionId`, adds the 3 review columns, drops `SessionSummary`. `dotnet ef migrations list` against the local rehearsal Postgres shows both migrations already applied there (not "(Pending)"), consistent with the rehearsal evidence this file's TARGETED-2 predecessor recorded.
- ✅ [backend] `20260818155126_AddTotalSlideCount` inspected: runs after the first, makes `LastSlideIndex` nullable and adds nullable `TotalSlideCount` — matches CA-5 exactly, no additional migration exists.
- ✅ [backend] `TrainingLink` entity matches CA-1 + DM-1 field-for-field; no `Status` column anywhere on it.
- ✅ [backend] `LearningSession` matches CA-1 + DM-2: `TrainingLinkId`, `RecipientName`, all fields present with correct nullability.
- ✅ [backend] `SessionQuestion` has `ReviewResult`/`ReviewNote`/`ReviewedAt`; `UpdateBy`/`UpdateDate` are `set`; `DeleteBy`/`IsDelete`/`DeletedAt` are `init` — exact CA-4 match, no soft-delete flow anywhere referencing them.
- ✅ [backend] `SessionQuestion.SessionId`/`ChatMessage.SessionId` point at `LearningSession.Id` everywhere (repositories, indexes, controllers) per CA-1; not renamed to `LearningSessionId`.
- ✅ [backend] `SessionStatus`/`LinkStatus`/`ReviewResult` constant classes match CA-1's wire values exactly (`IN_PROGRESS`/`ENDED`, `ACTIVE`/`EXPIRED`, `correct`/`incorrect`); no stored link status.
- ✅ [backend] Query filters present on `TrainingLink`, `LearningSession`, `SessionQuestion`, `ChatMessage` in `ApplicationDbContext.OnModelCreating`; `CompanyIsolationTests.EveryEntityIsCompanyScoped` exists and passed in this round's test run.
- ✅ [backend] Repositories match CA-4: `ITrainingLinkRepository.GetByToken` (only bypass, XML doc intact), `ILearningSessionRepository.GetActiveByLearnerKey/GetLatestInProgressByLearnerKey/GetLatestEndedByLearnerKey/GetByTrainingLinkId`; both registered in `UnitOfWork.Register`; no `GetByIdAcrossCompanies` on the learner path.
- ✅ [backend] `INACTIVE_THRESHOLD_MINUTES` present in `ServerDefaults.GetInactiveThresholdMinutes()` (default 30) and documented in `.env.example`.
- ✅ [backend] `dotnet build` 0/0, `dotnet test --filter "Category!=Integration"` 149/149 passed this round (21 Providers + 127 Application + 1 template Api.IntegrationTests). `dotnet ef migrations has-pending-model-changes` confirms model/snapshot agreement.

## Per-Task Results — learning-session Phase 2 (FULL-3, Module B)

All 5/5 tasks ✅ Verified:

- ✅ [backend] `GET /api/training-links` returns `learnerCount`/`inProgressCount`/`endedCount`/`status` computed from `ExpiresAt` — confirmed in `ITrainingLinkService.GetAll`/`ToViewModel`.
- ✅ [backend] `CreateTrainingLinkDto` has no `recipientName` field; `maxAttendees` is `[Range(1, int.MaxValue)]` and the service re-validates `< 1` — matches LR-7.
- ✅ [backend] `ITrainingLinkService` has no `if` referencing `MaxAttendees` for enforcement anywhere — matches LR-2/Declined 2026-08-11.
- ✅ [frontend] `CreateTrainingLinkModal.tsx` has no recipient-name field and shows "ค่านี้ยังไม่มีผลในระบบ ระบบจะยังไม่จำกัดจำนวนผู้เข้าเรียนในเฟสนี้" under the `maxAttendees` input — matches F8.
- ✅ [frontend] `app/admin/page.tsx` → `TrainingLinksTable.tsx` shows learner/in-progress/ended counts and an ACTIVE/EXPIRED badge per link.

## Per-Task Results — learning-session Phase 3 (FULL-3, Module C) 🔒 Security gate

All 11/11 tasks ✅ Verified (one, the request-logging task, checked off this round):

- ✅ [backend] LR-1 sequence confirmed in `LearningSessionService.CreateSession`/`ResolveLinkForJoin`: token lookup → `CompanyContext.Resolve` first → expiry check (no row created if expired) → name trim/length validation → learnerKey length validation → row creation with all the specified defaults → commit.
- ✅ [backend] LR-2 — no `MaxAttendees` reference in the learning-session service.
- ✅ [backend] LR-3/LR-3a table traced end-to-end: `GetResumeState` returns `{link, resumable, lastEnded, linkExpired}`, always 200, empty `learnerKey` short-circuits to `resumable: null` without querying, `resumable` only ever an `IN_PROGRESS` row, `resumable` takes priority over `lastEnded`. All 6 combinations map correctly onto the actual `GET /api/learning-sessions/{token}/resume?learnerKey=` response shape.
- ✅ [backend] LR-4 — `UpdateProgress`: no-op + 200 when `ENDED`; `TotalSlideCount` only overwritten when `> 0`; `CompletedAllSlides` is one-way `true`; expiry never checked.
- ✅ [backend] LR-5 — `End`: idempotent no-op when already `ENDED`; `CompletedAllSlides` is OR, not overwrite; expiry never checked.
- ✅ [backend] LR-6 — Restart always creates a new row via `CreateSession`, never touches the old one; expiry enforced through the shared `ResolveLinkForJoin` path.
- ✅ [backend] LR-8 — no `PATCH /api/sessions/{token}` or `MarkStarted` anywhere in `src/`.
- ✅ [backend] IC-1 — every learner-facing entry point resolves `TrainingLink`/`LearningSession` first and calls `CompanyContext.Resolve` before any other query (`GetEntityByToken`, `GetEntityByLearnerKey`).
- ✅ [backend] CA-3/IC-1 — only `ITrainingLinkRepository.GetByToken()` bypasses the filter, XML doc intact; no `GetByIdAcrossCompanies` on the learner path.
- ✅ [backend] CA-2/CA-3 — `progress`/`end`/`summary`/questions/chat/voice-question all resolve via `(token, learnerKey)`; mismatch throws `GeneralException.NotFound` (404, confirmed in `GetEntityByLearnerKey`); `LearningSessionViewModel` has no `LearnerKey` field (confirmed, with an explicit comment explaining why); no public `learningSessionId` parameter accepted anywhere on the learner surface.
- ✅ [backend] **(newly checked this round)** request logging/cache/HTTPS — `Program.cs`'s custom "SafeRequestLogging" middleware logs only method/status/duration, explicitly never path or query (comment cites the exact reason: `RequestPath` on learner routes carries the token, and `learnerKey` arrives in the query string); a second middleware sets `Cache-Control: no-store`, `Pragma: no-cache`, `Referrer-Policy: no-referrer` on every `IsSensitiveLearnerPath` route (all 7 learner-facing prefixes plus the hub); `appsettings.json`/`appsettings.Development.json` suppress `Microsoft.AspNetCore` to `Warning`, which prevents ASP.NET Core's own default request-start/finish logs (which do include path+query) from ever being emitted; `UseHsts()`/`UseHttpsRedirection()` are applied outside Development. `HttpStatusCodeExceptionHandler`/`GlobalExceptionHandler` log only the exception, never the request path/query. **This confirms the application-layer half of CA-3.** The infrastructure half (does a not-yet-built reverse proxy/CDN/monitoring stack also avoid logging the query string, and does TLS actually terminate correctly) cannot be checked — there is no Dockerfile, CI, or deployment config in the repo — and stays open as LS-QA-10, routed to `security`/`devops`.
- ✅ [backend] SR-1/SR-2/SR-3 — env-driven threshold, `IsStalled` computed once in `LearningSessionService.ToViewModel`, `ENDED` rows never stalled (guarded by `Status == InProgress &&`).
- ✅ [backend] `LearningSessionServiceTests.cs` (574 lines, 40+ `[Fact]`/`[Theory]` cases) covers LR-1 through LR-6 exhaustively, including the specific edge cases `design.md`'s "Test ที่กระทบ" table calls out by name (empty-key resume, ENDED rows never resumable, wrong-key 404, restart not touching the old row).

## Per-Task Results — learning-session Phase 4 (FULL-3, Module D) 🔒 Security gate

5/6 ✅ Verified, 1 unverified (manual test, not executable in this session):

- ✅ **[backend] Realtime isolation test (R1)** — **closed 2026-08-19 (MANUAL-5)**. No available browser tool gives two genuinely separate browser profiles in this session (`Claude_Browser` tabs share one profile's `localStorage`; `claude-in-chrome` connects to a Chrome on a different physical Windows machine, which cannot reach this Mac's `localhost`) — so the test method changed from "two-browser UI" to "two independent SignalR connections", which proves the same mechanism (server-side group scoping) at the transport level instead of through the DOM. A Node script using `@microsoft/signalr` opened two independent `HubConnection`s to `/hubs/session`, each calling `JoinSession(token, learnerKey)` with a distinct `learnerKey` against two separate `LearningSession` rows on the same `TrainingLink` (seeded via direct SQL). Connection A called `SendChatMessage`; A received its own message back, **B received `[]` — nothing leaked.** This confirms `Groups.AddToGroupAsync` in `SessionHub.cs` groups by `LearningSession.Id`, exactly matching `IC-5`. `ReceiveNewQuestion`/IC-6 was not re-tested through a second channel because `SignalRRealtimeNotifier.cs` broadcasts every event type through the identical `Clients.Group(learningSessionId).SendAsync(...)` call — one shared code path, so one channel's proof covers both. Test script and seeded rows deleted after the run.
- ✅ [backend] `POST /api/voice-question` (`VoiceQuestionService.AskAsync`) resolves `(Token, LearnerKey)` to a `LearningSession` on every call — no caching of the resolved session across requests — before creating the question and broadcasting; matches R2.
- ✅ [backend] `SessionQuestion.SessionId`/`ChatMessage.SessionId` point at `LearningSession.Id` throughout; no code path writes either against a `TrainingLink` id.
- ✅ [backend] `IRealtimeNotifier.NotifyNewQuestionAsync`/`NotifyChatMessageAsync` both take a `learningSessionId` and broadcast to `hubContext.Clients.Group(learningSessionId)` — confirmed in `SignalRRealtimeNotifier.cs`, group key is the learning session id, never the token.
- ✅ [frontend] `useSessionChat(token, learnerKey)` calls `connection.invoke("JoinSession", token, learnerKey)` and `SendChatMessage(token, learnerKey, text)`, matching `SessionHub.cs`'s actual signatures exactly (which is CA-2's amended shape, not the baseline's original `JoinLearning(learningSessionId)` proposal). `useAgentSessionChat(learningSessionId)` calls `JoinSessionAsAgent`/`SendChatMessageAsAgent` with a JWT via `accessTokenFactory`, matching the Hub's authenticated agent path.
- ✅ [frontend] `grep -rn "X-Learner-Key"` and a read of every SignalR call site in `frontend/src` found no join/broadcast call using a token alone without `learnerKey`/session context.

## Per-Task Results — learning-session Phase 5 (FULL-3, Module E) 🔒 Security gate

7/10 ✅ Verified, 3 unverified (manual tests, not executable in this session):

- ✅ **[frontend] Manual 6-case LR-3 test** — **6/6 confirmed live 2026-08-19 (MANUAL-5)**. The 3 not-expired cases were confirmed live in MANUAL-4 (see prior round below). This round closed the remaining 3 `linkExpired=true` cases: seeded a training link with `ExpiresAt` set to the past via direct SQL plus a matching `LearningSession`, and verified each case in a real browser, checking `disabled` on the actual DOM buttons (`document.querySelectorAll('button')`), not just a screenshot. `resumable`+expired → confirm screen shown, "ใช่ เรียนต่อจากเดิม" `disabled:false`, "ไม่ใช่ เริ่มเรียนใหม่ในชื่ออื่น" `disabled:true` with the expiry explanation text — matches `join/[token]/page.tsx`'s `disabled={linkExpired}`. `lastEnded`-only+expired → recap screen shown, but the "เรียนอีกครั้ง" `NameForm` is not rendered at all (not merely disabled) — same end result, restart blocked. neither+expired → redirected to `/link-expired`, matching the `!resumable && !lastEnded && linkExpired` branch. Seeded link/sessions deleted after the run.
- ✅ **[frontend] Manual IC-7 test (leave mid-lesson, reopen same browser)** — **confirmed live 2026-08-19 (MANUAL-4).** Navigated directly to `/room/3d7e0f51-…` with no room-entry grant set → redirected to `/join/3d7e0f51-…` (confirmed via `location.href` in-page, not just visual inspection) and rendered the correct join-screen state for the current resume data. `room/[token]/page.tsx`'s `consumeRoomEntry(token)` gate behaves as designed when opened directly.
- ✅ [frontend] "เริ่มเรียนใหม่ในชื่ออื่น" (`handleJoin`'s restart branch) sends `learnerKey: getOrCreateLearnerKey()` — the existing stored key, never a fresh one — and calls `restartLearningSession`, which always creates a new row server-side (LR-6) without touching the old one.
- ✅ [frontend] No "confirmed" flag is ever written to `localStorage`/cookie/query string. The only persistent room-entry-adjacent value is `supportroom.learnerKey` (`IC-4`); the one-shot grant is `sessionStorage`-only and is deleted the instant it's read (`consumeRoomEntry`), so it cannot survive a reload of `/room` on its own and a fresh `/join` visit always re-asks.
- ✅ **[frontend] One-shot room-entry grant under React Strict Mode (dev mode)** — **confirmed live 2026-08-19 (MANUAL-4).** `frontend/next.config.ts` has `reactStrictMode: true` and the dev server that served this test session ran with that config. Flow exercised end-to-end: join → enter room → navigate back to `/join/[token]` (simulating "leave mid-lesson, reopen") → resume-confirmation screen appears → click "ใช่ เรียนต่อจากเดิม" → lands on `/room/[token]` and stays there (confirmed via `location.href`), not bounced back to `/join`. This is the exact Strict-Mode double-invocation regression `entryGrantedRef` was written to fix; it did not reproduce.
- ✅ [frontend] `hooks/use-tutor-session.ts`'s `persistProgress` fires from the `LOAD_SLIDE` effect (i.e., on every slide change) with `totalSlideCount: slidesRef.current.length || undefined`; no `setInterval`/heartbeat exists anywhere in the file.
- ✅ [frontend] The leave button (`ControlBar`'s `onLeave`) dispatches `END_SESSION` → `PERSIST_END` effect → `persistEnd` → `api.endLearningSession`; backend idempotency for repeated calls is confirmed at the service layer (Phase 3 LR-5 finding above).
- ✅ [frontend] `app/admin/learning-sessions/[id]/page.tsx`/the learner summary path uses `LearnerSessionQuestionViewModel`/`LearnerSessionSummaryViewModel` — confirmed to have no `reviewResult`/`reviewNote`/`reviewedAt`/`unansweredPoints` fields on the C# type, and the corresponding TS `LearnerSessionQuestion`/`LearnerSessionSummary` `Omit<>` the same fields.
- ✅ [frontend] "เรียนอีกครั้ง" button on the ended screen calls Restart with the prefillable stored name.
- ✅ [frontend] `getOrCreateLearnerKey()` uses `crypto.randomUUID()`, stored once under `supportroom.learnerKey` in `localStorage`, reused across links (confirmed no per-link key generation).

## Per-Task Results — learning-session Phase 6 (FULL-3, Module F) 🔒 Security gate

All 10/10 tasks ✅ Verified:

- ✅ [backend] RR-2 — `ReviewResult.IsValid` rejects everything except `"correct"`/`"incorrect"`/`null`, including `""`, via a `ValidationError`.
- ✅ [backend] RR-3 — `reviewResult: null` clears `ReviewResult`/`ReviewNote`/`ReviewedAt` together; `reviewNote` is trimmed, empty becomes `null`, length capped at `DtoLimits.ReviewNoteMaxLength` (2000).
- ✅ [backend] RR-4 — no history table/list anywhere; each call overwrites the prior review outright.
- ✅ [backend] RR-5 — `SessionQuestionViewModel` (CS) and `LearnerSessionQuestionViewModel` (learner) are genuinely separate classes, not one type with conditionally-hidden fields.
- ✅ [backend] RR-6 — `GetSummary`'s `UnansweredPoints` is computed inline on every call from `AnswerStatus == NotFound`, never persisted.
- ✅ [backend] `GetByTrainingLinkId` → `LearningSessionViewModel.IsStalled`/`TotalSlideCount` sent to the frontend; no client-side stalled computation exists.
- ✅ [backend] `SessionQuestionController`/`ChatMessagesController`'s by-learning-session/review endpoints and `SessionHub.JoinSessionAsAgent`/`SendChatMessageAsAgent` all fall under the fail-closed `FallbackPolicy = RequireAuthenticatedUser()` (confirmed in `AuthenticationConfiguration.cs`) or, for the Hub (which is class-level `[AllowAnonymous]` for the anonymous learner methods), have an explicit `guard.EnsureAuthenticated()`/`EnsureCanAccessCompany` call before touching anything — no public-token flow substitutes for CS auth anywhere in Phase 6's surface.
- ✅ [frontend] `app/admin/links/[token]/page.tsx` renders the "หยุดกลางคัน" badge from `session.isStalled` and "7/20" from `lastSlideIndex + 1`/`totalSlideCount`, both backend-computed values, not recomputed on the client.
- ✅ [frontend] `app/admin/learning-sessions/[id]/page.tsx`'s review UI is a free-text `<textarea>` plus "ตอบถูก"/"ตอบผิด"/"ล้างผลรีวิว" buttons — no dropdown/enum for the reason.
- ✅ [frontend] CS page shows `unansweredPoints` and full review fields (`SessionQuestion` type); the learner-facing summary type (Phase 5 finding) never carries them — cross-checked both directions.

## Design/requirement contract checks — learning-session Phase 1–6 (FULL-3)

- **Scope of the schema comparison:** this module owns `TrainingLink`, `LearningSession`, `SessionQuestion`, `ChatMessage` per its Data Model. `LessonConfig` and `DocumentResource` appear in `ApplicationDbContext` but are declared by other modules' `design.md` (`knowledge-base`) — confirmed present there by name, not re-verified field-by-field this round (out of this module's scope per `.claude/shared/conventions.md` §7). `Company`/`AdminUser` are the auth substrate, also out of this module's scope; both are correctly filter-free by design (documented directly on `ApplicationDbContext`), which this round read but did not re-audit — that belongs to whichever module owns TD-014.
- Every field in DM-1..DM-4 (read together with CA-1's amended names) matches the real entities exactly; DM-5 (`SessionSummary` deletion) is complete in source; DM-6/DM-7/DM-8 all confirmed as above.
- `dotnet ef migrations has-pending-model-changes` is the strongest possible confirmation available without applying to a real database: entities, `ApplicationDbContext`, and the migration pair fully agree.
- All four contract sections (Learning Lifecycle, Progress & Stalled, Review, Isolation & Credential) were read in full against the corresponding service/controller/hub code; no contradiction found anywhere except the three manual-test gaps recorded above, which are gaps in *verification*, not known code defects.
- No schema drift found: no model exists in `schema`/entities that isn't declared by *some* module's `design.md`.

## Unverified Behaviour — undeployed phases

This project has a real automated test suite (149 backend xUnit tests, 36 frontend Vitest tests, all green this round) — the blanket "no tests" caveat from the default project baseline does not apply globally here. But the suite does not reach every rule this module depends on, and three specific things were verified only by reading code, never by execution:

### Phase 4 — Module D (Conversation re-pointing & realtime)
- ~~R1/R2: two learners on the same link never see each other's chat/questions in real time.~~ — closed 2026-08-19 (MANUAL-5) via two independent SignalR connections against real seeded `LearningSession` rows (see Issues Found below); no longer unverified.

### Phase 5 — Module E (Learner-facing UI)
- ~~The `linkExpired=true` half of the 6-case `LR-3` branching in `app/join/[token]/page.tsx`.~~ — closed 2026-08-19 (MANUAL-5) with a real expired test link; all 6 cases now confirmed live. No longer unverified.
- ~~IC-7's one-shot room-entry grant under React Strict Mode~~ — confirmed live 2026-08-19 (MANUAL-4), no longer unverified.

No items remain in this section for Phase 4/5 as of MANUAL-5.

## Issues Found — learning-session Phase 1–6 (FULL-3)

No implementation bugs found this round. The only outstanding items are the three manual-test gaps above (Phase 4/5, routed to a future `qa-engineer` round with browser tooling, or to the user) and the two infrastructure-dependent items already in Open Issues (LS-QA-08 security audit, LS-QA-10 infra half of CA-3) — neither of those is a code defect.

## Review Outcome — learning-session Phase 1–6 (FULL-3)

**Not all-green: 3 of 53 tasks remain unchecked (Phase 4 ×1, Phase 5 ×3 minus one already covered — net 3 tasks), all for the same reason (no browser tool in this session), so this stops for a decision rather than auto-continuing.** Everything else — Phase 1, 2, 3, 6 in full, and the non-manual tasks in Phase 4/5 — is ✅ Verified this round by direct code inspection plus passing `typecheck`/`lint`/`test`/`build` on both stacks and `dotnet ef migrations has-pending-model-changes`.

`🔒 Security gate` on Phase 3/4/5/6 remains correctly in place in `plan.md`'s phase headings (not re-litigated, not closed by this round — functional verification is not a security audit). LS-QA-08 (security not yet run) and LS-QA-10 (infra half of CA-3 unconfirmed) stay open pending `security`/`devops`.

**This round does not make the module deploy-eligible** even though it is FULL: `devops` requires the phase to be *accepted*, and three tasks are still open pending manual tests this session could not run.

## Archived rounds

- Phase 1–6 FULL-1 — ❌ sent back for fixes → [`review/phase-1-6-full-1.md`](review/phase-1-6-full-1.md)
- Phase 1–6 FULL-2 — ⚠️ partial, superseded by TARGETED-1 → [`review/phase-1-6-full-2.md`](review/phase-1-6-full-2.md)
- Phase 3/5 TARGETED-1 — ✅ LS-QA-09 accepted, superseded by TARGETED-2 → [`review/phase-3-5-targeted-1.md`](review/phase-3-5-targeted-1.md)
- Phase 1–6 TARGETED-2 — ✅ LS-QA-01 accepted, superseded by FULL-3 → [`review/phase-1-6-targeted-2.md`](review/phase-1-6-targeted-2.md)

## Change Log

- 2026-08-19 — **MANUAL-5** (targeted, browser + direct SQL + SignalR script): closed the 2 remaining `LS-QA-05` gaps from MANUAL-4. (1) The 3 `linkExpired=true` `LR-3` cases: seeded an expired `TrainingLink` (`ExpiresAt` in the past, via direct SQL) with a matching `LearningSession`, tested all 3 combinations live in a browser checking real DOM `disabled` attributes — all 3 matched `design.md`'s LR-3 table exactly (resumable+expired: restart button disabled with explanation; lastEnded-only+expired: no restart form rendered at all; neither+expired: redirect to `/link-expired`). `LR-3` is now 6/6 confirmed live. (2) R1 two-browser realtime isolation: no available browser tool in this session gives two genuinely separate profiles (`Claude_Browser` shares `localStorage` across tabs; `claude-in-chrome` is bound to a Chrome on a separate physical Windows machine that cannot reach this Mac's `localhost`) — changed method to two independent `@microsoft/signalr` `HubConnection`s joining `/hubs/session` with distinct `learnerKey`s against two seeded `LearningSession` rows on one `TrainingLink`; connection A's `SendChatMessage` was received only by A, connection B received nothing, confirming `SessionHub.cs`'s group-by-`LearningSession.Id` scoping (IC-5) at the transport level. Did not separately re-test `ReceiveNewQuestion`/IC-6 since `SignalRRealtimeNotifier.cs` broadcasts every event type through the same `Clients.Group(learningSessionId).SendAsync(...)` call. All seeded test data and the SignalR test script were deleted after the run. `LS-QA-05` is now fully closed and removed from Open Issues; `plan.md`'s corresponding two checkboxes are `[x]`. No code changes made; no new defects found.
- 2026-08-19 — **MANUAL-4** (targeted, browser-only): ran the 3 outstanding manual-test items from FULL-3 against the running dev instance (frontend :3000, backend :5080, local Postgres). Closed 2 of 4 `plan.md` checkboxes: IC-7 direct-`/room` redirect (confirmed via `location.href`) and the Strict-Mode one-shot room-entry grant (confirmed `reactStrictMode: true` in `next.config.ts`, full join→leave→resume→re-enter flow held). Partially advanced the 6-case `LR-3` checkbox: 3/6 cases now confirmed live (resumable+not-expired, lastEnded-only+not-expired, neither+not-expired); the 3 `linkExpired=true` cases remain untested (no expired test link, no DB client available to force one). Attempted the R1 two-browser realtime-isolation checkbox and could not complete it: the available browser tool's tabs share one profile's `localStorage`, so two tabs could not hold distinct `learnerKey`s simultaneously — this is a tooling gap, not a code finding, and R1 is still open. No code changes made; no new defects found. IC-4 (`learner-key.ts` as a global browser-wide key, not per-token) was independently re-confirmed as intentional and already covered by FULL-3's file manifest and Phase 5 per-task results (line "getOrCreateLearnerKey() ... reused across links") — FULL-3 did verify this file, contrary to a concern raised about it possibly having been checked before a later edit.
- 2026-08-19 — **FULL-3**: full from-scratch re-verification of all 6 phases (53 tasks), run because the caller could not trust `review.md`/`deploy.md`/`review/*.md` as authored by a real prior pipeline round. Independently re-derived every claim from real code: Data Model DM-1..DM-8 field-by-field, all 4 contract sections, the 7 drift points `plan.md` asked to be adjudicated (all resolved as "implementation variant, matches contract" — no code change needed), `dotnet ef migrations has-pending-model-changes` (clean), backend build/test (0/0, 149/149), frontend typecheck/lint/test/build (all clean, 36/36 tests). Checked off one previously-open Phase 3 task (request logging/cache/HTTPS) after confirming `Program.cs`'s safe-logging middleware and cache headers. Left 3 tasks unchecked (Phase 4 R1 manual two-browser test; Phase 5 six-case LR-3 manual test and IC-7/Strict-Mode manual test) because this session has no browser/computer-use tool — traced all three through source with no contradiction found, but did not execute them. Archived TARGETED-2 to `review/phase-1-6-targeted-2.md`. Added `Unverified Behaviour` section naming the three specific rules the existing 149+36 automated tests do not reach.
- 2026-08-19 — TARGETED-1 archived after being superseded by TARGETED-2; detail in `review/phase-3-5-targeted-1.md`
- 2026-08-19 — TARGETED-2 re-verified LS-QA-01: isolated PostgreSQL 16 rehearsal, migration order/backfill/repoint, idempotent SQL, model/snapshot, rollback/upgrade evidence and automated checks passed; finding closed but LS-QA-05/08/10 and the FULL-before-deploy gate stayed open.
