# SupportRoom AI — Wireframe & UX Specification

> เวอร์ชันส่งต่อ UX/UI · ตรวจเทียบ implementation วันที่ 14 ส.ค. 2026  
> เอกสารนี้กำหนด **โครงหน้า ข้อมูล พฤติกรรม สถานะ และข้อจำกัด** ไม่ได้กำหนด visual style  
> Workflow ข้ามหน้าจออยู่ใน [`UX_UI_WORKFLOWS.md`](./UX_UI_WORKFLOWS.md)

## 1. วิธีอ่านและขอบเขต

ใช้ป้ายสถานะต่อไปนี้กับทุกหน้าจอ เพื่อไม่ให้สิ่งที่คิดไว้กับสิ่งที่ทำแล้วปนกัน

| ป้าย | ความหมาย | UX/UI ทำอะไรได้ |
|---|---|---|
| `AS-IS` | route, API และพฤติกรรมหลักมีในระบบแล้ว | redesign ได้ แต่ต้องคง behavior และข้อมูล |
| `DESIGN REQUIRED` | capability มีแล้ว แต่หน้าปัจจุบันยังไม่ครอบคลุม UX ที่ควรมี | ออกแบบ flow/state ให้ครบเพื่อส่ง engineering |
| `FUTURE CONCEPT` | ยังไม่ implement และ data model อาจเปลี่ยน | ทำ concept/exploration ได้ ห้ามระบุว่า approved |
| `DECISION NEEDED` | ต้องให้ Product/Security/Engineering ตัดสินใจก่อน | แสดงคำถามและทางเลือก ห้ามเดาคำตอบแทน |

สิ่งที่เคาะแล้ว:

- ผู้เรียนไม่มีบัญชี เข้าได้ด้วย public training link
- browser ใช้ `learnerKey` แยกผู้เรียนและกลับมาเรียนต่อ ชื่อไม่ใช่ identity
- หนึ่ง `TrainingLink` มีผู้เรียนหลายคน แต่ละคนมี `LearningSession` ของตัวเอง
- `owner` เห็นทุกบริษัท; `admin` และ `cs` สังกัดบริษัทเดียว
- ผู้เรียนไม่เห็นผลตรวจภายใน จุดที่ AI ตอบไม่ได้ หรือข้อมูล provider/document
- Push-to-Talk เป็นแบบกดค้าง ไม่ใช่ open mic/VAD
- กล้องเป็น local preview เท่านั้น ไม่ได้ส่งภาพให้ CS หรือ AI

สิ่งที่ยังไม่เคาะ:

- Provider/API key settings, shared key เทียบกับ BYOK และ secret storage
- Knowledge quality workflow รุ่นถัดไป
- forgot password, email invite, refresh token, SSO และ MFA
- privacy/consent wording, retention, data residency และช่องทางติดต่อเมื่อ link ใช้ไม่ได้

## 2. Persona และสิทธิ์

| Persona | Scope | งานหลัก |
|---|---|---|
| ผู้เรียน | link + session ของ browser ตนเอง | join, เรียน, ถามเสียง, chat, ดูสรุปของตน |
| CS | บริษัทตนเอง | บทเรียน, เอกสาร, ลิงก์, session, chat, review |
| Company admin | บริษัทตนเอง | ทุกอย่างของ CS + จัดการผู้ใช้บริษัทตนเอง |
| Owner | ทุกบริษัท | ทุกอย่าง + company management + system settings ในอนาคต |

> ระบบปัจจุบัน **ไม่รองรับ** ผู้ใช้ที่เลือกดูเพียงบางบริษัทหลายแห่ง เจ้าของระบบเห็นทุกบริษัท ส่วน admin/cs เห็นบริษัทเดียว

## 3. Sitemap

```text
Public learner
├── /join/[token]                  S01 Pre-join
├── /room/[token]                  S02 Voice classroom
├── /session-ended/[token]         S03 Learner recap
└── /link-expired                  S04 Invalid/expired link

Back office
├── /admin/login                   S05 Sign in
├── /admin/change-password         S06 Change password
└── /admin                         Authenticated shell
    ├── /admin                     S07 Link list / home
    ├── /admin/links/new           S08 Create link
    ├── /admin/links/[token]       S09 Link + learners
    ├── /admin/learning-sessions/[id] S10 Session review
    ├── /admin/lessons             S11 Lesson list
    ├── /admin/lessons/[slug]      S12 Lesson editor
    ├── /admin/documents           S13 Global documents
    └── /admin/users               S14 User management

Future / design exploration
├── Company management             F01 Owner · API บางส่วนมีแล้ว แต่ยังไม่มีหน้า
├── Provider settings              F02 Owner · proposed only
└── Knowledge operations           F03 Role/flow ยังต้องตัดสินใจ
```

