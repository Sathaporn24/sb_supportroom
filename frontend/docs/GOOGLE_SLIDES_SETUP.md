# Google Slides Setup

> สถานะ: **Prepared — Credentials Required** ยังไม่เคยทดสอบกับ Google Cloud Project จริง
> Entry point: `src/providers/slides/google-slides-provider.ts`

## ขั้นตอน

1. **สร้าง Google Cloud Project** ที่ [console.cloud.google.com](https://console.cloud.google.com)
2. **เปิด Google Slides API** — เมนู APIs & Services → Library → ค้นหา "Google Slides API" → Enable
3. **สร้าง Service Account** — APIs & Services → Credentials → Create Credentials →
   Service Account → ตั้งชื่อ (เช่น `supportroom-slides-reader`) ไม่ต้องให้สิทธิ์ Project Role ใด ๆ เพิ่ม
4. **สร้าง Key** — เปิด Service Account ที่สร้าง → Keys → Add Key → Create New Key →
   เลือก JSON → ดาวน์โหลดไฟล์ (เก็บให้ปลอดภัย ห้าม commit เข้า Git)
5. **แชร์ไฟล์ Google Slides ต้นฉบับให้ Service Account** — เปิด Google Slides ที่จะใช้สอน →
   Share → ใส่ Email ของ Service Account (รูปแบบ
   `xxx@xxx.iam.gserviceaccount.com` จากไฟล์ JSON) → สิทธิ์ **Viewer** ก็พอ
6. **หา Source URL และ presentationId** — เปิดไฟล์ที่ต้องการ คัดลอก URL แบบ
   `https://docs.google.com/presentation/d/<presentationId>/edit` (นี่คือ Source URL
   ที่ต้องกรอกในหน้า Admin ช่อง "Google Slides Source URL")
7. **เตรียม Embed URL** (ไม่บังคับ) — File → Share → Publish to web → เลือก
   Presentation → Publish จะได้ URL รูปแบบ `.../pub?...` **ใช้แทน Source URL ไม่ได้**
   (คนละ Identifier) แต่ใช้เป็น `slidesEmbedUrl` สำหรับแสดงผลได้ ถ้าไม่กรอก ระบบจะสร้าง
   Embed URL ให้อัตโนมัติจาก `presentationId` (ดู `buildEmbedUrlFromPresentationId`)
8. **ใส่ Environment Variables** ใน `.env.local`:
   ```env
   GOOGLE_SERVICE_ACCOUNT_PROJECT_ID=<project_id จากไฟล์ JSON>
   GOOGLE_SERVICE_ACCOUNT_EMAIL=<client_email จากไฟล์ JSON>
   GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY=<private_key จากไฟล์ JSON ใส่เป็นบรรทัดเดียว ใช้ \n แทน newline>
   ```
9. **สลับ Provider**: `SLIDES_PROVIDER=google`
10. **ทดสอบ**:
    - `GET /api/health` → เช็คว่า `providers.SLIDES_PROVIDER` เป็น `google`
    - `POST /api/slides/resolve` ด้วย Source URL จริง → ควรได้ `presentationId` กลับมา
      ไม่มี `warning`
    - `GET /api/slides/content?presentationId=xxx` → ควรได้ `slides[]` พร้อม
      `speakerNotes` จริงจากไฟล์
11. **Troubleshooting**:
    - **403 Forbidden**: Service Account ยังไม่ได้ถูกแชร์ไฟล์ หรือแชร์แต่ยังไม่ได้กด Save
    - **404 Not Found**: `presentationId` ผิด หรือ Parse URL ผิด (ตรวจว่าเป็น URL รูปแบบ
      `/presentation/d/<id>/edit` ไม่ใช่ Published URL แบบ `/presentation/d/e/<id>/pub`)
    - **Speaker Notes ว่างเปล่า**: บาง Slide อาจไม่มี Notes เลย — Provider คืนค่า `""`
      ไม่ throw error ตรวจสอบใน Google Slides ว่าใส่ Notes ไว้ครบทุก Slide ที่ต้องการสอน
    - **Private Key Error / Invalid PEM**: มักเกิดจากลืมแปลง newline ให้เป็น `\n`
      ในไฟล์ `.env` — โค้ด (`getGoogleServiceAccountEnv`) แปลง `\\n` → `\n` ให้อัตโนมัติ
      แล้ว แต่ต้องแน่ใจว่าไม่มีการ escape ซ้ำสองชั้นจาก Shell/Docker

## ข้อจำกัดสำคัญ

Published URL (`/pub`) และ Source (edit) URL ใช้ Identifier คนละแบบ — Published ID
(`2PACX-...`) เรียก Slides API ไม่ได้ ต้องมี Source URL เสมอเพื่ออ่าน Speaker Notes
ระบบจึงเก็บทั้งสอง URL แยกกันใน `LessonConfig` (`slidesSourceUrl` /
`slidesEmbedUrl`) — ถ้ามีแค่ Published URL ระบบจะ Fallback เป็น "แสดงผลได้อย่างเดียว"
(`isEmbedOnly: true`) และแจ้งเตือนใน Admin UI

## Google Slides ไม่ควรใช้กับเนื้อหาลับ

ถ้า Embed URL เป็น Published/Public การเข้าถึงไม่ได้ผูกกับ Auth ใด ๆ — อย่าใช้กับเนื้อหา
Sensitive ตามที่ระบุใน [SUPABASE_SETUP_AND_SCHEMA.md](./SUPABASE_SETUP_AND_SCHEMA.md) หัวข้อ Security
