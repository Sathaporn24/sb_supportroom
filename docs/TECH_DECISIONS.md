# TECH_DECISIONS — บันทึกการตัดสินใจเชิงเทคนิค

> เอกสารนี้มีชีวิต เพิ่มรายการใหม่ต่อท้ายเมื่อมีการตัดสินใจสำคัญ และอัปเดต `Status` เมื่อสถานะเปลี่ยน
> ห้ามลบรายการเก่า — ให้เปลี่ยนสถานะเป็น `Superseded` แล้วชี้ไปรายการใหม่
>
> รูปแบบ: Decision → Problem → Options → Comparison → Recommendation → Status
> รายการ ADR-0001 ถึง ADR-0005 (ยุค Next.js fullstack) อยู่ที่ `frontend/docs/adr/`

**สถานะที่ใช้:** `Proposed` · `Accepted` · `Rejected` · `Revisit Later` · `Superseded`

---

## TD-001 — เปลี่ยน TTS provider จาก Edge TTS ไปเป็นบริการที่มี SLA

**Problem**
`EdgeTTS.DotNet` เรียก Microsoft Read-Aloud แบบไม่เป็นทางการ ธันวาคม 2025 Microsoft เพิ่ม
anti-abuse token และกรอง IP ของ datacenter — รายงานจากชุมชนตรงกันว่า IP ของ cloud provider
เชื่อมต่อได้แต่ไม่ได้รับ audio frame กลับมา ร่องรอยการรับมือในโค้ด (แบ่ง chunk 180 ตัวอักษร,
retry 2 ครั้ง, timeout 12 วินาที, คอมเมนต์ถึง 502 จริงหลังรอ 24–46 วินาที) ตรงกับอาการนี้ทั้งหมด
เสียงคือหัวใจของสินค้า ถ้าเงียบก็ไม่มีบทเรียน

**Options**

- **A — Azure Speech Neural TTS** เสียง `th-TH-PremwadeeNeural` ตัวเดียวกับที่ใช้อยู่ มี SSML rate
- **B — Google Cloud TTS** อยู่ในระบบ Google เดียวกับ Slides/Gemini เสียงไทยคนละตัว
- **C — Gemini TTS / Live API** ลดจำนวน vendor
- **D — คงเดิม + residential proxy**
- **E — ElevenLabs Text to Speech** commercial API มี SLA เสียงไทยคนละตัวจากเดิม

**Comparison**

| | A: Azure | B: Google | C: Gemini TTS | D: คงเดิม | E: ElevenLabs |
|---|---|---|---|---|---|
| ความซับซ้อน | ต่ำมาก (1 คลาส) | ต่ำมาก | ต่ำ–กลาง | ไม่มี | ต่ำมาก (1 คลาส, HTTP POST เดียว) |
| ค่าใช้จ่าย | ~$16/1M อักขระ | ~$16/1M อักขระ | ตาม token | $0 + ค่า proxy | ตาม subscription tier |
| เสียงเปลี่ยนไหม | **ไม่เปลี่ยนเลย** | เปลี่ยน | เปลี่ยน | ไม่เปลี่ยน | เปลี่ยน |
| ความน่าเชื่อถือ | SLA | SLA | SLA | ไม่มี | SLA |
| Vendor lock-in | ต่ำ (`ITtsProvider` กันไว้แล้ว) | ต่ำ | ต่ำ | — | ต่ำ (`ITtsProvider` กันไว้แล้ว) |
| ดูแลต่อ | ต่ำ | ต่ำ | ต่ำ | สูง (เปราะ) | ต่ำ |
| รองรับภาษาไทยจริง | ใช่ | ใช่ | ใช่ | ใช่ | เฉพาะโมเดล `eleven_v3`/`eleven_v3_conversational` — `eleven_multilingual_v2` และ `eleven_flash_v2.5` **ไม่รองรับไทย** แม้ชื่อจะบอกว่า multilingual (ตรวจสอบสด ส.ค. 2026 กับ elevenlabs.io/docs) |

**Recommendation**
**Option E (ElevenLabs)** ถูกนำมาใช้งานจริงแล้ว — implement เป็น `ElevenLabsTtsProvider`
(`SupportRoom.Providers.Tts`) ตาม abstraction เดิม (`ITtsProvider`), เพิ่ม `"elevenlabs"` ใน
`TtsProvider.Allowed`, เพิ่ม factory case ใน `TtsProviderFactory`, และอ่าน credential ผ่าน
`ExternalServiceEnv.GetElevenLabs()` (`ELEVENLABS_API_KEY`, `ELEVENLABS_VOICE_ID` บังคับ,
`ELEVENLABS_MODEL_ID` optional)

⚠️ **ต้องใช้โมเดลตระกูล v3 เท่านั้น** — เป็นตระกูลเดียวของ ElevenLabs ที่รองรับภาษาไทย
(ตรวจสอบสดกับเอกสารจริงที่ elevenlabs.io/docs) `eleven_multilingual_v2` และ `eleven_flash_v2.5`
ไม่รองรับไทยทั้งที่ชื่อบอกว่า multilingual — ห้ามเปลี่ยนไปโมเดลอื่นโดยไม่ตรวจสอบก่อนว่ารองรับไทย

🔄 **อัปเดต 2026-08-24 (ดู TD-016 สำหรับตัวเลขเต็ม):** เดิมตั้ง default เป็น `eleven_v3` แต่วัดสด
พบว่า **`eleven_v3_conversational`** (โมเดลย่อยของตระกูลเดียวกัน รองรับไทยเหมือนกัน) เร็วกว่า
~2.5 เท่า (763 ตัวอักษรไทย: 9.4s → 3.8s) ถูกกว่า ($0.05 vs $0.10 ต่อ 1K ตัวอักษร) โดยคุณภาพเสียง
ฟังไม่ต่างกัน — `ExternalServiceEnv.GetElevenLabs()` (`ServerDefaults.cs`) เปลี่ยน default
`ELEVENLABS_MODEL_ID` เป็น `eleven_v3_conversational` แล้ว ยังตั้งค่าเองผ่าน env var ทับได้ตามเดิม

Option A (Azure) ยังคงเป็นทางเลือกที่ให้เสียงเดิมเป๊ะถ้าทีมต้องการเปรียบเทียบต่อ แต่ยังไม่ได้
ตัดสินใจเลือก A — ทีมเลือกทดสอบ ElevenLabs ก่อนโดยยังไม่ตัดการใช้งานจริง (`TTS_PROVIDER` ใน
`.env` การ deploy จริงยังเป็น `edge`)

**Status** `Accepted / Implemented, not yet cut over` — provider เขียนเสร็จแล้ว รอผู้พัฒนาใส่
`ELEVENLABS_API_KEY`/`ELEVENLABS_VOICE_ID` ของตัวเองและตั้ง `TTS_PROVIDER=elevenlabs` เพื่อทดสอบ
ก่อนตัดสินใจ cut over การ deploy จริง

---

## TD-002 — ป้องกัน admin surface และใส่ rate limiting ก่อน deploy

**Problem (baseline ตอนตัดสินใจ; auth แก้แล้วใน TD-014)**
เดิมไม่มี authentication และ `app.UseAuthorization()` เป็น no-op เมื่อ deploy ออกไป ใครก็ตามที่รู้ URL
สามารถอ่านบทเรียนทั้งหมด สร้าง/ลบข้อมูล เรียก `/api/admin/reset` (ถ้า `ALLOW_DATA_RESET=true`
ซึ่ง `.env.example` ตั้งไว้เป็น `true`) และยิง `/api/tts` กับ `/api/voice-question` ที่มีค่าใช้จ่ายจริงได้ไม่จำกัด

**Options**

- **A — reverse proxy / IP allowlist / VPN** ป้องกัน `/admin/*` ชั่วคราว
- **B — JWT หรือ cookie auth ในตัวของ ASP.NET Core เฉพาะ admin surface** + built-in rate limiting
- **C — Identity provider ภายนอก** (Clerk / Auth0 / Keycloak / SSO องค์กร)

**Comparison**

| | A: proxy | B: auth ในตัว | C: IdP ภายนอก |
|---|---|---|---|
| ความซับซ้อน | ต่ำมาก | ต่ำ–กลาง | กลาง–สูง |
| ค่าใช้จ่าย | $0 | $0 | รายเดือน หรือค่า ops |
| Audit ว่าใครทำอะไร | ไม่ได้ | ได้ | ได้ |
| เข้ากับ stack | ไม่แตะโค้ด | ในตัว framework | ต้องต่อเชื่อม |
| เหมาะกับผู้ใช้ภายในไม่กี่คน | ใช่ | ใช่ | เกินจำเป็น |

**Recommendation**
ทำ **A ทันที** เป็นมาตรการก่อน deploy แล้วตามด้วย **B** เป็นทางแก้จริง
ส่วน rate limiting ให้ใช้ `Microsoft.AspNetCore.RateLimiting` ที่มากับ framework
ใส่ policy เฉพาะ `/api/tts` และ `/api/voice-question` — งานเล็กมาก คุ้มมาก
`ApiErrorCode.Unauthorized` ถูกเตรียมไว้เป็นจุดเสียบอยู่แล้ว
เลือก **C** ก็ต่อเมื่อยืนยันได้ว่า School Bright มี IdP ที่ใช้ร่วมได้อยู่แล้ว — **ต้องถามทีมก่อน**

