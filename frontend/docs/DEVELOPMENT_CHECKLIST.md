# Development Checklist

## ก่อนแก้โค้ดใด ๆ ในส่วน AI/Backend/Data

1. อ่าน [SYSTEM_ARCHITECTURE.md](./SYSTEM_ARCHITECTURE.md) และ [SYSTEM_LOGIC.md](./SYSTEM_LOGIC.md)
2. เช็คว่า Interface ที่ต้องการมีอยู่แล้วหรือยังใน `src/providers/*/types.ts` หรือ
   `src/providers/data/repository-types.ts` — ถ้ามีแล้วให้ปรับ Implementation ไม่ใช่
   สร้าง Interface ซ้ำ
3. เช็ค `src/config/env.ts` ว่ามี Env Reader ของ Service ที่ต้องการหรือยัง

## ก่อน Commit

- [ ] `npm run lint` ผ่าน
- [ ] `npm run typecheck` ผ่าน
- [ ] `npm run test` ผ่าน
- [ ] `npm run build` ผ่าน
- [ ] ถ้าแก้ Route Handler → อัปเดต [API_CONTRACT.md](./API_CONTRACT.md)
- [ ] ถ้าแก้ Tutor State Machine → อัปเดต [STATE_MACHINE.md](./STATE_MACHINE.md) และ
      Diagram ใน [SEQUENCE_DIAGRAMS.md](./SEQUENCE_DIAGRAMS.md) ให้ตรงกับโค้ดจริง
- [ ] ถ้าแก้ Schema → เพิ่ม Migration ใหม่ใน `supabase/migrations/` (ห้ามแก้ Migration
      เดิมที่ Apply ไปแล้ว) และอัปเดต [ER_DIAGRAM.md](./ER_DIAGRAM.md)
- [ ] ไม่มี Secret ใหม่หลุดเข้า Client Bundle (ไฟล์ที่แตะ Credential ต้องมี
      `import "server-only"`)
- [ ] ไม่มี `console.log` เนื้อหา Transcript/Speaker Notes/Answer เต็ม ๆ

## Definition of Done ต่อ Feature เล็ก

- Mock Mode ยังรันได้โดยไม่มี `.env.local`
- ถ้าเพิ่ม Provider ใหม่ (Real): มี Mock คู่กันเสมอ และ Factory Default เป็น Mock
- ถ้าเพิ่ม Route Handler ใหม่: มี Zod Validation ที่ Input และคืน `ApiErrorResponse`
  รูปแบบเดียวกันเมื่อ Error
- เอกสารที่เกี่ยวข้องอัปเดตแล้ว (ห้ามปล่อยให้เอกสารพูดเกินจริงกว่าที่ Implement)
