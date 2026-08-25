# คลังความรู้และขาเข้าสื่อการสอน (knowledge-base) — Feasibility & Design

> ## ✅ สถานะ: ยืนยันแล้ว — **`## Data Model` คือ contract**
>
> เจ้าของโปรเจกต์เคาะครบทั้ง 6 ข้อ (Q1–Q6) เมื่อ **2026-08-19** — 5 ข้อยืนยันตรงตามข้อเสนอเดิม
> มีข้อเดียว (Q6) ที่เจ้าของโปรเจกต์ทักท้วงข้อเสนอเดิมและเลือกทิศทางที่ถูกกว่า (ดู `## Unresolved
> Open Questions` สำหรับรายละเอียดมติแต่ละข้อ) `backend-engineer` implement ตรงตัวได้ทันที
> ตาม `.claude/shared/conventions.md` §6 ข้อ 2 และ §7 — **พร้อมส่งต่อ `project-manager`**
>
> **amendment 2026-08-20 — เพิ่ม Module G / Phase 7 (DS-1..DS-12) ยืนยันแล้ว**: `qa-engineer` FULL
> รอบแรกพบว่า R3 ถูกสร้างครบเฉพาะฝั่งอ่าน — ไม่มีทางเซ็ต `DocumentResource.ScopeType = "category"`
> ได้เลยทั้ง 6 phase · เจ้าของโปรเจกต์เคาะทิศทางครบทุกข้อ (Q-A..Q-G) เมื่อ **2026-08-20**
> **ไม่มีการเปลี่ยน Data Model และไม่มี migration ใหม่** — Phase 1–6 ที่ implement ไปแล้วไม่ต้องแก้
> ย้อนหลัง ยกเว้น wire contract ของ `/api/documents` ที่เปลี่ยนพร้อมกันในเฟสเดียว (R-15) ·
> **พร้อมส่งต่อ `project-manager` เพื่อวาง Phase 7**

**โมดูล:** `knowledge-base` · **ที่มา:** `_docs/module/knowledge-base/requirement.md` (R1–R6, P1–P9)
**Stack ที่ตรวจแล้ว:** ASP.NET Core .NET 10 + EF Core/PostgreSQL + Pinecone + Next.js 15
(ตาม root `CLAUDE.md` — โปรเจกต์นี้ไม่ได้ใช้ Prisma กฎ `schema.prisma` ใน conventions §7
ให้อ่านเทียบเป็น entity + EF migration ของจริง)

---

## Feasibility Summary

**R1–R6 ทำได้ทั้งหมดด้วย stack เดิม ไม่ต้องเพิ่ม dependency หรือ external service ใหม่แม้แต่ตัวเดียว** —
ตรวจถึงระดับโค้ดแล้วทั้ง 6 ข้อ: taxonomy และ Q&A เป็นตารางใหม่ใน PostgreSQL ล้วน · คลังความรู้
ระดับที่สาม (R3) เป็นแค่ namespace key แบบใหม่บน Pinecone index เดิม ไม่ต้องสร้าง index ใหม่ ·
การลบ vector ทีละชุด (R6.1) ใช้ `POST /vectors/delete` ที่มี field `ids` อยู่แล้วใน Pinecone REST API
เดิม แค่ `PineconeKnowledgeIndexProvider` ยังไม่ได้เปิดใช้ (วันนี้ hardcode `deleteAll: true`) ·
คิวถาวร (R6.2) เป็นตาราง + polling ใน `QueuedHostedService` เดิม ไม่ต้องมี Redis/RabbitMQ

**ราคาที่ต้องจ่ายจริงอยู่ที่ 3 จุด ไม่ใช่ที่เทคโนโลยี:**

1. **R1.1 บังคับให้ `LessonConfig.CategoryId` เป็น required** = breaking change กับข้อมูลที่มีอยู่แล้ว
   ต้อง backfill ไม่มีทางเลี่ยง (ดู `## Migration Plan` และ Q3 ใน Open Questions)
2. **R6.1 ต้องใช้ soft delete จริงเป็นครั้งแรกในโปรเจกต์นี้** — วันนี้ `IsDelete`/`DeletedAt` มีอยู่ในทุก
   entity แต่ `RepositoryBase.Delete` เรียก `_set.Remove()` คือลบจริงทุกครั้ง และไม่มี global query
   filter ที่ไหนเลย (ตรงกับที่ `CLAUDE.md` §Known Baseline บันทึกไว้) การทำ soft delete จึงเป็นงาน
   ใหม่จริง ไม่ใช่แค่ "เปลี่ยนบรรทัดเดียว"
3. **R6.2 ไม่ใช่การย้ายที่เก็บคิว แต่เป็นการเปลี่ยนรูปงาน** — `BackgroundTaskQueue` วันนี้เก็บ
   `Func<IServiceProvider, CancellationToken, Task>` ที่ **จับ `byte[]` ของไฟล์ทั้งไฟล์ไว้ใน closure**
   (`IDocumentResourceService.UploadAsync` ส่ง `input.Content` เข้าไป) closure serialize ลง DB ไม่ได้
   งานที่ persist ได้ต้องเก็บแค่ id แล้ว **โหลดไฟล์ใหม่จาก storage ตอนถึงคิว**

**ข้อเท็จจริงที่ต้องแก้ไปพร้อมกันโดยไม่มีทางเลี่ยง:** บทเรียน `ContentSourceType = "pdf"` วันนี้
**ไม่เคยถูก index เข้า namespace ของตัวเองเลย** — `ILessonConfigService.SaveAsync` เรียก
`IndexLessonAsync` ใน `if (!string.IsNullOrEmpty(presentationId))` เท่านั้น ยืนยันจากโค้ดแล้ว
R4 ข้อ 5 (แก้บทพูดแล้วต้องมีผลกับคำตอบ) จึงบังคับให้ต้องเปิดเส้นทาง index ของบทเรียน PDF ขึ้นมา
ก่อน ไม่งั้น "re-index หน้าที่แก้" ไม่มีอะไรให้ re-index

---

## Feature-by-Feature Feasibility

| # | ความต้องการ | คำตัดสิน | หมายเหตุ |
|---|---|---|---|
| R1 | Taxonomy 2 ชั้นต่อบริษัท ใช้กับบทเรียน+เอกสาร | **ทำได้ตรงไปตรงมา** | ตาราง `KnowledgeCategory` self-referencing + คอลัมน์ `CategoryId` บน `LessonConfig` · ไม่มีอะไรใหม่เชิงเทคนิค |
| R1.1 | บทเรียนต้องมีหมวดเสมอ | **ทำได้ แต่เป็น breaking change** | `CategoryId` เป็น `required` บนตารางที่มีข้อมูลอยู่แล้ว → ต้อง backfill (Q3) |
| R1.2 | ลบหมวดที่มีของอยู่ไม่ได้ | **ทำได้ตรงไปตรงมา** | validation ใน service layer · แต่ตัวกติกา**ยังไม่ยืนยัน** (Q2) |
| R2 | แต่ละบริษัทจัดหมวดเอง | **ทำได้ตรงไปตรงมา** | `ICompanyScoped` + `HasQueryFilter` แบบเดียวกับทุก entity เดิม ไม่มีอะไรพิเศษ |
| R3 | คลังความรู้ 3 ระดับ (ฝั่ง**อ่าน**) | **ทำได้ ไม่ต้องเพิ่ม service** | เพิ่ม `KnowledgeNamespaces.ForCategory` + ยิง query เพิ่ม 1 namespace ต่อคำถาม · ต้นทุนคือ latency + Pinecone read unit ต่อคำถาม ไม่ใช่ dependency ใหม่ (ดู Risks) |
| R3-W | คลังความรู้ 3 ระดับ (ฝั่ง**เขียน** — CS วางเอกสารไว้ระดับหมวดได้จริง) | **ทำได้ ไม่ต้องแตะ schema เลย** | เพิ่ม 2026-08-20 หลัง QA FULL รอบแรกพบว่า `ScopeType = "category"` ไม่มีทางถูกเซ็ตได้เลยทั้ง 6 phase (ไม่มี DTO/endpoint/UI ไหนรับ scope จาก CS) — `DocumentResource.ScopeType/ScopeId` มีอยู่แล้วตาม DM-3 และ `EnsureValidScope`/`Resolve`/`GetByScope`/TX-6/TX-10 ถูกเขียนรอไว้ครบแล้ว สิ่งที่ขาดคือ **ทางเขียน** ล้วนๆ · ดู `## Document Scope Assignment Rules` (DS-1..DS-12) และ Module G |
| R3.1 | เตือนก่อนย้ายหมวด พร้อมตัวเลข | **ทำได้ตรงไปตรงมา** | นับจาก `DocumentResource`/`KnowledgeQnA` ที่ scope อยู่หมวดเก่า/ใหม่ · **ย้ายหมวดไม่ต้อง re-index อะไรเลย** เพราะ namespace ของบทเรียนผูกกับ slug ไม่ใช่หมวด — ราคาถูกกว่าที่ requirement เผื่อไว้ |
| R4 | บทพูด PDF แก้ได้ต่อหน้า | **ทำได้ แต่มีงานแฝง** | ตาราง `LessonSlideNarration` เอง trivial · งานแฝงคือต้องเปิดเส้นทาง index ของบทเรียน PDF ที่วันนี้ไม่มี (ดู Feasibility Summary) |
| R4.4 | เตือนไฟล์สแกน ไม่ทำ OCR | **ทำได้ตรงไปตรงมา** | ตรวจตอนสร้าง narration prefill: ทุกหน้าได้ข้อความว่าง → ตอบกลับเป็น flag ไม่ใช่ error |
| R5 | Q&A เป็นคลังความรู้ | **ทำได้ แต่ต้องขยาย 1 interface** | `KnowledgeSourceChunk` ต้องเพิ่ม `EmbedText` (embed คำถาม แต่เก็บ ถาม+ตอบ) — เป็นการเพิ่ม property optional ไม่ใช่ dependency ใหม่ |
| R5.3 | ห้ามคัดลอก Q&A มาตอบตรงๆ | **ทำได้แค่ระดับ prompt** | ไม่ใช่กลไกบังคับได้เหมือนโค้ด — requirement ยอมรับข้อจำกัดนี้ไว้แล้ว (R5.5) ยกไป Risks |
| R5.4 | CS เลือก scope ตอนบันทึก | **ทำได้ตรงไปตรงมา** | ใช้ `KnowledgeScopeType` ตัวเดียวกับ `DocumentResource` |
| R5.5 | เอกสารชนะ + ยกธง | **ทำได้ แต่พึ่งการตัดสินของโมเดล** | ต้องให้ provider คืน field `conflict` เพิ่มใน structured output · โมเดลเป็นคนตัดสินว่าอะไรคือ "ขัดกัน" ตามที่ requirement ระบุไว้เอง |
| R5.6 | รู้ผู้เขียน/เวลา/แก้ลบได้ | **ทำได้ตรงไปตรงมา และดีกว่าที่เผื่อไว้** | ระบบ**มี auth แล้ว** (`AdminUser` + `AdminRole`) `CreateBy`/`UpdateBy` มีค่าที่เชื่อถือได้ใส่จริง — ต่างจากตอนออกแบบ `learning-session` ที่ยังไม่มี |
| R5.7 | `cs` เขียนแล้วใช้ได้ทันที | **ทำได้ (คือไม่ต้องทำอะไร)** | ไม่มี approval flow = ไม่มีโค้ด · ความเสี่ยงยกไป Risks ตามที่ requirement ชดเชยด้วย R5.6 |
| R6.1 | soft delete + ลบ vector จริง | **ทำได้ แต่ soft delete เป็นงานใหม่จริง** | `RepositoryBase.Delete` = hard delete วันนี้ · Pinecone รองรับ `ids` ใน `/vectors/delete` อยู่แล้ว |
| R6.2 | คิวลง DB + ทำงานค้างต่อ | **ทำได้ ไม่ต้องเพิ่ม infra** | ตาราง + polling · **ห้ามเพิ่ม Redis/Hangfire** — instance เดียว ยังไม่ต้องการ (ดู Risks R-6) |
| R6.3 | ดูข้อความที่แปลงได้ตลอด | **ทำได้ แต่มีทางเลือกที่ต้องเคาะ** | เก็บ chunk ลง DB vs re-parse ตอนดู (Q5) — กระทบ schema โดยตรง |
| R6.4 | สถานะล้มเหลวบอกสาเหตุ | **ทำได้ตรงไปตรงมา** | คอลัมน์ `FailureReason` + `static class` const ตาม convention เดิม |
| P9 | ไม่มี UI สร้างบทเรียน | **ทำได้ แต่รอเคาะ scope** | ผูกกับ R1.1 ตามที่ requirement ระบุ (Q4) |

**ไม่มีข้อไหนที่อยู่นอก stack ปัจจุบัน และไม่มีข้อไหนที่ต้องเพิ่ม dependency** — ยืนยันตามที่
`requirement.md` คาดไว้

### การตัดสินใจที่ผู้ใช้ยืนยันแล้ว

ตารางนี้คือมติที่ **เจ้าของโปรเจกต์เคาะไปแล้ว** และเป็นข้อจำกัดตายตัวของการออกแบบนี้ —
agent ปลายทางอ่านตารางนี้เพื่อไม่ต้องรื้อถามใหม่ (`.claude/shared/conventions.md` §10)

| คำถาม | เลือกอะไร | ตัดอะไรทิ้ง | เมื่อไหร่ |
|---|---|---|---|
| หมวดเป็นป้ายกำกับหลายอัน หรือโฟลเดอร์อันเดียว | **โฟลเดอร์อันเดียว** — 1 บทเรียน 1 หมวด และต้องมีเสมอ (R1.1) | tagging หลายหมวดต่อบทเรียน · ความรู้ระดับหมวดที่กำกวมว่าเป็นของหมวดไหน | 2026-08-19 |
| ใครกำหนดชุดหมวด | **แต่ละบริษัทกำหนดเอง** (R2) | ชุดหมวดกลางของ School Bright · รายงานเทียบข้ามบริษัท | 2026-08-19 |
| บทพูด Google Slides แก้ในระบบได้ไหม | **ไม่ได้ ไปแก้ที่ Google Slides** (R4) | บทพูดสองที่ที่ขัดกัน · การละเมิดกฎสถาปัตยกรรมข้อ 8 | 2026-08-19 |
| บทพูด PDF เก็บยังไง | **เก็บเฉพาะหน้าที่ CS แก้จริง** ห้ามเซฟ prefill (R4.1) | การแช่แข็งสำเนาที่แปลงพังไว้ถาวร | 2026-08-19 |
| อัปโหลด PDF ใหม่ทำยังไงกับบทพูดเดิม | **ล้างทิ้งทั้งหมด + เตือนจำนวนหน้าก่อน** (R4.3) | การพยายามจับคู่หน้าเก่ากับหน้าใหม่ ซึ่งเลื่อนผิดเงียบๆ ได้ | 2026-08-19 |
| PDF สแกนทำยังไง | **เตือนให้พิมพ์เอง ไม่ทำ OCR** (R4.4) | dependency OCR ทั้งหมดในเฟสนี้ | 2026-08-19 |
| ปิดงานคำถามยังไง | **เกิดเองเมื่อมีคำตอบ ไม่มีปุ่ม "แก้แล้ว"** (R5.2) | workflow status · การส่งงานต่อระหว่างคน | 2026-08-19 |
| Q&A ขัดกับเอกสาร ใครชนะ | **เอกสารชนะ + ยกธงให้ CS** (R5.5) | การให้ Q&A ทับเอกสารเงียบๆ | 2026-08-19 |
| `cs` ต้องรออนุมัติไหม | **ไม่ต้อง ใช้ได้ทันที** (R5.7) | approval flow ทั้งชุด · แลกกับความเสี่ยงที่คำตอบผิดถูกใช้ทันที | 2026-08-19 |
| ลบเอกสารในฐานข้อมูลกับใน Pinecone | **soft delete ใน DB · ลบจริงใน Pinecone** (R6.1) | soft delete ฝั่ง vector ซึ่งไม่มีความหมายเพราะไม่มี query filter | 2026-08-19 |
| ชื่อ entity ของ "ลิงก์" | **`TrainingLink` ตามโค้ดจริง ไม่ rename** | มติ Q2 เดิมใน `learning-session/design.md` ที่จะ rename เป็น `LessonLink` — **ยกเลิกแล้ว ห้ามอ้างอิง** | 2026-08-18 |
| เอกสารที่อัปไปแล้วย้าย scope ได้ไหม (Q-A) | **ย้ายได้ — `PATCH /api/documents/{id}/scope`** (DS-5..DS-7) เติม call site ให้ KS-4 ที่เขียนไว้ตั้งแต่ต้นแต่ไม่เคยมีใครเรียก | ทางเลือก "อัปผิดแล้วต้องลบทิ้งอัปใหม่" ซึ่งทำให้เสียประวัติเอกสารและ `DocumentChunk` ทั้งชุด | 2026-08-20 |
| `UploadDocumentDto` คง `LessonSlug` ไว้คู่กับ scope ใหม่ไหม (Q-B) | **ลบ `LessonSlug` ทิ้ง ใช้ `ScopeType`/`ScopeId` อย่างเดียว** (DS-1) | ความเข้ากันได้ย้อนหลังของ wire contract — ยอมแลกเพราะมี caller แค่ 3 จุดในโปรเจกต์ ไม่มี client ภายนอก · เหตุผลเดียวกับที่ DM-3 ลบ `LessonId` ทั้งใบ: สองช่องทางที่แปลว่า scope เหมือนกันต้องตรวจซ้ำทุก call site และไม่มีอะไรบังคับได้ | 2026-08-20 |
| หน้าบทเรียนควรมี scope picker ด้วยไหม (Q-C) | **ไม่มี — คงเป็น `lesson` ของบทเรียนนั้นเสมอ** (DS-8) | การอัปเอกสารของหมวดอื่น/บริษัทจากหน้าบทเรียน ซึ่งเปิดทางให้วางผิดที่โดยไม่มีเหตุผลทางธุรกิจรองรับ | 2026-08-20 |
| งานนี้เข้า phase ไหน (Q-F) | **Phase 7 ใหม่ พร้อม 🔒 Security gate** (Module G) | การยัดกลับเข้า Phase 1 ซึ่งไม่ติด gate และ QA ปิดไปเกือบหมดแล้ว — จะทำให้สถานะ verify ของ Phase 1 กำกวม | 2026-08-20 |
| เอกสารหนึ่งใบอยู่หลายหมวดพร้อมกัน (Q-G) | **ไม่ทำ — `ScopeId` ช่องเดียวต่อไป** บันทึกเป็น O-9 deferred ที่รู้ตัว | ตารางเชื่อม many-to-many และการรื้อ `EnsureValidScope`/`Resolve`/`vector_delete` ทั้งชุด · **ถ้าวันหนึ่งต้องการจริง ต้อง amend ก่อน — retrofit แพงกว่าทำตั้งแต่ต้นมาก** | 2026-08-20 |

---

## Data Model

> **นี่คือ contract ที่ `backend-engineer` implement ตรงตัว** (หลังยืนยัน) — ชื่อ entity ชื่อฟิลด์
> ชนิด nullability index และ query filter ตามนี้เป๊ะ ห้ามเพิ่ม/ลด/เปลี่ยนชื่อเอง ถ้าขาดอะไร
> ให้ตีกลับมาที่ `system-analyst`
>
> **ทุกฟิลด์ที่ขึ้นต้นด้วย ⚠️ คือฟิลด์ที่กติกาจะเปลี่ยนถ้า Open Question ที่อ้างถึงถูกเคาะไปอีกทาง**

### DM-1 · `KnowledgeCategory` (ตารางใหม่) — หมวด 2 ชั้น (R1 · R2)

