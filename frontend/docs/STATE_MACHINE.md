# Tutor Engine State Machine

Source of truth: `frontend/src/tutor/types.ts`, `intents.ts`, `tutor-reducer.ts` และ reducer tests

## States (15)

`idle`, `preparing`, `intro-speaking`, `ready`, `slide-loading`, `slide-speaking`,
`waiting-slide-duration`, `push-to-talk-recording`, `processing-question`, `answer-speaking`,
`restarting-slide`, `paused`, `final-question-window`, `completed`, `error`

## Main Flow

```mermaid
stateDiagram-v2
    [*] --> idle
    idle --> preparing: JOIN
    preparing --> intro_speaking: LESSON_LOADED
    preparing --> error: LESSON_LOAD_FAILED
    intro_speaking --> ready: TTS_ENDED / ENTER_READY

    ready --> slide_loading: START or INTRO_TIMEOUT
    ready --> push_to_talk_recording: PUSH_TO_TALK_START
    push_to_talk_recording --> processing_question: PUSH_TO_TALK_END
    processing_question --> answer_speaking: READINESS_ANSWERED or QUESTION_ANSWERED or QUESTION_FAILED
    processing_question --> ready: NO_SPEECH from ready

    slide_loading --> slide_speaking: SLIDE_READY
    slide_speaking --> waiting_slide_duration: TTS_ENDED
    waiting_slide_duration --> slide_loading: duration ended / next slide
    waiting_slide_duration --> intro_speaking: last slide

    slide_speaking --> push_to_talk_recording: PUSH_TO_TALK_START
    waiting_slide_duration --> push_to_talk_recording: PUSH_TO_TALK_START
    final_question_window --> push_to_talk_recording: PUSH_TO_TALK_START
    processing_question --> restarting_slide: NO_SPEECH mid-lesson
    answer_speaking --> restarting_slide: answer ended mid-lesson
    restarting_slide --> slide_speaking: SLIDE_READY

    intro_speaking --> final_question_window: final prompt ended
    final_question_window --> intro_speaking: FINAL_QUESTION_TIMEOUT
    intro_speaking --> completed: closing ended
```

## Important Runtime Fields

- `currentSlideIndex` — ตำแหน่งบทเรียนจริง
- `answerSlideIndex` — slide override ชั่วคราวระหว่างอ่านคำตอบจาก slide อื่น
- `interruptedFrom` — state ก่อน Push-to-Talk เพื่อ resume ให้ถูกบริบท
- `afterSpeech` — action หลัง audio จบ เช่น start first slide, restart, await readiness, finish
- `completedAllSlides` — ใช้ persist session result
- `micNotice` — recoverable microphone error ไม่เปลี่ยนทั้งห้องเป็น error state

## Rules

- `NO_SPEECH` กลับไปยัง readiness/slide/final window แบบเงียบ
- `QUESTION_FAILED` พูดข้อความแจ้งก่อน resume
- ตอบ readiness ว่า ready จะพูดยืนยันแล้วเริ่ม slide แรก; not ready จะรอต่อโดยไม่มี auto-start timer
- คำตอบที่อ้าง slide อื่นจะแสดง slide นั้นชั่วคราว แต่ resume ที่ slide เดิม
- หลังคำตอบกลางบทเรียนจะ restart narration ของ slide เดิมพร้อม spoken bridge
- เวลา slide = `max(0, videoDurationMs - actualTtsMs) + breathPauseMs`
- Push-to-Talk ใช้ได้จาก `ready`, `slide-speaking`, `waiting-slide-duration`, `final-question-window`
- Pause/resume เก็บ `pausedFrom`; mic permission failure ใช้ `MIC_UNAVAILABLE` และ resume ได้

## Effects

`NONE`, `LOAD_LESSON`, `SPEAK`, `WAIT_READY_TIMEOUT`, `LOAD_SLIDE`, `WAIT_REMAINING`,
`START_RECORDING`, `STOP_RECORDING_AND_SEND`, `WAIT_FINAL_QUESTION`, `PERSIST_END`

Hook เป็นผู้รัน effects, เล่น Edge TTS audio, ใช้ MediaRecorder, เรียก REST และตั้ง timer
