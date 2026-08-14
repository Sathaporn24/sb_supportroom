# HANDOFF MASTER — SupportRoom AI

> สถานะ ณ 13 สิงหาคม 2026 บน branch `Dev-gun/Gun` (`1f9bbb1` ก่อนเริ่ม audit รอบนี้)
> เอกสารนี้คือจุดเริ่มต้นส่งมอบงานข้ามทีม ส่วน schema/API/code ยังคงเป็น source of truth
> ทางเทคนิคตามลิงก์ในแต่ละหัวข้อ

## Executive summary

ระบบมี prototype end-to-end ที่ครอบคลุมการสร้างบทเรียน → สร้างลิงก์ → ผู้เรียนเข้าเรียน
1:1 → AI บรรยาย/ตอบคำถามแบบ grounded → CS ดู log/แชต/รีวิวคำตอบ พร้อม multi-company
และหลังบ้านที่มี login 3 role แล้ว โครงหลักเหมาะแก่การแบ่งทีมต่อ แต่ **ยังไม่พร้อม production**
จนกว่าจะเคาะ/ทำ TTS ที่มี SLA, rate limiting, secret/provider settings, deploy/CI, durable indexing,
data-retention/privacy และ RAG evaluation

สถานะรวม: 🟡 **พร้อมส่งทีมออกแบบและพัฒนาต่อภายใต้ decision gates ด้านล่าง**

## สิ่งที่ audit รอบส่งมอบแก้ให้แล้ว

- แยก learner requests ออกจาก admin JWT/`?company=` เพื่อไม่ให้ browser ที่ล็อกอินหลังบ้าน
  พา tenant context ผิดบริษัทเข้าหน้าสาธารณะ
- ลด public payload: ไม่ส่ง source/provider/document IDs, attendance/internal IDs ที่ไม่จำเป็น,
  review metadata และ `UnansweredPoints` ให้ผู้เรียน
- ผูก lesson/PDF page/TTS/voice/chat กับ link + learner session จริง และบล็อกการเขียนหลัง session จบ
- SignalR group ผูก `LearningSession`; agent ต้องมี JWT; sender role/name derive ฝั่ง server
- ผู้เรียนเดิม reconnect ได้หลัง link หมดอายุ แต่ผู้เรียนใหม่/restart ไม่ได้; end/progress เป็น idempotent/guarded
- รักษา `?company=` ในทุกลิงก์หลังบ้านและป้องกันการเปิด token ข้าม company context
- Pin EF Core Relational 10.0.10 เพื่อแก้ assembly conflict และอัปเดตเอกสารเก่าที่ขัดกับโค้ด

Verification ณ รอบนี้: frontend lint/typecheck/build ผ่าน, Vitest 31/31, .NET solution build
0 warning/0 error,
Application unit tests 96/96 และ provider unit tests 21/21 (ไม่รวม integration);
migration/staging smoke/E2E ยังอยู่ใน blockers

## สิ่งที่ทำแล้วจริง

| Capability | สถานะ | หมายเหตุ |
|---|---|---|
| Frontend learner flow | ✅ Implemented | join, resume, room, PTT, chat, recap, learn again |
| Admin work flow | ✅ Implemented | lessons, Google/PDF, documents, links, learner summary, answer review |
| Multi-company isolation | ✅ Implemented | `CompanyId`, EF query filters, Pinecone namespaces |
| Back-office auth/RBAC | ✅ Implemented | JWT; `owner`, `admin`, `cs`; company switcher; user management |
| Link/session split | ✅ Implemented | 1 link → many `LearningSession`; summary คำนวณสด |
| Knowledge ingestion/RAG | 🟡 Implemented with debt | PDF/PPTX/DOCX/XLSX → parse/embed/Pinecone; ไม่มี durable queue/eval |
| Provider abstraction | 🟡 Code-level only | เลือกผ่าน env ตอน startup; ยังไม่มีหน้า settings/runtime switch |
| Database migration | 🟡 Generated, not applied | `20260813140603_SplitLinkAndAddAuth`; ยังไม่ verify กับ Postgres จริง |
| Production operations | 🔴 Missing | ไม่มี CI, Docker/deploy artifact, metrics/tracing, runbook |

## Product decisions ที่เคาะแล้ว — ห้ามทีมเปลี่ยนเงียบ ๆ

