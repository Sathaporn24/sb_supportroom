# Knowledge Base & Teaching Content Intake — Implementation Plan

## ⚠️ Cross-module dependency — read before Phase 1

**Local development/rehearsal:** ก่อน generate/test migration ของ Phase 1 ให้ local/rehearsal DB
apply baseline ของ `learning-session` ถึง `20260818155126_AddTotalSlideCount` ก่อน (ดู
`_docs/status.md` §learning-session และ `design.md` §Risks R-1) — local Compose ปัจจุบันผ่าน
เงื่อนไขนี้แล้ว จึงเริ่มพัฒนาได้โดยไม่ต้องรอ shared/production deployment

**Shared/production:** ก่อน apply migration จริง `devops`/ผู้ deploy ต้องยืนยันลำดับ migration
ทั้งหมด รวมถึง migration ค้างของ `learning-session` และมี backup ที่กู้คืนได้ก่อนเสมอ

## Plan Summary

โปรเจกต์นี้ **scaffold แล้ว** (ASP.NET Core .NET 10 + Next.js 15 มีโค้ดจริงอยู่แล้วสำหรับ
`learning-session`) แต่โมดูล `knowledge-base` เอง **ยังไม่มีโค้ดอะไรเลยแม้แต่บรรทัดเดียว** —
ทุก phase ด้านล่างเป็นงานสร้างใหม่ทั้งหมด ไม่มีงานเก่าให้ต่อ

12 phase เรียงตาม dependency chain ที่ `design.md` §Modules ระบุไว้ตรงตัว:
**Phase 1 (Module A) → Phase 2 (Module B) → Phase 3 (Module C) → Phase 4/5/6 (Module D, E, F
ขนานกันได้)**. Phase 1 ต้องเสร็จเป็นก้อนเดียว ห้ามแบ่งครึ่ง เพราะเป็น breaking migration ที่ทุก
phase อื่นพึ่งพา (`LessonConfig.CategoryId` required, `DocumentResource.ScopeType`/`ScopeId`
แทนที่ `LessonId` ทั้งคอลัมน์) — ปล่อยให้ codebase มีทั้ง `LessonId` เก่าและ `ScopeType` ใหม่ปนกัน
จะทำให้ query ครึ่งหนึ่งอ่านข้อมูลผิดชุด; migration ยังต้องคง default chain สองแถวต่อบริษัทตาม
MG-A3 เพื่อให้ทุก `LessonConfig.CategoryId` ชี้ Level 2 leaf ที่ assign ได้

Phase 2 (namespace ระดับหมวด) ต้องมาก่อน Phase 3 เพราะ `vector_delete` job ของ Phase 3 ต้องลบ
vector จาก namespace ที่ถูกต้องตั้งแต่แรก Phase 3 (durable queue) ต้องมาก่อน Phase 4/5/6 เพราะ
ทั้งสามใช้ `BackgroundJob` เป็นกลไก index (Module D อ่าน chunk ที่ worker ของ C เขียน, Module E
enqueue `lesson_index` ผ่าน worker เดียวกัน, Module F enqueue `qna_index`)

**เพิ่ม 2026-08-20 — Phase 7 (Module G) 🔒 Security gate**: `qa-engineer` FULL รอบแรกพบว่า R3
("เอกสารระดับหมวด") ถูกสร้างครบเฉพาะฝั่งอ่าน — ไม่มี DTO/endpoint/UI ไหนตั้งแต่ Phase 1–6 เซ็ต
`DocumentResource.ScopeType = "category"` ได้จริง `system-analyst` amend `design.md` ปิดช่องว่างนี้
แล้ว (DS-1..DS-12) ไม่มี migration ใหม่เพราะ `ScopeType`/`ScopeId` มีอยู่แล้วตั้งแต่ Phase 1 — สิ่งที่
breaking มีแค่ wire contract ของ `/api/documents` (`lessonSlug` → `scopeType`/`scopeId`) ซึ่งต้องแก้
backend+frontend พร้อมกันในเฟสเดียว ห้ามปล่อยคร่อม Phase 7 ขึ้นกับ Phase 1–4 (มีโค้ดอยู่แล้ว) แต่ควร
เริ่มลงมือจริงหลัง Phase 3 ปิด issue cross-company leak ที่ค้างอยู่ก่อน (ดู Sequencing Notes)

Phase 4/5/6 (Module D, E, F) ไม่มี dependency ระหว่างกันเอง ทำขนานได้เมื่อ Phase 1–3 ปิดหมดแล้ว

Phase 7–12 เป็น amendment ตามลำดับ Module G → H → I → J → K → L ที่ออกแบบเพิ่มภายหลัง;
โดย Module L ต้องตามหลัง Module K เพราะ purge ต้องรู้จักและลบ `LessonExcludedSlide` ได้ครบ พร้อม
ใช้ durable-job/vector/chunk/Q&A/narration ของ Phase 3–6 ที่มีอยู่แล้ว. Phase 12 เป็น lifecycle
destructive ข้าม public learner flow, Pinecone, object storage และ PostgreSQL จึงติด `🔒 Security gate`.

🔒 **Security gate ติดที่ Phase 2, 3, 4, 6** ตรงตาม `design.md` §Modules (B, C, D, F) — Phase 1
และ Phase 5 (Module A, E) ไม่ติด gate ด้วยเหตุผลที่ `design.md` เขียนไว้แล้วในแต่ละหัวข้อ Module

โปรเจกต์นี้มี test suite จริง (xUnit ฝั่ง backend, Vitest ฝั่ง frontend) — ไม่ได้ opt-out เหมือน
โปรเจกต์ template — จึงมีงาน `[backend]` test แยกสำหรับ pure logic 3 จุดที่ `design.md` R-12
ชี้ไว้ว่า test ได้โดยไม่ต้องพึ่ง provider จริง: namespace resolver (KS-1), แผนที่ผลลัพธ์→สถานะ
(DI-5), นิยามคิว (QQ-1)

## Phase 1: Taxonomy foundation & migration

- [x] [backend] สร้าง entity `KnowledgeCategory` ตาม DM-1 (`SupportRoom.Domain/Entities/KnowledgeCategory.cs`) — self-referencing ผ่าน `ParentId`, `Level`, `IsSystemDefault`
- [x] [backend] แก้ entity `LessonConfig` — เพิ่ม `CategoryId` (`required string`, `set`) ตาม DM-2
- [x] [backend] แก้ entity `DocumentResource` ตาม DM-3 — ลบคอลัมน์ `LessonId` ทั้งใบ, เพิ่ม `ScopeType`/`ScopeId`/`FailureReason`, เปลี่ยน `DeleteBy`/`IsDelete`/`DeletedAt` จาก `init` เป็น `set`
- [x] [backend] สร้าง `SupportRoom.Domain/Enums/KnowledgeScopeType.cs` (`static class` — `Lesson`/`Category`/`Company`) ตาม DM-11
- [x] [backend] สร้าง `SupportRoom.Domain/Enums/DocumentFailureReason.cs` (`static class` — `UnsupportedType`/`ExtractFailed`/`NoText`/`EmbeddingFailed`/`IndexFailed`) ตาม DM-11
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม `DbSet<KnowledgeCategory>`, index ตาม DM-15 (`KnowledgeCategory`, `LessonConfig.CategoryId`, `DocumentResource` ScopeType/ScopeId composite index แทน index เดิมของ `LessonId`)
- [x] [backend] สร้าง EF Core migration `AddKnowledgeTaxonomyAndScope` ครอบคลุม MG-A1..MG-A7: สร้าง `KnowledgeCategory`; เพิ่ม `LessonConfig.CategoryId` และ `DocumentResource.ScopeType`/`ScopeId`/`FailureReason` แบบ nullable ชั่วคราว; MG-A3 backfill default chain "ยังไม่จัดหมวด" **exactly 2 แถวต่อบริษัท** (Level 1 parent + Level 2 leaf ที่ `ParentId` ชี้ parent, ทั้งคู่ `IsSystemDefault = true`, `SortOrder = 9999`, exactly one flagged row ต่อ level); MG-A4 backfill `LessonConfig.CategoryId` ให้ชี้ leaf และ backfill scope จาก `LessonId`; ตั้ง `NOT NULL` แล้วลบ `LessonId`/index เดิม — **ต้องเรียงลำดับตาม MG-A1..MG-A7 ห้ามสลับ และห้ามสร้างตารางของ Phase 3–6 ล่วงหน้า**
- [x] [backend] เขียนคำเตือน rollback ที่ไม่สมบูรณ์ไว้ใน `Down()` ของ migration ตาม MG-A7
- [x] [backend] สร้าง `IKnowledgeCategoryRepository` (`GetByCompanyOrdered()`, `GetChildren(parentId)`, `GetSystemDefault()`) และ implementation ใน `SupportRoom.Providers.Data` — `GetSystemDefault()` ต้อง filter `IsSystemDefault && Level == 2` แล้ว `SingleOrDefault()` เพื่อคืน assignable leaf; ห้ามคืน parent และห้ามใช้ `FirstOrDefault()` ซ่อน leaf ซ้ำ
- [x] [backend] แก้ `IDocumentResourceRepository` ตาม DM-16 — เปลี่ยน `GetByLessonId` เป็น `GetByScope(scopeType, scopeId)`, เลิกใช้ `GetStandalone` (แทนด้วย `GetByScope(company, null)`), เพิ่ม `GetDeleted()`
- [x] [backend] แก้ `ILessonConfigRepository` — เพิ่ม `GetByCategoryId(categoryId)`, `CountByCategoryId(categoryId)`
- [x] [backend] ลงทะเบียน repository ใหม่/ที่แก้ทั้งหมดใน `UnitOfWork.Register`
- [x] [backend] สร้าง `IKnowledgeCategoryService`+impl (`IKnowledgeCategoryService.cs` ไฟล์เดียวตาม convention) — implement TX-1 (คำนวณ `Level` จาก `ParentId` ฝั่ง server เสมอ ห้ามเชื่อ client), TX-2 (ปฏิเสธหมวดย่อยซ้อนชั้นที่สาม), TX-3 (ชื่อซ้ำในพ่อเดียวกัน ตรวจที่ service layer), TX-6 (บล็อกลบหมวดที่มีของ พร้อมข้อความแยกจำนวนบทเรียน/เอกสาร และ Q&A = `0`), TX-10 (คง response `{losingDocuments, losingQnAs, gainingDocuments, gainingQnAs}` โดย Q&A counts = `0`), TX-11 (ป้องกันลบ/แก้ชื่อ/ย้ายชั้น/แยก chain ของทั้ง system-default parent และ leaf)
- [x] [backend] เพิ่ม validation TX-7 ใน `ILessonConfigService.SaveAsync` — ปฏิเสธ `Slug` ที่ขึ้นต้นด้วย `kbcat-` หรือเท่ากับ `kb-global`
- [x] [backend] เพิ่ม validation TX-4/TX-5 สำหรับ `LessonConfig.CategoryId` และ `DocumentResource` ที่ `ScopeType = category` ต้องชี้ไปแถว `Level == 2` เท่านั้น; validation ของ `KnowledgeQnA` อยู่ Phase 6 หลัง MG-F1
- [x] [backend] `GET /api/knowledge-categories` — ดึงหมวดทั้งบริษัทเรียงตามชั้น/`SortOrder`
- [x] [backend] `POST /api/knowledge-categories` — สร้างหมวด (TX-1..TX-3)
- [x] [backend] `PUT /api/knowledge-categories/{id}` — แก้ชื่อ/`SortOrder`/`Description` (TX-11 บล็อกแถว `IsSystemDefault`)
- [x] [backend] `DELETE /api/knowledge-categories/{id}` — ลบหมวด (TX-6, TX-11)
- [x] [backend] `GET /api/knowledge-categories/{id}/move-preview?targetCategoryId=...` — คืนตัวเลข 4 ค่าตาม TX-10; ใน Phase 1 `losingQnAs`/`gainingQnAs` = `0` จนกว่า Phase 6 จะเชื่อมค่าจริง
- [x] [backend] `PUT /api/lessons/{id}/category` — ย้ายบทเรียนข้ามหมวด (TX-9 — update คอลัมน์เดียว ไม่ enqueue job ใดๆ)
- [x] [backend] DTO/ViewModel สำหรับ `KnowledgeCategory` (list/detail/move-preview) ตาม convention `SupportRoom.Application`
- [x] [frontend] เพิ่ม type `KnowledgeCategory` และ union type `KnowledgeScopeType` ใน `src/types/domain.ts` ตรงกับ DM-1/DM-11
- [x] [frontend] เพิ่มเมธอดเรียก 6 endpoint หมวดข้างต้นใน `src/lib/api-client.ts`
- [x] [frontend] สร้างหน้า `/admin/categories` — เมนูจัดการหมวด 2 ชั้น (list, create, rename, delete) ใช้ component ใหม่ใน `src/components/admin/`
- [x] [frontend] เพิ่ม dropdown เลือกหมวด (เฉพาะแถว Level 2) ในหน้าแก้ไขบทเรียน `frontend/src/app/admin/lessons/[slug]/page.tsx` — เรียก `move-preview` ก่อนบันทึกเมื่อ `CategoryId` เปลี่ยน แล้วโชว์ตัวเลข 4 ค่าให้ยืนยันก่อนเรียก `PUT /api/lessons/{id}/category` (TX-10, R3.1)
- [x] [backend] unit test: `KnowledgeCategoryService`/repository/migration — TX-1 (คำนวณ Level จาก ParentId), TX-2 (ปฏิเสธชั้นที่สาม), TX-6 (นับของก่อนลบ), `GetSystemDefault()` คืน Level 2 leaf ได้เมื่อมี flagged parent+leaf และ fail-fast เมื่อ leaf ซ้ำ, TX-11 บล็อก update/delete ของทั้ง parent+leaf, และ MG-A3/MG-A4 ยืนยัน exactly 2 flagged rows ต่อบริษัท (one per level, linked chain) พร้อม `LessonConfig.CategoryId` ชี้ leaf

## Phase 2: Knowledge scope & 3-level retrieval 🔒 Security gate

**ขึ้นกับ:** Phase 1

- [x] [backend] แก้ `KnowledgeNamespaces` (`SupportRoom.Providers.Knowledge` หรือที่เดิมอยู่) — เพิ่ม `ForCategory(companyId, categoryId)` ตาม DM-12
- [x] [backend] แก้ interface `IVoiceQuestionProvider` — เพิ่ม input `CategoryNamespace` (**required string**) ตาม KS-3
- [x] [backend] แก้ `RagVoiceQuestionProvider` (Gemini/OpenAI variant ทั้งสอง) — ยิง 3 namespace พร้อมกันด้วย `Task.WhenAll` (บทเรียน + หมวด + บริษัท) แทนที่ 2 เดิม ตาม KS-3/R-2
- [x] [backend] แก้จุดเรียก `IVoiceQuestionProvider` ใน `IVoiceQuestionService` — resolve `CategoryNamespace` จาก `LessonConfig.CategoryId` ผ่าน resolver เดียว (KS-1) ก่อนเรียก provider
- [x] [backend] implement resolver namespace กลาง (KS-1) — ฟังก์ชันเดียวที่แปลง `ScopeType`/`ScopeId` เป็น namespace key ให้ทั้ง `DocumentResource` และ `KnowledgeQnA` ใช้ร่วมกัน ห้ามประกอบ key เองที่ call site อื่น
- [x] [backend] เพิ่ม validation KS-2 — ตรวจคู่ `ScopeType`/`ScopeId` ก่อนเซฟทุกครั้ง (`company` ต้องบังคับ `ScopeId == null`, `lesson`/`category` ต้องมีแถวจริงในบริษัทนี้)
- [x] [backend] เพิ่ม `metadata.sourceType = "document"` ในทุกจุดที่ `IDocumentResourceService`/`IAdminService` upsert chunk เข้า Pinecone ตาม KS-6
- [x] [backend] เพิ่ม `metadata.sourceType = "slide"` ใน `IndexLessonAsync` ตาม KS-6
- [x] [backend] แก้ตัวอ่าน metadata ฝั่งตอบคำถาม — treat "ไม่มี `sourceType`" เป็น `"document"` (ไม่ throw ไม่ทิ้ง chunk) ตาม KS-6
- [x] [backend] ยืนยัน/แก้ KS-11 — query ไปยัง namespace ที่ไม่มีอยู่ (บทเรียนที่ยังไม่เคย index) ต้อง fallback full-deck ตามพฤติกรรมเดิม ไม่ throw
- [x] [backend] unit test: namespace resolver (KS-1) — ครบ 3 กรณี `lesson`/`category`/`company` และกรณี `company` ที่ `ScopeId` ต้องเป็น null (KS-2)
- [ ] [backend] วัด latency จริงหลัง deploy 3-namespace query ตาม R-2 (บันทึกผลไว้ให้ `qa-engineer`/`devops` อ้างอิง)

## Phase 3: Durable indexing queue & failure reporting 🔒 Security gate

**ขึ้นกับ:** Phase 1 (ต้องมี `ScopeType` ก่อนถึงจะ resolve namespace ของงานได้), Phase 2 (เพื่อให้ `vector_delete` ลบจาก namespace ที่ถูกต้องตั้งแต่แรก)

- [x] [backend] สร้าง entity `BackgroundJob` ตาม DM-10 (**ไม่มี `HasQueryFilter`** โดยเจตนา — ต้อง comment อธิบายเหตุผลตาม DM-15)
- [x] [backend] สร้าง `SupportRoom.Domain/Enums/BackgroundJobType.cs` และ `BackgroundJobStatus.cs` (`static class`) ตาม DM-11
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม `DbSet<BackgroundJob>` + index `(Status, NextAttemptAt)` และ `(CompanyId, JobType, TargetId)` ตาม DM-15
- [x] [backend] สร้าง EF Core migration `AddDurableIndexingJobs` ตาม MG-C1 — สร้าง `BackgroundJob` พร้อม index ตาม DM-15
- [x] [backend] สร้าง `IBackgroundJobRepository` — `ClaimNext(now)` แบบ atomic (`UPDATE ... RETURNING` + `FOR UPDATE SKIP LOCKED` ตาม DI-12) และ `RequeueOrphanedRunning()` ทั้งคู่ `IgnoreQueryFilters()` พร้อม XML doc อธิบายเหตุผล ตาม DM-16/DI-4
- [x] [backend] ลงทะเบียน `IBackgroundJobRepository` ใน `UnitOfWork.Register`
- [x] [backend] แก้ `IKnowledgeIndexProvider` — เพิ่ม `DeleteVectorsAsync(namespaceKey, ids)` ตาม DM-13
- [x] [backend] implement `DeleteVectorsAsync` ใน `PineconeKnowledgeIndexProvider` — `POST /vectors/delete` ด้วย `ids`, ซอยเป็นชุดไม่เกิน 1000 id ต่อ request, แยก `DeleteRequest` เป็นสอง request type ไม่ให้ `deleteAll` หลุดมาพร้อม `ids`
- [x] [backend] สร้าง worker ใหม่ (hosted service) แทนที่ `IBackgroundTaskQueue`/`QueuedHostedService` เดิม — implement DI-1 (ขั้น 1–2 ใน request, ขั้น 3–5 ใน worker), DI-3 (โหลดไฟล์จาก `IDocumentStorageProvider.DownloadAsync(ObsKey)` เสมอ ห้ามส่ง `byte[]` ผ่าน `PayloadJson`), DI-4 (`ICompanyContext.Resolve(job.CompanyId)` เป็นสิ่งแรกก่อนแตะ repository ใดๆ), DI-9 (retry `MaxAttempts = 3` ด้วย backoff 1/5/15 นาทีตาม `AttemptCount`), DI-11 (`RequeueOrphanedRunning()` ตอน start โดยไม่เพิ่ม `AttemptCount`), DI-12 (polling ทุก 5 วินาทีเมื่อคิวว่าง)
- [x] [backend] แก้ `IDocumentResourceService.UploadAsync` — คงขั้น 1–2 synchronous (DI-2: ตรวจ content type ก่อนสร้าง job, ล้มที่นี่ → `failed`/`unsupported_type`) แล้ว enqueue `BackgroundJob` (`JobType = document_index`) แทนการเรียก `taskQueue.Enqueue` เดิม
- [x] [backend] implement DI-5 — แผนที่ผลลัพธ์ของ worker → `IndexingStatus`/`FailureReason` ทั้ง 4 กรณีแยกกัน (`extract_failed`/`no_text`/`embedding_failed`/`index_failed`) ไม่ยุบเป็นก้อนเดียว
- [x] [backend] implement DI-13 — ลบเอกสาร: soft delete `DocumentResource` (`Update` ไม่ใช่ `_repository.Delete()`) + soft delete `DocumentChunk` ทุกแถวของมัน + enqueue `BackgroundJob(vector_delete)` พร้อม `VectorId` ทั้งหมด + `NamespaceKey` ใน `PayloadJson` ทั้งหมดในทรานแซกชันเดียว — ไฟล์ใน object storage ไม่ถูกลบ
- [x] [backend] คง DI-14 — ปฏิเสธลบเอกสารที่เป็น `PdfDocumentResourceId` ของบทเรียนอยู่ พร้อมข้อความภาษาไทยเดิม
- [x] [backend] implement DI-15 — endpoint กู้คืนเอกสาร: ล้าง `IsDelete`/`DeletedAt`/`DeleteBy` แล้ว enqueue `BackgroundJob(document_index)` ใหม่ทั้งใบ
- [x] [backend] implement DI-16 — worker `vector_delete` retry เอง, ไม่ทำให้การลบเอกสาร (DB) ล้มเหลว
- [x] [backend] implement DI-17 — ลบ `IBackgroundTaskQueue.cs`/`BackgroundTaskQueue.cs` และการลงทะเบียนใน `ServiceConfiguration` ทิ้งทั้งหมด
- [x] [backend] แก้ ViewModel ของ `DocumentResource` — เพิ่ม `willRetryAt` ประกอบจาก job ล่าสุด (DI-10), **ห้าม map `LastErrorDetail` ลง ViewModel เด็ดขาด**
- [x] [backend] `GET /api/documents/{id}/deleted` หรือแก้ endpoint list เดิมให้รองรับ `GetDeleted()` (หน้ากู้คืน)
- [x] [backend] `POST /api/documents/{id}/restore` — เรียก DI-15
- [x] [backend] unit test: DI-5 (แผนที่ผลลัพธ์ → สถานะ ครบ 5 กรณีรวม success)
- [x] [backend] unit test: DI-9 (backoff calculation ตาม `AttemptCount`)
- [x] [frontend] แก้หน้า `frontend/src/app/admin/documents/page.tsx` — แสดง `willRetryAt` แยกจาก "ล้มเหลว ต้องทำอะไรสักอย่าง", แสดง `FailureReason` เป็นข้อความไทยที่ map จาก `DocumentFailureReason` (ไม่ใช่ raw string)
- [x] [frontend] เพิ่ม UI แจ้งเตือนว่ายังมีงานลบ vector ค้างอยู่ (R-4/DI-16) บนรายการเอกสาร
- [x] [frontend] เพิ่มหน้า/แท็บกู้คืนเอกสารที่ถูกลบ พร้อมปุ่มกู้คืนเรียก `POST /api/documents/{id}/restore`

## Phase 4: Extracted-text visibility 🔒 Security gate

**ขึ้นกับ:** Phase 3 (chunk ถูกเขียนโดย worker ของ Phase 3)

- [x] [backend] สร้าง entity `DocumentChunk` ตาม DM-4
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม `DbSet<DocumentChunk>` + index `(DocumentId, SeqNo)` และ `CompanyId` ตาม DM-15
- [x] [backend] สร้าง EF Core migration `AddDocumentChunks` ตาม MG-D1 — สร้าง `DocumentChunk` พร้อม index ตาม DM-15
- [x] [backend] สร้าง `IDocumentChunkRepository` — `GetByDocumentId(documentId)`, `DeleteByDocumentId(documentId)` (soft) — ลงทะเบียนใน `UnitOfWork.Register`
- [x] [backend] แก้ worker (Phase 3) ให้เขียน `DocumentChunk` ตาม DI-8 — soft delete แถวเดิมของ `DocumentId` แล้วเขียนชุดใหม่ทั้งหมดในทรานแซกชันเดียว (ห้าม merge ทีละแถว)
- [x] [backend] implement DI-6 — `HasSuspectCharacters` true เมื่อพบ NUL, C0 control (นอกจาก tab/newline/CR), Unicode PUA (`U+E000`–`U+F8FF`), หรือ `U+FFFD` — เป็นตัวช่วยเรียงลำดับเท่านั้น ห้ามใช้บล็อกการ index หรือกำหนด `failed`
- [x] [backend] `GET /api/documents/{id}/chunks` — คืน `DocumentChunk` เรียงตาม `SeqNo` พร้อม `ChunkKey`/`CharCount`/`HasSuspectCharacters` (DI-7) — ตรวจ role/`CompanyId` ก่อนคืนเนื้อหาดิบ (endpoint แรกในระบบที่ทำแบบนี้ — จุดที่ security gate เพ่งเล็ง)
- [x] [backend] DTO/ViewModel `DocumentChunk`
- [x] [frontend] สร้างหน้าดูข้อความที่แปลงได้ต่อเอกสาร (ลิงก์จากหน้ารายการเอกสาร) — แสดงทุก chunk เรียงตาม `SeqNo`, ไฮไลต์แถวที่ `HasSuspectCharacters = true` ให้เห็นก่อน, แสดงกรณี `extract_failed` เป็น "แปลงไม่ได้" ไม่ใช่หน้าว่างเปล่า (DI-7)

## Phase 5: PDF narration & lesson authoring

**ขึ้นกับ:** Phase 1 (ฟอร์มสร้างบทเรียนต้องมีช่องหมวดตั้งแต่แรก — P9 ผูกกับ R1.1), Phase 3 (NR-6 enqueue job)