## 4. App shell และ navigation

### Public shell

- ไม่มี back-office navigation, company switcher หรือปุ่ม login
- โลโก้/ชื่อระบบไม่ควรแย่งความสำคัญจากชื่อบทเรียนและ action หลัก
- มือถือเป็นลำดับแรก โดยเฉพาะหน้าที่เปิดจาก LINE
- ต้องมีข้อความไทยที่ใช้คำง่าย เหมาะกับผู้ใช้ที่ไม่คุ้นเทคโนโลยี

### Back-office shell

```text
┌──────────────────────────────────────────────────────────┐
│ Brand │ Company context / switcher │ User menu / sign out│
├──────────────┬───────────────────────────────────────────┤
│ Navigation   │ Page title · description · primary action│
│ - Links      │                                           │
│ - Lessons    │ Page content                              │
│ - Documents  │                                           │
│ - Users*     │                                           │
│ - Settings** │                                           │
└──────────────┴───────────────────────────────────────────┘
* owner/admin เท่านั้น   ** future owner only
```

- แสดงบริษัทที่กำลังดูชัดในทุกหน้า แม้ผู้ใช้เห็นได้บริษัทเดียว
- แสดง switcher เฉพาะ owner; admin/cs แสดงชื่อบริษัทแบบอ่านอย่างเดียว
- URL ของ owner ต้องคง `?company=` ระหว่าง navigation
- owner ที่ยังไม่เลือกบริษัทต้องเห็น company-picker state ไม่ใช่ empty state
- ซ่อน route ที่ไม่มีสิทธิ์ แต่ server authorization เป็นด่านจริงเสมอ
- Desktop/tablet เป็นเป้าหมายหลักของหลังบ้าน; mobile ต้องอ่านและทำ critical action ได้

---

## 5. Wireframe ฝั่งผู้เรียน

### S01 — Pre-join / กรอกชื่อเข้าห้อง

**Route:** `/join/[token]` · `AS-IS` · ไม่ล็อกอิน · mobile-first

```text
┌─────────────────────────────────┐
│ Brand                           │
│ ชื่อบทเรียน                     │
│ หน่วยงานผู้รับ (ถ้ามี)          │
├─────────────────────────────────┤
│                                 │
│       Local camera preview      │
│       / กล้องปิด / error         │
│                                 │
├─────────────────────────────────┤
│ [ไมค์] [กล้อง]  ระดับเสียง       │
│                                 │
│ ชื่อของคุณ                      │
│ [_____________________________] │
│                                 │
│ [       เข้าห้องเรียน       ]   │
│ ข้อความ privacy/voice           │
└─────────────────────────────────┘
```

**ข้อมูล:** `lessonTitle`, `recipientOrgName?`, `recipientName`; ชื่อยาวไม่เกิน 100 ตัวอักษร

**พฤติกรรม:**

- returning learner ที่มี key+name: join เดิมแล้วไปห้องหรือหน้าจบอัตโนมัติ
- returning learner กลับเข้า session เดิมได้แม้ link หมดอายุ หาก session เคยเริ่มแล้ว
- ผู้เรียนใหม่บน link หมดอายุ: ไป S04
- เปิด/ปิดกล้องและไมค์ได้; กล้องเป็น preview ในเครื่องเท่านั้น
- สร้าง session สำเร็จก่อนเข้า room เพื่อตัด half-joined state

**สถานะ:** loading link, requesting permission, preview ready, camera off, permission denied, no device, unsupported browser, empty/invalid name, joining, join error, expired/invalid

**Acceptance criteria:**

- CTA กดไม่ได้เมื่อชื่อว่างหรือกำลังส่ง และ error ผูกกับ field/action ที่เกี่ยวข้อง
- ผู้ใช้เข้าใจว่ากล้องไม่ได้เชื่อมกับมนุษย์อีกฝั่ง
- permission denied ต้องบอกวิธีไปแก้ที่ browser ไม่ใช่แจ้ง error อย่างเดียว
- keyboard/focus และ screen-reader label ครบทุก control

