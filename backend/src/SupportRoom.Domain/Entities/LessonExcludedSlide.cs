using SupportRoom.Domain.Common;

namespace SupportRoom.Domain.Entities;

/// <summary>
/// หน้าของบทเรียน PDF ที่ CS ตัดออกจากบทเรียน (R4.7) - "มีแถวที่ยังไม่ถูก soft delete" = หน้านั้น
/// ถูกตัดอยู่ตอนนี้ · เอาหน้ากลับ = soft delete แถว **ไม่ใช่ hard delete** เพราะ R4.7.7 ให้ตัด/เอากลับ
/// ได้ไม่จำกัดครั้ง และประวัติว่าใครตัดเมื่อไหร่/ใครเอากลับเมื่อไหร่คือเหตุผลหนึ่งที่ Q-K1 เลือกทางนี้
///
/// ทำไมเป็นตารางแยก ไม่ใช่ธงใน SlideConfigs (มติ Q-K1): LessonConfigService.SaveAsync เขียนทับ
/// SlideConfigs ทั้งก้อนจากค่าที่ client ส่งมาทุกครั้ง - ธงที่อยู่ในนั้นจะหายเงียบ ๆ ตอนใครสักคน
/// กดบันทึกบทเรียนเรื่องอื่น โดยไม่มี error ไม่มี log
///
/// ใช้กับ ContentSourceType = "pdf" เท่านั้น (R4.7.1) - Google Slides จัดการหน้าที่ต้นทาง (NR-9)
///
/// ⚠️ ไม่มีฟิลด์อื่นในตารางนี้และห้ามเติม (CR-3.13 ปฏิเสธ Reason/ExcludedByRole/SortOrder/
/// IsSkipTeachingOnly ไปแล้ว) - ดู design.md DM-17
/// </summary>
public sealed class LessonExcludedSlide : IEntityMaster<string>, ICompanyScoped
{
    public required string Id { get; init; }              // IdGenerator.GenerateId("exsl")
    public required string CompanyId { get; init; }
    public string? CreateBy { get; init; }
    public DateTime CreateDate { get; init; }
    public string? UpdateBy { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? DeleteBy { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? DeletedAt { get; set; }

    public required string LessonId { get; init; }

    /// <summary>"pdf-page-N" ตามที่ PdfSlidesRenderer สร้าง (N = เลขหน้าจริงของไฟล์ เริ่มจาก 1)
    /// ค่าเดียวกับ LessonSlideNarration.SlideObjectId เป๊ะ - นี่คือสิ่งที่ทำให้ R4.7.8 (บทพูดเดิม
    /// คืนมาตรง ๆ) ได้มาฟรี
    ///
    /// ⚠️ ห้ามเก็บเลขหน้าที่คนเห็น (1-9 ตาม R4.7.6) เด็ดขาด ไม่ว่าจะเป็น int แยกหรือคอลัมน์เสริม -
    /// ค่านั้นเปลี่ยนทุกครั้งที่มีการตัด/เอากลับหน้าอื่น แถวนี้จะชี้ผิดหน้าโดยไม่มี error
    /// เหตุผลเดียวกับ NR-4 คำต่อคำ</summary>
    public required string SlideObjectId { get; init; }
}