```csharp
using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// หมวดของบริษัทหนึ่ง - ตารางเดียวเก็บทั้งสองชั้น (category > subcategory) ผ่าน ParentId
/// เหตุผลที่ไม่แยกสองตาราง: DocumentResource/KnowledgeQnA อ้างหมวดด้วย ScopeId ช่องเดียว
/// ถ้าแยกสองตารางจะต้องมี ScopeType เพิ่มอีกค่าเพื่อบอกว่า id นั้นมาจากตารางไหน แล้ว
/// resolver ของ namespace จะแตกเป็นสองทางทุกจุดที่เรียก
///
/// ลึกได้แค่ 2 ชั้นเท่านั้น - ดู "Taxonomy Rules" ข้อ TX-2 ห้ามให้แถว Level 2 เป็นพ่อของใคร
/// </summary>
public sealed class KnowledgeCategory : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("kbcat")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>null = หมวดชั้นบน (category) · มีค่า = หมวดย่อย (subcategory)
    /// logical FK ชี้ไป KnowledgeCategory ด้วยกันเอง ไม่มี FK constraint จริง ตามแบบแผนเดิม</summary>
    public string? ParentId { get; init; }

    /// <summary>1 = category · 2 = subcategory · ค่านี้ derive ได้จาก ParentId แต่เก็บเป็นคอลัมน์
    /// เพราะ query "หมวดชั้นบนทั้งหมดของบริษัทนี้" ยิงทุกครั้งที่เปิดเมนู และการบังคับกติกา
    /// "ห้ามลึกเกิน 2" (TX-2) ตรวจที่คอลัมน์เดียวได้ ไม่ต้องไล่ recursive</summary>
    public required int Level { get; init; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>ลำดับในเมนู - เล็กก่อน · ค่าซ้ำกันได้ ถ้าซ้ำให้เรียงต่อด้วย Name</summary>
    public required int SortOrder { get; set; }

    /// <summary>true เฉพาะ default chain "ยังไม่จัดหมวด" ที่ migration สร้างให้ (ดู MG-A3)
    /// หนึ่งบริษัทต้องมี exactly 2 แถวที่เป็น true และเชื่อมกันเป็น chain เดียว:
    /// Level 1 parent หนึ่งแถว + Level 2 leaf หนึ่งแถวที่ ParentId ชี้ไปหา parent นั้น
    /// ทั้งสองแถวลบไม่ได้ เปลี่ยนชื่อไม่ได้ ย้ายชั้นไม่ได้ แต่เฉพาะ Level 2 leaf เท่านั้น
    /// ที่ assign ให้ LessonConfig/DocumentResource/KnowledgeQnA ได้ตาม TX-4/TX-5
    /// ⚠️ ฟิลด์นี้มีอยู่เพราะ Q3 เลือกทาง "หมวดตั้งต้น + ธงเตือน" - ถ้า Q3 เคาะเป็น
    /// "บังคับจัดก่อนใช้" ให้ลบฟิลด์นี้ทิ้งทั้งคอลัมน์</summary>
    public required bool IsSystemDefault { get; init; }
}
```

### DM-2 · `LessonConfig` (แก้ของเดิม — **breaking**)

เพิ่มฟิลด์เดียว ที่เหลือคงเดิมทั้งหมด (`Slug` · `Title` · `Description` · `SlidesSourceUrl` ·
`PresentationId` · `SlidesEmbedUrl` · `ContentSourceType` · `PdfDocumentResourceId` ·
`SlideConfigs` · `IsActive`)

> **แก้เอกสาร 2026-08-25 — ปิดหนี้ D-3 (ไม่ใช่การเปลี่ยนการตัดสินใจใหม่)**
> รายการข้างบนนี้เคยนับ `IntroWaitMs` · `BreathPauseMs` · `FinalQuestionWaitMs` เป็นฟิลด์ของ
> `LessonConfig` ด้วย — **สามคอลัมน์นั้นถูกลบออกจากตารางจริงไปแล้ว** โดย migration
> `20260822143217_RemoveLessonConfigPacingOverrides` ตามมติ **Module P (N1/N2/N3)** ของโมดูล
> `company-admin` ที่เจ้าของโปรเจกต์เคาะเมื่อ 2026-08-22: จังหวะการสอนเป็น **ค่ากลางระดับบริษัท
> อย่างเดียว ไม่มี override ต่อบทเรียน** · ค่ากลางอยู่ที่ `Company.DefaultIntroWaitMs` /
> `Company.DefaultBreathPauseMs` / `Company.DefaultFinalQuestionWaitMs` (`int` non-null)
> ซึ่ง **เป็นของโมดูล `company-admin` — เอกสารฉบับนี้อ้างถึงเพื่อบอกที่อยู่เท่านั้น ไม่ออกแบบทับ**
> (`.claude/shared/conventions.md` §7 เรื่อง cross-module ownership)
> **ผลกระทบต่อ `knowledge-base` = ไม่มี**: ไม่มี R1–R6, contract ชุดใดหรือ Module A–G ข้อไหน
> อ่านหรือเขียนสามฟิลด์นี้ — เดิมถูกจัดไว้ในกลุ่ม "ไม่แตะ" อยู่แล้ว การแก้รอบนี้จึงเป็นการลบ
> การอ้างถึงของที่ไม่มีอยู่จริงออก ไม่ใช่การเปลี่ยน scope

```csharp
    /// <summary>R1.1 - บทเรียนต้องมีหมวดเสมอ หนึ่งหมวดเท่านั้น
    /// ⚠️ ชี้ไปแถว Level 2 (subcategory) เท่านั้นตามข้อเสนอ Q1-A - ถ้า Q1 เคาะเป็น C
    /// ให้ผ่อนเป็น "Level 1 หรือ 2 ก็ได้" ที่ TX-4 ไม่ต้องแก้ชนิดคอลัมน์
    /// set ไม่ใช่ init เพราะ SaveAsync เป็น upsert ที่แก้ instance เดิมในที่ (แบบเดียวกับ Title)</summary>
    public required string CategoryId { get; set; }
```

`OnModelCreating` เพิ่ม `entity.HasIndex(x => x.CategoryId);` — ใช้ตอนนับของในหมวดก่อนลบ (TX-6)
และตอนสร้างเมนู · index เดิม `(CompanyId, Slug) IsUnique` และ `OwnsMany(SlideConfigs).ToJson()`
**คงไว้ทั้งหมด ห้ามแตะ**

### DM-3 · `DocumentResource` (แก้ของเดิม — **breaking**)

```csharp
public sealed class DocumentResource : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }      // ⚠️ เดิมเป็น init - ต้องเปลี่ยนเป็น set (R6.1)
    public bool IsDelete { get; set; }         // ⚠️ เดิมเป็น init - ต้องเปลี่ยนเป็น set (R6.1)
    public DateTime? DeletedAt { get; set; }   // ⚠️ เดิมเป็น init - ต้องเปลี่ยนเป็น set (R6.1)

    /// <summary>R3 - "lesson" | "category" | "company" (KnowledgeScopeType)
    /// แทนที่คอลัมน์ LessonId เดิมทั้งใบ (LessonId != null → lesson · null → company)</summary>
    public required string ScopeType { get; set; }

    /// <summary>LessonConfig.Id เมื่อ ScopeType = lesson · KnowledgeCategory.Id เมื่อ = category
    /// null เมื่อ = company เท่านั้น (ดู KS-1 สำหรับกติกาที่บังคับ)</summary>
    public string? ScopeId { get; set; }

    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required string ObsBucket { get; init; }
    public required string ObsKey { get; init; }

    /// <summary>"pending" | "indexed" | "failed" (DocumentIndexingStatus) - ค่าเดิม ไม่เพิ่มค่าใหม่</summary>
    public required string IndexingStatus { get; set; }
    public int IndexedChunkCount { get; set; }

    /// <summary>R6.4 - null เมื่อไม่ได้ failed · ค่าใน DocumentFailureReason เท่านั้น
    /// นี่คือค่าที่ CS เห็น ห้ามใส่ข้อความ error ดิบหรือชื่อ provider ลงคอลัมน์นี้
    /// รายละเอียดภายในไปอยู่ที่ BackgroundJob.LastErrorDetail ซึ่งไม่เคยออก API</summary>
    public string? FailureReason { get; set; }
}
```

**คอลัมน์ `LessonId` ถูกลบทั้งใบ** — เหตุผลที่ไม่เก็บไว้แล้วเพิ่ม `CategoryId` ข้างๆ: สองคอลัมน์
nullable ที่มีกติกา "ต้องมีค่าไม่เกินหนึ่งอัน" ต้องถูกตรวจซ้ำที่ทุก call site และไม่มีอะไรบังคับได้
ส่วน `ScopeType`+`ScopeId` มี resolver จุดเดียว (KS-1) และรูปเดียวกับ `KnowledgeQnA` เป๊ะ
ทำให้ทั้งเอกสารและ Q&A ใช้ฟังก์ชัน resolve namespace ตัวเดียวกัน

**`LessonConfig.PdfDocumentResourceId` ไม่เกี่ยวข้องกับ `ScopeType` และไม่เปลี่ยน** — มันคือ
"บทเรียนนี้ใช้ไฟล์ไหนเป็นเนื้อหาสอน" คนละเรื่องกับ "ไฟล์นี้ตอบคำถามในขอบเขตไหน"

### DM-4 · `DocumentChunk` (ตารางใหม่) — R6.1 + R6.3

```csharp
using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// ข้อความที่แปลงได้จริงต่อ chunk ณ ตอนที่ index สำเร็จ - เก็บไว้สองเหตุผลที่แยกกันไม่ได้:
///
/// 1. R6.1 ต้องลบ vector ทีละชุด ซึ่งต้องรู้ id จริงที่อยู่ใน Pinecone · chunk id ของ extractor
///    ไม่ต่อเนื่อง (PdfTextExtractor ข้ามหน้าว่าง, XlsxTextExtractor เป็นช่วงแถว) จึงเดา id ย้อนหลัง
///    จาก IndexedChunkCount ไม่ได้ · ทางเลือกอื่นคือ Pinecone list-by-prefix ซึ่งมีเฉพาะ serverless
///    และต้องทำ pagination เอง - แพงกว่าและผูกกับ tier ของ Pinecone
/// 2. R6.3 ต้องให้ CS เห็น "ข้อความที่คลังความรู้ได้รับไปจริง" ไม่ใช่ "ข้อความที่ parser วันนี้จะแปลงได้"
///    ซึ่งเป็นคนละอย่างกันทันทีที่มีการปรับ parser
///
/// ⚠️ ตารางนี้มีอยู่เพราะ Q5 เลือกทาง "เก็บลง DB" - ถ้า Q5 เคาะเป็น "re-parse ตอนดู"
/// ให้ลบตารางนี้ทิ้งและย้าย VectorId ไปเก็บเป็น string[] column บน DocumentResource แทน
/// (ยังต้องเก็บ id อยู่ดีเพราะข้อ 1 ข้างบนไม่หายไป)
///
/// ไม่ขัดกับกฎสถาปัตยกรรมข้อ 8 (ห้าม persist สำเนา teaching content ลง LessonConfig) เพราะ
/// นี่ไม่ใช่ LessonConfig และไม่ใช่บทพูด - เป็น input ของคลังความรู้ ซึ่งวันนี้ถูก persist อยู่แล้ว
/// ในรูป metadata "__text" ของ Pinecone (ดู PineconeKnowledgeIndexProvider.TextMetadataKey)
/// </summary>
public sealed class DocumentChunk : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("chunk")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string DocumentId { get; init; }

    /// <summary>chunkId ที่ extractor คืนมาตรงๆ - "page-3" | "slide-2" | "para-1" |
    /// "sheet-0-rows-2-21" · ห้ามแปลง ห้าม normalize</summary>
    public required string ChunkKey { get; init; }

    /// <summary>id จริงใน Pinecone = $"{DocumentId}-{ChunkKey}" · เก็บเป็นคอลัมน์แยกแทนที่จะ
    /// ประกอบตอนใช้ เพราะนี่คือค่าที่ส่งไปลบ และต้องตรงกับสิ่งที่ upsert ไปจริงแม้สูตรจะเปลี่ยน</summary>
    public required string VectorId { get; init; }

    /// <summary>namespace ที่แถวนี้ถูก upsert เข้าไปจริง - เก็บเพราะ scope ของเอกสารย้ายได้
    /// (KS-4) และตอนลบต้องลบจาก namespace ที่มันอยู่จริง ไม่ใช่ namespace ที่มันควรอยู่วันนี้</summary>
    public required string NamespaceKey { get; init; }

    /// <summary>ลำดับที่แสดงให้ CS เริ่มจาก 1 - ตามลำดับที่ extractor คืนมา</summary>
    public required int SeqNo { get; init; }

    public required string Text { get; init; }
    public required int CharCount { get; init; }

    /// <summary>R6.3 - true เมื่อ Text มีอักขระที่บ่งชี้ว่าแปลงเพี้ยน (ดู DI-6 สำหรับนิยามที่แน่นอน)
    /// เป็นแค่ตัวช่วยเรียงลำดับให้คนดูก่อน ไม่ใช่คำตัดสิน และไม่เคยบล็อกการ index
    /// requirement ระบุชัดว่า "สายตาคนคือทางเดียวที่จับเคสนี้ได้"</summary>
    public required bool HasSuspectCharacters { get; init; }
}
```

### DM-5 · `LessonSlideNarration` (ตารางใหม่) — R4

```csharp
using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// บทพูดที่ CS แก้ทับ - **มีแถวเฉพาะหน้าที่ถูกแก้จริงเท่านั้น** (R4.1) หน้าที่ไม่ได้แตะห้ามมีแถว
/// ไม่งั้นเป็นการแช่แข็งสำเนาที่แปลงพังไว้ถาวร และการปรับ PdfSlidesRenderer ให้ดีขึ้นวันหลัง
/// จะไม่มีผลกับหน้าเหล่านั้น
///
/// ใช้กับ ContentSourceType = "pdf" เท่านั้น - Google Slides แก้ที่ต้นทาง (R4 มติ 2026-08-19)
/// </summary>
public sealed class LessonSlideNarration : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("narr")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string LessonId { get; init; }

    /// <summary>"pdf-page-N" ตามที่ PdfSlidesRenderer สร้าง (N เริ่มจาก 1)
    /// ห้ามเก็บเลขหน้าเป็น int แยก - ค่านี้ต้องตรงกับ SlideObjectId ที่ tutor engine ใช้เป๊ะ</summary>
    public required string SlideObjectId { get; init; }

    /// <summary>ข้อความที่ CS พิมพ์ · trim แล้วต้องไม่ว่าง (ลบ = ลบแถว ไม่ใช่เซฟค่าว่าง)
    /// สูงสุด 5000 ตัวอักษร - เท่ากับที่ Edge TTS สังเคราะห์ได้ในหนึ่งหน้าโดยไม่ต้องซอย</summary>
    public required string NarrationText { get; set; }
}
```

### DM-6 · `KnowledgeQnA` (ตารางใหม่) — R5

```csharp
using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// คู่ถาม-ตอบที่ CS เขียน - แนวคิดหลักของ R5 "ถามประมาณไหน ตอบประมาณนั้น"
/// เข้า Pinecone namespace เดียวกับเอกสาร (ไม่แยก namespace) แยกด้วย metadata sourceType="qna"
/// เหตุผล: ถ้าแยก namespace จำนวน query ต่อคำถามจะเป็นสองเท่าทันที (ดู KS-3)
/// </summary>
public sealed class KnowledgeQnA : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("qna")
    public required string CompanyId { get; init; }

    /// <summary>R5.6 - AdminUser.Id ของคนเขียน · ระบบมี auth แล้ว ค่านี้เชื่อถือได้จริง
    /// ไม่ใช่ null เหมือน ReviewedBy ที่ learning-session ตัดออกไปตอนยังไม่มี auth</summary>
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>คำถามที่ CS เรียบเรียง - **ค่านี้คือสิ่งที่ถูก embed** (ดู KS-5)
    /// prefill ด้วย transcript ของคำถามต้นทางได้ แต่ CS แก้ได้ก่อนบันทึก
    /// trim แล้ว 1-1000 ตัวอักษร</summary>
    public required string Question { get; set; }

    /// <summary>คำตอบที่ถูก - trim แล้ว 1-5000 ตัวอักษร</summary>
    public required string Answer { get; set; }

    /// <summary>R5.4 - "lesson" | "category" | "company" (KnowledgeScopeType)</summary>
    public required string ScopeType { get; set; }

    /// <summary>รูปเดียวกับ DocumentResource.ScopeId เป๊ะ - ดู KS-1</summary>
    public string? ScopeId { get; set; }

    /// <summary>id ใน Pinecone = Id ของแถวนี้ตรงๆ (ไม่มี suffix เพราะ 1 Q&A = 1 vector)
    /// เก็บเป็นคอลัมน์ด้วยเหตุผลเดียวกับ DocumentChunk.VectorId</summary>
    public required string VectorId { get; init; }

    /// <summary>namespace ที่ถูก upsert เข้าไปจริง · เปลี่ยน ScopeType/ScopeId = ต้องลบจาก
    /// namespace เดิมก่อนแล้ว upsert เข้าอันใหม่ (KS-4) ค่านี้คือ "เดิม" ที่ต้องรู้</summary>
    public string? IndexedNamespaceKey { get; set; }

    /// <summary>ใช้ DocumentIndexingStatus ชุดเดียวกับเอกสาร - pending | indexed | failed</summary>
    public required string IndexingStatus { get; set; }

    /// <summary>ใช้ DocumentFailureReason ชุดเดียวกับเอกสาร (embedding_failed / index_failed
    /// เท่านั้นที่เกิดได้ที่นี่ - Q&A ไม่มีขั้นแปลงไฟล์)</summary>
    public string? FailureReason { get; set; }
}
```

### DM-7 · `KnowledgeQnASource` (ตารางใหม่) — R5.2

```csharp
/// <summary>
/// คำถามจริงที่ Q&A ใบนี้ถูกเขียนขึ้นมาตอบ - หนึ่ง Q&A ปิดได้หลายคำถาม
///
/// ⚠️ เหตุผลที่ความสัมพันธ์นี้อยู่ฝั่งนี้ ไม่ใช่คอลัมน์บน SessionQuestion:
/// SessionQuestion เป็น entity ที่ `_docs/module/learning-session/design.md` เป็นเจ้าของ
/// (conventions §1) module นี้อ้างถึงได้แต่ห้ามออกแบบใหม่จากฝั่งนี้ · การเก็บลิงก์ไว้ที่ตารางของ
/// เราเองทำให้ R5 ทั้งชุด **ไม่ต้องแก้ entity ของ module อื่นแม้แต่ฟิลด์เดียว**
/// </summary>
public sealed class KnowledgeQnASource : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("qnasrc")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string QnAId { get; init; }

    /// <summary>SessionQuestion.Id - logical FK ข้าม module ไม่มี FK constraint จริง</summary>
    public required string SessionQuestionId { get; init; }
}
```

### DM-8 · `KnowledgeQnAConflict` (ตารางใหม่) — R5.5

```csharp
/// <summary>
/// ธงที่ยกขึ้นเมื่อโมเดลรายงานว่า Q&A ที่หยิบมาขัดกับเอกสาร/สไลด์ - เอกสารชนะไปแล้วตอนตอบ
/// แถวนี้มีไว้เพื่อให้ CS ไปแก้ "เอกสารที่เป็นต้นเหตุ" ไม่ใช่เพื่อ block อะไร
///
/// เก็บเป็นตารางไม่ใช่คอลัมน์บน KnowledgeQnA เพราะ Q&A ใบเดียวขัดกับเอกสารคนละใบได้หลายครั้ง
/// และ CS ต้องเห็นหลักฐาน (คำถามจริงที่ทำให้เกิด) ไม่ใช่แค่ตัวนับ
/// </summary>
public sealed class KnowledgeQnAConflict : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("qnacf")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }                // null - เกิดจากระบบ ไม่ใช่คน
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string QnAId { get; init; }

    /// <summary>คำถามจริงที่ทำให้ธงถูกยก - SessionQuestion.Id (logical FK ข้าม module)
    /// null ได้เมื่อคำถามนั้นไม่ถูกบันทึก (readiness check ฯลฯ)</summary>
    public string? SessionQuestionId { get; init; }

    /// <summary>ชื่อที่คนอ่านรู้เรื่องของแหล่งที่ขัดกัน - FileName ของเอกสาร หรือ "สไลด์หน้า N"
    /// ประกอบจาก metadata ของ chunk ที่ชนะ · **ห้ามใส่ raw error หรือชื่อ provider** (R6.4)</summary>
    public required string ConflictingSourceLabel { get; init; }

    /// <summary>ประโยคที่โมเดลอธิบายว่าขัดกันตรงไหน - ตัดที่ 1000 ตัวอักษร
    /// เป็นข้อความจากโมเดล ไม่ใช่ error ของระบบ จึงแสดงให้ CS เห็นได้</summary>
    public string? ModelNote { get; init; }

    /// <summary>CS กดปิดธงเมื่อไปแก้เอกสารต้นเหตุแล้ว · null = ยังค้าง</summary>
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }               // AdminUser.Id
}
```

