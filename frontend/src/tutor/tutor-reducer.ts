import type { SessionQuestion, TeachingSlide } from "@/types/domain";
import type { TutorEvent } from "@/tutor/intents";
import type { AfterSpeechAction, TutorEffect, TutorRuntime, TutorState } from "@/tutor/types";
import { closingScript, finalQuestionScript, introScript, notReadyScript } from "@/tutor/scripts";
import { QUESTION_FAILED_TEXT } from "@/config/response-texts";

export type TutorContext = {
  slides: TeachingSlide[];
  introWaitMs: number;
  breathPauseMs: number;
  finalQuestionWaitMs: number;
  /**
   * Where the lesson picks up when it starts - 0 for someone new, the slide they left off on for
   * someone coming back after a closed tab or a dropped connection (CORE_FEATURE_SPEC §2.4).
   *
   * Carried in the context rather than in an event payload so the reducer stays a pure function
   * of (runtime, event, context): resuming is a property of who is learning, not of the START
   * they just pressed.
   */
  resumeSlideIndex: number;
};

export function createInitialRuntime(): TutorRuntime {
  return {
    state: "idle",
    currentSlideIndex: 0,
    answerSlideIndex: null,
    interruptedFrom: null,
    isMicEnabled: true,
    isCameraEnabled: false,
    isAiSpeaking: false,
    afterSpeech: null,
    questions: [],
    pausedFrom: null,
    errorMessage: null,
    completedAllSlides: false,
    micNotice: null,
    failedQuestionText: null,
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
  withFoundLead = false,
): { runtime: TutorRuntime; effect: TutorEffect } {
  return {
    runtime: { ...runtime, ...patch, state, afterSpeech, isAiSpeaking: true },
    effect: withFoundLead ? { kind: "SPEAK", text, withFoundLead: true } : { kind: "SPEAK", text },
  };
}

// TQ-24/U1 - "ready" is deliberately absent: readiness has exactly one answer path left (the
// button pair in TQ-18), so push-to-talk (and SUBMIT_TEXT_QUESTION, which shares this list) must
// not accept anything while the room is still waiting on that answer.
const PUSH_TO_TALK_STATES: TutorState[] = ["slide-speaking", "waiting-slide-duration", "final-question-window"];
const PAUSABLE_STATES: TutorState[] = ["ready", "slide-speaking", "waiting-slide-duration", "final-question-window"];

function loadSlide(runtime: TutorRuntime, slideIndex: number): { runtime: TutorRuntime; effect: TutorEffect } {
  return {
    runtime: {
      ...runtime,
      state: "slide-loading",
      currentSlideIndex: slideIndex,
      answerSlideIndex: null,
      isAiSpeaking: false,
      afterSpeech: null,
    },
    effect: { kind: "LOAD_SLIDE", slideIndex },
  };
}

/**
 * The lesson's opening slide. Two different things reach it - pressing the start button and the
 * intro timing out - so the resume point is resolved here once instead of at each of them, which
 * is how the first version of this shipped with one of the two still hardcoded to 0. (A third
 * caller - answering "พร้อมแล้ว" out loud - existed before U1 removed readiness-by-voice; TQ-25.)
 *
 * Clamped because a deck can lose slides between two visits: resuming past the end would load
 * nothing at all.
 */
function startFirstSlide(runtime: TutorRuntime, ctx: TutorContext): { runtime: TutorRuntime; effect: TutorEffect } {
  const lastIndex = Math.max(0, ctx.slides.length - 1);
  return loadSlide(runtime, Math.min(Math.max(0, ctx.resumeSlideIndex), lastIndex));
}

function restartCurrentSlide(
  runtime: TutorRuntime,
  withResumeBridge = false,
): { runtime: TutorRuntime; effect: TutorEffect } {
  return {
    // answerSlideIndex is dropped here - this is the "go back to where we left off" step
    // after an answer that referenced another slide.
    runtime: { ...runtime, state: "restarting-slide", answerSlideIndex: null },
    effect: { kind: "LOAD_SLIDE", slideIndex: runtime.currentSlideIndex, withResumeBridge },
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
  // TQ-24 (U1) - interruptedFrom is now only ever set by PUSH_TO_TALK_START or
  // SUBMIT_TEXT_QUESTION, neither of which accepts state "ready" any more, so there is no
  // "the readiness prompt got interrupted" case left to handle here.
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
      return speak(runtime, "intro-speaking", "ENTER_READY", introScript());
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
        case "RESUME_AFTER_ANSWER":
          return restartCurrentSlide({ ...runtime, isAiSpeaking: false, afterSpeech: null }, true);
        case "AWAIT_READINESS":
          return {
            runtime: { ...runtime, state: "ready", isAiSpeaking: false, afterSpeech: null },
            effect: { kind: "NONE" },
          };
        case "WAIT_FINAL_QUESTION":
          return {
            runtime: {
              ...runtime,
              state: "final-question-window",
              answerSlideIndex: null,
              isAiSpeaking: false,
              afterSpeech: null,
            },
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
      return startFirstSlide(runtime, ctx);
    }

    case "NOT_READY": {
      // TQ-18 (U1) - the sole remaining producer of AWAIT_READINESS, now that voice/typed
      // replies to the readiness prompt are gone. No auto-start timer: the recipient just said
      // they need a moment, so starting anyway a few seconds later would ignore that answer.
      if (runtime.state !== "ready") return noEffect(runtime);
      return speak(runtime, "answer-speaking", "AWAIT_READINESS", notReadyScript);
    }

    case "INTRO_TIMEOUT": {
      if (runtime.state !== "ready") return noEffect(runtime);
      return startFirstSlide(runtime, ctx);
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
        runtime: {
          ...runtime,
          interruptedFrom: runtime.state,
          state: "push-to-talk-recording",
          isAiSpeaking: false,
          afterSpeech: null,
          micNotice: null,
        },
        effect: { kind: "START_RECORDING" },
      };
    }

    case "PUSH_TO_TALK_END": {
      if (runtime.state !== "push-to-talk-recording") return noEffect(runtime);
      return { runtime: { ...runtime, state: "processing-question" }, effect: { kind: "STOP_RECORDING_AND_SEND" } };
    }

    case "SUBMIT_TEXT_QUESTION": {
      // TQ-14 - same allowed states as push-to-talk, minus "ready" (already absent from
      // PUSH_TO_TALK_STATES per TQ-24/U1) and minus "push-to-talk-recording" itself: there is
      // nothing to interrupt-and-record for a typed question.
      if (!PUSH_TO_TALK_STATES.includes(runtime.state)) return noEffect(runtime);
      return {
        runtime: {
          ...runtime,
          interruptedFrom: runtime.state,
          state: "processing-question",
          isAiSpeaking: false,
          afterSpeech: null,
          micNotice: null,
        },
        effect: { kind: "SEND_TEXT_QUESTION", text: event.text },
      };
    }

    case "NO_SPEECH": {
      if (runtime.state !== "processing-question") return noEffect(runtime);
      // Nothing was said, so say nothing back - resuming quietly is the right call here.
      return resumeAfterInterruption(runtime, ctx);
    }

    case "MIC_UNAVAILABLE": {
      if (runtime.state !== "push-to-talk-recording") return noEffect(runtime);
      // Recoverable, unlike FAIL: the lesson resumes exactly where push-to-talk interrupted it
      // (same recovery path as NO_SPEECH) - only a permission/device problem, not a broken
      // session. The message rides along on the runtime so the UI can show a dismissible notice
      // instead of nothing happening at all.
      return resumeAfterInterruption({ ...runtime, micNotice: event.message }, ctx);
    }

    case "CLEAR_MIC_NOTICE":
      return { runtime: { ...runtime, micNotice: null }, effect: { kind: "NONE" } };

    case "CLEAR_FAILED_QUESTION_TEXT":
      return { runtime: { ...runtime, failedQuestionText: null }, effect: { kind: "NONE" } };

    case "QUESTION_FAILED": {
      if (runtime.state !== "processing-question") return noEffect(runtime);
      // A real failure DOES get spoken. Silently resuming looks identical to a broken
      // push-to-talk button, which is how an expired API key stayed hidden for hours.
      const afterSpeech: AfterSpeechAction = runtime.completedAllSlides ? "WAIT_FINAL_QUESTION" : "RESTART_SLIDE";
      // QA-03 - only a typed-question failure carries text worth giving back to the drawer;
      // event.text is undefined for a failed voice question, which normalizes to null here.
      return speak(runtime, "answer-speaking", afterSpeech, QUESTION_FAILED_TEXT, {
        failedQuestionText: event.text ?? null,
      });
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
        source: event.source,
      };
      const afterSpeech: AfterSpeechAction = runtime.completedAllSlides
        ? "WAIT_FINAL_QUESTION"
        : "RESUME_AFTER_ANSWER";
      // If the answer is grounded in another slide, show that slide while it's being read
      // out. currentSlideIndex is untouched, so RESTART_SLIDE brings us right back.
      const referencedIndex = ctx.slides.findIndex((slide) => slide.slideObjectId === event.relatedSlideObjectId);
      const answerSlideIndex =
        event.relatedSlideObjectId && referencedIndex >= 0 && referencedIndex !== runtime.currentSlideIndex
          ? referencedIndex
          : null;
      return speak(
        runtime,
        "answer-speaking",
        afterSpeech,
        event.answer,
        { questions: [...runtime.questions, questionRecord], answerSlideIndex },
        // Only when something was actually found - "เจอแล้ว" ahead of "ไม่พบข้อมูล" would
        // contradict the very next sentence.
        event.answerStatus === "answered",
      );
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
