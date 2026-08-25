# Gemini and Voice/Text Question Integration

Gemini ทำหน้าที่ audio transcription ทุก voice mode และทำ answer/embedding ในบาง mode ·
`IVoiceQuestionProvider.AnswerTextAsync` (F10/TQ-7..TQ-9) ใช้ provider เดียวกันสำหรับคำถามที่พิมพ์
โดยข้าม step ถอดเสียง - ข้อความที่พิมพ์เข้ามาแทน transcript ตรงๆ

## Modes

| Voice mode | Transcription | Retrieval | Answer |
|---|---|---|---|
| `gemini` | Gemini | ไม่มี; ส่ง full deck | Gemini |
| `gemini-rag` | Gemini | Gemini embedding + Pinecone | Gemini |
| `openai-rag` | Gemini | OpenAI embedding + Pinecone | OpenAI-compatible API |

`GEMINI_API_KEY` บังคับทุก mode; `GEMINI_MODEL` default `gemini-flash-latest`

## Response Contract

Provider คืน:

```json
{
  "transcript": "...",
  "answer": "...",
  "answerStatus": "answered | not_found | out_of_scope | no_speech | transcription_failed",
  "relatedSlideObjectId": "..."
}
```

⚠️ **มติ U1 (2026-08-23)**: ไม่มี `readiness` เหลืออยู่ในสัญญานี้อีกต่อไป (ทั้งเสียงและพิมพ์) -
`no_speech`/`transcription_failed` เป็นไปไม่ได้เลยบนเส้นทางพิมพ์เพราะไม่มีเสียงให้ถอด และข้อความว่าง
ถูกปัดตั้งแต่ก่อนเรียก provider (TQ-3/TQ-10)

## Grounding Rules

- ตอบจาก retrieved chunks/full lesson context เท่านั้น
- ห้ามสร้าง slide object ID ที่ไม่มีใน context
- RAG query lesson namespace และ `kb-global`, merge top score แล้วกรองด้วย threshold
- Retrieval outage fallback ไป full-deck context
- Indexed corpus ที่มีแต่ไม่มี chunk ผ่าน threshold จะตอบ not-found ไม่ fallback

## Setup

1. ตั้ง `GEMINI_API_KEY` และ optional `GEMINI_MODEL`
2. เลือก `VOICE_QUESTION_PROVIDER`
3. ถ้าใช้ RAG ตั้ง `KNOWLEDGE_PROVIDER` และ Pinecone credentials
4. ให้ Pinecone dimension ตรงกับ embedding provider (ค่าเริ่มต้น 768)
5. Reindex ทุก namespace เมื่อเปลี่ยน embedding vendor/model/dimension

## Verification

ทดสอบ answered, not-found, out-of-scope, no-speech (เสียงเท่านั้น), malformed JSON, provider
429/5xx, retrieval outage และ related-slide mapping สำหรับทั้งเส้นทางเสียงและพิมพ์
โดยไม่ log transcript/prompt เต็ม
