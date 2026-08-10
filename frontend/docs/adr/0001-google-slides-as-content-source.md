# ADR 0001: Google Slides as the Content Source

## Status

Amended (2026-08-10)

Google Slides ยังเป็นแหล่งเนื้อหาที่รองรับ แต่ไม่ใช่แหล่งเดียวอีกต่อไป ระบบปัจจุบันเลือก
`contentSourceType=google_slides|pdf` ต่อบทเรียน และ backend .NET เป็นผู้ resolve content
ทั้งสองแบบ เนื้อหาจริงยังไม่ถูก snapshot ลง PostgreSQL

## Context

เฟสแรกของ `sb_supportroom` ใช้ Lesson Editor แบบ Custom (Step/Segment/Checkpoint
เขียนเอง) ซึ่ง CS ต้องเรียนรู้ UI เฉพาะทางและระบบต้องดูแล Editor เอง Product ตัดสินใจ
เปลี่ยนให้ CS ใช้เครื่องมือที่คุ้นเคยอยู่แล้ว (Google Slides) แทน

## Decision

ใช้ Google Slides เป็นแหล่งเนื้อหาการสอนหลัก โดย:

- 1 Slide = 1 ช่วงการสอน
- Speaker Notes ของแต่ละ Slide = บทพูดของ AI โดยตรง (Plain Text ไม่มี Syntax พิเศษ)
- ระบบไม่ Copy/Snapshot เนื้อหา — อ่านสดทุกครั้งที่เข้าห้อง (`GET /api/lessons/[slug]`)
- Database เก็บเฉพาะ Metadata (URL, ค่าจังหวะเวลา,
  `videoDurationMs` ต่อ Slide) ไม่เก็บเนื้อหาสไลด์เอง

## Consequences

**ข้อดี**: CS ใช้เครื่องมือที่คุ้นเคย, แก้เนื้อหาได้ทันทีโดยไม่ต้อง Deploy, รองรับ
รูปภาพ/วิดีโอ/Layout ซับซ้อนได้ฟรีจาก Google Slides เอง

**ข้อเสีย/ความเสี่ยงที่ยอมรับ**:

- ควบคุม Slide ภายใน iframe ข้าม Origin ได้จำกัด (ใช้ URL Fragment +
  Force Reload เป็น Workaround — ดู [SYSTEM_ARCHITECTURE.md](../SYSTEM_ARCHITECTURE.md))
- อ่าน Event "วิดีโอเล่นจบ" จาก iframe ไม่ได้ ต้องใช้ `videoDurationMs` ที่ Admin
  กำหนดเองแทน (ความแม่นยำขึ้นกับ Admin กรอกถูกหรือไม่)
- ถ้า Slides ถูกแก้ระหว่างมี Session ที่กำลังสอนอยู่ เนื้อหาที่เห็นจะเปลี่ยนตามทันที
  (ไม่มี Snapshot ป้องกัน) — ยอมรับความเสี่ยงนี้เพื่อแลกกับความเรียบง่ายของสถาปัตยกรรม
- Published Google Slides ไม่เหมาะกับเนื้อหาที่ต้องการความลับ (ดู
  [GOOGLE_SLIDES_SETUP.md](../GOOGLE_SLIDES_SETUP.md))
