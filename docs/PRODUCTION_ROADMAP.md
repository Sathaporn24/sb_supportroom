# PRODUCTION_ROADMAP — เอาระบบขึ้นให้ลูกค้าใช้จริง

> เป้าหมาย: deploy บน Huawei Cloud ให้ลูกค้าจริง (โรงเรียน + SCB) ใช้ได้
> เหตุผลเบื้องหลังแต่ละข้ออยู่ใน [`TECH_DECISIONS.md`](./TECH_DECISIONS.md) — ไฟล์นี้คือ *ลำดับงาน* ไม่ใช่การวิเคราะห์
>
> อัปเดตล่าสุด 11 ส.ค. 2026

---

## Phase 0 — ต้องทำก่อนมีข้อมูลลูกค้าจริง (ทำก่อนอย่างอื่นทั้งหมด)

### 0.1 Multi-tenancy ✅ เสร็จแล้ว (11 ส.ค. 2026)

**ตัดสินใจแล้วและลงมือเสร็จแล้ว: `CompanyId` column + EF Core global query filter** (TD-010 / TD-011 / TD-012)

ยืนยันด้วยการรันจริงและ test 6 ตัวใน `CompanyIsolationTests.cs`

- [x] `CompanyId` ทุก entity + index + migration `AddCompanyId`
- [x] `LessonConfig` unique เป็น `(CompanyId, Slug)` — สองบริษัทตั้งชื่อซ้ำกันได้แล้ว
- [x] global query filter ทุก entity + `ICompanyContext` (unresolved = เห็น 0 แถว)
- [x] `CompanyContextMiddleware` — ⚠️ ชั่วคราวจนกว่าจะมี auth (TD-002)
- [x] เลิกเชื่อ `lessonSlug`/`sessionId` จาก client — voice-question/chat/questions ใช้ token
- [x] rename เป็นกลาง: `RecipientName`, `RecipientOrgName`, `SenderRole` = recipient/agent
- [x] Pinecone namespace `{companyId}:{slug}` และ `{companyId}:kb-global`
- [x] test ยืนยัน isolation 6 ตัว รวมถึงตัวที่ดักว่า entity ใหม่ลืมใส่ filter

**ยังไม่ได้ทำ (ยกไป Phase อื่น):**
- ตรวจค่า `SenderRole` ที่ hub รับจาก client (ตอนนี้รับ string อะไรก็ได้)
- integration test กับ Postgres จริง (ตอนนี้ใช้ EF InMemory)

### 0.2 ยืนยันข้อกำหนดจาก SCB

- [ ] ข้อมูลเสียง/transcript ออกนอกประเทศได้ไหม
      (ตอนนี้เสียง → Gemini, transcript → OpenAI/gateway เมื่อใช้ `openai-rag`)
- [ ] ถ้าไม่ได้ → ต้องย้ายทุกอย่างไป ModelArts/on-prem ซึ่งเปลี่ยนแผนทั้งหมด **รู้ก่อนดีกว่า**
- [ ] ข้อกำหนด data retention / audit log

---

## Phase 1 — ทำให้ deploy ได้จริง

### 1.1 เปลี่ยน TTS 🔴

Edge TTS ถูก Microsoft ปิดกั้นบน datacenter IP — deploy แล้วเสียงจะเงียบ (TD-001)

- [ ] เช็คก่อน: Huawei Cloud มี TTS ภาษาไทยไหม
- [ ] ถ้าไม่มี → เขียน `AzureTtsProvider` (เสียง `th-TH-PremwadeeNeural` ตัวเดิม, SSML rate เดิม)
- [ ] เพิ่ม `"azure"` ใน `TtsProvider.Allowed` + `.env.example`
- [ ] เก็บ `edge` ไว้สำหรับ dev ในเครื่อง
- [ ] ทดสอบจริงจาก IP ปลายทางที่จะ deploy (ไม่ใช่จากเครื่อง dev)

### 1.2 Auth + rate limiting 🔴

- [ ] ต่อ IdP ของบริษัทกับ `/admin/*` และ `/api/admin/*`
- [ ] ใส่ `Microsoft.AspNetCore.RateLimiting` เฉพาะ `/api/tts` + `/api/voice-question`
      (สองเส้นนี้มีค่าใช้จ่ายจริงต่อครั้ง)
