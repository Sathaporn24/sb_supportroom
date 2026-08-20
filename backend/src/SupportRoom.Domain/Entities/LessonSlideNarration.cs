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
