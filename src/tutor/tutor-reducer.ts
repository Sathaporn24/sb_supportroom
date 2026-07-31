import type { Lesson, Step } from "@/types/domain";
import type { TutorAction } from "@/tutor/intents";
import type { TutorEffect, TutorRuntime, TutorState } from "@/tutor/types";
import { tutorConfig } from "@/config/tutor-config";
import { getCheckpointPromptText } from "@/config/checkpoint-prompts";
import {
  checkpointContinueScript,
  closingScript,
  greetingScript,
  noiseClarifyScript,
  noiseContinueScript,
  reviewIntroScript,
  reviewReturnScript,
  simplifiedExplanationScript,
  stillNotUnderstoodPromptScript,
  summaryAndFinalQaScript,
} from "@/tutor/scripts";

export function createInitialRuntime(sessionId: string, teacherName?: string): TutorRuntime {
  return {
    state: "PRE_JOIN",
    sessionId,
    currentStepIndex: 0,
    currentSegmentIndex: 0,
    isAiSpeaking: false,
    isUserSpeaking: false,
    isMicEnabled: true,
    isCameraEnabled: false,
    pendingUtterance: null,
    afterSpeech: null,
    answerReturnMode: null,
    pendingQuestionText: null,
    interruptionReason: null,
    notUnderstoodCount: 0,
    pausedFromState: null,
    lastAnswer: null,
    teacherName,
    completedAllSteps: false,
    questions: [],
    repeatedPoints: [],
    unresolvedItems: [],
    connectionStatus: "connected",
  };
}

type AdvanceResult =
  | { type: "segment"; stepIndex: number; segIndex: number }
  | { type: "checkpoint"; stepIndex: number }
  | { type: "final" };

function afterStep(lesson: Lesson, stepIndex: number): AdvanceResult {
  if (stepIndex + 1 < lesson.steps.length) {
    return { type: "segment", stepIndex: stepIndex + 1, segIndex: 0 };
  }
  return { type: "final" };
}

function advance(lesson: Lesson, stepIndex: number, segIndex: number): AdvanceResult {
  const step = lesson.steps[stepIndex];
  if (segIndex + 1 < step.segments.length) {
    return { type: "segment", stepIndex, segIndex: segIndex + 1 };
  }
  if (step.checkpointEnabled) {
    return { type: "checkpoint", stepIndex };
  }
  return afterStep(lesson, stepIndex);
}

function speak(
  runtime: TutorRuntime,
  patch: Partial<TutorRuntime>,
  text: string,
  afterSpeech: TutorRuntime["afterSpeech"],
  durationMs?: number,
): { runtime: TutorRuntime; effect: TutorEffect } {
  return {
    runtime: { ...runtime, ...patch, isAiSpeaking: true, pendingUtterance: text, afterSpeech },
    effect: { kind: "SPEAK", text, durationMs },
  };
}

function noEffect(runtime: TutorRuntime): { runtime: TutorRuntime; effect: TutorEffect } {
  return { runtime, effect: { kind: "NONE" } };
}

function enterAdvanceResult(
  runtime: TutorRuntime,
  lesson: Lesson,
  result: AdvanceResult,
): { runtime: TutorRuntime; effect: TutorEffect } {
  if (result.type === "segment") {
    const segment = lesson.steps[result.stepIndex].segments[result.segIndex];
    return speak(
      runtime,
      {
        state: "TEACHING",
        currentStepIndex: result.stepIndex,
        currentSegmentIndex: result.segIndex,
        activeMediaId: segment.mediaId,
        notUnderstoodCount: 0,
        interruptionReason: null,
      },
      segment.scriptText,
      "ADVANCE_TEACHING",
      segment.mockSpeakDurationMs,
    );
  }
  if (result.type === "checkpoint") {
    const step = lesson.steps[result.stepIndex];
    return speak(
      runtime,
      {
        state: "CHECKPOINT",
        currentStepIndex: result.stepIndex,
        resumeStepIndex: result.stepIndex,
        resumeSegmentIndex: undefined,
        resumeIsCheckpoint: true,
      },
      getCheckpointPromptText(step.checkpointPromptId),
      "WAIT_CHECKPOINT_SILENCE",
    );
  }
  return speak(runtime, { state: "FINAL_QA", completedAllSteps: true }, summaryAndFinalQaScript, "WAIT_FINAL_QA_SILENCE");
}