- [ ] backend บังคับ session expiry ด้วย ไม่ใช่แค่ frontend
- [ ] `ALLOW_DATA_RESET=false` ใน production
- [ ] `ALLOWED_ORIGINS` ระบุ domain จริง
- [ ] หมุน API key ที่เคยอยู่ใน git history

### 1.3 Docker + CI

- [ ] แยก test ที่ยิง provider จริงด้วย xUnit trait (`Category=Integration`)
- [ ] GitHub Actions: build + lint + typecheck + unit tests ทั้งสองฝั่ง
- [ ] Dockerfile ฝั่ง API ⚠️ ต้องมี native PDFium สำหรับ `PDFtoImage`
- [ ] Dockerfile ฝั่ง frontend
- [ ] docker compose สำหรับรัน local ครบชุด

### 1.4 Deploy Huawei Cloud

- [ ] RDS for PostgreSQL + รัน migration
- [ ] CCE หรือ container service สำหรับ API + frontend
- [ ] OBS bucket (`DOCUMENT_STORAGE_PROVIDER=huawei-obs`)
- [ ] จัดการ secrets (ห้ามใช้ `.env` ใน prod)
- [ ] ยืนยัน outbound ไปยัง Gemini / TTS / Pinecone ใช้งานได้จาก network ปลายทาง

---

## Phase 2 — ให้ดูแลต่อได้

### 2.1 มองเห็นระบบ

- [ ] Seq หรือ OTLP endpoint รับ Serilog (มี `CorrelationId` อยู่แล้ว ต่อได้เลย)
- [ ] นับอัตราแต่ละ `answerStatus` — ตอบคำถาม "AI ตอบได้จริงแค่ไหน" ที่ยังไม่มีใครตอบได้
- [ ] วัด latency แต่ละขั้นของ voice pipeline
- [ ] วัดต้นทุนต่อ 1 session (TTS อักขระ + token + embedding)

### 2.2 ความทนทาน

- [ ] Startup recovery: หา `DocumentResource` ที่ค้าง `pending` แล้ว enqueue ใหม่ (TD-003 option A)
- [ ] ลบ vector ตอนลบเอกสาร ด้วย ID prefix (TD-004 — chunk id เป็น `{documentId}-{chunkId}` อยู่แล้ว)
- [ ] pin `GEMINI_MODEL` / `OPENAI_MODEL` เป็นเวอร์ชันชัดเจนใน prod (TD-009)

### 2.3 เก็บกวาด (TD-008)

- [ ] ลบ dependency ตกค้าง frontend: `googleapis`, `msedge-tts`, `zod`, `client-only`,
      `bufferutil`, `utf-8-validate`
- [ ] ลบไฟล์ตกค้างที่ repo root: `node_modules/`, `.next/`, `next-env.d.ts`,
      `tsconfig.tsbuildinfo`, `public/`
- [ ] ตรึง `PackageReference` ที่เป็น `3.*` / `0.*` / `5.*`
- [ ] แก้ EF Core version conflict (MSB3277 ×5)
- [ ] ตัดสินใจเรื่อง `IsDelete`/`DeletedAt` — บังคับใช้จริง หรือลบทิ้ง

---

## Phase 3 — เมื่อโตขึ้นเท่านั้น (อย่าทำก่อน)

- Hangfire แทน in-memory queue
- SignalR + Redis backplane (จำเป็นเมื่อเกิน 1 instance)
- Distributed cache แทน `MemoryCache`
- Pre-render หน้า PDF ตอนอัปโหลด
- Reranking / hybrid search ใน RAG
- E2E tests (Playwright)

---

## ลำดับที่แนะนำ

```
0.1 multi-tenancy  ─┐
0.2 ถาม SCB        ─┤ ทำคู่กัน ผลจาก 0.2 อาจเปลี่ยน 1.1
                    ↓
1.1 TTS ─→ 1.2 auth ─→ 1.3 docker+CI ─→ 1.4 deploy
                    ↓
              2.x ทยอยทำหลังขึ้น prod
```

**หลักการ:** Phase 0 ทำก่อนเสมอเพราะต้นทุนจะพุ่งขึ้นทันทีที่มีข้อมูลลูกค้าจริง
ส่วน Phase 3 ห้ามทำล่วงหน้า — รอจนมีปัญหาจริงก่อน