**Status** `Accepted / Partially Implemented` (13 ส.ค. 2026) — JWT/RBAC/company authorization
ทำแล้วใน TD-014; rate limiting และ production abuse controls ยังไม่ทำ

---

## TD-003 — ทำให้คิว indexing เอกสารทนต่อการ restart

**Problem**
`IBackgroundTaskQueue` เป็น in-memory channel ถ้า process restart ระหว่างมีงานค้าง
`DocumentResource` จะค้างที่ `pending` ตลอดไป ไม่มีใครรู้และไม่มีทาง retry
นอกจาก re-upload หรือสั่ง `/api/admin/reindex` ทั้งระบบ

**Options**

- **A — Startup recovery** ตอน start ค้นหา row ที่ `pending` แล้ว enqueue ใหม่ (ยังใช้ queue เดิม)
- **B — Hangfire + PostgreSQL storage** เปลี่ยนให้ persist จริง
- **C — outbox table เขียนเอง**
- **D — RabbitMQ / Temporal**

**Comparison**

| | A: recovery | B: Hangfire | C: outbox เอง | D: broker |
|---|---|---|---|---|
| ความซับซ้อน | ต่ำมาก | ต่ำ–กลาง | กลาง | สูง |
| Dependency ใหม่ | ไม่มี | 1 package + ตารางใน DB เดิม | ไม่มี | service ใหม่ |
| Retry / backoff | ไม่มี | มีในตัว | เขียนเอง | มี |
| มองเห็นงานที่ค้าง | ไม่ได้ | dashboard | เขียนเอง | มี |
| แก้ปัญหาที่มีจริงวันนี้ | ได้ | ได้ครบ | ได้ | ได้ (เกินจำเป็น) |

**Recommendation**
ทำ **A ก่อน** เพราะแก้เคสหลัก (restart) ด้วยโค้ดไม่กี่บรรทัดและไม่เพิ่มอะไรเลย
เลื่อนไป **B** เมื่อเริ่มต้องการเห็น/สั่ง retry งานที่ล้มเหลว หรือเมื่อแยก worker ออกจาก API
**C** คือการเขียน Hangfire ฉบับด้อยกว่า — ไม่แนะนำ

**Status** `Proposed`

---

## TD-004 — ลบ vector ใน Pinecone เมื่อลบเอกสาร

**Problem**
`DocumentResourceService.DeleteAsync()` ลบไฟล์ในสตอเรจและ row ใน DB แต่ไม่ลบ vector
ระบบจึงยังตอบคำถามจากเอกสารที่ถูกลบไปแล้วได้ — เป็นปัญหาความถูกต้องและอาจเป็นปัญหาเชิงนโยบายข้อมูล

**Options**

- **A — ลบด้วย ID prefix บน Pinecone** โค้ดสร้าง chunk id เป็น `{documentId}-{chunkId}` อยู่แล้ว
  ซึ่งตรงกับรูปแบบ hierarchical id ที่ Pinecone แนะนำสำหรับการลบแบบ prefix พอดี
- **B — ลบด้วย metadata filter** (`documentId` ถูกเก็บใน metadata อยู่แล้ว)
- **C — เก็บ vector id ที่ upsert ไว้ในตาราง แล้วลบตามรายการ**
- **D — ย้ายไป pgvector** ให้การลบอยู่ใน transaction เดียวกับการลบเอกสาร

**Comparison**

| | A: prefix | B: metadata filter | C: เก็บ id | D: pgvector |
|---|---|---|---|---|
| ใช้ได้กับ Pinecone serverless | ได้ | **ไม่ได้** — serverless ไม่รองรับ | ได้ | ไม่เกี่ยว |
| ความซับซ้อน | ต่ำ | — | กลาง (migration + schema) | กลาง–สูง |
| แก้ที่ราก | ไม่ | — | ไม่ | ใช่ (transactional) |
| ต้อง re-index | ไม่ | — | ไม่ | ใช่ |

**Recommendation**
**Option A** — ตรงไปตรงมาที่สุด รูปแบบ id ที่มีอยู่รองรับพอดีโดยบังเอิญที่ดี
เพิ่ม `DeleteByPrefixAsync` ใน `IKnowledgeIndexProvider` แล้วเรียกจาก `DeleteAsync`
ทำเป็น best-effort ตามแบบเดียวกับ indexing (Pinecone ล่มไม่ควรทำให้ลบเอกสารไม่ได้)
**Option B ใช้ไม่ได้** — Pinecone serverless ไม่รองรับการลบด้วย metadata filter (ตรวจสอบ ส.ค. 2026)
พิจารณา **D** แยกต่างหากใน TD-005

**Status** `Proposed` — ความยากต่ำ คุณค่าชัดเจน เหมาะเป็นงานแรก ๆ

---

## TD-005 — พิจารณาย้าย vector store จาก Pinecone ไป pgvector

**Problem**
Pinecone เป็นระบบที่สองที่ต้อง sync กับ Postgres ด้วยมือ (ที่มาของ TD-004) และคิดขั้นต่ำ
~$50/เดือนไม่ว่าจะเก็บกี่ vector ขณะที่ปริมาณจริงของโปรเจกต์นี้ (1 chunk ต่อสไลด์ + ต่อหน้าเอกสาร)
อยู่ระดับหมื่น vector เท่านั้น

**Options**

- **A — คง Pinecone**
- **B — ย้ายไป pgvector บน PostgreSQL เดิม**
- **C — Qdrant / Weaviate**

**Comparison**

| | A: Pinecone | B: pgvector | C: Qdrant/Weaviate |
|---|---|---|---|
| ความซับซ้อนตอนย้าย | ไม่มี | provider ใหม่ + migration + re-index | เท่า ๆ B แต่ไม่ได้ประโยชน์ transaction |
| ค่าใช้จ่าย | ~$50+/เดือนขั้นต่ำ | ~$0 ส่วนเพิ่ม | ค่า host |
| ประสิทธิภาพที่ขนาดนี้ (<5M vector) | เร็ว | เทียบเท่า | เร็ว |
| Transactional consistency | ไม่มี | **มี** | ไม่มี |
| ภาระ ops | ต่ำสุด | ดูแล index เอง | สูงสุด |
| แก้หนี้ TD-004 ที่ราก | ไม่ | **ใช่** | ไม่ |

**Recommendation**
**ยังไม่ย้ายตอนนี้** — Pinecone ทำงานได้และการย้ายต้อง re-index ทั้งหมด
แต่ควรทบทวนเมื่อเกิดข้อใดข้อหนึ่ง: (ก) ค่า Pinecone เริ่มไม่สมเหตุสมผลกับปริมาณจริง
(ข) การ sync ระหว่างสองระบบทำให้เกิดบั๊กซ้ำอีก (ค) ต้องการ query ที่ผสม filter เชิงสัมพันธ์กับ vector
`IKnowledgeIndexProvider` ทำให้การย้ายเป็นงานจำกัดขอบเขต — ไม่ต้องรีบตัดสินใจ

**Status** `Revisit Later`

---

## TD-006 — ตั้ง CI และสร้าง deployment artifact

**Problem**
`.github/workflows/` มีอยู่แต่ว่างเปล่า ไม่มี Dockerfile ไม่มี compose ไม่มี IaC
การตรวจคุณภาพทั้งหมดต้องรันมือ และไม่มีอะไรกันไม่ให้โค้ดที่ build ไม่ผ่านถูก merge
อุปสรรคเฉพาะกิจ: test บางชุดใน `Application.Tests`/`Providers.Tests` ใช้ `RealHttpClientFactory`
ยิงไปยัง provider จริง จึงรันใน CI ไม่ได้ถ้าไม่แยกออกก่อน

**Options**

- **A — CI อย่างเดียว** (build + typecheck + lint + unit tests ทั้งสองฝั่ง)
- **B — CI + Dockerfile + compose**
- **C — CI/CD เต็มรูปแบบพร้อม deploy อัตโนมัติ**

**Comparison**

| | A | B | C |
|---|---|---|---|
| ความซับซ้อน | ต่ำ | กลาง | สูง |
| ต้องรู้ปลายทาง deploy ก่อน | ไม่ | ไม่ | **ใช่** |
| งานที่ต้องทำก่อน | แยก unit/integration ด้วย xUnit trait | + native PDFium ใน image | + secrets, environment |

**Recommendation**
**A แล้วต่อด้วย B** — ✅ งานที่ต้องทำก่อน (แยก test ที่ยิงของจริงด้วย xUnit trait) **ทำเสร็จแล้ว
13 ส.ค. 2026**: 11 test ติด `[Trait("Category","Integration")]` และ
`dotnet test --filter "Category!=Integration"` ผ่าน 76/76 — CI มีคำสั่งที่เขียวสะอาดให้ใช้แล้ว
เลื่อน **C** ไว้จนกว่าจะสรุปได้ว่า deploy ที่ไหน (Azure? Huawei Cloud? on-prem?) — **ต้องถามทีม**
⚠️ ตอนทำ Dockerfile: `PDFtoImage` ต้องมี native PDFium ใน image