- [x] [backend] สร้าง entity `LessonSlideNarration` ตาม DM-5
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม `DbSet<LessonSlideNarration>` + index `(LessonId, SlideObjectId)` และ `CompanyId` ตาม DM-15
- [x] [backend] สร้าง EF Core migration `AddLessonSlideNarrations` ตาม MG-E1 — สร้าง `LessonSlideNarration` พร้อม index ตาม DM-15
- [x] [backend] สร้าง `ILessonSlideNarrationRepository` — `GetByLessonId(lessonId)`, `GetOne(lessonId, slideObjectId)`, `DeleteByLessonId(lessonId)` (soft, คืนจำนวนที่ลบ) — ลงทะเบียนใน `UnitOfWork.Register`
- [x] [backend] implement NR-1 — resolver ลำดับบทพูดต่อหน้า: มีแถว `LessonSlideNarration` → ใช้ `NarrationText` · ไม่มี → ใช้ `PdfSlidesRenderer.BuildContent(...).SpeakerNotes` ของหน้านั้น ใช้ทั้งตอนสอนจริงและตอน index (NR-7)
- [x] [backend] `GET /api/lessons/{id}/narrations` — คืนบทพูดทุกหน้า (resolved ผ่าน NR-1) พร้อม flag `isLikelyScanned` ตาม NR-5 (ทุกหน้า `SpeakerNotes` ว่างหลัง trim)
- [x] [backend] `PUT /api/lessons/{id}/narrations/{slideObjectId}` — implement NR-2: trim แล้วเทียบกับ prefill, เท่ากัน → ลบแถวถ้ามี (ไม่สร้างใหม่), ต่างกัน → upsert, ค่าว่าง → ลบแถว — endpoint ต้อง**ปฏิเสธที่ server** ถ้า `ContentSourceType = google_slides` (NR-9)
- [x] [backend] ต่อจากบันทึก/ลบสำเร็จ — enqueue `BackgroundJob(lesson_index)` ที่ `TargetId = LessonId`, `PayloadJson = {"slideObjectIds":["pdf-page-N"]}` ตาม NR-6 — worker upsert เฉพาะ chunk ที่ระบุ ห้าม re-embed ทั้งเด็ค
- [x] [backend] implement NR-3 — trigger จาก `LessonConfig.PdfDocumentResourceId` **เปลี่ยนค่า** (ไม่ใช่เซฟทั่วไป): endpoint คืนจำนวนแถว `LessonSlideNarration` ที่จะถูกลบให้ยืนยันก่อน แล้ว soft delete ทุกแถวของบทเรียนนั้นในทรานแซกชันเดียวกับการเซฟ `PdfDocumentResourceId`
- [x] [backend] implement NR-7 — แก้ `ILessonConfigService.SaveAsync` ให้ index บทเรียน PDF ด้วย: `ContentSourceType = pdf` → build content จาก `PdfSlidesRenderer` ผ่าน NR-1 แล้ว index ด้วย `KnowledgeNamespaces.For(companyId, slug)`, `metadata.sourceType = "slide"` (KS-6) — เดิมมีแค่เส้นทาง `google_slides`
- [x] [backend] implement NR-8 — ยืนยัน `sourceType` แยกถูกต้องระหว่างบทพูด (`slide`) กับคำตอบเอกสารแนบ (`document`) ของบทเรียน PDF เดียวกัน — ไม่รวมสองตัวแปลงเข้าด้วยกัน (นอก scope ตาม O-4)
- [x] [backend] `POST /api/lessons` — endpoint สร้างบทเรียนใหม่ (P9/Q4 ขั้นต่ำ) รับ `Slug`/`Title`/`Description`/`ContentSourceType`/`CategoryId` (บังคับ, ต้องเป็น Level 2 ตาม TX-4) — ตรวจ TX-7 (`Slug` ห้ามชน `kbcat-`/`kb-global`) เหมือนหน้าแก้ไขเดิม
- [x] [frontend] สร้างหน้าแก้บทพูดต่อหน้า สำหรับบทเรียน `ContentSourceType = pdf` — list หน้า, prefill จาก `GetOne`/resolved narration, textarea แก้ทับ (max 5000 ตัวอักษร), แจ้งเตือนถ้า `isLikelyScanned = true` (NR-5) ว่าต้องพิมพ์เองทุกหน้า — **ซ่อน/ปฏิเสธ path นี้ทั้งหมดถ้า `ContentSourceType = google_slides`** (NR-9)
- [x] [frontend] เพิ่ม flow ยืนยันก่อนอัปโหลด PDF ใหม่ทับของเดิม — เรียก endpoint เช็คจำนวนหน้าที่จะถูกลบ (NR-3), โชว์ข้อความเตือนแล้วให้กดยืนยันก่อนเรียก save
- [x] [frontend] สร้างหน้า `/admin/lessons/new` (P9) — ฟอร์มสร้างบทเรียนใหม่ ใช้ component เดิมจากหน้าแก้ไขซ้ำ (`[slug]/page.tsx` ตาม Q4) เพิ่มช่องเลือกหมวด (บังคับ, dropdown เฉพาะ Level 2 จาก Phase 1) เรียก `POST /api/lessons`

## Phase 6: Q&A knowledge base & review queue 🔒 Security gate

**ขึ้นกับ:** Phase 1 (scope), Phase 2 (`sourceType` และ 3 namespace), Phase 3 (คิว index ของ Q&A)

- [x] [backend] สร้าง entity `KnowledgeQnA` ตาม DM-6
- [x] [backend] สร้าง entity `KnowledgeQnASource` ตาม DM-7 (logical FK ไป `SessionQuestion.Id` ข้าม module — ไม่แก้ entity ของ `learning-session`)
- [x] [backend] สร้าง entity `KnowledgeQnAConflict` ตาม DM-8
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม `DbSet` ทั้งสาม + index ตาม DM-15, และเพิ่ม index ใหม่บน `SessionQuestion` (`(CompanyId, AnswerStatus)`, `(CompanyId, ReviewResult)`) โดย**ไม่แก้ฟิลด์ใดๆ ของ `SessionQuestion`** (R-9) — แจ้ง `learning-session` module ว่ามีการแตะ `OnModelCreating` ของ entity ข้าม module
- [x] [backend] สร้าง EF Core migration `AddKnowledgeQnA` ตาม MG-F1 — สร้าง `KnowledgeQnA`, `KnowledgeQnASource`, `KnowledgeQnAConflict` พร้อม index ตาม DM-15 และเพิ่ม index `(CompanyId, AnswerStatus)`/`(CompanyId, ReviewResult)` บน `SessionQuestion`
- [x] [backend] แก้ `KnowledgeSourceChunk` ตาม DM-14 — เพิ่ม `EmbedText` (nullable, null = ใช้ `Text` เดิมทั้งหมด)
- [x] [backend] แก้ `IKnowledgeIndexingService.IndexChunksAsync` — embed `chunk.EmbedText ?? chunk.Text` (บรรทัดเดียว ที่เหลือคงสัญญาเดิมทั้งหมด)
- [x] [backend] สร้าง `IKnowledgeQnARepository` (`GetByScope(scopeType, scopeId)`, `Search(keyword)`), `IKnowledgeQnASourceRepository` (`GetBySessionQuestionIds(ids)` — เรียกครั้งเดียวต่อหน้า), `IKnowledgeQnAConflictRepository` (`GetUnresolved()`) — ลงทะเบียนใน `UnitOfWork.Register`
- [x] [backend] แก้ `ISessionQuestionRepository` — เพิ่ม `GetReviewQueue(...)` (คิวข้ามการเรียน/ข้ามบทเรียน) implement QQ-1 (`AnswerStatus == NotFound` **หรือ** `ReviewResult == Incorrect` **และ** ยังไม่มี `KnowledgeQnASource` ชี้มา; `OutOfScope`/`NoSpeech`/`TranscriptionFailed` ไม่เข้าคิว) — join `SessionQuestion → LearningSession → TrainingLink.LessonSlug` ตาม QQ-4 ห้าม denormalize `LessonId` ลง `SessionQuestion`
- [x] [backend] สร้าง `IKnowledgeQnAService`+impl — บันทึก Q&A ใหม่: validate KS-2 (scope), เขียน `EmbedText = Question`, `Text = "ถาม: {Question}\nตอบ: {Answer}"`, `Metadata = {sourceType: "qna", qnaId}` ตาม KS-5, บันทึก `CreateBy` จาก `AdminUser.Id` จริง (R5.6), enqueue `BackgroundJob(qna_index)`
- [x] [backend] เชื่อม validation TX-5 สำหรับ `KnowledgeQnA` ที่ `ScopeType = category` ให้ชี้ `KnowledgeCategory.Level == 2` และเปลี่ยน TX-6/TX-10 จาก Q&A count placeholder เป็นค่าจริงใน repository/service round เดียวกับ MG-F1
- [x] [backend] implement QQ-6 — แก้ Q&A: `Question` เปลี่ยน → enqueue `qna_index` ใหม่ (re-embed), แก้เฉพาะ `Answer` → re-upsert โดยข้าม embed call (เพราะ vector เท่าเดิม)
- [x] [backend] implement QQ-5 — ลบ Q&A (soft delete) → soft delete `KnowledgeQnASource` ทุกแถวที่ชี้มาในทรานแซกชันเดียว (คำถามกลับเข้าคิวอัตโนมัติผ่าน QQ-1)
- [x] [backend] implement QQ-7 — บันทึก Q&A ผูกกับหลาย `SessionQuestion` พร้อมกันได้ (สร้างหลายแถว `KnowledgeQnASource` ต่อหนึ่ง `QnAId`)
- [x] [backend] implement QQ-8 — prefill scope เป็น `lesson` ของบทเรียนที่คำถามเกิด แต่ CS ต้องเปลี่ยนได้ก่อนบันทึกเสมอ ไม่ auto-บันทึก
- [x] [backend] implement QQ-9 — สิทธิ์แก้/ลบ Q&A: ทุกคน (`owner`/`admin`/`cs`) ในบริษัทเดียวกันแก้/ลบของกันได้ (default ตาม O-1 จนกว่าจะ amend)
- [x] [backend] แก้ prompt/structured output ของ `IVoiceQuestionProvider` (ทั้ง Gemini และ OpenAI-compatible variant) — implement KS-7 (แยกบล็อกเอกสาร/สไลด์ก่อน บล็อก Q&A ทีหลัง พร้อมคำสั่งยึดบล็อกแรกเมื่อขัดกัน), KS-8 (ห้ามคัดลอก ต้องเรียบเรียงใหม่ ยังตอบ `not_found` ได้แม้หยิบ Q&A มาได้ถ้าไม่ตรงคำถามจริง), KS-9 (เพิ่ม field `conflict: {qnaId, sourceLabel, note} | null` ใน structured output)
- [x] [backend] implement KS-9/KS-10 — เมื่อ `conflict != null` และ `qnaId` ตรวจแล้วว่ามีจริงในบริษัทนี้ → บันทึก `KnowledgeQnAConflict` หนึ่งแถว (การบันทึกล้มเหลวห้ามทำให้ตอบคำถามล้มเหลว — log warning แล้วเดินต่อ)
- [x] [backend] `GET /api/qna-queue` — คิวรวมข้ามการเรียน/บทเรียน (QQ-1, QQ-4) พร้อมป้ายแยกแหล่งที่มา `not_found`/`incorrect` (QQ-3, คำถามเดียวเป็นได้ทั้งสองป้าย)
- [x] [backend] `POST /api/knowledge-qna` — บันทึก Q&A ใหม่ ผูกกับคำถาม 1 ใบขึ้นไปจากคิว (QQ-7)
- [x] [backend] `PUT /api/knowledge-qna/{id}` — แก้ Q&A (QQ-6)
- [x] [backend] `DELETE /api/knowledge-qna/{id}` — ลบ Q&A (QQ-5)
- [x] [backend] `GET /api/knowledge-qna-conflicts?resolved=false` — รายการธงขัดแย้งที่ยังไม่ปิด (QQ-10)
- [x] [backend] `PUT /api/knowledge-qna-conflicts/{id}/resolve` — CS กดปิดธง (`ResolvedAt`/`ResolvedBy`) ตาม QQ-10
- [x] [backend] DTO/ViewModel: `KnowledgeQnA` (list/detail), คิว review (รวม transcript ข้ามการเรียน/บทเรียน + ป้ายแหล่งที่มา), `KnowledgeQnAConflict`
- [x] [backend] unit test: QQ-1 (นิยามคิว — ครบกรณี `NotFound`/`Incorrect`/ทั้งสอง/`OutOfScope` ไม่เข้าคิว/มี `KnowledgeQnASource` แล้วไม่เข้าคิว)
- [x] [frontend] เพิ่ม type `KnowledgeQnA`, `KnowledgeQnAConflict`, ViewModel คิวรวม ใน `src/types/domain.ts`
- [x] [frontend] เพิ่มเมธอดเรียก endpoint คิว/Q&A/conflict ทั้งหมดข้างต้นใน `src/lib/api-client.ts`
- [x] [frontend] สร้างหน้าคิวรวม (P8/R5.1) — ตาราง/list ข้ามการเรียนและบทเรียน, แสดงป้ายแหล่งที่มา (QQ-3), แสดงชื่อบทเรียนต้นทางต่อแถว (QQ-4)
- [x] [frontend] สร้างหน้าเขียนคำตอบ — prefill คำถามจาก transcript, เลือก scope (บทเรียน/หมวด/บริษัท) prefill เป็น `lesson` แต่แก้ได้ก่อนบันทึกเสมอ (QQ-8), เลือกคำถามอื่นในคิวที่ปิดด้วยคำตอบเดียวกันได้ (QQ-7)
- [x] [frontend] สร้างหน้ารายการธงขัดแย้ง (QQ-10) — แยกเป็นหน้า/แท็บของตัวเอง ไม่ใช่ badge บนคิว, ปุ่มกดปิดธง

## Phase 7: Document scope assignment — R3 write path 🔒 Security gate

**ขึ้นกับ:** Phase 1 (`KnowledgeCategory`, `ScopeType`/`ScopeId`), Phase 2 (`EnsureValidScope`/`Resolve`
namespace resolver), Phase 3 (`BackgroundJob`/`vector_delete`/`document_index` worker), Phase 4
(`DocumentChunk`/`VectorId`/`NamespaceKey`) — ทั้งสี่มีโค้ดอยู่แล้ว ไม่มี task ใหม่ของ phase เหล่านั้นในนี้

**ไม่มี migration ในเฟสนี้** — `DocumentResource.ScopeType`/`ScopeId` สร้างไปแล้วใน MG-A2/MG-A5
(design.md ระบุชัดว่าถ้ากำลังจะ generate migration ในเฟสนี้แปลว่าทำเกิน contract ให้ตีกลับ
`system-analyst`) ฝั่ง DB เป็น additive ล้วน ไม่มีฟิลด์ใหม่ ไม่มีข้อมูลเดิมถูกแตะ — สิ่งที่ breaking
คือ wire contract ของ `/api/documents` เท่านั้น (`lessonSlug` → `scopeType`/`scopeId`)

- [x] [backend] แก้ `UploadDocumentDto` — ลบฟิลด์ `LessonSlug` ทิ้งทั้งฟิลด์ เพิ่ม `required string ScopeType` + `string? ScopeId` (รูปเดียวกับ `KnowledgeQnADto`) ตาม DS-1
- [x] [backend] แก้ `UploadDocumentRequest`/multipart form binding ใน `DocumentsController` — รับ field `scopeType`/`scopeId` แทน `lessonSlug` ตาม DS-1 (`ScopeId` เมื่อ `ScopeType = lesson` คือ `LessonConfig.Id` ไม่ใช่ `Slug`)
- [x] [backend] แก้ `IDocumentResourceService.UploadAsync` — เรียก `namespaceResolver.EnsureValidScope(CurrentCompanyId, input.ScopeType, input.ScopeId)` เป็นบรรทัดแรกสุดของเมธอด ก่อนเรียก `storageProvider.UploadAsync` เสมอ ตาม DS-2 (ห้ามเรียกหลังอัปโหลดไฟล์)
- [x] [backend] ตรวจ/แก้ mapping exception → HTTP status ของ `DocumentsController` upload endpoint ให้ครบ 6 กรณีปฏิเสธของ DS-3 ทั้งหมดผ่าน `EnsureValidScope` เดิม (ห้ามเขียน validation ชุดที่สองซ้ำ): `ScopeType` ว่าง/ไม่รู้จัก → 400 · `lesson` ที่ `ScopeId` ไม่มีในบริษัทนี้ → 404 · `category` ที่ชี้ `Level == 1` → 400 · `category` ที่ `ScopeId` ไม่มีจริง → 404 · `company` ที่มี `ScopeId` → 400 · `lesson`/`category` ที่ไม่มี `ScopeId` → 400
- [x] [backend] แก้ `GET /api/documents` — query param เปลี่ยนจาก `lessonSlug` เป็น `scopeType`/`scopeId` ตาม DS-4 (ไม่ส่ง query เลย = `company`) ยุบ `IDocumentResourceService.GetByLessonSlug`/`GetStandalone` เหลือเมธอดเดียวที่รับ scope (repository `GetByScope` มีอยู่แล้ว ไม่ต้องแก้)
- [x] [backend] เพิ่ม `IDocumentResourceService.MoveScopeAsync(id, scopeType, scopeId)` — เรียก `EnsureValidScope` ชุดเดียวกับ DS-3 ก่อนเสมอ (call site แรกของ KS-4) ตาม DS-5
- [x] [backend] implement การย้าย scope ใน `MoveScopeAsync` ตาม DS-6 ในทรานแซกชันเดียว: อ่าน `DocumentChunk` ของเอกสาร group ตาม `NamespaceKey` → สร้าง `BackgroundJob(vector_delete)` หนึ่งงานต่อกลุ่ม `PayloadJson = VectorDeleteJobPayload{NamespaceKey, VectorIds}` (รูปเดียวกับ DI-13) → soft delete แถว `DocumentChunk` ทั้งชุด → เขียน `ScopeType`/`ScopeId` ใหม่ + `IndexingStatus = pending` + `IndexedChunkCount = 0` + `FailureReason = null` + `UpdateBy`/`UpdateDate` → enqueue `BackgroundJob(document_index)` — ห้ามเพิ่ม `BackgroundJobType` ใหม่ ห้ามแก้ worker
- [x] [backend] implement เคสขอบของการย้ายตาม DS-7: ย้ายไป scope เดิมเป๊ะ → ไม่ทำอะไร ไม่ enqueue job ใดๆ คืน 200 · ไม่เคย index สำเร็จ (ไม่มี `DocumentChunk`) → ไม่สร้าง `vector_delete` แต่ยัง enqueue `document_index` · เอกสารที่ถูก soft delete แล้ว → 404 ห้ามย้ายของในถังกู้คืน · เอกสารที่เป็น `LessonConfig.PdfDocumentResourceId` ของบทเรียนอยู่ → ย้ายได้ไม่บล็อก (ต่างจาก DI-14) · มีงาน `document_index` ค้างอยู่ตอนย้าย → ไม่ต้องยกเลิกงานเดิม · สิทธิ์เท่ากับ upload/delete เดิม (`owner`/`admin`/`cs` ของบริษัทนั้น)
- [x] [backend] `PATCH /api/documents/{id}/scope` — body `{ scopeType, scopeId }` เรียก `MoveScopeAsync` ตาม DS-5
- [x] [backend] DTO/ViewModel: `MoveDocumentScopeDto` (`ScopeType`, `ScopeId`) — ไม่ต้องเพิ่มฟิลด์ใหม่บน `DocumentResourceViewModel` (`ScopeType`/`ScopeId` มีอยู่แล้ว ตาม DS-9)
- [x] [backend] unit test (R-12/DS-12) — upload ที่ `scopeType = category` ผ่าน `EnsureValidScope` จริง (ไม่ mock ข้าม) ครบทั้ง 6 กรณีปฏิเสธของ DS-3
- [x] [backend] unit test (R-12/DS-12) — การย้าย scope (DS-6) สร้าง `vector_delete` payload ที่ `NamespaceKey`/`VectorIds` ถูกชุด และตั้ง `IndexingStatus = pending` จริง
- [x] [backend] unit test (R-12/DS-12) — เคส "ย้ายไป scope เดิม = ไม่ enqueue อะไรเลย" และเคส "ไม่มี `DocumentChunk` = ไม่มี `vector_delete`" ตาม DS-7
- [x] [backend] อัปเดต `frontend/docs/API_CONTRACT.md` ให้ตรง wire contract ใหม่ของ `POST /api/documents` (multipart `scopeType`/`scopeId`), `GET /api/documents` (query `scopeType`/`scopeId`), และ `PATCH /api/documents/{id}/scope` ใหม่
- [x] [frontend] แก้ `src/types/domain.ts` — เปลี่ยน type ของ upload request/document list query จาก `lessonSlug` เป็น `scopeType`/`scopeId` ให้ตรง DTO ใหม่ เพิ่ม type สำหรับ `PATCH .../scope` body
- [x] [frontend] แก้ `src/lib/api-client.ts` — เมธอด upload document ส่ง `scopeType`/`scopeId` แทน `lessonSlug`, เมธอด list documents รับ `scopeType`/`scopeId` เป็น query param, เพิ่มเมธอดใหม่เรียก `PATCH /api/documents/{id}/scope`
- [x] [frontend] แก้ `DocumentUploadList.tsx` (ใช้ในหน้าบทเรียน) — ส่ง `scopeType = "lesson"` + `scopeId = LessonConfig.Id` ของบทเรียนนั้นเสมอ ไม่มี scope picker (บริบทหน้ากำหนดอยู่แล้วตามมติ Q-C) ตาม DS-8
- [x] [frontend] แก้ `app/admin/lessons/[slug]/page.tsx` และ `app/admin/lessons/new/page.tsx` — ปรับจุดเรียก `DocumentUploadList`/upload ให้ส่ง `scopeType`/`scopeId` ใหม่แทน `lessonSlug`
- [x] [frontend] แก้ `app/admin/documents/page.tsx` — เพิ่ม scope picker ตอนอัปโหลด: `RadioGroup` "ทั้งบริษัท / เฉพาะหมวด" + `Select` หมวดที่ `level === 2` แสดง "หมวดแม่ › หมวดย่อย" เมื่อเลือก "เฉพาะหมวด" — ลอกรูปจาก `KnowledgeQnAAnswerDialog.tsx` ตรงๆ ใช้ `listKnowledgeCategories()` เดิม ห้ามสร้าง pattern ที่สอง ตาม DS-8
- [x] [frontend] แก้ `app/admin/documents/page.tsx` — เพิ่มคอลัมน์ "ขอบเขต" ต่อแถว (แสดง "ทั้งบริษัท" หรือชื่อหมวด) และตัวกรองตาม scope ตาม DS-9
- [x] [frontend] แก้หัวข้อหน้า `/admin/documents` จาก "คลังเอกสาร (ใช้ได้ทุกบทเรียน)" เป็นข้อความที่ไม่ผิดเมื่อมีเอกสารระดับหมวดปนอยู่ ตาม DS-9
- [x] [frontend] เพิ่ม UI ย้าย scope เอกสารที่อัปไปแล้ว (ปุ่ม/dialog เลือก scope ใหม่ต่อแถวในหน้าคลัง) เรียก `PATCH .../scope` ที่เพิ่มใน `api-client.ts` ตาม DS-5

## Phase 8: Knowledge library view (R7) — extends `/admin/documents` 🔒 Security gate

**ขึ้นกับ:** Phase 1 (Module A — scope), Phase 4 (Module D — `DocumentChunk` ที่ KL-11 ค้น),
Phase 6 (Module F — `KnowledgeQnA` ที่ KL-8 อ่าน), Phase 7 (Module G — `ScopeType`/`ScopeId`
ที่ CS ตั้งเองได้จริง + หน้าคลังที่ต่อยอด) — ทั้งสี่มีโค้ดอยู่แล้ว **นี่คือการต่อยอด
`/admin/documents` ที่มีอยู่แล้ว ไม่ใช่หน้าใหม่** — ห้ามแก้ KS-1..KS-11/namespace/prompt/
`RagVoiceQuestionProvider` และห้ามเขียน `DocumentUploadList.tsx` ใหม่ทับของเดิม (scope picker/
filter/คอลัมน์ขอบเขต/ปุ่มย้าย/ปุ่มลบ มีอยู่แล้วครบ ณ 2026-08-25)

**MG-H1 ต้องอยู่เฟสนี้เฟสเดียวกับ KL-18..KL-24 ห้ามแยก** — เพิ่ม 2026-08-25 จากมติ Q-H1 = ทาง B

