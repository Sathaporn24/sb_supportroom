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
