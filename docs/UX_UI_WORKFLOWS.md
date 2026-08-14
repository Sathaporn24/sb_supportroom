# SupportRoom AI — UX/UI Workflows

> Companion ของ [`UX_UI_WIREFRAME_SPEC.md`](./UX_UI_WIREFRAME_SPEC.md)  
> อธิบาย transition ระหว่างหน้าจอ ระบบ และบทบาท โดยแยก current flow ออกจาก future concept

## WF01 — ผู้เรียนใหม่เข้าห้องและเรียนจนจบ (`AS-IS`)

```mermaid
flowchart TD
    A["เปิด /join/{token}"] --> B{"โหลด public link สำเร็จ?"}
    B -- "ไม่สำเร็จ/invalid" --> X["/link-expired"]
    B -- "สำเร็จ" --> C{"มี learnerKey + name เดิม?"}
    C -- "ไม่มี" --> D["แสดง pre-join + local media preview"]
    D --> E{"ชื่อถูกต้องและกดเข้าห้อง?"}
    E -- "ไม่" --> D
    E -- "ใช่" --> F["สร้าง LearningSession"]
    F -- "ผิดพลาด" --> D
    F -- "สำเร็จ" --> G["/room/{token}"]
    G --> H["AI เตรียมบทเรียน"]
    H --> I["พร้อมเริ่ม"]
    I --> J["บรรยาย slide"]
    J --> K{"ผู้เรียนถาม/แชต/จบ?"}
    K -- "ถามเสียง" --> L["PTT → STT/RAG → คำตอบ"]
    L --> J
    K -- "แชต" --> M["ส่งข้อความ + history"]
    M --> J
    K -- "จบ/ครบ" --> N["End session"]
    N --> O["/session-ended/{token}"]
    O --> P{"เรียนอีกครั้ง?"}
    P -- "ใช่และ link ใช้ได้" --> Q["สร้าง LearningSession รอบใหม่"]
    Q --> G
```

### UX checkpoints

- Pre-join: permission ถูกปฏิเสธแล้วต้องมี recovery instructions
- Room: state AI ต้องเห็น/ได้ยินชัด; processing หลายวินาทีต้องไม่ดูเหมือนค้าง
- Question: no speech/transcription fail/not found/out of scope ต้องใช้ข้อความคนละแบบ
- End: จบก่อนครบต้อง confirm; recap เห็นเฉพาะ session ของ browser ตน

## WF02 — ผู้เรียนกลับมาเรียนต่อและ link หมดอายุ (`AS-IS`)

```mermaid
flowchart TD
    A["เปิด link อีกครั้ง"] --> B{"มี key+name ใน browser?"}
    B -- "ไม่มี" --> C{"link ยัง active?"}
    C -- "ใช่" --> D["Pre-join ใหม่"]
    C -- "ไม่" --> X["Expired page"]
    B -- "มี" --> E["Join แบบ idempotent"]
    E --> F{"พบ session เดิม?"}
    F -- "ไม่พบ + link active" --> D
    F -- "ไม่พบ + link expired" --> X
    F -- "พบ IN_PROGRESS" --> G["กลับ /room และ resume progress"]
    F -- "พบ ENDED" --> H["ไป learner recap"]
```

**กติกา:** link expiry ป้องกันการเริ่มใหม่ แต่ไม่ไล่ผู้เรียนที่เริ่มทันเวลาออกกลางคัน

## WF03 — Voice question และ grounded answer (`AS-IS`)

```mermaid
sequenceDiagram
    actor L as ผู้เรียน
    participant UI as Room UI
    participant API as Backend
    participant STT as Audio/STT provider
    participant KB as Knowledge retrieval
    participant AI as Answer model
    participant DB as SessionQuestion

    L->>UI: กด PTT ค้าง
    UI-->>L: recording state
    L->>UI: ปล่อยปุ่ม
    UI->>API: audio + token + learnerKey
    API->>STT: ถอดเสียง/จำแนก readiness
    alt ไม่มีเสียงหรือถอดเสียงพัง
        API-->>UI: no_speech / transcription_failed
    else readiness intent
        API-->>UI: ready/not-ready (ไม่บันทึกเป็นคำถาม)
    else คำถามจริง
        API->>KB: ค้น lesson + company-global namespace
        alt ไม่มีหลักฐานเพียงพอ
            API->>DB: บันทึก not_found/out_of_scope
            API-->>UI: แจ้งตรง ๆ ว่าตอบไม่ได้
        else พบหลักฐาน
            API->>AI: grounded context + question
            AI-->>API: answer + related slide
            API->>DB: บันทึก answered
            API-->>UI: คำตอบ + reference behavior
        end
    end
```

