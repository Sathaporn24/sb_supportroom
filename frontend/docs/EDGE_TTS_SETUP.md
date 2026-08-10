# Edge TTS Setup

Backend ใช้ `EdgeTTS.DotNet` ผ่าน
`backend/src/SupportRoom.Providers.Tts/EdgeTtsProvider.cs` และไม่มี API key

```env
TTS_PROVIDER=edge
EDGE_TTS_VOICE=th-TH-PremwadeeNeural
EDGE_TTS_RATE=-10%
```

- `EDGE_TTS_VOICE` เปลี่ยนเป็น Edge voice short name อื่นได้
- `EDGE_TTS_RATE` รับ SSML rate เช่น `-10%`
- Provider chunk ข้อความยาวและรวม MP3 bytes เพื่อหลีกเลี่ยง long-narration timeout
- `POST /api/tts` รับ `{ text, rate? }`; per-request rate ใช้กับ filler/เสียงบางบริบทได้

ตรวจด้วยข้อความไทยสั้น/ยาว, punctuation, หลาย chunk, concurrent requests และ network failure
