# AGENTS.md

คำแนะนำสำหรับ Codex ที่ทำงานใน `sb_supportroom` ไฟล์นี้เป็น **Codex entry point** และ
compatibility adapter สำหรับ workflow เดียวกับ Claude Code ไม่ใช่ handbook หรือ agent pipeline
อีกชุดหนึ่ง

เป้าหมายคือให้ทั้งสองระบบใช้ logic ร่วมกันดังนี้:

```text
Claude Code → CLAUDE.md → .claude/shared/conventions.md → .claude/agents/<role>.md
Codex       → AGENTS.md → CLAUDE.md → .claude/shared/conventions.md → .claude/agents/<role>.md
```

อย่าคัดลอกหรือออกแบบ pipeline ใหม่ในไฟล์นี้ และอย่าแก้ `CLAUDE.md`/`.claude/` เพียงเพื่อทำ
Codex compatibility เว้นแต่ผู้ใช้ขอแก้ shared workflow ต้นทางอย่างชัดเจน

## Source of truth และลำดับความสำคัญ

1. คำสั่งระบบ ข้อจำกัดของ runtime และคำขอล่าสุดของผู้ใช้มาก่อนเสมอ
2. `AGENTS.md` แปลงวิธีเรียกใช้ workflow ให้เข้ากับ Codex
3. `CLAUDE.md` เป็น project handbook ร่วม: purpose, architecture, commands, เอกสาร และภาพรวม pipeline
4. `.claude/shared/conventions.md` เป็นกฎกลางของ pipeline
5. `.claude/agents/<role>.md` เป็น workflow และ ownership ของ role ที่ผู้ใช้เรียก
6. `_docs/module/<name>/` เป็น business/design/plan/review contract ตาม ownership ที่ pipeline กำหนด
7. โค้ด config entities migrations tests และเอกสารเฉพาะระบบเป็นหลักฐานของ implementation จริง

ยืนยันข้อเท็จจริงจากไฟล์ปัจจุบันก่อนพูดหรือแก้ ห้ามใช้ความจำแทนการอ่านของจริง ถ้า summary/index
ขัดกับ requirement, design หรือ implementation ให้ใช้ source ที่มี authority ตาม ownership ใน
`.claude/shared/conventions.md` และรายงานความขัดแย้ง อย่าปรับ contract ให้ตามโค้ดโดยพลการ

## Routing ก่อนเริ่มงาน

### งานทั่วไปและงาน ad-hoc

- อ่านหัวข้อที่เกี่ยวข้องใน `CLAUDE.md` ก่อนลงมือ โดยค้นหา heading/identifier แล้วอ่าน section นั้น
  ให้ครบ ไม่จำเป็นต้องอ่านทั้งไฟล์
- ใช้ตาราง `Read First` และ `Files to Read Before Changes` ใน `CLAUDE.md` เพื่อเลือกเอกสารและ
  source ที่ต้องตรวจ
- ไม่ต้องโหลด `Agent pipeline` หรือ role files สำหรับคำถาม งานตรวจ หรือการแก้เล็ก ๆ ที่ผู้ใช้ไม่ได้
  เรียก pipeline role
- trace flow เดิมและค้นหาของที่ reuse ได้ก่อนสร้าง implementation ใหม่

### งานที่ผู้ใช้เรียก pipeline role

Role ที่รองรับมี 9 ตัว:

`setup`, `business-analyst`, `system-analyst`, `project-manager`, `frontend-engineer`,
`backend-engineer`, `qa-engineer`, `security`, `devops`

เมื่อผู้ใช้เรียก role ใดโดยชื่อ หรือขอทำงานตาม pipeline ที่ตรงกับ role นั้น:

1. อ่าน `_docs/status.md` เพื่อหา module/phase ปัจจุบัน โดยถือว่าเป็น index ไม่ใช่ source of truth
2. อ่าน `.claude/shared/conventions.md` ให้ครบก่อนทำ action ของ role
3. อ่าน `.claude/agents/<role>.md` ให้ครบ รวม workflow, stop conditions, artifact ownership และ output
4. อ่านเฉพาะ module documents/sections ที่ role และ conventions ระบุ
5. ทำงานเป็น role นั้นใน task ปัจจุบัน แล้วส่งมอบตาม handoff rule เดิม

อย่าอ่าน agent ทั้ง 9 ทุกครั้ง และอย่ารัน pipeline เต็มกับงานเล็ก ๆ ใช้ right-size rules จาก
`CLAUDE.md`/`.claude/shared/conventions.md`

Codex ห้าม spawn subagent, invoke role ถัดไป หรือ chain pipeline เอง เว้นแต่ผู้ใช้ขอ subagent,
parallel work หรือ continuous/autonomous pipeline อย่างชัดเจน แม้ได้รับอนุญาตให้ทำ role ย่อย
role นั้นต้องรักษา ownership และส่งผลกลับผู้ควบคุมตาม workflow เดิม `qa-engineer` และ `security`
ต้องมาจากคำขอของผู้ใช้ทุกครั้ง ห้าม auto-chain

