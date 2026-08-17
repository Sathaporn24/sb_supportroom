# SOLUTION_ARCHITECTURE — คลังความรู้เชิงสถาปัตยกรรมของ SupportRoom AI

> ประเมินเมื่อ **11 สิงหาคม 2026** — ราคาและความสามารถของบริการภายนอกเปลี่ยนเร็วมาก
> ตรวจสอบซ้ำก่อนตัดสินใจจริงเสมอ และอัปเดตวันที่เมื่อทบทวนใหม่
>
> เอกสารนี้ตอบคำถามว่า "ถ้ามีความต้องการใหม่ ทางเลือกมีอะไรบ้าง" — ไม่ใช่ใบสั่งให้เปลี่ยนของ
> ค่า default ของทุกหัวข้อคือ **ไม่ต้องเปลี่ยน** จนกว่าจะมีเหตุผลจากความต้องการจริง
>
> สถานะที่ใช้: `Recommended` · `Potential` · `Only if needed` · `Not suitable currently`

---

## Current Architecture (สรุปสั้น)

Next.js 15 (browser-only) ↔ ASP.NET Core .NET 10 (REST + SignalR) ↔ PostgreSQL
พร้อม provider ที่สลับได้ด้วย env var: Google Slides / PDF, Edge TTS, Gemini หรือ
OpenAI-compatible สำหรับ RAG, Pinecone สำหรับ vector, local disk หรือ Huawei OBS สำหรับไฟล์
รายละเอียดเต็มอยู่ใน [`PROJECT_CONTEXT.md`](./PROJECT_CONTEXT.md)

จุดแข็งเชิงสถาปัตยกรรมที่ควรรักษา:

- **Provider abstraction ที่แท้จริง** — เปลี่ยน vendor ได้ด้วย env var ตัวเดียว ทำให้ทุกข้อเสนอ
  ในเอกสารนี้มีต้นทุนการทดลองต่ำผิดปกติ (เขียน provider ใหม่ 1 คลาส ไม่แตะโค้ดเรียกใช้)
- **Pure reducer + effect runner** ฝั่ง frontend — ทดสอบ flow การสอนได้โดยไม่ต้องมีเสียงจริง
- **Graceful degradation หลายชั้น** — TTS พัง, retrieval พัง, Slides พัง ล้วนมีทางลง ไม่ล้มทั้ง session
- **คอมเมนต์เชิงเหตุผลหนาแน่น** — บันทึกว่าเคยพังยังไงจริง ๆ ประหยัดเวลาคนต่อไปมหาศาล

## Existing Capabilities

ระบบทำสิ่งเหล่านี้ได้แล้ว — อย่าสร้างซ้ำ

- จัดการบทเรียน + สร้างลิงก์ session ที่มีวันหมดอายุ
- เนื้อหาสอนจาก Google Slides (ดึง speaker notes สด) หรือ PDF (parse + render เป็นภาพต่อหน้า)
- อัปโหลด PDF/PPTX/DOCX/XLSX เข้า knowledge base พร้อม index แบบ background
- RAG ที่ query สอง namespace พร้อมกัน (บทเรียน + `kb-global`) merge แล้วกรองด้วย score threshold
- Push-to-Talk พร้อมเสียงคั่นระหว่างรอที่ prefetch ไว้ล่วงหน้าและไต่ระดับตามเวลารอ
- แสดงสไลด์ที่คำตอบอ้างอิงระหว่างพูด แล้วกลับสไลด์เดิมพร้อมประโยคเชื่อม
- แชตสดสองทางผ่าน SignalR พร้อม history hydration
- สรุป session + รายการที่ตอบไม่ได้
- Re-index ทั้งระบบด้วย endpoint เดียว
- Error envelope และ correlation id ที่ทำให้ grep log ครั้งเดียวเห็นทั้ง request

## Missing Capabilities

