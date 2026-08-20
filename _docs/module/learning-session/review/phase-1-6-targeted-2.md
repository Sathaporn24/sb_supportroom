# Phase 1–6 (learning-session) — TARGETED-2

## Verification Summary (TARGETED-2)

- Scope: LS-QA-01 ใน Phase 1 — migrations `20260813140603_SplitLinkAndAddAuth` และ `20260818155126_AddTotalSlideCount`, migration evidence, blast radius, EF contract และ automated checks ทั้งโครงการ
- Mode: **TARGETED** — ใช้ FULL-2 manifest เปรียบเทียบไฟล์ที่เปลี่ยน; ไม่ใช่ deploy-eligible FULL round
- Overall: **✅ LS-QA-01 verified**; โมดูลโดยรวมยัง **⚠️ Partial** เพราะ LS-QA-05/08/10 เปิดอยู่
- Isolated PostgreSQL 16 evidence: backend rehearsal ยืนยัน upgrade → rollback → upgrade, Company backfill, demo link/session backfill, `SessionQuestion`/`ChatMessage` repoint และ idempotent rerun; container ถูก stop/auto-remove และไม่แตะ deployment DB
- Source/EF re-check: `SplitLinkAndAddAuth` สร้าง `Company` ก่อน backfill, สร้าง `LearningSession` ก่อน repoint children, rename `TrainingSession` เป็น `TrainingLink` โดยรักษา PK, และ `AddTotalSlideCount` รันต่อท้ายเพื่อทำ `LastSlideIndex` nullable + เพิ่ม `TotalSlideCount` nullable
- `dotnet ef migrations script ... --idempotent` สร้าง SQL ตามลำดับทั้งสอง migration และ guard ทุก statement ด้วย `__EFMigrationsHistory`; `has-pending-model-changes` ยืนยันว่า model ตรง snapshot
- Backend build: ผ่าน 0 warning / 0 error; non-integration tests ผ่าน 140/140 (API 1 + Application 118 + Providers 21)
- Frontend: lint/typecheck ผ่าน, tests 36/36 ผ่านบน bundled Node v24, production build ผ่าน; warning เดิม Next.js เรื่อง multiple lockfiles เท่านั้น
- TARGETED นี้ไม่รัน migration เองตามกฎ QA read-only, ไม่แตะ deployment DB และไม่ครอบ manual browser/security/infrastructure audit; ไม่ทดแทน FULL ก่อน deploy

## Verified File Manifest — learning-session Phase 1–6 (as of TARGETED-2)

FULL-2 manifest คงไว้จนกว่า open issues จะปิด; TARGETED-2 ยืนยันว่าไฟล์ migration/entity/context/snapshot ใน blast radius ยังตรงสถิติเดิม และไม่มี migration ใหม่.

