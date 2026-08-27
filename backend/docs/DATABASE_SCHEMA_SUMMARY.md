# SupportRoom Database Schema Summary

เอกสารนี้สรุป persistence model ของ working tree ปัจจุบัน โดยยึด
`ApplicationDbContext`, domain entities และ EF Core model snapshot เป็น source of truth

## ภาพรวมระบบจัดเก็บข้อมูล

ระบบเต็มรูปแบบใช้ข้อมูลถาวร 3 ส่วน แต่มีฐานข้อมูลจริง 2 ระบบ:

| ระบบ | หน้าที่ | จำนวนเชิง logical ต่อ environment |
|---|---|---:|
| PostgreSQL | ข้อมูลธุรกิจ ผู้ใช้ บทเรียน เอกสาร session และงานเบื้องหลัง | 1 database |
| Pinecone | embedding vectors สำหรับ RAG | 1 index แบ่งด้วย namespace |
| Local disk หรือ Huawei OBS | file bytes ของ PDF/DOCX/PPTX และเอกสารอื่น | 1 storage provider; ไม่ใช่ฐานข้อมูล |

ระบบเป็น multi-tenant database: ไม่สร้าง PostgreSQL database แยกต่อบริษัท แต่ใช้
`CompanyId` และ EF Core query filters แยกข้อมูลใน database เดียว

## PostgreSQL inventory

- 17 business tables
- 260 business columns เมื่อนับ audit columns ที่ซ้ำในทุกตารางและ `SlideConfigs` JSONB
- PostgreSQL ที่ apply migration แล้วจะมี `__EFMigrationsHistory` เพิ่มอีก 1 internal table
- ความสัมพันธ์ข้าม entity ส่วนใหญ่เป็น logical string IDs และไม่มี PostgreSQL foreign-key constraint

### Audit columns

ทุก business table มีคอลัมน์ต่อไปนี้:

| Column | Type | Nullable | ความหมาย |
|---|---|---:|---|
| `Id` | `text` | no | Primary key |
| `CreateBy` | `text` | yes | ผู้สร้าง หรือ null เมื่อระบบสร้าง |
| `CreateDate` | `timestamptz` | no | เวลาสร้าง |
| `UpdateBy` | `text` | yes | ผู้แก้ล่าสุด |
| `UpdateDate` | `timestamptz` | yes | เวลาแก้ล่าสุด |
| `DeleteBy` | `text` | yes | ผู้ทำ soft delete |
| `IsDelete` | `boolean` | no | soft-delete flag |
| `DeletedAt` | `timestamptz` | yes | เวลา soft delete |

ทุกตารางยกเว้น `Company` มี `CompanyId text` ด้วย โดย `AdminUser.CompanyId` nullable สำหรับ
`owner`; ตารางอื่นเป็น non-null

**`AuditLog` เป็นข้อยกเว้นเดียวของรูปแบบข้างบนทั้งหมด** — append-only, ไม่มี audit columns ชุดนี้
เลย (ไม่มี `UpdateBy`/`UpdateDate`/`DeleteBy`/`IsDelete`/`DeletedAt`) มีแค่ `Id`/`CompanyId` +
คอลัมน์เฉพาะของตัวเอง ดูรายละเอียดที่ `## บันทึกประวัติการกระทำ (Audit)` ด้านล่าง

ในรายการด้านล่าง คำว่า “columns เฉพาะตาราง” หมายถึงคอลัมน์นอกเหนือจาก audit columns ด้านบน

## บริษัทและสิทธิ์ผู้ดูแล

### `Company` — 13 columns

Columns เฉพาะตาราง:

- `Name text`
- `IsActive boolean`
- `DefaultIntroWaitMs integer`
- `DefaultBreathPauseMs integer`
- `DefaultFinalQuestionWaitMs integer`

Indexes: `IsActive`

### `AdminUser` — 16 columns

Columns เฉพาะตาราง:

- `CompanyId text?`
- `Role text` — `owner | admin | cs`
- `Email text` — unique ทั้งระบบ
- `PasswordHash text?`
- `DisplayName text`
- `IsActive boolean`
- `LastLoginAt timestamptz?`
- `MustChangePassword boolean`

Indexes: `CompanyId`, unique `Email`

## หมวดหมู่ บทเรียน และเนื้อหา

### `KnowledgeCategory` — 15 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `ParentId text?`
- `Level integer`
- `Name text`
- `Description text?`
- `SortOrder integer`
- `IsSystemDefault boolean`

Indexes: `CompanyId`, `(CompanyId, ParentId, SortOrder)`

### `LessonConfig` — 22 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `Slug text`
- `CategoryId text`
- `Title text`
- `Description text?`
- `SlidesSourceUrl text`
- `PresentationId text?`
- `SlidesEmbedUrl text?`
- `ContentSourceType text` — `google_slides | pdf`
- `PdfDocumentResourceId text?`
- `SlideConfigs jsonb`
- `IsActive boolean`
- `PurgeJobId text?`
- `PurgeStartedAt timestamptz?`

`SlideConfigs` เป็น JSON array โดยแต่ละ object มี `slideObjectId text`, `slideIndex integer` และ
`videoDurationMs integer?`

Indexes: `CategoryId`, unique `(CompanyId, Slug)`, `(CompanyId, IsDelete, DeletedAt)`

### `LessonSlideNarration` — 12 columns

Columns เฉพาะตาราง: `CompanyId text`, `LessonId text`, `SlideObjectId text`, `NarrationText text`

Indexes: `CompanyId`, `(LessonId, SlideObjectId)`

### `LessonExcludedSlide` — 11 columns

Columns เฉพาะตาราง: `CompanyId text`, `LessonId text`, `SlideObjectId text`

Indexes: `CompanyId`, `(LessonId, SlideObjectId)`

## เอกสารและงานเบื้องหลัง

### `DocumentResource` — 20 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `ScopeType text` — `lesson | category | company`
- `ScopeId text?` — null เมื่อ scope เป็น company
- `FileName text`
- `ContentType text`
- `SizeBytes bigint`
- `ObsBucket text`
- `ObsKey text`
- `IndexingStatus text` — `pending | indexed | failed`
- `IndexedChunkCount integer`
- `FailureReason text?`
- `ContentHash text?`

Indexes: `(CompanyId, ScopeType, ScopeId)`, `(CompanyId, ContentHash)`

### `DocumentChunk` — 17 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `DocumentId text`
- `ChunkKey text`
- `VectorId text`
- `NamespaceKey text`
- `SeqNo integer`
- `Text text`
- `CharCount integer`
- `HasSuspectCharacters boolean`

Indexes: `CompanyId`, `(DocumentId, SeqNo)`

### `BackgroundJob` — 19 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `JobType text`
- `TargetId text`
- `PayloadJson text?`
- `Status text` — `pending | running | succeeded | failed | canceled`
- `AttemptCount integer`
- `NextAttemptAt timestamptz`
- `StartedAt timestamptz?`
- `FinishedAt timestamptz?`
- `LastErrorCode text?`
- `LastErrorDetail text?`

Indexes: `(Status, NextAttemptAt)`, `(CompanyId, JobType, TargetId)`

`TargetId` เป็น polymorphic logical reference ตาม `JobType`; ปัจจุบันชี้ไป document, lesson หรือ
Q&A ได้ จึงไม่มี foreign key เดียวที่ database บังคับได้

## คลังความรู้แบบ Q&A

### `KnowledgeQnA` — 17 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `Question text`
- `Answer text`
- `ScopeType text`
- `ScopeId text?`
- `VectorId text`
- `IndexedNamespaceKey text?`
- `IndexingStatus text`
- `FailureReason text?`

Index: `(CompanyId, ScopeType, ScopeId)`

### `KnowledgeQnASource` — 11 columns