**Status** `Proposed`

---

## TD-007 — สร้างชุดวัดคุณภาพคำตอบ RAG

**Problem**
`RAG_TOP_K` และ `RAG_MIN_SCORE` (default 3 และ 0.4) ถูกปรับด้วยการอ่าน log ตามคอมเมนต์ในโค้ด
ไม่มีวิธีตอบคำถามว่า "เปลี่ยนโมเดล/threshold แล้วดีขึ้นหรือแย่ลง" ได้อย่างมีหลักฐาน
ยิ่งมี provider สามแบบ (`gemini`, `gemini-rag`, `openai-rag`) ยิ่งเลือกไม่ได้ว่าอันไหนดีกว่า

**Options**

- **A — eval set ใน repo** ไฟล์คำถามจริง + สไลด์ที่ควรอ้างอิง + `answerStatus` ที่ควรได้ รันเป็น test
- **B — Langfuse (self-host)** trace + dataset + eval พร้อมเห็น latency/token/cost ต่อคำถาม
- **C — ไม่ทำอะไร ปรับตามความรู้สึกต่อไป**

**Comparison**

| | A: eval set | B: Langfuse | C: เดิม |
|---|---|---|---|
| ความซับซ้อน | ต่ำ | กลาง | ไม่มี |
| ค่าใช้จ่าย | โควตา API ตอนรัน | self-host ฟรี + เวลาดูแล | $0 |
| ตอบว่า "ดีขึ้นไหม" ได้ | ได้ | ได้ | ไม่ได้ |
| เห็น traffic จริงในการใช้งาน | ไม่ | ได้ | ไม่ |

**Recommendation**
เริ่มที่ **A** — 20–30 คำถามจริงจาก session ที่ผ่านมาก็พอเปลี่ยนการปรับจูนจาก "เดา"
เป็น "วัดได้" แล้ว และทำให้เทียบ provider สามแบบได้ตรง ๆ
พิจารณา **B** เมื่อมี traffic จริงมากพอที่การมองเห็น production จะคุ้ม
ก้าวแรกที่ถูกที่สุด: log อัตราแต่ละ `answerStatus` และ latency ของแต่ละขั้นใน voice pipeline

**Status** `Proposed`

---

## TD-008 — เก็บกวาดหนี้เล็กที่กระทบความน่าเชื่อถือของโค้ด

**Problem**
หลายอย่างเล็กแต่ทำให้คนอ่านเข้าใจผิดหรือทำให้ build ไม่ deterministic:

1. dependency ตกค้างใน frontend: `googleapis`, `msedge-tts`, `zod`, `client-only`,
   `bufferutil`, `utf-8-validate` (ไม่มีโค้ดเรียกใช้)
2. ไฟล์ตกค้างที่ repo root: `node_modules/`, `.next/`, `next-env.d.ts`,
   `tsconfig.tsbuildinfo`, `public/` (เหลือจากตอนย้ายเป็น monorepo)
3. ~~EF Core version conflict~~ — แก้แล้วโดย pin EF Relational 10.0.10 ที่ Application project
4. `PackageReference` แบบ floating: `AWSSDK.S3 3.*`, `PdfPig 0.*`,
   `DocumentFormat.OpenXml 3.*`, `PDFtoImage 5.*`
5. `IsDelete`/`DeletedAt` มีในทุก entity แต่ไม่มีพฤติกรรม soft-delete จริง (ลบจริงทุกครั้ง
   ไม่มี global query filter)

**Options**

- **A — ทำทั้งหมดรวดเดียวเป็น PR เก็บกวาดก้อนเดียว**
- **B — ทยอยแก้ตอนที่แตะไฟล์นั้น ๆ อยู่แล้ว**
- **C — ปล่อยไว้**

**Recommendation**
**A** สำหรับข้อ 1–4 เพราะเป็นงานกลไก ตรวจสอบได้ด้วย build/test ที่มีอยู่ และให้ผลชัดเจน
(ขนาด install เล็กลง, build ซ้ำได้ผลเดิม, warning หายไป)
ข้อ 5 ต้องตัดสินใจก่อนว่าจะ **บังคับใช้ soft-delete จริง** (เพิ่ม global query filter — เปลี่ยนพฤติกรรม)
หรือ **ลบฟิลด์ทิ้ง** (ต้องมี migration) — อย่าปล่อยให้กำกวมต่อไป ให้แยกเป็นรายการของตัวเองเมื่อได้ข้อสรุป
สำหรับข้อ 3: ตรึงเวอร์ชัน EF Core ให้ตรงกับที่ Npgsql ดึงมา หรือรอ Npgsql ที่รองรับ 10.0.10

**Status** `Proposed`

---

## TD-009 — ตรึงเวอร์ชันโมเดลใน production

**Problem**
`GEMINI_MODEL` มีค่า default เป็น `gemini-flash-latest` ซึ่งเป็น alias ที่เลื่อนตามรุ่นใหม่
พฤติกรรมของการถอดเสียงและการตอบจึงเปลี่ยนได้โดยไม่มี commit ใด ๆ ในระบบ — คุณภาพเสียหาย
โดยไม่มีการเปลี่ยนแปลงให้ย้อนกลับ (คอมเมนต์ในโค้ดบันทึกไว้แล้วว่า `gemini-1.5-flash` เคยถูกปลดระวาง)

**Options**

- **A — คง alias ไว้** ได้ของใหม่อัตโนมัติ
- **B — ตรึงเวอร์ชันชัดเจนใน production** แล้วเลื่อนอย่างตั้งใจ
- **C — ตรึงใน production, ใช้ alias ใน development**

**Recommendation**
**C** — production ต้องทำซ้ำได้ ส่วน development ได้เห็นของใหม่ก่อน ต้องทำ TD-007 (eval set)
ควบคู่กันจึงจะรู้ว่าการเลื่อนเวอร์ชันแต่ละครั้งดีขึ้นจริงหรือไม่
ใช้หลักเดียวกันกับ `OPENAI_MODEL` และ `OPENAI_EMBEDDING_MODEL`
⚠️ การเปลี่ยน embedding model **ต้อง re-index ทั้งหมด** — vector ที่ embed คนละโมเดล/คนละ vendor
ค้นหาข้ามกันไม่ได้ (โค้ดเตือนเรื่องนี้ไว้แล้วใน `KnowledgeProvider`)

**Status** `Proposed`

---

## TD-010 — แยกข้อมูลระหว่างลูกค้า (multi-tenancy)

**Problem**
ระบบออกแบบมาสำหรับลูกค้ารายเดียว ตอนนี้มี SCB สนใจแล้ว จึงต้องรองรับหลาย company
ปัจจุบันมีจุดที่ข้อมูลจะปนกันทันที 3 จุด: `LessonConfig.Slug` unique ทั้งระบบ,
Pinecone namespace ใช้ `lessonSlug` ตรง ๆ, และ `kb-global` namespace เดียวที่ทุกบทเรียนใช้ร่วมกัน

**Options**

- **A — `CompanyId` column + EF Core global query filter** ใช้ database เดียว
- **B — แยก schema หรือ database ต่อ company**
- **C — A + ย้าย vector จาก Pinecone มา pgvector** ให้ isolation อยู่ใต้กลไกเดียวกันทั้งหมด

**Comparison**

| | A: CompanyId | B: แยก DB | C: A + pgvector |
|---|---|---|---|
| ความซับซ้อน | ต่ำ | สูง | กลาง |
| ops | จัดการที่เดียว | migration ต้องรันทุก company | จัดการที่เดียว |
| ความเสี่ยงหลัก | ลืมใส่ filter = รั่ว | ต้นทุนต่อ company สูง | เท่ากับ A |
| ครอบคลุม vector ด้วยไหม | **ไม่** ต้องแยก namespace ต่างหาก | **ไม่** เช่นกัน | **ใช่** |
| อธิบายกับธนาคาร | ต้องอธิบายกลไกในโค้ด | "คนละฐาน" เข้าใจง่าย | ต้องอธิบายกลไกในโค้ด |

**Recommendation**
**Option A** — เหมาะกับขนาดงานปัจจุบันและมีแค่ 6 ตาราง ไม่มี FK จริงสักเส้น
การเพิ่มคอลัมน์จึงเป็น migration เดียว

ข้อบังคับสองข้อที่มาพร้อมการเลือก A:

1. **ต้องใช้ EF Core global query filter** ประกาศครั้งเดียวใน `OnModelCreating`
   ห้ามไล่ใส่ `WHERE CompanyId = ...` เองรายจุด เพราะฐานข้อมูลไม่มี FK/constraint
   ช่วยอะไรเลย โค้ดคือด่านเดียว
2. **ต้องแก้ Pinecone namespace แยกต่างหาก** เพราะอยู่นอกฐานข้อมูล — ข้อนี้ Option B
   ก็ไม่ได้แก้ให้อัตโนมัติเหมือนกัน (เป็นเหตุผลที่ B ไม่คุ้มกับความยากที่เพิ่มมา)