### DM-9 · `QuestionQueueDismissal` — **ตัดออกทั้งตาราง** (✅ Q6 ยืนยัน 2026-08-19)

ข้อเสนอเดิมของ agent นี้คือปุ่ม "ไม่ต้องตอบ" + เหตุผลอิสระ เพื่อไม่ให้คิวกองสะสมคำถามที่ตอบไม่ได้จริงๆ
เจ้าของโปรเจกต์ทักท้วงว่าข้อเสนอนี้แก้ปัญหาผิดจุด: **คำถามนอกเรื่องระบบต้องถูกกรองตั้งแต่ในห้องเรียน
ตอนที่ครูถาม ไม่ใช่มากรองทีหลังในคิว** — และตรวจโค้ดแล้วยืนยันว่า **ระบบทำแบบนี้อยู่แล้ววันนี้จริง**:
`AnswerStatus.cs` มี `OutOfScope` แยกจาก `NotFound` อยู่แล้ว และ QQ-1 (นิยามคิว) ดึงจาก `NotFound`
เท่านั้น ไม่เคยดึง `OutOfScope` เข้าคิว — คำถามนอกเรื่องจึงไม่มีทางเข้าคิวได้ตั้งแต่ต้น ไม่ต้องมีกลไก
dismiss ใดๆ มารองรับ ขอบเขตจริงที่เหลือของ Q6 คือกรณีที่แคบกว่ามาก (คำถามที่เกี่ยวกับระบบจริงแต่
CS ตัดสินว่าไม่มีคำตอบมาตรฐาน) ซึ่งเจ้าของโปรเจกต์เลือก **ปล่อยค้างไว้ในคิวเฉยๆ** เพราะเกิดน้อยจริง
— ไม่มีกลไกพิเศษ ไม่มีตาราง ดู QQ-1/QQ-2 สำหรับกติกาที่ใช้จริง

### DM-10 · `BackgroundJob` (ตารางใหม่) — R6.2

```csharp
using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// งานเบื้องหลังที่รอด restart ได้ - แทนที่ IBackgroundTaskQueue ที่เก็บ closure ในหน่วยความจำ
///
/// ⚠️ จุดที่พลาดง่ายที่สุดของ R6.2: แถวนี้เก็บได้แค่ **id** ไม่ใช่ payload หนัก - งานเดิมจับ
/// byte[] ของไฟล์ทั้งไฟล์ไว้ใน closure (IDocumentResourceService.UploadAsync ส่ง input.Content
/// เข้า taskQueue.Enqueue) worker ตัวใหม่ต้อง **โหลดไฟล์ใหม่จาก IDocumentStorageProvider
/// ด้วย ObsKey ทุกครั้ง** ไม่มีทางลัด
///
/// CompanyId บนแถวนี้ไม่ใช่แค่ audit - worker ต้องเอาไป Resolve ใส่ ICompanyContext ของ scope
/// ตัวเองก่อนทำอะไร ไม่งั้น query filter จะ match ศูนย์แถวและงานจะ "สำเร็จ" โดยไม่ทำอะไรเลย
/// (comment เดิมใน IndexUploadedDocumentAsync อธิบายกับดักนี้ไว้แล้ว - ย้ายมาด้วย)
/// </summary>
public sealed class BackgroundJob : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("job")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>BackgroundJobType</summary>
    public required string JobType { get; init; }

    /// <summary>DocumentResource.Id | LessonConfig.Id | KnowledgeQnA.Id ตาม JobType</summary>
    public required string TargetId { get; init; }

    /// <summary>JSON เล็กๆ สำหรับพารามิเตอร์ที่ไม่ใช่ id - เช่น {"vectorIds":[...],"namespace":"..."}
    /// ของงาน vector_delete · null เมื่องานไม่ต้องการ · **ห้ามใส่เนื้อไฟล์หรือข้อความยาว**</summary>
    public string? PayloadJson { get; init; }

    /// <summary>BackgroundJobStatus - pending | running | succeeded | failed</summary>
    public required string Status { get; set; }

    public required int AttemptCount { get; set; }

    /// <summary>เวลาที่งานนี้พร้อมถูกหยิบครั้งถัดไป - ตอนสร้าง = CreateDate
    /// หลังล้ม = เวลาปัจจุบัน + backoff ตาม DI-9</summary>
    public required DateTime NextAttemptAt { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    /// <summary>ค่าใน DocumentFailureReason - ค่าที่ปลอดภัยจะแสดงให้ CS</summary>
    public string? LastErrorCode { get; set; }

    /// <summary>ข้อความ exception จริง สำหรับ log/debug เท่านั้น ตัดที่ 2000 ตัวอักษร
    /// **ห้าม map ลง ViewModel ใดๆ ห้ามออก API** (R6.4 "ห้ามแสดงรายละเอียดภายในของ provider")</summary>
    public string? LastErrorDetail { get; set; }
}
```

### DM-11 · constants ใหม่ (`static class` + `const string` — ห้ามใช้ C# enum)

```csharp
// SupportRoom.Domain/Enums/KnowledgeScopeType.cs — ใหม่
/// <summary>ขอบเขตที่ความรู้ชิ้นหนึ่งตอบได้ - ใช้ร่วมกันระหว่าง DocumentResource และ
/// KnowledgeQnA โดยเจตนา เพื่อให้ resolve namespace ได้ด้วยฟังก์ชันเดียว (KS-1)
/// String constants ให้ตรงกับ TS union type เป๊ะ</summary>
public static class KnowledgeScopeType
{
    public const string Lesson = "lesson";
    public const string Category = "category";
    public const string Company = "company";
}

// SupportRoom.Domain/Enums/DocumentFailureReason.cs — ใหม่ (R6.4)
/// <summary>สาเหตุที่ล้มเหลว แยกตาม "CS ต้องทำอะไรต่อ" ไม่ใช่แยกตามชั้นของโค้ดที่ throw</summary>
public static class DocumentFailureReason
{
    /// <summary>นามสกุล/ContentType ไม่รองรับ → CS ต้องแปลงไฟล์แล้วอัปใหม่</summary>
    public const string UnsupportedType = "unsupported_type";

    /// <summary>เปิดไฟล์/แปลงไม่สำเร็จ (ไฟล์เสีย ใส่รหัสผ่าน) → CS ต้องเปลี่ยนไฟล์</summary>
    public const string ExtractFailed = "extract_failed";

    /// <summary>แปลงได้แต่ไม่มีข้อความเลย - แทบทุกครั้งคือไฟล์สแกน → CS ต้องพิมพ์เอง
    /// นี่คือค่าที่ครอบเคส R4.4 ฝั่งคลังความรู้ (ซึ่งวันนี้จับได้แล้ว แต่บอกไม่ได้ว่าเพราะอะไร)</summary>
    public const string NoText = "no_text";

    /// <summary>เรียก embedding provider ไม่สำเร็จ → ระบบลองใหม่เอง CS ไม่ต้องทำอะไร</summary>
    public const string EmbeddingFailed = "embedding_failed";

    /// <summary>upsert/delete กับ Pinecone ไม่สำเร็จ → ระบบลองใหม่เอง</summary>
    public const string IndexFailed = "index_failed";
}

// SupportRoom.Domain/Enums/BackgroundJobType.cs — ใหม่
public static class BackgroundJobType
{
    public const string DocumentIndex = "document_index";
    public const string LessonIndex = "lesson_index";
    public const string QnaIndex = "qna_index";
    public const string VectorDelete = "vector_delete";
}

// SupportRoom.Domain/Enums/BackgroundJobStatus.cs — ใหม่
public static class BackgroundJobStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}
```

**`DocumentIndexingStatus` เดิมไม่เปลี่ยนค่าและไม่เพิ่มค่าใหม่** — เจตนา: การเพิ่มค่า `processing`
จะบังคับให้ TS union type และทุกหน้าจอที่ switch บนสถานะเปลี่ยนตาม ทั้งที่ข้อมูลเดียวกัน
อ่านได้จาก `BackgroundJob.Status` + `NextAttemptAt` ผ่าน ViewModel อยู่แล้ว (DI-10)

### DM-12 · `KnowledgeNamespaces` (แก้ของเดิม — เพิ่มเมธอด)

```csharp
public static class KnowledgeNamespaces
{
    private const string GlobalSuffix = "kb-global";

    public static string For(string companyId, string lessonSlug) => $"{companyId}:{lessonSlug}";

    public static string ForGlobal(string companyId) => $"{companyId}:{GlobalSuffix}";

    /// <summary>R3 - ระดับหมวด · categoryId มา prefix "kbcat-" จาก IdGenerator อยู่แล้ว
    /// จึงชนกับ lessonSlug ไม่ได้ตราบใดที่ TX-7 บังคับว่า slug ห้ามขึ้นต้นด้วย "kbcat-"
    /// และห้ามเท่ากับ "kb-global" · เหตุผลที่ key ใช้ id ไม่ใช่ชื่อหมวด: เปลี่ยนชื่อหมวดแล้ว
    /// vector ทั้งกองต้องไม่ย้ายบ้าน (ต่างจาก lesson namespace ที่ผูก slug ไว้ - หนี้เดิม R-8)</summary>
    public static string ForCategory(string companyId, string categoryId) => $"{companyId}:{categoryId}";
}
```

### DM-13 · `IKnowledgeIndexProvider` (แก้ของเดิม — เพิ่มเมธอด) — R6.1

```csharp
public interface IKnowledgeIndexProvider
{
    Task UpsertAsync(string namespaceKey, IReadOnlyList<KnowledgeChunk> chunks);
    Task<IReadOnlyList<ScoredChunk>> QueryAsync(string namespaceKey, float[] queryVector, int topK);
    Task DeleteNamespaceAsync(string namespaceKey);

    /// <summary>R6.1 - ลบทีละชุดด้วย id ตรงๆ · ids ว่าง = no-op ไม่ยิง request
    /// สัญญาเดียวกับ DeleteNamespaceAsync: ลบ id ที่ไม่มีอยู่แล้วไม่ถือเป็นความล้มเหลว</summary>
    Task DeleteVectorsAsync(string namespaceKey, IReadOnlyList<string> ids);
}
```

`PineconeKnowledgeIndexProvider` implement ด้วย `POST /vectors/delete` body
`{"ids": [...], "namespace": "..."}` — endpoint และ client เดิมทั้งหมด · **ส่งได้ครั้งละไม่เกิน
1000 id ตามลิมิตของ Pinecone ต้องซอยเป็นชุด** · `DeleteRequest` เดิมที่ hardcode
`deleteAll = true` ต้องแยกเป็นสอง request type อย่าเผลอส่ง `deleteAll` มาด้วยกับ `ids`

### DM-14 · `KnowledgeSourceChunk` (แก้ของเดิม — เพิ่ม property) — R5

```csharp
public sealed class KnowledgeSourceChunk
{
    public required string Id { get; init; }
    public required string Text { get; init; }

    /// <summary>ข้อความที่เอาไป embed จริง · null = ใช้ Text (พฤติกรรมเดิมทุกประการ)
    /// มีไว้เพื่อ Q&A: embed เฉพาะ "คำถาม" เพราะ retrieval คือการจับคู่คำถามกับคำถาม (R5 หลัก)
    /// แต่สิ่งที่ต้องส่งเข้า prompt คือ ถาม+ตอบ ทั้งคู่ (KS-5)</summary>
    public string? EmbedText { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
```

`IKnowledgeIndexingService.IndexChunksAsync` เปลี่ยนบรรทัดเดียว: embed `chunk.EmbedText ?? chunk.Text`
· ที่เหลือ (skip chunk ที่ text ว่าง, ไม่ throw, คืนจำนวนที่ index ได้) **คงสัญญาเดิมทั้งหมด**

### DM-15 · `ApplicationDbContext.OnModelCreating` (ส่วนที่เปลี่ยน)

```csharp
public DbSet<KnowledgeCategory> KnowledgeCategory => Set<KnowledgeCategory>();
public DbSet<DocumentChunk> DocumentChunk => Set<DocumentChunk>();
public DbSet<LessonSlideNarration> LessonSlideNarration => Set<LessonSlideNarration>();
public DbSet<KnowledgeQnA> KnowledgeQnA => Set<KnowledgeQnA>();
public DbSet<KnowledgeQnASource> KnowledgeQnASource => Set<KnowledgeQnASource>();
public DbSet<KnowledgeQnAConflict> KnowledgeQnAConflict => Set<KnowledgeQnAConflict>();
public DbSet<BackgroundJob> BackgroundJob => Set<BackgroundJob>();

builder.Entity<KnowledgeCategory>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.CompanyId);
    // เมนู: ดึงหมวดทั้งบริษัทเรียงตามชั้นแล้วลำดับ ในคิวรีเดียว
    entity.HasIndex(x => new { x.CompanyId, x.ParentId, x.SortOrder });
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
});

builder.Entity<LessonConfig>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => new { x.CompanyId, x.Slug }).IsUnique();   // เดิม ห้ามแตะ
    entity.HasIndex(x => x.CategoryId);                             // ใหม่
    entity.OwnsMany(x => x.SlideConfigs, owned => owned.ToJson());  // เดิม ห้ามแตะ
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
});

builder.Entity<DocumentResource>(entity =>
{
    entity.HasKey(x => x.Id);
    // เดิม HasIndex(x => x.LessonId) - ลบทิ้งพร้อมคอลัมน์
    entity.HasIndex(x => new { x.CompanyId, x.ScopeType, x.ScopeId });
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
});

builder.Entity<DocumentChunk>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => new { x.DocumentId, x.SeqNo });
    entity.HasIndex(x => x.CompanyId);
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
});

builder.Entity<LessonSlideNarration>(entity =>
{
    entity.HasKey(x => x.Id);
    // ไม่ IsUnique: soft delete ทำให้แถวที่ถูกลบยังกินคีย์อยู่ กติกา "หน้าละหนึ่งแถว"
    // บังคับที่ service layer (NR-2) ด้วยเหตุผลเดียวกับ TX-3
    entity.HasIndex(x => new { x.LessonId, x.SlideObjectId });
    entity.HasIndex(x => x.CompanyId);
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
});

builder.Entity<KnowledgeQnA>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => new { x.CompanyId, x.ScopeType, x.ScopeId });
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
});

builder.Entity<KnowledgeQnASource>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.QnAId);
    // คิวเช็ค "คำถามนี้มี Q&A แล้วหรือยัง" ยิงต่อคำถามทุกแถวในหน้าคิว
    entity.HasIndex(x => new { x.CompanyId, x.SessionQuestionId });
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
});

builder.Entity<KnowledgeQnAConflict>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.QnAId);
    entity.HasIndex(x => new { x.CompanyId, x.ResolvedAt });
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId && !x.IsDelete);
});

builder.Entity<BackgroundJob>(entity =>
{
    entity.HasKey(x => x.Id);
    // worker หยิบงานด้วยคิวรีนี้ทุกรอบ polling - index นี้ไม่ใช่ของแถม
    entity.HasIndex(x => new { x.Status, x.NextAttemptAt });
    entity.HasIndex(x => new { x.CompanyId, x.JobType, x.TargetId });
    // ⚠️ ไม่มี HasQueryFilter โดยเจตนา - worker หยิบงานข้ามบริษัทก่อนที่จะรู้ว่า
    // งานนั้นเป็นของบริษัทไหน (แบบเดียวกับ TrainingLink.GetByToken ที่ IgnoreQueryFilters)
    // การเข้าถึงจาก request ต้องกรอง CompanyId เองทุกครั้ง - ดู SEC-2
});

// SessionQuestion - เพิ่ม index อย่างเดียว ไม่แตะฟิลด์ใดๆ (ดู R-9 เรื่องการข้าม module)
builder.Entity<SessionQuestion>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.SessionId);                              // เดิม ห้ามแตะ
    entity.HasIndex(x => x.CompanyId);                              // เดิม ห้ามแตะ
    entity.HasIndex(x => new { x.CompanyId, x.AnswerStatus });      // ใหม่ - คิว R5.1 แหล่งที่ 1
    entity.HasIndex(x => new { x.CompanyId, x.ReviewResult });      // ใหม่ - คิว R5.1 แหล่งที่ 2
    entity.HasQueryFilter(x => x.CompanyId == companyContext.CompanyId);
});
```

⚠️ **`HasQueryFilter` ของตารางใหม่ทุกใบมี `&& !x.IsDelete`** ซึ่ง `TrainingLink`/`LearningSession`/
`SessionQuestion`/`ChatMessage`/`LessonConfig` **ไม่มี** — เจตนา ไม่ใช่ความไม่สม่ำเสมอที่ต้อง "แก้ให้ตรงกัน":
R6.1 คือจุดแรกในโปรเจกต์ที่ soft delete มีความหมายจริง การไล่เติม filter ให้ entity เดิมที่โค้ดลบจริง
มาตลอดจะเปลี่ยนพฤติกรรมของ module อื่นโดยไม่มีใครขอ (ดู `## Unresolved Open Questions` ข้อ O-3)

### DM-16 · Repository

| Repository | สถานะ | หมายเหตุ |
|---|---|---|
| `IKnowledgeCategoryRepository` | **ใหม่** | `GetByCompanyOrdered()` · `GetChildren(parentId)` · `GetSystemDefault()` = คืน default **Level 2 leaf** ที่ assign ได้เท่านั้น โดย filter `IsSystemDefault && Level == 2` แล้ว `SingleOrDefault()`; parent ที่เป็น system default ไม่อยู่ในผลลัพธ์ และ leaf ซ้ำต้อง fail-fast แทนการเลือกแถวใดแถวหนึ่งเงียบๆ |
| `IDocumentChunkRepository` | **ใหม่** | `GetByDocumentId(documentId)` · `DeleteByDocumentId(documentId)` (soft) |
| `ILessonSlideNarrationRepository` | **ใหม่** | `GetByLessonId(lessonId)` · `GetOne(lessonId, slideObjectId)` · `DeleteByLessonId(lessonId)` (soft, คืนจำนวนที่ลบ — R4.3 ต้องเอาไปเตือนก่อน) |
| `IKnowledgeQnARepository` | **ใหม่** | `GetByScope(scopeType, scopeId)` · `Search(keyword)` |
| `IKnowledgeQnASourceRepository` | **ใหม่** | `GetBySessionQuestionIds(IReadOnlyList<string>)` — คิวเรียกครั้งเดียวต่อหน้า ห้ามยิงต่อแถว |
| `IKnowledgeQnAConflictRepository` | **ใหม่** | `GetUnresolved()` |
| `IBackgroundJobRepository` | **ใหม่** | `ClaimNext(DateTime now)` + `RequeueOrphanedRunning()` ทั้งคู่ต้อง `IgnoreQueryFilters()` **พร้อม XML doc อธิบายว่าทำไม** ตามแบบแผนของ `GetByToken` |
| `IDocumentResourceRepository` | **แก้** | `GetByLessonId` → `GetByScope(scopeType, scopeId)` · `GetStandalone` → เลิกใช้ (แทนด้วย `GetByScope(company, null)`) · เพิ่ม `GetDeleted()` สำหรับหน้ากู้คืน |
| `ILessonConfigRepository` | **แก้** | เพิ่ม `GetByCategoryId(categoryId)` · `CountByCategoryId(categoryId)` (ใช้ตอนจะลบหมวด TX-6) |
| `ISessionQuestionRepository` | **แก้** | เพิ่ม `GetReviewQueue(...)` — คิวข้ามการเรียน/ข้ามบทเรียน (P8/R5.1) · **ไม่แก้ entity** |

ทุกตัวที่เพิ่ม/แก้ต้องลงทะเบียนใน `UnitOfWork.Register` (ลืม = resolve ไม่ได้ตอน runtime)

---

## Taxonomy Rules (R1 · R2 · R1.2 · R3.1) — contract

