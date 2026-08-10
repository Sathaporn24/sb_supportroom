# Development Checklist

## ก่อนแก้

- ระบุว่า change อยู่ frontend, backend layer ใด หรือ wire contract
- อ่าน interface/DTO/ViewModel/type ที่เกี่ยวข้องก่อนสร้าง contract ใหม่
- ถ้าแตะ Tutor reducer ให้อ่าน `STATE_MACHINE.md` และ reducer tests
- ถ้าแตะ provider ให้ตรวจ provider selection และ `.env.example`

## ก่อนส่งงาน

### Frontend

- [ ] `npm run lint`
- [ ] `npm run typecheck`
- [ ] `npm run test`
- [ ] `npm run build`

### Backend

- [ ] `dotnet build SupportRoom.slnx`
- [ ] unit tests ผ่านโดยไม่พึ่ง network
- [ ] integration/provider tests ที่ต้องใช้ credentials ถูกแยกและรายงานชัดเจน
- [ ] ไม่มี EF/Npgsql assembly conflict warning

### Contract และ Security

- [ ] TypeScript types ตรงกับ DTO/ViewModel และ JSON camelCase
- [ ] error ใช้ error envelope กลาง
- [ ] validation คืน 4xx ไม่ใช่ accidental 500
- [ ] ไม่ log secret, transcript, prompt, speaker notes หรือ answer เต็ม
- [ ] upload มี size/type/path validation
- [ ] งาน admin/cost-sensitive พิจารณา auth และ rate limiting

### Data และเอกสาร

- [ ] Schema change มี EF migration ใหม่และอัปเดต ER diagram
- [ ] Vector/storage lifecycle สอดคล้องกับ database lifecycle
- [ ] Background job รับมือ restart/failure ตามระดับความเสี่ยง
- [ ] เอกสาร architecture/API/state/setup ที่เกี่ยวข้องอัปเดตตามโค้ดจริง
