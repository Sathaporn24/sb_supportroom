import type { SessionQuestion, TeachingSlide } from "@/types/domain";
import type { TutorEvent } from "@/tutor/intents";
import type { AfterSpeechAction, TutorEffect, TutorRuntime, TutorState } from "@/tutor/types";
import { closingScript, finalQuestionScript, introScript } from "@/tutor/scripts";

export type TutorContext = {
  slides: TeachingSlide[];
  introWaitMs: number;
  breathPauseMs: number;
  finalQuestionWaitMs: number;
  teacherName?: string;
};

export function createInitialRuntime(): TutorRuntime {
  return {
    state: "idle",
    currentSlideIndex: 0,
    isMicEnabled: true,
    isCameraEnabled: false,
    isAiSpeaking: false,
    afterSpeech: null,
    questions: [],
    pausedFrom: null,
    errorMessage: null,
    completedAllSlides: false,
  };
}

function noEffect(runtime: TutorRuntime): { runtime: TutorRuntime; effect: TutorEffect } {
  return { runtime, effect: { kind: "NONE" } };
}

function speak(
  runtime: TutorRuntime,
  state: TutorState,
  afterSpeech: AfterSpeechAction,
  text: string,
  patch: Partial<TutorRuntime> = {},
): { runtime: TutorRuntime; effect: TutorEffect } {
  return {
    runtime: { ...runtime, ...patch, state, afterSpeech, isAiSpeaking: true },
    effect: { kind: "SPEAK", text },
  };
}

const PUSH_TO_TALK_STATES: TutorState[] = ["slide-speaking", "waiting-slide-duration", "final-question-window"];
const PAUSABLE_STATES: TutorState[] = ["ready", "slide-speaking", "waiting-slide-duration", "final-question-window"];

function loadSlide(runtime: TutorRuntime, slideIndex: number): { runtime: TutorRuntime; effect: TutorEffect } {
  return {
    runtime: {
      ...runtime,
      state: "slide-loading",
      currentSlideIndex: slideIndex,
      isAiSpeaking: false,
      afterSpeech: null,
    },
    effect: { kind: "LOAD_SLIDE", slideIndex },
  };
}

function restartCurrentSlide(runtime: TutorRuntime): { runtime: TutorRuntime; effect: TutorEffect } {
  return {
    runtime: { ...runtime, state: "restarting-slide" },
    effect: { kind: "LOAD_SLIDE", slideIndex: runtime.currentSlideIndex },
  };
}

function advanceToNextSlideOrFinish(
  runtime: TutorRuntime,
  ctx: TutorContext,
): { runtime: TutorRuntime; effect: TutorEffect } {
  const nextIndex = runtime.currentSlideIndex + 1;
  if (nextIndex >= ctx.slides.length) {
    return speak(runtime, "intro-speaking", "WAIT_FINAL_QUESTION", finalQuestionScript, { completedAllSlides: true });
  }
  return loadSlide(runtime, nextIndex);
}

function resumeAfterInterruption(
  runtime: TutorRuntime,
  ctx: TutorContext,
): { runtime: TutorRuntime; effect: TutorEffect } {
  if (runtime.completedAllSlides) {
    return {
      runtime: { ...runtime, state: "final-question-window" },
      effect: { kind: "WAIT_FINAL_QUESTION", ms: ctx.finalQuestionWaitMs },
    };
  }
  return restartCurrentSlide(runtime);
}

