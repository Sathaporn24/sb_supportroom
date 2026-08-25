import type { AnswerStatus, QuestionSource } from "@/types/domain";

/** User/UI-triggered events. */
export type TutorUserEvent =
  | { type: "JOIN" }
  | { type: "START" }
  /** TQ-18/U1 - the only remaining way to answer "ยังไม่พร้อม". Accepted only from "ready". */
  | { type: "NOT_READY" }
  | { type: "PUSH_TO_TALK_START" }
  | { type: "PUSH_TO_TALK_END" }
  /** TQ-14 - submitting the typed question drawer. Unlike push-to-talk this does not interrupt
   * narration on its own; T5 draws the line at "กดส่ง", not at opening the drawer or typing. */
  | { type: "SUBMIT_TEXT_QUESTION"; text: string }
  | { type: "PAUSE" }
  | { type: "RESUME" }
  | { type: "END_SESSION" }
  | { type: "TOGGLE_MIC" }
  | { type: "TOGGLE_CAMERA" }
  /** Clears runtime.micNotice once the teacher has seen it (dismiss click or auto-dismiss timer). */
  | { type: "CLEAR_MIC_NOTICE" }
  /** QA-03 - the drawer dispatches this right after it has copied failedQuestionText back into
   * its own draft state, so the runtime doesn't keep re-offering the same recovered text. */
  | { type: "CLEAR_FAILED_QUESTION_TEXT" };

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
      /** Which channel produced this question - the hook knows (it called askVoiceQuestion or
       * askTextQuestion), and the reducer needs it to fill SessionQuestion.source on the local
       * record it keeps for the drawer's timeline. */
      source: QuestionSource;
    }
  /** text is set only for a failed typed question (sendTextQuestion's catch) - the voice path
   * has nothing worth restoring into an input field, so it dispatches this with text omitted. */
  | { type: "QUESTION_FAILED"; text?: string }
  | { type: "RESTART_CURRENT_SLIDE" }
  | { type: "NEXT_SLIDE" }
  | { type: "FINAL_QUESTION_TIMEOUT" }
  | { type: "FAIL"; message: string }
  /** Push-to-talk couldn't get a mic stream (permission denied, no device, ...) - recoverable,
   * unlike FAIL: the lesson resumes right where it was interrupted instead of ending the session. */
  | { type: "MIC_UNAVAILABLE"; message: string };

export type TutorEvent = TutorUserEvent | TutorInternalEvent;
