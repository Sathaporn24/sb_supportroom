import type { AnswerStatus } from "@/types/domain";

/** User/UI-triggered events. */
export type TutorUserEvent =
  | { type: "JOIN" }
  | { type: "START" }
  | { type: "PUSH_TO_TALK_START" }
  | { type: "PUSH_TO_TALK_END" }
  | { type: "PAUSE" }
  | { type: "RESUME" }
  | { type: "END_SESSION" }
  | { type: "TOGGLE_MIC" }
  | { type: "TOGGLE_CAMERA" }
  /** Clears runtime.micNotice once the teacher has seen it (dismiss click or auto-dismiss timer). */
  | { type: "CLEAR_MIC_NOTICE" };

/** Engine-driven events, dispatched by the hook after an effect settles. */
export type TutorInternalEvent =
  | { type: "LESSON_LOADED" }
  | { type: "LESSON_LOAD_FAILED"; message: string }
  | { type: "INTRO_TIMEOUT" }
  | { type: "SLIDE_READY" }
  /** elapsedMs = actual TTS playback duration, used to compute the remaining video wait. */
  | { type: "TTS_ENDED"; elapsedMs: number }
  | { type: "SLIDE_DURATION_ENDED" }
  | { type: "NO_SPEECH" }
  | {
      type: "QUESTION_ANSWERED";
      transcript: string;
      answer: string;
      answerStatus: AnswerStatus;
      relatedSlideObjectId?: string;
    }
  | { type: "QUESTION_FAILED" }
  /** Spoken answer to "พร้อมหรือยังคะ?", used instead of clicking the start button. */
  | { type: "READINESS_ANSWERED"; ready: boolean }
  | { type: "RESTART_CURRENT_SLIDE" }
  | { type: "NEXT_SLIDE" }
  | { type: "FINAL_QUESTION_TIMEOUT" }
  | { type: "FAIL"; message: string }
  /** Push-to-talk couldn't get a mic stream (permission denied, no device, ...) - recoverable,
   * unlike FAIL: the lesson resumes right where it was interrupted instead of ending the session. */
  | { type: "MIC_UNAVAILABLE"; message: string };

export type TutorEvent = TutorUserEvent | TutorInternalEvent;
