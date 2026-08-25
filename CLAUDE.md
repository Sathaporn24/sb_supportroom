# CLAUDE.md

คำแนะนำสำหรับผู้ช่วยเขียนโค้ดและทีมพัฒนาที่ทำงานใน `sb_supportroom`

## System Purpose

ห้องเรียนสาธิตแบบโต้ตอบด้วยเสียง สำหรับทีม CS ของ School Bright ใช้สอนคุณครูให้ใช้งานระบบ
โดยไม่ต้องมีคนนั่งประกบ: CS ตั้งค่าบทเรียน (Google Slides หรือ PDF) → สร้างลิงก์ session →
คุณครูเข้าห้องที่หน้าตาเหมือน video call → AI บรรยายทีละสไลด์เป็นภาษาไทย → คุณครูกด
Push-to-Talk ถามได้ตลอด → AI ตอบโดยอ้างอิงเฉพาะเนื้อหาบทเรียน แล้วกลับไปสอนต่อจากจุดเดิม

หัวใจคือ **grounded tutor** ไม่ใช่ chatbot ทั่วไป — ต้องรายงาน `not_found` เมื่อไม่มีข้อมูล
ไม่ใช่เดาคำตอบ

## Read First

| ต้องการอะไร | อ่านที่ |
|---|---|
| ส่งมอบงานข้ามทีม / สถานะ / decision gates | [`docs/HANDOFF_MASTER.md`](./docs/HANDOFF_MASTER.md) |
| ภาพรวมระบบตามโค้ดจริง, flow, ER, API map, หนี้เทคนิค | [`docs/PROJECT_CONTEXT.md`](./docs/PROJECT_CONTEXT.md) |
| ทางเลือกเทคโนโลยี, build vs buy, MVP/Production/Scale | [`docs/SOLUTION_ARCHITECTURE.md`](./docs/SOLUTION_ARCHITECTURE.md) |
| การตัดสินใจเชิงเทคนิคและเหตุผล | [`docs/TECH_DECISIONS.md`](./docs/TECH_DECISIONS.md) |
| ลำดับงานเพื่อขึ้น production | [`docs/PRODUCTION_ROADMAP.md`](./docs/PRODUCTION_ROADMAP.md) |
| สเปกฟีเจอร์หลัก: ลิงก์ vs การเรียน, รีวิวคำตอบ | [`docs/CORE_FEATURE_SPEC.md`](./docs/CORE_FEATURE_SPEC.md) |

## Current Architecture

ระบบเป็น monorepo แยกส่วนชัดเจน:

- `frontend/` — Next.js 15 + React 19 + TypeScript + Tailwind v4 + shadcn/ui (Base UI, style `base-nova`)
- `backend/` — ASP.NET Core .NET 10 + EF Core/PostgreSQL + SignalR
- Frontend ติดต่อ backend ผ่าน `frontend/src/lib/api-client.ts` และ
  `frontend/src/hooks/use-session-chat.ts`
- ไม่มี Next.js Route Handlers, Supabase repository หรือ Mock providers ในระบบปัจจุบัน
- เนื้อหาหลักต่อบทเรียนเลือกได้ระหว่าง Google Slides กับ PDF
- RAG ใช้ Pinecone ร่วมกับ Gemini หรือ OpenAI-compatible embeddings/answering

## Architecture Rules

1. Browser ห้ามเรียก database หรือ external AI provider โดยตรง ทุก request ผ่าน .NET API
2. Credentials อยู่ใน `backend/src/SupportRoom.Api/.env` หรือ deployment secrets เท่านั้น
3. Tutor engine ใน `frontend/src/tutor/` ต้องเป็น pure reducer; browser APIs อยู่ใน hooks
4. Business orchestration อยู่ใน `SupportRoom.Application`; controllers ต้องบาง
5. Entities/contracts อยู่ใน `SupportRoom.Domain`; provider implementation อยู่ใน
   `SupportRoom.Providers.*`
6. Database schema เปลี่ยนผ่าน EF Core migration ใหม่ ห้ามแก้ migration ที่ deploy แล้ว
7. Frontend/backend wire contract ใช้ camelCase และต้องอัปเดต TypeScript types กับ ViewModels คู่กัน
8. ห้าม persist สำเนา Google Slides/PDF teaching content ลง LessonConfig; เก็บเฉพาะ metadata

## UI Component System (frontend)

