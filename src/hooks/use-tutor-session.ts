"use client";

import { useCallback, useEffect, useReducer, useRef } from "react";
import type { TrainingSession } from "@/types/domain";
import type { TutorAction, TutorUserAction } from "@/tutor/intents";
import type { TutorEffect, TutorRuntime } from "@/tutor/types";
import { createInitialRuntime, tutorReducer } from "@/tutor/tutor-reducer";
import { aiAnswerProvider } from "@/providers/ai";
import { textToSpeechProvider } from "@/providers/tts";
import { speechToTextProvider } from "@/providers/stt";
import { sessionRepository, reportRepository } from "@/providers/data";
import type { SessionSummary } from "@/types/domain";

function buildInitialRuntime(session: TrainingSession): TutorRuntime {
  const base = createInitialRuntime(session.id, session.teacherName);
  if (!session.startedAt) {
    return base;
  }
  const lesson = session.lessonSnapshot;
  const stepIndex = Math.min(session.lastStepIndex, lesson.steps.length - 1);
  const step = lesson.steps[stepIndex];
  const segIndex = Math.min(session.lastSegmentIndex, step.segments.length - 1);
  const segment = step.segments[segIndex];
  return {
    ...base,
    state: "TEACHING",
    currentStepIndex: stepIndex,
    currentSegmentIndex: segIndex,
    activeMediaId: segment.mediaId,
    startedAt: session.startedAt,
    isAiSpeaking: true,
    pendingUtterance: segment.scriptText,
    pendingDurationMs: segment.mockSpeakDurationMs,
    afterSpeech: "ADVANCE_TEACHING",
  };
}

export function useTutorSession(session: TrainingSession) {
  const lesson = session.lessonSnapshot;
  const runtimeRef = useRef<TutorRuntime>(buildInitialRuntime(session));
  const [, forceRender] = useReducer((n: number) => n + 1, 0);
  const ttsAbortRef = useRef<AbortController | null>(null);
  const silenceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const mountedRef = useRef(true);

  const persistProgress = useCallback(() => {
    const runtime = runtimeRef.current;
    void sessionRepository.update({
      ...session,
      startedAt: runtime.startedAt ?? session.startedAt,
      lastStepIndex: runtime.currentStepIndex,
      lastSegmentIndex: runtime.currentSegmentIndex,
      disconnectedAt: runtime.disconnectedAt,
    });
  }, [session]);

  const persistSummaryAndEnd = useCallback(() => {
    const runtime = runtimeRef.current;
    const summary: SessionSummary = {
      sessionId: session.id,
      completedAllSteps: runtime.completedAllSteps,
      lastStepIndex: runtime.currentStepIndex,
      lastStepTitle: lesson.steps[runtime.currentStepIndex]?.title,
      questions: runtime.questions,
      repeatedPoints: runtime.repeatedPoints,
      unresolvedItems: runtime.unresolvedItems,
      startedAt: runtime.startedAt,
      endedAt: runtime.endedAt ?? new Date().toISOString(),
    };
    void reportRepository.save(summary);
    void sessionRepository.update({
      ...session,
      startedAt: runtime.startedAt ?? session.startedAt,
      endedAt: summary.endedAt,
      completedAllSteps: runtime.completedAllSteps,
      lastStepIndex: runtime.currentStepIndex,
      lastSegmentIndex: runtime.currentSegmentIndex,
    });
  }, [session, lesson]);

  const runEffect = useCallback(
    (effect: TutorEffect) => {
      switch (effect.kind) {
        case "SPEAK": {
          const controller = new AbortController();
          ttsAbortRef.current = controller;
          textToSpeechProvider
            .speak(effect.text, { signal: controller.signal, durationMs: effect.durationMs })
            .then(() => {
              if (!controller.signal.aborted && mountedRef.current) {
                dispatch({ type: "SPEECH_DONE" });
              }
            })
            .catch(() => {
              // Superseded by a newer action (pause/interrupt) - nothing to do.
            });
          break;
        }
        case "WAIT_SILENCE": {
          silenceTimerRef.current = setTimeout(() => {
            if (mountedRef.current) {
              dispatch({ type: "SILENCE_TIMEOUT" });
            }
          }, effect.ms);
          break;
        }
        case "CALL_AI": {
          aiAnswerProvider
            .answer({
              lessonSnapshot: lesson,
              currentStepIndex: runtimeRef.current.currentStepIndex,
              currentSegmentIndex: runtimeRef.current.currentSegmentIndex,
              question: effect.question,
            })
            .then((result) => {
              if (mountedRef.current) {
                dispatch({ type: "ANSWER_READY", result });
              }
            });
          break;
        }
        case "PERSIST_PROGRESS":
          persistProgress();
          break;
        case "PERSIST_SUMMARY":
          persistSummaryAndEnd();
          break;
        case "NONE":
        default:
          break;
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [lesson, persistProgress, persistSummaryAndEnd],
  );

  const dispatch = useCallback(
    (action: TutorAction) => {
      ttsAbortRef.current?.abort();
      ttsAbortRef.current = null;
      if (silenceTimerRef.current) {
        clearTimeout(silenceTimerRef.current);
        silenceTimerRef.current = null;
      }
      const { runtime: next, effect } = tutorReducer(runtimeRef.current, action, lesson);
      runtimeRef.current = next;
      forceRender();
      runEffect(effect);
      if (next.startedAt && effect.kind !== "PERSIST_PROGRESS" && effect.kind !== "PERSIST_SUMMARY") {
        persistProgress();
      }
    },
    [lesson, runEffect, persistProgress],
  );

  useEffect(() => {
    mountedRef.current = true;
    void speechToTextProvider.start((text) => dispatch({ type: "ASK_QUESTION", question: text }));

    if (runtimeRef.current.state === "PRE_JOIN") {
      dispatch({ type: "JOIN_ROOM", teacherName: session.teacherName });
    } else if (runtimeRef.current.pendingUtterance) {
      runEffect({ kind: "SPEAK", text: runtimeRef.current.pendingUtterance, durationMs: runtimeRef.current.pendingDurationMs });
    }

    return () => {
      mountedRef.current = false;
      ttsAbortRef.current?.abort();
      if (silenceTimerRef.current) {
        clearTimeout(silenceTimerRef.current);
      }
      void speechToTextProvider.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const sendAction = useCallback((action: TutorUserAction) => dispatch(action), [dispatch]);

  const submitChatMessage = useCallback((text: string) => {
    speechToTextProvider.pushTranscript(text);
  }, []);

  return {
    runtime: runtimeRef.current,
    lesson,
    sendAction,
    submitChatMessage,
  };
}
