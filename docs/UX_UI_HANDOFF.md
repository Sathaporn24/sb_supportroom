# UX/UI HANDOFF — Screen, Role and State Inventory

> เป้าหมายคือให้ UX/UI ออกแบบตาม capability ที่มีจริงทั้งหมด ไม่ใช่ยึด styling ปัจจุบัน
> หลายหน้าหลังบ้านยังใช้ plain HTML โดยตั้งใจรอ Figma/design system

## เอกสารที่ใช้ส่งมอบ

1. [`UX_UI_WIREFRAME_SPEC.md`](./UX_UI_WIREFRAME_SPEC.md) — wireframe, screen specification, data/state, responsive และ acceptance criteria
2. [`UX_UI_WORKFLOWS.md`](./UX_UI_WORKFLOWS.md) — workflow ข้ามหน้า, permission, reconnect/error และ future provider/knowledge flow
3. ไฟล์นี้ — inventory/checklist ฉบับย่อสำหรับติดตาม scope

> หากส่งให้ UX/UI ให้ส่งทั้งสามไฟล์ ไม่ควรส่ง inventory นี้เพียงไฟล์เดียว

## Personas และสิทธิ์

| Persona | Scope | งานหลัก |
|---|---|---|
| Learner / recipient | ลิงก์และ session ของ browser ตนเอง | join, เรียน, ถามเสียง/ถามพิมพ์ผ่าน Ask-AI drawer, recap |
| CS | บริษัทตนเอง | บทเรียน, เอกสาร, ลิงก์, ติดตาม session, review คำถาม |
| Company admin | บริษัทตนเอง | ทุกอย่างของ CS + จัดการผู้ใช้บริษัทตนเอง |
| Owner | ทุกบริษัท | ทุกอย่าง + switch/company management + system provider settings |

## Route inventory ปัจจุบัน

| Route | ผู้ใช้ | เป้าหมาย | State ที่ต้องออกแบบ |
|---|---|---|---|
| `/admin/login` | ทุก back office | เข้าสู่ระบบ | default, invalid credentials, inactive account/company, submitting |
| `/admin/change-password` | signed-in | เปลี่ยน/บังคับเปลี่ยนรหัส | forced/voluntary, mismatch, wrong current, policy error, success |
| `/admin` | CS/Admin/Owner | dashboard + ลิงก์ล่าสุด | no company, loading, empty, expired/active, API error, destructive reset |
| `/admin/lessons` | CS/Admin/Owner | รายการบทเรียน | loading, empty, active/inactive, error |
| `/admin/lessons/[slug]` | CS/Admin/Owner | แก้ Google/PDF + timing + attachments | sync/upload/index status, invalid URL/PDF, unsaved, save success/error, empty slides |
| `/admin/documents` | CS/Admin/Owner | global knowledge documents | drag/upload, pending/indexed/failed, unsupported/oversize, delete confirm/error |
| `/admin/links/new` | CS/Admin/Owner | เลือกบทเรียนและสร้างลิงก์ | search/no result, inactive lesson, form validation, success/copy |
| `/admin/links/[token]` | CS/Admin/Owner | รายละเอียดลิงก์ + ผู้เข้าเรียน | empty, active/expired, in progress/stalled/ended, long list |
| `/admin/learning-sessions/[id]` | CS/Admin/Owner | live summary/question feed/answer review | live/reconnecting, no questions, not_found, reviewed/unreviewed, save error |
| `/admin/users` | Admin/Owner | เพิ่ม/ปิด/เปลี่ยน role | no company, no permission, empty, last-admin guard, duplicate email, busy/error |
| `/join/[token]` | Learner | prejoin, name, camera/mic | loading, permission ask/denied/no device, invalid/expired, joining error |
| `/room/[token]` | Learner | tutor room | preparing, ready, narrating, wait, recording, processing, answering, paused, mic error, TTS degrade, reconnect, fatal |
| `/session-ended/[token]` | Learner | recap + learn again | loading, no questions, answered/not-found/out-of-scope, restart error |
| `/link-expired` | Learner | terminal invalid/expired state | contact/help CTA (ยังไม่กำหนดช่องทาง) |

## Screens ที่ควรเพิ่มใน design แต่ยังไม่มี implementation

1. Owner: company management (list/create/rename/activate/deactivate + impact confirmation)
2. Owner: provider/API key settings ตาม [`PROVIDER_SETTINGS_SPEC.md`](./PROVIDER_SETTINGS_SPEC.md)
3. Owner/Ops: configuration change history + rollback
4. Admin: password reset/invite flow ถ้า D-09 รับเข้า release
5. Knowledge: health/coverage dashboard และ review queue ตาม [`KNOWLEDGE_ROADMAP.md`](./KNOWLEDGE_ROADMAP.md)
6. Learner: privacy/consent ก่อน microphone/external AI ตาม D-02/D-06
7. Generic: 403, 404, maintenance/provider outage และ offline/retry states

## Navigation requirements

- ทุกหน้า back office ต้องแสดงบริษัทที่กำลังดูชัด และ URL ต้องคง `?company=` ระหว่าง navigation
- Owner ที่ยังไม่เลือกบริษัทต้องเห็น company-picker state ไม่ใช่ empty data ที่ตีความว่าไม่มีข้อมูล
- Nav ต้องซ่อน/disable ตาม role แต่ server-side authorization ยังเป็นด่านจริง
- ปุ่ม reset/reindex/provider activate ต้องแยกจากงานประจำและมี confirm + impact + audit
- Mobile: learner room สำคัญสูงสุด; back office รองรับ tablet/desktop อย่างน้อยและกำหนด
  behavior ของ table/filter/drawer ให้ชัด

## Room interaction contract ที่ UX ห้ามทำให้เปลี่ยนโดยไม่คุย engineering

- Push-to-Talk เป็น press-and-hold ไม่ใช่ VAD/open mic
- ผู้เรียน interrupt narration เพื่อถามได้ แล้วระบบกลับจุดเดิม
- AI อาจย้อนแสดง related slide ชั่วคราวระหว่างตอบ
- TTS พังแล้วบทเรียนเดินต่อแบบเงียบ เป็น degradation ที่ต้องสื่อสารโดยไม่ทำ session จบ
- กล้องเป็น local preview ไม่ได้ส่งให้ agent; UI ต้องไม่สื่อว่ามี video call กับมนุษย์จริง
- Voice question และ typed question ต่างกันแค่ source แต่เป็น SessionQuestion เดียวกัน แสดงรวมใน drawer เดียวกัน (ไม่มีฟีเจอร์แชตแยกต่างหาก)

## Deliverables ที่ขอจาก UX/UI

- Sitemap + role matrix + user flows
- Design tokens/components + responsive rules
- Wireframe และ hi-fi ครบ routes/states ในตาราง
- Prototype 4 critical flows: first login, create lesson/link, new/returning learner, review incorrect answer
- Content spec ภาษาไทย: labels, empty/error/help, privacy/consent, destructive confirmations
- Accessibility: keyboard, focus, screen reader labels, contrast, reduced motion, PTT alternative
- Handoff annotations: API data/state dependency และ acceptance criteria ต่อ screen
