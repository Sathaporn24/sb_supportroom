namespace SupportRoom.Domain.Enums;

/// <summary>String constants ด้วยเหตุผลเดียวกับ BackgroundJobStatus/AnswerStatus - ห้ามใช้ C# enum
/// (convention ของโปรเจกต์) · R5: เพิ่มค่าใหม่ที่นี่ได้โดยไม่ต้องแตะ schema</summary>
public static class AuditAction
{
    public const string Create = "create";
    public const string Update = "update";

    /// <summary>รวม soft-delete (IsDelete false -> true) ด้วย ไม่ใช่แค่ hard delete - R1.1
    /// soft-delete คือรูปแบบการลบปกติของระบบนี้ ดู AU-6</summary>
    public const string Delete = "delete";
}