1. ห้องเป็นการเรียน 1:1 ที่หน้าตาคล้าย video call ไม่ใช่ group meeting และไม่ส่ง media
   ระหว่างคนจริงด้วย WebRTC
2. ผู้เรียนไม่มี account; ใช้ public link token + browser `learnerKey`; หน้า join เก็บชื่อเท่านั้น
3. หนึ่งลิงก์ใช้ได้หลายคน; “เรียนอีกครั้ง” สร้าง `LearningSession` ใหม่และเก็บประวัติรอบเก่า
4. Grounded tutor ต้องตอบ `not_found` เมื่อไม่มีหลักฐาน ห้ามเดาจากความรู้ทั่วไป
5. Knowledge มี 2 scope: เฉพาะบทเรียน และ global ของบริษัทนั้น; ห้ามข้ามบริษัท
6. บทเรียนใช้ Google Slides หรือ PDF เป็นแหล่งหลัก; เก็บ metadata ไม่ snapshot เนื้อหาลง DB
7. Admin roles ตายตัว: `owner` ทุกบริษัท/ตั้งค่าระบบ, `admin` บริษัทตัวเอง+ผู้ใช้,
   `cs` ทำงานบริษัทตัวเอง; ไม่ทำ permission builder
8. API keys/provider settings **ตาม architecture ปัจจุบัน** เป็นค่าระดับระบบของ School Bright
   และ `owner` เท่านั้น; D-03 คือจุดยืนยันว่าจะคงข้อนี้หรือเปิด BYOK ก่อนสร้าง schema จริง
9. ผู้เรียนเห็นคำถาม/คำตอบของตนเองหลังจบ แต่ไม่เห็นรายการ internal follow-up ของ CS
10. ยังไม่ทำ group room, per-slide event log, review “resolved” state, custom vocabulary per company,
    และการบังคับ `MaxAttendees`

เหตุผลฉบับเต็ม: [`CORE_FEATURE_SPEC.md`](./CORE_FEATURE_SPEC.md),
[`TECH_DECISIONS.md`](./TECH_DECISIONS.md)

## Workstream handoff

### UX/UI

ส่งให้ทีมออกแบบตามชุด UX/UI ต่อไปนี้:

1. [`UX_UI_WIREFRAME_SPEC.md`](./UX_UI_WIREFRAME_SPEC.md) — wireframe และ specification รายหน้า
2. [`UX_UI_WORKFLOWS.md`](./UX_UI_WORKFLOWS.md) — workflow ข้ามหน้า ระบบ สิทธิ์ และ error paths
3. [`UX_UI_HANDOFF.md`](./UX_UI_HANDOFF.md) — route/state inventory และ deliverable checklist

ต้องออกแบบ **ทุก route และทุก state** โดยแบ่งเป็น learner/public และ back office และครอบคลุม
responsive + loading/empty/error/permission/expired/reconnecting/success ไม่ใช่เฉพาะ happy path

สิ่งที่ยังไม่ควรให้ UX ตีความเอง:

- ชื่อแบรนด์/white-label และคำเรียก “ผู้เรียน/ลูกค้า/CS” ต่อ product
- วิธีแสดง privacy/consent ก่อนใช้ไมค์และส่งเสียงออก external provider
- หน้า provider settings ให้ใช้ information architecture ใน
  [`PROVIDER_SETTINGS_SPEC.md`](./PROVIDER_SETTINGS_SPEC.md) แต่ยังไม่ implement จน decision gates ผ่าน
- flow forgot/reset password, company management และ audit log ยังไม่มี product scope ครบ

### Backend / Data

ส่ง [`BACKEND_DB_HANDOFF.md`](./BACKEND_DB_HANDOFF.md), [`schema.dbml`](./schema.dbml), migration,
และ [`frontend/docs/API_CONTRACT.md`](../frontend/docs/API_CONTRACT.md) ให้ทีมพร้อมกัน

กฎสำคัญ: DBML วาดความสัมพันธ์เชิง domain แต่ฐานข้อมูลปัจจุบัน **ไม่มี foreign keys จริง**;
vector อยู่ Pinecone และไฟล์อยู่ local/OBS จึงมี consistency ข้ามระบบที่ต้องออกแบบต่อ

### Knowledge / Answer quality

ส่ง [`KNOWLEDGE_ROADMAP.md`](./KNOWLEDGE_ROADMAP.md) เป็นแกน product/engineering แยกต่างหาก
จากหน้า upload เอกสาร เพราะ “มีไฟล์ในคลัง” ไม่เท่ากับ “ตอบถูกและสอดคล้อง”