- [x] [backend] เพิ่ม `DocumentResource.ContentHash` (`string?`, `init`-only) ตาม DM-3 — comment อธิบาย 2 กรณีที่เป็น `null`: แถวที่อัปก่อน `MG-H1` (จงใจไม่ backfill) และไม่มีกรณีอื่นอีก
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม index `(CompanyId, ContentHash)` แบบ**ไม่ unique**ตาม DM-15
- [x] [backend] สร้าง EF Core migration `AddDocumentContentHash` (**MG-H1**) — เพิ่มคอลัมน์ `ContentHash` แบบ **nullable ถาวร** (ไม่มีขั้นตอนตามมาที่ทำให้ `NOT NULL`) + index ตามข้างบน — **additive ล้วน ห้าม backfill ห้าม UPDATE ห้ามแตะข้อมูลเดิมแม้แถวเดียว ห้ามแตะ Pinecone**; `Down()` = drop index + drop column (ย้อนกลับได้สมบูรณ์ 100%)
- [x] [backend] แก้ `IDocumentResourceService.UploadAsync` — คำนวณ SHA-256 ของ byte ทั้งไฟล์ (hex ตัวพิมพ์เล็ก 64 ตัว ไม่มี prefix/ขีดคั่น) จาก `input.Content` ก้อนเดียวกับที่ส่งเข้า storage **ก่อน**เขียนแถวลง DB เสมอทุกทางอัปโหลด รวม `handlePdfUpload` (UC-5) — คำนวณครั้งเดียวตลอดอายุแถว ห้าม recompute ตอนย้าย scope/soft delete/กู้คืน/re-index; ห้ามรับ hash จาก client
- [x] [backend] แก้ `IDocumentResourceRepository.GetAllInCompany()` (KL-2/KL-9 pattern) — `FindBy(_ => true)` พึ่ง EF query filter (`CompanyId` + `!IsDelete`) ล้วนๆ **ห้ามใช้ `IgnoreQueryFilters()` เด็ดขาด**
- [x] [backend] แก้ `DocumentResourceService.GetByScope(null, null)` — เรียก `GetAllInCompany()` แทนการแปลงค่าว่างเป็น `company` เดิม (DS-4 เก่า); `scopeType` ที่ส่งมาแล้วพฤติกรรมเดิมทุกอย่างคงเดิม
- [x] [backend] เพิ่ม `IKnowledgeQnARepository.GetAllInCompany()` — รูปเดียวกับข้างบนทุกประการ (`FindBy(_ => true)`, ห้าม `IgnoreQueryFilters()`)
- [x] [backend] เพิ่ม `IKnowledgeQnAService.GetAll(KnowledgeQnAFilter filter)` — รับ query เดียวกับเอกสาร (`scopeType`/`scopeId`/`status`/`q`) ตีความเหมือนกันทุกข้อ (KL-2..KL-5, KL-11..KL-13); ใช้ `KnowledgeQnAViewModel` เดิม **ห้ามเพิ่มฟิลด์**
- [x] [backend] `GET /api/knowledge-qna` ใหม่ทั้งเส้น (controller) — เรียก `guard.EnsureAuthenticated()` ตรงๆ ใน service (เหตุผลเดียวกับ DI-7) ไม่พึ่ง query filter อย่างเดียว; สิทธิ์ = `owner`/`admin`/`cs` ของบริษัทนั้น เรียง `CreateDate` ลง
- [x] [backend] implement KL-5 — filter ตามหมวด X คืนทั้ง (ก) แถว `ScopeType=category && ScopeId=X` และ (ข) แถว `ScopeType=lesson` ที่ `LessonConfig.CategoryId=X` ทั้งฝั่งเอกสารและ Q&A
- [x] [backend] implement KL-11 — ค้นในเนื้อหาด้วย `EF.Functions.ILike` (parameterized เสมอ **ห้ามต่อ SQL เป็นสตริงเข้า `FromSqlRaw`**): เอกสารติดเมื่อ `FileName` ตรงหรือมี `DocumentChunk.Text` แถวใดตรง (`DISTINCT` ระดับเอกสาร); Q&A ติดเมื่อ `Question` หรือ `Answer` ตรง
- [x] [backend] implement KL-12 — `q` trim แล้วว่าง = ไม่ค้น (คืนตาม filter อื่น), ต่ำกว่า 2 ตัวอักษร = ไม่ค้น (ไม่ error), ค้น+filter ทำงานแบบ AND เสมอ, ไม่มี pagination
- [x] [backend] implement KL-19 — นิยาม "เนื้อหาซ้ำ": `CompanyId` เดียวกัน และ `ContentHash` เท่ากันเป๊ะ; `ContentHash = null` ไม่เคยซ้ำกับอะไรเลยแม้กับ `null` ด้วยกันเอง (เขียนเงื่อนไข `hash != null &&` ในโค้ดตรงๆ ห้ามพึ่ง `NULL = NULL` ของ SQL); ไม่นับแถว soft-deleted; **ห้ามเทียบข้ามบริษัทเด็ดขาด**; scope ไม่เกี่ยว — ไฟล์เดียวกันคนละ scope ก็นับซ้ำ; **แก้ถ้อยคำ 2026-08-25 (design.md KL-19)**: เทียบ hash **ในหน่วยความจำ** บนลิสต์ก้อนเดียวกับ KL-20 ไม่ใช่เป็นเงื่อนไข `Where` ที่ระดับคิวรี (KL-20 ต้อง materialize อยู่แล้วโดยเลี่ยงไม่ได้ การเทียบ hash บนลิสต์นั้นจึงฟรี ส่วนการแยกเป็นอีกคิวรีคือการเพิ่ม round-trip เปล่า ๆ) — **index `(CompanyId, ContentHash)` ยังต้องมีตาม DM-15/MG-H1 ห้าม drop** (ถูกต้องทันทีที่ทะลุเส้น ~500 ของ O-11); `CompanyId` ต้องอยู่ใน predicate ตรง ๆ ไม่ฝากไว้กับ query filter อย่างเดียว
- [x] [backend] implement KL-20 — นิยาม "ชื่อซ้ำ" แยกจาก KL-19: `CompanyId` เดียวกัน และ `FileName` เท่ากันหลัง trim + เทียบไม่สนตัวพิมพ์ใหญ่เล็ก; **แก้ถ้อยคำ 2026-08-25 (design.md KL-20)**: ใช้ `string.Equals(..., OrdinalIgnoreCase)` **ในหน่วยความจำ ห้ามใช้ `EF.Functions.ILike`** — `ILike` ตีความ `_`/`%` ที่อยู่ในชื่อไฟล์เป็น wildcard (`report_2026.pdf` จะแมตช์ `report-2026.pdf` เป็น false positive) ขัดกับคำสั่ง "เทียบเต็มสตริง" ของข้อนี้เอง และ throw ใน fake repository ของ `Application.Tests` ทำให้ test สี่ตัวข้างล่างเขียนไม่ได้ · **KL-11 ไม่เกี่ยว ยังเป็น `ILike` จริงตามเดิม**; ไม่นับแถว soft-deleted; ต้องรายงานแยก 4 แบบ (ชื่อ+เนื้อหาซ้ำ / เนื้อหาซ้ำชื่อต่าง / ชื่อซ้ำเนื้อหาต่าง / ไม่ซ้ำ) — ห้ามยุบเป็นข้อความเดียว
- [x] [backend] แก้ `UploadDocumentDto` — เพิ่ม `bool CheckDuplicate` ค่าเริ่มต้น `false` (additive, ไม่ breaking)
- [x] [backend] implement KL-21 — เมื่อ `CheckDuplicate = true`: ตรวจ KL-19/KL-20 **ก่อน**เขียนแถว/storage/เข้าคิว index; เจอซ้ำ → **ไม่เขียนอะไรทั้งสิ้น** คืน `409 Conflict` · **แก้ถ้อยคำ 2026-08-25 (design.md KL-21)**: payload ไม่ใช่ body เปล่า แต่ขี่มาใน `ApiErrorResponse` envelope เดิม — `GeneralException.Conflict(messageTh, details)` → `{ error: { code: "CONFLICT", message, details } }` โดย `details` = `DuplicateDocumentDto` = `{ duplicateByHash: [...], duplicateByFileName: [...] }`; ต้องเพิ่ม `"CONFLICT"` ใน `ApiErrorCode` **ทั้งสองฝั่ง** (`SupportRoom.Domain/Enums/ApiErrorCode.cs` ↔ `frontend/src/types/api.ts`); แต่ละรายการมีแค่ `id`/`fileName`/`scopeType`/`scopeId`/**`createdAt`** (**ไม่ใช่ `createDate`** — ชื่อ wire ตรงกับ `DocumentResourceViewModel.CreatedAt`); `CheckDuplicate = false` (ค่าเริ่มต้น) = พฤติกรรมเดิมทุกตัวอักษร แต่ยังคำนวณ/เก็บ `ContentHash` เสมอ
- [x] [backend] implement KL-23 — **ด่านก่อนบันทึก ไม่ใช่คำเตือนหลังบันทึก (มติ Q-H2 = ทาง (ข), เขียนใหม่ทั้งข้อ 2026-08-25 แทนถ้อยคำ "คืนคำเตือน ไม่บล็อก" เดิม)**: สร้าง `KnowledgeQnA` ใหม่ — ตรวจก่อนเขียนอะไรทั้งสิ้น (ก่อน `_repository.Add`, ก่อนเขียนแถว `KnowledgeQnASource` แม้แถวเดียว, ก่อนปิดคำถามในคิว, ก่อน `EnqueueJob(qna_index)`, ก่อน `UnitOfWork.Commit()`) ว่า `Question` ซ้ำเป๊ะหลัง trim + ยุบช่องว่างติดกันเหลือช่องเดียว + case-insensitive **ในหน่วยความจำ** (`CollapseWhitespace` + `OrdinalIgnoreCase`, **ห้ามย้ายไป SQL** — ไม่มี LINQ translation ปลอดภัยสำหรับ "ยุบช่องว่าง") กับ Q&A ที่ยังไม่ถูกลบของบริษัทเดียวกัน — เทียบเฉพาะ `Question` ไม่เทียบ `Answer`, ไม่เทียบข้ามบริษัท, ไม่สน scope, `CompanyId` อยู่ใน predicate ตรง ๆ ไม่ฝากไว้กับ query filter อย่างเดียว (คนละกลไกกับ KL-19/20 ทั้งหมด ไม่ใช้ `ContentHash`) · **ลำดับตรวจตายตัว**: `EnsureValidScope` → `SessionQuestionIds` ว่าง/หาไม่เจอ → ตรวจซ้ำ (400/404 ชนะ 409 เสมอ) · **ไม่มีธง `CheckDuplicate` — ตรวจ unconditional ทุกครั้งที่ create** (call site เดียวในระบบคือ `app/admin/qna-queue/page.tsx:130` โหมด `create`; ไม่มี caller ที่ต้องเงียบแบบ `handlePdfUpload` ของ KL-21 ห้ามลอกแบบ opt-in มา) · เพิ่ม `CreateKnowledgeQnADto.ConfirmDuplicate` (`bool`, ค่าเริ่มต้น `false`, wire `confirmDuplicate`) — `true` = ข้ามการตรวจ บันทึกปกติแม้มีของซ้ำจริง (ไม่ error) · เจอซ้ำ → `GeneralException.Conflict(messageTh, details)` → `{ error: { code: "CONFLICT", message, details } }` (ใช้ `ApiErrorCode.Conflict` ที่มีอยู่แล้วทั้งสองฝั่ง ไม่ใช่ค่าใหม่) โดย `details` = **`DuplicateQnAResponse` ใหม่** = `{ duplicateByQuestion: KnowledgeQnAViewModel[] }` — **ลิสต์ ไม่ใช่ใบเดียว**, เรียง `CreateDate` ลง, ใช้ `KnowledgeQnAViewModel` เดิมทั้งใบ (ห้ามเพิ่มฟิลด์ ห้ามยัดลง `DuplicateDocumentDto`/`DuplicateDocumentsResponse` ของ KL-21 — คนละ endpoint คนละ shape) · **breaking change มี caller เดียว**: ลบ `KnowledgeQnACreateResultViewModel` ทั้งคลาสพร้อมฟิลด์ `DuplicateWarning`; `POST /api/knowledge-qna` คืน **`Ok(new { qna = ... })` รูปเดียวกับ `PUT`** (แก้บั๊ก casing แฝงของโค้ดวันนี้ที่คืน ViewModel ทั้งใบตรงๆ แล้ว serialize เป็น `qnA` ไม่ใช่ `qna` ไปในตัว) · **`UpdateAsync`/`PUT` ไม่ตรวจซ้ำ ไม่มี `ConfirmDuplicate` — ห้ามเติมเอง "เพื่อความสม่ำเสมอ"** (QQ-6 คุมทางแก้อยู่แล้ว ถ้าเห็นว่าควรมี ให้ตีกลับ `system-analyst`)
- [x] [backend] DTO/ViewModel: `DuplicateDocumentDto` (payload ของ 409 ตาม KL-21), `KnowledgeQnAFilter`, ยืนยันว่า `ContentHash` **ไม่ออก API** (ไม่เพิ่มลง `DocumentResourceViewModel` หรือ response ใดเลย)
- [x] [backend] unit test (R-12 pattern) — KL-19: สองบริษัทมีไฟล์ hash เท่ากันเป๊ะ ต้อง**ไม่**เตือนข้ามกัน (ต้องมี test สองบริษัทจริง ไม่ใช่ test บริษัทเดียว)
- [x] [backend] unit test — KL-20: ครบ 4 กรณี (ชื่อ+เนื้อหาซ้ำ / เนื้อหาซ้ำชื่อต่าง / ชื่อซ้ำเนื้อหาต่าง / ไม่ซ้ำ)
- [x] [backend] unit test — KL-19 null-handling: `ContentHash = null` สองแถวไม่ถูกจับว่าซ้ำกัน
- [x] [backend] unit test — KL-23: **แก้ถ้อยคำทั้งข้อ 2026-08-25 (มติ Q-H2)** จาก "test คำเตือน (`DuplicateWarning`)" เดิมเป็น **test ด่าน 409**: เขียนใหม่ 3 ตัวเดิมที่ `KnowledgeQnAServiceTests.cs:222/242/259` ให้ยืนยัน `GeneralException.Conflict`/409 พร้อม `DuplicateQnAResponse` แทนการยืนยัน `DuplicateWarning` (ตัวที่ `:259` คือ test สองบริษัทที่พิสูจน์ว่าไม่เตือนข้ามกัน — **ต้องคงไว้ ห้ามลบ** ปรับแค่ assertion ให้ตรง shape ใหม่) ยังต้องครอบ trim/ยุบช่องว่าง/case-insensitive เหมือนเดิม · เพิ่มอีก 2 ตัวใหม่: (1) เมื่อโยน 409 แล้ว **ไม่มี**แถว `KnowledgeQnA`/`KnowledgeQnASource` ถูกเขียนแม้แถวเดียวและ**ไม่มี** job เข้าคิว (`EnqueueJob(qna_index)` ไม่ถูกเรียก) — proof ว่าตรวจก่อน `Commit()` จริง (2) `ConfirmDuplicate = true` บันทึกผ่านสำเร็จแม้มี `Question` ซ้ำอยู่แล้ว (ข้าม 409 ได้จริง ไม่ error)
- [x] [frontend] แก้ `app/admin/documents/page.tsx` — เพิ่มตัวกรอง `scopeType`/`scopeId` เริ่มต้นจาก URL query param (KL-2 ค่าเริ่มต้น "ทั้งหมด" เมื่อไม่มี/ไม่รู้จัก query)
- [x] [frontend] แก้ layout หน้า `/admin/documents` ตาม KL-1 — แถบ filter+ค้นหาชุดเดียวคุมทั้งหน้า ด้านล่างสองตารางแยกกัน ("เอกสาร" และ "คำถาม-คำตอบ (Q&A)")
- [x] [frontend] เพิ่มตัวเลือก filter ที่ 4 ("เฉพาะบทเรียน" → `Select` บทเรียนด้วย `listLessons()` ที่มีอยู่แล้ว) ต่อยอดจาก 3 ตัวเดิมใน `DocumentUploadList.tsx:255-280` (KL-4) — ตัวเลือกชุดเดียวกันใช้กับทั้งสองตาราง
- [x] [frontend] แก้ `scopeLabel()` (`DocumentUploadList.tsx:119-129`) ตาม KL-6 — `lesson` แสดงชื่อบทเรียนจริง (map จาก `listLessons()`) แทนคำว่า "บทเรียนนี้"; id ที่หาไม่เจอแสดง "บทเรียนที่ถูกลบไปแล้ว"/"หมวดที่ถูกลบไปแล้ว" ห้ามแสดง id ดิบ ห้ามซ่อนแถว
- [x] [frontend] เพิ่ม badge "ใช้เป็นสไลด์ของบทเรียน &lt;ชื่อ&gt;" ตาม KL-7 — เอกสารที่ `Id` ตรงกับ `LessonConfig.PdfDocumentResourceId` ของบทเรียนใดก็ตาม (ย้ายจาก badge เดิมที่ผูกกับโหมด `fixedScope`) ใช้ข้อมูลจาก `listLessons()` ที่โหลดอยู่แล้ว ไม่ต้องมี endpoint ใหม่
- [x] [frontend] เพิ่มช่องค้นหาเนื้อหาตาม KL-11..KL-13 — ต่อกับ `GET /api/documents`/`GET /api/knowledge-qna` query `q`; แสดงข้อความกำกับว่าต้องพิมพ์อย่างน้อย 2 ตัวอักษร และข้อความว่าการค้นเนื้อหาไม่ครอบเอกสารที่ index ไม่สำเร็จ (ค้นได้จากชื่อไฟล์เท่านั้น)
- [x] [frontend] เพิ่มตาราง Q&A ในหน้า `/admin/documents` (KL-1/KL-8) — ดึงจาก `GET /api/knowledge-qna` ใหม่, คอลัมน์ `Question`/`Answer`/ขอบเขต/`IndexingStatus`, ปุ่ม "แก้ไข" และ "ลบ" ต่อแถว
- [x] [frontend] ปุ่ม "แก้ไข" ต่อแถว Q&A (KL-14) — เปิด `KnowledgeQnAAnswerDialog.tsx` ที่มีอยู่แล้วเป็นฐาน (ห้ามสร้าง pattern ที่สอง), เรียก `updateKnowledgeQnA(id, ...)` ที่มีอยู่แล้ว
- [x] [frontend] ปุ่ม "ลบ" ต่อแถว Q&A (KL-15/16) — dialog ยืนยันที่บอกผลข้างเคียงครบ: "คำถามที่ Q&A นี้เคยปิดไว้จะกลับเข้าคิวรีวิวอีกครั้ง" + "ข้อมูลจะถูกลบออกจากคลังความรู้ AI จะเลิกใช้ตอบ" (ห้ามใช้ `window.confirm()` เปล่า); ลบสำเร็จ → ข้อความยืนยันพร้อมลิงก์ไป `/admin/qna-queue`
- [x] [frontend] UI ด่าน Q&A ซ้ำตาม **KL-26** — **เขียนใหม่ทั้งข้อ 2026-08-25 (มติ Q-H2 = ทาง (ข), เคาะแล้ว)** แทนถ้อยคำเดิม "แสดงคำตอบเดิมพร้อมตัวเลือก 'บันทึกเพิ่มอยู่ดี'/'ไปแก้ใบเดิมแทน' หลัง backend คืนคำเตือน" ซึ่งเป็นแบบหลังบันทึกที่ตกไปแล้ว — ของเดิมทำที่ `KnowledgeQnAAnswerDialog` โหมด `create` ที่ `/admin/qna-queue` **ที่เดียว** (ทางสร้างเดียวตาม KL-25 หน้าคลัง `/admin/documents` ไม่มีทางสร้าง Q&A ห้ามเพิ่มปุ่ม "เพิ่ม Q&A" ที่นั่น) ยังถูกอยู่ สิ่งที่เปลี่ยนคือ**กลไกรับ**: **รับ 409 ก่อนบันทึก** — `catch (err instanceof ApiClientError && err.status === 409)` แล้วอ่าน `err.response.error.details` เป็น `DuplicateQnAResponse` (**pattern เดียวกับ `DocumentUploadList.tsx:218-224` คำต่อคำ ห้ามสร้าง pattern ที่สอง**) เก็บ state เป็น**ลิสต์** (ไม่ใช่ใบเดียว) พร้อม input ที่ CS กรอกไว้ทั้งชุดไว้ส่งซ้ำได้ · ต่อรายการในลิสต์แสดง: คำถามที่ซ้ำ + **คำตอบเดิมเต็มๆ** + ป้ายขอบเขต (ใช้ `scopeLabel()` ชุดเดียวกับ KL-6 ห้าม id ดิบ) + วันที่บันทึก + ข้อความชัดเจนว่า **"ยังไม่ได้บันทึก"** (ห้ามให้ UI พูดสิ่งที่ระบบไม่ได้ทำ) · **สามปุ่ม**: **"ยืนยันบันทึกซ้ำ"** = ส่งคำขอเดิมซ้ำด้วย `confirmDuplicate: true` (mirror ของปุ่ม "อัปโหลดต่อไป" ที่ KL-22 ใช้ `checkDuplicate: false` — ใช้ได้เสมอ ไม่มีทางไหนที่ระบบปฏิเสธถาวร) · **"แก้ใบเดิมแทน" ต่อรายการ** (หนึ่งปุ่มต่อหนึ่งแถวเพราะซ้ำได้หลายใบ) — สลับ dialog เดิมเป็น `mode: "edit"` บนแถวนั้น**ในที่เดิม** โดยใช้ `KnowledgeQnAViewModel` ที่มากับ payload 409 ตรงๆ (**ไม่ fetch เพิ่ม ไม่มี endpoint ใหม่ ไม่ออกจากหน้า**) บันทึกด้วย `updateKnowledgeQnA` เดิมตาม KL-14 · **"ยกเลิก"** = ปิด dialog ไม่บันทึกอะไร คำถามในคิวยังอยู่ครบ · **⛔ ห้ามลิงก์ไป `/admin/documents?q=<คำถาม>` เด็ดขาด** — ทางเลือกนี้ตกไปพร้อมมติ Q-H2 แล้ว (เหตุผล: เป็นการค้นหาไม่ใช่ชี้ไปที่แถว, ออกจากหน้าจะทิ้งคำตอบที่พิมพ์ค้าง+การเลือกแถวคิว, ไม่มี requirement ขอให้หน้าคลังอ่าน `q` param) · **ต้องเขียนบอกที่หน้าจอ**: กด "แก้ใบเดิมแทน" แล้วบันทึกสำเร็จ **คำถามในคิวที่ CS เลือกไว้ยังไม่ถูกปิดและยังอยู่ในคิว** (QQ-6 การแก้ไม่แตะคิว — เป็นช่องว่างที่รับรู้แล้ว บันทึกเป็น `O-13`/R7.7 ในอนาคต **ไม่ใช่งานที่ทำตอนนี้ ห้ามผูกคำถามในคิวเข้ากับ Q&A ที่มีอยู่แล้วเป็นผลข้างเคียงของการแก้**) · **ลบ prop `onEditExisting` ออกจาก `KnowledgeQnAAnswerDialog.tsx:27-30` ทั้งหมด** (ไม่เคยถูกส่งจาก call site เดียวที่มีและไม่มีวันถูกส่งตาม KL-25 — ปุ่มตายของ Q-H2 ที่ต้องหายไปจริง ไม่ใช่ย้ายที่) · phasing ไม่เปลี่ยน แก้เฉพาะถ้อยคำ ไม่แตะ checkbox
- [x] [frontend] เพิ่ม `checkDuplicate: true` เฉพาะที่ฟอร์มอัปโหลดในหน้าคลังรวม `/admin/documents` เท่านั้น (KL-21) — จุดเดียวในระบบที่ส่งค่านี้เป็น `true`
- [x] [frontend] เพิ่ม dialog เตือนซ้ำเมื่อได้ `409` ตาม KL-22 — ระบุซ้ำแบบไหน (ชื่อ/เนื้อหา/ทั้งสอง), ชื่อไฟล์+ป้ายขอบเขตของใบที่ซ้ำ (ใช้ป้ายชุดเดียวกับ KL-6), วันที่อัปของใบเดิม, ปุ่ม "อัปโหลดต่อไป" (ส่งคำขอเดิมซ้ำด้วย `checkDuplicate: false`) และ "ยกเลิก"; เขียนกำกับไว้ว่าการเตือนครอบเฉพาะเอกสารที่อัปหลังระบบเปิดใช้ความสามารถนี้ (เอกสารเก่า `ContentHash = null` ไม่ถูกจับ)
- [x] [frontend] แก้ `src/types/domain.ts` — เพิ่ม `UploadDocumentDto.checkDuplicate` (`boolean`, default `false`), type ของ `DuplicateDocumentDto`/409 response, `KnowledgeQnAFilter` · **เพิ่ม 2026-08-25 (มติ Q-H2, ตาม KL-23/KL-26)**: เพิ่ม `CreateKnowledgeQnADto.confirmDuplicate` (`boolean`, default `false`) และ type ใหม่ `DuplicateQnAResponse = { duplicateByQuestion: KnowledgeQnA[] }` (ใช้ type `KnowledgeQnA` ที่มีอยู่แล้วที่ `domain.ts:523-532` ห้ามสร้าง type ใหม่ซ้ำ) — ลบ type ใดๆ ที่เคยถูกเพิ่มไว้แทน `DuplicateWarning`/`KnowledgeQnACreateResultViewModel` เดิมของรอบแรก ถ้ามี
- [x] [frontend] แก้ `src/lib/api-client.ts` — เพิ่มเมธอดเรียก `GET /api/knowledge-qna`, ปรับเมธอด upload document ให้ส่ง `checkDuplicate` และรองรับ handling `409`
- [x] [backend] อัปเดต `frontend/docs/API_CONTRACT.md` ให้ตรง wire contract ใหม่ของ `GET /api/documents` (ไม่ส่ง `scopeType` = ทุก scope), `GET /api/knowledge-qna` ใหม่, และ `UploadDocumentDto.checkDuplicate`/409 payload

## Phase 9: Upload consolidation (R8) — delete-only, no gate

**ขึ้นกับ:** Phase 8 (Module H) — **hard ordering constraint (R-18): ต้อง deploy หลัง Phase 8
เสมอ**, ไม่ใช่แค่เขียนโค้ดเรียงกัน — ลบการ์ดโดยที่หน้าคลังยังไม่มี KL-2/KL-4/KL-7 จะทำให้ CS
ไม่เหลือทางเห็นเอกสารระดับบทเรียนจากที่ไหนเลยทั้งระบบ

**ไม่มี migration ไม่มี endpoint ใหม่ ไม่มีการเปลี่ยน wire contract** — งานลบ UI ล้วน

- [x] [frontend] ลบการ์ด "เอกสารประกอบ" ออกจาก `frontend/src/components/admin/LessonForm.tsx` บรรทัด 679–698 (`CardHeader` + คำอธิบาย + `<DocumentUploadList fixedScope=... primaryDocumentId=... />`) และลบ `import DocumentUploadList` ที่บรรทัด 11 ตามไป (UC-1) — ⚠️ ที่อยู่จริงคือ `LessonForm.tsx` **ไม่ใช่** `app/admin/lessons/[slug]/page.tsx` ที่ `requirement.md` R8.1/R8.3 ระบุไว้ (ที่อยู่ก่อน refactor)
- [x] [frontend] ลบโหมด `fixedScope` ออกจาก `DocumentUploadList` ทั้งโหมด (UC-2) — ลบ props union (`DocumentUploadList.tsx:131-133`), ตัวแปร `libraryMode`, `activeScope`, `fixedScope ?? ...`, เงื่อนไข `{libraryMode && ...}` ทุกจุด เหลือทางเดียวคือ call site ที่ `/admin/documents`
- [x] [frontend] ลบ prop `primaryDocumentId` ออกจาก `DocumentUploadList.tsx:132, 356-360` (UC-3) — **ตรวจยืนยันว่า badge ตาม KL-7 (Phase 8) ทำหน้าที่แทนอยู่แล้วที่หน้าคลัง ห้ามลบทั้งสองที่พร้อมกันโดยไม่มี badge รองรับ**
- [x] [frontend] แทนที่การ์ดที่ถูกลบด้วยบรรทัดสรุปอ่านอย่างเดียวใน `LessonForm.tsx` (UC-6) — "เอกสารประกอบของบทเรียนนี้: N รายการ" + ลิงก์ "ดูในคลังความรู้" โดย N มาจาก `listDocuments({ scopeType: "lesson", scopeId: lesson.id })` ที่มีอยู่แล้ว (`api-client.ts:506`) ไม่ต้องมี endpoint ใหม่ — แสดงเฉพาะตอนแก้บทเรียนที่มีอยู่แล้ว (มี `lesson.id`)
- [x] [frontend] ลิงก์ "ดูในคลังความรู้" ตาม UC-7 ต้องพา query param มาให้แล้ว — `/admin/documents?scopeType=lesson&scopeId=<lesson.id>` (หน้าคลังอ่าน filter ตั้งต้นจาก URL ตามที่ทำไว้แล้วใน Phase 8)
- [x] [frontend] ตรวจว่าบรรทัดสรุป UC-6 **ไม่มี** ปุ่มอัปโหลด/ลบ/ย้าย scope/ตารางรายการไฟล์ใดๆ (UC-8) — อ่านอย่างเดียวเท่านั้น
- [x] [frontend] ตรวจยืนยัน `app/admin/lessons/new/page.tsx` ไม่ต้องแก้อะไร (UC-9) — การ์ดเดิม render เฉพาะเมื่อมี `lesson` (แก้บทเรียนที่มีอยู่) หน้าสร้างใหม่ไม่เคยมีการ์ดนี้อยู่แล้ว และไม่ต้องมีบรรทัดสรุป UC-6 ด้วยเหตุผลเดียวกัน (ยังไม่มี `lesson.id`)

⛔ **ห้ามแตะ `handlePdfUpload` (`LessonForm.tsx:187`), `pdfField`, `handlePdfFileSelected`, และ input ที่บรรทัด 450 ในเฟสนี้ (UC-5/R8.3)** — เรียกจากบรรทัด 243/250/450, ใช้ `api.uploadDocument` ตัวเดียวกับการ์ดที่ถูกลบซึ่งเป็นเหตุผลเดียวที่คนอ่านผิดได้, หลังอัปเรียก `previewPdfLessonContent` ต่อเพื่อสร้าง `slideConfigs` + ตั้ง `pdfDocumentResourceId` — ลบทางนี้ = สร้างบทเรียนแบบ PDF ไม่ได้อีกเลย และ R4 ทั้งชุดพัง

## Phase 10: Content-management phase for PDF lesson creation (CR-2) — Module J 🔒 Security gate

**ขึ้นกับ:** Phase 1 (Module A — บทเรียนต้องมีหมวดตั้งแต่ขั้นแรกของ NR-12), Phase 3 (Module C —
`BackgroundJob` ที่ NR-6 ใช้ตอน flush), Phase 5 (Module E — endpoint บันทึกบทพูด + หน้าแก้บทพูด +
`ILessonSlideNarrationResolver` ที่ข้อนี้ใช้ซ้ำทั้งชุด ไม่ได้เขียนใหม่), Phase 7 (Module G —
`ScopeType`/`ScopeId` ขาเขียนของเอกสาร + `EnsureValidScope` ที่ NR-14 พึ่ง) — ทั้งสี่มีโค้ดอยู่แล้ว
เริ่มได้ทันที ไม่ต้องรออะไรเชิงฟังก์ชัน

**ไม่ขึ้นกับ Phase 8/9 เชิงฟังก์ชัน แต่ห้ามทำขนานกับ Phase 9 (R-24)** — ทั้งสองแก้
`frontend/src/components/admin/LessonForm.tsx` ไฟล์เดียวกันคนละบริเวณ (Phase 9 ลบการ์ด
"เอกสารประกอบ" ~บรรทัด 679-698 + โหมด `fixedScope`; Phase 10 รื้อ flow การสร้าง/`handlePdfUpload`
ในบริเวณอื่นของไฟล์เดียวกัน) — ทำขนานกันเมื่อไหร่ฝ่ายหนึ่งจะเขียนทับอีกฝ่ายโดยที่ typecheck/build
ผ่านหมด **⚠️ Phase 9 ต้อง merge เป็นโค้ดจริงก่อน ไม่ใช่แค่มี task list ใน `plan.md`** — **แก้ไข
2026-08-26 (รอบวาง Phase 10): ตรวจโค้ดจริงแล้วพบว่า `LessonForm.tsx` ตอนนี้มีบรรทัดสรุปอ่านอย่าง
เดียว ("เอกสารประกอบของบทเรียนนี้: N รายการ") แทนการ์ด `DocumentUploadList`/`fixedScope` เดิมแล้ว
(commit `91e6d19`) — ฝั่งโค้ดของ Phase 9 จึง merge จริงแล้ว ไม่ใช่ "ยังไม่มีบรรทัดเดียว" ตามที่เคย
เขียนไว้** แต่ checkbox ของ Phase 9 ใน `plan.md` **ยังไม่มีช่องไหนถูกติ๊กเลย** (ติ๊กได้เฉพาะ
`qa-engineer` เท่านั้น) → ยังไม่ผ่าน QA formal อย่างเป็นทางการ ดังนั้น **ห้าม dispatch งาน
`[frontend]` ของ Phase 10 จนกว่า Phase 9 จะผ่าน `qa-engineer` และมี checkbox ติ๊กครบ** (เกณฑ์เดิม
"merged + ผ่าน QA" ยังใช้อยู่ครบสองเงื่อนไข มีแค่เงื่อนไขแรกที่เป็นจริงแล้ว) — งาน `[backend]` ของ
Phase 10 ไม่แตะ `LessonForm.tsx` จึงไม่ติดเงื่อนไขนี้ ทำได้ก่อน · ถ้าจำเป็นต้องสลับลำดับจริง ๆ ทำได้
แต่ต้องเรียงกัน ห้ามคร่อมกัน

**ไม่มี migration · ไม่มีฟิลด์ใหม่ · ไม่มี entity ใหม่ · Data Model ไม่ถูกแตะแม้ฟิลด์เดียว** — ทุก
endpoint ใหม่ของเฟสนี้ไม่แตะฐานข้อมูล (NR-10 · NR-18) ส่วนที่เหลือคือการเรียงลำดับ endpoint ที่มี
อยู่แล้ว (NR-12)

🔒 **เหตุผลของ gate (design.md §Module J) — 3 ข้อ**:
(1) ไฟล์จากภายนอกถูก parse/render ในโปรเซสโดยไม่มีแถวใน DB คุมอยู่ — endpoint ใหม่ไม่ผ่าน
`DocumentParserFactory` (ตัวนั้นอยู่ในเส้นทาง `UploadAsync` เท่านั้น) ไฟล์พังหรือไม่ใช่ PDF ต้องตก
เป็น 4xx สะอาดเสมอ ห้ามหลุดเป็น 500 หรือทำ process ตาย
(2) company isolation ของ preview session เขียนด้วยมือ (NR-11) — `IMemoryCache` ไม่มี
`HasQueryFilter` มาช่วยเลย เป็นของชิ้นแรกในโปรเจกต์ที่เก็บเนื้อหาไฟล์ของบริษัทหนึ่งไว้นอก
PostgreSQL โดยชี้ด้วย id ที่มาจาก request
(3) ต้นทุนหน่วยความจำ/CPU ที่ผู้ใช้ที่ล็อกอินแล้วสั่งได้โดยตรง (R-22) — ไฟล์ 30 MB ต่อ session
ค้างในหน่วยความจำ 10 นาทีแบบ sliding + PDFium render ทุกหน้าที่ CS เลื่อนดู

- [x] [backend] `POST /api/lessons/pdf-preview/session` (NR-10) — multipart field `file` เดียว,
  `[RequestSizeLimit(30 * 1024 * 1024)]` ค่าเดียวกับ `DocumentsController` ห้ามตั้งค่าใหม่, เรียก
  `PdfSlidesRenderer.BuildContent(stream, previewId, file.FileName)` ในหน่วยความจำล้วน (ห้ามเขียน
  PostgreSQL/object storage/Pinecone/`BackgroundJob` ในเส้นทางนี้เด็ดขาด), คืน
  `{ previewId, title, pageCount, isLikelyScanned, slides: [{ slideObjectId, index, narrationText }] }`
  ตรงกับ `LessonNarrationSlideViewModel` ทุกฟิลด์ที่ใช้ร่วม (`IsOverridden` ไม่มีในเฟสนี้ ละไปเฉย ๆ
  ไม่ใช่ส่ง `false` ปลอม) — `previewId = IdGenerator.GenerateId("pdfprev")`
- [x] [backend] เก็บ byte ทั้งไฟล์ + `CompanyId` ของผู้เรียกใน `IMemoryCache` คีย์
  `pdf-preview:{previewId}`, `SlidingExpiration = TimeSpan.FromMinutes(10)` — รูปเดียวกับ
  `pdf-bytes:{documentId}` ที่ `GetPdfBytesAsync` ใช้อยู่แล้ว ห้ามสร้างกลไก cache ที่สอง — **ไม่มี
  endpoint ลบ session** หมดอายุเองทางเดียวเท่านั้น (ห้ามเพิ่ม `DELETE`)
- [x] [backend] `GET /api/lessons/pdf-preview/{previewId}/pages/{pageNumber:int}` (NR-10) — คืน
  `image/png` จาก `PdfSlidesRenderer.RenderPagePng(stream, pageNumber)` โดยอ่าน byte จาก
  `IMemoryCache` เดียวกับข้างต้น
- [x] [backend] implement NR-11 — ทั้งสอง endpoint ข้างต้นต้องเทียบ `CompanyId` ที่เก็บไว้กับ
  `CurrentCompanyId` **ก่อน**ใช้ byte แม้แต่ไบต์เดียว, ไม่ตรง **หรือ**หมดอายุ/ไม่มีรายการ →
  `GeneralException.NotFound` ทั้งสองกรณี (ข้อความเดียวกัน ห้ามแยกให้เดาได้ว่า id นี้มีอยู่จริงแต่เป็น
  ของบริษัทอื่น) — `previewId` ห้ามใช้แทน `documentId` ที่ endpoint ใดของระบบ และ `documentId` ห้าม
  ใช้กับ endpoint ของ NR-10 (คนละ id space)
- [x] [backend] implement NR-5 ในฝั่ง preview session — สูตร `isLikelyScanned` ต้องเป็นสูตรเดียวกับ
  `LessonSlideNarrationService.GetAllAsync` เป๊ะ: `Slides.Count > 0 && Slides.All(s =>
  IsNullOrWhiteSpace(s.SpeakerNotes))`
- [x] [backend] `GET /api/documents/{documentId}/pdf-pages/{pageNumber:int}` (NR-18) —
  admin-auth, company scope มาจาก query filter ของ `_documentResourceRepository.Get` ตามปกติ (ไม่
  ต้อง `IgnoreQueryFilters()`), ใช้ `RenderPdfPageAsync` เดิมทั้งดุ้น (ห่อบาง ๆ ~10 บรรทัด) — endpoint
  learner-side เดิมที่ผูก link token (`LessonController:69-89`) ห้ามถูกแก้, ห้ามถูกเรียกจากฝั่ง admin
  และห้ามถูกเปิดให้ใช้โดยไม่มี token
- [x] [backend] unit test (R-12 pattern) — NR-11: อ่าน `pdf-preview:{previewId}` ด้วย `CompanyId`
  ที่ไม่ตรงเจ้าของต้องได้ `NotFound` **เหมือนกันเป๊ะ**กับกรณี id หมดอายุ/ไม่มีอยู่จริง (ข้อความต้อง
  เท่ากันทั้งสองกรณี — พิสูจน์ว่าไม่มีทางเดาได้ว่า id นี้เป็นของบริษัทอื่น)
- [x] [backend] อัปเดต `frontend/docs/API_CONTRACT.md` ให้ตรง endpoint ใหม่ทั้งสามตัวของ Module J
  (`POST /api/lessons/pdf-preview/session`, `GET /api/lessons/pdf-preview/{previewId}/pages/{n}`,
  `GET /api/documents/{documentId}/pdf-pages/{n}`)
- [x] [frontend] แก้ `LessonForm.tsx` โหมด `mode="create"` + `contentSourceType === "pdf"` —
  เลือกไฟล์เรียก `POST /api/lessons/pdf-preview/session` แทน `handlePdfUpload` (NR-12/CR-2.a) —
  ไม่มีอะไรถูกเขียนลง server จนกว่าจะกดยืนยันสร้าง — **โหมดแก้ (`isEdit`) ไม่เปลี่ยนพฤติกรรมแม้
  ตัวอักษรเดียว** ยังเรียก `handlePdfFileSelected`/`handlePdfUpload` เดิมทั้งเส้น รวม dialog นับหน้า
  ของ NR-3 (NR-17)
- [x] [frontend] แก้เงื่อนไข `canCreate` (`LessonForm.tsx:335-341`) สำหรับโหมด `create`+`pdf` — จาก
  "มี `form.pdfDocumentResourceId`" เป็น "มี preview session สำเร็จ" (NR-12)
- [x] [frontend] แก้ `handleCreate` (`LessonForm.tsx:264-275`) สำหรับโหมด `create`+`pdf` — จากเรียก
  `saveLesson` แล้ว `router.push` ทันที เป็นเปิดเฟสจัดการเนื้อหาใหม่ (R4.6.2) — **โหมด
  `google_slides` คงเดิมทุกตัวอักษร** กด "สร้าง" แล้ว `saveLesson` ทันทีเหมือนวันนี้ (R4.6.9/NR-9)
- [x] [frontend] สร้าง component ร่วม `SlideNarrationEditorCard` รับ prop `imageSrc: string` (NR-18,
  มติ `Q-J2` = "ใส่ทั้งสองที่") — ใช้ทั้งเฟสจัดการเนื้อหาใหม่และหน้าแก้บทพูดเดิม
- [x] [frontend] สร้างหน้า/เฟสจัดการเนื้อหาก่อนสร้างบทเรียน PDF — list ทุกหน้าจาก preview session,
  ต่อหน้าแสดงภาพจาก `GET /api/lessons/pdf-preview/{previewId}/pages/{n}` คู่กับ `SlideNarrationEditorCard`
  (textarea แก้บทพูด, prefill จาก `narrationText`), เก็บ draft เฉพาะหน้าที่ CS แตะไว้ใน state ฝั่ง
  client ล้วน (R4.6.4/R4.6.5 — ห้ามยิง server เพื่อ persist ระหว่างเฟส) — แสดงคำเตือน
  `isLikelyScanned` **ทันทีที่เข้าเฟส ไม่ใช่ตอนกดยืนยัน** (NR-5)
- [x] [frontend] implement NR-15 — ก่อนยืนยันเมื่อเข้าเงื่อนไขใดก็ได้: (ก) ไม่มีหน้าไหนถูกแตะเลย
  (ข) มีหน้าที่ข้อความว่างหลัง trim (รวมเคสไฟล์สแกน) → เตือนพร้อม**จำนวน**หน้า (ไม่ใช่คำเตือนลอย ๆ)
  แล้วให้กดยืนยันซ้ำผ่านได้เสมอ ไม่มีทางไหนที่ระบบปฏิเสธถาวร — คำนวณจาก draft ฝั่ง client ล้วน
  **ห้ามยิง server เพื่อถาม** — ห้ามบังคับเลื่อนดูครบทุกหน้า และห้ามนับ "เลื่อนผ่าน" เป็นการตรวจ
- [x] [frontend] implement NR-16 — เปลี่ยน/แทนที่ไฟล์ระหว่างอยู่ในเฟส: ล้าง draft ทุกหน้า + ทิ้ง
  preview session เดิม (ปล่อยหมดอายุเอง ไม่เรียก endpoint ใด) + สร้าง session ใหม่จากไฟล์ใหม่ —
  **เงียบ ๆ ไม่มี dialog ไม่มีการนับหน้า** (จงใจต่างจาก NR-3 เพราะยังไม่มีอะไร persist) — ยังอยู่ใต้
  NR-4 เต็มที่: ห้ามพยายามจับคู่หน้าเก่ากับหน้าใหม่
- [x] [frontend] implement NR-12 — ลำดับยืนยันการสร้าง 4 ขั้นตายตัว **ห้ามสลับ 3↔4 เด็ดขาด** (ดู
  คำเตือนกับดักที่ NR-3 ท้ายข้อ — สลับแล้วบทพูดที่เพิ่ง flush จะถูกลบทิ้งเงียบ ๆ ทั้งชุด): **ขั้น 1**
  `POST /api/lessons` (`contentSourceType: "pdf"`, `pdfDocumentResourceId: undefined`,
  `slideConfigs` จาก preview session, `categoryId`/`isActive` ตามฟอร์ม) → ได้ `lesson.id` **ขั้น 2**
  `POST /api/documents` ด้วย**ไฟล์จริงจากเบราว์เซอร์** (ไม่ใช่ `previewId`) + `scopeType: "lesson"`,
  `scopeId: lesson.id` (NR-14), `checkDuplicate: false` **ขั้น 3** `POST /api/lessons` อีกครั้งใส่
  `pdfDocumentResourceId` จากขั้น 2 (จุดที่ NR-7 index ทั้งเด็คแบบ inline) **ขั้น 4** flush บทพูด
  ทีละหน้าเฉพาะหน้าที่ CS แตะผ่าน `PUT /api/lessons/{id}/narrations/{slideObjectId}` เดิม (NR-2) —
  ต้องมี progress bar (จำนวนขั้น = 3 + จำนวนหน้าที่แก้) · สำเร็จครบ 4 ขั้น → `router.push` ไปหน้า
  แก้บทเรียนของ slug นั้น
- [x] [frontend] implement NR-13 — สัญญาความล้มเหลวครบ 4 กรณีของ NR-12 ไม่เหลือให้ engineer ตัดสิน:
  **ล้มขั้น 1** → ไม่มีอะไรเกิดขึ้นทั้งระบบ, error ของ server ตรง ๆ, CS อยู่ในเฟสเดิม draft ทุกหน้า
  ยังอยู่ครบ กดยืนยันซ้ำได้ · **ล้มขั้น 2** → มีบทเรียนที่ยังไม่มีไฟล์ **ห้ามลบบทเรียนทิ้งเองโดย
  อัตโนมัติ**, UI บอกตรง ๆ ว่า "บทเรียนถูกสร้างแล้ว แต่อัปไฟล์ไม่สำเร็จ" + ปุ่ม "ลองอัปไฟล์อีกครั้ง"
  ทำต่อจากขั้น 2 (ไฟล์และ draft ยังอยู่ในเบราว์เซอร์ครบ) · **ล้มขั้น 3** → บทเรียน+เอกสารมีแล้วแต่ยัง
  ไม่ผูกกัน, ปุ่ม "ลองอีกครั้ง" ทำต่อจากขั้น 3 **ห้ามอัปไฟล์ใหม่ซ้ำ** · **ล้มขั้น 4 หน้าที่ k** →
  บทเรียนใช้งานได้จริงแล้ว บทพูดเซฟไปบางหน้า, UI บอกจำนวนหน้าที่สำเร็จ/ไม่สำเร็จ + ปุ่มลองซ้ำเฉพาะ
  หน้าที่เหลือ + **ต้องมีลิงก์ "ไปหน้าแก้บทพูด" (`/admin/lessons/{slug}/narrations`) เสมอ** — ทุกกรณี
  ข้างบน: ห้ามลบเอกสารที่อัปสำเร็จไปแล้วโดยอัตโนมัติ
- [x] [frontend] แก้ `handlePdfUpload` (โหมดแก้ `isEdit`) — ส่ง `scopeType: "lesson"` เสมอตาม NR-14
  (เดิม hardcode `company`) ให้ทั้งสองโหมด (สร้าง/แก้) ใช้ `lesson` scope ตรงกัน — **ไม่แตะ dialog
  นับหน้าของ NR-3 หรือลำดับเรียกอื่นใดของโหมดแก้**
- [x] [frontend] แก้หน้า `/admin/lessons/[slug]/narrations` — สลับไปใช้ `SlideNarrationEditorCard`
  ร่วม ส่ง `imageSrc` จาก `GET /api/documents/{documentId}/pdf-pages/{pageNumber}` ใหม่ (NR-18) —
  หน้าตาต้องเหมือนกับเฟสสร้างใหม่ตาม R4.6.4
- [x] [frontend] เพิ่ม type ใน `src/types/domain.ts` — `PdfPreviewSessionResponse`,
  `PdfPreviewSlide` (ไม่มีฟิลด์ `isOverridden`) ตรงกับ response ของ NR-10
- [x] [frontend] เพิ่มเมธอดเรียก 3 endpoint ใหม่ใน `src/lib/api-client.ts` —
  `createPdfPreviewSession(file)`, `getPdfPreviewPageUrl(previewId, pageNumber)`,
  `getLessonPdfPageUrl(documentId, pageNumber)` (NR-18)

## Phase 11: Cut pages from PDF lesson (R4.7) — Module K 🔒 Security gate

**⛔ ห้ามเริ่ม phase นี้ก่อน Phase 10 ปิดรอบ QA แบบ FULL (R-26/มติ Q-K4) — ดูรายละเอียดเงื่อนไขที่
`## Sequencing Notes`** ณ วันที่วางแผนนี้ Phase 10 ผ่านแค่ TARGETED 21/21 ยังไม่ FULL ยังไม่ audit
ยังไม่ deploy — เขียน task ไว้พร้อม dispatch ได้ แต่ **ห้ามมอบหมายงานจริงจนกว่าเงื่อนไขทั้งสามข้อ
ใน Sequencing Notes จะเป็นจริงครบ**

**ขึ้นกับ:** Phase 3 (`BackgroundJob` + `vector_delete` + `DeleteVectorsAsync` ที่ EX-5/EX-6 ใช้ทั้งชุด)
· Phase 4 (`DocumentChunk` — `VectorId`/`NamespaceKey`/`Text` คือสิ่งเดียวที่ทำให้ EX-6 ทำได้โดยไม่ต้อง
parse ไฟล์ใหม่) · Phase 5 (endpoint/หน้าแก้บทพูด + `ILessonSlideNarrationResolver` ที่ใช้ซ้ำทั้งชุด) ·
Phase 10 (`SlideNarrationEditorCard` + `PdfLessonContentPhase` + ลำดับ 4 ขั้นของ NR-12 ที่ EX-9 ต่อยอด)
— สามตัวแรกมีโค้ดอยู่แล้ว **ตัวสุดท้ายคือเงื่อนไขเวลา ไม่ใช่แค่เงื่อนไขฟังก์ชัน (R-26)**

**ไม่มี field ใหม่บนตารางเดิม ไม่มี backfill — Data Model ถูกแตะแค่ตารางใหม่ 1 ใบ (`LessonExcludedSlide`)**
— MG-K1 กับ EX-1..EX-12 อยู่ phase เดียวกันทั้งหมดตามข้อบังคับของ `design.md` §Migration Plan

🔒 **เหตุผลของ gate (design.md §Module K) — 3 ข้อที่ `security` ต้องตรวจแยกเป็นรายการ**:
(1) `slideObjectId` ที่ผู้ใช้ส่งเข้ามาดิบๆ ถูกใช้ประกอบ vector id ที่จะถูก *ลบจริง* ใน Pinecone
สองชุดพร้อมกัน (`pdf-page-N` ใน `lesson_index` และ `{documentId}-page-N` ใน `vector_delete`) — ไม่ validate
ว่าเป็นหน้าที่มีอยู่จริงของบทเรียนใบนั้น (EX-12(ข)) จะเกิดการลบข้ามบทเรียนได้ทันที
(2) `LessonId` ต้องผ่าน query filter ของบริษัทก่อนถูกใช้ประกอบ namespace — ถ้าไม่ตรวจคู่กับ
`slideObjectId` ขอบเขตความเสียหายกว้างกว่าข้อ (1) อีก
(3) การ toggle ซ้ำๆ ต้องไม่ทำให้เกิดงานลบสะสมที่ลบของคนอื่น (idempotency ของ EX-4)
**สิทธิ์ `cs`/`admin` เท่ากับการแก้บทพูดวันนี้ไม่ใช่คำตอบของข้อนี้** — การแก้บทพูดผิดหน้าคือข้อความผิด
การตัดผิดหน้าคือ vector หายจากคลังถาวร

- [x] [backend] สร้าง entity `LessonExcludedSlide` ตาม DM-17 (`SupportRoom.Domain/Entities/LessonExcludedSlide.cs`) — `Id`/`CompanyId`/soft-delete fields ตาม `ICompanyScoped`/`IEntityMaster<string>` มาตรฐาน, `LessonId` (`required string`), `SlideObjectId` (`required string`, ค่า `pdf-page-N`) — **ห้ามเติมฟิลด์อื่นใดนอกเหนือจากที่ระบุ** (ไม่มี `Reason`/`ExcludedByRole`/`SortOrder`/`IsSkipTeachingOnly` — CR-3.13 ปฏิเสธไปแล้ว)
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม `DbSet<LessonExcludedSlide>`, index `(LessonId, SlideObjectId)` **ไม่ unique** (กติกา "หน้าละหนึ่งแถว" บังคับที่ service layer แทน) และ index `(CompanyId)`, `HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete)` ตาม DM-17
- [x] [backend] สร้าง EF Core migration `AddLessonExcludedSlides` ตาม MG-K1 — additive ล้วน: สร้างตาราง `LessonExcludedSlide` เท่านั้น ไม่มีคอลัมน์ใหม่บนตารางเดิม ไม่มี backfill ไม่มี `UPDATE` ไม่แตะ Pinecone; `Down()` = drop table
- [x] [backend] สร้าง `ILessonExcludedSlideRepository` — `GetByLessonId(lessonId)` (คืนทุกแถวรวม soft-deleted เพื่อให้ toggle หาแถวเดิมกลับมาปลด `IsDelete` ได้ตาม EX-4), `GetOne(lessonId, slideObjectId)` (รวม soft-deleted), `DeleteByLessonId(lessonId)` (soft, คืนจำนวนที่ลบ) — ลงทะเบียนใน `UnitOfWork.Register`
- [x] [backend] เพิ่มค่าใหม่ `LessonPage` ใน `VectorDeleteTargetKind` (`static class`/`const string` ตาม convention เดิมของ enum นี้) — คนละค่ากับ `Document` เพราะ `Document` มี guard `stillDeleted` ที่จะ `return` ทิ้งงานทันทีเมื่อเอกสารเด็คยัง active อยู่ (EX-6)
- [x] [backend] implement EX-4 — `PUT /api/lessons/{id}/slides/{slideObjectId}/excluded` body `{ excluded: bool }`, admin-auth ปกติ (`cs`/`admin`, ห้าม `[AllowAnonymous]`) — `excluded=true`: มีแถวไม่ถูกลบอยู่แล้ว = no-op ตอบ 200 (idempotent), มีแถว soft-deleted = ปลด `IsDelete` แถวเดิมกลับมาใช้ (ห้ามสร้างแถวใหม่), ไม่มีแถว = สร้างใหม่ · `excluded=false`: soft delete แถว, ไม่มีแถวอยู่แล้ว = no-op · **enqueue งานตาม EX-5/EX-6 เฉพาะเมื่อสถานะเปลี่ยนจริงเท่านั้น**
- [x] [backend] implement EX-12(ข) ใน endpoint ของ EX-4 (และทุก call site ที่รับ `slideObjectId` จาก request ในเฟสนี้) — validate ว่า `slideObjectId` อยู่ในชุดที่ `PreviewPdfAsync(lesson.PdfDocumentResourceId)` คืนมาจริง ก่อนใช้ประกอบ vector id ใดๆ ทั้งสิ้น ไม่อยู่ → `GeneralException.NotFound("หน้าเอกสาร")` (รูปเดียวกับ `LessonSlideNarrationService.SaveAsync` วันนี้)
- [x] [backend] implement EX-8 — ตรวจพื้นแข็ง "เหลืออย่างน้อย 1 หน้า" ที่ server ทุกเส้นทาง (ทั้ง EX-4 และ EX-9): จำนวนหน้าทั้งหมดมาจาก `lessonConfigService.PreviewPdfAsync(...)` เท่านั้น (ห้ามเชื่อ client, ห้ามใช้ `SlideConfigs.Count`) — `pageCount - excludedCountAfterThisChange < 1` → `GeneralException.ValidationError("บทเรียนต้องเหลืออย่างน้อย 1 หน้า - ตัดหน้าสุดท้ายไม่ได้")` เป็นข้อห้ามแข็ง **ห้ามมี `confirm` flag แบบ KL-23/NR-15**
- [x] [backend] implement EX-1 (ผู้บริโภครายที่ 1) — แก้ `GetTeachingContentBySlugAsync` ให้กรองหน้าที่ถูกตัดทิ้งและเรียง `Index` ใหม่เป็น `0..M-1` ตามลำดับที่เหลือ (`GetTeachingContentByLinkAsync` ได้ผลจาก (1) อยู่แล้ว ไม่ต้องกรองซ้ำ)
- [x] [backend] implement EX-3(ก) — `TeachingSlideViewModel` ที่คืนจาก `GetTeachingContentBySlugAsync`: หน้าที่ถูกตัดหายจาก list, `Index` คำนวณใหม่ตามลำดับที่เหลือ, `SlideUrl` ยังคงมาจากเลขหน้าจริงที่สร้างไว้ก่อนกรอง (ไม่ต้องแก้)
- [x] [backend] implement EX-1 (ผู้บริโภครายที่ 3) — แก้เส้น `SaveAsync`/NR-7 (index ทั้งเด็คแบบ inline) ให้กรองหน้าที่ถูกตัดทิ้งก่อน build content เข้า index — ต้องเกิด**หลัง**จาก exclusion ของคำขอนั้นถูกเขียนลง DB แล้วตามลำดับบังคับของ EX-9
- [x] [backend] implement EX-5/EX-6 (ผู้บริโภครายที่ 4) — แก้ `ProcessLessonIndexAsync`: หน้าที่ถูกตัดต้องเข้า `toDelete` เสมอ **ห้ามถูกดรอปด้วย `.Where(resolvedById.ContainsKey)`** (กับดักของ NR-6 ที่ทำให้ R4.7.4 ล้มเงียบๆ ถ้าพลาด) · เอาหน้ากลับ = resolve ผ่าน NR-1 ตามปกติแล้ว upsert (บทพูดเดิมคืนมาเองตาม R4.7.8)
- [x] [backend] implement EX-6 — ถอด/คืน vector ชุดที่ 2 (สำเนาเอกสารของเด็ค `{documentId}-page-N`): หา `DocumentChunk` ของ `lesson.PdfDocumentResourceId` ที่ `ChunkKey == "page-{N}"` (N แปลงตรงจาก `pdf-page-N`) — ไม่เจอแถว (หน้าว่าง) = ไม่มีอะไรต้องลบ ไม่ใช่ error · ตัดหน้า → enqueue `BackgroundJob(vector_delete)` ด้วย `VectorDeleteJobPayload` เดิม, `Kind = LessonPage`, `NamespaceKey` มาจาก `DocumentChunk.NamespaceKey` ของแถวนั้น (ไม่ใช่ namespace คำนวณสดจาก scope ปัจจุบัน) · เอาหน้ากลับ → embed `DocumentChunk.Text` ของแถวนั้นแล้ว upsert กลับด้วย `VectorId`/`NamespaceKey` เดิม (embedding หน้าเดียว) · **แถว `DocumentChunk` ไม่ถูกแตะเลยทั้งสองทิศทาง**
- [x] [backend] implement EX-6 จุดบังคับที่สอง — แก้ `ProcessDocumentIndexAsync`: เมื่อ index เอกสารที่เป็น `PdfDocumentResourceId` ของบทเรียนใด ต้องสร้างแถว `DocumentChunk` ครบทุกหน้าตามเดิม **แต่ตัดหน้าที่ถูกตัดออกจากชุดที่ส่งไป embed/upsert** — ไม่งั้น re-index รอบถัดไป (ย้าย scope DS-5 / กู้คืน DI-15) จะทำให้ vector ของหน้าที่ถูกตัดฟื้นขึ้นมาเงียบๆ
- [x] [backend] implement EX-9 — เพิ่มฟิลด์ `excludedSlideObjectIds: string[]` (optional) ใน `POST /api/lessons` — `null`/ไม่ส่ง = ไม่แตะของเดิม, `[]` = ไม่มีหน้าไหนถูกตัด, มีค่า = แทนที่ทั้งชุด (ไม่ใช่เพิ่มทีละหน้า) — ทุก `slideObjectId` ต้อง validate ตาม EX-12 และผ่านพื้นแข็ง EX-8 ก่อนเขียน · **ลำดับภายใน `SaveAsync` เป็นข้อบังคับ**: (ก) ล้าง `LessonExcludedSlide`/`LessonSlideNarration` เดิมของ NR-3 → (ข) เขียน exclusion ชุดใหม่ → (ค) NR-7 index ทั้งเด็คโดยข้ามหน้าที่ถูกตัด — สลับ (ก)(ข) = หน้าที่ CS ตัดทั้งเฟสหายเงียบๆ
- [x] [backend] implement EX-10 — trigger เดิมของ NR-3 (`LessonConfig.PdfDocumentResourceId` เปลี่ยนค่า) ต้องล้าง `LessonExcludedSlide` ของบทเรียนนั้นด้วยในทรานแซกชันเดียวกับที่ล้างบทพูด (`_excludedSlideRepository.DeleteByLessonId` ต่อจาก `_narrationRepository.DeleteByLessonId`)
- [x] [backend] แก้ `GET /api/lessons/{id}/narrations/count` (breaking wire contract — caller เดียวคือ `LessonForm.tsx` โหมด `isEdit`) — response เปลี่ยนจาก `{ count }` เป็น `{ count, excludedCount }`; `ILessonSlideNarrationService.CountByLessonId` คืนคู่ค่าแทน `int`
- [x] [backend] implement EX-2 — ทุก endpoint ใหม่ของเฟสนี้ใช้ `EnsurePdfSource` ตัวเดิมของ `LessonSlideNarrationService` (ห้ามเขียน guard ตัวที่สอง) และสิทธิ์ `cs`/`admin` เท่ากับ endpoint บทพูดวันนี้เป๊ะ — การปฏิเสธต้องเกิดที่ server ทุกกรณี
- [x] [backend] implement EX-12(ก) — `LessonSlideNarrationService.SaveAsync` ปฏิเสธการแก้บทพูดของหน้าที่ถูกตัดอยู่ด้วย `GeneralException.ValidationError("หน้านี้ถูกตัดออกจากบทเรียนแล้ว - เอาหน้ากลับก่อนจึงจะแก้บทพูดได้")` — และยังคง NR-2 (ห้ามสร้างแถวบทพูดขึ้นมา "เก็บ prefill ไว้ให้" เวลาตัดหน้าที่ไม่เคยแก้บทพูด)
- [x] [backend] implement EX-3(ข)/EX-11 — `LessonNarrationsViewModel` (คืนจาก `LessonSlideNarrationService.GetAllAsync`, ผู้บริโภครายที่ 5 ของ EX-1 ที่**ไม่กรองเลย คืนครบทุกหน้า**): `Index` คงความหมายเดิม (ลำดับหน้าจริงของไฟล์) ห้ามเรียงใหม่ + เพิ่ม `IsExcluded: bool` และ `LessonIndex: int?` (0-based ในกลุ่มที่เหลือ, `null` เมื่อถูกตัด)
- [x] [backend] DTO/ViewModel: `ToggleSlideExcludedDto` (`{ excluded: bool }`), แก้ `LessonNarrationsViewModel` ตามข้างบน, แก้ narration-count ViewModel เป็น `{ count, excludedCount }`, เพิ่ม `excludedSlideObjectIds` ใน `CreateLessonDto`
- [x] [backend] unit test — EX-8: ตรวจพื้นแข็ง "เหลืออย่างน้อย 1 หน้า" ทั้งเส้น toggle (EX-4) และเส้นสร้างบทเรียน (EX-9) ปฏิเสธด้วย `ValidationError` เสมอ ไม่มี confirm flag ให้ผ่าน
- [x] [backend] unit test — EX-9: ยืนยันลำดับ (ก)(ข)(ค) ภายใน `SaveAsync` — เขียน `excludedSlideObjectIds` ในคำขอเดียวกับที่ trigger NR-3 แล้วตรวจว่า exclusion ชุดใหม่ยังอยู่ครบ ไม่ถูกล้างทิ้งจากลำดับผิด
- [x] [backend] unit test — EX-12(ข): `slideObjectId` ที่ไม่อยู่ในชุดที่ `PreviewPdfAsync` คืนมา (รวมกรณีข้าม `LessonId` — ของบทเรียนอื่น) ต้องได้ `NotFound` ทั้งสองกรณี ไม่ใช่แค่กรณีไม่มีอยู่จริง
- [x] [backend] unit test — EX-4: toggle ซ้ำๆ (`excluded=true` สองครั้งติด, `excluded=false` บนแถวที่ไม่เคยมี) ไม่ enqueue งานซ้ำ และไม่สร้างแถวซ้ำ (หน้าละหนึ่งแถวเสมอ)
- [x] [backend] อัปเดต `frontend/docs/API_CONTRACT.md` ให้ตรง wire contract ใหม่ของ `PUT /api/lessons/{id}/slides/{slideObjectId}/excluded`, `GET /api/lessons/{id}/narrations/count` (`{count, excludedCount}`), `LessonNarrationsViewModel` (`isExcluded`/`lessonIndex`), และ `POST /api/lessons` (`excludedSlideObjectIds`)
- [x] [frontend] แก้ `src/types/domain.ts` — เพิ่ม `isExcluded`/`lessonIndex` ใน type ของ narration slide, เปลี่ยน type ของ narration-count response เป็น `{ count, excludedCount }`, เพิ่ม `excludedSlideObjectIds?: string[]` ใน type ของ create-lesson request
- [x] [frontend] แก้ `src/lib/api-client.ts` — เพิ่มเมธอด `toggleExcludedSlide(lessonId, slideObjectId, excluded)` เรียก EX-4, ปรับเมธอดเรียก narration count ให้รับ shape ใหม่, ปรับเมธอดสร้างบทเรียนให้ส่ง `excludedSlideObjectIds` ได้
- [x] [frontend] **แก้จุด blast-radius ที่ 1 (NR-4)** — `frontend/src/app/admin/lessons/[slug]/narrations/page.tsx:143` เลิกคำนวณเลขหน้าที่ส่งเข้า endpoint ภาพสไลด์จาก `slide.index + 1` เปลี่ยนเป็นแปลงจาก `slideObjectId` (`pdf-page-N` → N) เสมอ — ไม่งั้นภาพสไลด์จะชี้คนละหน้ากับบทพูดข้างๆ ทันทีที่หน้าถูกตัด/เอากลับ
- [x] [frontend] **แก้จุด blast-radius ที่ 2 (NR-4)** — `frontend/src/components/admin/PdfLessonContentPhase.tsx:309` แก้แบบเดียวกันเป๊ะ (แปลงจาก `slideObjectId` แทน `slide.index + 1`) — สองไฟล์นี้ต้องแก้พร้อมกันในเฟสเดียว ไม่ใช่แก้ที่เดียวแล้วอีกที่ค้าง
- [x] [frontend] แก้ `SlideNarrationEditorCard` (ใช้ร่วมสองที่ตาม NR-18) — เพิ่ม prop `isExcluded: boolean` + `onToggleExcluded` เรนเดอร์การ์ดสีจาง + badge "ตัดออกแล้ว" + ปุ่ม "เอากลับ" เมื่อ `isExcluded=true`, `Textarea` ต้อง `readOnly` เมื่อ `isExcluded=true` (server ปฏิเสธด้วยตาม EX-12(ก) — สองชั้น)
- [x] [frontend] แก้ `frontend/src/app/admin/lessons/[slug]/narrations/page.tsx` ตาม EX-11 — แสดงทุกหน้าเรียงตามลำดับไฟล์ ไม่ซ่อน ไม่แยกกล่อง: หน้าที่ยังอยู่แสดงเลข `LessonIndex + 1` (1–9) + ปุ่ม "ตัดหน้านี้ออก"; หน้าที่ถูกตัดแสดง "หน้าที่ N ของไฟล์" (N จาก `slideObjectId`) + ปุ่ม "เอากลับ"; ภาพสไลด์ยังต้องแสดงสำหรับหน้าที่ถูกตัด — ต่อกับ `toggleExcludedSlide` ใหม่
- [x] [frontend] เพิ่มปุ่ม "ตัดหน้านี้ออก"/"เอากลับ" ในเฟสจัดการเนื้อหา (`PdfLessonContentPhase.tsx`) ตาม EX-9 — ระหว่างเฟสเป็น draft ในเบราว์เซอร์ล้วน (ห้ามยิง endpoint EX-4 ระหว่างเฟส เพราะยังไม่มี `LessonId`), เก็บ `excludedSlideObjectIds` ไว้ใน state เดียวกับ draft บทพูด แล้วส่งไปกับขั้นที่ 3 ของ NR-12 (`POST /api/lessons` ครั้งที่สอง)
- [x] [frontend] implement EX-8 (ชั้นที่สองฝั่ง UI) — ปิดปุ่ม "ตัดหน้านี้ออก" ของหน้าสุดท้ายที่เหลือทั้งในหน้าแก้บทพูดและเฟสจัดการเนื้อหา (server ยังบล็อกจริงตาม EX-8 backend — นี่เป็นแค่ชั้นที่สอง)
- [x] [frontend] แก้เงื่อนไข NR-15 (คำเตือนก่อนยืนยันในเฟสจัดการเนื้อหา) — หน้าที่ถูกตัดต้องไม่ถูกนับในทั้งสองเงื่อนไข ("ยังไม่ได้ตรวจบทพูด N หน้า" และ "มี N หน้าที่บทพูดว่าง") รวมกรณีขอบ: ตัดทุกหน้าที่บทพูดว่างจนหมด → ไม่ต้องเตือนข้อ (ข) เลย
- [x] [frontend] แก้เงื่อนไข NR-16 (เปลี่ยนไฟล์กลางเฟส) — ล้าง draft ของหน้าที่ตัดไว้ทิ้งด้วยเงียบๆ เหมือนบทพูด (ยังไม่มีอะไร persist)
- [x] [frontend] แก้กล่องเตือนของ NR-3 (อัปโหลด PDF ทับของเดิมในโหมดแก้บทเรียนที่มีอยู่แล้ว) ตาม EX-10 — อ่าน `{ count, excludedCount }` ใหม่ ข้อความต้องบอกทั้งสองจำนวน ("บทพูดที่แก้ไว้ N หน้า และหน้าที่ตัดออกไว้ M หน้า จะถูกล้างทั้งหมด") และตัดวลีของจำนวนใดที่เป็น 0 ทิ้ง (ห้ามขึ้น "0 หน้า")

## Phase 12: Lesson trash, restore & permanent purge (R9) — Module L 🔒 Security gate

**⛔ ห้าม dispatch phase นี้ก่อน Phase 11 (Module K) ปิด implementation** — Module L ต้อง snapshot
และ hard-delete `LessonExcludedSlide` ตาม LT-15/LT-19; เริ่มก่อนจะทำให้ตารางลูกใหม่ของ K หลุดจาก
purge lifecycle ได้. Phase นี้ยังขึ้นกับ Phase 3/4/5/6 ที่มี durable `BackgroundJob`, document
chunks/vector metadata, narration และ Q&A/source/conflict อยู่แล้ว.

**Data Model และ migration ต้องอยู่ใน phase เดียวกับ lifecycle:** `MG-L1` เพิ่มเฉพาะ
`LessonConfig.PurgeJobId`/`PurgeStartedAt`, index trash และตาราง `SessionQuestionReviewExclusion`
ตาม DM-2/DM-18 — additive schema เท่านั้น, ไม่มี backfill/cleanup อัตโนมัติ, และต้องผ่าน preflight
LT-24 ก่อน apply. ห้ามเพิ่ม company retention setting, field, endpoint หรือ UI ใน phase นี้ (O-18).

🔒 **เหตุผลของ gate (design.md §Module L) — `security` ต้องตรวจแยก:** owner-only permanent delete
และ selected-company context; ทุก `IgnoreQueryFilters()` ต้องมี tenant predicate; revoked public token
ต้อง bind กับ `learnerKey` + session `IN_PROGRESS`; stale job/restore-purge race; dependency id มาจาก
DB; และ shared-PDF guard ก่อน external delete.

- [x] [backend] แก้ entity `LessonConfig` ตาม DM-2 — เปลี่ยน `DeleteBy`/`IsDelete`/`DeletedAt` จาก `init` เป็น `set`; เพิ่ม nullable `PurgeJobId`/`PurgeStartedAt` เท่านั้น พร้อมรักษา active/trash/purging/purged state invariant; ห้ามเพิ่ม enum หรือ state column ซ้ำ
- [x] [backend] สร้าง entity `SessionQuestionReviewExclusion` ตาม DM-18 ใน `SupportRoom.Domain/Entities` — audit/soft-delete fields มาตรฐาน, `SessionQuestionId`, `LessonId`, `Reason`; ห้ามเพิ่ม FK จริงหรือฟิลด์ retention เพิ่ม
- [x] [backend] เพิ่ม constants ตาม DM-11: `BackgroundJobType.LessonPurge = "lesson_purge"`, `BackgroundJobStatus.Canceled = "canceled"`, และ `QuestionReviewExclusionReason.LessonPermanentlyDeleted = "lesson_permanently_deleted"` — ใช้ `static class`/`const string` ตาม convention เดิม
- [x] [backend] แก้ `ApplicationDbContext.OnModelCreating` ตาม DM-15 — `DbSet<SessionQuestionReviewExclusion>`, LessonConfig index `(CompanyId, IsDelete, DeletedAt)` และ query filter `CompanyId == current && !IsDelete`; exclusion unique index `(CompanyId, SessionQuestionId)` + index `(CompanyId, LessonId)` + query filter; ห้ามเติม filter ให้ `TrainingLink`/`LearningSession`/`SessionQuestion`
- [x] [backend] สร้าง EF Core migration `AddLessonTrashLifecycle` (MG-L1) — additive columns/index/table ตาม DM-2/DM-18 เท่านั้น, `Down()` drop schema; ก่อน generate/apply เขียนและรัน preflight LT-24 ยืนยันว่าไม่มี `LessonConfig.IsDelete=true`, ถ้าพบต้องหยุดและรายงานรายแถวโดยไม่สร้าง purge job ย้อนหลัง
- [x] [backend] เพิ่ม repository methods ตาม DM-16 สำหรับ lesson trash/purge: `GetTrash(companyId)`, `GetForTrashAction(companyId, id)`, conditional archive/restore/claim, `GetPurgeDependencies(companyId, lessonId)` และ hard-finalization; ทุก method ที่ใช้ `IgnoreQueryFilters()` ต้อง re-apply `CompanyId` ใน query/update เดียวกันตาม LT-23
- [x] [backend] สร้าง repository สำหรับ `SessionQuestionReviewExclusion` — batch insert แบบ idempotent จาก snapshot question ids, query exclusion ids สำหรับ queue, และ registration ใน UnitOfWork; unique index ต้องทำให้ retry สร้างซ้ำเป็น no-op
- [x] [backend] implement archive service ตาม LT-1..LT-3 — `owner`/`admin` + selected company context เท่านั้น, transaction เดียวสร้าง `lesson_purge` job (`NextAttemptAt = now + 60 days`) แล้ว mark LessonConfig trash และ revoke TrainingLink ทุกใบ; ไม่แตะ document/Q&A/vector/narration/session/question ในขั้นนี้ และเรียกซ้ำต้องไม่สร้าง job ซ้ำ
- [x] [backend] implement restore service ตาม LT-1/LT-2/LT-4/LT-21 — conditional เฉพาะ trash ที่ `PurgeStartedAt=null`, clear deletion/purge fields, cancel เฉพาะ job id ที่ตรง และคง TrainingLink ทุกใบ revoked; worker claim แล้วต้องตอบ 409, ไม่ re-index และไม่ restore link
- [x] [backend] เพิ่ม endpoints/DTO/ViewModel camelCase ตาม LT-9/LT-22: `GET /api/lessons/trash`, `POST /api/lessons/{id}/trash`, `POST /api/lessons/{id}/restore`, `POST /api/lessons/{id}/permanent-delete` body `{ confirmationTitle }`; destructive actionsใช้ id ไม่ใช้ slug และ `cs` ต้อง 403 server-side ทุกเส้น
- [x] [backend] implement trash-list projection ตาม LT-7/LT-9 — normal list/get/save ไม่เห็น trashed lesson; trash endpoint คืน `deletedAt`, `scheduledPurgeAt`, `remainingDays`, `urgency`, `purgeState` และข้อความ/สถานะ purging ที่ไม่มี action; time calculation ใช้ UTC และ threshold >14d neutral, >7d yellow, >24h red, ≤24h red-with-today
- [x] [backend] implement manual permanent-delete ตาม LT-2/LT-10 — owner only, server trim + ordinal-exact compare `confirmationTitle` กับ `lesson.Title.Trim()`; ถูกต้องแล้วเร่ง job เดิมเป็น `NextAttemptAt=now` และตอบ 202, ห้าม delete inline หรือสร้าง job ใบที่สอง; active session ยังคงรอ worker ตาม LT-12
- [x] [backend] amend public learner authorization ตาม LT-5/LT-6 — new join/restart ต้องปฏิเสธ revoked `TrainingLink`; resume/progress/question/TTS/content/PDF page ต้องรับ `learnerKey` และ allow trashed lesson ได้เฉพาะ token+learnerKey ที่ผูก session เดิมของ link และ `IN_PROGRESS`; ห้ามใช้ raw `GetByToken` เป็น authorization หรือเปิดทาง token อย่างเดียว
- [x] [backend] amend active Q&A review queue ตาม LT-8/LT-16 — derive candidates ผ่าน session/link/lesson แล้วซ่อน lesson ที่ trash; restore กลับตาม QQ-1 โดยไม่เขียน tombstone; ก่อนตรวจ `KnowledgeQnASource` ต้องตัด `SessionQuestionReviewExclusion` เพื่อ permanent suppress หลัง purge
- [x] [backend] extend durable worker/processor สำหรับ `lesson_purge` ตาม LT-11..LT-14 — resolve `job.CompanyId` ก่อนทุก query, stale/missing/restored/generation-mismatch job no-op succeeded; ถ้ายังมี `LearningSession.IN_PROGRESS` ให้เลื่อน 1 ชั่วโมงโดยไม่ claim; claim ด้วย conditional update ตั้ง `PurgeStartedAt`; retry 3 ครั้งแรกตาม DI-9 แล้วคง pending/retry ทุก 24 ชั่วโมงไม่มีกำหนด (ไม่มี email/notification) และ requeue running job เดิมหลัง restart
- [x] [backend] implement purge dependency snapshot + permanent question exclusions ตาม LT-15/LT-16/LT-20 — snapshot links/sessions/questions, narration, `LessonExcludedSlide`, lesson-scope document/chunks, Q&A/source/conflict และ primary PDF จาก DB ภายใต้ company เดียว; insert exclusion ของทุก question ก่อนลบ Q&A/source; ห้ามวนเรียก `IKnowledgeQnAService.DeleteAsync`
- [x] [backend] implement external-delete/finalization ordering ตาม LT-17..LT-19 — delete lesson namespace ก่อน, delete document/Q&A vectors ด้วย stored namespace keys, then storage bytes; preserve shared `PdfDocumentResourceId` ถ้ายังมี lesson อื่นในบริษัทอ้าง; external missing resource = success; DB transaction สุดท้าย hard-delete narration/exclusions/page exclusions/Q&A/source/conflict/documents-chunks ที่ไม่ preserve/LessonConfig เท่านั้น และ retain TrainingLink/session/question/exclusion/job history
- [x] [backend] amend history/report projections ตาม LT-19 — rows ที่ยังอ้าง lesson hard-deleted แสดง fallback “บทเรียนที่ถูกลบ” โดยไม่ dereference parent ที่ไม่มีแล้ว
- [x] [backend] unit/integration tests — role matrix + archive/restore state machine: `cs` 403, admin/owner archive/restore, only owner manual delete, conditional restore loses to claimed purge, link remains revoked, and repeated actions/job generation are idempotent
- [ ] [backend] unit/integration tests — tenant isolation LT-23: company A ใช้ lesson/link/job id ของ B กับ every trash/restore/manual-purge/purge-repository path ได้ not found/forbidden และข้อมูล B ไม่เปลี่ยน; cover every `IgnoreQueryFilters()` path explicitly
- [x] [backend] unit/integration tests — public policy LT-5/LT-6: new join/restart revoked link fails; token-only content/PDF/resume fails; matching token+learnerKey+IN_PROGRESS continues; wrong learnerKey/ENDED session cannot read/start asking
- [x] [backend] unit tests — worker timing/reliability: 60-day schedule, active session reschedules hourly without `PurgeStartedAt`, conditional claim race, stale generation no-op, post-third-failure daily retry, idempotent external missing deletes, and shared-PDF guard preserves every resource/vector/byte when another lesson references it
- [ ] [backend] unit/integration tests — queue/purge data: trash hides questions then restore returns eligible ones; purge exclusion suppresses permanently before Q&A-source test; finalization retains TrainingLink/session/question/exclusion/job history while removing every scoped dependent including `LessonExcludedSlide`
- [x] [backend] update `frontend/docs/API_CONTRACT.md` and relevant backend workflow/API documentation for four lesson lifecycle endpoints, trash view model, learnerKey additions, role matrix, and irreversible-purge/retry semantics
- [x] [frontend] extend `src/types/domain.ts` and `src/lib/api-client.ts` in lockstep — trash lesson/list types (`deletedAt`, `scheduledPurgeAt`, `remainingDays`, `urgency`, `purgeState`), `archiveLesson`, `restoreLesson`, `requestLessonPermanentDelete`, plus required `learnerKey` on all affected public content/progress/question/TTS/PDF calls; no new endpoint beyond LT-22
- [x] [frontend] amend lesson-management page with an active/trash tab under the existing lesson screen (LT-7) — normal tab never renders trashed lessons; trash tab is read-only and renders no edit/upload/move/link action, no document/file table and no bulk action
- [x] [frontend] implement trash-row countdown/purge-state UI (LT-9) — show exact remaining status only in trash: neutral >14 days, yellow ≤14/>7, red ≤7, special “จะถูกลบถาวรภายในวันนี้” at ≤24h; `purging` shows “กำลังลบถาวร” and all actions disabled; no email/notification UI
- [x] [frontend] implement archive/restore controls according to LT-2/LT-3/LT-4 — show only to owner/admin, require refresh of active/trash lists after success, and never offer link restoration or edit controls for a trashed lesson; frontend visibility is secondary to server authorization
- [x] [frontend] implement owner-only permanent-delete confirmation dialog (LT-2/LT-10) — require exact lesson title input, submit `{ confirmationTitle }`, handle 202 as queued/not immediate deletion, and keep action absent for admin/cs; active-session delay/purging state must not imply completion
- [x] [frontend] amend public learner client callers/UI for required `learnerKey` wire contract — preserve an already running session’s content/progress/question/TTS/PDF flow, but present server rejection for revoked link new join/restart without exposing trashed lesson metadata
- [x] [frontend] frontend tests — countdown threshold mapping/boundaries and role/action visibility; confirm trash view has no edit/upload/delete-file/move/bulk controls and permanent-delete dialog sends title confirmation only for owner

## Phase 13: Create-lesson commit modal & confirm-dialog replacements (R10) — Module M

**เพิ่ม 2026-08-26 (CR-5)** — frontend ล้วน ไม่มี migration · ไม่มีฟิลด์ใหม่ · ไม่มี entity ใหม่ ·
ไม่มี endpoint ใหม่ · **ไม่มีงาน `[backend]` แม้ task เดียว**

**ขึ้นกับ:** Phase 10 (Module J — `PdfLessonContentPhase.tsx`/`commit()`/`flushNarrations()`/
`handleRetryFailedNarrations()` ที่ NR-20..NR-24 แก้อยู่คือโค้ดของเฟสนี้ที่ส่งมอบแล้ว), Phase 11
(Module K — `excludedSlideObjectIds` ที่ปุ่มลองซ้ำขั้น 3 ต้องส่งไปด้วย implement แล้ว), Phase 12
(Module L — **เฉพาะจุดที่ 5 ของตาราง CD-5**, `handleArchiveLesson` เป็นโค้ดของ Module L) — ทั้งสาม
มีโค้ดอยู่แล้ว

**⛔ ห้ามทำขนานกับ Phase 12 (Module L) — ข้อบังคับ ไม่ใช่คำแนะนำ (R-35)**: `plan.md` Phase 12
ยังไม่มี checkbox ใดถูก `qa-engineer` ติ๊กเลย แต่โค้ดของ Module L มีอยู่จริงแล้วบางส่วน
(`LessonTrashList.tsx`, `LessonPermanentDeleteDialog.tsx`, `handleArchiveLesson` ใน
`admin/lessons/page.tsx`) — งาน implement แล้วแต่ยังไม่ผ่าน QA formal อย่างเป็นทางการ · จุดที่ 5
ของ CD-5 (แทนที่ `window.confirm` ใน `handleArchiveLesson`) แก้ไฟล์เดียวกันบริเวณเดียวกันกับที่
Module L เพิ่ง implement — ทำขนานกันฝ่ายหนึ่งจะเขียนทับอีกฝ่ายโดย typecheck/build ผ่านหมด (เหตุผล
เดียวกับ R-24/Phase 9↔10 คำต่อคำ) **ห้าม dispatch งานตัวที่ 5 ในกลุ่ม CD ของ Phase นี้ (ซึ่งแก้
`handleArchiveLesson`) จนกว่า Phase 12 จะปิดรอบ QA (checkbox ติ๊กครบ)** — งานอื่นทั้งหมดของ Phase 13
(กลุ่ม NR-20..NR-24 และอีก 4 จุดของ CD ที่ไม่แตะ `admin/lessons/page.tsx`'s `handleArchiveLesson`)
ไม่ติดเงื่อนไขนี้ ทำได้ก่อน — แต่ 3 จุดจาก 5 ของ CD (จุด 3/`handleResetDemoData`, จุด 4/
`handleDeleteParent`, จุด 5/`handleArchiveLesson`) อยู่ใน **ไฟล์เดียวกัน** (`admin/lessons/page.tsx`)
ดังนั้นเพื่อไม่ให้เกิด merge conflict ระหว่างงานคนละ task ในไฟล์เดียวกัน แนะนำมอบหมายจุด 3/4 ให้เสร็จ
และ merge ก่อน แล้วค่อยเพิ่มจุด 5 ทีหลังเมื่อ Phase 12 ปิด QA (ไม่ใช่ hard blocker แบบ Phase 12 แต่
เป็นการลดความเสี่ยง merge เปล่าๆ)

**ไม่ติด 🔒 Security gate** — ตาม `design.md` §Module M งานทั้งชุดไม่แตะ endpoint, ไม่แตะสิทธิ์,
ไม่แตะ input ที่ผู้ใช้ส่งเข้าระบบ, ไม่แตะ preview session/company isolation ของ Module J และ R10.9
บังคับให้เงื่อนไข/สิทธิ์/ผลลัพธ์ของทั้ง 5 จุด CD คงเดิมทุกตัวอักษร — แต่ gate ของ Phase 10/Phase 12
ไม่ถูกยกเลิกหรือแทนที่ด้วยข้อนี้ ทั้งสองยังต้องถูก `security` audit ตามเดิม (แยกจากงานนี้)
**เงื่อนไขที่ `qa-engineer` เพิ่ม gate เองได้ตาม `conventions.md` §4 ถ้าพบระหว่างตรวจ**:
implementation ไปแตะสิทธิ์/บทบาทของจุดใดใน 5 จุด CD · กล่องยืนยันเริ่มแสดงข้อมูลที่มาจาก server ที่
จุดนั้นไม่เคยแสดง · หรือมีการเพิ่ม/ลดขั้นยืนยันจากที่ R10.9 กำหนด

**R-34 (แจ้งเตือน ไม่ใช่ task)**: Phase 10 ปิดรอบ FULL แล้วแต่ยังไม่ deploy — งานของ Phase 13 แก้
โค้ดในเฟสนั้นต่อ (`PdfLessonContentPhase.tsx`) ทำให้ file manifest ของรอบ FULL ก่อนหน้าไม่ตรงอีก
ต่อไป ไฟล์นี้ต้องกลับเข้า watchlist ของรอบ QA ถัดไป — แนะนำให้ `devops` deploy Phase 10 พร้อมกับ
หรือหลัง Phase 13 ไม่ใช่ก่อน

**O-19/R-36 (นอกขอบเขต Phase 13 — บันทึกไว้กันสับสน)**: อาการ "ไม่พบบทเรียนนี้ค่ะ" จะหยุดปรากฏใน
flow สร้างบทเรียนหลังรอบนี้ **โดยไม่มีใครแก้ต้นเหตุ** — ห้ามถือว่า Phase 13 แก้บั๊กนี้แล้ว เป็นรายการ
ตรวจแยกของ `qa-engineer` หลัง Phase 13 ลงตัว (เส้นทางที่เกี่ยว: `admin/lessons/[slug]/page.tsx` กับ
การ encode slug ภาษาไทย — Phase 13 ไม่แตะทั้งสองอย่าง)

### กลุ่ม A — modal ยืนยันการสร้างบทเรียน PDF (NR-20..NR-24, `PdfLessonContentPhase.tsx`)

- [x] [frontend] implement NR-20 — เปลี่ยนตัวแปรที่คุม `open` ของกล่องเป็น state เฉพาะของกล่องเอง
  (เช่น `commitModalOpen`) ที่ถูกเซ็ต `true` พร้อมกับการเริ่ม `commit()` — **ห้ามคำนวณ `open` จาก
  `commitStarted`/`lessonId`/`committing` เด็ดขาด** (R-33: `commitStarted` คือ `lessonId !== null`
  ซึ่งล้มขั้น 1 ไม่มีวันเป็นจริง กล่องจะไม่เปิดเลยตอนล้มขั้น 1 ถ้าผูกผิดตัวแปร) — ไม่มี code path ใด
  เซ็ต `open` กลับเป็น `false` นอกจากปุ่มที่ NR-24 อนุญาต; สามสถานะเท่านั้น: `running`/`succeeded`/`failed`
  ตามนิยามใน NR-20 (ไม่มีสถานะที่สี่ "ปิดไปเฉยๆ ทั้งที่ยังมีงานค้าง")
- [x] [frontend] implement NR-21(ก) — ลบ `router.push` ที่ถูกยิงจากผลของ API ใดๆ ในเส้นทางนี้ทั้งหมด
  ที่ **`flushNarrations()` (จุดเรียกที่ 1: จาก `commit()`)** — การเปลี่ยนหน้าต้องเกิดได้ทางเดียวคือ
  event คลิกของ CS บนปุ่มยืนยันในสถานะ `succeeded` (ตาม NR-24) ไม่ใช่ผลข้างเคียงของการ flush สำเร็จ
- [x] [frontend] implement NR-21(ก) จุดเรียกที่ 2 — แก้ **`handleRetryFailedNarrations()`** ให้ไม่ยิง
  `router.push` อัตโนมัติเช่นกัน (เรียก `flushNarrations()` เดียวกัน ต้องแก้คู่กับจุดเรียกที่ 1 ไม่งั้น
  การลองซ้ำที่สำเร็จจะยังพา CS ออกจากหน้าไปเองโดยไม่มี error/log — R-32) — **เขียนเป็น task แยกจาก
  จุดเรียกที่ 1 โดยเจตนา เพื่อให้ตรวจแยกกันได้ว่าทั้งสองจุดถูกแก้จริง**
- [x] [frontend] implement NR-21(ข) — เปลี่ยนปลายทางของ `router.push` (ที่ย้ายมาอยู่ในปุ่มยืนยันของ
  สถานะ `succeeded` แล้วจากสองงานข้างบน) เป็น **`/admin/lessons`** ไม่ใช่ `/admin/lessons/{slug}` —
  คง `lessonSlug`/`lessonSlugRef` ไว้ (ยังใช้กับลิงก์ทางออกของ NR-13/NR-24 ที่ยังเป็น
  `/admin/lessons/{slug}/narrations`)
- [x] [frontend] implement NR-22 — สร้าง checklist 4 รายการในกล่อง: (1) สร้างบทเรียน (2) อัปโหลดไฟล์
  PDF (3) ผูกไฟล์กับบทเรียน (4) รายการรวมของบทพูด ("สำเร็จ k / N หน้า") — สถานะต่อรายการ 4 แบบ (รอ/
  กำลังทำ/สำเร็จ/ล้มเหลว), **ห้ามติ๊กก่อน API ตอบสำเร็จ**, คำนวณจาก state/ref ชุดเดียวที่มีอยู่แล้ว
  (`lessonId`, `documentId`, `documentLinked`, `narrationResults`, `stepError`) **ห้ามสร้างตัวนับที่สอง
  ขนานกับ `completedSteps`/`totalSteps` เดิม** ซึ่งยังต้องคงอยู่คู่กัน (checklist เป็นของที่เพิ่ม ไม่ใช่
  ของที่มาแทน) — ไอคอนจาก `lucide-react` + semantic token ของ shadcn เท่านั้น ห้ามเพิ่ม dependency ใหม่
- [x] [frontend] implement NR-23 — ล็อกกล่องสามชั้น: (ก) `onOpenChange` ปฏิเสธการปิดที่ไม่ได้มาจากปุ่ม
  ใน NR-24 โดยลอกรูปที่มีอยู่แล้วจาก `LessonPermanentDeleteDialog.tsx:70`
  (`onOpenChange={(next) => !next && !submitting && onClose()}`) — ห้ามคิดกลไกใหม่ (ข) ส่ง
  `showCloseButton={false}` อย่างชัดเจนให้ `DialogContent` (default คือ `true`) (ค) ทุกทางออกที่ CS
  มองเห็นต้องเป็นปุ่ม/ลิงก์ในกล่องเท่านั้นตาม NR-24 — ครอบทุกสถานะ (`running`/`succeeded`/`failed`)
  ไม่ใช่เฉพาะ `running`; Esc และคลิกนอกกล่องปิดไม่ได้เลยไม่ว่าสถานะไหน
- [x] [frontend] implement NR-24 — ตารางปุ่มต่อสถานะ ตายตัว: `running` → ไม่มีปุ่มใดเลย (ห้ามมี X/
  ยกเลิก/ปิด) · `succeeded` → ปุ่มยืนยันปุ่มเดียวที่พาไป `/admin/lessons` ตาม NR-21(ข) ห้ามมีทางปิดอื่น
  · `failed` ขั้น 1 → "ลองอีกครั้ง" (เรียก `commit()` ซ้ำ) + "กลับไปแก้ข้อมูลบทเรียน" (ปิดกล่องคืน CS
  สู่เฟสเดิมโดย draft/ไฟล์/รายการหน้าที่ตัดต้องยังอยู่ครบตาม NR-13) — ห้ามมีลิงก์ไปหน้าแก้บทพูดในสถานะ
  นี้ (`lessonSlug` เป็น `null`) · `failed` ขั้น 2 → "ลองอัปโหลดไฟล์อีกครั้ง" + ลิงก์ทางออก · `failed`
  ขั้น 3 → "ลองอีกครั้ง" (ต้องส่ง `excludedSlideObjectIds` ไปด้วยทุกครั้งตาม NR-13) + ลิงก์ทางออก ·
  `failed` ขั้น 4 → "ลองใหม่เฉพาะหน้าที่เหลือ" + ลิงก์ทางออก · ลิงก์ทางออกทุกกรณีคือ
  `/admin/lessons/{slug}/narrations` เหมือนเดิมทุกตัวอักษร เป็นการ **navigate ออกไป ไม่ใช่การ dismiss
  กล่อง** · ข้อความบอกสถานะของแต่ละความล้มเหลวคงเดิมทุกตัวอักษรตาม NR-13

### กลุ่ม B — แทนที่ `window.confirm` ด้วย `AlertDialog` ของระบบ (CD-1..CD-10, 5 จุด)

- [x] [frontend] วางสถาปัตยกรรมร่วมของทั้ง 5 จุด (CD-2/CD-3/CD-4) — เลือกระหว่างสกัดเป็นคอมโพเนนต์ร่วม
  (เช่น `ConfirmDialog` ใน `components/shared/` หรือ `components/admin/` **ห้ามวางใน
  `components/ui/`**) กับทำ inline รายจุด ก่อนเริ่มแก้ทั้ง 5 จุด — ใช้ `AlertDialog` ที่โปรเจกต์มีอยู่
  แล้วเท่านั้น (`ui/alert-dialog.tsx`) **ห้ามติดตั้ง primitive ใหม่จาก shadcn CLI และห้ามใช้ pattern
  ของ `LessonPermanentDeleteDialog` (พิมพ์ยืนยัน) กับจุดใดใน 5 จุดนี้** (ทั้ง 5 เป็น yes/no ล้วน) —
  ถ้าสกัดเป็นตัวร่วม ต้องรับเฉพาะ title/description/ปุ่มยืนยัน (label+variant)/`onConfirm`/`onCancel`
  ห้ามมี business logic ห้ามรู้จัก entity ใด ห้ามยิง API เอง (CD-3(ข)) — แต่ละจุดต้องแยกฟังก์ชันเดิม
  ออกเป็นตัวเปิดกล่อง (เก็บ payload ลง state) กับตัวทำงานจริงที่ถูกเรียกจากปุ่มยืนยัน **ห้ามห่อกล่องด้วย
  `Promise`/`resolve` ใน `ref`** (CD-4) — payload ที่เก็บลง state ต้องเป็น id/object ที่กดมา ไม่ใช่
  closure ของ handler
- [x] [frontend] CD-5 จุดที่ 1 — `components/admin/CategoryFormDialog.tsx:130` แทนที่
  `window.confirm` ของการลบหมวดหมู่ย่อยด้วย `AlertDialog` inline — หัวเรื่อง "ลบหมวดหมู่ย่อย",
  ข้อความ `ต้องการลบหมวดหมู่ย่อย "{row.category.name}" ใช่หรือไม่?` (คำต่อคำ ห้ามแก้แม้ช่องว่างเดียว),
  ปุ่มยืนยัน "ลบหมวดหมู่ย่อย" (`variant="destructive"`), `AlertDialogCancel` ค่า default — **เป็นกล่อง
  ซ้อนกล่อง** (`CategoryFormDialog` เป็น dialog ของฟอร์มหมวดอยู่แล้ว) กดยกเลิกหรือยืนยันจนจบต้องกลับมา
  ที่ฟอร์มเดิมโดยฟอร์มไม่ถูกรีเซ็ตและ dialog แม่ไม่ปิด (CD-7) — สิทธิ์/เงื่อนไข/เส้นทาง error หลัง API
  ล้มเหลวต้องไปที่เดิมทุกอย่าง (CD-8)
- [x] [frontend] CD-5 จุดที่ 2 — `components/admin/DocumentUploadList.tsx:244` แทนที่
  `window.confirm` ของการลบเอกสารด้วย `AlertDialog` inline — หัวเรื่อง "ลบเอกสาร", ข้อความ
  `ต้องการลบเอกสารนี้ใช่หรือไม่?` (คำต่อคำ), ปุ่มยืนยัน "ลบเอกสาร" (`variant="destructive"`) —
  **ห้ามแตะ exports `statusLabels`/`failureReasonLabels`/`scopeLabel`/`statusVariant` ของไฟล์นี้เลย**
  (ใช้ร่วมโดย `DocumentLibraryFilterBar.tsx`/`KnowledgeQnATable.tsx`/`KnowledgeQnAAnswerDialog.tsx` —
  CD-10, เข้า shared-code watchlist ของ `qa-engineer` รอบนี้ด้วย) — error หลัง API ล้มเหลวไปที่เดิม
  (CD-8)
- [x] [frontend] CD-5 จุดที่ 3 — `app/admin/lessons/page.tsx:94` (`handleResetDemoData`) แทนที่
  `window.confirm` ด้วย `AlertDialog` — หัวเรื่อง "รีเซ็ตข้อมูล Demo", ข้อความ
  `ต้องการรีเซ็ตข้อมูล Demo ทั้งหมดกลับเป็นค่าเริ่มต้นใช่หรือไม่?` (คำต่อคำ), ปุ่มยืนยัน "รีเซ็ตข้อมูล"
  (`variant="destructive"`)
- [x] [frontend] CD-5 จุดที่ 4 — `app/admin/lessons/page.tsx:116` (`handleDeleteParent`) แทนที่
  `window.confirm` ด้วย `AlertDialog` — หัวเรื่อง "ลบหมวดหมู่", ข้อความ
  `ต้องการลบหมวดหมู่ "{parent.name}" ใช่หรือไม่?` (คำต่อคำ), ปุ่มยืนยัน "ลบหมวดหมู่"
  (`variant="destructive"`) — error หลัง API ล้มเหลวไปที่เดิม (`admin/lessons/page.tsx:122-125`
  ตาม CD-8)
- [x] [frontend] CD-5 จุดที่ 5 — `app/admin/lessons/page.tsx:144` (`handleArchiveLesson`, **โค้ดของ
  Module L — ⛔ ห้าม dispatch จนกว่า Phase 12 จะปิดรอบ QA เต็ม checkbox**) แทนที่ `window.confirm`
  ด้วย `AlertDialog` — หัวเรื่อง "ย้ายบทเรียนไปถังขยะ", ข้อความ
  `ต้องการย้ายบทเรียน "{lesson.title}" ไปถังขยะใช่หรือไม่? ลิงก์การสอนทั้งหมดของบทเรียนนี้จะถูกยกเลิก
  ทันที` (คำต่อคำ), ปุ่มยืนยัน "ย้ายไปถังขยะ" (`variant="destructive"`) — **ต้องคง `setBusyLessonId`
  และ `setTrashRefreshToken` ครบตาม LT-3** ไม่งั้นรายการถังขยะไม่รีเฟรช (CD-8)
- [x] [frontend] ยืนยันหลังแก้ครบ 5 จุด — ไม่มี `window.confirm`/`window.alert`/`window.prompt`
  หลงเหลืออยู่ในโค้ดที่แตะ และไม่มีกล่องที่ถูกแตะนอกเหนือ 5 จุดนี้ (CD-1) — **ห้ามแตะ
  `LessonPermanentDeleteDialog`, `lesson-editor-pdf-replace-dialog` (NR-3), `pdf-content-phase-warning-dialog`
  (NR-15), `CategoryMovePreviewDialog` ในรอบนี้เลย** (CD-9) — เจอ `window.confirm`/`alert`/`prompt`
  จุดที่หกระหว่างทำงาน **ให้หยุดแล้วตีกลับไปที่ `system-analyst` ไม่ใช่เติมเอง**

## Sequencing Notes

- **Local/rehearsal baseline ของ `learning-session` ต้องถึง `20260818155126_AddTotalSlideCount` ก่อน
  generate/test Phase 1** — local Compose ปัจจุบันผ่านแล้ว จึงไม่ต้องรอ shared/production deployment;
  แต่ก่อน shared/production migration จริง `devops` ต้องยืนยันลำดับ migration และ backup ที่กู้คืนได้
- Phase 1 → Phase 2 → Phase 3 → (Phase 4, 5, 6 ขนานกันได้) ตรงตาม dependency chain ของ
  `design.md` §Modules คำต่อคำ — Phase 1 เป็น breaking migration ที่ทุก phase อื่นพึ่งพา
  ต้องเสร็จเป็นก้อนเดียว ห้ามแบ่งครึ่ง (มิฉะนั้น codebase จะมีทั้ง `LessonId` เก่าและ `ScopeType`
  ใหม่ปนกัน — เหตุผลเดียวกับที่ `learning-session` Module A ห้ามแบ่งครึ่ง)
- **Default-chain invariant ของ Phase 1:** `IsSystemDefault` เป็น flag ของ chain จึงมี exactly
  2 แถวต่อบริษัท (parent Level 1 + assignable leaf Level 2) ไม่ใช่ unique-row flag; ทุกจุดที่หา
  default เพื่อ assign ต้องเรียก `GetSystemDefault()` ซึ่ง filter `IsSystemDefault && Level == 2`
  ก่อน `SingleOrDefault()` และต้อง fail-fast หากพบ leaf ซ้ำ
- Phase 2 ต้องเสร็จก่อน Phase 3 เพื่อให้งาน `vector_delete` ของ Phase 3 ลบจาก namespace ที่ถูกต้อง
  ตั้งแต่แรก (ไม่ใช่ hard blocker ทางเทคนิค แต่ design.md แนะนำลำดับนี้ไว้ชัด)
- Phase 3 ต้องเสร็จก่อน Phase 4/5/6 เพราะทั้งสามใช้ `BackgroundJob` เป็นกลไก index: Phase 4
  อ่าน `DocumentChunk` ที่ worker ของ Phase 3 เขียน, Phase 5 enqueue `lesson_index` ผ่าน worker
  เดียวกัน, Phase 6 enqueue `qna_index` ผ่าน worker เดียวกัน
- Phase 4, 5, 6 ไม่มี dependency ระหว่างกันเอง — ทำขนานกันได้ด้วย `frontend-engineer`/
  `backend-engineer` คนละคู่พร้อมกันเมื่อ Phase 1–3 ปิดหมดแล้ว
- **Phase 1 ไม่ติด gate, Phase 2/3/4/6 ติด, Phase 5 ไม่ติด** — ตรงตาม `design.md` §Modules:
  - Phase 2 — namespace ระดับหมวดเป็น key ชนิดแรกที่ประกอบจากค่าที่ CS พิมพ์เข้ามาเอง พลาด =
    บริษัทหนึ่งได้คำตอบจากคลังของอีกบริษัทโดยไม่มี error
  - Phase 3 — `BackgroundJob` เป็นตารางเดียวที่จงใจไม่มี query filter + `DeleteVectorsAsync`
    ลบถาวร + `LastErrorDetail` ห้ามหลุดออก API
  - Phase 4 — endpoint แรกในระบบที่คืนเนื้อหาดิบของไฟล์ที่อัปโหลด
  - Phase 6 — ข้อความที่ `cs` พิมพ์ไหลเข้า prompt แล้วใช้ตอบครูทุกคนทันทีโดยไม่มีอนุมัติ (R5.7)
    ถือเป็น untrusted input ที่เข้าถึง output ของระบบโดยตรง
  - Phase 5 หมายเหตุ: ถ้าตอน implement พบว่าหน้าสร้างบทเรียนแตะ `Slug` โดยข้าม TX-7 หรือรับ
    `CategoryId` โดยไม่ตรวจว่าเป็นของบริษัทตัวเอง `qa-engineer` ติด gate เพิ่มได้ตามสิทธิ์
    add-only ใน conventions §4
- `KnowledgeQnASource`/`KnowledgeQnAConflict` (Phase 6) อ้างถึง `SessionQuestion.Id` แบบ
  logical FK ข้าม module เท่านั้น — ห้ามแก้ entity `SessionQuestion` ของ `learning-session`
  นอกจากเพิ่ม index สองตัวใน `OnModelCreating` (R-9) แจ้ง `learning-session` module ทราบตอนแก้จริง
- `SessionQuestion.SessionId` (ไม่ใช่ `LearningSessionId`) คือชื่อจริงในโค้ด — ทุก phase ที่อ้างถึง
  ต้องยึดชื่อนี้ ไม่ใช่ตามที่ `learning-session/design.md` DM-3 เขียนไว้ผิด (R-10, drift ของ
  module อื่น ไม่ใช่ของแผนนี้ — ห้ามแก้เอง รายงานให้ `qa-engineer` route)
- Test tasks ที่ระบุไว้ (Phase 1/2/3/6) ครอบเฉพาะ pure logic ที่ `design.md` R-12 ชี้ว่า test ได้
  โดยไม่ต้องพึ่ง provider จริง — ไม่ใช่ blanket coverage ทั้ง phase
- Migration ของแต่ละ phase มีชื่อและขอบเขตตายตัวตาม MG-A1..MG-F1: `AddKnowledgeTaxonomyAndScope`
  (Phase 1), `AddDurableIndexingJobs` (Phase 3), `AddDocumentChunks` (Phase 4),
  `AddLessonSlideNarrations` (Phase 5), `AddKnowledgeQnA` (Phase 6) — ห้ามรวม entity ของ phase หลัง
  เข้ากับ migration ก่อนหน้า; ก่อน apply กับ shared/production จริงยังคงเป็น DevOps hard stop และต้องมี
  backup ที่กู้คืนได้
- **Phase 7 (Module G) ขึ้นกับ Phase 1/2/3/4 — ทั้งสี่มีโค้ดอยู่แล้ว** (`KnowledgeCategory`/`ScopeType`,
  `EnsureValidScope`/`Resolve`, `BackgroundJob`/worker, `DocumentChunk`) แต่ **ไม่ขึ้นกับ Phase 5/6**
  ทำขนานกับสองตัวนั้นได้ถ้าจำเป็น — สิ่งที่บล็อกจริงคือ Phase 3 ต้องปิด issue cross-company leak/IDOR
  ที่ค้างอยู่ก่อน (ดู `_docs/status.md` §knowledge-base) เพราะ DS-6 ใช้ `BackgroundJob`/soft delete
  pattern ชุดเดียวกับที่บั๊กนั้นอยู่ — ถ้าเริ่ม Phase 7 ก่อน backend fix ของ Phase 3 ผ่าน QA
  TARGETED confirm จะแยกไม่ออกว่าบั๊กใหม่มาจากไหนถ้าเจอ cross-company leak ซ้ำระหว่างทดสอบ DS-3/DS-6 —
  **ณ วันที่วางแผนนี้ (2026-08-20) backend fix ของ Phase 3 อยู่ใน working tree แล้วแต่ยังไม่ผ่าน
  `qa-engineer` TARGETED ยืนยัน** ดังนั้น Phase 7 เขียน task ไว้พร้อมส่งได้ แต่ **ควรเริ่มลงมือจริงหลัง
  QA TARGETED ของ Phase 3 ปิดเรียบร้อยแล้ว** ไม่ใช่ขนานทันที — ตรวจ `_docs/status.md`/`review.md`
  อีกครั้งก่อนมอบหมายงานจริง
- **Phase 7 🔒 Security gate** — ครั้งแรกที่ค่า `ScopeType`/`ScopeId` ที่ CS ส่งมาจาก request กำหนด
  namespace ของ vector ฝั่งเอกสารโดยตรง (จนถึง Phase 6 ค่านี้ถูกกำหนดโดยโค้ด server ล้วน) เหตุผล
  เดียวกับ gate ของ Phase 2 คำต่อคำ — namespace key คือ isolation อย่างเดียวที่ vector store มี;
  จุดที่ต้องตรวจเป็นพิเศษ: DS-2 (เรียก `EnsureValidScope` จริงก่อนแตะ storage), DS-3 ครบ 6 กรณี
  โดยเฉพาะ `company` + `ScopeId` ต้องถูกปฏิเสธ, `ScopeId` ของอีกบริษัทต้องตกที่ 404, DS-5 (IDOR สองชั้น
  จาก id ใน path + scope ใน body พร้อมกัน), DS-6 (`VectorId` ใน `PayloadJson` ต้องมาจากเอกสารของบริษัท
  ผู้เรียกเท่านั้น — id ผิดบริษัทที่หลุดเข้าไปจะลบคลังความรู้ของบริษัทอื่นถาวร)
- Phase 7 ไม่มี migration — `DocumentResource.ScopeType`/`ScopeId` สร้างไปแล้วใน MG-A2/MG-A5 ของ
  Phase 1; ถ้าระหว่างทำ Phase 7 เกิดต้อง generate migration ใหม่แปลว่ากำลังทำเกิน contract ให้หยุด
  แล้วตีกลับ `system-analyst` ตาม `design.md` §Migration Plan
- **Phase 8 (Module H) ขึ้นกับ Phase 1, 4, 6, 7 — ทั้งสี่มีโค้ดอยู่แล้ว** (`KnowledgeCategory`/scope,
  `DocumentChunk` ที่ KL-11 ค้น, `KnowledgeQnA` ที่ KL-8 อ่าน, `ScopeType`/`ScopeId` ที่ CS ตั้งเองได้จริง
  ผ่าน Phase 7) — เริ่มได้ทันที ไม่มี blocker ที่ยังค้างเหมือน Phase 7 เคยมีกับ Phase 3
- **Phase 8 มี migration ใบเดียวคือ `MG-H1` (`AddDocumentContentHash`) และต้องอยู่เฟสเดียวกับงาน
  KL-18..KL-24 ทั้งหมด ห้ามแยกไปเฟสอื่นไม่ว่าก่อนหรือหลัง** — คอลัมน์ที่ไม่มีใครเขียนค่าลงไปจะทำให้
  เอกสารที่อัประหว่างนั้นมี `ContentHash = null` ถาวรโดยไม่มีใครตั้งใจ ซึ่งย้อนไม่ได้เพราะ design.md
  ห้าม backfill ย้อนหลัง (KL-24)
- **Phase 8 🔒 Security gate — สี่เหตุผล** ตาม `design.md` Module H (~บรรทัด 1219-1236):
  (1) `GetAllInCompany()` (KL-2/KL-9) เป็น query path ใหม่ที่ไม่มีเงื่อนไข `Where` เลย ฝาก isolation
  ไว้กับ EF global query filter ตัวเดียว — เคยพลาดมาแล้วจริงที่ `GetDeleted`/`IgnoreQueryFilters()`
  (2) `GET /api/knowledge-qna` เป็น endpoint ที่สองของระบบที่คืนเนื้อความคลังความรู้ทั้งก้อน (ตัวแรกคือ
  DI-7 ของ Phase 4) (3) การแก้ Q&A จากหน้ารวม (KL-14) ทำให้ข้อความที่ `cs` พิมพ์ถูก re-index เข้า prompt
  ได้อีกครั้งหลังบันทึกไปแล้ว — prompt injection ผ่านช่อง `Answer` โดยไม่มีขั้นอนุมัติ (เหตุผลเดียวกับ
  gate ของ Phase 6) (4) **KL-19/KL-21 (การเตือนเนื้อหาซ้ำ) เป็นช่องทางใหม่ที่ตอบว่า "ไฟล์นี้มีอยู่แล้ว
  ในระบบหรือไม่" จากข้อมูลที่ผู้เรียกป้อนเข้ามาเอง — payload 409 เปิดเผยชื่อไฟล์/ขอบเขต/วันที่ของเอกสาร
  ใบอื่น ถ้าคิวรีจับซ้ำหลุด `CompanyId` เมื่อไหร่ นี่กลายเป็น cross-tenant existence oracle ที่ถามทีละไฟล์
  ได้ว่าบริษัทอื่นมีไฟล์นี้ไหม** — ต้องมี test สองบริษัทถือไฟล์เดียวกัน (hash เท่ากันเป๊ะ) ยืนยันว่าไม่เตือน
  ข้ามกัน (เขียนไว้เป็น unit test ใน Phase 8 แล้ว)
- **Phase 9 (Module I) ขึ้นกับ Phase 8 แบบ deploy ต้องเรียงกัน ไม่ใช่แค่เขียนโค้ดเรียงกัน (R-18)** —
  ปล่อย Phase 9 ก่อน Phase 8 deploy เมื่อไหร่ CS จะไม่เหลือทางเห็นเอกสารระดับบทเรียนจากที่ไหนเลยทั้งระบบ
  เพราะการ์ดที่ถูกลบเป็นทางเดียวที่มีอยู่วันนี้ และ Phase 8 (KL-2/KL-4/KL-7) คือทางแทนที่
- Phase 9 ไม่ติด gate — เป็นการลบ UI ล้วนที่ไม่เพิ่มพื้นที่โจมตีใหม่แม้แต่จุดเดียว (ไม่มี endpoint ใหม่
  ไม่มี input ใหม่ ไม่มี query ใหม่ ไม่มีการเปลี่ยนสิทธิ์); UC-5 (ห้ามแตะทางอัป PDF ตัวสไลด์) เป็นความเสี่ยง
  เชิงฟังก์ชัน ไม่ใช่เชิงความปลอดภัย — `qa-engineer` ต้องจับข้อนี้ ไม่ใช่ `security`
- Phase 9 ไม่มี migration เลย
- **Phase 10 (Module J) ขึ้นกับ Phase 1, 3, 5, 7 — ทั้งสี่มีโค้ดอยู่แล้ว** (`KnowledgeCategory`,
  `BackgroundJob`/NR-6, endpoint บันทึกบทพูด + `ILessonSlideNarrationResolver`/NR-1..NR-2 ที่ใช้ซ้ำ
  ทั้งชุด, `ScopeType`/`ScopeId` ขาเขียน + `EnsureValidScope` ที่ NR-14 พึ่ง) — เริ่มได้ทันที ไม่ต้อง
  รออะไรเชิงฟังก์ชัน · **ไม่ขึ้นกับ Phase 8 เชิงฟังก์ชัน**
- **Phase 10 ห้ามทำขนานกับ Phase 9 (R-24)** — ทั้งสองแก้ `LessonForm.tsx` ไฟล์เดียวกันคนละบริเวณ
  (Phase 9 = ลบการ์ด "เอกสารประกอบ" + โหมด `fixedScope`; Phase 10 = รื้อ flow การสร้างบทเรียน PDF/
  `handlePdfUpload`) ทำขนานกันฝ่ายหนึ่งจะเขียนทับอีกฝ่ายโดย typecheck/build ผ่านหมด — **Phase 9
  ต้องขึ้นเป็นโค้ดจริงก่อน (merged + ผ่าน `qa-engineer`) ไม่ใช่แค่มี task list ใน `plan.md`** —
  **แก้ไข 2026-08-26 (รอบวาง Phase 10):** ตรวจ `LessonForm.tsx` จริงแล้ว การ์ด `DocumentUploadList`/
  `fixedScope` เดิมถูกแทนที่ด้วยบรรทัดสรุปอ่านอย่างเดียวแล้ว (commit `91e6d19` "Module H/I ครบ") —
  **โค้ดของ Phase 9 merge แล้ว** แต่ checkbox ของ Phase 9 ใน `plan.md` ยังไม่มีช่องใดถูก `qa-engineer`
  ติ๊กเลย → เงื่อนไข "merged" ผ่านแล้ว เงื่อนไข "ผ่าน `qa-engineer`" ยังไม่ผ่าน — **ห้าม dispatch งาน
  `[frontend]` ของ Phase 10 จนกว่า Phase 9 จะผ่าน `qa-engineer` (checkbox ติ๊กครบ)** งาน `[backend]`
  ของ Phase 10 ไม่แตะ `LessonForm.tsx` จึงไม่ติดเงื่อนไขนี้ ทำก่อนได้
- **Phase 10 🔒 Security gate — 3 เหตุผล** ตาม `design.md` §Module J: (1) endpoint ใหม่ parse/render
  ไฟล์ PDF ที่ยังไม่ persist ในโปรเซสโดยไม่ผ่าน `DocumentParserFactory` — ไฟล์พัง/ไม่ใช่ PDF ต้อง
  ตกเป็น 4xx สะอาดเสมอ (2) company isolation ของ preview session เขียนด้วยมือ (NR-11) — `IMemoryCache`
  ไม่มี query filter ช่วย เป็นของชิ้นแรกที่เก็บเนื้อหาไฟล์บริษัทหนึ่งไว้นอก PostgreSQL ชี้ด้วย id จาก
  request (3) ต้นทุนหน่วยความจำ/CPU ที่ผู้ใช้ล็อกอินแล้วสั่งได้ตรง (ไฟล์ 30MB × session ค้าง 10 นาที
  + PDFium render ทุกหน้าที่เลื่อนดู, R-22)
- Phase 10 ไม่มี migration — ทุก endpoint ใหม่ไม่แตะฐานข้อมูล (NR-10/NR-18); ถ้าระหว่างทำเกิดต้อง
  generate migration ใหม่แปลว่ากำลังทำเกิน contract ให้หยุดแล้วตีกลับ `system-analyst`
- **Phase 11 (Module K) ห้ามเริ่มก่อน Phase 10 ปิดรอบ QA แบบ FULL — เป็นเงื่อนไขเวลา ไม่ใช่แค่
  เงื่อนไขฟังก์ชัน (R-26/มติ Q-K4)** — Phase 10 วันนี้ผ่านแค่ **TARGETED 21/21** ยังไม่ FULL ยังไม่
  audit ยังไม่ deploy · Phase 11 แก้ไฟล์ชุดเดียวกันแทบทั้งหมดกับที่ Phase 10 เพิ่ง implement
  (`SlideNarrationEditorCard.tsx` · `PdfLessonContentPhase.tsx` · `narrations/page.tsx` ·
  `LessonForm.tsx` · `ILessonConfigService.cs` · `ILessonSlideNarrationService.cs` ·
  `IBackgroundJobProcessor.cs`) และ amend contract 14 ข้อที่ Phase 10 เพิ่ง implement ตาม — เริ่มก่อน
  = สถานะ verify ของ Phase 10 กำกวมถาวร แยกไม่ออกว่าอาการที่เจอเป็นของ Phase 10 หรือ 11 (R-24 ซ้ำ
  อีกรูปหนึ่งแต่คราวนี้เป็นแกนเวลา ไม่ใช่แกนไฟล์) · **เงื่อนไขเริ่ม Phase 11 ที่ต้องเป็นจริงครบทุกข้อ**:
  (1) Phase 10 ผ่าน QA แบบ FULL แล้ว (2) checkbox ของ Phase 10 ใน `plan.md` ติ๊กครบ (3) ไม่มี issue
  ของ Phase 10 ค้างใน `review.md` · **`security` ของ Phase 10 ไม่จำเป็นต้องผ่านก่อนเริ่มเขียนโค้ด
  Phase 11** (คนละชั้นกัน) **แต่ `devops` ห้ามปล่อย Phase 11 ถ้า Phase 10 ยังไม่ผ่านทั้ง FULL และ
  audit** เพราะ Phase 11 ทับโค้ดของ Phase 10 · ถ้ามีเหตุให้ต้องรีบจริง ทางที่ถูกคือเร่งปิดรอบ FULL
  ของ Phase 10 ไม่ใช่เริ่ม Phase 11 ขนานไป
- **Phase 11 🔒 Security gate — 3 เหตุผล** ตาม `design.md` §Module K: (1) `slideObjectId` จาก
  request ถูกใช้ประกอบ vector id ที่ถูกลบจริงใน Pinecone สองชุดพร้อมกัน (`pdf-page-N` และ
  `{documentId}-page-N`) — ไม่ validate เทียบชุดหน้าจริงของเด็คนั้นก่อนใช้ = ลบข้ามบทเรียนได้ทันที
  (2) `LessonId` ต้องผ่าน query filter ของบริษัทก่อนประกอบ namespace คู่กับ `slideObjectId` เสมอ
  (3) การ toggle ซ้ำๆ ต้องไม่ทำให้เกิดงานลบสะสมที่ลบของคนอื่น (idempotency ของ EX-4) — สิทธิ์
  `cs`/`admin` เท่ากับแก้บทพูดวันนี้ไม่ใช่คำตอบของข้อนี้ (แก้บทพูดผิดหน้า = ข้อความผิด, ตัดผิดหน้า =
  vector หายจากคลังถาวร)
- **`GET /api/lessons/{id}/narrations/count` เปลี่ยน response shape เป็น `{ count, excludedCount }`
  เป็น breaking wire contract ที่มี caller เดียว** (`LessonForm.tsx` โหมด `isEdit`) — backend task
  และ frontend caller/TS-type update ของ Phase 11 อยู่ phase เดียวกัน ห้ามปล่อยคร่อม (เหตุผลเดียวกับ
  R-15/EX-10)
- **MG-K1 (migration `LessonExcludedSlide`) กับ EX-1..EX-12 (contract การตัดหน้าทั้งชุด) ต้องอยู่
  phase เดียวกันเสมอ ห้ามแยก** — ตารางที่ไม่มี endpoint ใดเขียนค่าลงไปจะไม่มีความหมายอะไรเลย
  (เหตุผลเดียวกับ MG-H1/KL-18..KL-24 ที่ห้ามแยกใน Phase 8)
- **สองจุด blast-radius ที่ `design.md` ชี้ไว้ (NR-4)** — `narrations/page.tsx:143` และ
  `PdfLessonContentPhase.tsx:309` ต่างคำนวณเลขหน้าไฟล์จาก `slide.index + 1` วันนี้ (ถูกต้องเพราะ
  ยังไม่มีการเรียงเลขใหม่) — เมื่อ Phase 11 ทำให้หน้าที่ถูกตัดหายไปจาก list ของฝั่งสอนและ `Index`
  ถูกคำนวณใหม่ ทั้งสองจุดนี้จะกลายเป็นภาพสไลด์คนละหน้ากับบทพูดข้างๆ ถ้าไม่ถูกแก้ให้แปลงจาก
  `slideObjectId` แทน — ต้องแก้พร้อมกันในเฟสเดียว เขียนเป็น task แยกสองบรรทัดใน Phase 11
  ไม่ใช่ถูกกลืนเข้า "implement EX-3"
- Phase 11 ไม่มี migration ใหม่นอกจาก `AddLessonExcludedSlides` (MG-K1) เท่านั้น — additive ล้วน
  ไม่มี backfill ไม่มีคอลัมน์ใหม่บนตารางเดิม; ถ้าระหว่างทำเกิดต้อง generate migration ใบอื่นเพิ่ม
  แปลว่ากำลังทำเกิน contract ให้หยุดแล้วตีกลับ `system-analyst`
- **Phase 12 (Module L) ต้องเริ่มหลัง Phase 11 ปิด implementation** — ไม่ใช่แค่ dependency
  เชิงชื่อ: LT-15/LT-19 ต้อง snapshot และ hard-delete `LessonExcludedSlide` ด้วย หาก L ถูกส่งมอบ
  ก่อน K จะไม่มีใครรับประกันว่าลูกตารางที่ K เพิ่มจะถูกลบเมื่อ permanent purge · Phase 12 ใช้
  `BackgroundJob`/retry ของ Phase 3, stored vector metadata จาก Phase 4, narration จาก Phase 5,
  Q&A/source/conflict จาก Phase 6 และ public learner flow ที่มีอยู่จริง; ห้ามเขียน purge worker
  แบบ queue หรือ scheduler ชุดที่สอง
- **MG-L1 ต้องอยู่ Phase 12 พร้อม lifecycle ทั้งชุด ห้ามแยก** — schema อย่างเดียวไม่สร้าง trash
  ที่ restore/purge ได้ และ lifecycle ที่ไม่มี `PurgeJobId`/`PurgeStartedAt` จะกัน stale job/race
  ไม่ได้ · migration เป็น additive ไม่มี backfill; preflight LT-24 ต้องยืนยัน `LessonConfig.IsDelete`
  ที่มีอยู่เป็น 0 ก่อน apply และถ้าไม่ใช่ 0 ต้องหยุดตรวจ, ไม่เดาและไม่สร้าง job ย้อนหลัง
- **Phase 12 🔒 Security gate — six concerns**: owner-only hard delete/selected company context;
  `IgnoreQueryFilters()` trash/purge predicates; revoked token must bind `learnerKey` + `IN_PROGRESS`;
  stale-job/restore race; dependency ids from DB; and destructive Pinecone/object-storage deletes with
  shared-primary-PDF protection. `security` must audit these after QA, before devops ships the phase.
- **R9 retention is fixed at 60 days in this phase** — O-18 explicitly defers per-company retention;
  do not add `Company` field, settings UI, DTO, endpoint, validation range, snapshot/recalculation rule,
  email, notification or cleanup/backfill as a “preparation” task.
- **Phase 13 (Module M) ห้ามทำขนานกับ Phase 12 (Module L) — R-35** · จุดที่ 5 ของ CD-5 (แทนที่
  `window.confirm` ใน `handleArchiveLesson`) แก้ไฟล์/บริเวณเดียวกับที่ Module L implement ไปแล้วแต่
  ยังไม่ผ่าน QA (`plan.md` Phase 12 ยัง `[ ]` ทุกช่อง) — **ห้าม dispatch task ของจุดที่ 5 จนกว่า
  Phase 12 จะปิดรอบ QA (checkbox ติ๊กครบ)**; งานอื่นทั้งหมดของ Phase 13 (กลุ่ม NR-20..NR-24 และ CD
  จุดที่ 1/2/3/4) ไม่ติดเงื่อนไขนี้ ทำได้ก่อน
- **Phase 13 ขึ้นกับ Phase 10 (Module J) และ Phase 11 (Module K) เชิงโค้ด — ทั้งสองมีโค้ดอยู่แล้ว**
  (`PdfLessonContentPhase.tsx`/`commit()`/`flushNarrations()`/`handleRetryFailedNarrations()` จาก
  Phase 10, `excludedSlideObjectIds` ที่ปุ่มลองซ้ำขั้น 3 ต้องส่งจาก Phase 11) — ไม่มีเงื่อนไขเวลาแบบ
  Phase 10↔11 (ทั้งสองปิดแล้ว) แต่ **R-34**: Phase 13 แก้โค้ดในไฟล์ที่ Phase 10 เพิ่งปิดรอบ FULL
  (Round 8) ทำให้ file manifest ของรอบนั้นไม่ตรงอีกต่อไป — `PdfLessonContentPhase.tsx` กลับเข้า
  watchlist ของรอบ QA ถัดไป; แนะนำให้ `devops` deploy Phase 10 พร้อมกับหรือหลัง Phase 13 ไม่ใช่ก่อน
- **NR-21 ของ Phase 13 ต้องแก้สองจุดเรียกแยกกัน ไม่ใช่จุดเดียว (R-32)** — `router.push` อยู่ใน
  `flushNarrations()` ซึ่งถูกเรียกทั้งจาก `commit()` และจาก `handleRetryFailedNarrations()`; แก้แค่จุด
  แรกจะทำให้การลองซ้ำที่สำเร็จยังพา CS ออกจากหน้าไปเองโดยไม่มี error — เขียนไว้เป็น task แยกสองบรรทัด
  ใน Phase 13 โดยเจตนา
- **Phase 13 ไม่ติด 🔒 Security gate ตาม `design.md` §Module M** แต่ `qa-engineer` เพิ่มได้เองถ้าพบ
  ระหว่างตรวจว่า implementation แตะสิทธิ์/บทบาทของจุดใดใน 5 จุด CD, กล่องเริ่มแสดงข้อมูลจาก server ที่
  ไม่เคยแสดงมาก่อน, หรือมีการเพิ่ม/ลดขั้นยืนยันจากที่ R10.9 กำหนด — gate ของ Phase 10/12 เองไม่ถูก
  ยกเลิกหรือแทนที่ด้วย Phase 13
- **O-19/R-36 — Phase 13 ไม่แก้อาการ "ไม่พบบทเรียนนี้ค่ะ"** แม้ NR-21(ข) จะเปลี่ยนปลายทางไปที่
  `/admin/lessons` ซึ่งเป็นหน้าที่อาการเคยเกิด — ห้ามถือว่ารอบนี้แก้บั๊กนั้นแล้ว เป็นรายการตรวจแยกของ
  `qa-engineer` หลัง Phase 13 ลงตัว

## Unresolved Open Questions

ไม่มีคำถามที่บล็อกการเริ่ม Phase 1 — `design.md` ปิดครบ Q1–Q6 แล้วเมื่อ 2026-08-19 หัวข้อ
"🟡 ค้างไว้โดยตั้งใจ" (O-1..O-7) ใน `design.md` ไม่บล็อกงานใด ๆ ในแผนนี้ เพราะทุกจุดมี default
ที่ระบุไว้แล้วให้ implement ตามนั้น (เช่น O-1 → QQ-9 default "ทุกคนแก้/ลบของกันได้")

`Q-J1`/`Q-J2` ของ Module J (Phase 10) เคาะครบแล้วเมื่อ 2026-08-26 (`Q-J1` = "ไม่ขัด — แตะชั่วคราวได้",
`Q-J2` = "ใส่ทั้งสองที่") — ไม่มีคำถามค้างที่บล็อก Phase 10 เช่นกัน

`Q-K1`..`Q-K4` ของ Module K (Phase 11, CR-3) เคาะครบแล้วเมื่อ 2026-08-26 เช่นกัน (`Q-K1` = ตารางแยก
`LessonExcludedSlide`, `Q-K3` = หน้าแก้บทพูดแสดงทุกหน้ารวมหน้าที่ถูกตัด, `Q-K4` = ห้ามเริ่มก่อน
Module J ปิด FULL) — สิ่งเดียวที่ไม่บล็อกการเขียน task แต่บล็อกการ **dispatch** คือเงื่อนไขเวลา
ของ R-26 ที่เขียนไว้ใน `## Sequencing Notes` ข้างต้น ไม่ใช่คำถามที่ยังรอคำตอบ

`Q-L1`..`Q-L5` ของ Module L (Phase 12, CR-4) เคาะครบแล้วเมื่อ 2026-08-26 — existing session
เรียนต่อได้แต่ new join/restart ไม่ได้, queue ซ่อนขณะ trash/กลับเมื่อ restore, shared primary PDF
preserve จนไม่มี lesson อื่นอ้าง, active session เลื่อน purge ตรวจใหม่ทุกชั่วโมง และไม่มี email.
ไม่มีคำถามค้างที่บล็อกการเขียน Phase 12; O-18 เป็น deferred scope ไม่ใช่ open question.

## Change Log

- 2026-08-26 (project-manager) — เพิ่ม **Phase 13: Create-lesson commit modal & confirm-dialog
  replacements (R10) — Module M** จาก `design.md` amendment (CR-5, NR-20..NR-24, CD-1..CD-10,
  R-32..R-36, O-19) · frontend ล้วน 15 tasks แบ่งกลุ่ม A (modal state machine NR-20/21×2 จุดเรียก/
  22/23/24 แยกเป็น task ของตัวเอง) และกลุ่ม B (สถาปัตยกรรมร่วม + 5 จุด CD-5 + task ยืนยันปิดท้าย) ·
  ไม่มี migration/entity/endpoint/backend task ใดเลย · ไม่ติด 🔒 Security gate ตาม `design.md`
  §Module M แต่เขียนเงื่อนไขที่ `qa-engineer` เพิ่มเองได้ไว้ใน Sequencing Notes · เพิ่มข้อบังคับ
  "ห้ามทำขนานกับ Phase 12" (R-35, จุดที่ 5 ของ CD-5 คือ `handleArchiveLesson` ซึ่งเป็นโค้ดของ
  Module L ที่ยังไม่ผ่าน QA) และ "ต้องแก้สองจุดเรียกของ `router.push`" (R-32) เป็น Sequencing Notes
  แยกจากหัว phase — ไม่แตะ checkbox ของ Phase 1–12 ใดเลย

- 2026-08-26 (QA Round 11 TARGETED) — re-check P11-01 ครั้งที่ 2 จากโค้ดจริงทั้งสอง entry point
  ตาม stop condition ก่อนขยายเป็น FULL · `ApplyExcludedSlidesAsync` ปิดฝั่ง `SaveAsync` แล้ว:
  materialize ทุกแถว, group ทุก `SlideObjectId`, hard-delete sibling ผ่าน repository `Delete`
  และ regression test seed legacy duplicate บนหน้าที่ request ไม่ได้แตะได้จริง · แต่ฝั่ง EX-4
  `ToggleAsync(..., excluded: false)` ยังเลือกและ soft-delete เพียงหนึ่งแถว; legacy sibling ที่ยัง live
  ค้างอยู่ทำให้หน้ากลับมาไม่ได้จริงแม้ endpoint ตอบสำเร็จ และ test ใหม่ยืนยันให้สองแถวคงอยู่เอง ·
  targeted tests ผ่าน 2/2 แต่ไม่ขยาย FULL ตามเงื่อนไขผู้ใช้ · **ไม่ติ๊ก checkbox เพิ่ม:** Phase 11
  ยัง 36/37, Phase 12 ยังเริ่มไม่ได้ · P11-01 ถึงเพดาน failed re-check 2 แล้ว จึงส่งการตัดสินใจ
  รอบถัดไปให้เจ้าของโปรเจกต์แทนการวนกลับ engineer อัตโนมัติ

- 2026-08-26 (QA Round 10 FULL) — ตรวจ Phase 11 / Module K ใหม่ครบ 37 tasks หลังแก้
  P11-01..P11-04 · ติ๊กเพิ่ม 2 ข้อที่ยืนยันว่าผ่านจริง: P11-02 create flow ใช้
  `touchedAndNotExcludedIds` ครบ flush/progress/retry และ P11-03 มี cross-lesson
  `NotFound` test ที่ใช้ PDF คนละจำนวนหน้าจริง · P11-04 ปิดจาก frontend production build ใหม่
  ที่ผ่านครบ · **P11-01 ยัง Partial**: `ApplyExcludedSlidesAsync` group แล้วเลือกแถวตัวแทน แต่ไม่
  soft-delete duplicate siblings ที่ค้างจากก่อน fix ขณะที่ `GetOne` ยังใช้ `SingleOrDefault` กับ
  ทุกแถวรวม soft-deleted จึงยัง 500 ได้ · automated checks: frontend typecheck/lint/test 69/69/
  build ผ่าน, backend Release build 0/0 และ test 287/287, EF pending-model clean · ผลรวม
  **36/37 Verified, 1/37 Partial** — Phase 11 ยังไม่ปิดและ Phase 12 ยังเริ่มไม่ได้

- 2026-08-26 (QA Round 9 FULL) — ตรวจ **Phase 11 / Module K** จากโค้ดจริงครบ 37 tasks
  (26 backend + 11 frontend) และเปลี่ยน checkbox เป็น `[x]` จำนวน 34 ข้อ · เว้น 3 ข้อเป็น
  `[ ]` ตาม finding ใน `review.md`: P11-01 การ retry NR-13 ขั้นที่ 3 สร้างแถว exclusion ซ้ำ,
  P11-02 create flow ยัง flush narration ของหน้าที่แก้แล้วตัดออก และ P11-03 ยังไม่มี test
  cross-lesson `slideObjectId` → `NotFound` ตาม EX-12(ข) · รัน frontend typecheck/lint/test
  (69/69), backend Release build (0 warning/0 error), test (286/286) และ EF pending-model check
  (clean); frontend production build compile ผ่านแต่ page collection ติด generated `.next` cache
  ไม่สอดคล้อง (P11-04 Minor) · ผลรวม **34/37 Verified, 3/37 Partial** จึงยังไม่ปิด Phase 11
  และยังห้ามเริ่ม Phase 12

- 2026-08-26 (รอบสี่) — Amend: เพิ่ม **Phase 12: Lesson trash, restore & permanent purge (R9) —
  Module L 🔒 Security gate** ตาม `design.md` §Module L / DM-2 / DM-18 / **LT-1..LT-24** หลัง
  CR-4 และ Q-L1..Q-L5 ได้รับการยืนยันครบ · ไม่แก้ย้อนหลัง task/checkbox ของ Phase 1–11 แม้ข้อเดียว
  · Phase 12 วาง `MG-L1` (nullable `PurgeJobId`/`PurgeStartedAt`, trash index,
  `SessionQuestionReviewExclusion`) ไว้ phase เดียวกับ lifecycle ตามข้อบังคับ — additive ไม่มี
  backfill แต่มี preflight LT-24 ก่อน apply · task แยกครอบ archive/restore/manual queue, revoked-link
  token+learnerKey policy, queue hide/restore/permanent exclusion, `lesson_purge` claim/retry รายชั่วโมง/
  รายวัน, ordered external delete + DB finalization, shared-PDF guard, frontend trash tab/countdown/title
  confirmation และ tests tenant/race/idempotency ทั้งชุด · ติด gate เพราะ owner destructive action,
  `IgnoreQueryFilters()`, public token binding และ external hard deletes · **ขึ้นกับ Phase 11 ปิด
  implementation** เพื่อ purge `LessonExcludedSlide` ได้ครบ; O-18 retention ต่อบริษัทถูกตัดออกจาก
  task list โดยเจตนาตามคำสั่งเจ้าของโปรเจกต์

- 2026-08-26 (รอบสาม) — Amend: เพิ่ม **Phase 11: Cut pages from PDF lesson (R4.7) — Module K
  🔒 Security gate** ตาม `design.md` §Module K / EX-1..EX-12 (+ NR-1/NR-2/NR-3/NR-4/NR-5/NR-6/
  NR-7/NR-8/NR-12/NR-13/NR-15/NR-16/NR-17/NR-18 ที่ถูก amend) หลังมติ CR-3 ปิดครบ 2026-08-26 ·
  Phase 1–10 เดิมไม่ถูกแก้ย้อนหลังแม้บรรทัดเดียว (checkbox ที่ QA ติ๊กแล้วคงเดิมทั้งหมด) · Phase 11
  แตะ Data Model แค่ตารางใหม่ 1 ใบ (`LessonExcludedSlide`, DM-17) พร้อม migration เดียว (`MG-K1` —
  `AddLessonExcludedSlides`) ซึ่งอยู่ phase เดียวกับ EX-1..EX-12 ทั้งชุดตามข้อบังคับ ไม่มี backfill
  ไม่มีคอลัมน์ใหม่บนตารางเดิม · task ใหม่ล้วนครอบ toggle endpoint (EX-4), พื้นแข็งเหลืออย่างน้อย
  1 หน้า (EX-8), ผู้บริโภคทั้ง 5 รายของ EX-1 (กรอง 2 ที่ ไม่กรอง 1 ที่ ส่งเข้า toDelete 1 ที่),
  การถอด/คืน vector สองชุดพร้อมกัน (EX-5/EX-6 รวม `VectorDeleteTargetKind.LessonPage` ค่าใหม่และ
  จุดบังคับที่สองใน `ProcessDocumentIndexAsync`), ลำดับเขียน exclusion ในขั้นที่ 3 ของ NR-12
  (EX-9), การล้าง exclusion พร้อมบทพูดตอนอัปโหลดไฟล์ทับ (EX-10, breaking wire contract
  `{count}` → `{count, excludedCount}` มี caller เดียวคือ `LessonForm.tsx` isEdit — backend+
  frontend อยู่เฟสเดียว), การ validate `slideObjectId` สองชั้น (EX-12) ที่ `security` ต้องตรวจแยก
  จากสิทธิ์ `cs`/`admin` · เพิ่ม task แยกสองบรรทัดสำหรับจุด blast-radius ที่ `design.md` ชี้ชัด
  (`narrations/page.tsx:143`, `PdfLessonContentPhase.tsx:309` — ต้องเลิกคำนวณเลขหน้าไฟล์จาก
  `slide.index + 1` เปลี่ยนเป็นแปลงจาก `slideObjectId`) · ติด 🔒 Security gate ด้วย 3 เหตุผลตาม
  `design.md` §Module K (`slideObjectId` ดิบขับ vector-delete จริงสองชุด, `LessonId` ต้องคู่กับ
  query filter ก่อนประกอบ namespace, idempotency ของ toggle) · **ขึ้นกับ Phase 3/4/5/10 (สามตัวแรก
  มีโค้ดอยู่แล้ว) แต่ Phase 10 เป็นเงื่อนไขเวลาไม่ใช่แค่ฟังก์ชัน (R-26/มติ Q-K4)** — เขียน
  Sequencing Note ระบุเงื่อนไขเริ่ม Phase 11 ครบ 3 ข้อ (Phase 10 ผ่าน FULL, checkbox ติ๊กครบ,
  ไม่มี issue ค้างใน `review.md`) ตามรูปแบบเดียวกับที่ Phase 9→10 เคยเขียนไว้ (R-24) — ณ วันที่วาง
  แผนนี้ Phase 10 ผ่านแค่ TARGETED 21/21 จึง**ห้าม dispatch งานของ Phase 11 จนกว่าเงื่อนไขทั้งสาม
  จะเป็นจริง**
- 2026-08-26 (รอบสอง) — `project-manager` ตรวจว่า Phase 10 (Module J, เพิ่มไว้ก่อนหน้าในวันเดียวกัน)
  ครอบ NR-10..NR-19 ครบตาม `design.md` §Module J แล้ว ไม่มีช่องว่างต้องเพิ่ม task — แก้เฉพาะถ้อยคำ
  R-24 sequencing note 2 จุด (หัว Phase 10 + `## Sequencing Notes`): ตรวจโค้ดจริงพบว่า
  `LessonForm.tsx` มีบรรทัดสรุปอ่านอย่างเดียวแทนการ์ด `DocumentUploadList`/`fixedScope` เดิมแล้ว
  (commit `91e6d19`) — **Phase 9 merge เป็นโค้ดจริงแล้ว** แก้คำที่เคยเขียนว่า "ยังไม่มีโค้ดเขียนจริง
  แม้บรรทัดเดียว" ให้ตรงกับของจริง แต่ **บทสรุปเชิงปฏิบัติไม่เปลี่ยน**: checkbox ของ Phase 9 ยังไม่มี
  ช่องใดถูก `qa-engineer` ติ๊ก จึงยังห้าม dispatch งาน `[frontend]` ของ Phase 10 ต่อไปจนกว่า Phase 9
  จะผ่าน QA จริง · ไม่แตะ checkbox ใดทั้ง Phase 9/10 · ไม่เพิ่ม/ลบ task

- 2026-08-26 — Amend: เพิ่ม **Phase 10: Content-management phase for PDF lesson creation (CR-2) —
  Module J 🔒 Security gate** ตาม `design.md` §Module J / NR-10..NR-19 (+ NR-1/NR-2/NR-3/NR-5/
  NR-6/NR-8 ที่ถูก amend เฉพาะจุด) หลังมติ `Q-J1`/`Q-J2` เคาะครบเมื่อ 2026-08-26 · Phase 1–9 เดิม
  ไม่ถูกแก้ย้อนหลังแม้บรรทัดเดียว (checkbox ที่ QA ติ๊กแล้วคงเดิมทั้งหมด) · Phase 10 ไม่มี migration/
  ฟิลด์ใหม่/entity ใหม่ (ทุก endpoint ใหม่ไม่แตะฐานข้อมูล) เป็น task ใหม่ล้วนครอบ endpoint preview
  session ใหม่ 2 ตัว (NR-10), endpoint ภาพสไลด์ตัวที่สามสำหรับหน้าแก้บทพูดเดิม (NR-18), company
  isolation ที่เขียนด้วยมือ (NR-11), ลำดับ commit 4 ขั้นตายตัวห้ามสลับ 3↔4 (NR-12), สัญญาความ
  ล้มเหลวครบ 4 กรณี (NR-13), มติ `ScopeType = lesson` ตอน commit (NR-14), คำเตือนก่อนยืนยัน (NR-15),
  ล้าง draft เงียบ ๆ เมื่อเปลี่ยนไฟล์กลางเฟส (NR-16), ขอบเขต + supersede R8.3/UC-5 อย่างเป็นทางการ
  (NR-17) · ติด 🔒 Security gate ด้วย 3 เหตุผลตาม `design.md` §Module J (parse/render ไฟล์นอก DB,
  company isolation มือเขียนของ `IMemoryCache`, ต้นทุน RAM/CPU ที่ผู้ใช้สั่งได้ตรง — R-22) ·
  **ขึ้นกับ Phase 1/3/5/7 (มีโค้ดอยู่แล้ว เริ่มได้ทันที) แต่ห้ามทำขนานกับ Phase 9 (R-24)** — ทั้งสอง
  แก้ `LessonForm.tsx` ไฟล์เดียวกันคนละบริเวณ · ระบุชัดว่า ณ วันที่วางแผนนี้ Phase 9 ยังไม่มีโค้ด
  เขียนจริงแม้บรรทัดเดียว (มีแต่ task list ที่ยังไม่ติ๊ก) จึงห้าม dispatch งาน `[frontend]` ของ
  Phase 10 จนกว่า Phase 9 จะ merge เป็นโค้ดจริงและผ่าน `qa-engineer` ก่อน — งาน `[backend]` ของ
  Phase 10 ไม่แตะ `LessonForm.tsx` จึงเริ่มก่อนได้
- 2026-08-25 — **`project-manager` แก้ถ้อยคำ task KL-23 ของ Phase 8 ให้ตรงมติ `Q-H2` (= ทาง (ข),
  เจ้าของโปรเจกต์เคาะ 2026-08-25) — targeted correction ตาม `design.md` KL-23 (เขียนใหม่ทั้งข้อ) +
  KL-26 (ใหม่) ไม่ใช่ scope ใหม่: 4 bullet, แก้คำอธิบายเท่านั้น ไม่แตะ checkbox แม้ช่องเดียว
  ไม่เพิ่ม/ลบ/สลับ task และ phasing ไม่เปลี่ยน** · (1) backend task KL-23 (implement): จาก
  "คืนคำเตือนหลังบันทึก ไม่บล็อก" เป็น **ด่านก่อนเขียน** — ตรวจก่อน `_repository.Add`/
  `KnowledgeQnASource`/ปิดคิว/`EnqueueJob`/`Commit()`; ไม่มีธง `CheckDuplicate` (unconditional);
  เพิ่มธง `ConfirmDuplicate` (wire `confirmDuplicate`, default `false`) เป็นตัวข้ามการตรวจแทน;
  เจอซ้ำ → 409 + `DuplicateQnAResponse` ใหม่ (ลิสต์ ไม่ใช่ใบเดียว); ลบ
  `KnowledgeQnACreateResultViewModel`/`DuplicateWarning` ทั้งคลาส, success response เปลี่ยนเป็น
  `Ok(new { qna = ... })`; ระบุชัดว่า `PUT` ไม่ตรวจซ้ำ ไม่มีธงนี้ (2) backend unit test KL-23:
  เขียนใหม่ 3 ตัวเดิม (`:222/242/259` — `:259` คือ test สองบริษัท ห้ามลบ) ให้ยืนยัน 409 แทน
  `DuplicateWarning` + เพิ่ม 2 ตัวใหม่ (ไม่เขียน/ไม่เข้าคิวเมื่อ 409, `ConfirmDuplicate=true`
  บันทึกผ่านจริง) (3) frontend task UI ด่าน Q&A ซ้ำ (KL-26): จาก UI คำเตือนหลังบันทึกเป็น UI รับ
  409 ก่อนบันทึกที่ `KnowledgeQnAAnswerDialog` โหมด `create` ที่ `/admin/qna-queue` เท่านั้น
  (pattern เดียวกับ `DocumentUploadList.tsx:218-224`), สามปุ่ม "ยืนยันบันทึกซ้ำ"/"แก้ใบเดิมแทน"
  ต่อรายการ (สลับ `mode: "edit"` ในที่เดิม ไม่ fetch ไม่ navigate)/"ยกเลิก", ห้ามลิงก์ไป
  `/admin/documents?q=...` เด็ดขาด, ต้องบอกว่าแก้ใบเดิมไม่ปิดคำถามในคิว (ช่องว่างที่รับรู้แล้ว =
  `O-13`/R7.7 อนาคต), ลบ prop `onEditExisting` ทิ้งทั้งหมด (4) frontend types task
  (`src/types/domain.ts`): เพิ่ม `CreateKnowledgeQnADto.confirmDuplicate` และ
  `DuplicateQnAResponse` (ใช้ type `KnowledgeQnA` เดิม), ลบ type ของรอบแรกที่แทน
  `DuplicateWarning`/`KnowledgeQnACreateResultViewModel` ถ้ามี
- 2026-08-25 — **`system-analyst` แก้ถ้อยคำ task ของ Phase 8 ให้ตรง contract ที่ amend ในวันเดียวกัน
  — 5 task, แก้คำอธิบายเท่านั้น ไม่แตะ checkbox แม้ช่องเดียว ไม่เพิ่ม/ลบ/สลับ task และ phasing
  ไม่เปลี่ยน จึงไม่ต้องมีรอบ `project-manager`** · (1) task KL-19: เลิกอ้างว่าคิวรีใช้ index
  `(CompanyId, ContentHash)` (โค้ดจริงเทียบ hash ในหน่วยความจำบนลิสต์ก้อนเดียวกับ KL-20) แต่
  index ยังต้องมี ห้าม drop · (2) task KL-20: `EF.Functions.ILike` → in-memory
  `OrdinalIgnoreCase` เพราะ `ILike` ตีความ `_`/`%` ในชื่อไฟล์เป็น wildcard = false positive ·
  (3) task KL-21: `createDate` → **`createdAt`** และระบุว่า 409 ขี่มาใน `ApiErrorResponse.error.details`
  ไม่ใช่ body เปล่า · (4) task KL-23: ระบุว่า normalize/เทียบทำในหน่วยความจำ ห้ามย้ายไป SQL ·
  (5) **task frontend ที่เคยเขียนว่า "เพิ่มฟอร์มบันทึก Q&A ใหม่จากหน้ารวม" — ถ้อยคำนี้คลาดเคลื่อน
  `design.md` KL-1..KL-17 ไม่เคยให้อำนาจสร้าง Q&A กับหน้า `/admin/documents`** และตาม QQ-2/QQ-7
  มันสร้างไม่ได้อยู่แล้ว (`SessionQuestionIds` ต้องมีอย่างน้อยหนึ่ง) → แก้เป็น "UI เตือน Q&A ซ้ำ
  ตาม KL-23" และชี้ไป **KL-25** ที่เพิ่งเขียนขึ้นเพื่อปิดข้อนี้ให้ขาด · **`frontend-engineer`
  ที่ไม่ยอมสร้างปุ่มเองทำถูกต้อง โค้ดไม่ต้องแก้จากข้อนี้** · ⚠️ **เพิ่มหมายเหตุว่า Phase 8 ยังปิด
  ไม่ได้เพราะ `Q-H2`** (คำเตือน KL-23 ขึ้นหลังบันทึกสำเร็จ → ปุ่มสองปุ่มให้ผลเหมือนกัน) รอมติ
  เจ้าของโปรเจกต์ใน `design.md` §Unresolved Open Questions
- 2026-08-19 — สร้าง `plan.md` ครั้งแรกจาก `design.md` ที่ confirmed แล้ว (Q1–Q6 เคาะครบ) ·
  6 phase ตรงตาม Module A–F · Phase 2/3/4/6 ติด 🔒 Security gate ตาม design.md §Modules ·
  บันทึก cross-module migration dependency กับ `learning-session` ไว้ที่หัวเอกสาร
- 2026-08-19 — Amend ให้ตรง migration phasing ที่ SA ยืนยัน: Phase 1 ใช้ `AddKnowledgeTaxonomyAndScope`
  (MG-A1..MG-A7) และคืน Q&A counts เป็น `0`; เพิ่ม migration เฉพาะ Phase 3/4/5/6 ตาม MG-C1/MG-D1/
  MG-E1/MG-F1 พร้อมย้าย validation และการเชื่อม Q&A counts จริงไป Phase 6 · local development ใช้
  baseline ที่ผ่านแล้วได้โดยไม่ต้องรอ shared/production deployment; real migration ยังเป็น DevOps hard stop
- 2026-08-19 — Amend default-chain contract: ระบุ MG-A3/MG-A4 สร้าง parent+leaf ที่ flagged อย่างละหนึ่ง
  แถวต่อบริษัทและ backfill บทเรียนไป leaf; `GetSystemDefault()` filter leaf ก่อน `SingleOrDefault()`;
  เพิ่ม test สำหรับ invariant, duplicate leaf และการบล็อก update/delete ของทั้ง chain โดยไม่เปลี่ยน
  migration SQL/ชื่อ/shape
- 2026-08-20 — Amend: เพิ่ม **Phase 7: Document scope assignment — R3 write path 🔒 Security gate**
  ตาม `design.md` Module G (DS-1..DS-12) ที่ `system-analyst` เพิ่งปิดช่องว่าง R3 ฝั่งเขียน — Phase 1–6
  เดิมไม่ถูกแก้ย้อนหลังแม้แต่บรรทัดเดียว (checkbox ที่ QA ติ๊กแล้วคงเดิมทั้งหมด) เพิ่ม task ใหม่ล้วน
  ครอบ DS-1 (DTO เปลี่ยน `LessonSlug` → `ScopeType`/`ScopeId`), DS-2 (`EnsureValidScope` ก่อนแตะ
  storage), DS-3 (6 กรณีปฏิเสธ), DS-4 (list กรองตาม scope), DS-5/DS-6/DS-7 (`PATCH .../scope` +
  ย้าย scope ผ่าน `BackgroundJob` เดิม), DS-8/DS-9 (UI scope picker + คอลัมน์ขอบเขต ลอกจาก
  `KnowledgeQnAAnswerDialog.tsx`), DS-12 (3 กลุ่ม test) — ไม่มี task migration ใดๆ ตามที่ design.md
  ระบุชัดว่า Phase 7 ไม่แตะ schema · เพิ่ม Sequencing Note ว่า Phase 7 ขึ้นกับ Phase 1–4 (มีโค้ดแล้ว)
  ไม่ขึ้นกับ 5/6 แต่ควรรอ Phase 3 ผ่าน QA TARGETED (backend fix cross-company leak อยู่ใน working
  tree แล้วแต่ยังไม่ยืนยัน ณ วันที่วางแผนนี้) ก่อนเริ่มลงมือจริง เพื่อไม่ให้แยกไม่ออกว่าบั๊กใหม่มาจากไหน
  ถ้าเจอ cross-company leak ซ้ำระหว่างทดสอบ DS-3/DS-6
- 2026-08-25 — Amend: เพิ่ม **Phase 8: Knowledge library view (R7) — extends `/admin/documents`
  🔒 Security gate** และ **Phase 9: Upload consolidation (R8) — delete-only, no gate** ตาม
  `design.md` Module H/I (KL-1..KL-24, UC-1..UC-10) หลังมติ Q-H1 = ทาง B ปิดช่องว่างสุดท้ายของ R7.5 ·
  Phase 1–7 เดิมไม่ถูกแก้ย้อนหลังแม้บรรทัดเดียว (checkbox ที่ QA ติ๊กแล้วคงเดิมทั้งหมด) · Phase 8 มี
  migration ใบเดียวคือ `MG-H1` (`AddDocumentContentHash`) อยู่เฟสเดียวกับ KL-18..KL-24 ตามข้อบังคับ
  ห้ามแยก · Phase 8 ติด gate ด้วย 4 เหตุผล (ไม่มีเงื่อนไข `Where`, endpoint คืนเนื้อความคลังความรู้ตัวที่สอง,
  prompt injection ผ่านการแก้ Q&A, และ 409 duplicate-check เป็น cross-tenant existence oracle) · Phase 9
  ไม่ติด gate เพราะเป็นการลบ UI ล้วน แต่ต้อง deploy หลัง Phase 8 เสมอ (R-18) ไม่ใช่แค่เขียนโค้ดเรียงกัน ·
  ระบุที่อยู่จริง `LessonForm.tsx:679-698` แทนที่อยู่เก่าใน `requirement.md` R8.1/R8.3 และย้ำ
  ห้ามแตะ `handlePdfUpload` (`LessonForm.tsx:187`) ตาม UC-5/R8.3