| # | กติกา |
|---|---|
| **TX-1** | หมวดมีได้ **2 ชั้นเท่านั้น** · `Level = 1` ⟺ `ParentId == null` · `Level = 2` ⟺ `ParentId != null` และแถวที่ ParentId ชี้ไปต้องมี `Level == 1` · ความสัมพันธ์นี้ต้องตรวจทุกครั้งที่สร้าง/ย้าย ห้ามเชื่อค่า `Level` ที่ client ส่งมา — คำนวณจาก `ParentId` ฝั่ง server เสมอ |
| **TX-2** | สร้างหมวดย่อยใต้แถวที่ `Level == 2` → ปฏิเสธด้วย `GeneralException.ValidationError("หมวดย่อยซ้อนได้ชั้นเดียว")` ห้ามยอมให้เกิดชั้นที่สาม |
| **TX-3** | ชื่อหมวดห้ามซ้ำภายในพ่อเดียวกัน (เทียบแบบ trim + case-insensitive) · **บังคับที่ service layer ไม่ใช่ unique index** เพราะ soft delete ทำให้แถวที่ลบไปแล้วยังกินชื่ออยู่ ซึ่งผิด — ชื่อที่ลบไปแล้วต้องเอากลับมาใช้ได้ |
| **TX-4** | บทเรียนผูกกับหมวดได้เฉพาะแถวที่ `Level == 2` (ข้อเสนอ Q1-A) · ถ้าส่ง `CategoryId` ที่ `Level == 1` มา → ปฏิเสธ · ⚠️ ถ้า Q1 เคาะเป็น C ให้ผ่อนกติกานี้เป็น "Level 1 ก็ได้ ถ้าหมวดนั้นไม่มีลูก" |
| **TX-5** | เอกสาร/Q&A ที่ `ScopeType = category` ผูกกับแถว `Level == 2` เท่านั้น (ข้อเสนอ Q1-A) · **phased availability:** Phase 1 บังคับกติกานี้กับ `DocumentResource`; Phase 6 ต้องนำ validation เดียวกันมาใช้กับ `KnowledgeQnA` ในรอบเดียวกับ MG-F1 · ⚠️ ถ้า Q1 เคาะเป็น B หรือ C ให้ผ่อนเป็น Level 1 หรือ 2 ก็ได้ แล้ว **KS-3 ต้องเพิ่ม namespace ที่สี่** |
| **TX-6** | **ลบหมวดไม่ได้ถ้ายังมีของอยู่** (ข้อเสนอ Q2 ทาง "บล็อก") — final contract นับบทเรียนที่ `CategoryId` ชี้มา + เอกสารและ Q&A ที่ `ScopeType = category` และ `ScopeId` ชี้มา + หมวดย่อยที่ยังไม่ถูกลบ · **phased availability:** Phase 1–5 ยังไม่มีตาราง Q&A จึงนับบทเรียน/เอกสาร/หมวดย่อย และคืน `qnaCount = 0`; Phase 6 ต้องเปลี่ยนเป็นนับ Q&A จริงในรอบเดียวกับ MG-F1 จากนั้น Q&A เป็น hard block เต็มรูปแบบ · ข้อความปฏิเสธต้องบอกจำนวนแยกประเภท ("มีบทเรียน 3 · เอกสาร 12 · Q&A 5 อยู่ในหมวดนี้") ไม่ใช่แค่ "ลบไม่ได้" |
| **TX-7** | `LessonConfig.Slug` ห้ามขึ้นต้นด้วย `kbcat-` และห้ามเท่ากับ `kb-global` — เพราะ namespace key ทั้งสามระดับใช้ pattern `{companyId}:{scope}` เหมือนกัน slug ที่ชนจะทำให้บทเรียนอ่านคลังของหมวดหรือของบริษัทเป็นของตัวเอง · ตรวจตอนสร้าง/แก้บทเรียน (รวมหน้าสร้างใหม่ของ P9) |
| **TX-8** | **เปลี่ยนชื่อหมวดไม่กระทบ vector เลย** — namespace ผูกกับ `KnowledgeCategory.Id` ไม่ใช่ชื่อ · ห้าม "ปรับปรุง" ให้ใช้ชื่อหรือ slug ของหมวดเป็น key |
| **TX-9** | **ย้ายบทเรียนข้ามหมวดไม่ต้อง re-index อะไรเลย** — namespace ของบทเรียนผูกกับ `LessonConfig.Slug` ที่ไม่ได้เปลี่ยน สิ่งที่เปลี่ยนคือ "ยิง namespace ของหมวดไหนควบไปด้วย" ตอนตอบเท่านั้น · การย้ายจึงเป็นการ update คอลัมน์เดียว ไม่ใช่งานเบื้องหลัง |
| **TX-10** | **R3.1 — ก่อนย้าย ต้องคืนตัวเลขให้ยืนยัน** endpoint ย้ายหมวดต้องมีโหมด preview ที่คืน `{ losingDocuments, losingQnAs, gainingDocuments, gainingQnAs }` เสมอเพื่อให้ wire contract ไม่เปลี่ยนข้าม phase · Phase 1–5 นับเอกสารของหมวดเก่า/ใหม่และคืน `losingQnAs = 0`, `gainingQnAs = 0`; Phase 6 ต้องเชื่อมสองค่านี้กับ `KnowledgeQnA` จริงในรอบเดียวกับ MG-F1 · UI ต้องแสดงตัวเลขทั้งสี่แล้วให้กดยืนยันอีกครั้ง · ห้ามย้ายจากคำสั่งเดียวโดยไม่ผ่านการยืนยัน |
| **TX-11** | `IsSystemDefault = true` หมายถึง **default chain สองแถวต่อบริษัท**: Level 1 parent หนึ่งแถว + Level 2 leaf หนึ่งแถว และ `leaf.ParentId` ต้องชี้ parent นั้น · ทั้งสองแถวลบไม่ได้ เปลี่ยนชื่อไม่ได้ ย้ายชั้นไม่ได้ และห้ามแยกออกจากกัน · เฉพาะ leaf เท่านั้นที่ assign ให้บทเรียน/เอกสาร/Q&A ได้ตาม TX-4/TX-5 และ `GetSystemDefault()` ต้องคืน leaf นี้ · **ย้ายของออกจาก leaf ได้ตามปกติ** และเมื่อไม่มีของเหลือแล้วทั้ง chain ยังอยู่ (ไม่ auto-delete — ของใหม่ที่ import มาทีหลังต้องมีที่ลง) |

---

## Knowledge Scope & Retrieval Rules (R3 · R5.3 · R5.5) — contract

| # | กติกา |
|---|---|
| **KS-1** | **resolver เดียวสำหรับทุกอย่าง** — `ScopeType`/`ScopeId` ของทั้ง `DocumentResource` และ `KnowledgeQnA` แปลงเป็น namespace ด้วยฟังก์ชันเดียว: `lesson` → `KnowledgeNamespaces.For(companyId, lesson.Slug)` · `category` → `ForCategory(companyId, scopeId)` · `company` → `ForGlobal(companyId)` · **ห้ามประกอบ key เองที่ call site ใดๆ** — comment เดิมในโค้ดอธิบายไว้แล้วว่า namespace key คือ isolation อย่างเดียวที่ vector store มี |
| **KS-2** | ความถูกต้องของคู่ `ScopeType`/`ScopeId` ที่ต้องตรวจก่อนเซฟทุกครั้ง: `lesson` → `ScopeId` ต้องเป็น `LessonConfig.Id` ที่มีอยู่ในบริษัทนี้ · `category` → ต้องเป็น `KnowledgeCategory.Id` ที่มีอยู่และผ่าน TX-5 · `company` → `ScopeId` **ต้องเป็น null** (ส่งค่ามา = ปฏิเสธ ไม่ใช่เพิกเฉย) |
| **KS-3** | **ต่อหนึ่งคำถาม ยิง 3 namespace พร้อมกันแล้ว merge** (ข้อเสนอ Q1-A): namespace ของบทเรียน + namespace ของหมวดที่บทเรียนนั้นสังกัด + namespace ของบริษัท · วันนี้ยิง 2 (`RagVoiceQuestionProvider` มี `LessonNamespace` + `GlobalNamespace`) จึงเพิ่มเป็นสาม · `IVoiceQuestionProvider` input ต้องเพิ่ม `CategoryNamespace` (**required string** ไม่ใช่ nullable — บทเรียนต้องมีหมวดเสมอตาม R1.1 การทำ nullable จะเปิดทางให้ลืมส่ง) · ⚠️ ถ้า Q1 เคาะเป็น B หรือ C ต้องเพิ่ม `ParentCategoryNamespace` เป็นตัวที่สี่ |
| **KS-4** | **เปลี่ยน scope ของเอกสาร/Q&A = ย้ายบ้าน ไม่ใช่ update คอลัมน์** — ต้องลบ vector ออกจาก `NamespaceKey`/`IndexedNamespaceKey` เดิม แล้ว upsert เข้าอันใหม่ · ทำผ่าน `BackgroundJob` ไม่ใช่ใน request (ต้อง embed ใหม่ ถ้าเป็นเอกสารก็ต้องแปลงไฟล์ใหม่) · ระหว่างที่งานยังไม่เสร็จ สถานะกลับไปเป็น `pending` และ CS เห็นว่ากำลังย้าย |
| **KS-5** | **Q&A embed แค่คำถาม แต่ส่งเข้า prompt ทั้งคู่** — `EmbedText = Question` · `Text = "ถาม: {Question}\nตอบ: {Answer}"` · `Metadata = { sourceType: "qna", qnaId: {Id} }` · เหตุผล: R5 คือการจับคู่ "ถามประมาณไหน" การ embed คำตอบเข้าไปด้วยจะเจือจาง signal ของคำถาม |
| **KS-6** | **ทุก chunk ต้องมี `metadata.sourceType`** ตั้งแต่นี้ไป: `"qna"` · `"document"` (เพิ่มใน `IDocumentResourceService` และ `IAdminService` ที่วันนี้ส่งแค่ documentId/chunkId/fileName) · `"slide"` (เพิ่มใน `IndexLessonAsync`) · vector เก่าที่ index ไว้ก่อน migration จะไม่มี field นี้ → **ตัวอ่านต้อง treat "ไม่มี sourceType" = `"document"`** ห้าม throw และห้ามทิ้ง chunk นั้น |
| **KS-7** | **R5.5 — เอกสารชนะ** ตอนประกอบ prompt ต้องแยกผลลัพธ์ที่ merge มาแล้วออกเป็นสองบล็อกตาม `sourceType`: บล็อกเอกสาร/สไลด์ก่อน แล้วบล็อก Q&A ทีหลัง พร้อมคำสั่งชัดว่า **เมื่อสองบล็อกขัดกันให้ยึดบล็อกแรก** · ห้ามยัดรวมเป็นก้อนเดียวเรียงตาม score เพราะจะไม่มีทางบอกโมเดลได้ว่าอันไหนคืออันไหน |
| **KS-8** | **R5.3 — ห้ามคัดลอก** prompt ต้องสั่งให้เรียบเรียงใหม่และให้ตอบ `not_found` ได้แม้จะหยิบ Q&A มาได้ ถ้า Q&A ที่หยิบมาไม่ตรงคำถามจริง · ตัวอย่างที่ต้องอยู่ใน prompt: "ลบข้อมูลนักเรียนยังไง" กับ "ลบข้อมูลนักเรียนที่จบไปแล้วยังไง" ใกล้กันมากในเชิงภาษาแต่คนละเรื่องในสาระ |
| **KS-9** | **R5.5 — โมเดลรายงานความขัดแย้ง** structured output ของ provider เพิ่ม field `conflict: { qnaId: string, sourceLabel: string, note: string } \| null` · เมื่อไม่ null ให้บันทึก `KnowledgeQnAConflict` หนึ่งแถว · **การบันทึกธงล้มเหลวห้ามทำให้การตอบคำถามล้มเหลว** — log warning แล้วเดินต่อ ตาม convention "service ห้ามพัง flow หลักเพราะ integration รอง" |
| **KS-10** | `qnaId` ที่โมเดลคืนมาต้องถูกตรวจว่ามีจริงและอยู่ในบริษัทนี้ก่อนบันทึกธง — โมเดลแต่ง id ขึ้นมาเองได้ · ไม่ผ่าน = ทิ้งธงนั้นแล้ว log warning ไม่ใช่ throw |
| **KS-11** | **บทเรียนที่ยังไม่เคย index (รวมบทเรียน PDF ทุกใบวันนี้) ต้องไม่ทำให้คำถามพัง** — query ไปที่ namespace ที่ไม่มีอยู่ Pinecone คืน 404/ว่าง ซึ่งพฤติกรรมเดิมจัดการอยู่แล้ว (fallback full-deck) ห้าม "แก้" ด้วยการ throw |

---

## PDF Narration Rules (R4) — contract

| # | กติกา |
|---|---|
| **NR-1** | **ลำดับการ resolve บทพูดต่อหน้า** (ใช้ทั้งตอนสอนและตอน index): มีแถว `LessonSlideNarration` ของ `(LessonId, SlideObjectId)` นั้น → ใช้ `NarrationText` · ไม่มี → ใช้ `PdfSlidesRenderer.BuildContent(...).SpeakerNotes` ของหน้านั้นตามเดิม · **ไม่มีเงื่อนไขที่สาม** |
| **NR-2** | **เซฟเฉพาะที่แก้จริง (R4.1)** — endpoint บันทึกบทพูดรับทีละหน้า และต้องเทียบกับ prefill ก่อน: ข้อความที่ส่งมา (trim แล้ว) **เท่ากับ** ข้อความที่ระบบดึงได้ → **ลบแถวถ้ามี แล้วไม่สร้างแถวใหม่** · ต่างกัน → upsert แถว · ส่งค่าว่างมา → ลบแถว · ห้ามมี code path ใดที่เขียนทุกหน้าลง DB พร้อมกัน |
| **NR-3** | **อัปโหลด PDF ใหม่ = ล้างบทพูดทั้งหมดของบทเรียนนั้น (R4.3)** — trigger คือ `LessonConfig.PdfDocumentResourceId` **เปลี่ยนค่า** (ไม่ใช่การเซฟทั่วไป) · ก่อนเซฟ ต้องคืนจำนวนแถวที่จะถูกลบให้ยืนยันก่อน ("บทพูดที่แก้ไว้ N หน้าจะถูกลบทั้งหมด") · ยืนยันแล้วจึง soft delete ทุกแถวของบทเรียนนั้นในทรานแซกชันเดียวกับการเซฟ |
| **NR-4** | **ห้ามพยายามจับคู่หน้าเก่ากับหน้าใหม่** — `pdf-page-N` ผูกกับเลขหน้าล้วน แทรกหน้าเดียวทุกหน้าถัดไปเลื่อนผิดโดยไม่มี error · ถ้ามีใครเสนอ heuristic จับคู่ ให้ตีกลับมาที่ `system-analyst` ก่อน อย่า implement |
| **NR-5** | **ตรวจไฟล์สแกน (R4.4)** — ตอนเปิดหน้าแก้บทพูด ถ้า **ทุกหน้า** ได้ `SpeakerNotes` ว่างหลัง trim → ตอบกลับพร้อม flag `isLikelyScanned = true` และ UI ขึ้นคำเตือนว่าต้องพิมพ์บทพูดเองทุกหน้า · **เป็นคำเตือน ไม่ใช่ error** — บันทึกบทพูดต่อได้ตามปกติ · ไม่ทำ OCR |
| **NR-6** | **แก้บทพูดแล้วต้อง re-index หน้านั้น (R4.5)** — บันทึก/ลบแถวสำเร็จ → enqueue `BackgroundJob` ชนิด `lesson_index` ที่ `TargetId = LessonId` และ `PayloadJson = {"slideObjectIds":["pdf-page-7"]}` · worker upsert เฉพาะ chunk ที่ระบุ (chunk id ของบทเรียน = `SlideObjectId` อยู่แล้ว) **ห้าม re-embed ทั้งเด็ค** |
| **NR-7** | **บทเรียน PDF ต้องถูก index เข้า namespace ของตัวเอง** — วันนี้ไม่เคยเกิดขึ้นเลย เพราะ `ILessonConfigService.SaveAsync` เรียก `IndexLessonAsync` ใน `if (!string.IsNullOrEmpty(presentationId))` · แก้เป็น: `ContentSourceType = "google_slides"` → เส้นทางเดิมทุกประการ · `= "pdf"` → build content จาก `PdfSlidesRenderer` ผ่าน NR-1 แล้ว index ด้วย `KnowledgeNamespaces.For(companyId, slug)` เหมือนกัน · **ถ้าไม่ทำข้อนี้ NR-6 ไม่มีอะไรให้ re-index และ R4.5 เป็นโมฆะ** |
| **NR-8** | บทเรียน PDF จะมีข้อความจากไฟล์เดียวกันสองเส้นทาง (บทพูดจาก `PdfSlidesRenderer` ที่เก็บหน้าว่างไว้ · คำตอบจาก `PdfTextExtractor` ผ่านเอกสารแนบที่ตัดหน้าว่างทิ้ง) · **เฟสนี้ไม่รวมสองตัวแปลงเข้าด้วยกัน** — ยอมรับว่าซ้ำ แต่ต้องแยก `sourceType` ให้ถูก (`slide` vs `document`) เพื่อให้ KS-7 ยังบอกได้ว่าอันไหนคืออันไหน · การรวมตัวแปลงเป็นงานคนละรอบ (O-4) |
| **NR-9** | Google Slides **ไม่มีช่องแก้บทพูดในระบบ** — endpoint บันทึกบทพูดต้องปฏิเสธบทเรียนที่ `ContentSourceType = "google_slides"` ที่ฝั่ง server ด้วย ไม่ใช่แค่ซ่อนปุ่มใน UI |

---

## Q&A Queue Rules (R5) — contract