### S02 — Voice classroom / ห้องเรียน

**Route:** `/room/[token]` · `AS-IS` · หน้าหลักของสินค้า

```text
┌──────────────────────────────────────────────────────────┐
│ Brand                              เชื่อมต่ออยู่ / สถานะ │
├──────────────────────────────────────┬───────────────────┤
│                                      │ AI tile           │
│       Google Slides / PDF            │ พูด/คิด/เตรียม    │
│       slide N / total                ├───────────────────┤
│       reference slide notice         │ Learner preview   │
│                                      │ local camera      │
├──────────────────────────────────────┴───────────────────┤
│ mic notice / degraded / reconnect                        │
├──────────────────────────────────────────────────────────┤
│ [ไมค์] [กล้อง] [กดค้างเพื่อพูด] [เสียง AI] [แชต] [จบ]  │
└──────────────────────────────────────────────────────────┘
                         ┌─────────────────────────────┐
                         │ Chat + Q&A drawer           │
                         │ history / input / send      │
                         └─────────────────────────────┘
```

**สถานะ AI ที่ต้องสื่อโดยไม่พึ่งข้อความอย่างเดียว:** preparing, ready, narrating, waiting, recording, processing, answering, paused, completed, error

**กติกาพฤติกรรม:**

- หน้า ready มี CTA “พร้อมแล้ว เริ่มเรียนเลย” และเริ่มด้วยเสียงได้
- PTT เปิดเฉพาะช่วงที่ถามได้; recording เริ่มเมื่อกดและส่งเมื่อปล่อย
- ผู้เรียน interrupt narration เพื่อถาม แล้วระบบกลับจุดเดิม
- ระหว่างตอบอาจแสดง related slide ชั่วคราว ต้องบอกว่าหลังตอบจะกลับสไลด์เดิม
- TTS พังต้องแจ้ง degraded state แต่ไม่บังคับจบ session
- chat และ voice question เป็นคนละข้อมูล แม้อาจรวมใน drawer
- กดจบต้องมี confirm ที่บอกผลของการจบก่อนเรียนครบ

**Responsive:**

- Mobile: slide เป็นพื้นที่หลัก; AI/learner tile ย่อได้; PTT อยู่ระยะนิ้วโป้ง; drawer เป็น bottom sheet/full screen
- Desktop: slide + tiles ด้านข้าง; drawer ไม่บัง control หลัก
- ห้ามใช้สีอย่างเดียวแยก recording/processing/error

**Acceptance criteria:**

- ผู้ใช้รู้ตลอดว่า AI กำลังพูด ฟัง หรือคิด
- ปุ่ม PTT มี label/state ชัดและมี keyboard-accessible alternative
- reconnect ไม่ทำให้เกิด session ใหม่หรือส่งข้อความซ้ำ
- error state มี retry/exit ตามสิ่งที่แก้ได้จริง

### S03 — Learner recap / เรียนจบแล้ว

**Route:** `/session-ended/[token]` · `AS-IS`

```text
┌──────────────────────────────────────┐
│ เรียนจบแล้ว                          │
│ เรียนครบ/จบก่อนครบ · จำนวนคำถาม      │
├──────────────────────────────────────┤
│ คำถามของคุณ                          │
│ ┌ คำถาม ──────────────────────────┐ │
│ │ คำตอบ / ตอบไม่ได้ / นอกขอบเขต   │ │
│ └─────────────────────────────────┘ │
│              ...                     │
├──────────────────────────────────────┤
│ [เรียนอีกครั้ง]                       │
└──────────────────────────────────────┘
```

**ข้อมูลที่แสดงได้:** คำถาม/คำตอบของผู้เรียนคนนี้, answer status, `completedAllSlides`

**ห้ามแสดง:** review result/note, unanswered points รวม, provider/model, document/vector identifiers, session ของคนอื่น

**สถานะ:** loading, completed, ended early, no questions, restart submitting/error, expired link ที่ restart ไม่ได้

**Acceptance criteria:** “เรียนอีกครั้ง” สื่อชัดว่าเริ่มรอบใหม่; empty state ไม่ทำให้ผู้เรียนรู้สึกว่าทำผิด

### S04 — Invalid or expired link