`frontend/` คือ frontend จริงเพียงจุดเดียวของระบบ — อย่าสร้างหรือปฏิบัติกับโปรเจกต์ Next.js
อื่นใดในระดับ repo root ว่าเป็นแอปจริง

- Component base: **Base UI** (`@base-ui/react`), style preset: **Nova** (`base-nova` ใน
  `frontend/components.json`) — ให้ Nova อยู่ในแนวเดียวกับ Figma design system ที่ทีมออกแบบเลือก
  เมื่อมีการอัปเดต preset
- `frontend/src/components/ui/` สงวนไว้สำหรับ shadcn primitives เท่านั้น (ไฟล์ที่ shadcn CLI
  generate) ห้ามใส่ business logic หรือโค้ดเฉพาะ SupportRoom ที่นี่ และห้ามเขียน primitive เอง
  ถ้า shadcn มีให้แล้ว
  - ตั้งชื่อไฟล์ใน `ui/` เป็น **lowercase** ตาม convention ของ shadcn เสมอ — ไฟล์ที่ shadcn
    generate ใหม่จะ import ข้ามกันด้วย path ตัวเล็ก และ Windows (case-insensitive) จะไม่เตือน
    แต่ Linux production server จะพังทันที
  - ก่อนเพิ่ม component ใหม่ ให้เช็ค `frontend/.agents/skills/shadcn/SKILL.md` และ
    `frontend/src/components/ui/` ก่อนเสมอ — ใช้ของเดิมถ้ามีแล้ว เพิ่มเฉพาะที่ระบบต้องใช้จริง
    (ห้ามติดตั้งไว้ล่วงหน้าโดยยังไม่มีจุดใช้งาน)
- Component เฉพาะโดเมนอยู่ใน `frontend/src/components/meeting/` และ `.../admin/`;
  composition ที่ใช้ซ้ำข้ามโดเมนอยู่ใน `.../shared/` — ทั้งสามแยกจาก `ui/` ชัดเจน
- **ใช้ shadcn semantic tokens เท่านั้น** (`bg-background`, `text-muted-foreground`, `bg-primary`,
  `border`, `bg-muted`, `text-destructive` ฯลฯ) ธีม "room-*" เดิมถูกลบออกทั้งหมดแล้ว พร้อมกับ
  `tailwind.config.*` — Tailwind v4 กำหนดธีมผ่าน CSS ล้วนใน `src/app/globals.css`
  - `@theme inline` ต้องอยู่ **top level** ของ `globals.css` เท่านั้น ห้ามย้ายไปไว้ใน `@layer base`
    เพราะจะทำให้ค่า dark mode ไม่ถูกส่งต่อไปยัง utility ที่ generate ออกมา
  - ค่าที่จงใจต่างจาก Nova default (แก้ที่ `:root`/`.dark` ใน `globals.css` เท่านั้น):
    - `--primary` — สีส้มแบรนด์ School Bright (`#f97316` ตาม Figma redesign, ปรับ 2026-08-25
      จากเฉดแดง-ส้มเข้มเดิม)
    - `--sidebar-accent`/`--sidebar-accent-foreground` — sidebar item ที่ active/hover เป็น
      pill สีส้มทึบ (`#e86a27`) ตัวหนังสือขาว ไม่ใช่ neutral highlight ของ Nova
    - `--tree-subcategory`/`--tree-subcategory-foreground`, `--tree-header` — สี accent เฉพาะ
      หน้า tree หมวดบทเรียน (`CategoryTree.tsx`): แถบพีชตอนหมวดย่อยขยาย, แถบครีมที่หัวตาราง
    - `--info`/`--info-foreground` — ปุ่มฟ้า (เช่น "+ เพิ่มบทเรียน" ใน `CategoryTree.tsx`)
      ที่ Figma แยกสีจาก primary orange
    - `--background` — เปลี่ยนเป็น `#fcfcfc` (`oklch(0.9911 0 0)`) ตาม Figma redesign, ปรับ
      2026-08-25 จาก `#fafafa` เดิม — สีพื้นหลังทั้งหน้า ยังจงใจต่างจาก `--card` เล็กน้อยเพื่อให้
      การ์ดยังอ่านออกว่าลอยอยู่เหนือพื้นหลัง
    - `--card` — เปลี่ยนเป็นสีขาวล้วน (`oklch(1 0 0)`) ตาม Figma redesign, ปรับ 2026-08-25 จากสีเทา
      อมม่วง (`oklch(0.9173 0.0067 286.266)`) เดิม — พื้นหลังการ์ด/panel ทั่วทั้งแอป (`ui/card.tsx`)
      ให้ตรงกับ `--popover` ที่เป็นสีขาวล้วนอยู่แล้ว
    - ถ้าจะกลับไปใช้ Nova neutral ล้วน ต้องแก้ทั้ง 7 จุดนี้ ไม่ใช่แค่ `--primary` อีกต่อไป
  - ยังไม่มี theme switcher ในระบบ (ไม่ได้ติดตั้ง `next-themes`) — token ฝั่ง `.dark`
    ประกาศไว้ครบและถูกต้องแล้ว แต่ยังไม่มีอะไร toggle คลาส `dark` ในการใช้งานจริง
