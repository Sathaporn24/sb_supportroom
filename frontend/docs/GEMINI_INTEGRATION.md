# Gemini and Voice Question Integration

Gemini ทำหน้าที่ audio transcription ทุก voice mode และทำ answer/embedding ในบาง mode

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
  "relatedSlideObjectId": "...",
  "readiness": "ready | not_ready"
}
```

Readiness request ใช้ prompt สั้นและไม่ persist เป็น `SessionQuestion`

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

ทดสอบ readiness, answered, not-found, out-of-scope, no-speech, malformed JSON, provider 429/5xx,
retrieval outage และ related-slide mapping โดยไม่ log transcript/prompt เต็ม