**Route:** `/link-expired` · `AS-IS` + `DECISION NEEDED`

```text
┌──────────────────────────────────────┐
│ ลิงก์นี้ใช้งานไม่ได้                  │
│ อาจหมดอายุหรือไม่ถูกต้อง             │
│                                      │
│ [ช่องทางติดต่อ / กลับหน้าหลัก]*       │
└──────────────────────────────────────┘
* Product ต้องกำหนดเจ้าของช่องทางและข้อความ
```

ห้ามเปิดเผยว่า token ใดเคยมีอยู่หรือมีผู้เรียนกี่คน ข้อความต้องช่วยให้ไปต่อได้ ไม่ใช่ทางตัน

---

## 6. Wireframe ฝั่งหลังบ้าน

### S05 — Sign in

**Route:** `/admin/login` · `AS-IS`

```text
┌──────────────────────────────────────┐
│ Brand                                │
│ เข้าสู่ระบบหลังบ้าน                  │
│ อีเมล       [____________________]   │
│ รหัสผ่าน    [____________________] 👁 │
│ [            เข้าสู่ระบบ          ] │
│                                      │
│ Organization SSO*                    │
└──────────────────────────────────────┘
* FUTURE CONCEPT ไม่ระบุ Google จนกว่าจะเลือก IdP
```

**สถานะ:** default, invalid field, submitting, invalid credentials (ข้อความเดียว), inactive account, inactive company, network error

**Behavior:** success → forced password change หรือ S07; ไม่มี forgot password/SSO ในระบบปัจจุบัน

**Security UX:** ห้ามบอกว่าอีเมลมีอยู่หรือไม่; password manager/autocomplete ทำงานได้; error ไม่ล้างอีเมล

### S06 — Change password

**Route:** `/admin/change-password` · `AS-IS`

```text
┌──────────────────────────────────────┐
│ เปลี่ยนรหัสผ่าน                      │
│ เหตุผลที่ต้องเปลี่ยน*                 │
│ รหัสผ่านปัจจุบัน [_______________]   │
│ รหัสผ่านใหม่    [_______________]   │
│ ยืนยันรหัสผ่าน  [_______________]** │
│ ข้อกำหนดรหัสผ่าน                     │
│ [บันทึกรหัสผ่าน]                     │
└──────────────────────────────────────┘
* แสดงเมื่อ first login   ** UX field; backend รับ current/new
```

**สถานะ:** forced/voluntary, mismatch, wrong current, policy error, submitting, success

**Acceptance criteria:** forced flow ออกจากหน้านี้ไม่ได้จนสำเร็จ; voluntary flow ย้อนกลับได้; success อัปเดต user state แล้วไป S07

### S07 — Link list / back-office home

**Route:** `/admin?company={id}` · `AS-IS`

```text
┌─────────────────────────────────────────────────────────┐
│ รายการลิงก์                          [+ สร้างลิงก์]     │
│ [ค้นหา] [บทเรียน] [ACTIVE/EXPIRED] [ช่วงวัน]*          │
├─────────────────────────────────────────────────────────┤
│ บทเรียน │ หน่วยงาน │ หมดอายุ │ ผู้เข้าเรียน │ ผู้สร้าง │
│ ...      │ ...       │ ACTIVE  │ 12          │ ...      │
└─────────────────────────────────────────────────────────┘
* filter behavior เป็น DESIGN REQUIRED
```

**ข้อมูลต่อแถว:** lesson title/slug, recipient organization, expiry + computed status, learner count, creator

**สถานะ:** owner no-company, loading, empty-first-use, empty-filter, API error, long list/pagination decision, copied link feedback

**ข้อควรระวัง:** `ACTIVE/EXPIRED` คำนวณจากเวลา ไม่ได้เก็บ status; destructive demo reset เป็น owner-only และไม่ควรอยู่ใกล้งานประจำ

### S08 — Create training link

**Route:** `/admin/links/new` · `AS-IS`

```text
┌────────────────────────────────────────────┐
│ สร้างลิงก์การเรียน                         │
│ บทเรียน*          [เลือก / ค้นหา ▾]        │
│ หน่วยงานผู้รับ     [____________________]  │
│ วันหมดอายุ*        [date/time picker]       │
│ จำนวนผู้เรียนสูงสุด [____________________]* │
│ [ยกเลิก] [สร้างลิงก์]                      │
├────────────────────────────────────────────┤
│ Success: URL [________________] [คัดลอก]   │
└────────────────────────────────────────────┘
* MaxAttendees มี field ใน DB แต่ยังไม่ enforce — ห้ามออกแบบเป็นข้อจำกัดที่ทำงานแล้ว
```