function resumeSegment(runtime: TutorRuntime, lesson: Lesson): { runtime: TutorRuntime; effect: TutorEffect } {
  const stepIndex = runtime.resumeStepIndex ?? runtime.currentStepIndex;
  if (runtime.resumeIsCheckpoint) {
    const step = lesson.steps[stepIndex];
    return speak(
      runtime,
      { state: "CHECKPOINT", currentStepIndex: stepIndex, interruptionReason: null, reviewStepIndex: undefined },
      getCheckpointPromptText(step.checkpointPromptId),
      "WAIT_CHECKPOINT_SILENCE",
    );
  }
  const segIndex = runtime.resumeSegmentIndex ?? runtime.currentSegmentIndex;
  const segment = lesson.steps[stepIndex].segments[segIndex];
  return speak(
    runtime,
    {
      state: "TEACHING",
      currentStepIndex: stepIndex,
      currentSegmentIndex: segIndex,
      activeMediaId: segment.mediaId,
      interruptionReason: null,
      reviewStepIndex: undefined,
    },
    segment.scriptText,
    "ADVANCE_TEACHING",
    segment.mockSpeakDurationMs,
  );
}

const PAUSABLE_STATES: TutorState[] = [
  "GREETING",
  "WAITING_READY",
  "TEACHING",
  "CHECKPOINT",
  "INTERRUPTED",
  "REVIEWING",
  "FINAL_QA",
];

const ASK_QUESTION_STATES: TutorState[] = ["TEACHING", "CHECKPOINT", "INTERRUPTED", "FINAL_QA"];

function currentStep(lesson: Lesson, runtime: TutorRuntime): Step {
  return lesson.steps[runtime.currentStepIndex];
}