## Blockers ก่อน production

| Priority | Blocker | Owner ที่ควรรับ |
|---|---|---|
| P0 | เลือก TTS ที่มี SLA; Edge TTS ปัจจุบันไม่เหมาะ cloud | Product + Backend/Infra |
| P0 | ใส่ rate limit/abuse controls โดยเฉพาะ voice/TTS/join | Backend/Security |
| P0 | ตัดสินวิธีเก็บ/หมุน/encrypt provider secrets และ rollback | Security + Platform |
| P0 | ยืนยัน data residency, privacy notice, consent, retention, deletion | Product/Legal/Security |
| P0 | Apply migration บน staging Postgres + backup/rollback + smoke test | Backend/DBA |
| P1 | Durable indexing/retry + ลบ vectors เมื่อเอกสารถูกลบ | Backend |
| P1 | CI, deterministic dependencies, Docker/deploy artifact | Platform |
| P1 | API integration/E2E tests รวม auth, tenant isolation, learner flow | QA + Engineering |
| P1 | RAG eval set + quality/latency/cost telemetry | AI/Knowledge |
| P1 | Monitoring, alerting, audit event model, support runbook | Platform/Ops |

## Decisions ที่ต้องการจากคุณ

| ID | คำถาม | ค่าแนะนำเริ่มต้น | ผลถ้ายังไม่ตอบ |
|---|---|---|---|
| D-01 | Deploy ที่ Huawei Cloud, Azure หรือ on-prem? | Huawei ถ้า data policy อนุญาต; ไม่เช่นนั้นตามข้อกำหนดลูกค้า | บล็อก TTS/secrets/CI/deploy design |
| D-02 | เสียง/transcript/embedding ออกนอกประเทศหรือข้าม vendor ได้ไหม? | ห้ามสมมติ ต้องยืนยันเป็นลายลักษณ์อักษร | บล็อก provider shortlist และ privacy |
| D-03 | ใครเป็นผู้ถือ/จ่าย API key: School Bright ทั้งระบบ หรือบางบริษัท BYOK? | Shared system key ตาม TD-014 | เปลี่ยน schema, RBAC, billing, UX settings |
| D-04 | Settings เปลี่ยนได้ runtime ทันทีหรือใช้ draft → test → activate? | Draft → test → activate + rollback | Runtime switch ตรง ๆ เสี่ยง outage ทุกบริษัท |
| D-05 | ต้องเก็บ version ของ provider/prompt ต่อคำตอบย้อนหลังหรือไม่? | เก็บ config snapshot/version ต่อ question | ถ้าไม่เก็บ อธิบาย regression/ค่าใช้จ่ายย้อนหลังไม่ได้ |
| D-06 | Retention ของเสียง, transcript, answer, chat, files, audit กี่วัน? | แยก policy ต่อชนิดข้อมูล | บล็อก delete/export/compliance flow |
| D-07 | เป้าหมาย quality ของ Knowledge คืออะไร? | grounded accuracy + citation + not-found precision | ทีมปรับ RAG โดยไม่มีเกณฑ์รับงาน |
| D-08 | ปริมาณ session/วัน และพร้อมกันสูงสุด? | ใส่ estimate ก่อนเลือก scale | บล็อก capacity/rate/cost design |
| D-09 | ต้องมี forgot password/email invite/SSO ใน release แรกไหม? | Password+admin reset ก่อน; SSO ถัดไป | บล็อก auth UX/SMTP/IdP integration |
| D-10 | ต้อง white-label ต่อบริษัทหรือคง School Bright Support? | คงแบรนด์เดียวใน release แรก | บล็อก design system/content tone |

## Acceptance ก่อนส่งทีมลงมือ

- Product owner ลงนาม D-01 ถึง D-08 หรือระบุว่า deferred พร้อม owner/date
- UX มี route/state/role matrix ครบ ไม่ส่งแค่ภาพ dashboard
- Backend ยืนยัน schema + API + auth boundary และ migration rehearsal
- AI/Knowledge มี eval dataset และ baseline ก่อนเปลี่ยน model/chunk/threshold
- QA มี smoke path อย่างน้อย: owner/admin/cs, 2 companies, Google/PDF, new/returning/expired
  learner, voice answer/not_found, chat, review, reindex failure