⚠️ ทบทวนใหม่ถ้า SCB ระบุว่าต้องการ physical data separation — ถ้าบังคับจริงต้องไป B
ตั้งแต่แรก จะได้ไม่ทำสองรอบ (ดู PRODUCTION_ROADMAP §0.2)
ส่วน Option C ยังเปิดไว้ ทำต่อจาก A ได้โดยไม่เสียของ (ดู TD-005)

**Status** `Accepted` — ลงมือเสร็จแล้ว 11 ส.ค. 2026 (migration `AddCompanyId` + `CompanyIsolationTests`)
ทบทวนใหม่ถ้า SCB ระบุว่าต้องการ physical data separation

---

## TD-011 — ที่มาของ companyId ในแต่ละ request

**Problem**
เลือก TD-010 Option A แล้ว แต่ยังไม่ได้ตอบว่า **แต่ละ request รู้ได้อย่างไรว่าเป็น company ไหน**
ระบบมีผู้ใช้สองแบบที่ต่างกันสิ้นเชิง: ฝั่ง admin จะมี auth (TD-002) แต่ **ฝั่งคุณครูไม่มี auth เลย**
มีแค่ลิงก์ที่มี `TrainingSession.Token`

ที่สำคัญกว่านั้น โค้ดปัจจุบันรับ `lessonSlug` มาจาก client แล้วใช้ค้นหาบทเรียนตรง ๆ
โดยไม่ตรวจว่าตรงกับ session ที่อ้างมาหรือไม่:

- `GET /api/lessons/{slug}` ค้นด้วย slug ล้วน ไม่มีบริบท session
- `POST /api/voice-question` เรียก `GetTeachingContentBySlugAsync(input.LessonSlug)`
  แยกจาก `input.SessionId` ทั้งสองค่ามาจาก client และไม่เคยถูกตรวจว่าเป็นคู่กันจริง

วันนี้ไม่มีผล เพราะมี company เดียว แต่เมื่อ slug ไม่ unique ทั้งระบบแล้ว
**นี่กลายเป็นช่องอ่านข้ามลูกค้า** — ส่ง sessionId ของตัวเองคู่กับ slug ของอีก company

**Options**

- **A — resolve companyId จาก session token** ทุก endpoint ฝั่งคุณครูรับ token/sessionId
  แล้ว derive ทั้ง companyId และ lesson จากแถวนั้น เลิกเชื่อ `lessonSlug` ที่ client ส่งมา
- **B — ให้ client ส่ง companyId มาด้วย** ง่ายที่สุด แต่ client ปลอมได้ = ไม่ใช่การป้องกัน
- **C — แยกด้วย subdomain/host header** เช่น `scb.supportroom.app`
- **D — ผูกกับ auth อย่างเดียว** ใช้ไม่ได้กับฝั่งคุณครูที่ไม่มี auth

**Comparison**

| | A: จาก token | B: client ส่งมา | C: subdomain | D: auth |
|---|---|---|---|---|
| ปลอมได้ไหม | ไม่ (token คือ credential อยู่แล้ว) | **ปลอมได้** | ไม่ แต่ต้องคู่กับอย่างอื่น | ไม่ |
| ใช้กับฝั่งคุณครูได้ | ได้ | ได้ | ได้ | **ไม่ได้** |
| ต้องแก้ frontend | น้อย | น้อย | ต้องตั้ง DNS/cert ต่อ company | — |
| ความซับซ้อน | ต่ำ–กลาง | ต่ำ | กลาง–สูง | — |

**Recommendation**
**Option A สำหรับฝั่งคุณครู + auth claim สำหรับฝั่ง admin**

`TrainingSession.Token` เป็นความลับที่เดายากและทำหน้าที่เป็น credential อยู่แล้ว
จึงเป็นแหล่งความจริงที่ถูกต้องสำหรับ company ของ request นั้น

งานที่ตามมาซึ่งใหญ่กว่าการเพิ่มคอลัมน์:
- `GET /api/lessons/{slug}` ต้องรับบริบท session (หรือแยกเป็น endpoint ที่รับ token)
- `POST /api/voice-question` ต้องหา lesson **ผ่าน session** ไม่ใช่ผ่าน slug ที่ client ส่ง
- `TrainingSession.LessonSlug` ที่ denormalized ไว้ ต้องไล่แก้ให้ครบคู่กับ `LessonConfig.Slug`
- SignalR `JoinSession` ตรวจ token อยู่แล้ว — ต้องเพิ่มการตรวจ companyId ให้สอดคล้อง

**Status** `Accepted` — ลงมือเสร็จแล้ว 11 ส.ค. 2026

---

## TD-012 — ทำคำเรียกให้เป็นกลาง แทนการทำ vocabulary ต่อ company

**Problem**
schema และ UI เขียนด้วยภาษาของ School Bright ล้วน — `TeacherName`, `SchoolName`,
`SenderRole = "teacher"`, และสคริปต์เสียงที่ hardcode ว่า `สวัสดีค่ะคุณครู{ชื่อ}`
พอ SCB เอาไปใช้ ระบบจะทักลูกค้าธนาคารว่า "คุณครู" และเก็บชื่อสาขาไว้ในคอลัมน์ชื่อ `SchoolName`

**Options**

- **A — เปลี่ยนคำให้เป็นกลาง ตัดการทักทายรายบุคคลออก** ไม่มีอะไรตั้งค่าได้
- **B — ตาราง `Company` เก็บ vocabulary ต่อ company** (`RecipientTerm`, `GreetingScript` ฯลฯ)
- **C — ไม่ทำอะไร** เปลี่ยนสคริปต์ตอน onboard ลูกค้าใหม่แต่ละราย

**Comparison**

| | A: เป็นกลาง | B: ตาราง Company | C: ไม่ทำ |
|---|---|---|---|
| ความซับซ้อน | ต่ำ (rename + แก้ string) | กลาง (ตารางใหม่ + admin UI + FK) | ไม่มี |
| ต้องมีตาราง `Company` ไหม | **ไม่ต้อง** — `CompanyId` เป็น string จาก IdP claim พอ | ต้องมี | ไม่ต้อง |
| ลูกค้าเห็นชื่อตัวเองไหม | ไม่ (ทักแบบกลาง) | เห็น | เห็น (แต่แก้โค้ดต่อราย) |
| แก้ปัญหาชื่อคอลัมน์โกหก | ได้ | ได้ | ไม่ได้ |
| เพิ่มทีหลังได้ไหม | ได้ — B ต่อยอดจาก A ได้ตรง ๆ | — | — |

**Recommendation**
**Option A** — เหตุผลเดียวที่เคยเสนอตาราง `Company` คือเก็บ `RecipientTerm`/`GreetingScript`
พอตัดการทักทายรายบุคคลออก ตารางนั้นก็ไม่มีเหตุผลจะมีอยู่ `CompanyId` เป็น string
ที่ derive จาก IdP claim ก็เพียงพอ

ขอบเขต: rename คอลัมน์ + ค่าใน `SenderRole` + ข้อความไทยที่ผู้ใช้เห็น ~18 จุด
และตัดพารามิเตอร์ชื่อออกจาก `introScript()` — ต้องไปกับ migration ของ TD-010 รอบเดียวกัน
ไม่แยกรอบ

⚠️ ห้าม find/replace คำว่า "ครู" แบบดิบ — `config/response-texts.ts` มี "ขอเวลาสัก**ครู่**นะคะ"
ซึ่งแปลว่า *a moment* ไม่ใช่ teacher

เปิดทาง B ไว้ทีหลังถ้าลูกค้าเรียกร้องให้ทักทายด้วยชื่อจริง — ตอนนั้นค่อยเพิ่มตาราง
โดยไม่ต้องรื้อสิ่งที่ทำใน A

**Status** `Accepted` (11 ส.ค. 2026)

---

## TD-013 — ตั้งชื่อและขอบเขตตาราง เมื่อแยก "ลิงก์" ออกจาก "การเรียน"

**Problem**
[`CORE_FEATURE_SPEC.md`](./CORE_FEATURE_SPEC.md) เคาะให้ 1 ลิงก์รองรับหลายการเรียน
ผลคือ **ความหมายของแถวใน `TrainingSession` กลับด้าน** — วันนี้ 1 แถว = การเรียนของคน 1 คน
(มี `StartedAt`, `Status`, `LastSlideObjectId` อยู่ในตัว) หลังแยกแล้ว 1 แถว = ลิงก์ที่หลายคนใช้ร่วมกัน

จุดที่เจ็บจริงคือ `SessionQuestion.SessionId` และ `ChatMessage.SessionId` ซึ่งหลังแยกแล้ว
**ต้อง**ชี้ไปที่ "การเรียน" (คำถามเป็นของคนที่ถาม ไม่ใช่ของลิงก์) พร้อมกันนั้น `SessionSummary`
ก็ซ้ำซ้อนทั้ง 3 คอลัมน์

**Options**