- ลิงก์ที่หน้าตาเป็นปุ่มให้ใช้ `<Link className={buttonVariants({...})}>` ไม่ใช่
  `<Button render={<Link/>}>` — Base UI Button คาดหวัง `<button>` จริง การ render เป็น `<a>`
  จะทิ้ง native button semantics และขึ้น error ที่ console
- ใช้ shadcn skill (`frontend/.agents/skills/shadcn/SKILL.md`) เป็นแนวทางหลักเวลาทำงานกับ UI —
  รวม critical rules เรื่อง styling, forms, composition, icons

## Folder Map

```text
frontend/src/app/                    Next.js pages
frontend/src/components/             UI, admin, meeting room
frontend/src/hooks/                  media, tutor orchestration, SignalR
frontend/src/lib/api-client.ts       REST client จุดเดียว
frontend/src/tutor/                  pure state machine
frontend/src/types/                  frontend wire/domain types

backend/src/SupportRoom.Api/         controllers, hub, startup, hosted queue
backend/src/SupportRoom.Application/ services, DTOs, ViewModels
backend/src/SupportRoom.Domain/      entities, constants, environment readers
backend/src/SupportRoom.Infrastructure/ CORS, error handling
backend/src/SupportRoom.Providers.Data/ EF Core, repositories, migrations
backend/src/SupportRoom.Providers.Slides/ Google Slides + PDF rendering
backend/src/SupportRoom.Providers.Tts/ Edge TTS
backend/src/SupportRoom.Providers.VoiceQuestion/ Gemini/OpenAI RAG flows
backend/src/SupportRoom.Providers.Knowledge/ embeddings + Pinecone
backend/src/SupportRoom.Providers.DocumentParsing/ PDF/PPTX/DOCX/XLSX extraction
backend/src/SupportRoom.Providers.Storage/ local/Huawei OBS
backend/tests/                        xUnit test projects
```

## Provider Configuration

ทุกหมวดต้องกำหนดค่า ไม่มี Mock fallback:

| หมวด | ค่าที่รองรับ |
|---|---|
| `SLIDES_PROVIDER` | `google` |
| `TTS_PROVIDER` | `edge` |
| `VOICE_QUESTION_PROVIDER` | `gemini`, `gemini-rag`, `openai-rag` |
| `KNOWLEDGE_PROVIDER` | `pinecone`, `pinecone-openai` |
| `DOCUMENT_STORAGE_PROVIDER` | `local`, `huawei-obs` |

ดูค่าทั้งหมดใน `backend/src/SupportRoom.Api/.env.example`

## Commands

```powershell
# Frontend
cd frontend
npm install
npm run lint
npm run typecheck
npm run test
npm run build

# Backend
cd ../backend
dotnet restore SupportRoom.slnx
dotnet build SupportRoom.slnx
dotnet test SupportRoom.slnx --filter "Category!=Integration"   # ปกติใช้ตัวนี้
dotnet test SupportRoom.slnx                                    # รวม test ที่ยิง provider จริง (ต้องมี .env)
dotnet run --project src/SupportRoom.Api
```

Apply database migration:

```powershell
dotnet ef database update --project src/SupportRoom.Providers.Data --startup-project src/SupportRoom.Api
```

## Files to Read Before Changes

| งาน | อ่านก่อน |
|---|---|
| Tutor state machine | `frontend/docs/STATE_MACHINE.md`, reducer tests |
| REST/SignalR contract | `frontend/docs/API_CONTRACT.md`, controllers/hub |
| Database schema | `backend/docs/ER_DIAGRAM_AND_WORKFLOW.md`, EF migrations |
| Provider/environment | `frontend/docs/ENVIRONMENT_SETUP.md`, `.env.example`, provider factory |
| RAG/document flow | `frontend/docs/SYSTEM_LOGIC.md`, Knowledge/VoiceQuestion services |
| Backend layering convention | `backend/.claude/skills/dotnet-layered-backend/SKILL.md` |

