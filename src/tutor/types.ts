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
  isMicEnabled: boolean;
  isCameraEnabled: boolean;
  isAiSpeaking: boolean;
  afterSpeech: AfterSpeechAction;
  questions: SessionQuestion[];
  pausedFrom: TutorState | null;
  errorMessage: string | null;
  completedAllSlides: boolean;
};

export type TutorEffect =
  | { kind: "NONE" }
  | { kind: "LOAD_LESSON" }
  /** Generic "speak this text" - context for what happens after comes from afterSpeech. */
  | { kind: "SPEAK"; text: string }
  | { kind: "WAIT_READY_TIMEOUT"; ms: number }
  | { kind: "LOAD_SLIDE"; slideIndex: number }
  | { kind: "WAIT_REMAINING"; ms: number }
  | { kind: "START_RECORDING" }
  | { kind: "STOP_RECORDING_AND_SEND" }
  | { kind: "WAIT_FINAL_QUESTION"; ms: number }
  | { kind: "PERSIST_END"; completedAllSlides: boolean };
