"use client";

import { useCallback, useEffect, useReducer, useRef, useState } from "react";
import type { LessonConfig, TeachingSlide, TrainingSession } from "@/types/domain";
import type { TutorEvent, TutorUserEvent } from "@/tutor/intents";
import type { TutorEffect, TutorRuntime } from "@/tutor/types";
import { createInitialRuntime, tutorReducer, type TutorContext } from "@/tutor/tutor-reducer";
import * as api from "@/lib/api-client";

const MIN_RECORDING_MS = 300;

export function useTutorSession(session: TrainingSession) {
  const runtimeRef = useRef<TutorRuntime>(createInitialRuntime());
  const [, forceRender] = useReducer((n: number) => n + 1, 0);
  const [embedUrl, setEmbedUrl] = useState("");
  const [loadError, setLoadError] = useState<string | null>(null);

  const slidesRef = useRef<TeachingSlide[]>([]);
  const lessonRef = useRef<LessonConfig | null>(null);
  const ctxRef = useRef<TutorContext>({
    slides: [],
    introWaitMs: 5_000,
    breathPauseMs: 1_000,
    finalQuestionWaitMs: 5_000,
    teacherName: session.teacherName,
  });

  const audioRef = useRef<HTMLAudioElement | null>(null);
  const audioUrlRef = useRef<string | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const micStreamRef = useRef<MediaStream | null>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const recordedChunksRef = useRef<Blob[]>([]);
  const recordingStartRef = useRef(0);
  const mountedRef = useRef(true);
  /** In-flight startRecording(), so a fast release can wait for it instead of bailing out. */
  const pendingStartRef = useRef<Promise<void> | null>(null);

  // Plain closures (not useCallback) - this hook's own effect-runner calls them
  // directly each render, they are never passed as props needing referential
  // stability. Only `sendEvent` at the bottom is memoized for consumers.

  function clearPending() {
    if (audioRef.current) {
      audioRef.current.pause();
      audioRef.current.src = "";
      audioRef.current = null;
    }
    if (audioUrlRef.current) {
      URL.revokeObjectURL(audioUrlRef.current);
      audioUrlRef.current = null;
    }
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }

  function dispatch(event: TutorEvent) {
    clearPending();
    const { runtime: next, effect } = tutorReducer(runtimeRef.current, event, ctxRef.current);
    runtimeRef.current = next;
    forceRender();
    runEffect(effect);
  }

  async function playText(text: string) {
    try {
      const blob = await api.synthesizeSpeech(text);
      if (!mountedRef.current) return;
      const url = URL.createObjectURL(blob);
      audioUrlRef.current = url;
      const audio = new Audio(url);
      audioRef.current = audio;
      audio.addEventListener("ended", () => {
        const elapsedMs = Number.isFinite(audio.duration) ? audio.duration * 1000 : 0;
        dispatch({ type: "TTS_ENDED", elapsedMs });
      });
      await audio.play();
    } catch (err) {
      dispatch({ type: "FAIL", message: err instanceof Error ? err.message : "แปลงข้อความเป็นเสียงไม่สำเร็จ" });
    }
  }

  async function loadSlideAudio(slideIndex: number) {
    const slide = slidesRef.current[slideIndex];
    if (!slide) {
      dispatch({ type: "FAIL", message: "ไม่พบสไลด์ที่ต้องการ" });
      return;
    }
    try {
      const blob = await api.synthesizeSpeech(slide.speakerNotes);
      if (!mountedRef.current) return;
      // Dispatch the state transition FIRST - dispatch() always clears any pending
      // audio/timer via clearPending(), so building the <audio> element before this
      // would get its src wiped out by that same clear right before play() runs.
      dispatch({ type: "SLIDE_READY" });
      const url = URL.createObjectURL(blob);
      audioUrlRef.current = url;
      const audio = new Audio(url);
      audioRef.current = audio;
      audio.addEventListener("ended", () => {
        const elapsedMs = Number.isFinite(audio.duration) ? audio.duration * 1000 : 0;
        dispatch({ type: "TTS_ENDED", elapsedMs });
      });
      await audio.play();
    } catch (err) {
      dispatch({ type: "FAIL", message: err instanceof Error ? err.message : "เตรียมเสียงสไลด์ไม่สำเร็จ" });
    }
  }

  async function ensureMicStream(): Promise<MediaStream> {
    if (micStreamRef.current) {
      return micStreamRef.current;
    }
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    micStreamRef.current = stream;
    return stream;
  }

  async function startRecording() {
    try {
      const stream = await ensureMicStream();
      const recorder = new MediaRecorder(stream);
      recordedChunksRef.current = [];
      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) recordedChunksRef.current.push(e.data);
      };
      mediaRecorderRef.current = recorder;
      recorder.start();
    } catch (err) {
      dispatch({ type: "FAIL", message: err instanceof Error ? err.message : "ไม่สามารถเข้าถึงไมโครโฟนได้" });
    }
  }

  async function stopRecordingAndSend() {
    // A quick tap can release the button before getUserMedia has even resolved. Waiting on
    // the in-flight start means the press still records something; bailing out here (the
    // old behaviour) silently captured nothing at all and just resumed the slide.
    await pendingStartRef.current;
    pendingStartRef.current = null;

    const recorder = mediaRecorderRef.current;
    const durationMs = Date.now() - recordingStartRef.current;
    if (!recorder || recorder.state === "inactive") {
      dispatch({ type: "NO_SPEECH" });
      return;
    }

    const audioBlob = await new Promise<Blob>((resolve) => {
      recorder.addEventListener(
        "stop",
        () => resolve(new Blob(recordedChunksRef.current, { type: recorder.mimeType || "audio/webm" })),
        { once: true },
      );
      recorder.stop();
    });
    mediaRecorderRef.current = null;

    if (durationMs < MIN_RECORDING_MS) {
      dispatch({ type: "NO_SPEECH" });
      return;
    }

    try {
      const currentSlide = slidesRef.current[runtimeRef.current.currentSlideIndex];
      const result = await api.askVoiceQuestion({
        audioBlob,
        lessonSlug: session.lessonSlug,
        sessionId: session.id,
        currentSlideObjectId: currentSlide?.slideObjectId,
        durationMs,
      });

      if (!mountedRef.current) return;

      if (result.answerStatus === "no_speech" || result.answerStatus === "transcription_failed") {
        dispatch({ type: "NO_SPEECH" });
        return;
      }

      dispatch({
        type: "QUESTION_ANSWERED",
        transcript: result.transcript,
        answer: result.answer,
        answerStatus: result.answerStatus,
        relatedSlideObjectId: result.relatedSlideObjectId,
      });
    } catch {
      if (mountedRef.current) {
        dispatch({ type: "QUESTION_FAILED" });
      }
    }
  }

  async function persistEnd(completedAllSlides: boolean) {
    const lastSlide = slidesRef.current[runtimeRef.current.currentSlideIndex];
    try {
      await api.endSession(session.token, { completedAllSlides, lastSlideObjectId: lastSlide?.slideObjectId });
    } catch {
      // Best-effort - the room still shows ENDED locally even if persistence fails
      // (e.g. a transient network issue during a demo).
    }
  }

  function runEffect(effect: TutorEffect) {
    switch (effect.kind) {
      case "LOAD_LESSON":
        void (async () => {
          try {
            const { lesson, embedUrl: url, slides } = await api.getLessonBySlug(session.lessonSlug);
            lessonRef.current = lesson;
            slidesRef.current = slides;
            ctxRef.current = {
              slides,
              introWaitMs: lesson.introWaitMs,
              breathPauseMs: lesson.breathPauseMs,
              finalQuestionWaitMs: lesson.finalQuestionWaitMs,
              teacherName: session.teacherName,
            };
            if (mountedRef.current) {
              setEmbedUrl(url);
              dispatch({ type: "LESSON_LOADED" });
            }
          } catch (err) {
            if (mountedRef.current) {
              const message = err instanceof Error ? err.message : "โหลดบทเรียนไม่สำเร็จ";
              setLoadError(message);
              dispatch({ type: "LESSON_LOAD_FAILED", message });
            }
          }
        })();
        break;
      case "SPEAK":
        void playText(effect.text);
        break;
      case "WAIT_READY_TIMEOUT":
        timerRef.current = setTimeout(() => dispatch({ type: "INTRO_TIMEOUT" }), effect.ms);
        break;
      case "LOAD_SLIDE":
        void loadSlideAudio(effect.slideIndex);
        break;
      case "WAIT_REMAINING":
        timerRef.current = setTimeout(() => dispatch({ type: "SLIDE_DURATION_ENDED" }), effect.ms);
        break;
      case "START_RECORDING":
        // Stamped synchronously, before any await: measuring from recorder.start() instead
        // would charge a slow mic hand-off against the teacher's hold time and trip the
        // too-short check on a perfectly normal press.
        recordingStartRef.current = Date.now();
        pendingStartRef.current = startRecording();
        break;
      case "STOP_RECORDING_AND_SEND":
        void stopRecordingAndSend();
        break;
      case "WAIT_FINAL_QUESTION":
        timerRef.current = setTimeout(() => dispatch({ type: "FINAL_QUESTION_TIMEOUT" }), effect.ms);
        break;
      case "PERSIST_END":
        void persistEnd(effect.completedAllSlides);
        break;
      case "NONE":
      default:
        break;
    }
  }

  useEffect(() => {
    mountedRef.current = true;
    dispatch({ type: "JOIN" });
    void api.markSessionStarted(session.token).catch(() => undefined);
    // Get the permission prompt and device hand-off out of the way while the intro plays,
    // so the first press records instantly. Failures are ignored here - the first real
    // press retries and surfaces a proper error message if the mic is genuinely blocked.
    void ensureMicStream().catch(() => undefined);

    return () => {
      mountedRef.current = false;
      clearPending();
      mediaRecorderRef.current?.stream.getTracks().forEach((track) => track.stop());
      micStreamRef.current?.getTracks().forEach((track) => track.stop());
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // dispatch/runEffect close over refs + the stable `session` prop only, so freezing
  // to the mount-time closure here is safe and avoids recreating sendEvent every render.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const sendEvent = useCallback((event: TutorUserEvent) => dispatch(event), []);

  // While an answer references another slide, the room shows that slide instead - the
  // lesson position (currentSlideIndex) is unchanged and comes back after the answer.
  const { currentSlideIndex, answerSlideIndex } = runtimeRef.current;
  const displayedSlideIndex = answerSlideIndex ?? currentSlideIndex;

  return {
    runtime: runtimeRef.current,
    embedUrl,
    loadError,
    currentSlide: slidesRef.current[displayedSlideIndex],
    isShowingReferencedSlide: answerSlideIndex !== null,
    resumeSlideNumber: currentSlideIndex + 1,
    totalSlides: slidesRef.current.length,
    sendEvent,
  };
}