## Existing Integrations

| Integration | ใช้ทำอะไร | ถ้าล้มเหลว |
|---|---|---|
| Google Slides API | ดึง speaker notes + slide URL (service account) | save: log warning ไม่ block; เปิดห้อง: 502 |
| Gemini | ถอดเสียงทุก provider + embeddings + ตอบ (`gemini`, `gemini-rag`) | `transcription_failed` / fallback full-deck |
| OpenAI-compatible | ตอบ (`openai-rag`) + embeddings (`pinecone-openai`) รองรับ gateway อื่นผ่าน `OPENAI_BASE_URL` | `transcription_failed` |
| Pinecone | vector index, namespace = lessonSlug หรือ `kb-global` | log warning → fallback full-deck |
| Edge TTS | สังเคราะห์เสียงไทย (WebSocket ไม่ต้องมี key) | ข้าม chunk ที่พัง; frontend เดินหน้าต่อแบบไม่มีเสียง |
| local disk / Huawei OBS | เก็บไฟล์เอกสาร (ผ่าน AWS S3 SDK) | 500 ตอน upload / 404 ตอน download |

⚠️ เสียงคำถามของคุณครูถูกส่งไป Gemini และ transcript อาจถูกส่งไป OpenAI/gateway เมื่อใช้ `openai-rag`

## Feature Development Pattern

```
Entity (Domain) → Migration (Providers.Data) → Repository + ลงทะเบียนใน UnitOfWork.Register
  → Service interface+impl ในไฟล์เดียว (Application) → DTO + ViewModel
  → ลงทะเบียนใน ServiceConfiguration → Controller (บาง)
  → types/domain.ts → api-client.ts → hook → component
```

ต่อ external service ใหม่: สร้าง interface + factory ใน `SupportRoom.Providers.*`,
เพิ่มค่าที่รับได้ใน `ProviderSelection.cs`, เพิ่ม env ใน `.env.example`

Convention ที่ไม่ปกติแต่จงใจ — ทำตาม อย่าเปลี่ยนเอง:
- ไฟล์ service ชื่อ `IXxxService.cs` บรรจุทั้ง interface และ implementation
- status ใช้ `static class` + `const string` ไม่ใช่ C# enum (ให้ตรงกับ TS union type)
- ข้อความที่ผู้ใช้เห็นเป็นภาษาไทย รวมถึง exception message
- service ห้ามพัง flow หลักเพราะ integration รอง — log warning + degrade แทน throw
- คอมเมนต์อธิบาย "ทำไม" ไม่ใช่ "ทำอะไร" — codebase นี้บันทึกเหตุการณ์ที่เคยพังจริงไว้ รักษาระดับนี้

## Solution Design Rule

ก่อนลงมือฟีเจอร์ที่ไม่ trivial:

1. เข้าใจความต้องการจริง (ปัญหาของผู้ใช้ ไม่ใช่วิธีแก้ที่เขาเสนอมา)
2. ไล่ flow เดิมที่เกี่ยวข้องให้จบ
3. หาโค้ด/service/endpoint ที่ใช้ซ้ำได้ — provider abstraction ที่นี่ดีอยู่แล้ว
4. ถามว่ามี library/API/บริการที่แก้ปัญหานี้แล้วหรือยัง
5. เทียบทางเลือก 2–4 แบบพร้อม trade-off
6. เลือกสถาปัตยกรรมที่ง่ายที่สุดที่ตอบความต้องการ *ปัจจุบัน*
7. อย่าสร้าง infrastructure ล่วงหน้า
8. บันทึกการตัดสินใจสำคัญลง `docs/TECH_DECISIONS.md`
9. ลงมือเมื่อทิศทางชัดแล้ว
10. ตรวจด้วย lint/typecheck/test/build ทั้งสองฝั่ง แล้วอ่าน `git diff` ก่อนส่ง

## Definition of Done

- Frontend lint, typecheck, tests และ build ผ่าน
- Backend build/test ผ่านโดยแยก unit tests จาก tests ที่ต้องใช้ provider จริง
- ไม่มี secret หรือ transcript/answer เต็มใน source/log
- API DTO/ViewModel และ TypeScript types ตรงกัน
- Schema change มี migration ใหม่
- เอกสารที่เกี่ยวข้องอัปเดตตามโค้ดจริง