export function tutorReducer(
  runtime: TutorRuntime,
  event: TutorEvent,
  ctx: TutorContext,
): { runtime: TutorRuntime; effect: TutorEffect } {
  switch (event.type) {
    case "JOIN": {
      if (runtime.state !== "idle") return noEffect(runtime);
      return { runtime: { ...runtime, state: "preparing" }, effect: { kind: "LOAD_LESSON" } };
    }

    case "LESSON_LOADED": {
      if (runtime.state !== "preparing") return noEffect(runtime);
      return speak(runtime, "intro-speaking", "ENTER_READY", introScript(ctx.teacherName));
    }

    case "LESSON_LOAD_FAILED": {
      return { runtime: { ...runtime, state: "error", errorMessage: event.message }, effect: { kind: "NONE" } };
    }

    case "TTS_ENDED": {
      switch (runtime.afterSpeech) {
        case "ENTER_READY":
          return {
            runtime: { ...runtime, state: "ready", isAiSpeaking: false, afterSpeech: null },
            effect: { kind: "WAIT_READY_TIMEOUT", ms: ctx.introWaitMs },
          };
        case "WAIT_SLIDE_DURATION": {
          const slide = ctx.slides[runtime.currentSlideIndex];
          const remaining = Math.max(0, (slide?.videoDurationMs ?? 0) - event.elapsedMs);
          return {
            runtime: { ...runtime, state: "waiting-slide-duration", isAiSpeaking: false, afterSpeech: null },
            effect: { kind: "WAIT_REMAINING", ms: remaining + ctx.breathPauseMs },
          };
        }
        case "RESTART_SLIDE":
          return restartCurrentSlide({ ...runtime, isAiSpeaking: false, afterSpeech: null });
        case "WAIT_FINAL_QUESTION":
          return {
            runtime: { ...runtime, state: "final-question-window", isAiSpeaking: false, afterSpeech: null },
            effect: { kind: "WAIT_FINAL_QUESTION", ms: ctx.finalQuestionWaitMs },
          };
        case "FINISH_SESSION":
          return {
            runtime: { ...runtime, state: "completed", isAiSpeaking: false, afterSpeech: null },
            effect: { kind: "PERSIST_END", completedAllSlides: runtime.completedAllSlides },
          };
        default:
          return noEffect(runtime);
      }
    }

    case "START": {
      if (runtime.state !== "ready") return noEffect(runtime);
      return loadSlide(runtime, 0);
    }

    case "INTRO_TIMEOUT": {
      if (runtime.state !== "ready") return noEffect(runtime);
      return loadSlide(runtime, 0);
    }

    case "SLIDE_READY": {
      if (runtime.state !== "slide-loading" && runtime.state !== "restarting-slide") return noEffect(runtime);
      return {
        runtime: { ...runtime, state: "slide-speaking", afterSpeech: "WAIT_SLIDE_DURATION", isAiSpeaking: true },
        effect: { kind: "NONE" },
      };
    }

    case "SLIDE_DURATION_ENDED": {
      if (runtime.state !== "waiting-slide-duration") return noEffect(runtime);
      return advanceToNextSlideOrFinish(runtime, ctx);
    }

    case "NEXT_SLIDE": {
      if (runtime.state !== "waiting-slide-duration") return noEffect(runtime);
      return advanceToNextSlideOrFinish(runtime, ctx);
    }

    case "PUSH_TO_TALK_START": {
      if (!PUSH_TO_TALK_STATES.includes(runtime.state)) return noEffect(runtime);
      return {
        runtime: { ...runtime, state: "push-to-talk-recording", isAiSpeaking: false, afterSpeech: null },
        effect: { kind: "START_RECORDING" },
      };
    }

    case "PUSH_TO_TALK_END": {
      if (runtime.state !== "push-to-talk-recording") return noEffect(runtime);
      return { runtime: { ...runtime, state: "processing-question" }, effect: { kind: "STOP_RECORDING_AND_SEND" } };
    }

    case "NO_SPEECH":
    case "QUESTION_FAILED": {
      if (runtime.state !== "processing-question") return noEffect(runtime);
      return resumeAfterInterruption(runtime, ctx);
    }

    case "QUESTION_ANSWERED": {
      if (runtime.state !== "processing-question") return noEffect(runtime);
      const questionRecord: SessionQuestion = {
        id: `local-${Date.now()}`,
        sessionId: "",
        slideObjectId: event.relatedSlideObjectId,
        transcript: event.transcript,
        answer: event.answer,
        answerStatus: event.answerStatus,
        createdAt: new Date().toISOString(),
      };
      const afterSpeech: AfterSpeechAction = runtime.completedAllSlides ? "WAIT_FINAL_QUESTION" : "RESTART_SLIDE";
      return speak(runtime, "answer-speaking", afterSpeech, event.answer, {
        questions: [...runtime.questions, questionRecord],
      });
    }

    case "RESTART_CURRENT_SLIDE":
      return restartCurrentSlide(runtime);

    case "FINAL_QUESTION_TIMEOUT": {
      if (runtime.state !== "final-question-window") return noEffect(runtime);
      return speak(runtime, "intro-speaking", "FINISH_SESSION", closingScript);
    }

    case "PAUSE": {
      if (!PAUSABLE_STATES.includes(runtime.state)) return noEffect(runtime);
      return {
        runtime: { ...runtime, pausedFrom: runtime.state, state: "paused", isAiSpeaking: false, afterSpeech: null },
        effect: { kind: "NONE" },
      };
    }

    case "RESUME": {
      if (runtime.state !== "paused" || !runtime.pausedFrom) return noEffect(runtime);
      const from = runtime.pausedFrom;
      const base: TutorRuntime = { ...runtime, pausedFrom: null };
      if (from === "ready") {
        return { runtime: { ...base, state: "ready" }, effect: { kind: "WAIT_READY_TIMEOUT", ms: ctx.introWaitMs } };
      }
      if (from === "final-question-window") {
        return {
          runtime: { ...base, state: "final-question-window" },
          effect: { kind: "WAIT_FINAL_QUESTION", ms: ctx.finalQuestionWaitMs },
        };
      }
      return restartCurrentSlide(base);
    }

    case "END_SESSION": {
      if (runtime.state === "completed") return noEffect(runtime);
      return {
        runtime: { ...runtime, state: "completed", isAiSpeaking: false, afterSpeech: null },
        effect: { kind: "PERSIST_END", completedAllSlides: runtime.completedAllSlides },
      };
    }

    case "FAIL":
      return { runtime: { ...runtime, state: "error", errorMessage: event.message }, effect: { kind: "NONE" } };

    case "TOGGLE_MIC":
      return { runtime: { ...runtime, isMicEnabled: !runtime.isMicEnabled }, effect: { kind: "NONE" } };

    case "TOGGLE_CAMERA":
      return { runtime: { ...runtime, isCameraEnabled: !runtime.isCameraEnabled }, effect: { kind: "NONE" } };

    default:
      return noEffect(runtime);
  }
}
