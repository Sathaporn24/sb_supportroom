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

6 phase เรียงตาม dependency chain ที่ `design.md` §Modules ระบุไว้ตรงตัว:
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

- [ ] [backend] เพิ่ม `DocumentResource.ContentHash` (`string?`, `init`-only) ตาม DM-3 — comment อธิบาย 2 กรณีที่เป็น `null`: แถวที่อัปก่อน `MG-H1` (จงใจไม่ backfill) และไม่มีกรณีอื่นอีก
- [ ] [backend] แก้ `ApplicationDbContext.OnModelCreating` — เพิ่ม index `(CompanyId, ContentHash)` แบบ**ไม่ unique**ตาม DM-15
- [ ] [backend] สร้าง EF Core migration `AddDocumentContentHash` (**MG-H1**) — เพิ่มคอลัมน์ `ContentHash` แบบ **nullable ถาวร** (ไม่มีขั้นตอนตามมาที่ทำให้ `NOT NULL`) + index ตามข้างบน — **additive ล้วน ห้าม backfill ห้าม UPDATE ห้ามแตะข้อมูลเดิมแม้แถวเดียว ห้ามแตะ Pinecone**; `Down()` = drop index + drop column (ย้อนกลับได้สมบูรณ์ 100%)
- [ ] [backend] แก้ `IDocumentResourceService.UploadAsync` — คำนวณ SHA-256 ของ byte ทั้งไฟล์ (hex ตัวพิมพ์เล็ก 64 ตัว ไม่มี prefix/ขีดคั่น) จาก `input.Content` ก้อนเดียวกับที่ส่งเข้า storage **ก่อน**เขียนแถวลง DB เสมอทุกทางอัปโหลด รวม `handlePdfUpload` (UC-5) — คำนวณครั้งเดียวตลอดอายุแถว ห้าม recompute ตอนย้าย scope/soft delete/กู้คืน/re-index; ห้ามรับ hash จาก client
- [ ] [backend] แก้ `IDocumentResourceRepository.GetAllInCompany()` (KL-2/KL-9 pattern) — `FindBy(_ => true)` พึ่ง EF query filter (`CompanyId` + `!IsDelete`) ล้วนๆ **ห้ามใช้ `IgnoreQueryFilters()` เด็ดขาด**
- [ ] [backend] แก้ `DocumentResourceService.GetByScope(null, null)` — เรียก `GetAllInCompany()` แทนการแปลงค่าว่างเป็น `company` เดิม (DS-4 เก่า); `scopeType` ที่ส่งมาแล้วพฤติกรรมเดิมทุกอย่างคงเดิม
- [ ] [backend] เพิ่ม `IKnowledgeQnARepository.GetAllInCompany()` — รูปเดียวกับข้างบนทุกประการ (`FindBy(_ => true)`, ห้าม `IgnoreQueryFilters()`)
- [ ] [backend] เพิ่ม `IKnowledgeQnAService.GetAll(KnowledgeQnAFilter filter)` — รับ query เดียวกับเอกสาร (`scopeType`/`scopeId`/`status`/`q`) ตีความเหมือนกันทุกข้อ (KL-2..KL-5, KL-11..KL-13); ใช้ `KnowledgeQnAViewModel` เดิม **ห้ามเพิ่มฟิลด์**
- [ ] [backend] `GET /api/knowledge-qna` ใหม่ทั้งเส้น (controller) — เรียก `guard.EnsureAuthenticated()` ตรงๆ ใน service (เหตุผลเดียวกับ DI-7) ไม่พึ่ง query filter อย่างเดียว; สิทธิ์ = `owner`/`admin`/`cs` ของบริษัทนั้น เรียง `CreateDate` ลง
- [ ] [backend] implement KL-5 — filter ตามหมวด X คืนทั้ง (ก) แถว `ScopeType=category && ScopeId=X` และ (ข) แถว `ScopeType=lesson` ที่ `LessonConfig.CategoryId=X` ทั้งฝั่งเอกสารและ Q&A
- [ ] [backend] implement KL-11 — ค้นในเนื้อหาด้วย `EF.Functions.ILike` (parameterized เสมอ **ห้ามต่อ SQL เป็นสตริงเข้า `FromSqlRaw`**): เอกสารติดเมื่อ `FileName` ตรงหรือมี `DocumentChunk.Text` แถวใดตรง (`DISTINCT` ระดับเอกสาร); Q&A ติดเมื่อ `Question` หรือ `Answer` ตรง
- [ ] [backend] implement KL-12 — `q` trim แล้วว่าง = ไม่ค้น (คืนตาม filter อื่น), ต่ำกว่า 2 ตัวอักษร = ไม่ค้น (ไม่ error), ค้น+filter ทำงานแบบ AND เสมอ, ไม่มี pagination
- [ ] [backend] implement KL-19 — นิยาม "เนื้อหาซ้ำ": `CompanyId` เดียวกัน และ `ContentHash` เท่ากันเป๊ะ; `ContentHash = null` ไม่เคยซ้ำกับอะไรเลยแม้กับ `null` ด้วยกันเอง (เขียนเงื่อนไข `hash != null &&` ในโค้ดตรงๆ ห้ามพึ่ง `NULL = NULL` ของ SQL); ไม่นับแถว soft-deleted; **ห้ามเทียบข้ามบริษัทเด็ดขาด**; scope ไม่เกี่ยว — ไฟล์เดียวกันคนละ scope ก็นับซ้ำ; **แก้ถ้อยคำ 2026-08-25 (design.md KL-19)**: เทียบ hash **ในหน่วยความจำ** บนลิสต์ก้อนเดียวกับ KL-20 ไม่ใช่เป็นเงื่อนไข `Where` ที่ระดับคิวรี (KL-20 ต้อง materialize อยู่แล้วโดยเลี่ยงไม่ได้ การเทียบ hash บนลิสต์นั้นจึงฟรี ส่วนการแยกเป็นอีกคิวรีคือการเพิ่ม round-trip เปล่า ๆ) — **index `(CompanyId, ContentHash)` ยังต้องมีตาม DM-15/MG-H1 ห้าม drop** (ถูกต้องทันทีที่ทะลุเส้น ~500 ของ O-11); `CompanyId` ต้องอยู่ใน predicate ตรง ๆ ไม่ฝากไว้กับ query filter อย่างเดียว
- [ ] [backend] implement KL-20 — นิยาม "ชื่อซ้ำ" แยกจาก KL-19: `CompanyId` เดียวกัน และ `FileName` เท่ากันหลัง trim + เทียบไม่สนตัวพิมพ์ใหญ่เล็ก; **แก้ถ้อยคำ 2026-08-25 (design.md KL-20)**: ใช้ `string.Equals(..., OrdinalIgnoreCase)` **ในหน่วยความจำ ห้ามใช้ `EF.Functions.ILike`** — `ILike` ตีความ `_`/`%` ที่อยู่ในชื่อไฟล์เป็น wildcard (`report_2026.pdf` จะแมตช์ `report-2026.pdf` เป็น false positive) ขัดกับคำสั่ง "เทียบเต็มสตริง" ของข้อนี้เอง และ throw ใน fake repository ของ `Application.Tests` ทำให้ test สี่ตัวข้างล่างเขียนไม่ได้ · **KL-11 ไม่เกี่ยว ยังเป็น `ILike` จริงตามเดิม**; ไม่นับแถว soft-deleted; ต้องรายงานแยก 4 แบบ (ชื่อ+เนื้อหาซ้ำ / เนื้อหาซ้ำชื่อต่าง / ชื่อซ้ำเนื้อหาต่าง / ไม่ซ้ำ) — ห้ามยุบเป็นข้อความเดียว
- [ ] [backend] แก้ `UploadDocumentDto` — เพิ่ม `bool CheckDuplicate` ค่าเริ่มต้น `false` (additive, ไม่ breaking)
- [ ] [backend] implement KL-21 — เมื่อ `CheckDuplicate = true`: ตรวจ KL-19/KL-20 **ก่อน**เขียนแถว/storage/เข้าคิว index; เจอซ้ำ → **ไม่เขียนอะไรทั้งสิ้น** คืน `409 Conflict` · **แก้ถ้อยคำ 2026-08-25 (design.md KL-21)**: payload ไม่ใช่ body เปล่า แต่ขี่มาใน `ApiErrorResponse` envelope เดิม — `GeneralException.Conflict(messageTh, details)` → `{ error: { code: "CONFLICT", message, details } }` โดย `details` = `DuplicateDocumentDto` = `{ duplicateByHash: [...], duplicateByFileName: [...] }`; ต้องเพิ่ม `"CONFLICT"` ใน `ApiErrorCode` **ทั้งสองฝั่ง** (`SupportRoom.Domain/Enums/ApiErrorCode.cs` ↔ `frontend/src/types/api.ts`); แต่ละรายการมีแค่ `id`/`fileName`/`scopeType`/`scopeId`/**`createdAt`** (**ไม่ใช่ `createDate`** — ชื่อ wire ตรงกับ `DocumentResourceViewModel.CreatedAt`); `CheckDuplicate = false` (ค่าเริ่มต้น) = พฤติกรรมเดิมทุกตัวอักษร แต่ยังคำนวณ/เก็บ `ContentHash` เสมอ
- [ ] [backend] implement KL-23 — **ด่านก่อนบันทึก ไม่ใช่คำเตือนหลังบันทึก (มติ Q-H2 = ทาง (ข), เขียนใหม่ทั้งข้อ 2026-08-25 แทนถ้อยคำ "คืนคำเตือน ไม่บล็อก" เดิม)**: สร้าง `KnowledgeQnA` ใหม่ — ตรวจก่อนเขียนอะไรทั้งสิ้น (ก่อน `_repository.Add`, ก่อนเขียนแถว `KnowledgeQnASource` แม้แถวเดียว, ก่อนปิดคำถามในคิว, ก่อน `EnqueueJob(qna_index)`, ก่อน `UnitOfWork.Commit()`) ว่า `Question` ซ้ำเป๊ะหลัง trim + ยุบช่องว่างติดกันเหลือช่องเดียว + case-insensitive **ในหน่วยความจำ** (`CollapseWhitespace` + `OrdinalIgnoreCase`, **ห้ามย้ายไป SQL** — ไม่มี LINQ translation ปลอดภัยสำหรับ "ยุบช่องว่าง") กับ Q&A ที่ยังไม่ถูกลบของบริษัทเดียวกัน — เทียบเฉพาะ `Question` ไม่เทียบ `Answer`, ไม่เทียบข้ามบริษัท, ไม่สน scope, `CompanyId` อยู่ใน predicate ตรง ๆ ไม่ฝากไว้กับ query filter อย่างเดียว (คนละกลไกกับ KL-19/20 ทั้งหมด ไม่ใช้ `ContentHash`) · **ลำดับตรวจตายตัว**: `EnsureValidScope` → `SessionQuestionIds` ว่าง/หาไม่เจอ → ตรวจซ้ำ (400/404 ชนะ 409 เสมอ) · **ไม่มีธง `CheckDuplicate` — ตรวจ unconditional ทุกครั้งที่ create** (call site เดียวในระบบคือ `app/admin/qna-queue/page.tsx:130` โหมด `create`; ไม่มี caller ที่ต้องเงียบแบบ `handlePdfUpload` ของ KL-21 ห้ามลอกแบบ opt-in มา) · เพิ่ม `CreateKnowledgeQnADto.ConfirmDuplicate` (`bool`, ค่าเริ่มต้น `false`, wire `confirmDuplicate`) — `true` = ข้ามการตรวจ บันทึกปกติแม้มีของซ้ำจริง (ไม่ error) · เจอซ้ำ → `GeneralException.Conflict(messageTh, details)` → `{ error: { code: "CONFLICT", message, details } }` (ใช้ `ApiErrorCode.Conflict` ที่มีอยู่แล้วทั้งสองฝั่ง ไม่ใช่ค่าใหม่) โดย `details` = **`DuplicateQnAResponse` ใหม่** = `{ duplicateByQuestion: KnowledgeQnAViewModel[] }` — **ลิสต์ ไม่ใช่ใบเดียว**, เรียง `CreateDate` ลง, ใช้ `KnowledgeQnAViewModel` เดิมทั้งใบ (ห้ามเพิ่มฟิลด์ ห้ามยัดลง `DuplicateDocumentDto`/`DuplicateDocumentsResponse` ของ KL-21 — คนละ endpoint คนละ shape) · **breaking change มี caller เดียว**: ลบ `KnowledgeQnACreateResultViewModel` ทั้งคลาสพร้อมฟิลด์ `DuplicateWarning`; `POST /api/knowledge-qna` คืน **`Ok(new { qna = ... })` รูปเดียวกับ `PUT`** (แก้บั๊ก casing แฝงของโค้ดวันนี้ที่คืน ViewModel ทั้งใบตรงๆ แล้ว serialize เป็น `qnA` ไม่ใช่ `qna` ไปในตัว) · **`UpdateAsync`/`PUT` ไม่ตรวจซ้ำ ไม่มี `ConfirmDuplicate` — ห้ามเติมเอง "เพื่อความสม่ำเสมอ"** (QQ-6 คุมทางแก้อยู่แล้ว ถ้าเห็นว่าควรมี ให้ตีกลับ `system-analyst`)
- [ ] [backend] DTO/ViewModel: `DuplicateDocumentDto` (payload ของ 409 ตาม KL-21), `KnowledgeQnAFilter`, ยืนยันว่า `ContentHash` **ไม่ออก API** (ไม่เพิ่มลง `DocumentResourceViewModel` หรือ response ใดเลย)
- [ ] [backend] unit test (R-12 pattern) — KL-19: สองบริษัทมีไฟล์ hash เท่ากันเป๊ะ ต้อง**ไม่**เตือนข้ามกัน (ต้องมี test สองบริษัทจริง ไม่ใช่ test บริษัทเดียว)
- [ ] [backend] unit test — KL-20: ครบ 4 กรณี (ชื่อ+เนื้อหาซ้ำ / เนื้อหาซ้ำชื่อต่าง / ชื่อซ้ำเนื้อหาต่าง / ไม่ซ้ำ)
- [ ] [backend] unit test — KL-19 null-handling: `ContentHash = null` สองแถวไม่ถูกจับว่าซ้ำกัน
- [ ] [backend] unit test — KL-23: **แก้ถ้อยคำทั้งข้อ 2026-08-25 (มติ Q-H2)** จาก "test คำเตือน (`DuplicateWarning`)" เดิมเป็น **test ด่าน 409**: เขียนใหม่ 3 ตัวเดิมที่ `KnowledgeQnAServiceTests.cs:222/242/259` ให้ยืนยัน `GeneralException.Conflict`/409 พร้อม `DuplicateQnAResponse` แทนการยืนยัน `DuplicateWarning` (ตัวที่ `:259` คือ test สองบริษัทที่พิสูจน์ว่าไม่เตือนข้ามกัน — **ต้องคงไว้ ห้ามลบ** ปรับแค่ assertion ให้ตรง shape ใหม่) ยังต้องครอบ trim/ยุบช่องว่าง/case-insensitive เหมือนเดิม · เพิ่มอีก 2 ตัวใหม่: (1) เมื่อโยน 409 แล้ว **ไม่มี**แถว `KnowledgeQnA`/`KnowledgeQnASource` ถูกเขียนแม้แถวเดียวและ**ไม่มี** job เข้าคิว (`EnqueueJob(qna_index)` ไม่ถูกเรียก) — proof ว่าตรวจก่อน `Commit()` จริง (2) `ConfirmDuplicate = true` บันทึกผ่านสำเร็จแม้มี `Question` ซ้ำอยู่แล้ว (ข้าม 409 ได้จริง ไม่ error)
- [ ] [frontend] แก้ `app/admin/documents/page.tsx` — เพิ่มตัวกรอง `scopeType`/`scopeId` เริ่มต้นจาก URL query param (KL-2 ค่าเริ่มต้น "ทั้งหมด" เมื่อไม่มี/ไม่รู้จัก query)
- [ ] [frontend] แก้ layout หน้า `/admin/documents` ตาม KL-1 — แถบ filter+ค้นหาชุดเดียวคุมทั้งหน้า ด้านล่างสองตารางแยกกัน ("เอกสาร" และ "คำถาม-คำตอบ (Q&A)")
- [ ] [frontend] เพิ่มตัวเลือก filter ที่ 4 ("เฉพาะบทเรียน" → `Select` บทเรียนด้วย `listLessons()` ที่มีอยู่แล้ว) ต่อยอดจาก 3 ตัวเดิมใน `DocumentUploadList.tsx:255-280` (KL-4) — ตัวเลือกชุดเดียวกันใช้กับทั้งสองตาราง
- [ ] [frontend] แก้ `scopeLabel()` (`DocumentUploadList.tsx:119-129`) ตาม KL-6 — `lesson` แสดงชื่อบทเรียนจริง (map จาก `listLessons()`) แทนคำว่า "บทเรียนนี้"; id ที่หาไม่เจอแสดง "บทเรียนที่ถูกลบไปแล้ว"/"หมวดที่ถูกลบไปแล้ว" ห้ามแสดง id ดิบ ห้ามซ่อนแถว
- [ ] [frontend] เพิ่ม badge "ใช้เป็นสไลด์ของบทเรียน &lt;ชื่อ&gt;" ตาม KL-7 — เอกสารที่ `Id` ตรงกับ `LessonConfig.PdfDocumentResourceId` ของบทเรียนใดก็ตาม (ย้ายจาก badge เดิมที่ผูกกับโหมด `fixedScope`) ใช้ข้อมูลจาก `listLessons()` ที่โหลดอยู่แล้ว ไม่ต้องมี endpoint ใหม่
- [ ] [frontend] เพิ่มช่องค้นหาเนื้อหาตาม KL-11..KL-13 — ต่อกับ `GET /api/documents`/`GET /api/knowledge-qna` query `q`; แสดงข้อความกำกับว่าต้องพิมพ์อย่างน้อย 2 ตัวอักษร และข้อความว่าการค้นเนื้อหาไม่ครอบเอกสารที่ index ไม่สำเร็จ (ค้นได้จากชื่อไฟล์เท่านั้น)
- [ ] [frontend] เพิ่มตาราง Q&A ในหน้า `/admin/documents` (KL-1/KL-8) — ดึงจาก `GET /api/knowledge-qna` ใหม่, คอลัมน์ `Question`/`Answer`/ขอบเขต/`IndexingStatus`, ปุ่ม "แก้ไข" และ "ลบ" ต่อแถว
- [ ] [frontend] ปุ่ม "แก้ไข" ต่อแถว Q&A (KL-14) — เปิด `KnowledgeQnAAnswerDialog.tsx` ที่มีอยู่แล้วเป็นฐาน (ห้ามสร้าง pattern ที่สอง), เรียก `updateKnowledgeQnA(id, ...)` ที่มีอยู่แล้ว
- [ ] [frontend] ปุ่ม "ลบ" ต่อแถว Q&A (KL-15/16) — dialog ยืนยันที่บอกผลข้างเคียงครบ: "คำถามที่ Q&A นี้เคยปิดไว้จะกลับเข้าคิวรีวิวอีกครั้ง" + "ข้อมูลจะถูกลบออกจากคลังความรู้ AI จะเลิกใช้ตอบ" (ห้ามใช้ `window.confirm()` เปล่า); ลบสำเร็จ → ข้อความยืนยันพร้อมลิงก์ไป `/admin/qna-queue`
- [ ] [frontend] UI ด่าน Q&A ซ้ำตาม **KL-26** — **เขียนใหม่ทั้งข้อ 2026-08-25 (มติ Q-H2 = ทาง (ข), เคาะแล้ว)** แทนถ้อยคำเดิม "แสดงคำตอบเดิมพร้อมตัวเลือก 'บันทึกเพิ่มอยู่ดี'/'ไปแก้ใบเดิมแทน' หลัง backend คืนคำเตือน" ซึ่งเป็นแบบหลังบันทึกที่ตกไปแล้ว — ของเดิมทำที่ `KnowledgeQnAAnswerDialog` โหมด `create` ที่ `/admin/qna-queue` **ที่เดียว** (ทางสร้างเดียวตาม KL-25 หน้าคลัง `/admin/documents` ไม่มีทางสร้าง Q&A ห้ามเพิ่มปุ่ม "เพิ่ม Q&A" ที่นั่น) ยังถูกอยู่ สิ่งที่เปลี่ยนคือ**กลไกรับ**: **รับ 409 ก่อนบันทึก** — `catch (err instanceof ApiClientError && err.status === 409)` แล้วอ่าน `err.response.error.details` เป็น `DuplicateQnAResponse` (**pattern เดียวกับ `DocumentUploadList.tsx:218-224` คำต่อคำ ห้ามสร้าง pattern ที่สอง**) เก็บ state เป็น**ลิสต์** (ไม่ใช่ใบเดียว) พร้อม input ที่ CS กรอกไว้ทั้งชุดไว้ส่งซ้ำได้ · ต่อรายการในลิสต์แสดง: คำถามที่ซ้ำ + **คำตอบเดิมเต็มๆ** + ป้ายขอบเขต (ใช้ `scopeLabel()` ชุดเดียวกับ KL-6 ห้าม id ดิบ) + วันที่บันทึก + ข้อความชัดเจนว่า **"ยังไม่ได้บันทึก"** (ห้ามให้ UI พูดสิ่งที่ระบบไม่ได้ทำ) · **สามปุ่ม**: **"ยืนยันบันทึกซ้ำ"** = ส่งคำขอเดิมซ้ำด้วย `confirmDuplicate: true` (mirror ของปุ่ม "อัปโหลดต่อไป" ที่ KL-22 ใช้ `checkDuplicate: false` — ใช้ได้เสมอ ไม่มีทางไหนที่ระบบปฏิเสธถาวร) · **"แก้ใบเดิมแทน" ต่อรายการ** (หนึ่งปุ่มต่อหนึ่งแถวเพราะซ้ำได้หลายใบ) — สลับ dialog เดิมเป็น `mode: "edit"` บนแถวนั้น**ในที่เดิม** โดยใช้ `KnowledgeQnAViewModel` ที่มากับ payload 409 ตรงๆ (**ไม่ fetch เพิ่ม ไม่มี endpoint ใหม่ ไม่ออกจากหน้า**) บันทึกด้วย `updateKnowledgeQnA` เดิมตาม KL-14 · **"ยกเลิก"** = ปิด dialog ไม่บันทึกอะไร คำถามในคิวยังอยู่ครบ · **⛔ ห้ามลิงก์ไป `/admin/documents?q=<คำถาม>` เด็ดขาด** — ทางเลือกนี้ตกไปพร้อมมติ Q-H2 แล้ว (เหตุผล: เป็นการค้นหาไม่ใช่ชี้ไปที่แถว, ออกจากหน้าจะทิ้งคำตอบที่พิมพ์ค้าง+การเลือกแถวคิว, ไม่มี requirement ขอให้หน้าคลังอ่าน `q` param) · **ต้องเขียนบอกที่หน้าจอ**: กด "แก้ใบเดิมแทน" แล้วบันทึกสำเร็จ **คำถามในคิวที่ CS เลือกไว้ยังไม่ถูกปิดและยังอยู่ในคิว** (QQ-6 การแก้ไม่แตะคิว — เป็นช่องว่างที่รับรู้แล้ว บันทึกเป็น `O-13`/R7.7 ในอนาคต **ไม่ใช่งานที่ทำตอนนี้ ห้ามผูกคำถามในคิวเข้ากับ Q&A ที่มีอยู่แล้วเป็นผลข้างเคียงของการแก้**) · **ลบ prop `onEditExisting` ออกจาก `KnowledgeQnAAnswerDialog.tsx:27-30` ทั้งหมด** (ไม่เคยถูกส่งจาก call site เดียวที่มีและไม่มีวันถูกส่งตาม KL-25 — ปุ่มตายของ Q-H2 ที่ต้องหายไปจริง ไม่ใช่ย้ายที่) · phasing ไม่เปลี่ยน แก้เฉพาะถ้อยคำ ไม่แตะ checkbox
- [ ] [frontend] เพิ่ม `checkDuplicate: true` เฉพาะที่ฟอร์มอัปโหลดในหน้าคลังรวม `/admin/documents` เท่านั้น (KL-21) — จุดเดียวในระบบที่ส่งค่านี้เป็น `true`
- [ ] [frontend] เพิ่ม dialog เตือนซ้ำเมื่อได้ `409` ตาม KL-22 — ระบุซ้ำแบบไหน (ชื่อ/เนื้อหา/ทั้งสอง), ชื่อไฟล์+ป้ายขอบเขตของใบที่ซ้ำ (ใช้ป้ายชุดเดียวกับ KL-6), วันที่อัปของใบเดิม, ปุ่ม "อัปโหลดต่อไป" (ส่งคำขอเดิมซ้ำด้วย `checkDuplicate: false`) และ "ยกเลิก"; เขียนกำกับไว้ว่าการเตือนครอบเฉพาะเอกสารที่อัปหลังระบบเปิดใช้ความสามารถนี้ (เอกสารเก่า `ContentHash = null` ไม่ถูกจับ)
- [ ] [frontend] แก้ `src/types/domain.ts` — เพิ่ม `UploadDocumentDto.checkDuplicate` (`boolean`, default `false`), type ของ `DuplicateDocumentDto`/409 response, `KnowledgeQnAFilter` · **เพิ่ม 2026-08-25 (มติ Q-H2, ตาม KL-23/KL-26)**: เพิ่ม `CreateKnowledgeQnADto.confirmDuplicate` (`boolean`, default `false`) และ type ใหม่ `DuplicateQnAResponse = { duplicateByQuestion: KnowledgeQnA[] }` (ใช้ type `KnowledgeQnA` ที่มีอยู่แล้วที่ `domain.ts:523-532` ห้ามสร้าง type ใหม่ซ้ำ) — ลบ type ใดๆ ที่เคยถูกเพิ่มไว้แทน `DuplicateWarning`/`KnowledgeQnACreateResultViewModel` เดิมของรอบแรก ถ้ามี
- [ ] [frontend] แก้ `src/lib/api-client.ts` — เพิ่มเมธอดเรียก `GET /api/knowledge-qna`, ปรับเมธอด upload document ให้ส่ง `checkDuplicate` และรองรับ handling `409`
- [ ] [backend] อัปเดต `frontend/docs/API_CONTRACT.md` ให้ตรง wire contract ใหม่ของ `GET /api/documents` (ไม่ส่ง `scopeType` = ทุก scope), `GET /api/knowledge-qna` ใหม่, และ `UploadDocumentDto.checkDuplicate`/409 payload

## Phase 9: Upload consolidation (R8) — delete-only, no gate

**ขึ้นกับ:** Phase 8 (Module H) — **hard ordering constraint (R-18): ต้อง deploy หลัง Phase 8
เสมอ**, ไม่ใช่แค่เขียนโค้ดเรียงกัน — ลบการ์ดโดยที่หน้าคลังยังไม่มี KL-2/KL-4/KL-7 จะทำให้ CS
ไม่เหลือทางเห็นเอกสารระดับบทเรียนจากที่ไหนเลยทั้งระบบ

**ไม่มี migration ไม่มี endpoint ใหม่ ไม่มีการเปลี่ยน wire contract** — งานลบ UI ล้วน

- [ ] [frontend] ลบการ์ด "เอกสารประกอบ" ออกจาก `frontend/src/components/admin/LessonForm.tsx` บรรทัด 679–698 (`CardHeader` + คำอธิบาย + `<DocumentUploadList fixedScope=... primaryDocumentId=... />`) และลบ `import DocumentUploadList` ที่บรรทัด 11 ตามไป (UC-1) — ⚠️ ที่อยู่จริงคือ `LessonForm.tsx` **ไม่ใช่** `app/admin/lessons/[slug]/page.tsx` ที่ `requirement.md` R8.1/R8.3 ระบุไว้ (ที่อยู่ก่อน refactor)
- [ ] [frontend] ลบโหมด `fixedScope` ออกจาก `DocumentUploadList` ทั้งโหมด (UC-2) — ลบ props union (`DocumentUploadList.tsx:131-133`), ตัวแปร `libraryMode`, `activeScope`, `fixedScope ?? ...`, เงื่อนไข `{libraryMode && ...}` ทุกจุด เหลือทางเดียวคือ call site ที่ `/admin/documents`
- [ ] [frontend] ลบ prop `primaryDocumentId` ออกจาก `DocumentUploadList.tsx:132, 356-360` (UC-3) — **ตรวจยืนยันว่า badge ตาม KL-7 (Phase 8) ทำหน้าที่แทนอยู่แล้วที่หน้าคลัง ห้ามลบทั้งสองที่พร้อมกันโดยไม่มี badge รองรับ**
- [ ] [frontend] แทนที่การ์ดที่ถูกลบด้วยบรรทัดสรุปอ่านอย่างเดียวใน `LessonForm.tsx` (UC-6) — "เอกสารประกอบของบทเรียนนี้: N รายการ" + ลิงก์ "ดูในคลังความรู้" โดย N มาจาก `listDocuments({ scopeType: "lesson", scopeId: lesson.id })` ที่มีอยู่แล้ว (`api-client.ts:506`) ไม่ต้องมี endpoint ใหม่ — แสดงเฉพาะตอนแก้บทเรียนที่มีอยู่แล้ว (มี `lesson.id`)
- [ ] [frontend] ลิงก์ "ดูในคลังความรู้" ตาม UC-7 ต้องพา query param มาให้แล้ว — `/admin/documents?scopeType=lesson&scopeId=<lesson.id>` (หน้าคลังอ่าน filter ตั้งต้นจาก URL ตามที่ทำไว้แล้วใน Phase 8)
- [ ] [frontend] ตรวจว่าบรรทัดสรุป UC-6 **ไม่มี** ปุ่มอัปโหลด/ลบ/ย้าย scope/ตารางรายการไฟล์ใดๆ (UC-8) — อ่านอย่างเดียวเท่านั้น
- [ ] [frontend] ตรวจยืนยัน `app/admin/lessons/new/page.tsx` ไม่ต้องแก้อะไร (UC-9) — การ์ดเดิม render เฉพาะเมื่อมี `lesson` (แก้บทเรียนที่มีอยู่) หน้าสร้างใหม่ไม่เคยมีการ์ดนี้อยู่แล้ว และไม่ต้องมีบรรทัดสรุป UC-6 ด้วยเหตุผลเดียวกัน (ยังไม่มี `lesson.id`)

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
ผ่านหมด **⚠️ Phase 9 ต้อง merge เป็นโค้ดจริงก่อน ไม่ใช่แค่มี task list ใน `plan.md`** — ณ วันที่
วางแผน Phase 10 นี้ (2026-08-26) **Phase 9 ยังไม่มีโค้ดเขียนจริงแม้บรรทัดเดียว มีแต่ checkbox ที่ยัง
ไม่ติ๊กสักช่อง** ดังนั้น **ห้าม dispatch งาน `[frontend]` ของ Phase 10 จนกว่า Phase 9 จะขึ้นจริงใน
โค้ด (merged และผ่าน `qa-engineer` แล้ว ไม่ใช่แค่วางแผนไว้)** — งาน `[backend]` ของ Phase 10 ไม่แตะ
`LessonForm.tsx` จึงไม่ติดเงื่อนไขนี้ ทำได้ก่อน · ถ้าจำเป็นต้องสลับลำดับจริง ๆ ทำได้ แต่ต้องเรียงกัน
ห้ามคร่อมกัน

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

- [ ] [backend] `POST /api/lessons/pdf-preview/session` (NR-10) — multipart field `file` เดียว,
  `[RequestSizeLimit(30 * 1024 * 1024)]` ค่าเดียวกับ `DocumentsController` ห้ามตั้งค่าใหม่, เรียก
  `PdfSlidesRenderer.BuildContent(stream, previewId, file.FileName)` ในหน่วยความจำล้วน (ห้ามเขียน
  PostgreSQL/object storage/Pinecone/`BackgroundJob` ในเส้นทางนี้เด็ดขาด), คืน
  `{ previewId, title, pageCount, isLikelyScanned, slides: [{ slideObjectId, index, narrationText }] }`
  ตรงกับ `LessonNarrationSlideViewModel` ทุกฟิลด์ที่ใช้ร่วม (`IsOverridden` ไม่มีในเฟสนี้ ละไปเฉย ๆ
  ไม่ใช่ส่ง `false` ปลอม) — `previewId = IdGenerator.GenerateId("pdfprev")`
- [ ] [backend] เก็บ byte ทั้งไฟล์ + `CompanyId` ของผู้เรียกใน `IMemoryCache` คีย์
  `pdf-preview:{previewId}`, `SlidingExpiration = TimeSpan.FromMinutes(10)` — รูปเดียวกับ
  `pdf-bytes:{documentId}` ที่ `GetPdfBytesAsync` ใช้อยู่แล้ว ห้ามสร้างกลไก cache ที่สอง — **ไม่มี
  endpoint ลบ session** หมดอายุเองทางเดียวเท่านั้น (ห้ามเพิ่ม `DELETE`)
- [ ] [backend] `GET /api/lessons/pdf-preview/{previewId}/pages/{pageNumber:int}` (NR-10) — คืน
  `image/png` จาก `PdfSlidesRenderer.RenderPagePng(stream, pageNumber)` โดยอ่าน byte จาก
  `IMemoryCache` เดียวกับข้างต้น
- [ ] [backend] implement NR-11 — ทั้งสอง endpoint ข้างต้นต้องเทียบ `CompanyId` ที่เก็บไว้กับ
  `CurrentCompanyId` **ก่อน**ใช้ byte แม้แต่ไบต์เดียว, ไม่ตรง **หรือ**หมดอายุ/ไม่มีรายการ →
  `GeneralException.NotFound` ทั้งสองกรณี (ข้อความเดียวกัน ห้ามแยกให้เดาได้ว่า id นี้มีอยู่จริงแต่เป็น
  ของบริษัทอื่น) — `previewId` ห้ามใช้แทน `documentId` ที่ endpoint ใดของระบบ และ `documentId` ห้าม
  ใช้กับ endpoint ของ NR-10 (คนละ id space)
- [ ] [backend] implement NR-5 ในฝั่ง preview session — สูตร `isLikelyScanned` ต้องเป็นสูตรเดียวกับ
  `LessonSlideNarrationService.GetAllAsync` เป๊ะ: `Slides.Count > 0 && Slides.All(s =>
  IsNullOrWhiteSpace(s.SpeakerNotes))`
- [ ] [backend] `GET /api/documents/{documentId}/pdf-pages/{pageNumber:int}` (NR-18) —
  admin-auth, company scope มาจาก query filter ของ `_documentResourceRepository.Get` ตามปกติ (ไม่
  ต้อง `IgnoreQueryFilters()`), ใช้ `RenderPdfPageAsync` เดิมทั้งดุ้น (ห่อบาง ๆ ~10 บรรทัด) — endpoint
  learner-side เดิมที่ผูก link token (`LessonController:69-89`) ห้ามถูกแก้, ห้ามถูกเรียกจากฝั่ง admin
  และห้ามถูกเปิดให้ใช้โดยไม่มี token
- [ ] [backend] unit test (R-12 pattern) — NR-11: อ่าน `pdf-preview:{previewId}` ด้วย `CompanyId`
  ที่ไม่ตรงเจ้าของต้องได้ `NotFound` **เหมือนกันเป๊ะ**กับกรณี id หมดอายุ/ไม่มีอยู่จริง (ข้อความต้อง
  เท่ากันทั้งสองกรณี — พิสูจน์ว่าไม่มีทางเดาได้ว่า id นี้เป็นของบริษัทอื่น)
- [ ] [backend] อัปเดต `frontend/docs/API_CONTRACT.md` ให้ตรง endpoint ใหม่ทั้งสามตัวของ Module J
  (`POST /api/lessons/pdf-preview/session`, `GET /api/lessons/pdf-preview/{previewId}/pages/{n}`,
  `GET /api/documents/{documentId}/pdf-pages/{n}`)
- [ ] [frontend] แก้ `LessonForm.tsx` โหมด `mode="create"` + `contentSourceType === "pdf"` —
  เลือกไฟล์เรียก `POST /api/lessons/pdf-preview/session` แทน `handlePdfUpload` (NR-12/CR-2.a) —
  ไม่มีอะไรถูกเขียนลง server จนกว่าจะกดยืนยันสร้าง — **โหมดแก้ (`isEdit`) ไม่เปลี่ยนพฤติกรรมแม้
  ตัวอักษรเดียว** ยังเรียก `handlePdfFileSelected`/`handlePdfUpload` เดิมทั้งเส้น รวม dialog นับหน้า
  ของ NR-3 (NR-17)
- [ ] [frontend] แก้เงื่อนไข `canCreate` (`LessonForm.tsx:335-341`) สำหรับโหมด `create`+`pdf` — จาก
  "มี `form.pdfDocumentResourceId`" เป็น "มี preview session สำเร็จ" (NR-12)
- [ ] [frontend] แก้ `handleCreate` (`LessonForm.tsx:264-275`) สำหรับโหมด `create`+`pdf` — จากเรียก
  `saveLesson` แล้ว `router.push` ทันที เป็นเปิดเฟสจัดการเนื้อหาใหม่ (R4.6.2) — **โหมด
  `google_slides` คงเดิมทุกตัวอักษร** กด "สร้าง" แล้ว `saveLesson` ทันทีเหมือนวันนี้ (R4.6.9/NR-9)
- [ ] [frontend] สร้าง component ร่วม `SlideNarrationEditorCard` รับ prop `imageSrc: string` (NR-18,
  มติ `Q-J2` = "ใส่ทั้งสองที่") — ใช้ทั้งเฟสจัดการเนื้อหาใหม่และหน้าแก้บทพูดเดิม
- [ ] [frontend] สร้างหน้า/เฟสจัดการเนื้อหาก่อนสร้างบทเรียน PDF — list ทุกหน้าจาก preview session,
  ต่อหน้าแสดงภาพจาก `GET /api/lessons/pdf-preview/{previewId}/pages/{n}` คู่กับ `SlideNarrationEditorCard`
  (textarea แก้บทพูด, prefill จาก `narrationText`), เก็บ draft เฉพาะหน้าที่ CS แตะไว้ใน state ฝั่ง
  client ล้วน (R4.6.4/R4.6.5 — ห้ามยิง server เพื่อ persist ระหว่างเฟส) — แสดงคำเตือน
  `isLikelyScanned` **ทันทีที่เข้าเฟส ไม่ใช่ตอนกดยืนยัน** (NR-5)
- [ ] [frontend] implement NR-15 — ก่อนยืนยันเมื่อเข้าเงื่อนไขใดก็ได้: (ก) ไม่มีหน้าไหนถูกแตะเลย
  (ข) มีหน้าที่ข้อความว่างหลัง trim (รวมเคสไฟล์สแกน) → เตือนพร้อม**จำนวน**หน้า (ไม่ใช่คำเตือนลอย ๆ)
  แล้วให้กดยืนยันซ้ำผ่านได้เสมอ ไม่มีทางไหนที่ระบบปฏิเสธถาวร — คำนวณจาก draft ฝั่ง client ล้วน
  **ห้ามยิง server เพื่อถาม** — ห้ามบังคับเลื่อนดูครบทุกหน้า และห้ามนับ "เลื่อนผ่าน" เป็นการตรวจ
- [ ] [frontend] implement NR-16 — เปลี่ยน/แทนที่ไฟล์ระหว่างอยู่ในเฟส: ล้าง draft ทุกหน้า + ทิ้ง
  preview session เดิม (ปล่อยหมดอายุเอง ไม่เรียก endpoint ใด) + สร้าง session ใหม่จากไฟล์ใหม่ —
  **เงียบ ๆ ไม่มี dialog ไม่มีการนับหน้า** (จงใจต่างจาก NR-3 เพราะยังไม่มีอะไร persist) — ยังอยู่ใต้
  NR-4 เต็มที่: ห้ามพยายามจับคู่หน้าเก่ากับหน้าใหม่
- [ ] [frontend] implement NR-12 — ลำดับยืนยันการสร้าง 4 ขั้นตายตัว **ห้ามสลับ 3↔4 เด็ดขาด** (ดู
  คำเตือนกับดักที่ NR-3 ท้ายข้อ — สลับแล้วบทพูดที่เพิ่ง flush จะถูกลบทิ้งเงียบ ๆ ทั้งชุด): **ขั้น 1**
  `POST /api/lessons` (`contentSourceType: "pdf"`, `pdfDocumentResourceId: undefined`,
  `slideConfigs` จาก preview session, `categoryId`/`isActive` ตามฟอร์ม) → ได้ `lesson.id` **ขั้น 2**
  `POST /api/documents` ด้วย**ไฟล์จริงจากเบราว์เซอร์** (ไม่ใช่ `previewId`) + `scopeType: "lesson"`,
  `scopeId: lesson.id` (NR-14), `checkDuplicate: false` **ขั้น 3** `POST /api/lessons` อีกครั้งใส่
  `pdfDocumentResourceId` จากขั้น 2 (จุดที่ NR-7 index ทั้งเด็คแบบ inline) **ขั้น 4** flush บทพูด
  ทีละหน้าเฉพาะหน้าที่ CS แตะผ่าน `PUT /api/lessons/{id}/narrations/{slideObjectId}` เดิม (NR-2) —
  ต้องมี progress bar (จำนวนขั้น = 3 + จำนวนหน้าที่แก้) · สำเร็จครบ 4 ขั้น → `router.push` ไปหน้า
  แก้บทเรียนของ slug นั้น
- [ ] [frontend] implement NR-13 — สัญญาความล้มเหลวครบ 4 กรณีของ NR-12 ไม่เหลือให้ engineer ตัดสิน:
  **ล้มขั้น 1** → ไม่มีอะไรเกิดขึ้นทั้งระบบ, error ของ server ตรง ๆ, CS อยู่ในเฟสเดิม draft ทุกหน้า
  ยังอยู่ครบ กดยืนยันซ้ำได้ · **ล้มขั้น 2** → มีบทเรียนที่ยังไม่มีไฟล์ **ห้ามลบบทเรียนทิ้งเองโดย
  อัตโนมัติ**, UI บอกตรง ๆ ว่า "บทเรียนถูกสร้างแล้ว แต่อัปไฟล์ไม่สำเร็จ" + ปุ่ม "ลองอัปไฟล์อีกครั้ง"
  ทำต่อจากขั้น 2 (ไฟล์และ draft ยังอยู่ในเบราว์เซอร์ครบ) · **ล้มขั้น 3** → บทเรียน+เอกสารมีแล้วแต่ยัง
  ไม่ผูกกัน, ปุ่ม "ลองอีกครั้ง" ทำต่อจากขั้น 3 **ห้ามอัปไฟล์ใหม่ซ้ำ** · **ล้มขั้น 4 หน้าที่ k** →
  บทเรียนใช้งานได้จริงแล้ว บทพูดเซฟไปบางหน้า, UI บอกจำนวนหน้าที่สำเร็จ/ไม่สำเร็จ + ปุ่มลองซ้ำเฉพาะ
  หน้าที่เหลือ + **ต้องมีลิงก์ "ไปหน้าแก้บทพูด" (`/admin/lessons/{slug}/narrations`) เสมอ** — ทุกกรณี
  ข้างบน: ห้ามลบเอกสารที่อัปสำเร็จไปแล้วโดยอัตโนมัติ
- [ ] [frontend] แก้ `handlePdfUpload` (โหมดแก้ `isEdit`) — ส่ง `scopeType: "lesson"` เสมอตาม NR-14
  (เดิม hardcode `company`) ให้ทั้งสองโหมด (สร้าง/แก้) ใช้ `lesson` scope ตรงกัน — **ไม่แตะ dialog
  นับหน้าของ NR-3 หรือลำดับเรียกอื่นใดของโหมดแก้**
- [ ] [frontend] แก้หน้า `/admin/lessons/[slug]/narrations` — สลับไปใช้ `SlideNarrationEditorCard`
  ร่วม ส่ง `imageSrc` จาก `GET /api/documents/{documentId}/pdf-pages/{pageNumber}` ใหม่ (NR-18) —
  หน้าตาต้องเหมือนกับเฟสสร้างใหม่ตาม R4.6.4
- [ ] [frontend] เพิ่ม type ใน `src/types/domain.ts` — `PdfPreviewSessionResponse`,
  `PdfPreviewSlide` (ไม่มีฟิลด์ `isOverridden`) ตรงกับ response ของ NR-10
- [ ] [frontend] เพิ่มเมธอดเรียก 3 endpoint ใหม่ใน `src/lib/api-client.ts` —
  `createPdfPreviewSession(file)`, `getPdfPreviewPageUrl(previewId, pageNumber)`,
  `getLessonPdfPageUrl(documentId, pageNumber)` (NR-18)

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
  ต้องขึ้นเป็นโค้ดจริงก่อน (merged + ผ่าน `qa-engineer`) ไม่ใช่แค่มี task list ใน `plan.md`** ณ วันที่
  วางแผน Phase 10 (2026-08-26) Phase 9 ยังไม่มีโค้ดเขียนจริงแม้บรรทัดเดียว — **ห้าม dispatch งาน
  `[frontend]` ของ Phase 10 จนกว่า Phase 9 จะขึ้นจริงในโค้ด** งาน `[backend]` ของ Phase 10 ไม่แตะ
  `LessonForm.tsx` จึงไม่ติดเงื่อนไขนี้ ทำก่อนได้
- **Phase 10 🔒 Security gate — 3 เหตุผล** ตาม `design.md` §Module J: (1) endpoint ใหม่ parse/render
  ไฟล์ PDF ที่ยังไม่ persist ในโปรเซสโดยไม่ผ่าน `DocumentParserFactory` — ไฟล์พัง/ไม่ใช่ PDF ต้อง
  ตกเป็น 4xx สะอาดเสมอ (2) company isolation ของ preview session เขียนด้วยมือ (NR-11) — `IMemoryCache`
  ไม่มี query filter ช่วย เป็นของชิ้นแรกที่เก็บเนื้อหาไฟล์บริษัทหนึ่งไว้นอก PostgreSQL ชี้ด้วย id จาก
  request (3) ต้นทุนหน่วยความจำ/CPU ที่ผู้ใช้ล็อกอินแล้วสั่งได้ตรง (ไฟล์ 30MB × session ค้าง 10 นาที
  + PDFium render ทุกหน้าที่เลื่อนดู, R-22)
- Phase 10 ไม่มี migration — ทุก endpoint ใหม่ไม่แตะฐานข้อมูล (NR-10/NR-18); ถ้าระหว่างทำเกิดต้อง
  generate migration ใหม่แปลว่ากำลังทำเกิน contract ให้หยุดแล้วตีกลับ `system-analyst`

## Unresolved Open Questions

ไม่มีคำถามที่บล็อกการเริ่ม Phase 1 — `design.md` ปิดครบ Q1–Q6 แล้วเมื่อ 2026-08-19 หัวข้อ
"🟡 ค้างไว้โดยตั้งใจ" (O-1..O-7) ใน `design.md` ไม่บล็อกงานใด ๆ ในแผนนี้ เพราะทุกจุดมี default
ที่ระบุไว้แล้วให้ implement ตามนั้น (เช่น O-1 → QQ-9 default "ทุกคนแก้/ลบของกันได้")

`Q-J1`/`Q-J2` ของ Module J (Phase 10) เคาะครบแล้วเมื่อ 2026-08-26 (`Q-J1` = "ไม่ขัด — แตะชั่วคราวได้",
`Q-J2` = "ใส่ทั้งสองที่") — ไม่มีคำถามค้างที่บล็อก Phase 10 เช่นกัน

## Change Log

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