Project-scoped Codex wrappers ของ role ทั้ง 9 อยู่ใน `.codex/agents/*.toml` แต่ละไฟล์มีเฉพาะ
Codex metadata และคำสั่งให้กลับมาอ่าน shared workflow ข้างต้น ไม่ใช่ source of truth ชุดใหม่
ใช้ wrapper เหล่านี้เฉพาะเมื่อผู้ใช้ขอ delegation/subagent หรือเรียก named agent อย่างชัดเจน;
การเรียก role เพื่อให้ task ปัจจุบันทำงานในบทบาทนั้นอย่างเดียวไม่จำเป็นต้อง spawn thread ใหม่

## การแปลง Claude concepts เป็น Codex

ไฟล์ใต้ `.claude/` เขียนสำหรับ Claude Code แต่ให้ Codex รักษา **ความหมายและผลลัพธ์ของ workflow**
โดยใช้ capability ที่ session มีจริง:

| Claude concept | Codex interpretation |
|---|---|
| YAML frontmatter `tools`, `model`, `effort`, `permissionMode` | เป็น Claude metadata ไม่เปลี่ยน model, tools หรือ permission ของ Codex |
| `Read`, `Glob`, `Grep` | ใช้ file reading และ `rg`/`rg --files` หรือเครื่องมือค้นหาที่มี |
| `Write`, `Edit`, `MultiEdit` | ใช้ `apply_patch` และรักษา scope ของไฟล์เดิม |
| `Bash` | ใช้ shell ที่ session อนุญาตตาม sandbox/approval |
| `AskUserQuestion` | ถามผู้ใช้ด้วยช่องทางที่ Codex มี เมื่อ workflow กำหนดให้หยุดรอหรือคำตอบมีผลต่อ contract |
| `Agent`/subagent | ใช้ได้เฉพาะเมื่อผู้ใช้อนุญาต delegation ตาม routing ด้านบน |
| Claude hooks | ไม่ถูกเรียกอัตโนมัติใน Codex ให้ถือข้อจำกัดที่ hook บังคับเป็น instruction |

ถ้า role file ระบุชื่อ tool ที่ Codex ไม่มี ให้ใช้เครื่องมือที่ใกล้เคียงที่สุดโดยไม่เปลี่ยน intent,
ownership, approval gate หรือผลลัพธ์ ห้ามตีความ Claude model name เช่น `sonnet`/`opus` ว่าเป็นคำสั่ง
เปลี่ยน model ของ Codex และห้ามถือว่า `permissionMode` ขยายสิทธิ์เหนือ sandbox ปัจจุบัน

Claude hooks ใต้ `.claude/hooks/` ไม่ทำงานใน Codex โดยอัตโนมัติ ดังนั้นระหว่าง pipeline run ให้
บังคับผลเดียวกันด้วยพฤติกรรม:

- ห้ามเขียนนอก repository ยกเว้น temp ที่ระบบอนุญาต
- ห้ามใช้ state-changing git (`init`, `add`, `commit`, `push`, `checkout`, branch/tag operations
  หรือแก้ `.git/`); read-only git เช่น `status`, `diff`, `log`, `show` ใช้เมื่อจำเป็นต่อการตรวจได้
- ถ้า instruction ขัดกับ sandbox หรือ capability จริง ให้หยุด action นั้นและรายงานข้อจำกัดตรง ๆ

## Repository-specific translation

บางส่วนของ AgentClaude เป็น generic template และอาจพูดถึง Node/Express, Prisma,
`schema.prisma`, Zustand หรือโครงสร้าง `web/`/`api/` ข้อความเหล่านั้นต้องไม่ override
architecture จริงในส่วนต้นของ `CLAUDE.md` และโค้ดปัจจุบันของ repository นี้

ใช้ mapping ต่อไปนี้เมื่อต้องปฏิบัติตาม **logic** ของ generic role:

| Generic AgentClaude term | `sb_supportroom` equivalent |
|---|---|
| `web/`, root `app/`, Next.js app ใหม่ | `frontend/` ซึ่งเป็น Next.js app จริงเพียงจุดเดียว |
| `api/`, Express routes/controllers | `backend/` และ ASP.NET Core controllers/hub |
| Express business logic | `SupportRoom.Application` services/orchestration |
| Prisma models/client | EF Core entities/configuration/repositories ใน Domain/Providers.Data |
| `schema.prisma` working schema | entities + EF configurations + migrations + `ApplicationDbContextModelSnapshot.cs` |
| Prisma migration | EF Core migration ใหม่; ห้ามแก้ migration ที่ deploy แล้ว |
| Zustand store | state pattern ที่มีอยู่จริง; tutor state ใช้ pure reducer และ side effects อยู่ใน hooks |
| Node backend test/build | `dotnet build`/`dotnet test` ตาม `CLAUDE.md` |

Data Model ที่ผู้ใช้ยืนยันใน `_docs/module/<name>/design.md` ยังคงเป็น contract authority ของ pipeline
ส่วน implementation ฝั่ง database ของ repo นี้คือ EF entities/configurations/migrations/snapshot
Backend ห้ามเพิ่ม field/relation ที่ design ไม่มีเอง และ QA ต้องเทียบ contract กับ implementation รวมถึง
DTO/ViewModel และ TypeScript types ที่เกี่ยวข้อง