- **A — `TrainingLink` + `LearningSession`** rename ของเดิม แล้วตั้งของใหม่เป็น `LearningSession`
- **B — คงชื่อ `TrainingSession` + เพิ่ม `LearningSession`** churn น้อยที่สุดตอนนี้
- **C — `TrainingInvite` + `LearningSession`** เหมือน A แต่ใช้คำว่า Invite

**Comparison**

| | A: TrainingLink | B: คงชื่อเดิม | C: TrainingInvite |
|---|---|---|---|
| `SessionQuestion.SessionId` อ่านแล้วเข้าใจตรง | **ใช่** (Session = การเรียน ที่เดียว) | **ไม่** — ชี้ไปตารางที่ไม่ชื่อ Session | ใช่ |
| ต้อง rename `SessionQuestion`/`ChatMessage`/route | ไม่ต้อง | ไม่ต้อง แต่ชื่อโกหกถาวร | ไม่ต้อง |
| `GetBySessionId()` (มี 4–5 ที่) กำกวมไหม | ไม่ | **กำกวมตลอดไป** | ไม่ |
| churn รอบนี้ | ~148 จุด แต่ compiler จับครบ | ต่ำสุด | เท่า A |
| ความแม่นของคำ | ลิงก์ส่งให้หลายคน = "link" ตรง | — | "invite" สื่อว่าเจาะจงคน — ไม่ตรง |

**Recommendation**
**Option A** — เหตุผลหลักไม่ใช่ความสวยของชื่อ แต่คือ พอคำว่า "Session" หายจากฝั่งลิงก์
ชื่อ `SessionQuestion` · `ChatMessage.SessionId` · `api/session-questions` กลับมา*ถูกต้อง*
โดยไม่ต้องแตะ — เปลี่ยนแค่ FK

ต้นทุนต่ำกว่าเลข 148 มาก เพราะ (ก) rename คลาส/property ใน C# compiler จับให้ครบ
(ข) `migrationBuilder.RenameTable` บรรทัดเดียว (ค) ไฟล์ส่วนใหญ่ **ต้องแก้อยู่แล้ว**
เพราะ `Status`/`StartedAt`/`LastSlideObjectId` ย้ายออกตามสเปก — การ rename เกาะไปกับ
churn ที่เกิดแน่นอนอยู่แล้ว และยังไม่เคย deploy migration จริง (ดู TD-006) จึงทำตอนนี้ถูกที่สุด

พ่วงในรายการเดียวกัน — **ลบ `SessionSummary`**: เหตุผลเดียวที่จะเก็บ snapshot คือ
"แช่แข็งภาพ ณ เวลานั้น" แต่โค้ดไม่ได้ทำแบบนั้นอยู่แล้ว (`GetBySessionId` join คำถามสดตอนอ่าน
ตาม comment ในไฟล์เอง) ได้ของครึ่งแช่แข็งครึ่งสด และฟีเจอร์รีวิวของ CS จะแก้ `SessionQuestion`
*หลัง*เรียนจบ ทำให้ `UnansweredPoints` ที่แช่แข็งไว้ค่อย ๆ ขัดกับความจริง
→ ลบ entity/ตาราง/repository/service แต่ **คง `SessionSummaryViewModel`** ไว้แบบคำนวณสด
frontend ไม่ต้องแก้

**Status** `Accepted` (13 ส.ค. 2026)

---

## TD-014 — ระบบล็อกอินฝั่ง admin และตาราง `Company`

**Problem**
TD-011 เคาะไว้ว่าฝั่งผู้เรียน derive `CompanyId` จาก token (ทำเสร็จแล้ว) แต่ **ฝั่ง admin ระบุว่า
"มาจาก auth claim" ซึ่งยังไม่เคยมี auth** ปัจจุบันใช้ `X-Company-Id` header ที่ใครก็ปลอมได้
คู่กับ `DEFAULT_COMPANY_ID` เป็น scaffold ชั่วคราว

สองอย่างที่ทำให้เรื่องนี้เร่งขึ้น:

1. หน้า settings ที่จะให้ admin แก้ API key / สลับ provider ผ่านหน้าเว็บ — เป็น endpoint
   ที่อันตรายที่สุดในระบบ ทำก่อน auth ไม่ได้
2. School Bright ดูแลลูกค้าหลายราย แปลว่า CS **คนเดียวต้องสลับดูข้อมูลข้ามหลาย company**
   ซึ่งตรงข้ามกับ `CompanyContextMiddleware` วันนี้ที่ล็อก 1 request = 1 company

ข้อกำหนดที่ได้จากทีม (13 ส.ค. 2026):

- **เจ้าของระบบคือ School Bright ฝ่ายเดียว** — ไม่มีใครอื่นแก้ค่าระดับระบบได้
- **แต่พนักงานของลูกค้าเข้าใช้หลังบ้านเองได้** (SCB มีทีม CS ของตัวเอง) และมีคนที่ต้อง
  ถูกจำกัดแล้วตั้งแต่วันนี้ — จึงต้องมี role และการจำกัดเป็น **ด่านความปลอดภัยจริง**
  ไม่ใช่แค่ความเป็นระเบียบ
- คนของลูกค้าสร้างลิงก์เทรนนิ่ง แก้บทเรียน และอัปโหลดเอกสารของ company ตัวเองได้
- **`admin` ของลูกค้าเพิ่ม/ปิดบัญชีลูกทีมตัวเองได้** ไม่ต้องส่งเรื่องมาที่ School Bright
  (ไม่งั้น School Bright กลายเป็นคอขวดของงานประจำที่ไม่ควรผ่านมือตัวเอง)
- API key เป็นของ School Bright ชุดเดียวทั้งระบบ ไม่แยกต่อ company → หน้า settings
  ต้องเป็นของ `owner` เท่านั้น
- **ห้ามบังคับ SSO อย่างเดียว** — พนักงานใหม่ที่ยังไม่ผ่านโปรอาจยังไม่มีเมลบริษัท
  แต่ต้องเข้าใช้ระบบแล้ว

**Options**

**หมายเหตุก่อนอ่าน** — "user store" กับ "วิธีพิสูจน์ตัวในแต่ละ request" เป็นคนละชั้น ไม่ใช่ทางเลือก
ที่ต้องเลือกอย่างใดอย่างหนึ่ง ตัวเลือกด้านล่างว่าด้วยชั้นแรก ส่วนชั้นที่สองสรุปแยกไว้ท้าย Recommendation

- **A — ASP.NET Core Identity** password เป็นพื้นฐาน, external login (Google/Microsoft)
  เป็นของเสริมที่ผูกเข้าบัญชีเดิมได้ทีหลัง
- **B — เขียน password hashing + token validation เอง**
- **C — IdP ภายนอก** (Auth0 / Clerk / Keycloak)

**Comparison**

| | A: Identity ในตัว | B: เขียนเอง | C: IdP ภายนอก |
|---|---|---|---|
| รองรับ password + SSO บนบัญชีเดียว | **มีในตัว** | เขียนเองทั้งหมด | มี |
| password hashing / lockout / reset | มีในตัว | **เขียนเอง = ที่พลาดง่ายสุด** | มี |
| ค่าใช้จ่าย | $0 | $0 | รายเดือน |
| dependency ใหม่ | ไม่มี (อยู่ใน framework) | ไม่มี | service ภายนอก |
| เลื่อน SSO ไปทำทีหลังได้ | **ได้** | ได้ | ได้ |
| เหมาะกับทีมเล็กภายใน | ใช่ | เกินจำเป็น | เกินจำเป็น |

**Recommendation**

**Option A** — ข้อกำหนด "password เป็นพื้นฐาน + SSO เสริมทีหลัง" คือสิ่งที่ ASP.NET Core Identity
ออกแบบมารองรับพอดี ไม่ใช่ระบบล็อกอินสองระบบซ้อนกัน แต่เป็นบัญชีเดียวที่มีสองประตู
**ทำ password ก่อน แล้วเสียบ external provider ทีหลังได้โดยไม่ต้องแตะบัญชีเดิม** จึงยังไม่ต้อง
ตัดสินใจเรื่อง Google vs Microsoft ตอนนี้
Option B คือการเขียน Identity ฉบับด้อยกว่าในส่วนที่พลาดแล้วเจ็บที่สุด — ไม่แนะนำ

**ชั้นที่สอง: ใช้ JWT bearer ไม่ใช่ cookie**

⚠️ แก้จากร่างแรกที่เขียนว่า "Identity + cookie auth" ซึ่งผิด — frontend กับ backend อยู่คนละ origin
(`NEXT_PUBLIC_API_BASE_URL` ชี้คนละโฮสต์) และคอมเมนต์ใน `use-session-chat.ts` บันทึกไว้เองว่า
จงใจตั้ง `withCredentials: false` เพื่อเลี่ยง `AllowCredentials()` บน CORS policy ที่ระบุ origin ชัดแล้ว

| | Cookie | JWT ผ่าน Authorization header |
|---|---|---|
| ข้าม origin | ต้อง `SameSite=None; Secure` + เปิด `AllowCredentials()` | ไม่ต้องแตะอะไร |
| CORS ที่ตั้งไว้ | ต้องผ่อนให้หลวมลง | คงเดิม |
| SignalR | ต้องเปลี่ยน `withCredentials` เป็น `true` | ส่ง token ผ่าน `accessTokenFactory` ที่รองรับในตัว |

