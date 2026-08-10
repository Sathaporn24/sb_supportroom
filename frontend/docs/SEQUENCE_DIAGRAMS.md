# Sequence Diagrams

ตรงกับ Route Handler และ Hook จริงในโค้ด (`src/app/api/**`, `src/hooks/use-tutor-session.ts`)

## 1. CS บันทึก Google Slides Lesson Config

```mermaid
sequenceDiagram
    actor CS
    participant UI as Admin UI (/admin/lessons/[slug])
    participant API as POST /api/lessons
    participant SlidesP as SlidesContentProvider
    participant Repo as LessonConfigRepository

    CS->>UI: กรอก Slides Source URL + videoDurationMs ต่อ Slide
    UI->>API: saveLesson(form)
    API->>SlidesP: resolvePresentation(sourceUrl)
    SlidesP-->>API: { presentationId, embedUrl, warning? }
    API->>Repo: save(LessonConfig)
    Repo-->>API: LessonConfig (saved)
    API-->>UI: { lesson }
    UI-->>CS: แสดงผลบันทึกสำเร็จ + คำเตือน (ถ้ามี)
```

## 2. Sync Google Slides และ Speaker Notes

```mermaid
sequenceDiagram
    actor CS
    participant UI as Admin UI
    participant Resolve as POST /api/slides/resolve
    participant Content as GET /api/slides/content
    participant SlidesP as SlidesContentProvider

    CS->>UI: กด "ตรวจสอบ/Sync Slides"
    UI->>Resolve: resolveSlides({ slidesSourceUrl, slidesEmbedUrl })
    Resolve->>SlidesP: resolvePresentation(...)
    SlidesP-->>Resolve: { presentationId, embedUrl, isEmbedOnly, warning? }
    Resolve-->>UI: ResolvedPresentation
    UI->>Content: getSlidesContentPreview(presentationId)
    Content->>SlidesP: getLessonContent({ presentationId })
    SlidesP-->>Content: { slides: [{ slideObjectId, index, speakerNotes }] }
    Content-->>UI: SlidesLessonContent
    UI-->>CS: แสดงรายการ Slide + Speaker Notes (อ่านอย่างเดียว) ให้กรอก videoDurationMs
```

## 3. CS สร้าง Session Link

```mermaid
sequenceDiagram
    actor CS
    participant UI as Admin UI (/admin/sessions/new)
    participant API as POST /api/sessions
    participant Repo as SessionRepository

    CS->>UI: เลือกบทเรียน (ต้อง isActive) + กรอกชื่อครู/โรงเรียน (ไม่บังคับ) + วันหมดอายุ
    UI->>API: createSession({ lessonSlug, teacherName?, schoolName?, expiresAt? })
    API->>API: ถ้าไม่ระบุ expiresAt ใช้ getDefaultSessionExpiryHours() (24 ชม.)
    API->>Repo: create(input)
    Repo-->>API: TrainingSession { token, ... }
    API-->>UI: { session }
    UI-->>CS: แสดงลิงก์ /join/[token] + ปุ่มคัดลอก
```

## 4. คุณครูเข้าห้องและเริ่มสอน

```mermaid
sequenceDiagram
    actor Teacher
    participant Join as /join/[token]
    participant Room as /room/[token]
    participant Hook as useTutorSession
    participant API as GET /api/lessons/[slug]
    participant TTS as POST /api/tts

    Teacher->>Join: เปิดลิงก์ + ทดสอบกล้อง/ไมค์
    Join->>Room: กด "เข้าร่วมห้องสอน"
    Room->>Hook: mount → dispatch(JOIN) + markSessionStarted(token)
    Hook->>API: getLessonBySlug(session.lessonSlug)
    API-->>Hook: { lesson, embedUrl, slides: TeachingSlide[] }
    Hook->>Hook: dispatch(LESSON_LOADED) → intro-speaking
    Hook->>TTS: synthesizeSpeech(introScript)
    TTS-->>Hook: audio blob
    Hook->>Hook: play() → on "ended" → dispatch(TTS_ENDED) → ready
    Note over Hook: รอ START หรือ INTRO_TIMEOUT (introWaitMs)
    Hook->>Hook: dispatch(START/INTRO_TIMEOUT) → slide-loading(0)
    loop ทุก Slide
        Hook->>TTS: synthesizeSpeech(slide.speakerNotes)
        TTS-->>Hook: audio blob
        Hook->>Hook: dispatch(SLIDE_READY) → play() → "ended" → dispatch(TTS_ENDED, elapsedMs)
        Hook->>Hook: WAIT_REMAINING = max(0, videoDurationMs-elapsedMs) + breathPauseMs
        Hook->>Hook: dispatch(SLIDE_DURATION_ENDED) → next slide หรือ final-question-window
    end
```