| # | กติกา |
|---|---|
| **QQ-1** | **นิยามของคิว (R5.1)** — `SessionQuestion` ในบริษัทนี้ที่ (`AnswerStatus == AnswerStatus.NotFound` **หรือ** `ReviewResult == ReviewResult.Incorrect`) **และ** ยังไม่มีแถว `KnowledgeQnASource` ที่ `SessionQuestionId` ชี้มา · `AnswerStatus.OutOfScope`/`NoSpeech`/`TranscriptionFailed` **ไม่เข้าคิว** (คนละปัญหา แก้ด้วยความรู้ไม่ได้) — คำถามนอกเรื่องระบบถูกกรองตั้งแต่ตอนตอบในห้องเรียนด้วยสถานะ `OutOfScope` อยู่แล้ว ไม่ใช่กลไกที่คิวต้องทำเพิ่ม (✅ Q6 ยืนยัน 2026-08-19 — ดู `## Unresolved Open Questions`) |
| **QQ-2** | **ออกจากคิวได้ทางเดียวเท่านั้น** — บันทึก Q&A ที่ผูกกับคำถามนั้น (R5.2 ปิดงานเกิดเอง) · **ไม่มีปุ่ม "ไม่ต้องตอบ" ไม่มีปุ่ม "แก้แล้ว" ไม่มีสถานะกลาง ไม่มีการมอบหมายงานให้ใคร** (✅ Q6 ยืนยัน 2026-08-19) · คำถามที่ CS อ่านแล้วตัดสินว่าไม่มีคำตอบมาตรฐาน (เช่น "แล้วแต่กรณี") **ปล่อยค้างไว้ในคิวเฉยๆ โดยเจตนา** — เจ้าของโปรเจกต์ยืนยันว่าเกิดน้อยจริง ไม่คุ้มสร้างกลไกพิเศษ |
| **QQ-3** | **แหล่งที่มาต้องแยกให้เห็นในหน้าคิว** — `not_found` (AI รู้ตัวว่าไม่มีข้อมูล) กับ "CS กดตอบผิด" ต้องกรองแยกกันได้ ไม่ใช่กองรวม · คำถามหนึ่งเป็นได้ทั้งสองพร้อมกัน (AI ตอบ not_found แล้ว CS ยังกด incorrect) ให้แสดงทั้งสองป้าย |
| **QQ-4** | **คิวรวมข้ามการเรียนและข้ามบทเรียน (P8)** — หน้าเดียว ไม่ใช่ต้องเปิดทีละการเรียน · แต่ละแถวต้องบอกได้ว่ามาจากบทเรียนไหน ซึ่งต้อง join `SessionQuestion → LearningSession → TrainingLink.LessonSlug` (สองชั้น) · **ห้าม denormalize `LessonId` ลง `SessionQuestion`** — entity นั้นเป็นของ module `learning-session` (R-9) |
| **QQ-5** | **ลบ Q&A แล้วคำถามกลับเข้าคิว** — `KnowledgeQnA` ถูก soft delete → แถว `KnowledgeQnASource` ที่ชี้ไปหามันต้องถูก soft delete ตามในทรานแซกชันเดียวกัน ทำให้คำถามเข้าเงื่อนไข QQ-1 อีกครั้ง · **นี่คือพฤติกรรมที่ต้องการ** ไม่ใช่ผลข้างเคียง: ความรู้ที่ถูกถอนออกไปแล้วแปลว่าคำถามนั้นกลับมาไม่มีคำตอบ |
| **QQ-6** | **แก้ Q&A ไม่ทำให้คำถามกลับเข้าคิว** — แก้ `Question`/`Answer` = ความรู้ยังอยู่ แค่ดีขึ้น · แต่ต้อง re-embed (KS-5) จึง enqueue `qna_index` ทุกครั้งที่ `Question` เปลี่ยน · แก้เฉพาะ `Answer` ก็ต้อง re-upsert เพราะ `Text` ที่เก็บใน Pinecone เปลี่ยน (แต่ vector เท่าเดิม — implement ให้ข้าม embed call ได้ถ้า `Question` ไม่เปลี่ยน เพื่อไม่เสียค่า embedding ฟรีๆ) |
| **QQ-7** | **หนึ่ง Q&A ปิดได้หลายคำถาม** — ตอนบันทึก CS เลือกคำถามอื่นในคิวที่ตอบด้วยคำตอบเดียวกันได้ → หลายแถว `KnowledgeQnASource` ต่อหนึ่ง `QnAId` · คำถามหนึ่งใบผูกกับ Q&A ได้หลายใบเช่นกัน (ไม่ห้าม) แต่ใบเดียวก็พอให้ออกจากคิว |
| **QQ-8** | **scope ตั้งต้นตอนเขียนคำตอบ (R5.4)** — prefill เป็น `lesson` ของบทเรียนที่คำถามนั้นเกิด **แต่ต้องให้ CS เปลี่ยนได้ก่อนบันทึกเสมอ** เพราะ requirement ระบุว่าคำถามจำนวนมากเป็นเรื่องทั่วไปที่ไม่ผูกกับบทเรียนที่บังเอิญถูกถามตอนนั้น · ห้าม auto-บันทึกโดยไม่ผ่านหน้าจอเลือก scope |
| **QQ-9** | **ใครเขียน/แก้/ลบ Q&A ได้ (R5.6/R5.7)** — `owner`/`admin`/`cs` ของบริษัทนั้นทำได้ทุกอย่างทันทีโดยไม่ต้องอนุมัติ · **แต่ `cs` ลบหรือแก้ของคนอื่นได้ไหมยังไม่ถูกกำหนด** → ดู O-1 ใน Open Questions · ระหว่างที่ยังไม่เคาะ ให้ implement เป็น "ทุกคนในบริษัทแก้/ลบของกันได้" ซึ่งตรงกับที่ `AdminRole` อธิบายสิทธิ์ `cs` ไว้ (`review answers`) และไม่ต้องเดาเพิ่ม |
| **QQ-10** | **ธงขัดแย้ง (R5.5) เป็นรายการของตัวเอง ไม่ใช่ badge บนคิว** — CS ต้องเห็นเป็นหน้า/แท็บแยกว่า "Q&A เหล่านี้ขัดกับเอกสาร" เพราะสิ่งที่ต้องทำต่อคนละอย่างกับคิว: คิว = เขียนคำตอบ · ธง = ไปแก้เอกสารต้นเหตุ · ปิดธงด้วยการกดยืนยัน (`ResolvedAt`/`ResolvedBy`) ไม่ใช่ auto |

---

## Document Intake & Job Rules (R6) — contract

| # | กติกา |
|---|---|
| **DI-1** | **5 ขั้นของการอัปโหลด** (ตามที่ requirement นิยาม): (1) เก็บไฟล์ + แถว `pending` → (2) สร้าง `BackgroundJob` → (3) แปลงไฟล์เป็นข้อความ → (4) embedding → (5) upsert Pinecone · **ขั้น 1–2 อยู่ใน request · ขั้น 3–5 อยู่ใน worker เท่านั้น** |
| **DI-2** | **ขั้น 1 ยังต้องตรวจ content type แบบ synchronous** — `DocumentParserFactory.Create` เรียกก่อนสร้าง job เหมือนเดิม ไฟล์ที่ไม่รองรับต้องได้ 400 ทันที **ไม่ใช่** ลงคิวแล้วไปล้มทีหลัง (พฤติกรรมนี้มีอยู่แล้ว ห้ามทำหาย) · ล้มที่นี่ → `IndexingStatus = failed`, `FailureReason = unsupported_type` |
| **DI-3** | **worker โหลดไฟล์เองเสมอ** — จาก `IDocumentStorageProvider.DownloadAsync(document.ObsKey)` · ห้ามส่ง `byte[]` ผ่าน `PayloadJson` และห้ามแคชไว้ในหน่วยความจำระหว่างรอคิว |
| **DI-4** | **worker ต้อง `ICompanyContext.Resolve(job.CompanyId)` เป็นสิ่งแรก** ก่อนแตะ repository ใดๆ · ไม่ทำ = query filter match ศูนย์แถว = งาน "สำเร็จ" โดยไม่ทำอะไร และเอกสารค้าง `pending` ตลอดกาลโดยไม่มี error ที่ไหนเลย (กับดักเดิมที่ comment ใน `IndexUploadedDocumentAsync` เตือนไว้ — ย้ายคำเตือนนี้ไปที่ worker ตัวใหม่ด้วย) |
| **DI-5** | **แผนที่ผลลัพธ์ → สถานะ (R6.4)** — index ได้ ≥ 1 chunk → `indexed`, `FailureReason = null` · แปลงไฟล์ throw → `failed` + `extract_failed` · แปลงได้แต่ได้ 0 chunk (หรือทุก chunk ข้อความว่าง) → `failed` + `no_text` · embed throw → `failed` + `embedding_failed` · upsert throw → `failed` + `index_failed` · **ห้ามยุบสี่กรณีหลังเป็นก้อนเดียวเหมือนวันนี้** |
| **DI-6** | **`HasSuspectCharacters` (R6.3)** — true เมื่อข้อความของ chunk นั้นมีอย่างน้อยหนึ่งใน: `\0` (NUL) · อักขระ C0 control นอกจาก `\t`/`\n`/`\r` · อักขระในช่วง Unicode PUA `U+E000`–`U+F8FF` (นี่คือเคสที่พบจริง — ฟอนต์ที่ export จาก Google Slides map วรรณยุกต์ไทยไปที่ PUA ดู `PdfSlidesRenderer.ThaiPuaGlyphFixups`) · `U+FFFD` (replacement character) · **เป็นตัวช่วยเรียงลำดับให้คนดูก่อนเท่านั้น ห้ามใช้บล็อกการ index และห้ามตั้งสถานะเป็น failed จากค่านี้** |
| **DI-7** | **CS ดูข้อความที่แปลงได้ตลอด ทุกไฟล์ (R6.3)** — ไม่ใช่เฉพาะไฟล์ที่ล้มเหลว · แสดง `DocumentChunk` เรียงตาม `SeqNo` พร้อม `ChunkKey`, `CharCount` และธง `HasSuspectCharacters` · ไฟล์ที่สถานะ `failed` แบบ `extract_failed` จะไม่มี chunk เลย → แสดงว่าแปลงไม่ได้ ไม่ใช่หน้าว่างเปล่าโดยไม่บอกอะไร |
| **DI-8** | **`DocumentChunk` ถูกแทนที่ทั้งชุดทุกครั้งที่ index สำเร็จ** — soft delete แถวเดิมของ `DocumentId` นั้นแล้วเขียนชุดใหม่ ในทรานแซกชันเดียว · ห้าม merge ทีละแถว (chunk key อาจหายไประหว่างเวอร์ชัน) |
| **DI-9** | **retry (R6.2)** — `MaxAttempts = 3` · backoff จาก `AttemptCount`: 1 → +1 นาที, 2 → +5 นาที, 3 → +15 นาที · ครบ 3 ครั้ง → `Status = failed` ถาวร ไม่ลองอีก และ `DocumentResource.FailureReason` คงค่าล่าสุดไว้ · เพดานนี้มีอยู่เพราะขั้น 4 **เสียเงินจริง** การ retry ไม่จำกัดกับไฟล์ที่ embed ไม่ผ่านจะเผาเงินเงียบๆ · CS กด "ลองใหม่" ด้วยมือได้เสมอ = สร้าง job ใบใหม่ (`AttemptCount` เริ่มที่ 0) ไม่ใช่ปลุกใบเดิม |
| **DI-10** | **สถานะที่ CS เห็น ประกอบจากสองที่** — `DocumentResource.IndexingStatus` + job ล่าสุดของเอกสารนั้น · ViewModel เพิ่ม `willRetryAt` (= `NextAttemptAt` เมื่อ job ยัง `pending`/`running` และ `AttemptCount < 3`) เพื่อให้ UI บอกได้ว่า "ล้มเหลว แต่ระบบจะลองใหม่" ต่างจาก "ล้มเหลว ต้องทำอะไรสักอย่าง" · **`LastErrorDetail` ห้าม map ลง ViewModel เด็ดขาด** |
| **DI-11** | **สตาร์ตแล้วเก็บงานค้าง (R6.2)** — ตอน `QueuedHostedService` เริ่ม ให้ `RequeueOrphanedRunning()`: ทุกแถว `Status = running` → กลับเป็น `pending`, `StartedAt = null` **โดยไม่เพิ่ม `AttemptCount`** (มันไม่ได้ล้ม มันถูกฆ่ากลางคัน) · ปลอดภัยเพราะระบบรัน instance เดียว — ถ้าวันหนึ่งรันหลาย instance ข้อนี้จะแย่งงานกันเอง (R-6) |
| **DI-12** | **worker หยิบงานแบบ atomic** — `ClaimNext` ต้อง `UPDATE ... SET Status='running' WHERE Id = (SELECT Id FROM ... WHERE Status='pending' AND NextAttemptAt <= now ORDER BY NextAttemptAt LIMIT 1 FOR UPDATE SKIP LOCKED) RETURNING *` · polling ทุก 5 วินาทีเมื่อคิวว่าง · `IgnoreQueryFilters()` เพราะหยิบข้ามบริษัท |
| **DI-13** | **ลบเอกสาร (R6.1)** — ในทรานแซกชันเดียว: soft delete `DocumentResource` (ตั้ง `IsDelete`/`DeletedAt`/`DeleteBy` แล้ว `Update` — **ไม่ใช่ `_repository.Delete()` ซึ่งเป็น hard delete**) + soft delete `DocumentChunk` ทุกแถวของมัน + สร้าง `BackgroundJob` ชนิด `vector_delete` ที่ `PayloadJson` บรรจุ `VectorId` ทั้งหมดพร้อม `NamespaceKey` · **ไฟล์ใน object storage ไม่ถูกลบ** (ต้องใช้ตอนกู้คืน) — ต่างจากวันนี้ที่ `DeleteAsync` เรียก `storageProvider.DeleteAsync` ทันที |
| **DI-14** | **กติกาเดิมที่ห้ามทำหาย** — ลบเอกสารที่เป็น `PdfDocumentResourceId` ของบทเรียนไหนอยู่ไม่ได้ ต้องคงการปฏิเสธพร้อมข้อความภาษาไทยเดิมไว้ (โค้ดเดิมอธิบายว่าถ้าปล่อยผ่านจะกลายเป็น 500 ตอนเปิดห้อง ไม่ใช่แค่ไฟล์แนบหาย) |
| **DI-15** | **กู้คืนเอกสาร (R6.1)** — ล้าง `IsDelete`/`DeletedAt`/`DeleteBy` แล้วสร้าง `document_index` job ใหม่ → แปลงและ embed ใหม่ทั้งใบจากไฟล์ที่ยังอยู่ · เสียค่า embedding อีกรอบ ซึ่ง requirement ยอมรับไว้แล้ว · `DocumentChunk` ชุดเดิมที่ soft delete ไว้ **ไม่ถูกปลุกคืน** — ชุดใหม่จาก DI-8 มาแทน |
| **DI-16** | **ลบ vector ล้มเหลวห้ามทำให้การลบเอกสารล้มเหลว** — แถว DB ลบไปแล้ว งาน `vector_delete` retry เอง · แต่ระหว่างที่ยังไม่สำเร็จ **AI ยังตอบจากเอกสารที่ถูกถอนไปแล้วได้** ซึ่งคือ P5 ที่ยังไม่ปิดสนิท → หน้ารายการต้องบอกได้ว่ายังมีงานลบ vector ค้างอยู่ (R-4) |
| **DI-17** | **`IBackgroundTaskQueue` เดิมถูกลบทิ้งทั้งตัว** — `BackgroundTaskQueue.cs` + การลงทะเบียนใน `ServiceConfiguration` · การคงไว้ควบคู่กันจะทำให้มีสองคิวที่ไม่รู้จักกัน และคนถัดไปจะหยิบตัวที่หายตอน restart ไปใช้โดยไม่รู้ตัว |

---

## Document Scope Assignment Rules (R3 — write path) — contract

