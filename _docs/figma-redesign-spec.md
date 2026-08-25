# Figma Redesign → Implementation Spec (สำหรับ Codex)

> เอกสารนี้สรุปผลจากการไล่เทียบ Figma redesign ไฟล์ `sb_support-AI` (20 เฟรมใน Section 5)
> กับโค้ดจริงในโปรเจกต์นี้ ทีละเฟรม ทีละหน้า — เขียนขึ้นเพราะ Codex อ่าน Figma ตรงๆ ไม่ได้
> เอกสารนี้ตั้งใจให้ **ครบในตัวเอง** ไม่ต้องเปิด Figma ประกอบ
>
> Audit เต็มอยู่ใน session memory `project-figma-redesign-audit` (ฝั่ง Claude) — เอกสารนี้คือ
> "ผลลัพธ์ที่พร้อมใช้งาน" สกัดมาจากตรงนั้น ไม่ใช่ log การไล่เช็ค

## สรุปภาพรวมก่อนอ่านรายละเอียด

- **บทเรียน+redesign ส่วนใหญ่คือ re-skin ของที่มีอยู่แล้วและทำงานได้จริง** — ฟีเจอร์ส่วนใหญ่ไม่ต้องสร้างใหม่ แค่จัดวาง UI ใหม่
- **มีการยุบรวมหน้าจริง 2 จุด** (รายละเอียดในส่วนที่ 1-2) — เปลี่ยน sidebar จาก 10 รายการเหลือ 6 รายการ
- **มีฟีเจอร์ใหม่จริงแค่ 1 จุด** (checkbox โหมดทดลองในฟอร์มเพิ่มบทเรียน — ส่วนที่ 3) และ **ยังห้ามลงมือทำ** จนกว่าจะผ่าน business-analyst นิยามพฤติกรรมให้ชัดก่อน
- **มีจุดที่ Figma เป็น mock/ของทีมดีไซน์ใส่มาเฉยๆ ไม่ใช่ requirement จริง** — ระบุไว้ชัดในหัวข้อ "ห้ามทำ" ท้ายเอกสาร ห้ามหยิบไป spec ต่อไม่ว่าจะตีความแบบไหนก็ตาม

---

## ส่วนที่ 1: หน้า "บทเรียน" — ยุบรวม 3 หน้าเดิมเป็นหน้าเดียว 2 แท็บ

### สถานะปัจจุบัน (3 หน้าแยกกัน, 3 sidebar item)

| Sidebar เดิม | Route | ไฟล์ |
|---|---|---|
| "ลิงก์การเรียน" | `/admin` (root) | [admin/page.tsx](frontend/src/app/admin/page.tsx) + [TrainingLinksTable.tsx](frontend/src/components/admin/TrainingLinksTable.tsx) |
| "บทเรียน" | `/admin/lessons` | [admin/lessons/page.tsx](frontend/src/app/admin/lessons/page.tsx) |
| "หมวดความรู้" | `/admin/categories` | [admin/categories/page.tsx](frontend/src/app/admin/categories/page.tsx) |

- `/admin/lessons` วันนี้เป็น **ตารางแบน ไม่มีการจัดกลุ่มตามหมวดเลย**
- `/admin/categories` เป็นหน้า tree แยกต่างหาก มี 3 dialog action แยกกัน (สร้างหมวดหลัก, สร้างหมวดย่อย, แก้ไข) ผ่าน `CategoryFormDialog`
- Schema เบื้องหลัง: `KnowledgeCategory` — self-referencing 2 ชั้น (`Level 1` = หมวดหลัก, `Level 2` = หมวดย่อย), scope ต่อบริษัท (ดู `_docs/module/knowledge-base/design.md` DM-1) — **schema นี้รองรับ tree ที่ redesign ต้องการอยู่แล้ว ไม่ต้อง migration**

### ดีไซน์ใหม่ (1 หน้า, 2 แท็บ)

**Title:** "บทเรียน"
**Subtitle:** "จัดการเนื้อหาบทเรียน และสร้างลิงก์กับบทเรียน"
**Tabs:** `จัดการบทเรียน` (default) · `ประวัติการสร้างลิงก์`

