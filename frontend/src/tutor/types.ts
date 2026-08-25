import type { SessionQuestion } from "@/types/domain";

export type TutorState =
  | "idle"
  | "preparing"
  | "intro-speaking"
  | "ready"
  | "slide-loading"
  | "slide-speaking"
  | "waiting-slide-duration"
  | "push-to-talk-recording"
  | "processing-question"
  | "answer-speaking"
  | "restarting-slide"
  | "paused"
  | "final-question-window"
  | "completed"
  | "error";

/**
 * Tells the TTS_ENDED handler what to do once the current utterance finishes speaking -
 * lets "intro-speaking"/"answer-speaking" be reused for several kinds of AI utterances
 * (greeting, final-question prompt, closing, answers) without adding more states.
 */
export type AfterSpeechAction =
  | "ENTER_READY"
  | "WAIT_SLIDE_DURATION"
  | "RESTART_SLIDE"
  /** Like RESTART_SLIDE, but leads the narration with a "we're back where we left off" line. */
  | "RESUME_AFTER_ANSWER"
  /** Teacher said they aren't ready yet - go back to waiting, with no auto-start timer. */
  | "AWAIT_READINESS"
  | "WAIT_FINAL_QUESTION"
  | "FINISH_SESSION"
  | null;

export type TutorRuntime = {
  state: TutorState;
  currentSlideIndex: number;
  /**
   * Slide to *display* while an answer is being spoken, when the answer is grounded in a
   * different slide than the one being taught. Purely a view override - currentSlideIndex
   * stays put so the lesson resumes exactly where it was interrupted.
   */
  answerSlideIndex: number | null;
  /**
   * State the teacher was in when they pressed the talk button. Push-to-talk is allowed
   * from the readiness prompt as well as mid-lesson, and those resume very differently -
   * one goes back to waiting, the other back to a slide - so the origin has to be
   * remembered rather than inferred once we're already in processing-question.
   */
  interruptedFrom: TutorState | null;
  isMicEnabled: boolean;
  isCameraEnabled: boolean;
  isAiSpeaking: boolean;
  afterSpeech: AfterSpeechAction;
  questions: SessionQuestion[];
  pausedFrom: TutorState | null;
  errorMessage: string | null;
  completedAllSlides: boolean;
  /**
   * Non-fatal, dismissible notice (e.g. mic permission denied) - unlike errorMessage/state
   * "error" this does NOT replace the room UI. The lesson resumes right where push-to-talk
   * interrupted it; this just tells the teacher why their question wasn't recorded.
   */
  micNotice: string | null;
  /**
   * QA-03 - the text of a typed question that failed to send (network/upstream error, not a
   * validation failure the drawer already blocked client-side). The drawer already cleared its
   * own `draft` state optimistically on submit (TQ-15), so this is the only place that text
   * survives - the drawer restores it into the input once, then dispatches
   * CLEAR_FAILED_QUESTION_TEXT to consume it, mirroring how micNotice is dismissed.
   */
  failedQuestionText: string | null;
};

export type TutorEffect =
  | { kind: "NONE" }
  | { kind: "LOAD_LESSON" }
  /**
   * Generic "speak this text" - context for what happens after comes from afterSpeech.
   * withFoundLead prefixes an "อ้อ เจอแล้วค่ะ" beat, so the answer arrives as a discovery
   * rather than cutting in cold after the waiting sounds. Set only for answers that
   * actually found something; leading a "ไม่พบข้อมูล" with "เจอแล้ว" contradicts itself.
   */
  | { kind: "SPEAK"; text: string; withFoundLead?: boolean }
  | { kind: "WAIT_READY_TIMEOUT"; ms: number }
  /** withResumeBridge prepends a spoken hand-back line; the exact wording is picked by the effect runner. */
  | { kind: "LOAD_SLIDE"; slideIndex: number; withResumeBridge?: boolean }
  | { kind: "WAIT_REMAINING"; ms: number }
  | { kind: "START_RECORDING" }
  | { kind: "STOP_RECORDING_AND_SEND" }
  /** TQ-14 - send a typed question. Carries the text itself, unlike STOP_RECORDING_AND_SEND
   * which reads the recorded audio out of a ref the effect runner already owns. */
  | { kind: "SEND_TEXT_QUESTION"; text: string }
  | { kind: "WAIT_FINAL_QUESTION"; ms: number }
  | { kind: "PERSIST_END"; completedAllSlides: boolean };
