# PROVIDER / API KEY SETTINGS — Product + Technical Spec Draft

> สถานะ: **Proposed / ยังไม่ implement** ต้องผ่าน D-01 ถึง D-05 ใน `HANDOFF_MASTER.md`
> หน้า settings เป็น system-wide และ `owner` เท่านั้นตาม TD-014

> **2026-08-21 — ความสนใจล่าสุดจากเจ้าของโปรเจกต์:** หลังเพิ่ม ElevenLabs เป็น TTS provider ตัวที่สอง
> (แก้ TD-001) เจ้าของโปรเจกต์ถามถึงการสลับ provider (เริ่มจาก TTS: edge ↔ elevenlabs) ผ่านหน้าเว็บ
> "ในอนาคตอันใกล้" — ยังไม่ได้เรียก `business-analyst` มาตอบ D-01..D-05 จริง แค่บันทึกไว้ว่ามีความ
> สนใจเป็นรูปธรรมแล้ว ไม่ใช่แค่แนวคิดลอยๆ ในเอกสารเดิม — ตอนนี้สลับ TTS provider ทำได้แล้วผ่าน
> `TTS_PROVIDER` ใน `.env` + restart backend (ไม่มีหน้าเว็บ) ตามที่ตกลงกันไว้ชั่วคราว

## ปัญหาที่ต้องแก้

ระบบปัจจุบันเลือก provider ด้วย environment variables ตอน process startup และ key อยู่ `.env`/
deployment secrets การทำหน้า “เปลี่ยน API key” จึงไม่ใช่แค่เพิ่ม input: ต้องมี secret storage,
validation, activation, rollback, audit, config versioning และกติกาว่าเปลี่ยนทั้งระบบหรือรายบริษัท

## Capability model ที่ควรใช้

อย่าผูก UI กับ vendor อย่าง `Gemini`/`ElevenLabs` โดยตรง ให้แยกตามหน้าที่:

| Capability | ปัจจุบัน | Candidate/examples | Compatibility ที่ต้อง validate |
|---|---|---|---|
| Teaching content | Google Slides/PDF | partner content API | auth, slide/order/notes/media contract |
| STT / audio understanding | Gemini | partner/OpenAI-compatible/cloud speech | mime/language/latency/data region |
| Answer LLM | Gemini/OpenAI-compatible | partner model gateway | JSON schema, context, timeout, grounded behavior |
| Embedding | Gemini/OpenAI-compatible | partner embedding | dimensions/model/index space |
| Vector store | Pinecone | pgvector/partner | dimensions, metric, namespaces, delete/filter |
| TTS | Edge | Azure/Google/ElevenLabs/partner | Thai voice, rate, format, streaming, SLA |
| Object storage | local/Huawei OBS | S3-compatible/partner | region, encryption, lifecycle |

เหตุผล: provider หนึ่งอาจทำหลาย capability และ deployment หนึ่งอาจผสม provider ได้ การเก็บเป็น
profile+binding ทำให้เพิ่ม AI พันธมิตรโดยไม่ต้องออกแบบหน้าจอใหม่ทุกครั้ง

## Recommended information architecture

1. **Providers** — provider profiles, endpoint/region, credential status, last test, capabilities
2. **Runtime bindings** — active profile/model ต่อ capability
3. **Knowledge configuration** — chunk/embedding/vector/top-K/threshold และ reindex state
4. **Prompt & policy** — prompt version, grounded policy, response schema, moderation/PII policy
5. **Change history** — who/when/from/to/test result/activation/rollback
6. **Usage & health** — request count, latency, error, tokens/characters, estimated cost

## Safe change flow

```text
Create/Edit Draft
  → enter secret (never reveal existing value)
  → Test Connection
  → Compatibility Check
  → optional shadow/canary/eval
  → impact preview (global, reindex required, downtime/cost)
  → Activate with audit
  → monitor health
  → Roll back to previous version
```

### Compatibility rules

- เปลี่ยน embedding model/dimensions/vector store → สร้าง index version ใหม่และ reindex ก่อน switch
- เปลี่ยน answer model/prompt → รัน regression eval; ไม่จำเป็นต้อง reindex หาก embedding ไม่เปลี่ยน
- เปลี่ยน STT → ทดสอบ audio formats/Thai transcription accuracy (readiness ไม่ผ่าน STT อีกต่อไป
  ตั้งแต่มติ U1 2026-08-23 - ตอบได้ทางเดียวคือกดปุ่มในหน้าห้อง)
- เปลี่ยน TTS → voice preview, output format, max text, rate/streaming/fallback
- เปลี่ยน endpoint/key อย่างเดียว → connection test + canary; key เก่าอยู่ช่วง rollback สั้นตาม policy
- ห้ามเปิด config ที่ test ไม่ผ่าน หรือมี required secret ขาด

## Secret handling requirements

- Frontend รับ secret ใหม่ได้แต่ API ไม่คืน plaintext; แสดง masked fingerprint/last 4 เท่านั้น
- Encrypt at rest ด้วย KMS/Vault/managed secret store; encryption key ห้ามอยู่ DB เดียวกัน
- Key version + created/updated/tested/activated/rotated/revoked metadata
- Never log request body/header/secret; redact provider error ที่ echo credential/URL
- Owner only + step-up/re-auth สำหรับ reveal-free replace/activate/revoke
- Audit ทุก change; รองรับ rotation without downtime
- `.env` เหมาะ bootstrap/local dev เท่านั้น ไม่ใช่ backend ของ settings UI

## Proposed data model (conceptual, not migration-ready)

```text
ProviderProfile
  Id, Name, ProviderType, BaseUrl, Region, Capabilities[], IsEnabled
  NonSecretConfigJson, SecretReference, CreatedBy/At, UpdatedBy/At

RuntimeConfigVersion
  Id, Status(draft|testing|active|failed|retired), Version, CreatedBy/At, ActivatedBy/At
  BindingsJson, PromptVersionId, KnowledgeConfigJson, PreviousVersionId

ProviderTestRun
  Id, ProfileId, ConfigVersionId, Capability, Status, LatencyMs, SafeError, CreatedBy/At

ConfigurationAuditEvent
  Id, ActorId, Action, TargetId, BeforeFingerprint, AfterFingerprint, CreatedAt
```

อย่าเก็บ key ใน `NonSecretConfigJson`; `SecretReference` ต้องชี้ managed secret store

## UX states/permissions

- Owner เท่านั้นเห็น menu/route/API; admin/cs ได้ 403 และไม่เห็น metadata provider
- Empty/bootstrap, configured/untested, testing, valid, invalid, rate-limited, degraded, activating,
  active, rollback available, reindex required/in progress/failed
- Confirm ต้องบอก affected companies, expected cost/downtime, reindex requirement และ fallback

## Release slices

1. Read-only config/health จาก env + owner gate
2. Provider profiles + managed secret reference + connection test
3. Versioned draft/activate/rollback สำหรับ TTS/answer model
4. Embedding/vector changes พร้อม versioned index + reindex/cutover
5. Metrics/cost/budget/canary และ optional BYOK เมื่อ product ตัดสินใจจริง

