import type { AnswerResult } from "@/providers/ai/types";
import type { SummaryQuestion } from "@/types/domain";

export type TutorState =
  | "PRE_JOIN"
  | "GREETING"
  | "WAITING_READY"
  | "TEACHING"
  | "CHECKPOINT"
  | "INTERRUPTED"
  | "ANSWERING"
  | "REVIEWING"
  | "PAUSED"
  | "FINAL_QA"
  | "ENDED"
  | "EXPIRED";

/** Why the engine entered INTERRUPTED, so SPEECH_DONE/SILENCE_TIMEOUT know what to do next. */
export type InterruptionReason = "NOISE" | "NOT_UNDERSTOOD" | "STILL_NOT_UNDERSTOOD" | "REVIEW_PREVIOUS" | null;

/**
 * Tells the SPEECH_DONE handler what to do once the current pendingUtterance finishes -
 * keeps the reducer a flat switch instead of re-deriving intent from (state, reason) pairs.
 */
export type AfterSpeechAction =
  | "ENTER_WAITING_READY"
  | "ADVANCE_TEACHING"
  | "WAIT_CHECKPOINT_SILENCE"
  | "ADVANCE_AFTER_CHECKPOINT"
  | "WAIT_NOISE_CLARIFY"
  | "RESUME_SEGMENT"
  | "WAIT_STILL_NOT_UNDERSTOOD_REPLY"
  | "WAIT_FINAL_QA_SILENCE"
  | "END_SESSION_AFTER_CLOSING"
  | null;

export type TutorRuntime = {
  state: TutorState;
  sessionId: string;
  currentStepIndex: number;
  currentSegmentIndex: number;
  resumeStepIndex?: number;
  resumeSegmentIndex?: number;
  activeMediaId?: string;
  isAiSpeaking: boolean;
  isUserSpeaking: boolean;
  isMicEnabled: boolean;
  isCameraEnabled: boolean;

  // Extra bookkeeping beyond the spec's minimum runtime shape.
  pendingUtterance: string | null;
  pendingDurationMs?: number;
  afterSpeech: AfterSpeechAction;
  resumeIsCheckpoint?: boolean;
  answerReturnMode: "TEACHING" | "FINAL_QA" | null;
  pendingQuestionText: string | null;
  interruptionReason: InterruptionReason;
  notUnderstoodCount: number;
  reviewStepIndex?: number;
  pausedFromState: TutorState | null;
  lastAnswer: AnswerResult | null;
  teacherName?: string;
  startedAt?: string;
  endedAt?: string;
  completedAllSteps: boolean;
  questions: SummaryQuestion[];
  repeatedPoints: string[];
  unresolvedItems: string[];
  connectionStatus: "connected" | "disconnected";
  disconnectedAt?: string;
};

/** Effects the reducer asks the hook to perform - keeps the reducer pure/testable. */
export type TutorEffect =
  | { kind: "NONE" }
  | { kind: "SPEAK"; text: string; durationMs?: number }
  | { kind: "WAIT_SILENCE"; ms: number }
  | { kind: "CALL_AI"; question: string }
  | { kind: "PERSIST_PROGRESS" }
  | { kind: "PERSIST_SUMMARY" };