#### แท็บ "จัดการบทเรียน" — แทนที่ `/admin/lessons` + `/admin/categories`

โครงสร้างที่เห็นใน Figma:

- ปุ่มขวาบน: **"+ สร้างหมวดหมู่"** (สีส้ม) — เปิด wizard สร้างหมวด (ดูด้านล่าง)
- รายการหมวดหลัก (Level 1) แต่ละอันมี:
  - หัวข้อชื่อหมวดหลัก + ปุ่ม "+ เพิ่มบทเรียน" + ไอคอนแก้ไข/ลบ
  - accordion ของหมวดย่อย (Level 2) แต่ละอันขยาย/ยุบได้ — ตัวอย่างในภาพ: "ระบบ Login" (ขยายอยู่), "ข้อมูลบุคคล", "วิชาการ", "กิจการนักเรียน", "บริหารทั่วไป", "บัญชี/การเงิน", "ร้านค้า/สหกรณ์", "รายงาน" (ยุบอยู่)
  - ภายในหมวดย่อยที่ขยาย: ตาราง **ชื่อบทเรียน | สถานะ (toggle) | จัดการ** (ปุ่ม "สร้างลิงก์การสอน" สีส้ม + ไอคอนแก้ไข)
- ในภาพตัวอย่างมีหมวดหลัก 2 อัน ("School Bright Website", "School Bright Mobile") — **ชื่อทั้งสองนี้เป็น mock ที่ทีมดีไซน์ใส่เป็นตัวอย่างเฉยๆ ไม่ใช่ concept ใหม่ระดับ "Product"** ยืนยันแล้วกับ user — โครงสร้างจริงคือแค่หมวดหลัก (Level 1) 2 แถวตามที่ `KnowledgeCategory` รองรับอยู่แล้ว ไม่มีชั้นใหม่เหนือหมวดหลัก

**Wizard สร้างหมวด** (แทนที่ 3-dialog เดิมของ `CategoryFormDialog`) — เปิดเป็น modal ทับหน้านี้เลย ไม่ไปหน้าแยก:
- Modal "สร้างหมวดหมู่หลัก": step indicator 2 ขั้น (① สร้างหมวดหมู่หลัก ② สร้างหมวดหมู่ย่อย), field เดียว "ชื่อหมวดหมู่" + ปุ่ม "ยกเลิก"/"ถัดไป"
- Modal "แก้ไขหมวดหมู่ย่อย": step indicator เดียวกัน (① แก้ไขหมวดหมู่หลัก ② แก้ไขหมวดหมู่ย่อย) แต่หน้าจอที่เห็นโชว์ **2 ช่อง "ชื่อหมวดหมู่ย่อย" พร้อมกัน** + ไอคอนถังขยะ + ลิงก์ "+ เพิ่มหมวดหมู่ย่อย" + ปุ่ม "ยกเลิก"/"ยืนยัน" — ตีความได้ว่าเป็นหน้าจอแก้ไขหมวดย่อยหลายอันพร้อมกันในครั้งเดียว (list ของ input ที่เพิ่ม/ลบได้) ไม่ใช่ step-by-step ทีละอัน — **หมายเหตุ: ผมเห็นจาก static screenshot เท่านั้น ไม่มี interactive prototype ให้ทดสอบจริง ถ้า Codex ต้องการความชัดเจนเรื่อง interaction flow (ทีละ step หรือ list เดียวจบ) ควรถามทีมดีไซน์ก่อนเขียนโค้ดจริง**

#### แท็บ "ประวัติการสร้างลิงก์" — แทนที่ `/admin` (root)

