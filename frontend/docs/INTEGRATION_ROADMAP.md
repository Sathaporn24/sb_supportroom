# Integration Roadmap

ระบบเชื่อม real providers แล้วในโค้ด; roadmap ปัจจุบันเน้น production hardening

## P0 — Baseline and Security

1. จัด EF Core/Npgsql versions ให้ตรงกัน
2. แยก unit tests ออกจาก live provider tests และทำให้ default suite hermetic
3. เพิ่ม meaningful API integration tests และ CI
4. ~~เพิ่ม authentication/authorization สำหรับ Admin/API/SignalR~~ ทำ baseline JWT/RBAC แล้ว;
   เพิ่ม integration/security tests และ session revocation/refresh ตาม requirement
5. เพิ่ม rate limiting สำหรับ TTS, voice, upload, reindex และ session questions
6. ~~Enforce session expiry/status ฝั่ง backend~~ ทำแล้ว; เพิ่ม regression/E2E สำหรับ reconnect หลัง expiry

## P1 — Reliability

1. เปลี่ยน document indexing เป็น bounded/durable queue หรือเพิ่ม recovery ของ Pending jobs
2. เพิ่ม vector deletion/reindex semantics เมื่อเอกสารถูกลบหรือย้าย namespace
3. เพิ่ม cancellation/timeouts/health checks สำหรับ providers ภายนอก
4. กำหนด database migration/deployment runbook
5. เพิ่ม retry/recovery UI สำหรับ document indexing failures

## P2 — Quality and Operations

1. Browser E2E สำหรับ admin → join → room → summary
2. Metrics/tracing สำหรับ provider latency, queue depth และ RAG quality
3. ทดสอบ RAG corpus จริงและ tune `RAG_TOP_K`/`RAG_MIN_SCORE`
4. ทดสอบ PDF rendering/document parsers กับไฟล์ผิดรูปแบบและไฟล์ขนาดใหญ่
5. อัปเดต/ลด frontend dependencies ที่เหลือจาก backend เดิม