## Known Baseline Issues

เรียงตามความเร่งด่วน — รายละเอียดและทางเลือกอยู่ใน `docs/PROJECT_CONTEXT.md` §19 และ
`docs/TECH_DECISIONS.md`

- **Edge TTS ไม่เหมาะกับ production** — Microsoft เริ่มบล็อกการเรียก Read-Aloud แบบไม่เป็นทางการ
  (ธ.ค. 2025) และกรอง IP ของ datacenter; deploy บน cloud มีโอกาสสูงที่เสียงจะเงียบทั้งระบบ (TD-001)
- มี JWT auth/RBAC หลังบ้านแล้ว แต่ยังไม่มี rate limiting/abuse controls (TD-002 ทำเพียงบางส่วน)
- ไม่มี CI (`.github/workflows/` ว่างเปล่า) และไม่มี Dockerfile/deployment artifact (TD-006)
- Background indexing queue เป็น in-memory — restart แล้วงานค้างที่ `pending` ตลอดไป (TD-003)
- Document deletion ยังทิ้ง vectors ไว้ใน Pinecone (TD-004 — chunk id เป็น `{documentId}-{chunkId}`
  อยู่แล้ว ลบด้วย ID prefix ได้; serverless ไม่รองรับ delete by metadata filter)
- migration `20260813140603_SplitLinkAndAddAuth` สร้างรวม TD-013/TD-014 แล้ว แต่ยังไม่เคย apply
  กับ PostgreSQL จริง ต้อง rehearsal บน staging และตรวจ backfill/rollback ก่อน deploy
- API integration test project ยังไม่มี test ที่ยืนยัน endpoint จริง (`UnitTest1.cs` ยังเป็น template)
- EF Core Relational conflict แก้แล้วโดย pin 10.0.10; ยังมี PackageReference แบบ floating ที่ควรตรึงก่อน CI
- `IsDelete`/`DeletedAt` มีในทุก entity แต่โค้ดลบจริงทุกครั้ง ไม่มี global query filter
- Frontend มี dependency ตกค้างที่ไม่มีโค้ดเรียกใช้: `googleapis`, `msedge-tts`, `zod`,
  `client-only`, `bufferutil`, `utf-8-validate`
- `PackageReference` แบบ floating (`3.*`, `0.*`, `5.*`) ทำให้ build ไม่ deterministic

<!-- agentclaude-pipeline:start -->

## Agent pipeline (merged from AgentClaude)

This repo defines a fixed, hand-off-based agent pipeline for building a project from a vague idea through to verified, security-reviewed, deployed code. Each stage is a subagent under `.claude/agents/`, each owns exactly one artifact, and **no agent ever invokes the next one** — structurally true in every mode, since none of them holds the `Agent` tool. By default the user decides every handoff explicitly; an opt-in autonomous mode lets the session chain them instead, but five points (requirement interview, schema confirmation, a failed QA round, a Critical/Important security finding, an actual deploy/migration) always wait for a person regardless. **`qa-engineer` and `security` are never auto-chained in any mode — they run only when the user explicitly asks for them, every time.** See "Rules that hold across every agent" below.

## Read this first

`.claude/shared/conventions.md` is the authoritative source for the rules every agent shares: module-folder resolution, the `_docs/status.md` index, dates, amend discipline, version control, handoffs, the design-as-contract rule, and where the stack is defined. The agent files deliberately don't repeat those rules — they point at that file, so changing a rule means editing one place, not nine.

## The pipeline

```
setup (once per project)
   ↓
business-analyst → system-analyst → project-manager → frontend-engineer / backend-engineer
                                                                  ↓
                                                            qa-engineer
                                                  ↓            ↓            ↓
                                       implementation bug   schema gap   business gap
                                                  ↓            ↓            ↓
                                    frontend/backend-engineer  system-analyst  business-analyst
                                                                  ↓
                                                security (sensitive phases) → devops
```