เนื้อหาตรงกับ `TrainingLinksTable.tsx` เกือบทั้งหมด (มีอยู่แล้ว ทำงานได้จริง แค่ย้ายมาอยู่ในแท็บนี้แทนที่จะเป็นหน้า root):
- Search box: Figma เขียน "ค้นหาบทเรียนหรือห้องงาน..." ของจริงคือ "ค้นหาบทเรียนหรือหน่วยงาน..." — ใช้ข้อความของจริง ไม่ต้องแก้
- ตาราง: วันที่สร้าง | หน่วยงาน (คอลัมน์นี้มีในโค้ดจริง แต่ไม่เห็นในภาพ Figma ตัวอย่าง — **ต้องคงไว้ ไม่ใช่ตัด**) | ภาพรวมการเรียน (ผู้เรียน N คน / กำลังเรียน N รอบ / จบแล้ว N รอบ — ของจริงใช้หน่วย "รอบ" ไม่ใช่ "ราย" ตามที่ Figma เขียน) | หมดอายุ | สถานะ (รวม badge "ใกล้หมดอายุ" ที่ Figma ไม่ได้โชว์แต่ของจริงมี — คงไว้) | การจัดการ (คัดลอกลิงก์ / ดูผู้เข้าเรียน)
- Pagination 10 แถว/หน้า — คงพฤติกรรมเดิม

### Task สำหรับ Codex

1. รวม 3 route (`/admin`, `/admin/lessons`, `/admin/categories`) เป็นหน้าเดียวพร้อม 2 แท็บ (ตัดสินใจ URL scheme เอง เช่น `/admin/lessons` เป็น base + query/sub-route สำหรับแท็บ — ไม่ใช่เรื่อง data model จึงไม่ต้องผ่าน system-analyst)
2. เปลี่ยน lessons list จากตารางแบนเป็น tree ตาม `KnowledgeCategory` (Level 1 → Level 2 → lesson) พร้อม accordion
3. ย้าย `CategoryFormDialog` ให้เปิดจากปุ่มในหน้านี้แทนหน้า `/admin/categories` เดิม รวม 3 action เดิมเป็น wizard 2 ขั้นตอนตามที่อธิบายข้างต้น (ยืนยัน interaction flow กับทีมดีไซน์ก่อนถ้าไม่ชัวร์)
4. ย้าย `TrainingLinksTable` เข้าแท็บ "ประวัติการสร้างลิงก์" คงทุก column/badge ที่มีอยู่จริงวันนี้ไว้ครบ
5. ปรับ `AdminSidebar.tsx` ให้เหลือ 1 รายการ "บทเรียน" แทนที่ 3 รายการเดิม

---

## ส่วนที่ 2: หน้า "ตั้งค่าบริษัท" — ยุบรวม 2 หน้าเดิมเป็นหน้าเดียว 2 แท็บ

### สถานะปัจจุบัน

| Sidebar เดิม | Route | ไฟล์ | เข้าถึงได้ |
|---|---|---|---|
| "บริษัททั้งหมด" | `/admin/companies` | [admin/companies/page.tsx](frontend/src/app/admin/companies/page.tsx) | owner เท่านั้น |
| "ตั้งค่าบริษัท" | `/admin/settings` | [admin/settings/page.tsx](frontend/src/app/admin/settings/page.tsx) | ตาม `SETTINGS_SECTIONS` access |

### ดีไซน์ใหม่

**Title:** "ตั้งค่าบริษัท"
**Tabs:** `จัดการบริษัท` · `ตั้งค่าบริษัท`

#### แท็บ "จัดการบริษัท" — แทนที่ `/admin/companies`

ของจริงมีอยู่แล้ว (list + add + disable flow, disable จริงตัดสิทธิ์ login ของพนักงานบริษัทนั้นผ่าน `IAuthService.EnsureCompanyStillUsable`) แต่ **ตารางใน Figma ไม่ตรงกับของจริง ต้องแก้ตอน implement**:

| Figma (ผิด) | ของจริง (ใช้อันนี้) |
|---|---| 
| ลำดับ / รหัสบริษัท / **Email** / สถานะ / การทำงาน | รหัสบริษัท / **ชื่อบริษัท** / สถานะ / การจัดการ |

คอลัมน์ "Email" ใน Figma โชว์ค่าเป็นชื่อบริษัทภาษาไทย ("บริษัททดสอบ 1") อยู่แล้ว — แปลว่า designerตั้งใจให้เป็นชื่อบริษัท แค่ตั้ง label คอลัมน์ผิดเป็น "Email" **ให้ใช้ label "ชื่อบริษัท" แมปกับ field `company.name` ของจริง ไม่ต้องเพิ่ม field email ใดๆ**

ปุ่ม action "ปิดใช้งาน" ใน Figma ทำเป็น badge สีแดง ของจริงเป็น `Button variant="destructive"` — ใช้ของจริง

