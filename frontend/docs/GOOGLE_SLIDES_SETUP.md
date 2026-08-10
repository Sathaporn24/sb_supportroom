# Google Slides Setup

Implementation อยู่ที่ `backend/src/SupportRoom.Providers.Slides/GoogleSlidesProvider.cs`

## Setup

1. เปิด Google Slides API ใน Google Cloud project
2. สร้าง service account และดาวน์โหลด credential
3. แชร์ presentation ให้ service account email เป็น Viewer
4. ตั้งค่าฝั่ง backend:

```env
SLIDES_PROVIDER=google
GOOGLE_SERVICE_ACCOUNT_PROJECT_ID=...
GOOGLE_SERVICE_ACCOUNT_EMAIL=...
GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n"
```

Private key ใน `.env` ใช้ literal `\n`; backend แปลงกลับเป็น newline

## Behavior

- `POST /api/slides/resolve` derive presentation ID และ embed URL
- `GET /api/slides/content` อ่าน slide order/speaker notes สด
- Save lesson พยายาม resolve ID และ best-effort index notes ลง Pinecone
- การ resolve/index ล้มเหลวระหว่าง save ถูก log แต่ save metadata อาจยังสำเร็จ
- ห้องสอนอ่าน content สด จึงเห็นการแก้ Google Slides โดยไม่ snapshot ลง PostgreSQL

## Verification

- URL แบบ edit/present/embed resolve ถูกต้อง
- service account ที่ไม่มีสิทธิ์คืน upstream error
- notes ภาษาไทยและ slide object IDs ตรงกับ deck
- slide ที่ไม่มี notes ถูกจัดการตาม content policy
- related slide จาก AI ต้องเป็น object ID ที่มีจริง