**สถานะ:** lessons loading/empty, no active lesson, validation, submitting, success, copy success/fail, API error

**Acceptance criteria:** เลือกได้เฉพาะ active lesson; อธิบายคำว่า “หน่วยงาน”; success ไม่ทำให้ URL หายก่อนผู้ใช้คัดลอก

### S09 — Training link detail + learners

**Route:** `/admin/links/[token]` · `AS-IS`

```text
┌─────────────────────────────────────────────────────────┐
│ ← ลิงก์ / บทเรียน · หน่วยงาน · ACTIVE/EXPIRED          │
│ Public URL [______________________________] [คัดลอก]   │
│ วันหมดอายุ · จำนวนผู้เรียน                              │
├─────────────────────────────────────────────────────────┤
│ ผู้เรียน │ ความคืบหน้า │ สถานะ       │ ล่าสุด │ คำถาม │
│ ก       │ 7/20 ▰▰▰▱   │ กำลังเรียน   │ ...    │ 2     │
│ ข       │ 4/20 ▰▱▱▱   │ หยุดกลางคัน │ ...    │ 1     │
│ ค       │ 20/20       │ เรียนจบ      │ ...    │ 3     │
└─────────────────────────────────────────────────────────┘
```

**สถานะต่อ session:** `IN_PROGRESS`, `ENDED`, `stalled` ซึ่งคำนวณจาก `LastActivityAt`; stalled เป็น inference ไม่ใช่ข้อเท็จจริง

**สถานะหน้า:** loading, no learners, active/expired link, realtime update/reconnecting, long list, not found/forbidden

**Action:** เลือกแถว → S10; copy URL; ไม่มี action เปลี่ยน session ของผู้เรียนจากหน้านี้ในปัจจุบัน

### S10 — Learning session review

**Route:** `/admin/learning-sessions/[id]` · `AS-IS` + `DESIGN REQUIRED`

```text
┌─────────────────────────────────────────────────────────┐
│ ← ผู้เรียน · บทเรียน · เวลา · progress/status           │
│ คำถามทั้งหมด N · ตอบไม่ได้ N · ยังไม่ตรวจ N             │
├─────────────────────────────────┬───────────────────────┤
│ Review queue                    │ Live chat             │
│ Q transcript                    │ message history       │
│ AI answer · answer status       │ [message________][ส่ง]│
│ related slide                   │                       │
│ ( ) ถูก  ( ) ผิด               │ connection status     │
│ หมายเหตุ [___________________]  │                       │
│ save state / reviewer / time    │                       │
└─────────────────────────────────┴───────────────────────┘
```

**สถานะ:** live/reconnecting, no questions, unanswered/out-of-scope/no-speech/transcription-failed, unreviewed, reviewed-correct, reviewed-incorrect, saving/error

**Interaction requirement:** รองรับตรวจหลายข้อเร็ว, keyboard navigation, filter “ยังไม่ตรวจ”, auto-save หรือ explicit save ต้องเลือกหนึ่งแบบและบอกสถานะชัด

**หมายเหตุ:** ปัจจุบัน review note เป็น free text; reason taxonomy/work queue เป็น `FUTURE CONCEPT`

### S11 — Lesson list

**Route:** `/admin/lessons` · `AS-IS` + `DESIGN REQUIRED`

```text
┌─────────────────────────────────────────────────────────┐
│ บทเรียน                              [+ สร้างบทเรียน]* │
│ [ค้นหา] [Google Slides/PDF] [Active/Inactive]           │
├─────────────────────────────────────────────────────────┤
│ ชื่อ │ source │ active │ แก้ไขล่าสุด │ indexing/ปัญหา*  │
└─────────────────────────────────────────────────────────┘
* ตรวจ API ก่อนกำหนด create route/health aggregate
```

**สถานะ:** loading, empty, filtered empty, active/inactive, source badge, API error

### S12 — Lesson editor

**Route:** `/admin/lessons/[slug]` · `AS-IS` + `DESIGN REQUIRED` · ฟอร์มซับซ้อนที่สุด