## 5. Push-to-Talk → Gemini/Mock → Hugging Face/Mock → Resume/Restart Slide

```mermaid
sequenceDiagram
    actor Teacher
    participant Room as Room UI
    participant Hook as useTutorSession
    participant VQ as POST /api/voice-question
    participant VoiceP as VoiceQuestionProvider
    participant TTS as POST /api/tts

    Teacher->>Room: กดค้างปุ่มไมค์ (mouse/touch/keyboard)
    Room->>Hook: dispatch(PUSH_TO_TALK_START)
    Hook->>Hook: หยุดเสียง AI ทันที (abort <audio> ที่กำลังเล่น) + เริ่ม MediaRecorder
    Teacher->>Room: ปล่อยปุ่ม
    Room->>Hook: dispatch(PUSH_TO_TALK_END)
    Hook->>Hook: stop recorder → durationMs = now - startedAt
    alt durationMs < MIN_VOICE_DURATION_MS
        Hook->>Hook: dispatch(NO_SPEECH) — ไม่เรียก API
    else ส่งเสียงไปประมวลผล
        Hook->>VQ: askVoiceQuestion({ audioBlob, lessonSlug, sessionId, durationMs, currentSlideObjectId })
        VQ->>VoiceP: transcribeAndAnswer({ audio, lessonSlides: ทุก Slide Notes })
        VoiceP-->>VQ: { transcript, answer, answerStatus, relatedSlideObjectId? }
        VQ->>VQ: persist SessionQuestion (ถ้า answerStatus != no_speech)
        VQ-->>Hook: VoiceQuestionResult
        alt answerStatus = no_speech หรือ transcription_failed
            Hook->>Hook: dispatch(NO_SPEECH) — กลับไปสอนต่อทันที ไม่พูดเพิ่ม
        else answered / not_found / out_of_scope
            Hook->>Hook: dispatch(QUESTION_ANSWERED)
            Hook->>TTS: synthesizeSpeech(answer)
            TTS-->>Hook: audio blob
            Hook->>Hook: play() → "ended" → dispatch(TTS_ENDED)
        end
    end
    Hook->>Hook: restartCurrentSlide() (mid-lesson) หรือกลับ final-question-window (ท้ายบทเรียน)
```

## 6. จบ Session และบันทึก Summary

```mermaid
sequenceDiagram
    actor Teacher
    participant Room as Room UI
    participant Hook as useTutorSession
    participant API as PATCH /api/sessions/[token]
    participant SessionRepo as SessionRepository
    participant QRepo as SessionQuestionRepository
    participant SummaryRepo as SessionSummaryRepository

    alt กด "ออกจากห้อง"
        Teacher->>Room: dispatch(END_SESSION)
    else เงียบเกิน finalQuestionWaitMs ในหน้าต่างคำถามท้ายบทเรียน
        Hook->>Hook: dispatch(FINAL_QUESTION_TIMEOUT) → พูดคำกล่าวลา → TTS_ENDED(FINISH_SESSION)
    end
    Hook->>Hook: state = completed, effect PERSIST_END{ completedAllSlides }
    Hook->>API: endSession(token, { completedAllSlides, lastSlideObjectId })
    API->>SessionRepo: end(sessionId, result)
    API->>QRepo: listBySession(sessionId)
    QRepo-->>API: SessionQuestion[]
    API->>SummaryRepo: save({ sessionId, completedAllSlides, questions, unansweredPoints })
    API-->>Hook: { session }
    Room->>Room: router.replace("/session-ended")
```
