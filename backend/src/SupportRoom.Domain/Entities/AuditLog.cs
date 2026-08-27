namespace SupportRoom.Domain.Entities;

/// <summary>
/// บันทึกดิบเรียงตามเวลาว่า AdminUser คนไหนทำอะไรกับแถวไหนเมื่อไหร่ (R1/R2) - แก้ปัญหา P1
/// ที่คอลัมน์ CreateBy/UpdateBy/DeleteBy เก็บได้แค่ "คนล่าสุด" ตารางนี้ไม่ได้มาแทนคอลัมน์พวกนั้น
/// มันอยู่ข้างๆ กัน
///
/// ⛔ จงใจ NOT implement IEntityMaster (มติ Q-A1) - append-only: ไม่มี IsDelete/UpdateBy/DeleteBy
///    ให้แก้หรือลบ ถ้าวันหนึ่งมีโค้ดที่ Update หรือ Delete แถวในตารางนี้ นั่นคือบั๊ก ไม่ใช่ฟีเจอร์
///
/// ⛔ จงใจ NOT implement ICompanyScoped และ **ไม่มี HasQueryFilter** (มติ OQ-2) ด้วยสองเหตุผล
///    ที่เป็นอิสระจากกัน: (1) CompanyId เป็น null ได้ ซึ่ง ICompanyScoped ไม่รองรับ
///    (2) filter `CompanyId == context` จะทำให้แถวระดับระบบ (null) หายไปจากทุกคนตลอดกาล
///    ผลที่ตามมาซึ่งต้องรู้: ตารางนี้ไม่มีตาข่ายนิรภัยเหมือน 14 ตารางที่มี filter - **ทางอ่านใดๆ
///    ในอนาคตต้อง filter CompanyId ด้วยตัวเอง** รูปแบบเดียวกับ BackgroundJob (design.md
///    ของ knowledge-base, SEC-2) เป๊ะ
///
/// ⛔ ไม่เก็บค่าก่อน/หลัง (R2 - เจ้าของโปรเจกต์ตัดออกแล้ว 2026-08-27) และไม่มี MetadataJson
///    (มติ Q-A2) - ตารางนี้ตอบว่า "ใครแก้แถวนี้ตอนไหน" ไม่ตอบว่า "เปลี่ยนจากอะไรเป็นอะไร"
///    ถ้าต้องกู้ค่าเดิม ต้องพึ่งฐานข้อมูล backup (C5)
/// </summary>
public sealed class AuditLog
{
    /// <summary>IdGenerator.GenerateId("audit")</summary>
    public required string Id { get; init; }

    /// <summary>
    /// บริษัทของ **ข้อมูลที่ถูกกระทำ** ไม่ใช่ของคนที่ลงมือ (R3.1) - owner ที่ CompanyId = null
    /// แก้ข้อมูลของบริษัท A แถวนี้ต้องเป็น A ไม่ใช่ null ไม่งั้นบริษัทเจ้าของข้อมูลจะมองไม่เห็น
    /// การกระทำที่เกิดกับข้อมูลตัวเอง · null = ระดับระบบ ดูกติกาการหาค่าที่ AU-11
    /// </summary>
    public string? CompanyId { get; init; }

    /// <summary>
    /// AdminUser.Id ของคนที่ลงมือ - **ไม่มีวันเป็น null** (มติ OQ-3: ไม่มีคน = ไม่มีแถว)
    /// ไม่ทำ FK จริงตาม convention ของโปรเจกต์ (ทุกความสัมพันธ์ข้าม entity เป็น logical string id)
    /// และเพราะ AdminUser ถูก deactivate ไม่ใช่ลบ (AdminUser.cs:47-49) id จึง resolve ได้เสมอ
    /// </summary>
    public required string ActorUserId { get; init; }

    /// <summary>
    /// ค่าจาก AuditAction - รอบนี้มี create | update | delete (R5: เพิ่มค่าใหม่ได้ทีหลัง
    /// โดยไม่ต้อง migration) · เป็น string ไม่ใช่ C# enum ตาม convention ของโปรเจกต์
    /// </summary>
    public required string Action { get; init; }

    /// <summary>ชื่อคลาสของ entity ที่ถูกกระทำ เช่น "LessonConfig" - มาจาก
    /// entry.Metadata.ClrType.Name ไม่ใช่ชื่อตารางที่พิมพ์มือ (AU-12)</summary>
    public required string EntityName { get; init; }

    /// <summary>primary key ของแถวที่ถูกกระทำ - ทุก PK ในโปรเจกต์นี้เป็น string เดี่ยว
    /// ที่ service สร้างเองก่อน SaveChanges (IdGenerator) ดู AU-10 ว่าทำไมข้อเท็จจริงนี้สำคัญ</summary>
    public required string EntityId { get; init; }

    /// <summary>UTC เสมอ · หนึ่ง SaveChanges ที่เกิดหลายแถวต้องใช้ค่าเดียวกันทุกแถว (AU-9)
    /// เพื่อให้ "การกระทำครั้งเดียว" ยังจับกลุ่มกันได้ตอน SELECT แม้ไม่มีคอลัมน์ correlation id</summary>
    public required DateTime OccurredAt { get; init; }
}