**ห้าม UX เปลี่ยน:** ผู้เรียนไม่เห็นว่าส่งข้อใดให้ CS ตรวจ; readiness question ไม่ปรากฏใน review queue

## WF04 — Login, first password change และ authorization (`AS-IS`)

```mermaid
flowchart TD
    A["/admin/login"] --> B["ส่ง email + password"]
    B --> C{"ตรวจผ่าน?"}
    C -- "ไม่ผ่าน" --> D["ข้อความ credentials เดียวกัน"]
    C -- "account/company inactive" --> E["แจ้งถูกปิด + contact admin"]
    C -- "ผ่าน" --> F["ออก JWT + โหลด signed-in user"]
    F --> G{"MustChangePassword?"}
    G -- "ใช่" --> H["/admin/change-password"]
    H --> I{"เปลี่ยนสำเร็จ?"}
    I -- "ไม่" --> H
    I -- "ใช่" --> J["Back-office home"]
    G -- "ไม่" --> J
    J --> K{"JWT หมดอายุ?"}
    K -- "ใช่" --> A
    K -- "403" --> L["Forbidden state ไม่วน login"]
```

**ปัจจุบันไม่มี:** refresh token, server-side revoke/logout session, forgot/reset email, invite, SSO, MFA

## WF05 — Company context และ permission (`AS-IS`)

```mermaid
flowchart TD
    A["เข้าสู่ back office"] --> B{"Role"}
    B -- "owner" --> C["โหลด active companies"]
    C --> D{"เลือก company แล้ว?"}
    D -- "ไม่" --> E["Company picker state"]
    D -- "ใช่" --> F["คง ?company= ระหว่าง navigation"]
    B -- "admin/cs" --> G["ใช้ CompanyId ของบัญชี"]
    F --> H["โหลดข้อมูล tenant"]
    G --> H
    H --> I{"ขอข้อมูล company อื่น?"}
    I -- "admin/cs" --> J["403"]
    I -- "owner" --> H
```

### Role actions

| Action | Owner | Admin | CS |
|---|:---:|:---:|:---:|
| ลิงก์/บทเรียน/เอกสาร/review | ✓ ทุกบริษัท | ✓ บริษัทตน | ✓ บริษัทตน |
| จัดการผู้ใช้ | ✓ | ✓ บริษัทตน | — |
| สร้าง/กำหนด owner | ✓ | — | — |
| Company management | ✓ | — | — |
| Provider/system config | future owner only | — | — |

## WF06 — สร้าง/แก้บทเรียน แล้วสร้างลิงก์ (`AS-IS`)

```mermaid
flowchart TD
    A["Lesson list"] --> B["เปิด lesson editor"]
    B --> C{"Source type"}
    C -- "Google Slides" --> D["ใส่ URL → Resolve/preview"]
    C -- "PDF" --> E["Upload → Preview pages"]
    D --> F{"resolve สำเร็จ?"}
    E --> F
    F -- "ไม่" --> G["แก้ source/retry โดยไม่เสียฟอร์ม"]
    F -- "ใช่" --> H["ตั้ง metadata/timing/slide config"]
    H --> I["Save lesson"]
    I --> J{"Active?"}
    J -- "ไม่" --> K["เก็บได้ แต่สร้าง link ไม่ได้"]
    J -- "ใช่" --> L["Create training link"]
    L --> M["เลือก lesson + org + expiry"]
    M --> N["สร้าง URL"]
    N --> O["Copy/share"]
```

**Critical error UX:** source sync, PDF upload, save และ link creation เป็นคนละ operation ต้องบอกว่าอะไรล้มและ retry เฉพาะชั้นได้

## WF07 — Document ingestion (`AS-IS` พร้อม production gap)

```mermaid
stateDiagram-v2
    [*] --> UploadSelected
    UploadSelected --> Uploading
    Uploading --> Rejected: type/size invalid
    Uploading --> Pending: metadata saved
    Pending --> Indexed: extract/chunk/embed/upsert สำเร็จ
    Pending --> Failed: pipeline error
    Failed --> Pending: retry (future durable flow)
    Indexed --> Deleted: delete requested
```