| Agent | Owns | Reads | Writes |
|---|---|---|---|
| `setup` | project skeleton | `design.md` (optional), stack files | scaffolding, `schema.prisma`, `.env`, `.gitignore` |
| `business-analyst` | business requirements | `review.md`, `design.md`, `requirement.md` (amend) | `requirement.md` |
| `system-analyst` | feasibility + data model | `requirement.md`, `review.md`, stack files | `design.md` |
| `project-manager` | phased task list | `design.md`, `requirement.md`, stack files | `plan.md` |
| `frontend-engineer` | UI code | `plan.md`, `design.md`, `requirement.md`, `review.md` | app code |
| `backend-engineer` | API/DB code | `plan.md`, `design.md`, `requirement.md`, `review.md` | app code |
| `qa-engineer` | verification | all four docs + `schema.prisma` + real code | `review.md`, `review/phase-N.md`, `[x]` and add-only `🔒 Security gate` in `plan.md` |
| `security` | security audit | `requirement.md`, `design.md`, `review.md`, `schema.prisma`, real code | `security.md` |
| `devops` | deploy, CI, migrations | `status.md`, `review.md`, `security.md`, `plan.md`, `design.md`, `schema.prisma`, stack files | `deploy.md`, infra files |

Every agent also reads `_docs/status.md` when it starts and updates its own lines when it finishes (`conventions.md` §2) — that's left out of the table above rather than repeated on all nine rows.

`setup` runs once per project, before Phase 1. Everything after that loops per phase.

## Where things live

```
_docs/
├── status.md                    ← the index: what exists, how far it's got, who's next
└── module/
    └── sales-crm/
        ├── requirement.md       ← business-analyst
        ├── design.md            ← system-analyst
        ├── plan.md              ← project-manager  (checkboxes + added security gates: qa-engineer)
        ├── review.md            ← qa-engineer  (open issues + current round + unverified behaviour)
        ├── review/
        │   └── phase-N.md       ← qa-engineer  (archived rounds — read on demand only)
        ├── security.md          ← security
        └── deploy.md            ← devops

.claude/
├── shared/conventions.md        ← rules every agent follows
├── agents/*.md                  ← the nine agents
├── hooks/
│   ├── block-git.js              ← PreToolUse guard enforcing the no-git rule
│   └── block-outside-repo.js     ← PreToolUse guard keeping every write inside the repo root
└── settings.json                ← wires both hooks up (checked in, applies to everyone)
```

No *document* is written at the repo root — every module doc lives under `_docs/module/<name>/`. (Project files that belong at the root by convention are a different thing: `setup` writes `package.json`, `.env`, `.env.example`, and `.gitignore` there, and `devops` writes infra files.) Every doc agent resolves its module folder first: one folder → use it; several → ask the user; none → send them back to `business-analyst`.

A **module folder** is a delivery unit with its own doc set and phase numbering; the **Modules** inside `design.md` are feature groupings within one such unit. The test is whether the work would get its own business interview — if it's the same product being built out, it's one folder with several Modules, however large. Splitting folders is not a way to manage size. `conventions.md` §1 has the full rule.

## Rules that hold across every agent

Full text in `.claude/shared/conventions.md`; the short version:

- **No agent chains to the next — structurally, none of the nine has the `Agent` tool.** By default (manual mode) each finishes by saying what's ready and who should get it, then the user decides. When the user explicitly asks for a continuous/unattended run ("รันข้ามคืนได้เลย"), the session orchestrating the pipeline may chain the handoffs itself, opt-in per run — but five points always stop and wait for a person regardless of mode: `business-analyst` any time it runs, `system-analyst`'s schema confirmation, `qa-engineer` on any ⚠️/❌ result, `security` on any 🔴/🟠 finding, and `devops` before an actual deploy/migration. **`qa-engineer` and `security` are further exempt from auto-chaining altogether, in every mode** — the pipeline never invokes them on its own just because an engineer or a QA round finished; the user must ask for them by name every time. `.claude/shared/conventions.md` §6 has the full rule.
- **No git, ever.** No agent runs git or touches `.git`. `setup`/`devops` may *write* a `.gitignore` or CI file — that's writing a file, not running git. This is enforced by a `PreToolUse` hook (`.claude/hooks/block-git.js`), not left to the prompt: state-changing git commands are blocked at the tool call, read-only ones (`status`/`log`/`diff`/`show`) still run.
- **No agent writes outside this repo.** Every write resolves under the project root, whatever the reason. Enforced by a second `PreToolUse` hook (`.claude/hooks/block-outside-repo.js`) on `Write`/`Edit`/`MultiEdit`/`NotebookEdit` — the one exception is Claude Code's own scratchpad convention under the OS temp dir, which isn't an agent going off scope.
- **`design.md`'s Data Model is the contract.** `backend-engineer` implements it verbatim, `frontend-engineer` derives types from it, `qa-engineer` fails any drift. A gap goes back to `system-analyst`, never gets improvised. Once `setup` has written the real `schema.prisma`, the engineers work from that file — it's the contract's working copy and the one their queries must agree with — and `qa-engineer` is the agent that reads both and keeps them equal. If they ever disagree, `design.md` wins and the code is wrong. Only `setup` (at scaffold) and `backend-engineer` (propagating a confirmed amendment) ever write `schema.prisma`. **The comparison is scoped per module**: every model in a module's Data Model must exist in `schema.prisma` and match, but a model `schema.prisma` has and this `design.md` doesn't may belong to another module — `Grep` `model <Name>` across `_docs/module/*/design.md` before calling it drift. Only a model no module declares is an improvised change.
- **Only `qa-engineer` marks tasks done.** It sets `[x]` in `plan.md` after inspecting real code; nobody else touches a checkbox.
- **Amend, don't regenerate.** Existing docs are updated with `Edit`, section by section, with a dated line appended to their `## Change Log`. Never a full rewrite.
- **`review.md` stays small.** It holds `Open Issues — all phases`, the current verify round, and `Unverified Behaviour` for phases that haven't deployed yet; `qa-engineer` moves closed rounds verbatim into `review/phase-N.md`. Those first and third sections outlive their round on purpose — a later stage reads them after the round that produced them stopped being current. Every engineer/`security`/`devops` run reads `review.md` in full, so closed-phase detail left in it is a tax on the whole pipeline. Nobody opens an archive file as part of normal startup.
- **Dates come from the user.** No agent can reliably know today's date, so any agent writing a dated entry asks first and reuses that answer for the session.
- **Engineers never decide a rule — they implement or they stop.** Neither engineer has `AskUserQuestion`, deliberately: a rule settled in a chat with an engineer never reaches `requirement.md` or `design.md`, so the next phase and the next session don't inherit it. Unclear logic goes back to `system-analyst` (which routes on to `business-analyst` if it's a business question), and `design.md`'s contract sections carry the bar — an engineer must never have to decide. Anything not covered is either written into a contract section or listed as explicitly out of scope; leaving it unmentioned is neither.
- **Verify against real state, not memory.** A recalled fact from an earlier turn, a summary, or "I remember this does X" is a hypothesis, not a fact — read the actual current file/schema/code before stating or acting on it. If it disagrees with what's recalled, the file/code wins and the stale belief is corrected on the spot. `.claude/shared/conventions.md` §12 has the full rule.
- **`status.md` is an index, not a truth.** If it disagrees with the docs or the code, the docs and code win. It's also where an agent looks up which phase is in play, instead of scanning `plan.md` to work it out, and where `qa-engineer` stamps each phase's verify mode — `(FULL)` / `(TARGETED)` — for `devops` to gate on.
- **Read the section, not the file.** Every agent starts from a fresh context, so a whole-file read is a cost paid again on every run. `plan.md` → Plan Summary + your phase + Sequencing Notes + Open Questions. `design.md` → always Feature-by-Feature Feasibility, Risks, and Open Questions (they carry the confirmed decisions and the "don't implement this" list), plus your phase's contract section and your own module's entry. `conventions.md` §10 has the procedure. Exceptions by design: `project-manager` owns `plan.md`, `system-analyst` owns `design.md`, and `qa-engineer` reads the Data Model in full every round.
- **QA runs in one of two modes, and says which.** FULL covers every task in the phase and is the only mode that closes one; TARGETED re-checks named fixes plus their blast radius, the shared-code watchlist, the whole-project typecheck/lint/build, and the full schema contract. TARGETED is allowed only after a FULL round left a file manifest to compare against, and it must state what it didn't cover. `.claude/agents/qa-engineer.md` has the rules.
- **Nothing ships unverified.** `devops` refuses to deploy a phase `qa-engineer` hasn't accepted, one whose most recent round was TARGETED, one marked `🔒 Security gate` that `security` never audited, or one with unresolved Critical/Important security findings, without an explicit user override. `security` isn't gated on the mode — it audits the code independently.
- **Only `security` closes a `security` finding.** Each finding carries a `Status` — 🔵 Open, 🟣 Fix claimed, ✅ Fixed (re-audited), ⚪ Accepted. An engineer's fix moves it to 🟣 and no further; `qa-engineer`'s pass is functional and says so itself, so it cannot close one. `devops` blocks on 🔵 and 🟣 alike.
- **No test suite means nothing ever executes the logic.** Tests are opt-in and default to none, so `qa-engineer` verifies by reading code plus `typecheck`/`lint`/`build` — which cannot tell a right answer from a wrong one. When there's no suite, QA lists the specific rules it could only read under `## Unverified Behaviour — undeployed phases`, and `devops` puts that list in front of the user before deploying.
- **Sensitive phases are flagged in writing, not remembered.** `project-manager` marks any phase touching auth, personal data, payments, uploads, or untrusted input as `## Phase N: <name> 🔒 Security gate`; `qa-engineer` can add one PM didn't foresee — writing it into the phase heading itself (its one non-checkbox write to `plan.md`, add-only) as well as listing it in `review.md`; `devops` gates on it. Nobody removes a flag except the user.
- **An unsourced number is an assumption, in writing.** `business-analyst` has no web access by design; external facts come from the user and land in `requirement.md`'s `## References` table with their source. Anything used as a fact without a row there is written `(สมมติฐาน — ยังไม่ยืนยัน)`, and `system-analyst` must resolve it with the user before designing around it instead of promoting it to fact by using it.
- **A fix that fails twice gets escalated, not re-sent.** After the second failed re-check of the same item, `qa-engineer` stops routing it back and hands it to the user — an item that survives two fixes is usually misrouted (a design or business question), not badly implemented.

