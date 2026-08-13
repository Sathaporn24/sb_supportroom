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

**Comparison**

| | A: Azure | B: Google | C: Gemini TTS | D: คงเดิม |
|---|---|---|---|---|
| ความซับซ้อน | ต่ำมาก (1 คลาส) | ต่ำมาก | ต่ำ–กลาง | ไม่มี |
| ค่าใช้จ่าย | ~$16/1M อักขระ | ~$16/1M อักขระ | ตาม token | $0 + ค่า proxy |
| เสียงเปลี่ยนไหม | **ไม่เปลี่ยนเลย** | เปลี่ยน | เปลี่ยน | ไม่เปลี่ยน |
| ความน่าเชื่อถือ | SLA | SLA | SLA | ไม่มี |
| Vendor lock-in | ต่ำ (`ITtsProvider` กันไว้แล้ว) | ต่ำ | ต่ำ | — |
| ดูแลต่อ | ต่ำ | ต่ำ | ต่ำ | สูง (เปราะ) |

**Recommendation**
**Option A** — ได้เสียงเดิมเป๊ะ ไม่ต้องให้ผู้มีส่วนได้เสียมาฟังอนุมัติใหม่ และงานคือเขียน
`AzureTtsProvider` หนึ่งคลาสแล้วเพิ่ม `"azure"` ใน `TtsProvider.Allowed` — abstraction พร้อมอยู่แล้ว
เก็บ `edge` ไว้เป็นทางเลือกสำหรับ dev ในเครื่องได้

**Status** `Proposed` — **มีความสำคัญสูงสุดในบรรดาข้อเสนอทั้งหมด** ต้องการการอนุมัติเรื่อง
Azure subscription จากทีม

---

## TD-002 — ป้องกัน admin surface และใส่ rate limiting ก่อน deploy

**Problem**
ไม่มี authentication เลย `app.UseAuthorization()` เป็น no-op เมื่อ deploy ออกไป ใครก็ตามที่รู้ URL
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

**Status** `Proposed`

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
**A แล้วต่อด้วย B** — งานที่ต้องทำก่อนคือแยก test ที่ยิงของจริงออกด้วย xUnit trait
แล้วให้ CI รันเฉพาะ unit tests (`dotnet test --filter Category!=Integration`)
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
3. EF Core version conflict — MSB3277 ×5 (Npgsql 10.0.3 ดึง EF Relational 10.0.4 vs ที่อ้าง 10.0.10)
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

- เจ้าของระบบคือ School Bright ฝ่ายเดียว SCB เป็น "ลูกค้าที่ School Bright ดูแลให้"
- API key เป็นของ School Bright ชุดเดียวทั้งระบบ ไม่แยกต่อ company
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

**การเข้าถึงข้ามลูกค้า — เลือกทางง่ายไปก่อน**

ตอนนี้ admin ทุกคนคือ School Bright จึงเห็นได้ทุกลูกค้า → **`AdminUser` ไม่ต้องมี `CompanyId`**
และตัวสลับ company แสดงทุกรายที่ `IsActive`

ทีมยังไม่รู้ว่าวันหนึ่งพนักงาน SCB จะเข้าหลังบ้านเองไหม แต่ความไม่แน่นอนนี้**ไม่มีต้นทุน**
เพราะทางง่ายอัปเกรดเป็นทางยากได้แบบบวกอย่างเดียว:

```
เพิ่มตาราง AdminUserCompany (UserId, CompanyId)
กติกา: ไม่มีแถว = เห็นทุกลูกค้า (พนักงาน School Bright)
        มีแถว   = เห็นเฉพาะที่ระบุ (พนักงานฝั่งลูกค้า)
```

ตารางใหม่ 1 ตัว + แก้ query ของตัวสลับ 1 จุด **ไม่มีคอลัมน์ไหนเปลี่ยนความหมาย ไม่ต้อง
reshape ข้อมูลเดิม** จึงไม่ต้องเผื่อไว้ตอนนี้ (Solution Design Rule ข้อ 7)

**สิ่งที่ต้องทำไปพร้อมกัน เพราะทำทีหลังไม่ได้**

ทุก entity มี `CreateBy` / `UpdateBy` จาก `IEntityMaster` อยู่แล้ว แต่ **ไม่มีที่ไหนในโค้ดเขียนค่าลงไปเลย
(0 จุด)** ตอนนี้ยังไม่มี auth จึงไม่มีค่าจะเขียน — แต่พอ auth มาแล้ว การเติม user id ลงสองคอลัมน์นี้
แทบไม่มีต้นทุน ขณะที่ถ้าข้ามไป **จะไม่มีวันย้อนกลับไปรู้ได้ว่าใครสร้างลิงก์ไหน หรือใครรีวิวคำตอบไหน**
ข้อมูลที่ไม่ได้บันทึกตอนนั้นสร้างขึ้นใหม่ไม่ได้ — ให้ทำไปกับ TD นี้ อย่าเลื่อน

**ขอบเขตรอบแรก**

login + ปิด `/admin/*` + ตาราง `Company` + ตัวสลับ company + เติม `CreateBy`/`UpdateBy`
+ **หน้าจัดการ admin user** (เพิ่ม/ปิดบัญชี) — ทีม CS มีหลายคน การเพิ่มคนจึงเป็นความต้องการ
ของวันนี้ ไม่ใช่ของอนาคต ถ้าไม่มีหน้านี้ต้องแก้ DB ด้วยมือทุกครั้งที่มีคนเข้าใหม่

ยังไม่ทำรอบนี้: reset password ทางอีเมล (ต้องมี SMTP), invite flow, และ SSO
(รอรู้ก่อนว่าองค์กรใช้ Google หรือ Microsoft — เสียบทีหลังได้โดยไม่แตะบัญชีเดิม)

**Status** `Accepted` (13 ส.ค. 2026)
⚠️ ต้องทำ **ก่อน** หน้า settings (ดู TD-015 เมื่อมี) และ EF migration ของ TD-013 ควรรอไปสร้าง
พร้อมกับ schema ของ TD นี้ในตัวเดียว จะได้ไม่เสีย migration เปล่า

---

## รายการที่รอข้อมูลจากทีมก่อนตัดสินใจ

| คำถาม | ทำไมถึงบล็อก |
|---|---|
| จะ deploy ที่ไหน (Azure / Huawei Cloud / on-prem)? | กำหนดทิศทาง TD-001, TD-002, TD-006 พร้อมกัน (โค้ดรองรับ Huawei OBS + ModelArts อยู่แล้ว ซึ่งอาจเป็นสัญญาณ) |
| ~~School Bright มี identity provider ให้ใช้ร่วมหรือไม่?~~ | ตอบแล้ว 13 ส.ค. 2026 — ไม่ผูกกับ IdP ตัวเดียว ต้องรองรับทั้ง password และ SSO (ดู TD-014) |
| ~~ระบบต้องรองรับหลายโรงเรียนแบบแยกข้อมูลกันหรือไม่?~~ | ตอบแล้ว — ใช่ ทำเสร็จแล้ว (TD-010/TD-011) |
| ปริมาณคาดหวัง — กี่ session ต่อวัน, พร้อมกันสูงสุดเท่าไร? | ตัดสินเรื่อง scale-out, Pinecone vs pgvector, และงบ TTS |