| สิ่งที่ขาด | ผลกระทบ |
|---|---|
| Authentication / authorization | `/admin/*` เปิดสาธารณะเมื่อ deploy |
| Rate limiting / abuse prevention | ค่า API ไม่มีเพดาน |
| Durable job queue | งาน index หายเมื่อ restart |
| Vector lifecycle (ลบ/อัปเดต) | ตอบจากเอกสารที่ลบไปแล้ว |
| Observability (metrics/tracing/error tracking) | รู้ว่าพังก็ต่อเมื่อมีคนบอก |
| RAG quality evaluation | ปรับ threshold ด้วยความรู้สึก |
| CI/CD และ deployment artifact | ปล่อยของด้วยมือทั้งหมด |
| Multi-instance readiness | SignalR + MemoryCache ผูกกับ instance เดียว |
| Backend session-expiry enforcement | token หมดอายุยังใช้ได้ผ่าน API ตรง |
| Cost tracking ต่อ session | ไม่รู้ว่าหนึ่ง session ราคาเท่าไร |

---

## Technology Ecosystem

### 1. Voice — Text to Speech (สำคัญที่สุด)

**ปัญหาปัจจุบัน:** `EdgeTTS.DotNet` เรียก Microsoft Read-Aloud แบบไม่เป็นทางการ
ธันวาคม 2025 Microsoft เพิ่ม anti-abuse token และกรอง IP ของ datacenter — รายงานจากชุมชนตรงกันว่า
IP ของ cloud provider เชื่อมต่อได้แต่ไม่ได้รับ audio frame กลับมา ร่องรอยในโค้ด (chunk 180 ตัวอักษร,
retry 2 ครั้ง, timeout 12 วินาที, คอมเมนต์ถึง 502 จริงที่ 24–46 วินาที) สอดคล้องกับอาการนี้ทั้งหมด
**ถือเป็นข้อจำกัดที่บล็อก production ไม่ใช่แค่ปัญหาคุณภาพ**

| ตัวเลือก | แก้ปัญหาอะไร | ข้อดี | ข้อเสีย | ความยาก | สถานะ |
|---|---|---|---|---|---|
| **Azure Speech (Neural TTS)** | เสียงเดียวกันเป๊ะ (`th-TH-PremwadeeNeural`) แบบมี SLA | คุณภาพ/เสียงไม่เปลี่ยนจากเดิมเลย, รองรับ SSML rate ที่โค้ดใช้อยู่, ~$16/1M อักขระ | ต้องมี Azure subscription | **ต่ำมาก** — provider ใหม่ 1 คลาส, `TTS_PROVIDER=azure` | `Recommended` |
| Google Cloud TTS | ทางเลือกราคาใกล้เคียง | อยู่ในระบบ Google เดียวกับ Slides/Gemini อยู่แล้ว, ราคาใกล้เคียง Azure | เสียงไทยคนละตัว ต้องให้ทีมฟังก่อน | ต่ำ | `Potential` |
| Gemini TTS / Live API | รวมทุกอย่างไว้ผู้ให้บริการเดียว | ลดจำนวน vendor, ค่าใช้จ่ายต่อ output token ต่ำ | คุณภาพเสียงไทยต้องประเมินเอง, API ยังเปลี่ยนบ่อย | ต่ำ–กลาง | `Potential` |
| ElevenLabs | คุณภาพ/อารมณ์สูงสุด | เสียงเป็นธรรมชาติที่สุด, latency ต่ำ (Flash v2.5) | แพงกว่าชัดเจน, เกินความจำเป็นของ narration สอนงาน | ต่ำ | `Only if needed` |
| self-host (XTTS/F5-TTS ฯลฯ) | ตัดค่า API รายเดือน | ควบคุมเต็ม, ไม่ส่งข้อมูลออก | ต้องมี GPU + คนดูแล, คุณภาพไทยเป็นความเสี่ยง | สูง | `Not suitable currently` |
| อยู่กับ Edge TTS + residential proxy | ไม่ต้องแก้อะไร | ต้นทุน $0 | เปราะบาง, พึ่ง proxy, ผิดเจตนาผู้ให้บริการ | ต่ำ | `Not suitable currently` |

> โค้ดวันนี้พร้อมมาก: `ITtsProvider` + `TtsProviderFactory` มีอยู่แล้ว, `SynthesizeSpeechDto`
> รับ `rate` แบบ SSML percentage อยู่แล้ว — งานคือเขียนคลาสเดียวและเพิ่มค่าใน `TtsProvider.Allowed`

### 2. Voice — Speech to Text

ปัจจุบัน Gemini ทำหน้าที่ถอดเสียงในทุก provider (รวมถึงตอนใช้ `openai-rag`)