## Right-size the pipeline — don't run all of it for small work

The full chain is for building something new. Running nine stages for a copy fix is waste, not diligence. Pick the entry point by the size of the change:

| The work is | Start at | Skip |
|---|---|---|
| Copy/styling tweak, or a bug where requirement + schema are already clear | `frontend-engineer` / `backend-engineer` → `qa-engineer` | BA, SA, PM |
| Adds or alters a field/table/relation | `system-analyst` (amend) → engineer → `qa-engineer` | BA, PM |
| Changes a business rule, no schema impact | `business-analyst` (amend) → `system-analyst` (amend) → engineer → `qa-engineer` | PM |
| A new feature, module, or project | `business-analyst`, full chain | nothing |

`project-manager` only earns its run when there's enough work to need phasing. One or two tasks go straight to an engineer.

But **don't skip a stage the change actually needs** — a schema change that bypasses `system-analyst` is the exact failure this pipeline exists to prevent.

## Model and effort per agent

Set in each agent's frontmatter. The split puts the expensive model where a mistake propagates furthest, and the cheap one where the volume is:

| Agent | `model` | `effort` | Why |
|---|---|---|---|
| `setup` | sonnet | low | mechanical, runs once per project |
| `business-analyst` | opus | medium | short output, but an error here contaminates everything downstream |
| `system-analyst` | opus | high | hardest reasoning in the chain; a wrong schema is the costliest mistake available |
| `project-manager` | sonnet | medium | decomposition from an already-confirmed design |
| `frontend-engineer` | sonnet | medium | highest volume, highest output — where the savings actually are |
| `backend-engineer` | sonnet | medium | same |
| `qa-engineer` | sonnet | high | comparison work, so `effort: high` buys more here than the tier does — but note this is the highest-leverage cost decision in the table: with tests opt-in and usually absent, this agent is the *only* correctness guarantee in the chain and nothing re-checks it. `opus` is the upgrade to reach for first if verification starts missing things |
| `security` | opus | high | adversarial reasoning; what it misses, nobody catches |
| `devops` | sonnet | medium | little reasoning, high stakes — guarded by confirmation rules instead |

To change one, edit that agent's frontmatter. `inherit` follows the session's `/model`.

## Fixed stack (summary — the two engineer files are authoritative)

- **Frontend**: Next.js App Router · TypeScript · Tailwind · Zustand
- **Backend**: Node + Express · PostgreSQL · Prisma · REST · hand-rolled JWT · Zod
- **Package manager**: npm
- **Tests**: opt-in — `setup` offers Vitest once and defaults to none. `qa-engineer` runs every check that exists (`typecheck`/`lint`/`build`/`test`) and must state in `review.md` when there are no automated tests, so a ✅ is never mistaken for a tested ✅

Changing the stack means the user confirms it and `frontend-engineer.md`/`backend-engineer.md` get updated in place. Every other agent reads those two files rather than keeping its own copy.

## Coming back to a project

Read `_docs/status.md` first — it says which modules exist, how far each has got, and which agent should pick it up. Then open that module's docs in order: `requirement.md` → `design.md` → `plan.md` (unchecked boxes = remaining work) → `review.md` → `security.md` → `deploy.md`.

<!-- agentclaude-pipeline:end -->
