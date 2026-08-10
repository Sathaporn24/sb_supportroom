# Provider Integration Quick Reference

Provider integrations อยู่ใน .NET backend ทั้งหมด ไม่ได้อยู่ใน Next.js frontend

| Capability | Selection | Implementation |
|---|---|---|
| Google Slides | `SLIDES_PROVIDER=google` | `SupportRoom.Providers.Slides/GoogleSlidesProvider.cs` |
| Thai TTS | `TTS_PROVIDER=edge` | `SupportRoom.Providers.Tts/EdgeTtsProvider.cs` |
| Full-context voice answer | `VOICE_QUESTION_PROVIDER=gemini` | `GeminiVoiceQuestionProvider.cs` |
| Gemini RAG | `VOICE_QUESTION_PROVIDER=gemini-rag` | `RagVoiceQuestionProvider.cs` |
| OpenAI-compatible RAG | `VOICE_QUESTION_PROVIDER=openai-rag` | `RagVoiceQuestionProvider.cs` + `OpenAiRest.cs` |
| Gemini embeddings | `KNOWLEDGE_PROVIDER=pinecone` | `GeminiEmbeddingProvider.cs` |
| OpenAI embeddings | `KNOWLEDGE_PROVIDER=pinecone-openai` | `OpenAiEmbeddingProvider.cs` |
| Vector store | ทั้งสอง knowledge modes | `PineconeKnowledgeIndexProvider.cs` |
| Local document storage | `DOCUMENT_STORAGE_PROVIDER=local` | `LocalDocumentStorageProvider.cs` |
| Huawei OBS | `DOCUMENT_STORAGE_PROVIDER=huawei-obs` | `HuaweiObsDocumentStorageProvider.cs` |

## Rules

- Provider selection ทุกหมวดบังคับและไม่มี Mock fallback
- PostgreSQL/EF Core เป็น data layer บังคับ ไม่มี `DATA_PROVIDER`
- `openai-rag` ยังใช้ Gemini สำหรับ audio transcription
- embedding vendor ต้องตรงกับ vectors ทั้ง index; เปลี่ยน vendor/dimension ต้อง reindex
- credentials ถูกอ่านแบบ lazy เมื่อ provider call ต้องใช้จริง ยกเว้น selection ที่ validate ตอน startup

ดู environment values ใน `backend/src/SupportRoom.Api/.env.example` และรายละเอียดที่
[ENVIRONMENT_SETUP.md](./ENVIRONMENT_SETUP.md)
