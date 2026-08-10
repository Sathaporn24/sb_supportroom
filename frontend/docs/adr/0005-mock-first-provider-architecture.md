# ADR 0005: Mock-first Provider Architecture

## Status

Superseded (2026-08-10)

Backend .NET ปัจจุบันถอด Mock providers ออกแล้ว ทุก provider category ต้องเลือก real
implementation อย่างชัดเจน และ PostgreSQL เป็น dependency บังคับ การตัดสินใจด้านล่าง
อธิบาย architecture เดิมเท่านั้น

## Context

โปรเจกต์ต้องเตรียมเชื่อม 4 บริการภายนอกที่ยังไม่มี Credential ให้ในงานรอบนี้ (Google
Slides, Gemini, Hugging Face, Supabase) แต่ต้อง Demo และพัฒนาต่อได้ทันทีโดยไม่ติดขัด

## Decision

ทุก External Integration ต้องมี:

1. Interface กลาง (ไม่ผูกกับ SDK ผู้ให้บริการ)
2. Mock Implementation ที่ทำงานได้จริงแบบ End-to-end โดยไม่ต้องมี Credential
3. Real Implementation ("Prepared") ที่เขียนตาม Contract จริงของบริการนั้น แต่ยังไม่ผ่าน
   การทดสอบกับบริการจริง
4. Factory จุดเดียวต่อ Interface ที่เลือก Mock/Real จาก Environment Variable โดย
   Default เป็น Mock เสมอ

## Consequences

**ข้อดี**:

- Onboarding เร็ว — Dev ใหม่ Clone แล้ว `npm run dev` ได้ทันทีไม่ต้องขอ Credential ใคร
- Demo ได้ทุกเมื่อโดยไม่กลัว Rate Limit/Cost ของบริการภายนอก
- Mock บังคับให้ Interface ออกแบบสะอาดตั้งแต่แรก (ถ้า Mock ทำไม่ได้ แปลว่า Interface
  อาจผูกกับ Implementation Detail ของ Provider ใดตัวหนึ่งมากเกินไป)
- Unit Test ส่วนใหญ่ทดสอบผ่าน Mock ได้โดยไม่ต้อง Network Call จริง (เร็วและเสถียร)

**ข้อเสีย/ความเสี่ยงที่ยอมรับ**:

- Mock กับ Real อาจมีพฤติกรรมเพี้ยนกันในรายละเอียด (เช่น `MockVoiceQuestionProvider`
  ใช้ Transcript ตัวอย่างคงที่ ไม่ได้ถอดเสียงจริง) — ต้องทดสอบกับ Real Provider จริงก่อน
  Production เสมอ ห้ามเชื่อพฤติกรรม Mock 100%
- ต้องรักษาวินัยเพิ่ม Mock คู่กับ Real ทุกครั้งที่เพิ่ม Provider ใหม่ ไม่งั้นจะเสียหลักการนี้
  ไปเรื่อย ๆ (ดู [DEVELOPMENT_CHECKLIST.md](../DEVELOPMENT_CHECKLIST.md))