Columns เฉพาะตาราง: `CompanyId text`, `QnAId text`, `SessionQuestionId text`

Indexes: `QnAId`, `(CompanyId, SessionQuestionId)`

### `KnowledgeQnAConflict` — 15 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `QnAId text`
- `SessionQuestionId text?`
- `ConflictingSourceLabel text`
- `ModelNote text?`
- `ResolvedAt timestamptz?`
- `ResolvedBy text?`

Indexes: `QnAId`, `(CompanyId, ResolvedAt)`

## ลิงก์และประวัติการเรียน

### `TrainingLink` — 15 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `Token text` — unique ทั้งระบบ
- `LessonId text`
- `LessonSlug text`
- `RecipientOrgName text?`
- `ExpiresAt timestamptz`
- `MaxAttendees integer?`

Indexes: `CompanyId`, unique `Token`

### `LearningSession` — 20 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `TrainingLinkId text`
- `LearnerKey text`
- `RecipientName text`
- `Status text` — `IN_PROGRESS | ENDED`
- `StartedAt timestamptz`
- `EndedAt timestamptz?`
- `LastActivityAt timestamptz`
- `LastSlideObjectId text?`
- `LastSlideIndex integer?`
- `TotalSlideCount integer?`
- `CompletedAllSlides boolean`

Indexes: `CompanyId`, `(TrainingLinkId, LearnerKey)`

### `SessionQuestion` — 18 columns

Columns เฉพาะตาราง:

- `CompanyId text`
- `SessionId text`
- `SlideObjectId text?`
- `Transcript text?`
- `Answer text?`
- `AnswerStatus text`
- `Source text` — `voice | text`
- `ReviewResult text?` — `correct | incorrect`
- `ReviewNote text?`
- `ReviewedAt timestamptz?`

Indexes: `CompanyId`, `SessionId`, `(CompanyId, AnswerStatus)`, `(CompanyId, ReviewResult)`

### `SessionQuestionReviewExclusion` — 12 columns

Columns เฉพาะตาราง: `CompanyId text`, `SessionQuestionId text`, `LessonId text`, `Reason text`

Indexes: `(CompanyId, LessonId)`, unique `(CompanyId, SessionQuestionId)`

## บันทึกประวัติการกระทำ (Audit)

### `AuditLog` — 7 columns

ตารางเดียวในระบบที่**ไม่**ใช้ audit columns มาตรฐานด้านบน — append-only ล้วน ไม่มี `Update`/`Delete`
เกิดขึ้นกับแถวในตารางนี้เลย (audit-trail module, มติ Q-A1)

Columns ทั้งหมด:

- `Id text` PK
- `CompanyId text?` — บริษัทของ**ข้อมูลที่ถูกกระทำ**, `null` = ระดับระบบ (ไม่ใช่บริษัทของคนที่ลงมือ)
- `ActorUserId text` — `AdminUser.Id`, ไม่มีวันเป็น null
- `Action text` — `create | update | delete`
- `EntityName text` — ชื่อคลาส CLR ของ entity ที่ถูกกระทำ
- `EntityId text` — primary key ของแถวที่ถูกกระทำ
- `OccurredAt timestamptz`

Indexes: `(CompanyId, OccurredAt)`, `(EntityName, EntityId)`, `ActorUserId` — **ไม่มี** unique
constraint และ**ไม่มี** foreign key ไปยังตารางอื่นเลย (`ActorUserId`/`EntityName`/`EntityId` เป็น
logical string id ตาม convention ของโปรเจกต์)

ไม่เก็บ before/after diff และไม่มี `MetadataJson` (R2/มติ Q-A2) — ตอบได้แค่ "ใครแก้แถวไหนตอนไหน"

## Pinecone record shape

Pinecone ไม่มี table/column แบบ relational แต่แต่ละ record มี:

- `id`
- `values` — embedding vector
- `namespace`
- `metadata.__text`
- metadata เฉพาะแหล่งข้อมูล:
  - slide: `slideObjectId`, `index`, `sourceType=slide`
  - document: `documentId`, `chunkId`, `fileName`, `sourceType=document`
  - Q&A: `qnaId`, `sourceType=qna`

Namespace keys:

- lesson: `{companyId}:{lessonSlug}`
- category: `{companyId}:{categoryId}`
- company-wide: `{companyId}:kb-global`

## File storage shape

ไฟล์จริงอยู่ใน provider ที่เลือกด้วย `DOCUMENT_STORAGE_PROVIDER=local|huawei-obs` ไม่มี relational
columns เพิ่มใน provider นั้น PostgreSQL ใช้ `DocumentResource.ObsBucket` และ
`DocumentResource.ObsKey` เป็น pointer

## Isolation และ query filters

- `Company`, `AdminUser` ไม่มี global query filter เพราะต้องอ่านก่อน resolve company
- `BackgroundJob` มี `CompanyId` แต่ไม่มี global query filter เพราะ worker ต้อง claim งานข้ามบริษัท
- `AuditLog` มี `CompanyId` แต่ไม่มี global query filter เพราะค่าเป็น `null` ได้ (แถวระดับระบบ) —
  filter จะทำให้แถวเหล่านั้นหายไปจากทุกคนตลอดกาล (audit-trail module, มติ OQ-2) — ต่างจาก
  `BackgroundJob` ตรงที่นี่ไม่ใช่ปัญหา bootstrap ชั่วคราว แต่เป็นการไม่มี filter ถาวรโดยเจตนา
- อีก 13 tables มี company query filter
- ตารางที่ใช้ soft-delete ใน normal UI เพิ่มเงื่อนไข `!IsDelete` ตาม configuration ของแต่ละ entity
- query ของ `BackgroundJob`, `Company`, `AdminUser` และ `AuditLog` ต้องบังคับ authorization/scoping
  ที่ service หรือ repository เอง

## SQL creation and migration scripts

- [`supportroom-schema.sql`](./supportroom-schema.sql) — DDL ของ current model แบบสะอาด สร้าง
  17 business tables และ 36 indexes แต่ไม่สร้าง `__EFMigrationsHistory`; เหมาะกับการอ่าน schema,
  test database ชั่วคราว หรือกรณีที่ไม่ให้ EF migrations ดูแลฐานนั้นต่อ
- [`supportroom-migrations-idempotent.sql`](./supportroom-migrations-idempotent.sql) — replay EF
  migrations ทั้งหมดแบบ idempotent รวม `__EFMigrationsHistory`, data backfills และ migration ล่าสุด;
  ใช้ตัวนี้สำหรับฐานว่าง/ฐานเดิมที่ต้องให้ EF migrations ดูแลต่อ

ตัวอย่างรัน PostgreSQL โดยหยุดทันทีเมื่อมีข้อผิดพลาด:

```powershell
psql -v ON_ERROR_STOP=1 -d supportroom -f backend/docs/supportroom-migrations-idempotent.sql
```

ห้ามรัน clean schema script แล้วตามด้วย migration script บน database เดียวกัน เพราะ clean script
ไม่มี migration history และ EF จะพยายามสร้าง objects เดิมซ้ำ

## Authoritative sources

- `backend/src/SupportRoom.Providers.Data/Data/ApplicationDbContext.cs`
- `backend/src/SupportRoom.Providers.Data/Migrations/ApplicationDbContextModelSnapshot.cs`
- `backend/src/SupportRoom.Domain/Entities/`

Derived documentation:

- `backend/docs/supportroom.dbml`
- `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`
- `backend/docs/supportroom-schema.sql`
- `backend/docs/supportroom-migrations-idempotent.sql`

เมื่อ schema เปลี่ยน ต้องสร้าง EF migration ใหม่ แล้ว regenerate SQL scripts และอัปเดต summary,
DBML และ ER diagram ให้ตรงกับ model snapshot รุ่นใหม่
