# KNOWLEDGE / GROUNDED ANSWER ROADMAP

## North star

ระบบต้องตอบจากหลักฐานของบริษัท/บทเรียนที่ถูกต้อง, อ้างอิงกลับได้, ปฏิเสธอย่างเหมาะสมเมื่อไม่มี
ข้อมูล และทำให้ทีมรู้ว่า “ผิดเพราะข้อมูล, retrieval หรือ generation” เพื่อแก้ถูกชั้น

## Pipeline ปัจจุบัน

```text
Sources: Google notes | PDF lesson | global/lesson documents
  → extract/chunk
  → embed (Gemini/OpenAI-compatible)
  → Pinecone namespace company:lesson + company:kb-global
  → query both → merge top-K → threshold
  → answer with Gemini/OpenAI-compatible grounded prompt
  → SessionQuestion(answerStatus, transcript, answer, related slide)
  → CS review(correct/incorrect + free note)
```

มี fallback: retrieval fail/ไม่เคย index → full deck; index มีแต่ score ต่ำ → `not_found`

## สิ่งที่ “มีแล้ว” กับ “ยังไม่มี”

| Area | มีแล้ว | ยังไม่มี/ความเสี่ยง |
|---|---|---|
| Source scopes | lesson + company global | source ownership/version/effective date |
| File parsing | PDF/PPTX/DOCX/XLSX | quality preview, OCR, duplicate detection, metadata taxonomy |
| Indexing | async in-memory queue, status | durable retry, job visibility, stale/delete consistency |
| Retrieval | merge 2 namespaces, top-K, threshold | hybrid search, rerank, filters, citation contract |
| Generation | grounded prompt + statuses | prompt registry/version, schema-constrained response, guardrail tests |
| Feedback | correct/incorrect + note | reason taxonomy, resolved workflow, link feedback→source/chunk/config |
| Evaluation | logs/test pieces | dataset, baseline, automated regression, human rubric |
| Observability | basic logs without content | per-stage latency, cost/tokens, retrieval trace, quality dashboard |

## Recommended milestones

### K0 — Define quality contract (ก่อนปรับ algorithm)

- กำหนด metrics: retrieval recall@K, citation accuracy, grounded answer accuracy,
  not-found precision/recall, out-of-scope accuracy, transcription success, p50/p95 latency, cost/question
- สร้างชุดคำถาม 30–50 ข้อแรก ครอบคลุม answerable, absent, ambiguous, adversarial, cross-company
- ระบุ expected source/chunk/status และ human rubric
- เก็บ provider/model/embedding/prompt/config version ต่อผลทดสอบและคำถามจริง

### K1 — Reliable ingestion

- Durable job + idempotency + retry/dead-letter/startup recovery
- Content hash/dedup/version และ last successful index
- ลบ source → ลบ vectors; reindex แบบ versioned ไม่ปน embedding space
- Admin preview extracted chunks + indexing failure reason ที่ปลอดภัย

### K2 — Retrieval quality

- Metadata: company, lesson, document, page/slide, source version, updated time
- Tune chunking ตามชนิดเอกสาร; query normalization; optional hybrid/rerank เมื่อ baseline ชี้ว่าจำเป็น
- Source/citation ID เป็น contract จริง ไม่ใช้ `relatedSlideObjectId` กับเอกสารที่ไม่ใช่ slide แบบกำกวม
- ทดลองด้วย eval/canary เท่านั้น ไม่ปรับ threshold จากความรู้สึก

### K3 — Answer policy and feedback loop

- Prompt registry/version + structured response validation/retry
- แยก review reason ที่พิสูจน์แล้ว เช่น `missing_knowledge`, `retrieval_miss`, `hallucination`,
  `bad_transcription`, `stale_source`, `wrong_scope` โดยยังมี note เพิ่มเติม
- Queue ให้ Knowledge owner แก้ source/reindex/re-eval/mark resolved
- Dashboard trend ต่อ company/lesson/source/provider โดยไม่เปิด transcript เกินสิทธิ์

### K4 — Operations and governance

- Trace ต่อ question: safe identifiers, scores, chunk ids, config version, latency, cost
- Data retention/redaction/access control/export/delete
- SLO/alerts: indexing backlog/failure, provider error, latency, not-found spike, cost anomaly
- Approval workflow สำหรับ global knowledge และ high-impact prompt/provider changes

## Ownership proposal

| Role | รับผิดชอบ |
|---|---|
| Product/Domain owner | คำตอบที่ถือว่าถูก, source authority, review policy |
| Knowledge/AI engineer | chunk/retrieval/prompt/eval/trace |
| Backend | ingestion jobs, schemas, API, config versions, deletion consistency |
| CS/Content owner | ดูแล source และ review sampled/flagged answers |
| Security/Legal | privacy, retention, external processing, access |

## Definition of done ต่อการเปลี่ยน Knowledge

- มี hypothesis + eval dataset/version + baseline
- รันผลก่อน/หลัง พร้อม quality/latency/cost; ไม่มี cross-company leakage
- ระบุ reindex/cutover/rollback และ source compatibility
- เก็บ config/prompt/model version; monitoring พร้อมก่อน activate
- Human owner อนุมัติ sample ที่สำคัญ และ documentation/operational runbook อัปเดต

