# Testing Guide

## Frontend

```powershell
cd frontend
npm install
npm run lint
npm run typecheck
npm run test
npm run build
```

Tests ปัจจุบันครอบคลุม tutor reducer และ Google Slides URL utilities เป็นหลัก

## Backend

```powershell
cd backend
dotnet restore SupportRoom.slnx
dotnet build SupportRoom.slnx
dotnet test SupportRoom.slnx --filter "Category!=Integration"
# opt-in เมื่อมี credentials/network:
dotnet test SupportRoom.slnx --filter "Category=Integration"
```

Test projects:

- `SupportRoom.Application.Tests` — service behavior แต่บาง test สร้าง real providers
- `SupportRoom.Providers.Tests` — parser/pure provider logic และบาง live provider tests
- `SupportRoom.Api.IntegrationTests` — ปัจจุบันมีเพียง placeholder test ยังไม่ยืนยัน endpoints

live Google/Pinecone/Gemini/Edge TTS cases ต้องมี `.env`/network แต่ติด
`Category=Integration` แล้ว จึงตัดออกจาก default CI gate ได้

## Manual Smoke Test

1. Apply EF migrations และรัน backend ที่ `http://localhost:5138`
2. รัน frontend ที่ `http://localhost:3000`
3. ตรวจ `/api/health` และ provider selection
4. สร้าง Google Slides หรือ PDF lesson และเปิดใช้งาน
5. สร้าง session link แล้วเข้าหน้า join/room
6. ทดสอบ readiness ทั้ง click และเสียง
7. เดิน lesson, ปรับ volume, pause/resume และ Push-to-Talk
8. ตรวจ referenced-slide override และการกลับ slide เดิม
9. เปิดหน้า Admin session พร้อมกัน ทดสอบ live question/chat และ history
10. อัปโหลด standalone/lesson document รอ indexing status แล้วทดสอบ RAG
11. จบ session และตรวจ summary

## Known Baseline

- Frontend typecheck ต้องติดตั้ง `@microsoft/signalr` จาก lockfile ก่อน
- Backend build มี EF Core/Npgsql version conflict warning
- Backend tests ที่ไม่มี credentials/network จะ fail หลายรายการ
- ไม่มี automated browser E2E หรือ meaningful API integration tests