> **เพิ่ม 2026-08-20** หลัง `qa-engineer` FULL รอบแรกพบว่า R3 ถูกสร้างครบเฉพาะ**ฝั่งอ่าน**:
> `DocumentResource.ScopeType` รองรับ `"category"` (DM-3) · `EnsureValidScope` บังคับ TX-5/KS-2 ได้ครบ ·
> `Resolve` คืน `ForCategory` ได้ · `GetByScope(Category, id)` ถูกใช้ใน TX-6/TX-10 แล้ว —
> **แต่ไม่มี DTO, endpoint หรือหน้าจอไหนเลยทั้ง 6 phase ที่เซ็ตค่า `"category"` ได้จริง**
> (`UploadDocumentDto` มีแค่ `LessonSlug` ซึ่งแปลว่า `lesson` หรือ `company` เท่านั้น) ·
> Q&A ได้ทางเขียนนี้ไปแล้วใน Phase 6 (`KnowledgeQnAAnswerDialog`) เอกสารไม่เคยได้
>
> **นี่คือช่องว่างของ `design.md`/`plan.md` เดิม ไม่ใช่งานที่ engineer ทำตกหล่น** — ไม่เคยมี task ไหนสั่ง
> ให้สร้างทางเขียนนี้ · ไม่ต้องย้อนกลับไป `business-analyst` เพราะ `requirement.md` R3 ("เอกสารที่วาง
> ระดับหมวดต้องตอบได้ทุกบทเรียนในหมวดนั้น") และ R5.4 (CS เลือก scope 3 ระดับ) นิยามพฤติกรรมทาง
> ธุรกิจไว้ครบแล้ว สิ่งที่ขาดคือรูปของ DTO/endpoint/UI ซึ่งเป็นการตัดสินใจเชิงระบบ

| # | กติกา |
|---|---|
| **DS-1** | **upload รับ scope ตรงๆ ไม่อนุมานจาก `LessonSlug` อีกต่อไป** — `UploadDocumentDto` **ลบ `LessonSlug` ทิ้งทั้งฟิลด์** แล้วใช้ `public required string ScopeType { get; init; }` + `public string? ScopeId { get; init; }` (รูปเดียวกับ `KnowledgeQnADto` เป๊ะ) · `UploadDocumentRequest` (multipart form ใน `DocumentsController`) รับ field `scopeType`/`scopeId` แทน `lessonSlug` · **`ScopeId` เมื่อ `ScopeType = lesson` คือ `LessonConfig.Id` ไม่ใช่ `Slug`** เพื่อให้ผ่าน resolver ตัวเดียวกับทุก entity ตาม KS-1 (`LessonConfigViewModel.Id` ถูก expose อยู่แล้ว frontend มีค่านี้ในมือ) · **ห้ามคง `LessonSlug` ไว้คู่กัน** — เหตุผลเดียวกับที่ DM-3 ลบ `LessonId` ทั้งใบ: สองช่องทางที่แปลว่า scope เหมือนกันต้องตรวจซ้ำที่ทุก call site และไม่มีอะไรบังคับได้ |
| **DS-2** | **`EnsureValidScope` ต้องถูกเรียกก่อนแตะ object storage** — `DocumentResourceService.UploadAsync` เรียก `namespaceResolver.EnsureValidScope(CurrentCompanyId, input.ScopeType, input.ScopeId)` **เป็นสิ่งแรกสุดของเมธอด ก่อน `storageProvider.UploadAsync`** ไม่ใช่หลัง · เหตุผล: ไฟล์ที่อัปเข้า storage สำเร็จแล้วแต่ validation ไม่ผ่านจะค้างอยู่โดยไม่มีแถวใน DB ชี้ถึง = ไม่มีใครลบมันได้อีกเลย · นี่คือ call site แรกของ KS-2 ฝั่งเอกสาร ซึ่ง Phase 2 เขียน resolver ทิ้งไว้รอ (`status.md` บันทึกไว้เองว่ายังไม่มีใครเรียก) |
| **DS-3** | **กรณีที่ต้องปฏิเสธ ครบทั้ง 6 — ทั้งหมดมาจาก `EnsureValidScope` เดิม ห้ามเขียน validation ชุดที่สองซ้ำ**: `ScopeType` ว่าง/ไม่รู้จัก → 400 · `lesson` ที่ `ScopeId` ไม่มีในบริษัทนี้ → 404 · `category` ที่ชี้แถว `Level == 1` → 400 (TX-5) · `category` ที่ `ScopeId` ไม่มีจริง → 404 · `company` ที่**มี** `ScopeId` → 400 (KS-2 "ปฏิเสธ ไม่ใช่เพิกเฉย") · `lesson`/`category` ที่**ไม่มี** `ScopeId` → 400 |
| **DS-4** | **หน้ารายการต้องเห็นเอกสารระดับหมวดได้** — `GET /api/documents?lessonSlug=` เปลี่ยนเป็น `GET /api/documents?scopeType=&scopeId=` · ไม่ส่ง query เลย = `company` (คงพฤติกรรมเดิมของหน้าคลังกลาง) · `IDocumentResourceService.GetByLessonSlug`/`GetStandalone` ยุบเหลือเมธอดเดียวที่รับ scope (repository `GetByScope` รองรับอยู่แล้ว ไม่ต้องแก้) · **ข้อนี้ขาดไม่ได้เด็ดขาด** — ถ้าอัปเข้าหมวดได้แต่ไม่มีหน้าไหนแสดง เอกสารจะหายจากสายตา CS ทันทีหลังอัปโหลด และเข้าไปดู chunk (DI-7) หรือลบ (DI-13) ไม่ได้เลย |
| **DS-5** | **ย้าย scope ของเอกสารที่อัปไปแล้ว** — `PATCH /api/documents/{id}/scope` body `{ scopeType, scopeId }` · ตรวจด้วย `EnsureValidScope` ชุดเดียวกับ DS-3 ทุกกรณี · นี่คือ call site แรกของ KS-4 ("เปลี่ยน scope = ย้ายบ้าน ไม่ใช่ update คอลัมน์") ซึ่งเป็น contract ที่เขียนไว้ตั้งแต่ต้นแต่ไม่เคยมีใครเรียก — ช่องว่างชนิดเดียวกับ KS-2 เป๊ะ |
| **DS-6** | **การย้ายประกอบจาก job เดิม ห้ามเพิ่ม `BackgroundJobType` ใหม่** — ในทรานแซกชันเดียว: (1) อ่าน `DocumentChunk` ของเอกสารนั้น group ตาม `NamespaceKey` แล้วสร้าง `BackgroundJob` ชนิด `vector_delete` หนึ่งงานต่อกลุ่ม `PayloadJson` เป็น `VectorDeleteJobPayload` **รูปเดียวกับ DI-13 เป๊ะ** (2) soft delete แถว `DocumentChunk` ทั้งชุด (3) เขียน `ScopeType`/`ScopeId` ใหม่ + `IndexingStatus = pending` + `IndexedChunkCount = 0` + `FailureReason = null` + `UpdateBy`/`UpdateDate` (KS-4 สั่งให้สถานะกลับเป็น `pending` เพื่อให้ CS เห็นว่ากำลังย้าย) (4) enqueue `document_index` · **worker ไม่ต้องแก้แม้บรรทัดเดียว** เพราะ `ProcessDocumentIndexAsync` resolve namespace จาก entity ตอน process อยู่แล้ว จึงเข้า namespace ใหม่โดยอัตโนมัติ · ราคาคือค่า embedding อีกรอบ = เท่ากับ DI-15 (กู้คืน) ที่ `requirement.md` ยอมรับไว้แล้ว |
| **DS-7** | **เคสขอบของการย้าย — ไม่เหลือข้อไหนให้ engineer ตัดสินเอง**: ย้ายไป scope เดิมเป๊ะ → ไม่ทำอะไร ไม่ enqueue job ใดๆ คืน 200 · เอกสารที่ไม่เคย index สำเร็จ (ไม่มี `DocumentChunk`) → **ไม่สร้าง `vector_delete`** แต่ยัง enqueue `document_index` (ตรงกับพฤติกรรมที่ Phase 4 ตั้งไว้แล้วตอนลบ) · เอกสารที่ถูก soft delete ไปแล้ว → ปฏิเสธ 404 ต้อง restore ก่อน **ห้ามย้ายของในถังกู้คืน** · เอกสารที่เป็น `LessonConfig.PdfDocumentResourceId` ของบทเรียนใดอยู่ → **ย้ายได้ ไม่บล็อก** (ต่างจาก DI-14 ที่บล็อกการ*ลบ*) เพราะ `PdfDocumentResourceId` คนละความหมายกับ `ScopeType` ตาม DM-3 ไฟล์ยังอยู่ บทเรียนยังสอนได้เหมือนเดิม · มีงาน `document_index` ค้างอยู่ตอนย้าย → **ไม่ต้องยกเลิกงานเดิม** (worker resolve namespace ตอน process จาก entity ที่ย้ายแล้ว และ DI-8 แทนที่ chunk ทั้งชุดเสมอ ส่วน vector เก่าใน namespace เดิมถูกจัดการโดย `vector_delete` ที่สร้างจาก `DocumentChunk` ณ ตอนย้าย) · สิทธิ์: เท่ากับ upload/delete เดิมทุกประการ (`owner`/`admin`/`cs` ของบริษัทนั้น) **ไม่มีกติกา role ใหม่** |
| **DS-8** | **UI ตอนอัปโหลด** — `/admin/documents` เพิ่มตัวเลือกขอบเขต: RadioGroup "ทั้งบริษัท / เฉพาะหมวด" + `Select` หมวดที่ `level === 2` แสดงเป็น "หมวดแม่ › หมวดย่อย" เมื่อเลือก "เฉพาะหมวด" · **ลอกรูปจาก `KnowledgeQnAAnswerDialog.tsx` ตรงๆ ใช้ `listKnowledgeCategories()` เดิม ห้ามสร้าง pattern ที่สองสำหรับเรื่องเดียวกัน** · `DocumentUploadList` ที่ฝังอยู่ในหน้าบทเรียน (`/admin/lessons/[slug]`, `/admin/lessons/new`) **ไม่มี picker** — คงเป็น `lesson` ของบทเรียนนั้นเสมอ เพราะบริบทของหน้ากำหนดอยู่แล้ว (มติ Q-C) |
| **DS-9** | **หน้าคลังต้องกรองตาม scope ได้ และแสดง scope ต่อแถว** — คอลัมน์ "ขอบเขต" บอก "ทั้งบริษัท" หรือชื่อหมวด ไม่งั้น CS แยกไม่ออกว่าไฟล์ไหนอยู่ระดับไหน (`DocumentResourceViewModel.ScopeType/ScopeId` มีให้อยู่แล้ว ไม่ต้องเพิ่มฟิลด์) · **หัวข้อหน้าเดิม "คลังเอกสาร (ใช้ได้ทุกบทเรียน)" ต้องแก้** เพราะไม่จริงอีกต่อไปเมื่อหน้านี้มีเอกสารระดับหมวดปนอยู่ |
| **DS-10** | **ห้ามแตะโค้ดนับของ TX-6/TX-10** — `IKnowledgeCategoryService` เรียก `GetByScope(Category, id)` ถูกต้องอยู่แล้วทั้งการบล็อกลบหมวดและ preview ย้ายหมวด · ตัวเลขจะกลายเป็นค่าจริงเองทันทีที่ DS-1 ทำงาน ไม่ต้องแก้อะไรเพิ่ม |
| **DS-11** | **ไม่มี migration ใหม่ · ไม่มีฟิลด์ใหม่ · ข้อมูลเดิมไม่ถูกแตะแม้แถวเดียว** — ฝั่ง DB เป็น **additive ล้วน** เพราะ `ScopeType`/`ScopeId` มีอยู่แล้วตาม DM-3/MG-A2/MG-A5 · สิ่งที่ **breaking** คือ **wire contract** เท่านั้น (`lessonSlug` → `scopeType`/`scopeId` ทั้งขา upload และขา list) ซึ่งมี caller 3 จุดในโปรเจกต์นี้ (`DocumentUploadList.tsx`, `lessons/[slug]/page.tsx`, `lessons/new/page.tsx` ผ่าน `api-client.ts`) และไม่มี client ภายนอก · frontend/backend ต้องเปลี่ยนพร้อมกันในเฟสเดียว ห้ามปล่อยคร่อม |
| **DS-12** | **test ที่ต้องมี (R-12)** — upload ที่ `scopeType = category` ผ่าน `EnsureValidScope` จริง (ไม่ใช่ mock ข้าม) · ครบทั้ง 6 กรณีปฏิเสธของ DS-3 · การย้ายตาม DS-6 สร้าง `vector_delete` payload ที่ `NamespaceKey`/`VectorIds` ถูกชุด แล้วตั้ง `IndexingStatus = pending` จริง · เคส "ย้ายไป scope เดิม = ไม่ enqueue อะไรเลย" และ "ไม่มี `DocumentChunk` = ไม่มี `vector_delete`" ตาม DS-7 |

---

## Migration Plan

**แยก EF migration ตาม phase ที่เป็นเจ้าของ entity** — final Data Model ยังคงเป็น 7 ตารางเดิมทุก field/index
แต่ห้ามสร้างตารางของ Phase 3–6 ล่วงหน้าใน Phase 1 และห้ามย้อนแก้ migration ที่เคย apply หรือ
rehearse แล้ว แต่ละ phase ต้องสร้าง migration ใหม่ตามลำดับด้านล่าง

| # | Migration | ขั้น |
|---|---|---|
| **MG-A1** | `AddKnowledgeTaxonomyAndScope` (Phase 1) | สร้าง `KnowledgeCategory` พร้อม index ตาม DM-15 |
| **MG-A2** | `AddKnowledgeTaxonomyAndScope` (Phase 1) | เพิ่ม `LessonConfig.CategoryId` แบบ **nullable ชั่วคราว** · เพิ่ม `DocumentResource.ScopeType`/`ScopeId`/`FailureReason` แบบ nullable ชั่วคราว |
| **MG-A3** | `AddKnowledgeTaxonomyAndScope` (Phase 1) | **backfill default chain** — สำหรับทุก `CompanyId` ที่ปรากฏใน `LessonConfig` หรือ `DocumentResource`: สร้าง `KnowledgeCategory` exactly 2 แถว — Level 1 parent ชื่อ "ยังไม่จัดหมวด" และ Level 2 leaf ชื่อ "ยังไม่จัดหมวด" ที่ `ParentId` ชี้ parent นั้น · **ทั้งคู่ `IsSystemDefault = true` โดยตั้งใจ** (ไม่ใช่ cardinality bug), `SortOrder = 9999`; ต้องมี exactly one system-default row ต่อ level ต่อบริษัท และ chain ต้องเชื่อมกัน เพราะ TX-4 บังคับให้บทเรียนผูกกับ Level 2 ขณะที่ TX-11 ต้องป้องกันโครง parent ด้วย |
| **MG-A4** | `AddKnowledgeTaxonomyAndScope` (Phase 1) | **backfill `LessonConfig.CategoryId`** = id ของแถว Level 2 ที่สร้างใน MG-A3 ของบริษัทนั้น · **backfill `DocumentResource`**: `LessonId != null` → `ScopeType = 'lesson'`, `ScopeId = LessonId` · `LessonId == null` → `ScopeType = 'company'`, `ScopeId = null` · **การแปลงนี้รักษาพฤติกรรมการค้นหาเดิมไว้ 100%** — ไม่มีเอกสารใบไหนเปลี่ยน namespace |
| **MG-A5** | `AddKnowledgeTaxonomyAndScope` (Phase 1) | ตั้ง `LessonConfig.CategoryId` และ `DocumentResource.ScopeType` เป็น `NOT NULL` · **ลบคอลัมน์ `DocumentResource.LessonId` และ index ของมัน** |
| **MG-A6** | `AddKnowledgeTaxonomyAndScope` (Phase 1) | **ไม่มีการแตะ Pinecone ใน migration** — vector ทุกตัวอยู่ namespace เดิม เพราะ MG-A4 ไม่เปลี่ยน scope ของอะไรเลย · vector เก่าจะไม่มี `metadata.sourceType` ซึ่ง KS-6 รองรับไว้แล้ว **ห้าม reindex-all เป็นส่วนหนึ่งของ deploy** |
| **MG-A7** | `AddKnowledgeTaxonomyAndScope` (Phase 1) | **ย้อนกลับได้แค่บางส่วน** — `Down()` สร้าง `LessonId` คืนจาก `ScopeId` ที่ `ScopeType = 'lesson'` ได้ แต่เอกสารที่ถูกตั้งเป็น `category` หลัง deploy จะกลายเป็น `null` (= standalone) ซึ่งเป็นพฤติกรรมที่กว้างกว่าเดิม ไม่ใช่แคบกว่า · เขียนคำเตือนนี้ไว้ในตัว migration |
| **MG-C1** | `AddDurableIndexingJobs` (Phase 3) | สร้าง `BackgroundJob` พร้อม index ตาม DM-15 |
| **MG-D1** | `AddDocumentChunks` (Phase 4) | สร้าง `DocumentChunk` พร้อม index ตาม DM-15 |
| **MG-E1** | `AddLessonSlideNarrations` (Phase 5) | สร้าง `LessonSlideNarration` พร้อม index ตาม DM-15 |
| **MG-F1** | `AddKnowledgeQnA` (Phase 6) | สร้าง `KnowledgeQnA`, `KnowledgeQnASource`, `KnowledgeQnAConflict` พร้อม index ตาม DM-15 และเพิ่ม index `(CompanyId, AnswerStatus)`/`(CompanyId, ReviewResult)` บน `SessionQuestion` |

Phase 2 ไม่มี schema migration · ชื่อข้างบนเป็นชื่อ logical migration ที่ต้องใช้เป็นฐานตอน generate;
timestamp prefix ให้ EF Core สร้างตามจริง ห้ามคัดลอกจากเอกสาร

**Phase 7 (Module G / DS-1..DS-12) ไม่มี migration เช่นกัน** — `DocumentResource.ScopeType`/`ScopeId`
ถูกสร้างไปแล้วใน MG-A2/MG-A5 และ Phase 7 ไม่เพิ่ม/ลด/เปลี่ยนฟิลด์ใดเลย · ถ้าใครกำลังจะ generate
migration ในเฟสนี้ แปลว่ากำลังทำเกิน contract — ให้หยุดแล้วตีกลับมาที่ `system-analyst`

**phased availability ของ Q&A:** ตั้งแต่ Phase 1 endpoint ของ TX-6/TX-10 ต้องคง response shape ที่มี
Q&A count แต่คืน `0` จนกว่า MG-F1 จะสร้างตารางจริง Phase 6 ต้องเปลี่ยน repository/service ให้คำนวณ
ค่าจริงใน migration/implementation round เดียวกัน ห้ามให้ Phase 1 อ้าง entity หรือตารางของ Phase 6

**เอกสารที่ต้องตามไปแก้ตอน implement:** `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md` ·
`frontend/docs/API_CONTRACT.md` · `frontend/docs/SYSTEM_LOGIC.md` (RAG/document flow) ·
`docs/PROJECT_CONTEXT.md` §19 (TD-003/TD-004 ปิดไปแล้ว) · `docs/TECH_DECISIONS.md` (บันทึกมติ
ScopeType/ScopeId และคิวถาวร) · `frontend/src/types/domain.ts` (union type ใหม่ 4 ชุด)

---

## Modules

> **หมายเหตุ:** นี่คือ Module แบบ "กลุ่มฟีเจอร์ภายใน delivery unit เดียว" ตาม conventions §1
> ไม่ใช่ module folder แยก — งานทั้งหมดนี้เป็นสินค้าตัวเดียวกันที่ถูกสร้างต่อ

### Module A — Taxonomy foundation & migration

`KnowledgeCategory` · `LessonConfig.CategoryId` · `DocumentResource.ScopeType/ScopeId` ·
migration MG-A1..MG-A7 · CRUD หมวด + หน้าเมนู · TX-1..TX-11 ตาม phased availability

**ขึ้นกับ:** ไม่มี · **ทุก module ที่เหลือขึ้นกับ A** — ต้องเสร็จเป็นก้อนเดียว ห้ามแบ่งครึ่ง
มิฉะนั้น codebase จะมีทั้ง `LessonId` และ `ScopeType` ปนกัน

**ไม่ติด Security gate** — CRUD ที่ scope ด้วย `HasQueryFilter` แบบเดียวกับ entity เดิมทุกใบ
ไม่มี input จากภายนอกที่ไม่ผ่าน auth และไม่มีข้อมูลส่วนบุคคล · **ความเสี่ยงของ module นี้เป็น
เรื่อง migration ไม่ใช่เรื่อง security** (ดู R-1)

### Module B — Knowledge scope & 3-level retrieval 🔒 Security gate

`KnowledgeNamespaces.ForCategory` · `IVoiceQuestionProvider.CategoryNamespace` ·
`RagVoiceQuestionProvider` ยิง 3 namespace · `metadata.sourceType` ทุกเส้นทาง ·
KS-1..KS-3, KS-6, KS-11

**ขึ้นกับ:** A

**🔒 เหตุผลของ gate — company isolation ของ vector store ทั้งระบบอยู่ที่นี่**: vector อยู่นอก
PostgreSQL ทำให้ `HasQueryFilter` เอื้อมไม่ถึง namespace key คือ isolation อย่างเดียวที่มี
(comment ในโค้ดเดิมย้ำเรื่องนี้ไว้ที่ `KnowledgeNamespaces`, `IVoiceQuestionProvider` และ
`IVoiceQuestionService` รวมสามจุด) · module นี้เพิ่ม key ชนิดที่สามและเป็นครั้งแรกที่ key ถูก
ประกอบจากค่าที่ **CS พิมพ์เข้ามาเอง** (หมวดของบริษัท) ไม่ใช่ค่าที่ระบบสร้าง · พลาดที่นี่
= บริษัทหนึ่งได้คำตอบจากคลังความรู้ของอีกบริษัท **โดยไม่มี error ให้เห็นเลย**
ต้องตรวจเป็นพิเศษ: TX-7 (slug ชนกับ namespace prefix), KS-1 (ห้ามประกอบ key ที่ call site),
KS-2 (`company` scope ต้องบังคับ `ScopeId == null`)

### Module C — Durable indexing queue & failure reporting 🔒 Security gate

`BackgroundJob` + worker ใหม่ · `IKnowledgeIndexProvider.DeleteVectorsAsync` ·
soft delete ของ `DocumentResource` · `FailureReason` · ลบ `IBackgroundTaskQueue` เดิม ·
DI-1..DI-5, DI-9..DI-17 · migration MG-C1

**ขึ้นกับ:** A (ต้องมี `ScopeType` ก่อนถึงจะ resolve namespace ของงานได้) · B ควรเสร็จก่อน
เพื่อให้ `vector_delete` ลบจาก namespace ที่ถูกต้องตั้งแต่แรก

**🔒 เหตุผลของ gate**: (1) งานทั้งหมดรันนอก request scope และต้อง resolve `CompanyId` เอง —
`BackgroundJob` เป็นตารางเดียวในระบบที่ **จงใจไม่มี `HasQueryFilter`** (DM-15) ทำให้ทุกจุดที่
อ่านมันจาก request ต้องกรองเอง พลาดจุดเดียว = CS บริษัทหนึ่งเห็น/สั่งงานของอีกบริษัท ·
(2) `DeleteVectorsAsync` เป็นคำสั่งลบข้อมูลถาวรที่รับ id มาจาก `PayloadJson` — id ที่ผิดบริษัท
จะลบคลังความรู้ของคนอื่นทิ้งโดยกู้ไม่ได้ · (3) R6.4 ห้ามให้ `LastErrorDetail` (ข้อความ exception
ดิบจาก provider ซึ่งอาจมี URL/คีย์/ชื่อ bucket) หลุดออก API — เป็นข้อกำหนดที่ตรวจได้ด้วยการ
อ่าน ViewModel เท่านั้น

### Module D — Extracted-text visibility 🔒 Security gate

`DocumentChunk` · หน้าดูข้อความที่แปลงได้ · `HasSuspectCharacters` · DI-6..DI-8 · migration MG-D1

**ขึ้นกับ:** C (chunk ถูกเขียนโดย worker ของ C)

**🔒 เหตุผลของ gate**: module นี้เปิด endpoint ที่คืน **เนื้อหาดิบของไฟล์ที่อัปโหลด** ออกมาทาง
API ซึ่งเป็นข้อมูลที่ละเอียดอ่อนที่สุดในระบบนี้ (คู่มือภายใน ข้อมูลลูกค้าที่ CS อัปเข้ามา) ·
วันนี้ไม่มี endpoint ไหนคืนเนื้อไฟล์เลย นี่เป็นครั้งแรก · ต้องตรวจว่า `DocumentChunk` ถูกกรอง
ด้วย `CompanyId` จริงทุกเส้นทาง และการเข้าถึงผูกกับ role ที่มีสิทธิ์ในบริษัทนั้น

### Module E — PDF narration & lesson authoring

`LessonSlideNarration` · หน้าแก้บทพูดต่อหน้า · เปิดเส้นทาง index ของบทเรียน PDF (NR-7) ·
NR-1..NR-9 · migration MG-E1 · ⚠️ **หน้าสร้างบทเรียนใหม่ + ช่องเลือกหมวด (P9)** ตามข้อเสนอ Q4

**ขึ้นกับ:** A (ฟอร์มสร้างบทเรียนต้องมีช่องหมวดตั้งแต่แรก — requirement ผูก P9 กับ R1.1 ไว้ชัด) ·
C (NR-6 enqueue job)

**ไม่ติด Security gate** — บทพูดเป็นข้อความที่ CS ในบริษัทตัวเองพิมพ์ แสดงให้ผู้เรียนของบริษัท
ตัวเองฟัง ผ่าน entity ที่มี query filter ครบ · ไม่ต่างในเชิงความเสี่ยงจากการแก้ `LessonConfig.Title`
ที่ทำได้อยู่แล้ววันนี้ · **หมายเหตุสำหรับ `qa-engineer`**: ถ้าตอน implement พบว่าหน้าสร้างบทเรียน
ไปแตะ `Slug` โดยข้าม TX-7 หรือรับ `CategoryId` โดยไม่ตรวจว่าเป็นของบริษัทตัวเอง ให้ติด gate เพิ่ม
ตามสิทธิ์ add-only ใน conventions §4

### Module F — Q&A knowledge base & review queue 🔒 Security gate

`KnowledgeQnA` · `KnowledgeQnASource` · `KnowledgeQnAConflict` ·
หน้าคิวรวม (P8/R5.1) · หน้าเขียนคำตอบ + เลือก scope · หน้าธงขัดแย้ง ·
KS-5, KS-7..KS-10 · QQ-1..QQ-10 · migration MG-F1 · เชื่อม Q&A count จริงเข้า TX-6/TX-10

**ขึ้นกับ:** A (scope) · B (`sourceType` และ 3 namespace) · C (คิว index ของ Q&A)

**🔒 เหตุผลของ gate**: (1) **ข้อความที่ `cs` พิมพ์ไหลเข้า prompt ของ LLM แล้วถูกใช้ตอบครูทุกคน
ทันทีโดยไม่มีขั้นอนุมัติ (R5.7)** — เป็น untrusted input ที่เข้าถึง output ของระบบได้โดยตรง
prompt injection ผ่านช่อง `Answer` เป็นสิ่งที่ต้องตรวจจริง ไม่ใช่ทฤษฎี · (2) `ScopeType = company`
ทำให้ Q&A ใบเดียวตอบได้ทุกบทเรียนของบริษัท ความผิดพลาดของการตรวจ scope (KS-2) ขยายผล
กว้างที่สุดที่นี่ · (3) คิวแสดง `Transcript` ของคำถามที่ผู้เรียนถามข้ามการเรียนและข้ามบทเรียน
รวมในหน้าเดียว — เป็นการรวมข้อมูลที่วันนี้กระจายอยู่ ต้องยืนยันว่าการรวมนั้นยัง scope ด้วย
`CompanyId` ครบทุกชั้นของ join (`SessionQuestion → LearningSession → TrainingLink`)

### Module G — Document scope assignment (R3 write path) 🔒 Security gate

> **เพิ่ม 2026-08-20** — ปิดช่องว่างที่ `qa-engineer` FULL รอบแรกพบ: R3 มีแต่ฝั่งอ่าน ไม่มีฝั่งเขียน

`UploadDocumentDto`/`UploadDocumentRequest` รับ `ScopeType`/`ScopeId` แทน `LessonSlug` ·
call site แรกของ `EnsureValidScope` ฝั่งเอกสาร · `GET /api/documents` กรองตาม scope ·
`PATCH /api/documents/{id}/scope` (call site แรกของ KS-4) · scope picker ในหน้า `/admin/documents` ·
DS-1..DS-12 · **ไม่มี migration**

**ขึ้นกับ:** A (ต้องมี `KnowledgeCategory` และ `ScopeType`/`ScopeId`) · B (resolver + namespace ระดับหมวด) ·
C (`BackgroundJob`/`vector_delete`/`document_index` ที่ DS-6 ใช้ประกอบการย้าย) · D (`DocumentChunk`
ที่ DS-6 อ่าน `VectorId` ออกมา) — **ทั้งสี่เสร็จแล้ว** จึงเริ่มได้ทันทีที่ Phase 3 ปิด issue ที่ค้างอยู่

**🔒 เหตุผลของ gate — นี่คือครั้งแรกที่ค่าจาก request ของ CS กำหนด namespace ของ vector ฝั่งเอกสาร
โดยตรง**: จนถึง Phase 6 `DocumentResource.ScopeType` ถูกกำหนดโดยโค้ดฝั่ง server ล้วน (`lesson` หรือ
`company` อนุมานจาก `LessonSlug`) · DS-1 เปลี่ยนให้ **CS ส่ง `ScopeType`/`ScopeId` เข้ามาเอง** ซึ่งตรงกับ
เหตุผล gate ของ Module B คำต่อคำ — namespace key คือ isolation อย่างเดียวที่ vector store มี และ
`HasQueryFilter` เอื้อมไม่ถึง · จุดที่ต้องตรวจเป็นพิเศษ: **DS-2 (เรียก `EnsureValidScope` จริงก่อนแตะ
storage ไม่ใช่แค่มีโค้ดอยู่)** · DS-3 ครบทั้ง 6 กรณีโดยเฉพาะ `company` + `ScopeId` ที่ต้องปฏิเสธ ·
`ScopeId` ที่เป็น id ของ**อีกบริษัท** ต้องตกที่ 404 ไม่ใช่ผ่าน · DS-5 ที่รับ id จาก path + scope จาก body
พร้อมกัน (IDOR สองชั้นในคำขอเดียว) · DS-6 ที่ `VectorId` ใน `PayloadJson` ต้องมาจากเอกสารของบริษัท
ผู้เรียกเท่านั้น — id ผิดบริษัทที่หลุดเข้าไปจะลบคลังความรู้ของคนอื่นถาวร (เหตุผลเดียวกับ gate ของ Module C)

**ลำดับที่แนะนำให้ `project-manager`:** A → B → C → D, E, F (สามตัวหลังขนานกันได้) → **G**

G วางไว้ท้ายสุดเพราะขึ้นกับ A/B/C/D ครบทั้งสี่ · **แต่ G ไม่ได้ขึ้นกับ E หรือ F** ถ้าจำเป็นต้องรีบ
สามารถทำขนานกับสองตัวนั้นได้ — สิ่งที่บล็อกจริงคือ Phase 3 ที่ยังมี issue cross-company ค้างอยู่
(G ใช้ `BackgroundJob`/soft delete ชุดเดียวกัน การสร้างของใหม่ทับบั๊กเดิมจะทำให้แยกไม่ออกว่าอะไรพัง)

---

## Risks & Dependencies

| # | ความเสี่ยง / การพึ่งพา | สิ่งที่ต้องทำ |
|---|---|---|
| **R-1** | **MG-A1..MG-A7 แก้ข้อมูลที่มีอยู่แล้วของทุกบริษัทและย้อนกลับได้ไม่สมบูรณ์** ส่วน MG-C1/MG-D1/MG-E1/MG-F1 เป็น additive migrations · migration 2 ใบของ `learning-session` ต้องมาก่อน MG-A1 ตาม migration history | สำหรับ local development/rehearsal ให้ apply baseline ถึง `20260818155126_AddTotalSlideCount` บน local/rehearsal DB ก่อน generate/test MG-A1..MG-A7 — local Compose ปัจจุบันผ่านเงื่อนไขนี้แล้ว จึง**ไม่ต้องรอ shared/production deployment ก่อนพัฒนา** · ก่อน apply กับ shared/production จริง `devops` ต้องยืนยันลำดับ migration ทั้งชุดและมี backup ที่กู้คืนได้ |
| **R-2** | **R3 เพิ่ม query ต่อคำถามจาก 2 เป็น 3 (หรือ 4 ถ้า Q1 = B/C)** — Pinecone read unit และ latency เพิ่มตามสัดส่วน กับ *ทุกคำถาม* ของทุกผู้เรียน | ยิงขนานด้วย `Task.WhenAll` เหมือนที่ `RagVoiceQuestionProvider` ทำอยู่แล้ว (latency ≈ ตัวช้าสุด ไม่ใช่ผลรวม) · ต้องวัด latency จริงหลัง Module B ก่อนไปต่อ |
| **R-3** | **R5.5 บังคับได้แค่ระดับ prompt** — เอกสารชนะเพราะเราสั่งโมเดล ไม่ใช่เพราะโค้ดบังคับ และโมเดลเป็นคนตัดสินเองว่าอะไรคือ "ขัดกัน" (requirement ยอมรับไว้แล้ว) | KS-7 บังคับให้แยกสองบล็อกใน prompt ซึ่งเป็นสิ่งที่ทำได้จริงที่สุด · ธง KS-9 ที่พลาดไม่ทำให้ระบบพัง แค่ CS ไม่รู้ — ยอมรับได้ในเฟสนี้ |
| **R-4** | **P5 ยังไม่ปิดสนิทแม้ทำ R6.1 แล้ว** — ระหว่างที่งาน `vector_delete` ยังไม่สำเร็จ (retry อยู่ หรือ Pinecone ล่ม) AI ยังตอบจากเอกสารที่ถูกถอนไปแล้วได้ | DI-16 บังคับให้หน้ารายการเอกสารบอกได้ว่ายังมีงานลบ vector ค้าง · ช่องว่างนี้แคบลงจาก "ตลอดกาล" เหลือ "ไม่กี่นาที" ซึ่งเป็นสิ่งที่ requirement ขอ |
| **R-5** | **`HasSuspectCharacters` จับ "เสียหาย" ได้ไม่ครบ** — ข้อความที่แปลงผิดแบบยังเป็นตัวอักษรไทยปกติ (สลับลำดับ วรรณยุกต์หาย) heuristic จับไม่ได้เลย | DI-6 ระบุชัดว่าเป็นตัวช่วยเรียงลำดับ ไม่ใช่คำตัดสิน · R6.3 ตั้งอยู่บนสายตาคนตั้งแต่แรก ห้ามให้ใครเข้าใจว่าธงนี้แทนการตรวจด้วยคนได้ |
| **R-6** | **DI-11/DI-12 ตั้งอยู่บนสมมติฐาน "รัน instance เดียว"** — วันนี้จริง (ไม่มี Dockerfile/CI ยังไม่เคย deploy) แต่ถ้าวันหนึ่ง scale out เป็นสอง instance การ requeue ตอนสตาร์ตจะฆ่างานที่อีกตัวกำลังทำอยู่ | `FOR UPDATE SKIP LOCKED` ใน DI-12 ทำให้การแย่งงานปลอดภัยอยู่แล้ว · จุดที่พังคือ DI-11 เท่านั้น — วันที่ scale out ต้องเปลี่ยนเป็น lease (`LeasedUntil`/`LeasedBy`) แทนการ requeue แบบเหมา · **ห้ามสร้าง lease ล่วงหน้าวันนี้** (`CLAUDE.md` Solution Design Rule ข้อ 7) แต่ต้องบันทึกไว้ใน `docs/TECH_DECISIONS.md` |
| **R-7** | **R5.7 = คำตอบที่ผิดถูกใช้กับครูทุกคนทันที** (requirement ยอมรับและชดเชยด้วย R5.6) | R5.6 ให้ `CreateBy`/`CreateDate`/แก้/ลบได้ · QQ-5 ทำให้การลบ Q&A ผิดๆ พาคำถามกลับเข้าคิวทันที = ย้อนได้จริงในขั้นเดียว |
| **R-8** | **หนี้เดิมที่ module นี้ไม่แก้: lesson namespace ผูกกับ `Slug`** — เปลี่ยน slug ของบทเรียนวันนี้ = vector เดิมกลายเป็นกำพร้าใน namespace เก่าและบทเรียนใหม่ไม่มีความรู้เลย โดยไม่มี error | อยู่นอก R1–R6 · หมวด (TX-8) ออกแบบให้ผูกกับ id ตั้งแต่แรกเพื่อไม่สร้างหนี้ก้อนที่สอง · บันทึกไว้ที่ O-5 |
| **R-9** | **`SessionQuestion` เป็น entity ของ module `learning-session`** — module นี้อ่านและเพิ่ม index เท่านั้น ไม่แก้ฟิลด์ใดๆ เลย (นี่คือเหตุผลที่ `KnowledgeQnASource` เป็นตารางแทนที่จะเป็นคอลัมน์) | การเพิ่ม index ใน DM-15 เป็นการแตะ `OnModelCreating` ของ entity ข้าม module — ต้องแจ้ง `learning-session` ให้ทราบตอนส่งต่อ · ถ้า `qa-engineer` ถือว่าเป็น drift ให้ตีกลับมาที่ `system-analyst` เพื่อ amend `learning-session/design.md` ให้ตรงกัน |
| **R-10** | **`learning-session/design.md` ไม่ตรงกับโค้ดจริงอยู่แล้ววันนี้** — DM-3 ของไฟล์นั้นระบุ `SessionQuestion.LearningSessionId` แต่โค้ดจริงยังใช้ `SessionId` (ตรวจแล้วที่ `SessionQuestion.cs` และ `ApplicationDbContext`) เช่นเดียวกับมติ Q2 ที่ถูกยกเลิกไปแล้ว | **module นี้ยึดโค้ดจริง (`SessionId`)** ตามแบบเดียวกับที่เจ้าของโปรเจกต์ตัดสินเรื่องชื่อ `TrainingLink` · drift นี้เป็นของ module `learning-session` ไม่ใช่ของเรา — รายงานให้ `qa-engineer` route ห้ามแก้เอง |
| **R-11** | **`CLAUDE.md` §Known Baseline ล้าสมัยสองข้อ** — "ไม่มี auth/rate limiting (TD-002)" ไม่จริงแล้ว (`AdminUser` + `AdminRole` + `IAuthorizationGuard` มีครบ) · module นี้ออกแบบโดยอาศัยข้อเท็จจริงว่ามี auth (R5.6 ใช้ `CreateBy` เป็นค่าจริง) | ต้องอัปเดต `CLAUDE.md` และ `docs/PROJECT_CONTEXT.md` §19 ตอน implement · ถ้า auth **ยังไม่ครอบ `/admin/*` จริง** ให้ตีกลับมาที่ `system-analyst` ทันที เพราะ R5.6/R5.7 ทั้งคู่ตั้งอยู่บนข้อเท็จจริงนี้ |
| **R-12** | **ไม่มี automated test ที่ยืนยันกติกาพวกนี้ได้** — `backend/tests` มีอยู่แต่ API integration test ยังเป็น template และบางชุดยิง provider จริง | contract section ทั้ง 5 ชุดนี้เขียนมาให้ `qa-engineer` อ่านเทียบโค้ดได้ทีละข้อ · จุดที่ควรมี unit test จริงที่สุดสามจุด: KS-1 (namespace resolver), DI-5 (แผนที่ผลลัพธ์→สถานะ), QQ-1 (นิยามคิว) — ทั้งสามเป็น pure logic ที่ test ได้โดยไม่ต้องมี provider |
| **R-13** | **`IsSystemDefault` เป็น flag ของ chain ไม่ใช่ unique-row flag** — การ query เพียง `IsSystemDefault` จะได้สองแถวและ `SingleOrDefault()` จะ throw; การใช้ `FirstOrDefault()` จะซ่อนข้อมูลผิดรูปและอาจคืน parent ที่ assign ไม่ได้ | `GetSystemDefault()` ต้อง filter `IsSystemDefault && Level == 2` ก่อน `SingleOrDefault()` เพื่อคืน assignable leaf แบบ deterministic และยัง fail-fast เมื่อมี leaf ซ้ำ · migration/repository tests ต้องยืนยัน exactly 2 flagged rows ต่อบริษัท, one per level, linked chain, `LessonConfig.CategoryId` ชี้ leaf และ update/delete ของทั้ง parent+leaf ถูกบล็อก |
| **R-14** | **contract ที่ "เขียนไว้แล้วแต่ไม่มี call site" ไม่ถูกจับโดยอะไรเลย** — KS-2 (`EnsureValidScope`) และ KS-4 (เปลี่ยน scope = ย้ายบ้าน) ทั้งคู่ถูก implement ถูกต้อง มี unit test ผ่าน และผ่าน QA มาหกเฟส **โดยไม่เคยมีใครเรียกใช้จริงฝั่งเอกสาร** · `status.md` ของ Phase 2 บันทึกไว้เองด้วยซ้ำว่า "ยังไม่มี call site" แต่ไม่มีกลไกไหนพาข้อความนั้นไปโผล่ใน `plan.md` เป็น task | รอบนี้ปิดด้วย DS-2/DS-5 · **บทเรียนสำหรับรอบต่อไป: contract ข้อไหนที่ระบุพฤติกรรมของ "ตอนบันทึก" ต้องมี task คู่กันที่ระบุ *ใครเรียก* ไม่ใช่แค่ *มีฟังก์ชัน*** · `qa-engineer` ควรถือว่า "ฟังก์ชันที่ไม่มี production call site" เป็นสัญญาณเตือน ไม่ใช่โค้ดที่ผ่าน |
| **R-15** | **DS-1/DS-4 เปลี่ยน wire contract ของ endpoint ที่ frontend ใช้อยู่จริงแล้ว** (`lessonSlug` → `scopeType`/`scopeId` ทั้งขา upload และขา list) — ต่างจากทุก phase ก่อนหน้าที่เป็นการ *เพิ่ม* endpoint ใหม่ · ปล่อยคร่อมครึ่งเดียวเมื่อไหร่ หน้าอัปโหลดเอกสารพังทั้งหน้า | ต้องแก้ backend + `api-client.ts` + ทั้ง 3 caller **ในเฟสเดียวกัน** ห้ามแยกเป็นสอง phase · ข้อดีคือไม่มี client ภายนอกเลย ความเสี่ยงจึงจบภายใน repo นี้ · `frontend/docs/API_CONTRACT.md` ต้องอัปเดตในรอบเดียวกัน |

---

## Unresolved Open Questions

> **ไม่มีคำถามค้างแล้ว — พร้อมส่งต่อ `project-manager`**
> 6 ข้อที่เคยอยู่ในหัวข้อนี้ (Q1–Q6) **เจ้าของโปรเจกต์เคาะครบเมื่อ 2026-08-19** — Q1–Q5 ตรงตาม
> ข้อเสนอเดิมทุกข้อ · **Q6 เจ้าของโปรเจกต์ทักท้วงข้อเสนอเดิมและเลือกทิศทางที่ถูกกว่า** (รายละเอียด
> อยู่ในตารางข้างล่าง) เก็บตารางไว้เป็นบันทึกมติ ไม่ใช่รายการรอคำตอบ · ส่วน "ค้างไว้โดยตั้งใจ"
> ข้างล่าง **ยังมีผลบังคับเต็มที่**

### มติที่ปิดแล้ว (ยืนยัน 2026-08-19 โดยเจ้าของโปรเจกต์)

| # | คำถามเดิม | ✅ มติ | อยู่ในเอกสารที่ไหน |
|---|---|---|---|
| **Q1** | R3 บอก "3 ระดับ" แต่ R1 ให้หมวดมี 2 ชั้น — "หมวด" ใน R3 คือชั้นไหน? | **A: subcategory เท่านั้น** — บทเรียนอยู่ Level 2 เสมอ · หมวดใหญ่ (Level 1) จัดเมนูล้วน ไม่มี namespace ของตัวเอง | TX-4 · TX-5 · KS-3 |
| **Q2** | R1.2 (ลบหมวดที่มีของอยู่ไม่ได้) เป็นข้อที่อนุมานจาก R1.1 ยังไม่เคยยืนยันตรงๆ | **บล็อกแบบ hard block** บอกจำนวนแยกประเภทที่ยังค้างอยู่ | TX-6 |
| **Q3** | ข้อมูล `LessonConfig`/`DocumentResource` ที่มีอยู่แล้ววันนี้จะ migrate ยังไง | **หมวดตั้งต้น "ยังไม่จัดหมวด" (`IsSystemDefault`) + backfill อัตโนมัติ** ใช้งานได้ทันทีหลัง deploy ไม่บล็อกระบบ | DM-1 (`IsSystemDefault`) · MG-A3 · MG-A4 |
| **Q4** | P9 (ไม่มี UI สร้างบทเรียนใหม่) เข้า scope module นี้ไหม? | **เข้า scope แบบขั้นต่ำ** — ใช้ component หน้าแก้ไขเดิมซ้ำ เพิ่มช่องเลือกหมวด | Module E |
| **Q5** | R6.3 (ดูข้อความที่แปลงได้) — เก็บ chunk ลง DB หรือ re-parse ตอนดู? | **เก็บลง DB (`DocumentChunk`)** — ข้อความที่ CS เห็นต้องเป็นชุดเดียวกับที่ AI ใช้ตอบจริง 100% ไม่ใช่แปลงสดใหม่ทุกครั้ง | DM-4 |
| **Q6** | คำถามในคิวที่ตอบไม่ได้จริงๆ จะออกจากคิวยังไง? | **เจ้าของโปรเจกต์ปฏิเสธข้อเสนอเดิม** (ปุ่ม "ไม่ต้องตอบ" + `QuestionQueueDismissal`) **และให้เหตุผลที่ถูกกว่า**: คำถามนอกเรื่องระบบต้องถูกกรองตั้งแต่ในห้องเรียนตอนที่ครูถาม ไม่ใช่มากรองทีหลังในคิว — ตรวจโค้ดยืนยันว่าระบบทำแบบนี้อยู่แล้ววันนี้จริง (`AnswerStatus.OutOfScope` แยกจาก `NotFound`, QQ-1 ดึงจาก `NotFound` เท่านั้น) ขอบเขตจริงที่เหลือแคบกว่าเดิมมาก (คำถามเกี่ยวกับระบบจริงแต่ไม่มีคำตอบมาตรฐาน) และเลือก **ปล่อยค้างไว้ในคิว ไม่มีกลไกพิเศษ** เพราะเกิดน้อยจริง | DM-9 (ตัดออกทั้งตาราง) · QQ-1 · QQ-2 |

**การรื้อมติเหล่านี้ต้อง amend เอกสารนี้ก่อน** — engineer ที่เจอทางเลือกอื่นในตารางข้างบนให้อ่านเป็น
บันทึกเหตุผล ไม่ใช่ทางเลือกที่ยังหยิบได้

### 🟡 ค้างไว้โดยตั้งใจ — ไม่บล็อกการเริ่มงาน

| # | เรื่อง | สถานะ |
|---|---|---|
| **O-1** | **`cs` แก้/ลบ Q&A ของคนอื่นในบริษัทเดียวกันได้ไหม** | requirement ไม่ได้พูดถึง · QQ-9 กำหนดค่าเริ่มต้นเป็น "ได้" ซึ่งตรงกับที่ `AdminRole` อธิบายสิทธิ์ `cs` ไว้ · ถ้าอยากจำกัดให้แก้ได้เฉพาะของตัวเอง ต้อง amend QQ-9 ก่อน implement Module F **ห้าม engineer ตัดสินเอง** |
| **O-2** | **Q&A ไม่มีวันหมดอายุ** | R5.6 ระบุชัดว่า "ยังไม่ทำวันหมดอายุอัตโนมัติในเฟสนี้" — **นอก scope โดยเจตนา ห้าม implement** · ถ้าจะทำต้องกลับมา amend ก่อน |
| **O-3** | **soft delete ไม่ทั่วทั้งระบบ** | module นี้เติม `!IsDelete` ใน query filter เฉพาะ `DocumentResource` + ตารางใหม่ · `LessonConfig`/`TrainingLink`/`LearningSession`/`SessionQuestion`/`ChatMessage` ยังลบจริงเหมือนเดิม — **นอก scope โดยเจตนา** การไล่เติมจะเปลี่ยนพฤติกรรมของ module `learning-session` โดยไม่มีใครขอ · ถ้าอยากให้ทั้งระบบสม่ำเสมอ ต้องเปิดรอบใหม่ |
| **O-4** | **บทเรียน PDF ยังใช้ตัวแปลงสองตัวกับไฟล์เดียวกัน** | NR-8 ยอมรับความซ้ำนี้ในเฟสนี้ (บทพูดจาก `PdfSlidesRenderer` เก็บหน้าว่าง · คำตอบจาก `PdfTextExtractor` ตัดหน้าว่าง) — การรวมสองตัวแปลงเป็นงานคนละรอบ **ห้ามรวมเองระหว่าง implement** |
| **O-5** | **เปลี่ยน `LessonConfig.Slug` แล้ว vector กำพร้า** | หนี้เดิม อยู่นอก R1–R6 (R-8) · ถ้าจะแก้ในรอบนี้ต้อง amend เพิ่ม |
| **O-6** | **รายงานเทียบข้ามบริษัท** | ตัดออกโดยเจตนาแล้วตาม R2 — **นอก scope** |
| **O-7** | **OCR สำหรับ PDF สแกน · override บทพูด Google Slides · PPTX เป็นสื่อสอนหลัก · auto-detect เด็คต้นทางถูกแก้** | ทั้งสี่อยู่ในหัวข้อ "ที่ตัดออกจากเฟสนี้โดยตั้งใจ" ของ `requirement.md` — **ห้าม implement โดยไม่ amend `requirement.md` ก่อน** |
| **O-8** | **หน้าบทเรียนยังไม่แสดง "เอกสารระดับหมวดที่บทเรียนนี้ได้รับมรดก"** (เพิ่ม 2026-08-20, มติ Q-D) | วันนี้หน้าบทเรียนแสดงเฉพาะเอกสารที่ `ScopeType = lesson` ของตัวเอง · หลัง Phase 7 บทเรียนจะถูกตอบด้วยเอกสารระดับหมวดและระดับบริษัทด้วย (KS-3 ยิง 3 namespace) แต่ CS มองไม่เห็นจากหน้าบทเรียนว่ามีอะไรบ้าง — ต้องเดาเอง · **เจ้าของโปรเจกต์ตัดสินให้เลื่อน ไม่ใช่มองข้าม** เป็นรายการอ่านอย่างเดียวที่เพิ่มทีหลังได้โดยไม่กระทบ schema/contract ใดๆ (ข้อมูลครบอยู่แล้วผ่าน `GET /api/documents?scopeType=category&scopeId=` ตาม DS-4) · **ห้าม implement แถมระหว่าง Phase 7** |
| **O-9** | **เอกสาร/Q&A หนึ่งใบอยู่ได้หมวดเดียว — ยังไม่รองรับหลายหมวดพร้อมกัน** (เพิ่ม 2026-08-20, มติ Q-G) | `ScopeId` เป็นช่องเดียวตาม DM-3/DM-6 · **ถามเจ้าของโปรเจกต์ตรงๆ แล้วเมื่อ 2026-08-20 ว่ามีทิศทางนี้ในอนาคตอันใกล้ไหม — คำตอบคือยังไม่มี/ยังไม่คิดถึง** จึงตั้งใจไม่ออกแบบตารางเชื่อม many-to-many ล่วงหน้า (`CLAUDE.md` Solution Design Rule ข้อ 7) · **แต่ราคาของการเปลี่ยนใจไม่ถูก**: ต้องเพิ่มตารางเชื่อม แล้วรื้อ `EnsureValidScope`, `Resolve`, `GetByScope`, `vector_delete` payload, TX-5/TX-6/TX-10 และ DS-1..DS-7 ทั้งชุด เพราะทุกจุดตั้งอยู่บนสมมติฐาน "หนึ่ง scope ต่อหนึ่งแถว" · ถ้าวันหนึ่งต้องการจริง **ต้อง amend เอกสารนี้ก่อน ห้าม engineer ขยายเอง** |

### เรื่องที่ตรวจสอบครบแล้วในรอบนี้ (ไม่มีอะไรตกค้าง)

ทุกฟิลด์/ตารางที่มีอยู่ก่อนหน้าและเกี่ยวข้องกับ module นี้ ได้รับคำตัดสินแล้วทุกใบ:

| ของเดิม | คำตัดสิน |
|---|---|
| `DocumentResource.LessonId` | **ทำเลย** — ถูกแทนที่ด้วย `ScopeType`/`ScopeId` (DM-3, MG-A4/MG-A5) |
| `DocumentResource.IndexingStatus`/`IndexedChunkCount` | **ทำเลย** — คงไว้ เพิ่ม `FailureReason` ข้างๆ (DI-5) |
| `DocumentResource` audit fields ที่เป็น `init` | **ทำเลย** — ต้องเปลี่ยนเป็น `set` ไม่งั้น soft delete compile ไม่ผ่าน (DM-3) |
| `LessonConfig.SlideConfigs` (owned JSON) · `PresentationId` · `SlidesEmbedUrl` · `IsActive` | **ไม่แตะ** — ไม่เกี่ยวกับ R1–R6 |
| ~~`LessonConfig.IntroWaitMs`/`BreathPauseMs`/`FinalQuestionWaitMs`~~ | **ไม่มีอยู่ในระบบแล้ว** — ถูก drop ทั้งสามคอลัมน์โดย `20260822143217_RemoveLessonConfigPacingOverrides` (มติ Module P N1/N2/N3 ของ `company-admin`, 2026-08-22) · ค่ากลางย้ายไป `Company.Default*Ms` ซึ่งเป็นของโมดูลนั้น — **นอก scope ของเอกสารนี้** (แก้ 2026-08-25, D-3 · ดูกล่องหมายเหตุที่ DM-2) |
| `LessonConfig.PdfDocumentResourceId` | **ไม่แตะ** — คนละความหมายกับ `ScopeType` (DM-3) แต่เป็น trigger ของ NR-3 |
| `SessionQuestion.ReviewResult`/`ReviewNote`/`ReviewedAt` | **ทำเลย (อ่านอย่างเดียว)** — เป็น input แหล่งที่ 2 ของคิว (QQ-1) ไม่แก้ shape |
| `SessionQuestion.SessionId` (ชื่อจริงในโค้ด) | **ไม่แตะ** — ยึดตามโค้ดจริง drift กับ `learning-session/design.md` เป็นของ module นั้น (R-10) |
| `IBackgroundTaskQueue` + `BackgroundTaskQueue.cs` | **ทำเลย — ลบทิ้ง** (DI-17) |
| `IKnowledgeIndexProvider.DeleteNamespaceAsync` | **ไม่แตะ** — `IAdminService` reindex-all ยังใช้อยู่ · เพิ่ม `DeleteVectorsAsync` ข้างๆ (DM-13) |
| `KnowledgeNamespaces.For`/`ForGlobal` | **ไม่แตะ** — เพิ่ม `ForCategory` ข้างๆ (DM-12) |
| `IsDelete`/`DeletedAt` ที่มีในทุก entity แต่ไม่เคยใช้ | **ทำเลยบางส่วน** — ใช้จริงเฉพาะ `DocumentResource` + ตารางใหม่ · ที่เหลือ **นอก scope โดยเจตนา** (O-3) |
| `SessionStatus.cs` (`NOT_STARTED`/`IN_PROGRESS`/`ENDED`/`EXPIRED`) | **นอก scope** — เป็นของ module `learning-session` |
| `docs/KNOWLEDGE_ROADMAP.md` (K0–K4) | **นอก scope** — คนละชั้น (retrieval/eval quality) ไม่ทับกับเอกสารนี้ ตามที่ `requirement.md` ระบุ |

---

## Change Log

- 2026-08-19 — สร้างเอกสารครั้งแรกจาก `requirement.md` (R1–R6, P1–P9) · ยืนยัน feasibility
  ครบทุกข้อด้วย stack เดิม ไม่ต้องเพิ่ม dependency · Data Model 16 ส่วน (ตารางใหม่ 8 · แก้ของเดิม 3 ·
  constants 4 ชุด · interface 2 ตัว) · contract 5 ชุด (Taxonomy · Knowledge Scope & Retrieval ·
  PDF Narration · Q&A Queue · Document Intake & Job) · Migration Plan MG-1..MG-7 ·
  Module A–F พร้อม 🔒 Security gate ที่ B, C, D, F · **ยังไม่ผ่านการยืนยัน — ค้าง 6 คำถาม (Q1–Q6)**
  · บันทึก drift 2 จุดที่เจอระหว่างตรวจโค้ด: `learning-session/design.md` DM-3 ระบุ
  `SessionQuestion.LearningSessionId` แต่โค้ดจริงเป็น `SessionId` (R-10) และ `CLAUDE.md`
  §Known Baseline "ไม่มี auth (TD-002)" ล้าสมัยแล้ว (R-11)
- 2026-08-19 — เจ้าของโปรเจกต์เคาะครบทั้ง 6 ข้อ (Q1–Q6) **เอกสารนี้เป็น contract แล้ว พร้อมส่งต่อ
  `project-manager`** · Q1/Q2/Q3/Q4/Q5 ยืนยันตรงตามข้อเสนอเดิมทุกข้อ ไม่มีการแก้ Data Model
  ในส่วนนั้น · **Q6 เปลี่ยนทิศทางจากข้อเสนอเดิม** — เจ้าของโปรเจกต์ปฏิเสธปุ่ม "ไม่ต้องตอบ"
  (`QuestionQueueDismissal`) และชี้ว่าคำถามนอกเรื่องระบบต้องถูกกรองตั้งแต่ตอนครูถามในห้องเรียน
  ไม่ใช่มากรองทีหลังในคิว ตรวจโค้ดยืนยันแล้วว่าระบบทำแบบนี้อยู่แล้ววันนี้จริง (`AnswerStatus.cs`
  มี `OutOfScope` แยกจาก `NotFound` และ QQ-1 ดึงคิวจาก `NotFound` เท่านั้น) — **ตัด DM-9
  (`QuestionQueueDismissal`) ออกทั้งตาราง** พร้อมทุกจุดที่อ้างถึง (DbSet, `OnModelCreating`,
  repository, MG-1, Module F) และแก้ QQ-1/QQ-2 ให้สอดคล้อง ขอบเขตจริงของ Q6 ที่เหลือ (คำถามที่
  เกี่ยวกับระบบจริงแต่ไม่มีคำตอบมาตรฐาน) เลือกปล่อยค้างไว้ในคิวเฉยๆ โดยเจตนา ไม่มีกลไกพิเศษ ·
  ลบกล่องสถานะ "ฉบับรอยืนยัน" ที่หัวเอกสาร เปลี่ยนเป็นยืนยันว่า implement ได้ทันที · ปรับ
  `## Unresolved Open Questions` ให้เป็นตารางมติที่ปิดแล้วตาม pattern ของ
  `learning-session/design.md` · drift 2 จุดที่บันทึกไว้ในรอบก่อน (R-10, R-11) ยังไม่แก้ —
  เป็นของ module อื่น/เอกสารอื่น รายงานไว้พอ
- 2026-08-19 — ยืนยัน contract amendment หลัง `backend-engineer` พบ migration phasing ขัดกัน ·
  คง final Data Model เดิมทั้ง 7 ตาราง แต่แยก migration ตามเจ้าของ phase เป็น MG-A1..MG-A7,
  MG-C1, MG-D1, MG-E1 และ MG-F1 · กำหนด TX-5/TX-6/TX-10 แบบ phased availability โดย Phase 1–5
  คืน Q&A count เป็น `0` และ Phase 6 เชื่อมค่าจริงในรอบเดียวกับ MG-F1 · ชี้ชัดว่า local development
  ใช้ local/rehearsal migration baseline ได้โดยไม่ต้องรอ shared/production deployment; การ apply
  กับ shared/production ยังต้องผ่าน DevOps hard stop และ backup
- 2026-08-19 — ยืนยัน `IsSystemDefault` เป็น default chain สองแถวต่อบริษัทโดยตั้งใจ: Level 1 parent
  + Level 2 leaf ที่เชื่อมกัน และทั้งคู่ถูกป้องกันตาม TX-11; เฉพาะ leaf เท่านั้นที่ assign ได้ ·
  แก้ contract ของ `GetSystemDefault()` ให้ filter `IsSystemDefault && Level == 2` ก่อน
  `SingleOrDefault()` เพื่อคืน leaf อย่าง deterministic และ fail-fast เมื่อมี leaf ซ้ำ · final schema
  shape กับ migration SQL ไม่เปลี่ยน เพราะ MG-A3 implementation สร้างสองแถวตาม invariant นี้อยู่แล้ว
- 2026-08-20 — **amend: เพิ่มทางเขียนของ R3 (Module G / Phase 7) หลัง `qa-engineer` FULL รอบแรก
  พบว่า "คลังความรู้ระดับหมวด" ถูกสร้างครบเฉพาะฝั่งอ่าน** — `DocumentResource.ScopeType = "category"`
  มีอยู่ใน schema/validation/retrieval ครบตั้งแต่ Phase 1–2 แต่ไม่มี DTO, endpoint หรือหน้าจอไหนเลย
  ทั้ง 6 phase ที่เซ็ตค่านี้ได้จริง (`UploadDocumentDto` มีแค่ `LessonSlug` = `lesson` หรือ `company`
  เท่านั้น) · ยืนยันจาก `requirement.md` R3/R1/R5.4 แล้วว่าเจตนาเดิมคือให้ CS วางเอกสารระดับหมวดได้จริง
  **ไม่ใช่แค่ให้ระบบรองรับค่านี้ภายใน** จึงไม่ต้องย้อนกลับไป `business-analyst` — ที่ขาดคือรูปของ
  DTO/endpoint/UI ซึ่งเป็นการตัดสินใจเชิงระบบ · **เพิ่มหัวข้อ contract ใหม่ `## Document Scope
  Assignment Rules` (DS-1..DS-12)** · เพิ่มแถว R3-W ใน Feature-by-Feature · เพิ่ม 6 มติใหม่
  (Q-A..Q-G) ในตาราง "การตัดสินใจที่ผู้ใช้ยืนยันแล้ว" · เพิ่ม **Module G 🔒 Security gate** และ
  ลำดับ `... → G` · เพิ่ม R-14 (contract ที่ไม่มี call site ไม่ถูกจับโดยอะไรเลย — KS-2/KS-4 ผ่าน QA
  มาหกเฟสโดยไม่เคยถูกเรียก) และ R-15 (wire contract breaking ต้องแก้สองฝั่งในเฟสเดียว) ·
  เพิ่ม O-8 (มรดกเอกสารระดับหมวดในหน้าบทเรียน — เลื่อนโดยรู้ตัว) และ O-9 (หลายหมวดต่อเอกสาร —
  ถามแล้ว ยังไม่ต้องการ ไม่ออกแบบล่วงหน้า)
  · **additive/breaking**: ฝั่ง DB **additive ล้วน ไม่มี migration ใหม่ ไม่มีฟิลด์ใหม่ ข้อมูลเดิมไม่ถูก
  แตะแม้แถวเดียว** (`ScopeType`/`ScopeId` สร้างไปแล้วใน MG-A2/MG-A5) · ฝั่ง wire contract **breaking**
  ที่ `POST /api/documents` และ `GET /api/documents` (`lessonSlug` → `scopeType`/`scopeId`) ซึ่งมี
  caller 3 จุดใน repo นี้และไม่มี client ภายนอก — เจ้าของโปรเจกต์เลือกทางนี้เอง (Q-B) แทนการคง
  สองช่องทางไว้คู่กัน · **final Data Model ไม่เปลี่ยนแม้แต่ฟิลด์เดียว Phase 1–6 ไม่ต้องแก้ย้อนหลัง**
- 2026-08-25 — **amend เฉพาะเอกสาร: ปิดหนี้ D-3 (doc drift จาก Module P ของ `company-admin`)** ·
  DM-2 และตาราง "เรื่องที่ตรวจสอบครบแล้วในรอบนี้" ยังนับ `LessonConfig.IntroWaitMs` /
  `BreathPauseMs` / `FinalQuestionWaitMs` เป็นฟิลด์ที่มีอยู่จริง แต่ทั้งสามถูก drop ไปแล้วโดย
  migration `20260822143217_RemoveLessonConfigPacingOverrides` (ตามมติ Module P N1/N2/N3 ที่
  เจ้าของโปรเจกต์เคาะเมื่อ 2026-08-22: จังหวะการสอนเป็นค่ากลางระดับบริษัทอย่างเดียว ไม่มี override
  ต่อบทเรียน) · ตรวจกับของจริงแล้วทั้ง 3 จุด: `SupportRoom.Domain/Entities/LessonConfig.cs`
  ไม่มีสามฟิลด์นี้แล้ว · `ApplicationDbContext.OnModelCreating` บล็อก `LessonConfig` เหลือ
  `HasKey` + `(CompanyId, Slug) IsUnique` + `HasIndex(CategoryId)` + `OwnsMany(SlideConfigs).ToJson()`
  + query filter ตรงตาม DM-2 เดิมทุกบรรทัด · ค่ากลางอยู่ที่ `Company.DefaultIntroWaitMs`/
  `DefaultBreathPauseMs`/`DefaultFinalQuestionWaitMs` (`int` non-null) **ซึ่งเป็นของโมดูล
  `company-admin` — เอกสารนี้อ้างถึงอย่างเดียว ไม่ประกาศเป็นของตัวเอง** · **ไม่มีการเปลี่ยน
  การตัดสินใจ ไม่มี migration ใหม่ ไม่มีผลต่อ R1–R6 / contract ชุดใด / Module A–G ข้อใด** —
  สามฟิลด์นี้ถูกจัดเป็น "ไม่แตะ" มาตั้งแต่รอบแรก จึงไม่มี feasibility หรือ risk ข้อไหนที่ตั้งอยู่บน
  สมมติฐาน per-lesson pacing · ผลที่ได้คือ QA รอบหน้าของโมดูลนี้จะไม่รายงานสามคอลัมน์นี้เป็น drift
  ผิดๆ อีก · **ไม่แตะเอกสารของโมดูลอื่นแม้แต่ไฟล์เดียว** (`company-admin/design.md` บันทึกมติของ
  ตัวเองครบถูกต้องอยู่แล้ว)