```text
┌─────────────────────────────────────────────────────────┐
│ ← บทเรียน / ชื่อ                         [บันทึก]       │
│ [ข้อมูลทั่วไป] [แหล่งเนื้อหา] [Timing] [เอกสาร]        │
├──────────────────────────────────┬──────────────────────┤
│ ชื่อ · คำอธิบาย · active         │ Preview              │
│ Source: Google Slides / PDF      │ slide/page            │
│ URL หรือ Upload PDF              │ sync/loading/error    │
│ Sync / resolve status            │                       │
│ Intro/Breath/Final wait          │                       │
│ Slide list + video duration      │                       │
│ Attached documents              │                       │
└──────────────────────────────────┴──────────────────────┘
```

**สถานะ:** initial load, unsaved, saving/saved/error, invalid URL, resolving/sync success/fail, PDF uploading/preview error, empty slides, inactive, attached document indexing states

**Critical rules:** Google/PDF เป็นคนละ source; save ต้องไม่ทิ้งข้อมูลโดยเงียบ; เปลี่ยน source ต้อง confirm ผลกระทบ; timing หน่วยต้องชัด; preview ไม่ใช่ production learner view

### S13 — Global knowledge documents

**Route:** `/admin/documents` · `AS-IS` + `DESIGN REQUIRED`

```text
┌─────────────────────────────────────────────────────────┐
│ คลังเอกสารที่ใช้ได้ทุกบทเรียน       [อัปโหลดเอกสาร]   │
│ drop zone · type/size guidance                           │
├─────────────────────────────────────────────────────────┤
│ ชื่อไฟล์ │ ขนาด │ pending/indexed/failed │ chunks │ ... │
└─────────────────────────────────────────────────────────┘
```

**สถานะ:** empty, drag-over, uploading/progress, unsupported/oversize, pending, indexed, failed, delete confirm/error

**ข้อจำกัดปัจจุบัน:** indexing queue อยู่ใน memory; restart อาจทำ pending ค้าง; durable retry/reindex/delete-vector consistency เป็น future work จึงห้าม UI สัญญาว่า retry อัตโนมัติแล้ว

### S14 — User management

**Route:** `/admin/users` · `AS-IS`

```text
┌─────────────────────────────────────────────────────────┐
│ จัดการผู้ใช้ · บริษัท X                [+ เพิ่มผู้ใช้]  │
├─────────────────────────────────────────────────────────┤
│ ชื่อ │ อีเมล │ สิทธิ์ │ active │ last login │ action   │
├─────────────────────────────────────────────────────────┤
│ Add drawer/modal                                       │
│ ชื่อ · อีเมล · role · initial password                 │
│ [ยกเลิก] [เพิ่มและแสดงวิธีส่งต่อรหัส]                  │
└─────────────────────────────────────────────────────────┘
```

**Permission:** owner จัดการได้ทุกบริษัท/สร้าง owner; admin จัดการบริษัทตนและกำหนดได้ไม่สูงกว่าตน; cs เข้าไม่ได้

**สถานะ:** no company, loading, empty, duplicate email, weak password, submitting, deactivate confirm, last active owner/admin guard, forbidden/error

**Security UX:** initial password แสดง/ส่งต่ออย่างระมัดระวัง; ระบบยังไม่มี invite email; ห้ามทำให้ disabled role ดูเหมือน bug

---

## 7. Future concept — แยกออกจาก current scope

### F01 — Company management

`DESIGN REQUIRED` สำหรับ owner; API list/create/update มีบางส่วน แต่ยังไม่มี frontend route

- list/search company, create, rename, activate/deactivate
- deactivation confirm ต้องบอกว่าพนักงานบริษัทนั้นจะ sign in ไม่ได้ แต่ข้อมูลไม่ถูกลบ
- ต้องออกแบบ no company และ “กำลังดูบริษัทใด” ร่วมกับ switcher
- route และ IA ต้องตกลงกับ engineering ก่อน handoff final

### F02 — Provider/API key settings

`FUTURE CONCEPT` + `DECISION NEEDED`; ดู [`PROVIDER_SETTINGS_SPEC.md`](./PROVIDER_SETTINGS_SPEC.md)

```text
Provider profiles → Test connection → Compatibility check
→ Impact preview → Activate → Monitor → Rollback
```