ถ้า generic `setup` หรือ role file สมมติว่าเป็น empty project ให้ตรวจ repository จริงก่อนเสมอ Repo นี้
scaffold แล้วและมี architecture ชัดเจน ห้ามสร้าง Express/Prisma app หรือ Next.js app ที่ root เป็น
side effect ของคำสั่ง generic

## Pipeline invariants ที่ต้องรักษา

รายละเอียดเต็มอยู่ใน `.claude/shared/conventions.md`; ส่วนนี้บอกเฉพาะสิ่งที่ Codex ห้ามทำหายระหว่าง
การแปลง runtime:

- Module docs อยู่ใต้ `_docs/module/<kebab-name>/` และแต่ละ artifact มี owner ตาม pipeline
- Existing documents ใช้ amend เฉพาะ section ที่เกี่ยวข้องและ append Change Log ห้าม regenerate ทั้งไฟล์
- ก่อนเขียน dated entry ใน pipeline docs ให้ถามวันที่จากผู้ใช้ตาม conventions แม้ runtime จะรู้วันที่
- มีเพียง `qa-engineer` ที่เปลี่ยน checkbox ใน `plan.md` จาก `[ ]` เป็น `[x]` หลังตรวจโค้ดจริง
- Security gate, open issues/findings, review archive และ deploy gate ต้องรักษาตาม ownership เดิม
- `requirement.md` เป็น business contract; `design.md` เป็น technical/data contract; code ห้ามเปลี่ยน
  contract ย้อนหลังเพื่อทำให้สิ่งที่ implement ไปแล้วดูถูกต้อง
- ปิด pipeline run ด้วยการอัปเดตเฉพาะบรรทัดใน `_docs/status.md` ที่ run นั้นกระทบ แล้วบอกว่า
  พร้อมส่งต่อ role ใด โดยไม่เรียก role นั้นเอง
- **การสลับ engine (Claude Code ↔ Codex) กลางงานเป็นการออกแบบตั้งใจ ไม่ใช่ fallback** — ดู
  `.claude/shared/conventions.md` §13: state ของ pipeline อยู่ในไฟล์ (`status.md`/`plan.md`/
  `design.md`/`review.md`) ไม่ได้อยู่ใน memory ของ engine ไหน ดังนั้น role เดียวกันที่ Codex
  ทำกับที่ Claude subagent ทำมีน้ำหนักเท่ากัน — **ห้ามรัน role ซ้ำในอีก engine เพียงเพื่อเช็คผล
  ของตัวเอง** (เช่น Codex ตรวจ QA ติ๊ก checkbox ไปแล้ว ไม่ต้องให้ Claude `qa-engineer` มาตรวจซ้ำ
  ทั้งเฟสอีกรอบ) เว้นแต่ผู้ใช้ขอ second opinion แบบเจาะจงเหตุผล (เช่น phase ติด Security gate
  หรือผลลัพธ์ที่ดูน่าสงสัย) ซึ่งต้องระบุชัดว่าเป็นการตรวจซ้ำโดยตั้งใจ ไม่ใช่ความเคยชิน

Hard stops ของ continuous/autonomous run ยังคงเหมือน Claude workflow และต้องรอผู้ใช้:

1. business interview
2. schema/data-contract confirmation
3. QA ได้ผล ⚠️ หรือ ❌
4. security พบ Critical หรือ Important
5. ก่อน deploy หรือ migration กับ environment จริง

## Implementation และ verification

- รักษา architecture boundaries, provider configuration, feature path และ Definition of Done จาก
  `CLAUDE.md`
- เปลี่ยนเฉพาะสิ่งที่อยู่ใน scope และรักษา unrelated user changes
- Schema change ต้องมี EF migration ใหม่และตรวจ generated migration ด้วยตา โดยเฉพาะ rename/backfill
- Wire contract ใช้ camelCase; เปลี่ยน DTO/ViewModel แล้วอัปเดต TypeScript types คู่กัน
- เลือกรัน lint/typecheck/test/build ตามส่วนที่เปลี่ยน และห้ามอ้างว่าผ่านถ้าไม่ได้รัน
- ตรวจไฟล์ที่แก้และ diff ก่อนส่งมอบ ระบุ command ที่รัน สิ่งที่ผ่าน และสิ่งที่ยังไม่ได้ verify

## Communication และ safety

- คุยกับผู้ใช้เป็นภาษาไทย ใช้ technical terms, identifiers และ paths ตามต้นฉบับ
- นำด้วยผลลัพธ์และหลักฐาน พร้อมบอก assumption, limitation และสิ่งที่ยังไม่ได้ตรวจ
- ห้ามเปิดเผย secret, credential, transcript หรือคำตอบเต็มใน source, log หรือ tool output
- `.env` จริงอ่านเฉพาะเมื่อจำเป็นและห้าม echo ค่าออกมา
- การ deploy, migration กับ environment จริง, external state change หรือ action ที่ย้อนกลับยาก
  ต้องได้รับการยืนยันเฉพาะเจาะจงจากผู้ใช้ก่อน