export function tutorReducer(
  runtime: TutorRuntime,
  action: TutorAction,
  lesson: Lesson,
): { runtime: TutorRuntime; effect: TutorEffect } {
  switch (action.type) {
    case "JOIN_ROOM": {
      if (runtime.state !== "PRE_JOIN") {
        return noEffect(runtime);
      }
      const firstMediaId = lesson.steps[0]?.segments[0]?.mediaId;
      return speak(
        runtime,
        {
          state: "GREETING",
          teacherName: action.teacherName,
          activeMediaId: firstMediaId,
          startedAt: new Date().toISOString(),
        },
        greetingScript(action.teacherName),
        "ENTER_WAITING_READY",
      );
    }

    case "READY": {
      if (runtime.state !== "WAITING_READY") {
        return noEffect(runtime);
      }
      return enterAdvanceResult(runtime, lesson, { type: "segment", stepIndex: 0, segIndex: 0 });
    }

    case "NOISE_OR_MEANINGLESS": {
      if (runtime.state !== "TEACHING" && runtime.state !== "CHECKPOINT") {
        return noEffect(runtime);
      }
      return speak(
        runtime,
        {
          state: "INTERRUPTED",
          interruptionReason: "NOISE",
          resumeStepIndex: runtime.currentStepIndex,
          resumeSegmentIndex: runtime.currentSegmentIndex,
          resumeIsCheckpoint: runtime.state === "CHECKPOINT",
        },
        noiseClarifyScript,
        "WAIT_NOISE_CLARIFY",
      );
    }

    case "NOT_UNDERSTOOD": {
      if (runtime.state !== "TEACHING") {
        return noEffect(runtime);
      }
      const step = currentStep(lesson, runtime);
      const segment = step.segments[runtime.currentSegmentIndex];
      return speak(
        runtime,
        {
          state: "INTERRUPTED",
          interruptionReason: "NOT_UNDERSTOOD",
          notUnderstoodCount: runtime.notUnderstoodCount + 1,
          activeMediaId: segment.mediaId,
          resumeStepIndex: runtime.currentStepIndex,
          resumeSegmentIndex: runtime.currentSegmentIndex,
          resumeIsCheckpoint: false,
          repeatedPoints: [...runtime.repeatedPoints, step.title],
        },
        simplifiedExplanationScript(segment.scriptText),
        "ADVANCE_TEACHING",
      );
    }

    case "STILL_NOT_UNDERSTOOD": {
      if (runtime.notUnderstoodCount < 1 || !ASK_QUESTION_STATES.includes(runtime.state)) {
        return noEffect(runtime);
      }
      return speak(
        runtime,
        { state: "INTERRUPTED", interruptionReason: "STILL_NOT_UNDERSTOOD" },
        stillNotUnderstoodPromptScript,
        "WAIT_STILL_NOT_UNDERSTOOD_REPLY",
      );
    }

    case "REVIEW_PREVIOUS": {
      if ((runtime.state !== "TEACHING" && runtime.state !== "CHECKPOINT") || runtime.currentStepIndex === 0) {
        return noEffect(runtime);
      }
      const previousStep = lesson.steps[runtime.currentStepIndex - 1];
      const previousSegment = previousStep.segments[0];
      return speak(
        runtime,
        {
          state: "REVIEWING",
          reviewStepIndex: runtime.currentStepIndex - 1,
          activeMediaId: previousSegment.mediaId,
          resumeStepIndex: runtime.currentStepIndex,
          resumeSegmentIndex: runtime.currentSegmentIndex,
          resumeIsCheckpoint: runtime.state === "CHECKPOINT",
          repeatedPoints: [...runtime.repeatedPoints, previousStep.title],
        },
        `${reviewIntroScript(previousStep.title, previousSegment.scriptText)} ${reviewReturnScript}`,
        "RESUME_SEGMENT",
      );
    }

    case "ASK_QUESTION": {
      if (!ASK_QUESTION_STATES.includes(runtime.state)) {
        return noEffect(runtime);
      }
      let resumeStepIndex = runtime.resumeStepIndex;
      let resumeSegmentIndex = runtime.resumeSegmentIndex;
      let resumeIsCheckpoint = runtime.resumeIsCheckpoint;
      if (runtime.state === "TEACHING") {
        resumeStepIndex = runtime.currentStepIndex;
        resumeSegmentIndex = runtime.currentSegmentIndex;
        resumeIsCheckpoint = false;
      } else if (runtime.state === "CHECKPOINT") {
        resumeStepIndex = runtime.currentStepIndex;
        resumeSegmentIndex = undefined;
        resumeIsCheckpoint = true;
      }
      const answerReturnMode = runtime.state === "FINAL_QA" ? "FINAL_QA" : "TEACHING";
      return {
        runtime: {
          ...runtime,
          state: "ANSWERING",
          resumeStepIndex,
          resumeSegmentIndex,
          resumeIsCheckpoint,
          answerReturnMode,
          pendingQuestionText: action.question,
          isAiSpeaking: false,
          pendingUtterance: null,
        },
        effect: { kind: "CALL_AI", question: action.question },
      };
    }

    case "ANSWER_READY": {
      if (runtime.state !== "ANSWERING") {
        return noEffect(runtime);
      }
      const result = action.result;
      const questionText = runtime.pendingQuestionText ?? "";
      const questions = [
        ...runtime.questions,
        { question: questionText, answer: result.text, scope: result.scope, resolved: result.scope !== "UNKNOWN" },
      ];
      const unresolvedItems =
        result.scope === "UNKNOWN" ? [...runtime.unresolvedItems, questionText] : runtime.unresolvedItems;
      const afterSpeech = runtime.answerReturnMode === "FINAL_QA" ? "WAIT_FINAL_QA_SILENCE" : "RESUME_SEGMENT";
      return speak(
        runtime,
        {
          questions,
          unresolvedItems,
          activeMediaId: result.relatedMediaId ?? runtime.activeMediaId,
          lastAnswer: result,
          pendingQuestionText: null,
        },
        result.text,
        afterSpeech,
      );
    }

    case "PAUSE": {
      if (!PAUSABLE_STATES.includes(runtime.state)) {
        return noEffect(runtime);
      }
      return {
        runtime: { ...runtime, pausedFromState: runtime.state, state: "PAUSED", isAiSpeaking: false, pendingUtterance: null },
        effect: { kind: "NONE" },
      };
    }

    case "RESUME": {
      if (runtime.state !== "PAUSED" || !runtime.pausedFromState) {
        return noEffect(runtime);
      }
      const from = runtime.pausedFromState;
      const base: TutorRuntime = { ...runtime, pausedFromState: null };
      if (from === "WAITING_READY" || from === "GREETING") {
        return { runtime: { ...base, state: "WAITING_READY" }, effect: { kind: "WAIT_SILENCE", ms: tutorConfig.readyAutoContinueMs } };
      }
      if (from === "TEACHING") {
        const segment = currentStep(lesson, base).segments[base.currentSegmentIndex];
        return speak(base, { state: "TEACHING" }, segment.scriptText, "ADVANCE_TEACHING", segment.mockSpeakDurationMs);
      }
      if (from === "CHECKPOINT") {
        const step = currentStep(lesson, base);
        return speak(base, { state: "CHECKPOINT" }, getCheckpointPromptText(step.checkpointPromptId), "WAIT_CHECKPOINT_SILENCE");
      }
      if (from === "FINAL_QA") {
        return { runtime: { ...base, state: "FINAL_QA" }, effect: { kind: "WAIT_SILENCE", ms: tutorConfig.finalQuestionSilenceMs } };
      }
      // INTERRUPTED / REVIEWING: fall back to resuming the remembered teaching position.
      return resumeSegment(base, lesson);
    }

    case "SPEECH_DONE": {
      switch (runtime.afterSpeech) {
        case "ENTER_WAITING_READY":
          return {
            runtime: { ...runtime, state: "WAITING_READY", isAiSpeaking: false, pendingUtterance: null, afterSpeech: null },
            effect: { kind: "WAIT_SILENCE", ms: tutorConfig.readyAutoContinueMs },
          };
        case "ADVANCE_TEACHING":
          return enterAdvanceResult(
            { ...runtime, isAiSpeaking: false, pendingUtterance: null, afterSpeech: null },
            lesson,
            advance(lesson, runtime.currentStepIndex, runtime.currentSegmentIndex),
          );
        case "WAIT_CHECKPOINT_SILENCE":
          return {
            runtime: { ...runtime, isAiSpeaking: false, pendingUtterance: null, afterSpeech: null },
            effect: { kind: "WAIT_SILENCE", ms: tutorConfig.checkpointSilenceMs },
          };
        case "ADVANCE_AFTER_CHECKPOINT":
          return enterAdvanceResult(
            { ...runtime, isAiSpeaking: false, pendingUtterance: null, afterSpeech: null },
            lesson,
            afterStep(lesson, runtime.currentStepIndex),
          );
        case "WAIT_NOISE_CLARIFY":
          return {
            runtime: { ...runtime, isAiSpeaking: false, pendingUtterance: null, afterSpeech: null },
            effect: { kind: "WAIT_SILENCE", ms: tutorConfig.interruptionClarifyMs },
          };
        case "RESUME_SEGMENT":
          return resumeSegment({ ...runtime, isAiSpeaking: false, pendingUtterance: null, afterSpeech: null }, lesson);
        case "WAIT_STILL_NOT_UNDERSTOOD_REPLY":
          return {
            runtime: { ...runtime, isAiSpeaking: false, pendingUtterance: null, afterSpeech: null },
            effect: { kind: "NONE" },
          };
        case "WAIT_FINAL_QA_SILENCE":
          return {
            runtime: { ...runtime, isAiSpeaking: false, pendingUtterance: null, afterSpeech: null },
            effect: { kind: "WAIT_SILENCE", ms: tutorConfig.finalQuestionSilenceMs },
          };
        case "END_SESSION_AFTER_CLOSING":
          return {
            runtime: {
              ...runtime,
              state: "ENDED",
              isAiSpeaking: false,
              pendingUtterance: null,
              afterSpeech: null,
              endedAt: new Date().toISOString(),
            },
            effect: { kind: "PERSIST_SUMMARY" },
          };
        default:
          return noEffect(runtime);
      }
    }

    case "SILENCE_TIMEOUT": {
      if (runtime.state === "WAITING_READY") {
        return enterAdvanceResult(runtime, lesson, { type: "segment", stepIndex: 0, segIndex: 0 });
      }
      if (runtime.state === "CHECKPOINT") {
        return speak(runtime, {}, checkpointContinueScript, "ADVANCE_AFTER_CHECKPOINT");
      }
      if (runtime.state === "INTERRUPTED" && runtime.interruptionReason === "NOISE") {
        return speak(runtime, {}, noiseContinueScript, "RESUME_SEGMENT");
      }
      if (runtime.state === "FINAL_QA") {
        return speak(runtime, {}, closingScript, "END_SESSION_AFTER_CLOSING");
      }
      return noEffect(runtime);
    }

    case "TOGGLE_MIC":
      return { runtime: { ...runtime, isMicEnabled: !runtime.isMicEnabled }, effect: { kind: "NONE" } };

    case "TOGGLE_CAMERA":
      return { runtime: { ...runtime, isCameraEnabled: !runtime.isCameraEnabled }, effect: { kind: "NONE" } };

    case "DISCONNECT":
      return {
        runtime: { ...runtime, connectionStatus: "disconnected", disconnectedAt: new Date().toISOString() },
        effect: { kind: "PERSIST_PROGRESS" },
      };

    case "RECONNECT":
      return {
        runtime: { ...runtime, connectionStatus: "connected", disconnectedAt: undefined },
        effect: { kind: "NONE" },
      };

    case "LEAVE": {
      if (runtime.state === "ENDED" || runtime.state === "EXPIRED" || runtime.state === "PRE_JOIN") {
        return noEffect(runtime);
      }
      return {
        runtime: { ...runtime, state: "ENDED", isAiSpeaking: false, pendingUtterance: null, endedAt: new Date().toISOString() },
        effect: { kind: "PERSIST_SUMMARY" },
      };
    }

    default:
      return noEffect(runtime);
  }
}
