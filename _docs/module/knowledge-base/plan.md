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

## Unresolved Open Questions

ไม่มีคำถามที่บล็อกการเริ่ม Phase 1 — `design.md` ปิดครบ Q1–Q6 แล้วเมื่อ 2026-08-19 หัวข้อ
"🟡 ค้างไว้โดยตั้งใจ" (O-1..O-7) ใน `design.md` ไม่บล็อกงานใด ๆ ในแผนนี้ เพราะทุกจุดมี default
ที่ระบุไว้แล้วให้ implement ตามนั้น (เช่น O-1 → QQ-9 default "ทุกคนแก้/ลบของกันได้")

## Change Log

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