ใช้ `Microsoft.AspNetCore.Authentication.JwtBearer` ที่มากับ framework — ไม่ได้เขียน token
validation เอง ซึ่งเป็นเหตุผลเดียวกับที่ Option B ถูกปฏิเสธ

**สรุปสองชั้น: Identity (จัดการผู้ใช้/รหัสผ่าน/SSO) + JwtBearer (พิสูจน์ตัวต่อ request)**

**ตาราง `Company` — ให้มี**

```
Company
  Id        ← ค่าเดียวกับคอลัมน์ CompanyId ของทุกตาราง
  Name
  IsActive
```

⚠️ **ไม่ได้กลับคำ TD-012** — TD-012 ปฏิเสธตารางนี้เพราะเหตุผลเดียวตอนนั้นคือ "เก็บ vocabulary
ต่อบริษัท" ซึ่งพอตัดการทักทายด้วยชื่อออกก็ไม่เหลือเหตุผล เหตุผลรอบนี้เป็นคนละเรื่อง:
ต้องมีรายชื่อลูกค้าที่เชื่อถือได้ไว้ทำ dropdown สลับ company ข้อสรุปเรื่อง vocabulary ของ
TD-012 ยังคงเดิม — ไม่เก็บคำเรียกต่อบริษัท

**การเข้าถึงข้ามลูกค้า — role 3 ระดับ**

> 🔄 **แก้ไขสำคัญ 13 ส.ค. 2026 (วันเดียวกับที่เคาะ ยังไม่ได้เขียนโค้ด auth สักบรรทัด)**
>
> ร่างแรกเขียนว่า *"`AdminUser` ไม่ต้องมี `CompanyId` เพราะ admin ทุกคนคือ School Bright"*
> ตั้งอยู่บนคำตอบที่ว่า SCB เป็นแค่ลูกค้าที่ School Bright ดูแลให้
>
> ทีมยืนยันเพิ่มว่า **"CS" คือพนักงานของลูกค้าเอง** (SCB มีทีมของตัวเองเข้ามาใช้หลังบ้าน)
> และมีคนที่ต้องถูกจำกัดแล้วตั้งแต่วันนี้ ข้อสรุปเดิมจึงใช้ไม่ได้ และที่สำคัญกว่าคือ
> **ธรรมชาติของการจำกัดเปลี่ยนไป**:
>
> ```
> เดิม  จำกัด company = ความเป็นระเบียบ   พลาดแล้วน่ารำคาญ
> ใหม่  จำกัด company = ด่านความปลอดภัย  พลาดแล้วข้อมูล School Bright ไปโผล่ที่ SCB
> ```

```
AdminUser
  Role        "owner" | "admin" | "cs"
  CompanyId   null เฉพาะ owner · บังคับมีค่าเมื่อ admin/cs
```

| role | ขอบเขต | ทำอะไรได้ |
|---|---|---|
| `owner` | ทุก company | ทุกอย่าง + จัดการรายชื่อ company + หน้า settings (API key/provider) |
| `admin` | company ตัวเอง | งานทั้งหมดของ company นั้น **+ เพิ่ม/ปิดบัญชีคนใน company ตัวเอง** |
| `cs` | company ตัวเอง | สร้างลิงก์ · แก้บทเรียน · อัปโหลดเอกสาร · รีวิวคำตอบ — แต่จัดการ user ไม่ได้ |

School Bright เป็นแถวหนึ่งใน `Company` ด้วย — เป็นทั้งเจ้าของระบบและลูกค้าของตัวเอง
(ครูของ School Bright ก็ใช้ระบบนี้) `owner` จึงไม่ใช่ "คนของ company หนึ่ง" แต่เป็นสิทธิ์ที่
ตั้งฉากกับ company

**ทำไมต้องมี `Role` แยก ไม่ใช่ตีความจาก `CompanyId == null`**
เพราะตอนนี้เป็นด่านความปลอดภัยแล้ว ถ้ากติกาคือ "null = เห็นทุกอย่าง" วันไหนมีบั๊กที่เผลอ null
คอลัมน์นั้น คนคนนั้นได้สิทธิ์สูงสุดทันทีโดยไม่มีอะไรฟ้อง — ต้องออกแบบให้**พังแล้วปิด
ไม่ใช่พังแล้วเปิด**

**เลือก column แทนตาราง `AdminUserCompany` ที่ร่างแรกเสนอไว้** เพราะ admin/cs สังกัด company
*เดียว* ตารางเชื่อมจะคุ้มก็ต่อเมื่อคนหนึ่งคุมได้หลายราย ซึ่งยังไม่มีเคสนั้น

**ไม่ทำหน้าตั้งค่าสิทธิ์** — role ตายตัว 3 แบบพอ สิ่งที่ต้องมีจริงคือ dropdown เลือก role
ตอนเพิ่มคน ไม่ใช่หน้าประกอบสิทธิ์เอง หน้านั้นมีต้นทุนที่มองไม่เห็น: ทุกฟีเจอร์ใหม่หลังจากนั้น
ต้องมาเพิ่มสวิตช์ในหน้านั้นทุกครั้ง และต้องคิดว่าเปิดอันนี้ปิดอันนั้นแล้วเกิดอะไร
ค่อยทำวันที่ลูกค้าขอ role ที่เราไม่ได้เตรียมไว้ ซึ่งอาจไม่มีวันนั้น

**สามอย่างที่กลายเป็นเรื่องอันตรายทันทีเพราะมีผู้ใช้จากนอกองค์กร**

1. **`ALLOW_COMPANY_HEADER` ต้องลบทิ้งจากโค้ด ไม่ใช่แค่ปิดไว้** — เดิมยอมรับได้เพราะผู้ใช้
   ทุกคนเป็นคนใน ตอนนี้ใครตั้ง header นี้ได้ = ข้ามด่านทั้งหมด `DEFAULT_COMPANY_ID` ก็ถอดด้วย
2. **`/api/admin/reset` และ `/api/admin/reindex` ต้องเป็น `owner` เท่านั้น** — ตอนนี้ไม่มีการ
   แยกสิทธิ์เลย CS ของลูกค้ากดลบข้อมูลไม่ได้เด็ดขาด
3. **หน้า settings (API key/provider) ต้องเป็น `owner` เท่านั้น** — key เป็นของ School Bright
   ชุดเดียวทั้งระบบ ถ้า admin ของลูกค้าเปิดได้ เขาจะเห็น/แก้ key ที่ทุก company ใช้ร่วมกัน
   และสลับ provider ให้ทั้งระบบพังได้

**เส้นทาง 403 ต้องมีเทสต์คุม** — "cs ของ SCB ขอ `?company=schoolbright` แล้วต้องถูกปฏิเสธ"
คือด่านความปลอดภัยตัวจริง ต่อยอดจาก `CompanyIsolationTests` ที่มีอยู่แล้ว

**สิ่งที่ต้องทำไปพร้อมกัน เพราะทำทีหลังไม่ได้**

ก่อน TD-014 ทุก entity มี `CreateBy` / `UpdateBy` แต่ไม่มีที่ไหนเขียนค่า เพราะยังไม่มี auth;
implementation ปัจจุบันเติม actor ใน write paths หลักแล้ว การเติม user id ลงสองคอลัมน์นี้
แทบไม่มีต้นทุน ขณะที่ถ้าข้ามไป **จะไม่มีวันย้อนกลับไปรู้ได้ว่าใครสร้างลิงก์ไหน หรือใครรีวิวคำตอบไหน**
ข้อมูลที่ไม่ได้บันทึกตอนนั้นสร้างขึ้นใหม่ไม่ได้ — ให้ทำไปกับ TD นี้ อย่าเลื่อน

**ขอบเขตรอบแรก**

login + ปิด `/admin/*` + ตาราง `Company` + `AdminUser` (3 role) + ตัวสลับ company ผ่าน
`?company=` + เติม `CreateBy`/`UpdateBy` + **หน้าจัดการ admin user** (เพิ่ม/ปิดบัญชี/เลือก role)
— การเพิ่มคนเป็นความต้องการของวันนี้ ไม่ใช่ของอนาคต ถ้าไม่มีหน้านี้ต้องแก้ DB ด้วยมือ
ทุกครั้งที่มีคนเข้าใหม่ และ `admin` ของลูกค้าต้องทำเองได้โดยไม่ผ่าน School Bright

ยังไม่ทำรอบนี้: reset password ทางอีเมล (ต้องมี SMTP), invite flow, และ SSO
(รอรู้ก่อนว่าองค์กรใช้ Google หรือ Microsoft — เสียบทีหลังได้โดยไม่แตะบัญชีเดิม)

**Status** `Accepted` (13 ส.ค. 2026)
⚠️ ต้องทำ **ก่อน** หน้า settings (ดู TD-015 เมื่อมี) และ EF migration ของ TD-013 ควรรอไปสร้าง
พร้อมกับ schema ของ TD นี้ในตัวเดียว จะได้ไม่เสีย migration เปล่า

---