Slug/รหัสบริษัทของจริงรับเฉพาะ `[a-z0-9]+(-[a-z0-9]+)*` (ตัวเล็ก+ตัวเลข+ขีดกลาง) — ค่า mock ใน Figma "company_test01" (มี underscore) ผ่าน validation จริงไม่ได้ ห้ามอ้างอิงรูปแบบนี้

#### แท็บ "ตั้งค่าบริษัท" — แทนที่ `/admin/settings`

**ส่วน "จังหวะการสอน (ระดับบริษัท)" มีอยู่แล้วจริง 100% ไม่ต้องสร้างใหม่** — implement แล้วใน commit `301da2f` (22 ส.ค. 2569):
- Component: [LessonPacingSettingsSection.tsx](frontend/src/components/admin/settings/LessonPacingSettingsSection.tsx)
- Fields (ตรงกับ Figma ทุกตัว): `introWaitMs` "ระยะรอก่อนเริ่มสอน" (0-60000 ms), `breathPauseMs` "ช่วงหยุดหายใจระหว่างสไลด์" (0-10000 ms), `finalQuestionWaitMs` "ช่วงเปิดให้ถามคำถามสุดท้าย" (0-120000 ms)
- ปุ่ม "บันทึก"

**⚠️ Subtitle ใน Figma ผิด ห้ามใช้ตามนั้น**: ข้อความ mock ยังพูดถึงว่า "บทเรียนที่ตั้งค่าเฉพาะไว้แล้วจะไม่เปลี่ยน" (per-lesson override ยังอยู่) — แต่คอมมิต `301da2f` เดียวกันที่สร้าง feature นี้ **ตัด per-lesson override ออกไปแล้วทั้งหมด** ให้ใช้ copy จริงจากโค้ด:

> "ค่านี้มีผลกับทุกบทเรียนของบริษัทนี้ตั้งแต่การเข้าห้องเรียนครั้งถัดไปเท่านั้น ห้องที่กำลังเรียนอยู่ตอนนี้จะไม่เปลี่ยนกลางคัน"

Task ของส่วนนี้เหลือแค่: ย้าย component เดิมให้ไปอยู่ในแท็บใหม่ ไม่ต้องเขียน logic ใหม่

### Task สำหรับ Codex

1. รวม `/admin/companies` + `/admin/settings` เป็นหน้าเดียว 2 แท็บ
2. หน้า "จัดการบริษัท": ใช้ตารางของจริง (รหัสบริษัท/ชื่อบริษัท/สถานะ/การจัดการ) ไม่ใช่ตามที่ Figma เขียน
3. หน้า "ตั้งค่าบริษัท": ย้าย `LessonPacingSettingsSection` เข้ามาเฉยๆ ใช้ copy จริงจากโค้ด ไม่ใช่ copy จาก Figma
4. ปรับ `AdminSidebar.tsx` ให้เหลือ 1 รายการ "ตั้งค่าบริษัท" แทนที่ 2 รายการเดิม (คง gating เดิม: owner เท่านั้นเห็นแท็บ "จัดการบริษัท", ส่วนแท็บ "ตั้งค่าบริษัท" ตาม `SETTINGS_SECTIONS` access เหมือนเดิม)

---

## ส่วนที่ 3: ฟีเจอร์ใหม่จริง — checkbox โหมดทดลองในฟอร์มเพิ่มบทเรียน

**🚧 ห้าม implement ตอนนี้ — ต้องผ่าน business-analyst ก่อน**

หน้า [admin/lessons/new/page.tsx](frontend/src/app/admin/lessons/new/page.tsx) ตรงกับ Figma ทุก field ยกเว้นจุดเดียว:

Figma มี checkbox **"เปิดใช้งานทดลองฟังก่อนใช้จริง (เตรียมให้ทำการทดลองสอน)"** — ของจริงมีแค่ checkbox "เปิดใช้งานบทเรียนนี้ทันที (พร้อมให้สร้างลิงก์การสอน)" ผูกกับ `form.isActive` (toggle เปิด/ปิดใช้งานตรงๆ ไม่มี concept โหมดทดลอง)

