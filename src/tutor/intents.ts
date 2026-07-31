import type { AnswerResult } from "@/providers/ai/types";

/** User/demo-triggered intents (what Demo Controls and Chat dispatch). */
export type TutorUserAction =
  | { type: "JOIN_ROOM"; teacherName?: string }
  | { type: "READY" }
  | { type: "ASK_QUESTION"; question: string }
  | { type: "NOT_UNDERSTOOD" }
  | { type: "STILL_NOT_UNDERSTOOD" }
  | { type: "REVIEW_PREVIOUS" }
  | { type: "PAUSE" }
  | { type: "RESUME" }
  | { type: "NOISE_OR_MEANINGLESS" }
  | { type: "TOGGLE_MIC" }
  | { type: "TOGGLE_CAMERA" }
  | { type: "LEAVE" }
  | { type: "DISCONNECT" }
  | { type: "RECONNECT" };

/** Internal engine-driven events (dispatched by the hook, never by UI directly). */
export type TutorInternalAction =
  | { type: "SPEECH_DONE" }
  | { type: "SILENCE_TIMEOUT" }
  | { type: "ANSWER_READY"; result: AnswerResult };

export type TutorAction = TutorUserAction | TutorInternalAction;