| File | Bytes | Lines | Round |
|---|---:|---:|---|
| `_docs/module/learning-session/requirement.md` | 66073 | 431 | FULL-2 |
| `_docs/module/learning-session/design.md` | 128571 | 1173 | FULL-2 |
| `_docs/module/learning-session/plan.md` | 35685 | 230 | FULL-2 |
| `backend/src/SupportRoom.Domain/Entities/TrainingLink.cs` | 2069 | 42 | FULL-2 |
| `backend/src/SupportRoom.Domain/Entities/LearningSession.cs` | 3621 | 72 | FULL-2 |
| `backend/src/SupportRoom.Domain/Entities/SessionQuestion.cs` | 1734 | 39 | FULL-2 |
| `backend/src/SupportRoom.Domain/Entities/ChatMessage.cs` | 962 | 25 | FULL-2 |
| `backend/src/SupportRoom.Domain/Enums/SessionStatus.cs` | 1650 | 44 | FULL-2 |
| `backend/src/SupportRoom.Application/Dto/DtoLimits.cs` | 994 | 19 | FULL-2 |
| `backend/src/SupportRoom.Application/Dto/LearningSessionDto.cs` | 2337 | 60 | FULL-2 |
| `backend/src/SupportRoom.Application/Dto/CreateTrainingLinkDto.cs` | 1082 | 28 | FULL-2 |
| `backend/src/SupportRoom.Application/ViewModel/LearningSessionViewModel.cs` | 1715 | 33 | FULL-2 |
| `backend/src/SupportRoom.Application/ViewModel/TrainingLinkViewModel.cs` | 1459 | 32 | FULL-2 |
| `backend/src/SupportRoom.Application/Services/ITrainingLinkService.cs` | 8891 | 189 | FULL-2 |
| `backend/src/SupportRoom.Application/Services/ILearningSessionService.cs` | 16938 | 363 | FULL-2 |
| `backend/src/SupportRoom.Application/Services/ISessionQuestionService.cs` | 6235 | 128 | FULL-2 |
| `backend/src/SupportRoom.Application/Services/IVoiceQuestionService.cs` | 5714 | 118 | FULL-2 |
| `backend/src/SupportRoom.Application/Services/IChatMessageService.cs` | 4376 | 94 | FULL-2 |
| `backend/src/SupportRoom.Api/Program.cs` | 8530 | 193 | FULL-2 |
| `backend/src/SupportRoom.Api/Configurations/AuthenticationConfiguration.cs` | 4053 | 81 | FULL-2 |
| `backend/src/SupportRoom.Api/Controllers/TrainingLinkController.cs` | 3479 | 86 | FULL-2 |
| `backend/src/SupportRoom.Api/Controllers/LearningSessionController.cs` | 4859 | 108 | FULL-2 |
| `backend/src/SupportRoom.Api/Controllers/SessionQuestionController.cs` | 1731 | 40 | FULL-2 |
| `backend/src/SupportRoom.Api/Controllers/ChatMessagesController.cs` | 1342 | 34 | FULL-2 |
| `backend/src/SupportRoom.Api/Hubs/SessionHub.cs` | 5287 | 137 | FULL-2 |
| `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs` | 5745 | 119 | FULL-2 |
| `backend/src/SupportRoom.Providers.Data/Data/UnitOfWork/UnitOfWork.cs` | 1699 | 36 | FULL-2 |
| `backend/src/SupportRoom.Providers.Data/Repository/ITrainingLinkRepository.cs` | 1190 | 26 | FULL-2 |
| `backend/src/SupportRoom.Providers.Data/Repository/ILearningSessionRepository.cs` | 2778 | 55 | FULL-2 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260813140603_SplitLinkAndAddAuth.cs` | 20781 | 321 | FULL-2 |
| `backend/src/SupportRoom.Providers.Data/Migrations/20260818155126_AddTotalSlideCount.cs` | 1385 | 46 | FULL-2 |
| `backend/src/SupportRoom.Providers.Data/Migrations/ApplicationDbContextModelSnapshot.cs` | 19363 | 563 | FULL-2 |
| `backend/tests/SupportRoom.Application.Tests/TrainingLinkServiceTests.cs` | 6958 | 195 | FULL-2 |
| `backend/tests/SupportRoom.Application.Tests/LearningSessionServiceTests.cs` | 21926 | 526 | FULL-2 |
| `backend/tests/SupportRoom.Application.Tests/SessionQuestionServiceTests.cs` | 8593 | 220 | FULL-2 |
| `backend/tests/SupportRoom.Application.Tests/VoiceQuestionServiceTests.cs` | 8757 | 173 | FULL-2 |
| `backend/tests/SupportRoom.Application.Tests/ChatMessageServiceTests.cs` | 7446 | 173 | FULL-2 |
| `backend/tests/SupportRoom.Application.Tests/CompanyIsolationTests.cs` | 8853 | 197 | FULL-2 |
| `frontend/src/types/domain.ts` | 12710 | 365 | FULL-2 |
| `frontend/src/lib/api-client.ts` | 17427 | 452 | FULL-2 |
| `frontend/src/utils/learner-key.ts` | 3797 | 100 | FULL-2 |
| `frontend/src/utils/learner-key.test.ts` | 2074 | 77 | FULL-2 |
| `frontend/src/hooks/use-session-chat.ts` | 4993 | 128 | FULL-2 |
| `frontend/src/hooks/use-agent-session-chat.ts` | 4300 | 114 | FULL-2 |
| `frontend/src/hooks/use-tutor-session.ts` | 24367 | 542 | FULL-2 |
| `frontend/src/app/join/[token]/page.tsx` | 14410 | 327 | FULL-2 |
| `frontend/src/app/room/[token]/page.tsx` | 10680 | 255 | FULL-2 |
| `frontend/src/app/session-ended/[token]/page.tsx` | 3919 | 89 | FULL-2 |
| `frontend/src/app/admin/page.tsx` | 3268 | 87 | FULL-2 |
| `frontend/src/app/admin/links/[token]/page.tsx` | 7029 | 161 | FULL-2 |
| `frontend/src/app/admin/learning-sessions/[id]/page.tsx` | 9645 | 232 | FULL-2 |
| `frontend/src/components/admin/CreateTrainingLinkModal.tsx` | 6846 | 160 | FULL-2 |
| `frontend/src/components/admin/TrainingLinksTable.tsx` | 3716 | 76 | FULL-2 |
| `frontend/package.json` | 1253 | 50 | FULL-2 |

### TARGETED-2 file comparison

| File | FULL-2 bytes/lines | Current bytes/lines | Result |
|---|---:|---:|---|
| `20260813140603_SplitLinkAndAddAuth.cs` | 20781 / 321 | 20781 / 321 | ✅ inspected |
| `20260818155126_AddTotalSlideCount.cs` | 1385 / 46 | 1385 / 46 | ✅ inspected |
| `ApplicationDbContextModelSnapshot.cs` | 19363 / 563 | 19363 / 563 | ✅ inspected |
| `ApplicationDbContext.cs` | 5745 / 119 | 5745 / 119 | ✅ inspected |
| `TrainingLink.cs` / `LearningSession.cs` / child entities | FULL-2 baseline | unchanged | ✅ inspected |

## Per-Task Results — learning-session Phase 1 (TARGETED-2)

- ✅ [backend] `SplitLinkAndAddAuth`: isolated PostgreSQL 16 rehearsal ครอบ rename, Company/demo backfill, question/chat repoint และ idempotent rerun โดยไม่ apply deployment DB
- ✅ [backend] `AddTotalSlideCount`: รันต่อหลัง migration แรก; `LastSlideIndex` และ `TotalSlideCount` nullable และ snapshot/model ตรงกัน
- ✅ [backend] rollback: `Down()` repoint child rowsก่อน drop `LearningSession`, fold earliest round กลับสู่ legacy shape; source เตือนอย่างชัดเจนว่า lossy เมื่อหลาย rounds ต่อ link และ rehearsal upgrade-after-down ผ่าน
- ✅ [backend] post-migration regression evidence และการรัน QA ซ้ำ: build ผ่าน 0/0 และ non-integration tests 140/140

## Design/requirement contract checks — learning-session Phase 1 (TARGETED-2)

- CA-5 ตรงกับ migrations จริงสองใบ: `TrainingSession` → `TrainingLink`, `LearningSession` backfill, child `SessionId` repoint, review columns/`SessionSummary` removal และ `TotalSlideCount` ไม่มี migration เพิ่ม
- `LearningSession` ใน entity/context/snapshot มี `TrainingLinkId`, `LearnerKey`, `RecipientName`, nullable `LastSlideIndex`/`TotalSlideCount` และ composite index `(TrainingLinkId, LearnerKey)` ตรง CA-1/DM-2
- Company backfill รวม company IDs จาก legacy `TrainingSession`, `LessonConfig`, questions, chat และ documents ตาม source; rows ที่ไม่มี learning activity ไม่สร้าง invented session ส่วน child-only legacy rows ถูกย้ายเพื่อไม่ orphan
- idempotent SQL เรียง `SplitLinkAndAddAuth` ก่อน `AddTotalSlideCount`; no-pending-model-changes ยืนยัน EF model/snapshot consistency
- `Down()` เป็น lossy ตาม design และไม่ได้ถูกตีความเป็น production rollback plan: ต้อง backup ก่อน migration environment จริง

## Issues Found — learning-session Phase 1 (TARGETED-2)

ไม่มี issue ใหม่จาก LS-QA-01; finding ปิดหลังตรวจ rehearsal evidence, source/SQL order, model/snapshot และ automated checks

## Review Outcome — learning-session Phase 1 (TARGETED-2)

**LS-QA-01 accepted.** ยังไม่ส่งต่อ deploy: โมดูลมี open gates LS-QA-05/08/10 และผลล่าสุดเป็น TARGETED จึงต้องมี FULL round หลังปิด gates ก่อน `devops`.

## Change Log (from main review.md at time of archiving)

- 2026-08-19 — TARGETED-1 archived หลังถูก TARGETED-2 supersede; detail อยู่ใน `review/phase-3-5-targeted-1.md`
- 2026-08-19 — TARGETED-2 re-verify LS-QA-01: isolated PostgreSQL 16 rehearsal, migration order/backfill/repoint, idempotent SQL, model/snapshot, rollback/upgrade evidence และ automated checks ผ่าน; finding ปิด แต่ LS-QA-05/08/10 และ FULL-before-deploy gate ยังเปิด