ไม่มี field รองรับ "โหมดทดลอง" อยู่ใน `LessonConfigInput` เลยแม้แต่ตัวเดียว — ก่อนแตะโค้ดต้องตอบให้ได้ก่อนว่า:
- โหมดทดลองต่างจาก "ปิดใช้งาน" (`isActive=false`) ตรงไหน
- ครูเข้าห้องทดลองได้โดยไม่ต้องมีลิงก์จริงไหม
- ข้อมูล session ที่เกิดในโหมดทดลองนับเป็น record จริงไหม หรือถูกแยก/ลบทิ้ง
- ใครเปลี่ยนจากโหมดทดลอง → live ได้ (ต้องมี state transition ชัดเจน)

**ส่งต่อ business-analyst amend `_docs/module/knowledge-base/requirement.md` ก่อน แล้วค่อยกลับมาที่ system-analyst ถ้ากระทบ schema (เช่นต้องเพิ่ม status ใหม่ใน `LessonConfig`)**

---

## ส่วนที่ 4: แก้ copy หน้า "คลังเอกสาร" ให้ตรงกับมติ R8

หน้า [admin/documents/page.tsx](frontend/src/app/admin/documents/page.tsx) — Figma มี subtitle ประโยคท้ายว่า "เอกสารที่ผูกกับบทเรียนใดหนึ่งโดยเฉพาะให้อัปโหลดที่หน้าแก้ไขบทเรียนนั้นแทน" — **ประโยคนี้ขัดกับมติ R8 ที่ตัด embedded document uploader ออกจากหน้าแก้ไขบทเรียนไปแล้ว** (ยืนยันแล้วว่าหน้า lesson-edit ของจริง**ไม่มี**ส่วนอัปโหลดเอกสารเลย ตรงกับ R8)

**Task:** ตัดประโยคท้ายนี้ทิ้งจาก copy ที่จะ implement ทั้งหมด — ไม่ต้องรอ business-analyst เพราะเป็นแค่การลบข้อความที่ล้าสมัยให้ตรงกับพฤติกรรมจริงที่ตัดสินใจไปแล้ว (R8) ไม่ใช่การเปลี่ยนพฤติกรรม

---

## หน้าที่ตรวจแล้ว "ไม่มีอะไรเปลี่ยน" — re-skin เฉยๆ ใช้ของเดิมได้เลย

ตรวจแล้วทุกหน้าว่าไม่มี tab/merge ซ่อนอยู่ (เช็ค sidebar + tab bar ของแต่ละเฟรมโดยตรง ไม่ใช้การเดา):

| หน้า | ไฟล์ | หมายเหตุ |
|---|---|---|
| Login | [admin/login/page.tsx](frontend/src/app/admin/login/page.tsx) | ตรงกันทุกตัวอักษร |
| เปลี่ยนรหัสผ่านครั้งแรก | [admin/change-password/page.tsx](frontend/src/app/admin/change-password/page.tsx) | มีอยู่แล้ว (`AdminUser.MustChangePassword`, `AdminGuard.tsx`) |
| แก้บทเรียน | [admin/lessons/[slug]/page.tsx](frontend/src/app/admin/lessons/[slug]/page.tsx) | ตรงกัน ไม่มีส่วนอัปโหลดเอกสาร (สอดคล้อง R8) |
| Narration editor | (หน้าย่อยของแก้บทเรียน) | ตรงกันทุกตัวอักษร |
| qna-queue | [admin/qna-queue/page.tsx](frontend/src/app/admin/qna-queue/page.tsx) | Figma โชว์แค่ empty state ของจริงมี select-multiple + answer dialog ครบ |
| qna-conflicts | [admin/qna-conflicts/page.tsx](frontend/src/app/admin/qna-conflicts/page.tsx) | เหมือนกัน โชว์แค่ empty state ของจริงมี resolve-flag action ครบ |
| จัดการผู้ใช้ (users) | [admin/users/page.tsx](frontend/src/app/admin/users/page.tsx) | ตรงกัน ยกเว้นคอลัมน์ password (ดูหัวข้อ "ห้ามทำ") |
| Add-user dialog | `CreateUserDialog` ใน users/page.tsx | ตรงกัน must-change-password เป็น server-enforced อยู่แล้วไม่ใช่ checkbox |