**ข้อเท็จจริงปัจจุบัน:** background queue ยังไม่ durable และ retry/delete-vector consistency ยังเป็น production gap UI ต้องไม่แสดง automation ที่ backend ยังไม่มี

## WF08 — CS ตรวจคำตอบ AI (`AS-IS` → future feedback loop)

```mermaid
flowchart LR
    A["Link detail"] --> B["เลือก LearningSession"]
    B --> C["ดู Q&A + answerStatus"]
    C --> D{"ตรวจแล้ว?"}
    D -- "ถูก" --> E["ReviewResult=correct + note"]
    D -- "ผิด" --> F["ReviewResult=incorrect + note"]
    E --> G["บันทึก reviewer/time"]
    F --> G
    G -. "future" .-> H["จำแนกสาเหตุ"]
    H -.-> I["แก้ source/retrieval/prompt"]
    I -.-> J["reindex/eval/resolve"]
```

เส้นทึบคือสิ่งที่มีแล้ว เส้นประคือ Knowledge operations ที่ยังต้องออกแบบและพัฒนา

## WF09 — Provider/API key activation (`FUTURE CONCEPT`)

```mermaid
flowchart TD
    A["สร้าง/แก้ Provider Profile draft"] --> B["ใส่ secret ใหม่"]
    B --> C["Test connection"]
    C --> D{"ผ่าน?"}
    D -- "ไม่" --> E["Safe error + แก้ draft"]
    D -- "ผ่าน" --> F["Compatibility check"]
    F --> G{"Embedding/vector เปลี่ยน?"}
    G -- "ใช่" --> H["สร้าง index version + reindex"]
    G -- "ไม่" --> I["Regression eval/canary"]
    H --> I
    I --> J["Impact preview: company/cost/downtime/fallback"]
    J --> K["Owner re-auth + activate"]
    K --> L["Monitor health"]
    L --> M{"Degraded?"}
    M -- "ใช่" --> N["Rollback previous version"]
    M -- "ไม่" --> O["Retire old version ตาม policy"]
```

**Decision gates ก่อน final design:** global vs per-company/BYOK, KMS/Vault, deployment/data region, config version per answer, retention และ approval policy

## WF10 — Knowledge improvement loop (`FUTURE CONCEPT`)

```mermaid
flowchart TD
    A["คำถามจริง + CS review"] --> B["Triage reason"]
    B --> C{"สาเหตุ"}
    C -- "Missing/stale knowledge" --> D["แก้ source"]
    C -- "Retrieval miss" --> E["chunk/metadata/retrieval"]
    C -- "Hallucination/policy" --> F["prompt/model/guardrail"]
    C -- "Bad transcription" --> G["STT/audio flow"]
    C -- "Wrong scope" --> H["tenant/namespace security"]
    D --> I["Reindex/version"]
    E --> I
    F --> J["Eval without reindex"]
    G --> J
    H --> J
    I --> J["Regression dataset"]
    J --> K{"quality/latency/cost ผ่าน?"}
    K -- "ไม่" --> B
    K -- "ผ่าน" --> L["Approve → deploy → monitor → resolve"]
```

## Cross-flow error routing

| Event | UX response | ห้ามทำ |
|---|---|---|
| Public token invalid/unknown | S04 generic message | บอกว่า token เคยมีหรือเผย tenant |
| Link expired, new learner | S04 | สร้าง session ใหม่ |
| Link expired, existing in-progress learner | resume room | ไล่ออกจากบทเรียนกลางคัน |
| Admin JWT expired | เก็บ intended URL แล้ว sign in ใหม่ | แสดง 403 |
| Admin forbidden | 403 + back to allowed scope | วนไป login |
| Provider/TTS unavailable | degraded/retry ตาม capability | จบ learner session อัตโนมัติ |
| Network reconnect | แสดง stale/reconnecting และ idempotent retry | สร้าง session/message ซ้ำ |
| Save failed | เก็บ input + retry | ล้างฟอร์ม |

## Workflow acceptance checklist

- ทุก transition มี trigger, loading, success, error และ safe destination
- back/refresh/deep link ทำงานโดยไม่ข้าม permission หรือสร้างข้อมูลซ้ำ
- owner company context ไม่หายระหว่างหน้า
- returning learner กลับ session เดิม; restart เท่านั้นที่สร้างรอบใหม่
- realtime disconnect แยกจาก fatal error
- future workflow มีป้ายชัดและไม่ผสมใน prototype ของ release ปัจจุบัน