| ตัวเลือก | แก้ปัญหาอะไร | ข้อดี | ข้อเสีย | ความยาก | สถานะ |
|---|---|---|---|---|---|
| **คงไว้ที่ Gemini** | — | ใช้งานได้จริง, key เดียว, ราคาถูก, รองรับไทยดี | ผูกกับ Gemini quota | — | `Recommended` |
| Typhoon ASR Real-time | ASR ไทยแบบ streaming, open source | ออกแบบมาเพื่อไทยโดยเฉพาะ, CER เทียบเท่า Pathumma-Whisper-Large-v3 ที่ประหยัดกว่า ~45×, เปิดทางสู่ streaming แทน Push-to-Talk | ต้อง host เอง, เพิ่มชิ้นส่วนใหม่ในระบบ | กลาง–สูง | `Potential` (ถ้าจะทำ barge-in/streaming) |
| Deepgram / AssemblyAI / ElevenLabs Scribe | streaming + latency ต่ำ | ผู้ให้บริการเฉพาะทาง มี SLA | ต้องยืนยันคุณภาพภาษาไทยเองก่อน, เพิ่ม vendor | ต่ำ–กลาง | `Only if needed` |

### 3. AI / RAG

| หัวข้อ | ประเมิน |
|---|---|
| **สถาปัตยกรรม RAG ปัจจุบัน** | เหมาะกับปัญหานี้แล้ว: chunk = 1 สไลด์, dual-namespace query, score threshold, fallback เป็น full-deck — ไม่ต้องรื้อ |
| **LangChain / LlamaIndex / Semantic Kernel** | `Not suitable currently` — pipeline ที่นี่มีแค่ embed → query → prompt ซึ่งเขียนตรง ๆ อ่านง่ายกว่าและ debug ง่ายกว่า framework การเพิ่ม abstraction ตอนนี้คือหนี้สุทธิ |
| **`gemini-flash-latest` alias** | `Potential` ให้ pin เป็นเวอร์ชันชัดเจนใน production แล้วเลื่อนอย่างตั้งใจ — alias ทำให้พฤติกรรมเปลี่ยนได้โดยไม่มี commit |
| **Reranking (Cohere Rerank / cross-encoder)** | `Only if needed` — จะช่วยเมื่อพบว่า top-K ดึงของผิดบ่อย ควรมี eval ก่อนจึงจะรู้ว่าคุ้มไหม |
| **Hybrid search (BM25 + vector)** | `Potential` — ภาษาไทยไม่มีช่องว่างระหว่างคำ ทำให้ keyword search ต้องมี tokenizer เพิ่ม แต่ช่วยได้มากกับคำถามที่มีชื่อเมนู/ปุ่มเฉพาะ |
| **Structured output / JSON schema mode** | `Recommended` (เล็ก) — ตอนนี้ parse ด้วยการหา `{...}` นอกสุด และมีคอมเมนต์ว่า GLM ห่อ ```` ```json ```` การใช้ response schema ของ Gemini/`json_schema` ของ OpenAI จะลดความเปราะ |
| **Prompt versioning** | `Potential` — prompt ฝังใน C# string ตรง ๆ การแยกออกมาพร้อม version จะทำให้ทดลอง A/B ได้ |
| **Guardrails / PII filtering** | `Only if needed` — วันนี้เนื้อหาเป็นการสอนใช้ระบบ ความเสี่ยงต่ำ แต่ต้องระบุใน privacy notice ว่าเสียงถูกส่งออกไป |
| **MCP** | `Not suitable currently` — ไม่มี tool-calling ในระบบนี้ |

### 4. Vector Database

| ตัวเลือก | ข้อดี | ข้อเสีย | ความยาก | สถานะ |
|---|---|---|---|---|
| **คง Pinecone** | ใช้งานได้แล้ว, ไม่มีงาน ops | ขั้นต่ำ ~$50/เดือนไม่ว่าจะมีกี่ vector, serverless ลบด้วย metadata filter ไม่ได้, เป็นระบบที่สองที่ต้อง sync กับ Postgres | — | `Recommended` (สำหรับตอนนี้) |
| **pgvector บน PostgreSQL เดิม** | ตัดบริการที่สองทิ้ง, ลบ vector อยู่ใน transaction เดียวกับลบเอกสาร (แก้หนี้ข้อ 6 ได้ที่ราก), join vector กับข้อมูล relational ได้, ต้นทุนส่วนเพิ่มเกือบ 0, ที่ขนาด <5M vector เร็วพอ ๆ กัน | ต้องเขียน provider ใหม่ + re-index, ต้องดูแล HNSW index เอง | กลาง (`IKnowledgeIndexProvider` มีอยู่แล้ว) | `Potential` — น่าสนใจมากสำหรับขนาดงานจริงของโปรเจกต์นี้ (หลักพันถึงหมื่น chunk) |
| Qdrant / Weaviate | ฟีเจอร์แน่นกว่า pgvector | ยังเป็นบริการที่สองอยู่ดี ไม่ได้แก้ปัญหาหลัก | กลาง | `Only if needed` |

> **ประมาณการขนาดจริง:** 1 chunk ต่อสไลด์ + chunk ต่อหน้าเอกสาร — ต่อให้มี 100 บทเรียนและ
> 500 เอกสาร ก็ยังอยู่ระดับหมื่น vector ซึ่งอยู่ในโซนที่ pgvector ทำได้สบาย
> เหตุผลเดียวที่ยังไม่ควรย้ายทันทีคือ "ของที่ใช้อยู่ยังทำงานได้" ไม่ใช่เรื่องความสามารถ

### 5. Real-time Communication

| ตัวเลือก | ประเมิน |
|---|---|
| **SignalR (ปัจจุบัน)** | `Recommended` — เหมาะกับ chat + question broadcast ซึ่งเป็นทั้งหมดที่ต้องการวันนี้ |
| **SignalR + Redis backplane** | `Only if needed` — จำเป็นเมื่อ scale เกิน 1 instance เท่านั้น เพิ่มทีหลังได้ ไม่ต้องแก้โค้ด business |
| **Azure SignalR Service** | `Potential` — ถ้าไปทาง Azure อยู่แล้ว (จาก Azure Speech) จะได้ scale-out แบบ managed |
| **WebRTC / LiveKit / Daily / Agora** | `Not suitable currently` — ห้องนี้ *ดูเหมือน* video call แต่ไม่มีการส่งสื่อ peer-to-peer จริง กล้องคุณครูเป็น local preview ล้วน การเอา WebRTC เข้ามาคือความซับซ้อนที่ไม่มีความต้องการรองรับ |
| **Streaming TTS ผ่าน WebSocket** | `Potential` — จะลด time-to-first-audio ได้จริง แต่ต้องรื้อ effect runner ที่ตอนนี้เล่นเป็นก้อน (blob) เก็บไว้พิจารณาหลังแก้ TTS provider เสร็จ |

### 6. Background Processing

`IBackgroundTaskQueue` ปัจจุบันเป็น in-memory channel — งานที่ค้างหายเมื่อ restart

| ตัวเลือก | ข้อดี | ข้อเสีย | ความยาก | สถานะ |
|---|---|---|---|---|
| **Hangfire (PostgreSQL storage)** | ใช้ DB ที่มีอยู่, retry อัตโนมัติ, dashboard ดูงานค้าง/ล้มเหลว, ปรับ interface เดิมได้เกือบตรง | เพิ่มตาราง Hangfire ในฐานข้อมูลเดียวกัน | ต่ำ–กลาง | `Recommended` (เมื่อจะขึ้น production) |
| Postgres-backed outbox เขียนเอง | ควบคุมเต็ม, ไม่มี dependency ใหม่ | ต้องเขียน retry/backoff/poison-handling เอง — คือการสร้าง Hangfire ฉบับด้อยกว่า | กลาง | `Only if needed` |
| Quartz.NET | เก่งเรื่อง schedule ตามปฏิทิน | ไม่มี queue/dashboard/retry ในตัว ซึ่งคือสิ่งที่ต้องการพอดี | กลาง | `Not suitable currently` |
| Temporal / RabbitMQ / Kafka | ทนทานระดับ workflow/สตรีม | เกินความต้องการมากสำหรับงาน "index เอกสารที่เพิ่งอัปโหลด" | สูง | `Not suitable currently` |

> ทางที่ถูกที่สุดชั่วคราวโดยไม่เพิ่ม dependency: ตอน startup หา `DocumentResource` ที่ค้าง
> `pending` แล้ว enqueue ใหม่ — แก้เคสหลัก (restart) ด้วยโค้ดไม่กี่บรรทัด

### 7. Authentication & Authorization

| ตัวเลือก | ข้อดี | ข้อเสีย | ความยาก | สถานะ |
|---|---|---|---|---|
| **แยก admin ออกจาก public แล้วใส่ JWT/cookie auth ของ ASP.NET Core** | ในตัว, ไม่มี vendor, ครอบคลุมความเสี่ยงหลัก (`/admin/*` + `/api/admin/*`) | ต้องมีที่เก็บ user (แม้แค่ตารางเดียว) | ต่ำ–กลาง | `Recommended` |
| ป้องกันด้วย reverse proxy / IP allowlist / VPN | เร็วที่สุด ไม่แตะโค้ด | หยาบ, ไม่รู้ว่าใครทำอะไร | ต่ำมาก | `Potential` (มาตรการชั่วคราวก่อน deploy) |
| SSO เข้ากับระบบพนักงาน School Bright ที่มีอยู่ | ผู้ใช้จริงคือพนักงานอยู่แล้ว, ไม่มีรหัสผ่านชุดใหม่ | ขึ้นกับว่ามีอะไรให้เชื่อมบ้าง | กลาง | `Potential` — **ต้องถามทีมก่อนว่ามี IdP อยู่แล้วหรือไม่** |
| Clerk / Auth0 / Supabase Auth | เสร็จเร็ว, UI ครบ | ค่าใช้จ่ายต่อเนื่อง + vendor ใหม่ สำหรับผู้ใช้ภายในไม่กี่คน | ต่ำ | `Only if needed` |
| Keycloak | ครบ, self-host, ฟรี | ต้องดูแล service เพิ่มอีกตัว | สูง | `Not suitable currently` |
| **Rate limiting: `Microsoft.AspNetCore.RateLimiting` ในตัว** | มากับ framework, ใส่ policy เฉพาะ `/api/tts` และ `/api/voice-question` ได้ | นับแยกต่อ instance (หลาย instance = เพดานคูณจำนวน instance) | **ต่ำมาก** | `Recommended` |

### 8. Storage & Media

| ตัวเลือก | ประเมิน |
|---|---|
| **local disk / Huawei OBS (ปัจจุบัน)** | `Recommended` — `IDocumentStorageProvider` ทำผ่าน AWS S3 SDK อยู่แล้ว จึงเข้ากับ S3-compatible อะไรก็ได้ |
| Cloudflare R2 / AWS S3 / MinIO | `Potential` — ใช้ provider เดิมได้แทบทันที เลือกตามที่ deploy จริงและเรื่อง egress |
| CDN หน้าหน้า PDF ที่ render แล้ว | `Only if needed` — ตอนนี้ `MemoryCache` 10 นาทีเพียงพอ; ถ้าเปิดห้องพร้อมกันเยอะค่อยพิจารณา |
| Pre-render หน้า PDF ตอนอัปโหลดแทน on-demand | `Potential` — ย้ายงานหนักออกจาก critical path ของการเปิดห้อง ใช้ queue ที่มีอยู่ได้เลย |

### 9. Monitoring & Observability

| ตัวเลือก | ข้อดี | ข้อเสีย | ความยาก | สถานะ |
|---|---|---|---|---|
| **OpenTelemetry (.NET) → OTLP** | มาตรฐานกลาง, .NET รองรับดี, เปลี่ยน backend ทีหลังได้โดยไม่แก้ instrumentation, ต่อกับ `CorrelationId` ที่มีอยู่แล้วได้ | ต้องมีที่ส่งไป | ต่ำ–กลาง | `Recommended` |
| Seq | อ่าน Serilog structured log ได้ทันที, ติดตั้งง่าย, ฟรีสำหรับ single user | เน้น log อย่างเดียว | ต่ำมาก | `Recommended` (ก้าวแรกที่คุ้มที่สุด) |
| Sentry | error tracking + alert ทั้งฝั่ง .NET และ Next.js | อีกหนึ่ง SaaS | ต่ำ | `Potential` |
| **Langfuse (self-host ได้)** | เห็น trace ของ LLM: prompt/latency/token/cost ต่อคำถาม ซึ่งวันนี้มองไม่เห็นเลย, มี dataset + eval ในตัว | ต้องเติม instrumentation ในโค้ด provider | กลาง | `Potential` — คู่กับหัวข้อ RAG evaluation |
| Grafana + Prometheus | มาตรฐาน metrics | ต้องดูแล stack เอง | กลาง–สูง | `Only if needed` |

> คุ้มที่สุดในเชิงต้นทุน/ผลลัพธ์วันนี้: log counter ง่าย ๆ ของอัตรา `answerStatus` แต่ละแบบ
> และ latency แต่ละขั้นของ voice pipeline — ตอบคำถามว่า "AI ตอบได้จริงแค่ไหน" ซึ่งยังไม่มีใครตอบได้

### 10. Testing

| ตัวเลือก | ประเมิน |
|---|---|
| **แยก unit/integration ด้วย xUnit trait** | `Recommended` — จำเป็นก่อนตั้ง CI เพราะ test บางตัวยิง provider จริง |
| **Testcontainers for .NET (PostgreSQL)** | `Recommended` — ทำให้ `SupportRoom.Api.IntegrationTests` ที่ยังว่างมีค่าจริง ทดสอบ endpoint กับ Postgres จริงได้ |
| Playwright | `Potential` — จับ regression ของ flow ห้องสอนได้ แต่ต้อง mock เสียง/ไมค์ ลงทุนพอควร |
| WireMock.NET | `Potential` — ทดสอบ provider โดยไม่ยิงของจริงและไม่เสียเงิน |
| RAG eval set (คำถาม + คำตอบที่ควรได้ + สไลด์ที่ควรอ้างอิง) | `Recommended` — เป็น test ที่คุ้มที่สุดที่ยังไม่มี ทำให้ปรับ `RAG_MIN_SCORE`/`RAG_TOP_K`/เปลี่ยนโมเดล วัดผลได้แทนการเดา |
| k6 / NBomber | `Only if needed` |

### 11. Deployment

| ตัวเลือก | ประเมิน |
|---|---|
| **Dockerfile + docker compose (api, frontend, postgres)** | `Recommended` — ไปที่ไหนก็ได้ต่อ, แก้ปัญหา "ไม่มี artifact" ที่รากที่สุด ⚠️ `PDFtoImage` ต้องมี native PDFium ใน image |
| GitHub Actions CI (build + test ทั้งสองฝั่ง) | `Recommended` — โฟลเดอร์ `.github/workflows/` มีอยู่แล้วแต่ว่าง เป็นงานตั้งต้นที่ชัดเจน |
| Azure App Service / Container Apps | `Potential` — เข้ากันดีถ้าเลือก Azure Speech + Azure SignalR ⚠️ ต้องยืนยันว่า outbound IP ใช้กับ TTS ที่เลือกได้ |
| Huawei Cloud | `Potential` — สอดคล้องกับ OBS และ ModelArts ที่โค้ดรองรับอยู่แล้ว น่าจะเป็นทิศทางองค์กร — **ต้องยืนยันกับทีม** |
| .NET Aspire | `Only if needed` — ช่วยเรื่อง local orchestration แต่ไม่ได้แก้ปัญหาที่เจ็บอยู่ |
| Kubernetes | `Not suitable currently` — งานระดับนี้ยังไม่ต้องการ |

---

## MVP vs Production vs Scale

| ด้าน | MVP (วันนี้) | Production (ที่ควรมีก่อนใช้จริง) | Scale (เมื่อโตแล้วเท่านั้น) |
|---|---|---|---|
| TTS | Edge TTS | **Azure Speech (เสียงเดิม)** | streaming TTS + cache ประโยคที่ซ้ำ |
| Auth | ไม่มี | JWT/cookie สำหรับ admin + rate limiting | SSO องค์กร, RBAC |
| Queue | in-memory | Hangfire + Postgres | worker แยก process |
| Vector | Pinecone | Pinecone หรือ pgvector | ตัดสินจาก QPS จริง |
| Realtime | SignalR in-process | SignalR + sticky session | Redis backplane / Azure SignalR |
| Observability | Serilog file | Seq หรือ OTLP + error tracking | Langfuse + metrics dashboard |
| Deploy | รันมือ | Docker + GitHub Actions | autoscale หลาย instance |
| Testing | unit tests | แยก CI + integration ด้วย Testcontainers | E2E + RAG eval ใน pipeline |

## Security & Privacy Checklist

- [ ] `/admin/*` และ `/api/admin/*` ต้องไม่เปิดสาธารณะเมื่อ deploy
- [ ] `ALLOW_DATA_RESET=false` ใน production (ปัจจุบัน `.env.example` ตั้งเป็น `true`)
- [ ] `ALLOWED_ORIGINS` ระบุ domain จริง (localhost ถูกเพิ่มเฉพาะ Development แล้ว — ดีอยู่)
- [ ] Rate limit `/api/tts` และ `/api/voice-question`
- [ ] Backend บังคับ session expiry ด้วย ไม่ใช่แค่ frontend
- [x] ไม่ log transcript/answer เต็ม (ทำแล้ว)
- [x] ป้องกัน path traversal ใน local storage (ทำแล้ว)
- [ ] แจ้งผู้ใช้ว่าเสียงถูกส่งไป Gemini และ transcript อาจถูกส่งไป OpenAI/gateway
- [ ] หมุน API key ที่เคยอยู่ใน git history (ประวัติ commit มีร่องรอยการเปลี่ยน key หลายครั้ง)
- [ ] Multi-company isolation — `CompanyId` + global query filter + แยก Pinecone namespace
      (ตัดสินใจแล้ว TD-010/011/012 ยังไม่ได้ลงมือ) ⚠️ company = เจ้าของ support room
      (School Bright / SCB) ไม่ใช่โรงเรียนที่รับลิงก์

## Cost Awareness

| รายการ | ประเภท | หมายเหตุ |
|---|---|---|
| Edge TTS | `Free / Open Source` | ฟรีแต่ไม่มี SLA และกำลังถูกปิดกั้น |
| Azure / Google Cloud TTS | `Usage Based` | ~$16/1M อักขระ — narration หนึ่งบทเรียนอยู่หลักสตางค์ |
| ElevenLabs | `Potentially Expensive at Scale` | |
| Gemini Flash (ถอดเสียง + ตอบ) | `Usage Based` | ระดับ Flash ราคาต่ำ; Pro ไม่มี free tier แล้วตั้งแต่ เม.ย. 2026 |
| OpenAI-compatible answer step | `Usage Based` | เหตุผลเดิมที่เพิ่มเข้ามาคือกระจายโหลดออกจาก quota Gemini |
| Pinecone | `Usage Based` | ขั้นต่ำ ~$50/เดือน — จ่ายเท่ากันตั้งแต่ 100K ถึงหลายล้าน vector |
| pgvector | `Free / Open Source` | ใช้ Postgres ที่จ่ายอยู่แล้ว |
| Hangfire | `Free / Open Source` | (Hangfire Pro เสียเงิน แต่ไม่จำเป็น) |
| Seq / Langfuse (self-host) | `Free / Open Source` | |
| Huawei OBS | `Usage Based` | |

**ยังไม่มีใครวัด:** ต้นทุนต่อ 1 session (TTS อักขระ + Gemini token + embedding + Pinecone read)
ควรใส่ log ให้คำนวณได้ ก่อนตัดสินใจเรื่องราคาใด ๆ

## Solution Design Rule

ก่อนลงมือทำฟีเจอร์ที่ไม่ trivial:

1. เข้าใจความต้องการจริง (ปัญหาของผู้ใช้ ไม่ใช่วิธีแก้ที่เขาเสนอมา)
2. ไล่ flow ที่เกี่ยวข้องในระบบเดิมให้จบก่อน
3. หาโค้ด/service/endpoint ที่ใช้ซ้ำได้ — โปรเจกต์นี้มี provider abstraction ที่ดีอยู่แล้ว
4. ถามว่ามี library/API/บริการที่แก้ปัญหานี้แล้วหรือยัง
5. เทียบทางเลือก 2–4 แบบพร้อม trade-off
6. เลือกสถาปัตยกรรมที่ง่ายที่สุดที่ตอบความต้องการ *ปัจจุบัน*
7. อย่าสร้าง infrastructure ล่วงหน้า
8. บันทึกการตัดสินใจสำคัญลง [`TECH_DECISIONS.md`](./TECH_DECISIONS.md)
9. ลงมือเมื่อทิศทางชัดแล้ว
10. ตรวจด้วย lint/typecheck/test/build ทั้งสองฝั่ง แล้วอ่าน `git diff` ก่อนส่ง