---

## 🚫 ห้ามทำ — ยืนยันแล้วว่าเป็น mock ทั้งหมด อย่า spec ต่อไม่ว่าตีความแบบไหน

1. **คอลัมน์ "รหัสผ่าน" ในหน้า users** (โชว์ค่าดูเหมือน plaintext password) — เป็นไปไม่ได้ทางเทคนิค: `AdminUserViewModel.cs` เขียน comment ชัดว่าห้าม carry `PasswordHash` ออกมาเด็ดขาด ทุก field รหัสผ่านในระบบเป็น write-only (login/เปลี่ยนรหัส/สร้างบัญชี) ไม่เคยเป็น output — **user ยืนยันแล้วว่า mock ข้ามไปเลย**
2. **Role "ทั่วไป" ใน add-user dialog** — role จริงมีแค่ `owner`/`admin`/`cs` (เจ้าของ/แอดมิน/ทีม CS) — **user ยืนยันแล้วว่า mock ให้ถือเป็น role เดิมตัวใดตัวหนึ่ง ไม่ต้องเพิ่ม role ที่ 4**
3. **"Product" concept เหนือหมวดหมู่** (ชื่อ "School Bright"/"School Bright Mobile" ที่เห็นในหน้าบทเรียน) — **user ยืนยันแล้วว่าเป็นชื่อ mock เฉยๆ** ไม่ใช่ concept ใหม่ โครงสร้างจริงคือแค่หมวดหลัก (Level 1) ของ `KnowledgeCategory` ที่มีอยู่แล้ว
4. **ช่อง "Filter tasks...", dropdown "Status"/"Priority", และ pagination UI** ที่เห็นในหน้า companies list และ users list — เช็คทั้ง codebase แล้วไม่มี concept "priority" อยู่เลยสักที่ (ไม่มี ticket/kanban entity ในระบบ) และไม่มี search/pagination UI จริงอยู่เลยในทั้ง `frontend/src` — เป็น chrome ที่ติดมาจาก generic table template ที่ทีมดีไซน์ยังไม่ตัดออก **ไม่ต้องเอาไป spec เว้นแต่จะมีคนขอ search/filter จริงๆ แยกเป็นอีก request ต่างหาก**

---

## Sidebar เป้าหมาย (6 รายการ แทนที่ 10 รายการเดิม)

| # | รายการใหม่ | แทนที่รายการเดิม |
|---|---|---|
| 1 | บทเรียน | ลิงก์การเรียน + บทเรียน + หมวดความรู้ (ยุบรวม, ส่วนที่ 1) |
| 2 | คลังเอกสาร | (เดิม ไม่เปลี่ยน) |
| 3 | คำถามรอคำตอบ | (เดิม ไม่เปลี่ยน) |
| 4 | Q&A ขัดกับเอกสาร | (เดิม ไม่เปลี่ยน) |
| 5 | จัดการผู้ใช้งาน | ผู้ใช้งาน (label เปลี่ยน, ฟีเจอร์เดิม) |
| 6 | ตั้งค่าบริษัท | บริษัททั้งหมด + ตั้งค่าบริษัท (ยุบรวม, ส่วนที่ 2) |

"แดชบอร์ด" (placeholder Phase 2 ของจริง ยังไม่เคย build) ไม่ปรากฏในเฟรมที่ตรวจ — ไม่ทราบว่าถูกตัด concept ทิ้งจริงหรือแค่ไม่ได้อยู่ใน scope เฟรมที่มี เนื่องจากยังไม่มีของจริงให้เทียบอยู่แล้ว จึงไม่กระทบ implementation รอบนี้

## Reference เพิ่มเติม

- Role model: `owner`/`admin`/`cs`, กฎกันยกระดับสิทธิ์ตัวเอง (TD-014) — ดู `frontend/src/types/domain.ts:360`, `frontend/src/app/admin/users/page.tsx:43`
- Category schema: `_docs/module/knowledge-base/design.md` หัวข้อ DM-1 (`KnowledgeCategory`)
- คอมมิตอ้างอิง: `301da2f` feat(company-admin) ย้ายจังหวะการสอนเป็นค่ากลางระดับบริษัท