## TD-015 — พิมพ์ถาม AI แทนพูด (F10) + ตัดฟีเจอร์แชตคุยกับ CS ทิ้งทั้งชุด (F10-a)

**Problem**
สองเรื่องที่ดูเหมือนแยกกัน (เพิ่มช่องพิมพ์ / รื้อแชต) กลับต้องทำพร้อมกันเสมอ เพราะ
`ChatDrawer` เดิมเป็นทั้ง "ที่พิมพ์คุยกับ CS" และ "ที่แสดงคำถามที่ถามด้วยเสียง" ในไฟล์เดียว —
เปิดช่องพิมพ์ใหม่โดยไม่รื้อของเดิมก่อนจะได้ UI ที่มีทั้งแชตกับคนและพิมพ์ถาม AI ปนกัน
ซึ่งขัดกับมติที่ต้องการตัดความคาดหวังว่า "จะมีคนมาตอบ" ทิ้งไปเลย

**Options**

- **A — endpoint ใหม่ `/api/text-question` ที่ใช้ provider เดียวกับเสียง** ทั้งสองช่องทาง
  รับประกันเทียบเท่ากัน 100% เพราะโค้ด grounding/answer เป็นเมธอดเดียวกันจริง ๆ
- **B — ยัดเพิ่มใน `/api/voice-question`** ให้ `audio` เป็น nullable แล้วรับ `text` คู่กัน
- **C — เก็บแชตไว้เป็นทางสำรอง** แค่เพิ่มช่องพิมพ์ควบคู่กับแชตเดิม

**Comparison**

| | A: endpoint แยก + provider ร่วม | B: ยัดใน voice-question | C: เก็บแชตไว้คู่กัน |
|---|---|---|---|
| รับประกัน "เทียบเท่าเสียง 100%" | **ใช่** — โค้ดตอบคำถามเป็นเมธอดเดียวกันจริง | เสี่ยง drift ถ้าใครแก้ branch เดียวลืมอีกฝั่ง | ไม่เกี่ยวกัน — เป็นคนละฟีเจอร์ |
| transport/validation สะอาด | ใช่ — JSON ล้วน ไม่แบก `[RequestSizeLimit(10MB)]` ของเสียง | ไม่ — if-chain ควบคุมทั้งสองโหมดในแอ็กชันเดียว | ใช่ |
| ผู้เรียนยังคาดหวังว่ามีคนมาตอบ | ไม่ (ตัดแชตออกพร้อมกัน) | ไม่ | **ใช่ — ขัดกับสิ่งที่อยากแก้** |

**Recommendation**
**Option A** พร้อมตัดแชตทิ้งทั้งชุดในรอบเดียวกัน (ตัวเลือก C ถูกปฏิเสธ — เจ้าของโปรเจกต์ยืนยัน
2 รอบว่าไม่ต้องการทางสำรองให้คุยกับคน) `SessionQuestion.Source` (`voice`/`text`) เพิ่มเข้ามาเพราะ
คำถามที่ *พูด* มีสาเหตุตอบผิดข้อที่สี่ที่คำถามที่ *พิมพ์* ไม่มี (ถอดเสียงผิด) — ไม่รู้ที่มา CS
แยกไม่ออกว่ากำลังแก้ปัญหาความรู้หรือปัญหาการถอดเสียง เพิ่มเป็น backfill `voice` ได้ตรงไปตรงมา
เพราะวันที่ก่อนมตินี้ยังพิมพ์ถามไม่ได้เลยจริง ๆ ไม่ใช่การเดา

พ่วงมติ **U1** ในรอบเดียวกัน — ถอดการตอบ "พร้อมหรือยัง" ด้วยเสียงทิ้ง เหลือกดปุ่มทางเดียว:
เจ้าของโปรเจกต์เห็น trade-off เต็มที่ (ขยายขอบเขตงานให้โต พาดเข้า regression surface ของ
Module ที่ผ่าน QA ไปแล้ว) แล้วเลือกทางที่ตัดทิ้งเพื่อไม่ให้ระบบมีสองทางตอบคำถามเดียวกัน
(ปุ่ม + เสียง) ที่ต้อง sync กันตลอดไป

**Status** `Accepted` (23 ส.ค. 2026) — migration `RemoveChatMessageAndAddQuestionSource`
(`DropTable("ChatMessage")` พร้อมข้อมูลเดิม + `AddColumn("SessionQuestion", "Source")`)

---

## TD-016 — Benchmark จริงของ TTS/RAG latency + ปัญหา config ที่เจอระหว่างทดสอบสด (2026-08-24)

**Problem**
ทดสอบ ElevenLabs + Gemini RAG แบบสดครั้งแรกทั้งระบบ (local `dotnet run`, provider จริงทั้งหมด
ไม่ mock) เจอคอขวดและปัญหา config หลายจุดที่กระทบ latency โดยตรง เก็บตัวเลขจริงไว้ที่นี่เพราะ
เป็นข้อมูลตั้งต้นสำหรับตัดสินใจ optimize รอบต่อไป (อัปเกรดแผน ElevenLabs, ย่อ speaker notes,
เปลี่ยนโมเดล Gemini ฯลฯ) — ไม่ใช่การเดา เป็นตัวเลขวัดจริงทั้งหมด

**Findings — TTS (ElevenLabs)**

1. **โมเดล `eleven_v3` (ตอนนั้นเป็น default) ช้ามาก กับข้อความยาว** — narration 1,478 ตัวอักษร
   เป็น 1 request เดียว (ยังไม่ได้ chunk) วัดได้ **48.6s และ 50.4s** คนละรอบ
2. **แก้ด้วย 2 ทาง พร้อมกัน:**
   - **Chunking + concurrency**: แบ่งข้อความเป็นชิ้นละ ≤350 ตัวอักษร (`TextChunker.cs`, ใช้ร่วมกับ
     `EdgeTtsProvider`) ยิงพร้อมกันสูงสุด 2 ชิ้น (`ElevenLabsTtsProvider.MaxConcurrentChunks`) —
     จำกัดที่ 2 เพราะ **แผน Starter ของ ElevenLabs รองรับ concurrent request สูงสุด 3** (ตรวจสอบสด
     ส.ค. 2026: Free=2, Starter=3, Creator=5, Pro=10) เหลือ 1 ช่องกันชนกับงานอื่นที่เกิดพร้อมกัน
   - **เปลี่ยนโมเดลเป็น `eleven_v3_conversational`** (ดู TD-001 อัปเดต) เร็วกว่า ~2.5 เท่า
3. **ผลรวมหลังแก้ทั้งสองทาง**: narration ก้อนเดิม (5 chunks, ~1,474 ตัวอักษรรวม) จาก 34-36s
   เหลือ **~11 วินาที** (ลดลง ~3.2 เท่า) — วัดซ้ำได้ตรงกันหลายรอบ (10.9s, 11.3s, 11.6s)
4. **เพดานที่เหลือคือ concurrency ไม่ใช่ chunk size** — วัดแล้วโมเดลนี้มี fixed overhead ต่ำ (~150-800ms)
   บวกต้นทุนต่อตัวอักษรที่ค่อนข้างคงที่ (~4-12ms/ตัวอักษร, มี variance สูงในช่วง 190-760 ตัวอักษร
   คือส่วนใหญ่ตกอยู่ 3.5-4.8s โดยไม่ขึ้นกับความยาวในช่วงนี้มากนัก) — **การลดขนาด chunk ให้เล็กลงอีก
   ไม่ช่วยเวลารวม เพราะจำนวน request ที่มากขึ้นชนเพดาน concurrency เดิม** ตัวแปรที่มีผลจริงคือ
   (ก) จำนวน request ทั้งหมดที่ต้องยิง (ยิ่งข้อความสั้นยิ่งดี) และ (ข) concurrency ที่ทำได้พร้อมกัน
   (ผูกกับแผนที่สมัคร)
5. **ทางลัดที่ไม่ต้องแตะโค้ดเลย**: ถ้าย่อ speaker notes ต่อหน้าให้เหลือ ≤350 ตัวอักษร (ต่ำกว่า
   `MaxChunkChars`) จะกลายเป็น **1 chunk เดียว** ไม่ต้องรอคิว concurrency เลย — วัดจริงจากทดสอบ
   ที่ย่อเนื้อหาแล้ว: 146 ตัวอักษร → 2.2s, 227-259 ตัวอักษร → 3.0-4.0s (เทียบกับ 5-chunk เดิมที่
   ~11s) หน้าที่ยังไม่ได้ย่อ (988 ตัวอักษร, ยังต้องแบ่ง 3 chunks) ยังคงกินเวลา ~8.9s
6. **Free/Starter tier credit หมดเร็วกว่าที่คาด** — ระหว่างการทดสอบวันเดียว (สลับโมเดล, ทดสอบ
   chunking ซ้ำหลายรอบ, ทดสอบ narration ยาวๆ) ใช้ credit ของแผนไปจนเหลือ **10/10000** ก่อให้เกิด
   `401 quota_exceeded` กลางการทดสอบจริง ห้ามมองข้ามต้นทุน credit ตอนออกแบบ retry/prefetch — การ
   prefetch filler หลายชิ้นต่อ session (ดูข้อ RAG-4 ด้านล่าง) ทวีคูณการใช้ credit ขึ้นไปอีก