- ออกแบบตาม capability ไม่ใช่ vendor: STT, Answer LLM, Embedding, Vector, TTS, Storage, Content
- API ไม่คืน plaintext key; UI แสดง masked fingerprint/last four เท่านั้น
- แยก draft/testing/active/failed/retired และ audit/history
- เปลี่ยน embedding อาจบังคับ reindex; confirm ต้องบอก cost/downtime/affected companies
- ห้ามระบุ scope รายบริษัทจนตัดสิน shared key vs BYOK

### F03 — Knowledge operations

`FUTURE CONCEPT` + `DECISION NEEDED`; ดู [`KNOWLEDGE_ROADMAP.md`](./KNOWLEDGE_ROADMAP.md)

- source health/version, extracted-content preview, indexing jobs/retry
- review queue แยก missing knowledge/retrieval miss/hallucination/bad transcription/stale/wrong scope
- resolve → source update → reindex → re-evaluate → close
- quality dashboard ต้องรอ metrics/owner/retention; ห้ามสร้าง KPI จากข้อมูลที่ระบบยังไม่เก็บ

---

## 8. Shared component และ state contract

| Component/state | ต้องสื่อ |
|---|---|
| Loading | กำลังรออะไร; skeleton/spinner ที่ไม่ทำ layout กระโดด |
| Empty-first-use | ความหมาย + action เริ่มต้น |
| Empty-filter | ไม่มีผลจากเงื่อนไข + clear filter |
| Recoverable error | เกิดกับ action ใด + retry โดยไม่เสีย input |
| Fatal error | ไปต่อไม่ได้เพราะอะไร + safe exit/contact |
| 401 expired | session login หมดอายุ → sign in ใหม่ |
| 403 forbidden | ล็อกอินถูกแล้วแต่ไม่มีสิทธิ์ → ห้ามวนไป login |
| 404 | resource ไม่มี/ถูกลบ โดยไม่เปิดข้อมูลข้ามบริษัท |
| Offline/reconnect | ข้อมูลอาจ stale; action ที่ queue/retry ได้ต้องชัด |
| Destructive confirm | target, impact, recoverability, typed/step-up confirm เมื่อเสี่ยงสูง |
| Save state | unsaved/saving/saved/error; retry ไม่สร้างข้อมูลซ้ำ |
| Status badge | มี text/icon ไม่ใช้สีอย่างเดียว |

## 9. Responsive และ accessibility definition

- รองรับอย่างน้อย 320px, tablet และ desktop; room ทดสอบ landscape/portrait
- touch target ไม่น้อยกว่า 44×44px สำหรับ learner controls
- tab order, focus visible, skip link, dialog focus trap/restore และ Escape behavior
- form มี label/error/description association; live state ใช้ `aria-live` อย่างไม่รบกวน
- PTT ต้องมีทางเลือกสำหรับผู้ใช้ที่กดค้างไม่ได้
- transcript/Q&A อ่านด้วย screen reader ตามลำดับเวลา
- contrast, reduced motion, text zoom 200% และไม่บังคับ animation เพื่อเข้าใจสถานะ

## 10. สิ่งที่ UX/UI ต้องส่งกลับ

1. Sitemap และ role-aware navigation
2. Wireframe + hi-fi ของ S01–S14 ครบ default/loading/empty/error/permission states
3. Concept แยกไฟล์สำหรับ F01–F03 พร้อมป้าย future/assumption
4. Prototype: first login, lesson→link, new/returning learner, voice question, incorrect-answer review
5. Component inventory, tokens และ responsive behavior
6. Thai content spec: labels, helper, error, privacy, confirmations
7. Annotation ต่อหน้า: API/data dependency, permission, transition, acceptance criteria
8. Accessibility notes และ keyboard/PTT alternative

## 11. Out of scope จนกว่าจะมี decision

- เปลี่ยนผู้เรียนให้มีบัญชี
- admin/cs หลายบริษัทแบบเลือกบางบริษัท
- provider switching ที่ activate ได้ทันทีโดยไม่มี test/version/rollback
- plaintext API key reveal
- SSO/forgot password/invite/MFA ที่แสดงเหมือนพร้อมใช้
- Knowledge dashboard ที่ใช้ metrics หรือ taxonomy ที่ยังไม่ได้เก็บ
- Max attendees ที่ UI แสดงว่า enforce แล้ว