**Findings — RAG (Gemini + Pinecone)**

1. **`GEMINI_MODEL=gemini-flash-latest` ค้างไม่ตอบเลย (ไม่ error แม้แต่ error code)** — ทดสอบตรง
   ไปที่ Gemini API 3 รอบ ค้างครบทั้ง 3 รอบ (timeout 15s ที่ curl เอง, ไม่ใช่ timeout ฝั่งเรา) ทั้งที่
   โมเดลนี้มีอยู่จริงในรายการ `/v1beta/models` — **ต้นเหตุคือ alias นี้ใช้ไม่ได้กับ API key ที่สร้างใหม่**
   (`gemini-2.5-flash` ตรงๆ ตอบเร็วแต่ error 404 บอกให้ใช้ `gemini-3.6-flash` แทนสำหรับ key ใหม่)
   แก้โดยเปลี่ยน `GEMINI_MODEL=gemini-3.6-flash` ตรงๆ ตอบเร็วและถูกต้องทันที (มี free tier, ~15
   RPM/~1500 RPD, ไม่ผูกบัตรเครดิต — แต่ข้อมูล free tier ถูก Google เอาไปใช้พัฒนาโปรดักต์ ห้ามใช้กับ
   ข้อมูล sensitive)
2. **1 คำถามด้วยเสียง = อย่างน้อย 3 requests ไป Gemini** — ถอดเสียง (`generateContent.audio`) +
   สร้าง embedding คำถาม (`embedContent`) + สร้างคำตอบ (`generateContent.text`) ทำให้ชน free-tier
   rate limit (429) ง่ายกว่าที่คาดตอนทดสอบถี่ๆ (ยิ่งมีคนทดสอบคู่ขนานผ่าน curl พร้อมกันยิ่งชนไว)
   — เป็น rate limit ชั่วคราว หายเองใน ~1 นาที ไม่ใช่ปัญหาถาวร
3. **Pinecone index ว่างเปล่า (ยังไม่เคย index เอกสารจริง) ทำให้ retrieval คืน 0 matches ทุกครั้ง**
   → RAG fallback ไปส่ง **เนื้อหาทั้งเด็คของบทเรียน** เป็น context ให้ Gemini แทน prompt ใหญ่ขึ้น
   มาก วัดจริง: คำถามที่ fallback แบบนี้ใช้เวลา generate คำตอบ **16.4s** เทียบกับคำถามที่ไม่ fallback
   ที่ **3.6s** — ต่างกันเกือบ 5 เท่า **นี่คือเหตุผลที่ต้องรีบ index เอกสารเข้า Pinecone จริงก่อนวัด
   latency RAG ต่อ** ตัวเลขที่วัดตอนคลังว่างไม่สะท้อนของจริง
4. **สรุป full round-trip ที่วัดได้จริงวันนี้** (คลัง Pinecone ยังว่างเปล่า — คือกรณีแย่สุด):
   - คำถามพิมพ์: answer 5.4s + TTS 2.7s = **~8.1s รวม**
   - คำถามพูด (fallback เต็มเด็ค): transcribe 11.0s + embed/search 0.8s + answer 16.4s (fallback)
     + TTS 3.3s = **~31.4s รวม** — คาดว่าจะลดลงมากเมื่อ index เอกสารจริงแล้ว (ตัด fallback ออก)

**Config gotcha ที่เจอซ้ำหลายรอบระหว่างเซสชันนี้ — บันทึกไว้กันคนอื่นเจอซ้ำ**

- **`DotEnv.Load()` (Program.cs) ไม่ override ตัวแปรที่ "มีอยู่แล้ว" แม้ค่านั้นมาจากบรรทัดก่อนหน้า
  ในไฟล์เดียวกัน** — `.env` ที่มีตัวแปรชื่อซ้ำ 2 บรรทัด (เช่น `TTS_PROVIDER=edge` แล้วตามด้วย
  `TTS_PROVIDER=elevenlabs`) จะได้ค่า**บรรทัดแรก**ตอนรันผ่าน `dotnet run` local — ตรงข้ามกับ
  docker-compose's `env_file` ที่ใช้ค่า**บรรทัดสุดท้าย**เมื่อซ้ำกัน — พฤติกรรมต่างกันระหว่างสองวิธี
  รันจึงทำให้ local กับ docker เห็นค่าจริงคนละตัวจากไฟล์เดียวกัน ต้องเช็ค `docker exec <container>
  printenv <KEY>` เทียบกับไฟล์ `.env` เสมอเวลาสงสัยว่าทำไมพฤติกรรมไม่ตรงกับที่ตั้งไว้
- **แก้ `.env` แล้วต้อง restart process เสมอ — โหลดครั้งเดียวตอน start ไม่มี hot-reload** เจอ
  หลายรอบว่า "restart" ที่ผู้ใช้เข้าใจว่าทำแล้วจริงๆ ไม่ได้ฆ่า process เดิม (PID ไม่เปลี่ยน แม้
  build ใหม่ผ่านไปแล้ว) — วิธียืนยันที่ชัวร์ที่สุดคือเทียบเวลา start ของ process
  (`ps -eo pid,lstart,command | grep "dotnet run"`) กับเวลาแก้ไขล่าสุดของ `.env`
  (`stat -f "%Sm" .env`) ถ้า process เริ่มก่อนไฟล์ถูกแก้ = ยังไม่ได้ผลจริง
- **Docker container กับ local `dotnet run` เป็นคนละ DB คนละ backend กันได้ในเวลาเดียวกัน**
  ถ้าเปิดพร้อมกันทั้งสอง (`localhost:3000`+`:5080` local, `localhost:3001`+`:5138` docker) ผู้ใช้
  อาจทดสอบผิดตัวโดยไม่รู้ตัว — เช็ค `lsof -i :<port>` เทียบ `docker ps` เสมอเมื่อผลไม่ตรงกับที่คาด

**Recommendation**

1. **Index เอกสารเข้า Pinecone ก่อนวัด latency RAG รอบต่อไป** — ตัวเลขวันนี้เป็น worst-case
   (fallback เต็มเด็คทุกครั้ง) ไม่ใช่ของจริง
2. **ถ้า latency ยังไม่พอหลัง index แล้ว** ตัวเลือกเรียงตาม effort/impact จากที่ประเมินไว้:
   ย่อ speaker notes ให้ ≤350 ตัวอักษรต่อหน้า (ฟรี, ทำเองได้เลย) → อัปเกรดแผน ElevenLabs เป็น
   Creator (concurrency 5, ~$11/เดือน) → streaming TTS (ลงทุนสูงสุด เปลี่ยน architecture)
3. **ตรึง `GEMINI_MODEL=gemini-3.6-flash` ใน `.env.example`** พร้อมคอมเมนต์เตือนไม่ให้ใช้ alias
   `gemini-flash-latest`/`gemini-pro-latest` กับ key ที่สร้างใหม่โดยไม่ตรวจสอบก่อน — TD-009 (ตรึง
   เวอร์ชันโมเดลใน production) ครอบเรื่องนี้อยู่แล้วในหลักการ แต่ยังไม่มีรายการ Gemini เจาะจง
4. **จับตา ElevenLabs credit usage จริงเมื่อ deploy** — ปริมาณ prefetch filler ต่อ session
   (`prefetchFillers()`, `frontend/src/hooks/use-tutor-session.ts`) มีผลโดยตรงต่อการใช้ credit
   ไม่ใช่แค่ narration หลัก ควรมีตัวเลข credit/session จริงก่อนประเมินต้นทุนต่อเดือน

**Status** `Recorded` (24 ส.ค. 2026) — เป็นข้อมูลอ้างอิง ไม่ใช่การตัดสินใจปิดจบ รอ index เอกสารจริง
แล้ววัดซ้ำก่อนตัดสินใจ optimize รอบต่อไปตาม Recommendation ข้อ 1-2

---

## รายการที่รอข้อมูลจากทีมก่อนตัดสินใจ

| คำถาม | ทำไมถึงบล็อก |
|---|---|
| จะ deploy ที่ไหน (Azure / Huawei Cloud / on-prem)? | กำหนดทิศทาง TD-001, TD-002, TD-006 พร้อมกัน (โค้ดรองรับ Huawei OBS + ModelArts อยู่แล้ว ซึ่งอาจเป็นสัญญาณ) |
| ~~School Bright มี identity provider ให้ใช้ร่วมหรือไม่?~~ | ตอบแล้ว 13 ส.ค. 2026 — ไม่ผูกกับ IdP ตัวเดียว ต้องรองรับทั้ง password และ SSO (ดู TD-014) |
| ~~ระบบต้องรองรับหลายโรงเรียนแบบแยกข้อมูลกันหรือไม่?~~ | ตอบแล้ว — ใช่ ทำเสร็จแล้ว (TD-010/TD-011) |
| ปริมาณคาดหวัง — กี่ session ต่อวัน, พร้อมกันสูงสุดเท่าไร? | ตัดสินเรื่อง scale-out, Pinecone vs pgvector, และงบ TTS |
