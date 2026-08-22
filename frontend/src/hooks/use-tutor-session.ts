"use client";

import { useCallback, useEffect, useReducer, useRef, useState } from "react";
import type { LearningSession, TeachingSlide, PublicTrainingLink } from "@/types/domain";
import type { TutorEvent, TutorUserEvent } from "@/tutor/intents";
import type { TutorEffect, TutorRuntime } from "@/tutor/types";
import { createInitialRuntime, tutorReducer, type TutorContext } from "@/tutor/tutor-reducer";
import * as api from "@/lib/api-client";
import {
  ANSWER_FOUND_LEADS,
  PROCESSING_FILLER_GAP_MAX_MS,
  PROCESSING_FILLER_GAP_MIN_MS,
  PROCESSING_FILLER_STAGES,
  PROCESSING_FILLER_TEXTS,
  RESUME_BRIDGE_TEXTS,
} from "@/config/response-texts";

const MIN_RECORDING_MS = 300;

/** Persisted per-browser so the recipient's AI-volume preference survives a page reload. */
const AI_VOLUME_STORAGE_KEY = "sb_ai_volume";

/**
 * Wording variety lives here rather than in the reducer, which stays pure so the state
 * machine remains deterministic under test.
 */
function pickRandom<T>(items: readonly T[]): T {
  return items[Math.floor(Math.random() * items.length)];
}

/**
 * `link` says which lesson and which company; `learnerKey` says which of the many people on that
 * link this browser is. Every call that writes something down needs both - the token alone stopped
 * identifying a person once one link started serving a whole department.
 */
export function useTutorSession(link: PublicTrainingLink, learningSession: LearningSession, learnerKey: string) {
  const runtimeRef = useRef<TutorRuntime>(createInitialRuntime());
  const [, forceRender] = useReducer((n: number) => n + 1, 0);
  const [embedUrl, setEmbedUrl] = useState("");
  const [loadError, setLoadError] = useState<string | null>(null);

  const slidesRef = useRef<TeachingSlide[]>([]);
  const ctxRef = useRef<TutorContext>({
    slides: [],
    introWaitMs: 5_000,
    breathPauseMs: 500,
    finalQuestionWaitMs: 5_000,
    resumeSlideIndex: learningSession.lastSlideIndex ?? 0,
  });

  const audioRef = useRef<HTMLAudioElement | null>(null);
  const audioUrlRef = useRef<string | null>(null);
  /** AI playback volume, 0-1. Kept in a ref so every new <audio> can read the live value
   * synchronously at creation, and mirrored in state so the volume control re-renders. This
   * is a pure playback concern - it never touches the reducer/state machine (arch rule #3). */
  const volumeRef = useRef(1);
  const [volume, setVolumeState] = useState(1);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const micStreamRef = useRef<MediaStream | null>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const recordedChunksRef = useRef<Blob[]>([]);
  const recordingStartRef = useRef(0);
  const mountedRef = useRef(true);
  /** In-flight startRecording(), so a fast release can wait for it instead of bailing out. */
  const pendingStartRef = useRef<Promise<void> | null>(null);
  /** Pre-synthesized "please wait" lines - see prefetchFillers() / playProcessingFiller(). */
  const fillerBlobsRef = useRef<Blob[]>([]);
  /** One bucket per PROCESSING_FILLER_STAGES rung, kept in stage order. */
  const stageBlobsRef = useRef<Blob[][]>(PROCESSING_FILLER_STAGES.map(() => []));
  /** Separate from timerRef: this paces the filler chain, not a state-machine transition. */
  const fillerTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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
    if (fillerTimerRef.current) {
      clearTimeout(fillerTimerRef.current);
      fillerTimerRef.current = null;
    }
  }

  /** Sets AI playback volume (0-1). Applies immediately to the clip playing right now, so the
   * recipient can turn the AI down mid-sentence WITHOUT interrupting it (unlike push-to-talk,
   * which stops playback entirely). Persisted so the choice sticks across reloads. */
  function changeVolume(next: number) {
    const clamped = Math.min(1, Math.max(0, Number.isFinite(next) ? next : 1));
    volumeRef.current = clamped;
    setVolumeState(clamped);
    if (audioRef.current) audioRef.current.volume = clamped;
    try {
      window.localStorage.setItem(AI_VOLUME_STORAGE_KEY, String(clamped));
    } catch {
      // localStorage can throw (private mode, storage disabled) - volume still works this session.
    }
  }

  function dispatch(event: TutorEvent) {
    clearPending();
    const { runtime: next, effect } = tutorReducer(runtimeRef.current, event, ctxRef.current);
    runtimeRef.current = next;
    forceRender();
    runEffect(effect);
  }

  async function playText(text: string, withFoundLead = false) {
    try {
      // The lead rides along in the same synthesis request as the answer, so it flows into
      // it rather than landing as a separate clip with an audible seam.
      const lead = withFoundLead ? `${pickRandom(ANSWER_FOUND_LEADS)} ` : "";
      const blob = await api.synthesizeSpeech(`${lead}${text}`, link.token, learnerKey);
      if (!mountedRef.current) return;
      const url = URL.createObjectURL(blob);
      audioUrlRef.current = url;
      const audio = new Audio(url);
      audio.volume = volumeRef.current;
      audioRef.current = audio;
      audio.addEventListener("ended", () => {
        const elapsedMs = Number.isFinite(audio.duration) ? audio.duration * 1000 : 0;
        dispatch({ type: "TTS_ENDED", elapsedMs });
      });
      await audio.play();
    } catch {
      // Same graceful-degradation as loadSlideAudio below: a flaky TTS service (Edge Read-Aloud
      // can hang for tens of seconds under load before failing) must not take the whole session
      // down. This path carries the intro/answer/closing lines - previously the only one still
      // wired to FAIL, so a TTS hiccup here (confirmed live: real 502s after 24-46s) dropped the
      // whole room into the dead-end error screen. The state machine only needs TTS_ENDED to
      // advance; it doesn't care whether audio actually played.
      if (!mountedRef.current) return;
      dispatch({ type: "TTS_ENDED", elapsedMs: 0 });
    }
  }

  async function loadSlideAudio(slideIndex: number, withResumeBridge = false) {
    const slide = slidesRef.current[slideIndex];
    if (!slide) {
      dispatch({ type: "FAIL", message: "ไม่พบสไลด์ที่ต้องการ" });
      return;
    }
    try {
      // The hand-back line rides along in the same synthesis request rather than being a
      // second clip: one round-trip, and the sentence flows into the narration instead of
      // landing after an audible seam.
      const bridge = withResumeBridge ? `${pickRandom(RESUME_BRIDGE_TEXTS)} ` : "";
      const combinedText = `${bridge}${slide.speakerNotes}`;
      if (!combinedText.trim()) {
        // An image-only/blank slide (common for PDF lessons) has no speaker notes to say -
        // the backend rejects empty text outright (400), so skip narration instead of
        // treating "nothing to say" as a synthesis failure that halts the whole lesson.
        dispatch({ type: "SLIDE_READY" });
        dispatch({ type: "TTS_ENDED", elapsedMs: 0 });
        return;
      }
      const blob = await api.synthesizeSpeech(combinedText, link.token, learnerKey);
      if (!mountedRef.current) return;
      // Dispatch the state transition FIRST - dispatch() always clears any pending
      // audio/timer via clearPending(), so building the <audio> element before this
      // would get its src wiped out by that same clear right before play() runs.
      dispatch({ type: "SLIDE_READY" });
      const url = URL.createObjectURL(blob);
      audioUrlRef.current = url;
      const audio = new Audio(url);
      audio.volume = volumeRef.current;
      audioRef.current = audio;
      audio.addEventListener("ended", () => {
        const elapsedMs = Number.isFinite(audio.duration) ? audio.duration * 1000 : 0;
        dispatch({ type: "TTS_ENDED", elapsedMs });
      });
      await audio.play();
    } catch {
      // A flaky/slow TTS service (Edge Read-Aloud throttles under bursts and can hang for tens
      // of seconds before failing) must not take the whole lesson down. Fall back to showing the
      // slide silently for its normal duration - the same graceful path as a notes-less slide
      // above - instead of dispatching FAIL, which drops the entire session into an error state.
      if (!mountedRef.current) return;
      dispatch({ type: "SLIDE_READY" });
      dispatch({ type: "TTS_ENDED", elapsedMs: 0 });
    }
  }

  /**
   * Synthesizes the "please wait" lines ahead of time, during the intro, so the filler can
   * start the instant the button is released. Synthesizing on demand would cost ~1s of the
   * very silence this exists to cover.
   *
   * Sequential rather than parallel (15 simultaneous TTS calls is a good way to get rate
   * limited), and each blob is usable the moment it lands - a question asked early just
   * draws from a smaller pool instead of waiting for the whole set.
   */
  async function prefetchFillers() {
    // A random subset per session, so repeat sessions don't open with the same line.
    const shuffled = [...PROCESSING_FILLER_TEXTS].sort(() => Math.random() - 0.5).slice(0, 5);
    type Job = { text: string; rate?: string; push: (blob: Blob) => void };
    const jobs: Job[] = shuffled.map((text) => ({ text, push: (blob) => fillerBlobsRef.current.push(blob) }));
    // Two variants per stage rather than all of them: enough that a rung heard twice in one
    // wait isn't identical, without tripling the number of calls made during the intro.
    PROCESSING_FILLER_STAGES.forEach((stage, stageIndex) => {
      for (const { text, rate } of [...stage].sort(() => Math.random() - 0.5).slice(0, 2)) {
        // Slowing is the only way to make a hum drawl; stages 2 and 3 are sentences and
        // stay at lesson pace.
        jobs.push({ text, rate, push: (blob) => stageBlobsRef.current[stageIndex].push(blob) });
      }
    });

    // In need-order, so a question asked early still finds the opening line and stage 1
    // ready even if the later rungs haven't landed yet.
    for (const { text, rate, push } of jobs) {
      const blob = await api.synthesizeSpeech(text, link.token, learnerKey, rate).catch(() => null);
      if (!mountedRef.current) return;
      if (blob) push(blob);
    }
  }

  /**
   * Covers the wait between releasing the talk button and the answer arriving.
   * Deliberately does NOT dispatch TTS_ENDED - this is filler, not part of the lesson
   * script, so the state machine must not react to it. Whatever dispatches next (the
   * answer, a navigation, a failure) runs clearPending() and cuts it off mid-sentence,
   * which is exactly right: the real answer should never queue behind the filler.
   */
  function playProcessingFiller(stage = 0) {
    // Stage 0 is the opening acknowledgement; 1+ index into the waiting-sound rungs. A wait
    // long enough to run off the end sits on the last rung rather than looping back to a
    // hum - going quiet again after "ใกล้ได้แล้ว" would sound like progress reversing.
    const pool =
      stage === 0
        ? fillerBlobsRef.current
        : (stageBlobsRef.current[Math.min(stage, stageBlobsRef.current.length) - 1] ?? []);
    // A rung whose prefetch hasn't landed yet is skipped rather than waited on, so the
    // chain keeps moving forward through the stages instead of stalling on a missing clip.
    // Only forward to a rung that actually exists - once past the last one an empty pool
    // means nothing was ever synthesized, and rescheduling would just spin a timer.
    if (!pool.length) {
      if (stage < stageBlobsRef.current.length) scheduleNextFiller(stage + 1);
      return;
    }

    if (audioUrlRef.current) {
      URL.revokeObjectURL(audioUrlRef.current);
      audioUrlRef.current = null;
    }
    const url = URL.createObjectURL(pickRandom(pool));
    audioUrlRef.current = url;
    const audio = new Audio(url);
    audio.volume = volumeRef.current;
    audioRef.current = audio;
    // Keep topping the silence up for as long as we're still waiting, but leave a beat
    // between clips - chaining them back to back sounds like one unbroken stream rather
    // than someone pausing to work.
    audio.addEventListener("ended", () => scheduleNextFiller(stage + 1));
    void audio.play().catch(() => undefined);
  }

  function scheduleNextFiller(stage: number) {
    if (runtimeRef.current.state !== "processing-question") return;
    // Jittered so the sounds don't fall into a metronome. Both ends are checked because
    // the answer can land during the gap, at which point dispatch() clears this timer.
    const gap =
      PROCESSING_FILLER_GAP_MIN_MS + Math.random() * (PROCESSING_FILLER_GAP_MAX_MS - PROCESSING_FILLER_GAP_MIN_MS);
    fillerTimerRef.current = setTimeout(() => {
      if (runtimeRef.current.state === "processing-question") playProcessingFiller(stage);
    }, gap);
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
      // Recoverable - the lesson is fine, only the mic request failed. Denied/no-device get a
      // specific, actionable Thai message instead of the browser's raw (often English) error
      // text; MIC_UNAVAILABLE resumes right where push-to-talk interrupted rather than ending
      // the whole session like FAIL does.
      const domError = err as DOMException;
      const message =
        domError?.name === "NotAllowedError" || domError?.name === "SecurityError"
          ? "ไม่ได้รับอนุญาตให้ใช้ไมโครโฟน กรุณาอนุญาตการใช้งานไมค์ในเบราว์เซอร์ แล้วลองกดพูดอีกครั้งค่ะ"
          : domError?.name === "NotFoundError"
            ? "ไม่พบไมโครโฟนในอุปกรณ์นี้ กรุณาตรวจสอบไมค์แล้วลองใหม่อีกครั้งค่ะ"
            : "ไม่สามารถเข้าถึงไมโครโฟนได้ กรุณาลองใหม่อีกครั้งค่ะ";
      dispatch({ type: "MIC_UNAVAILABLE", message });
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

    // Answering "พร้อมหรือยังคะ?" is a yes/no, not a lesson question - the provider skips
    // the deck grounding for it, so it comes back much faster.
    const expecting = runtimeRef.current.interruptedFrom === "ready" ? "readiness" : "question";

    playProcessingFiller();

    try {
      const currentSlide = slidesRef.current[runtimeRef.current.currentSlideIndex];
      const result = await api.askVoiceQuestion({
        audioBlob,
        token: link.token,
        learnerKey,
        currentSlideObjectId: currentSlide?.slideObjectId,
        durationMs,
        expecting,
      });

      if (!mountedRef.current) return;

      if (result.readiness) {
        dispatch({ type: "READINESS_ANSWERED", ready: result.readiness === "ready" });
        return;
      }

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
    const slideIndex = runtimeRef.current.currentSlideIndex;
    const lastSlide = slidesRef.current[slideIndex];
    try {
      await api.endLearningSession(link.token, {
        learnerKey,
        completedAllSlides,
        lastSlideObjectId: lastSlide?.slideObjectId,
        lastSlideIndex: slideIndex,
      });
    } catch {
      // Best-effort - the room still shows ENDED locally even if persistence fails
      // (e.g. a transient network issue during a demo).
    }
  }

  /**
   * Updates the existing row on every slide change - never inserts (CORE_FEATURE_SPEC §2.3).
   * This write is also what feeds the "หยุดกลางคัน" calculation: the server stamps
   * LastActivityAt from it, and a learner who walks away simply stops sending these.
   *
   * Fire-and-forget on purpose. Losing one progress ping costs a slightly stale resume point;
   * blocking narration on it would make a slow network audible.
   */
  function persistProgress(slideIndex: number) {
    const slide = slidesRef.current[slideIndex];
    void api
      .updateLearningProgress(link.token, {
        learnerKey,
        lastSlideObjectId: slide?.slideObjectId,
        lastSlideIndex: slideIndex,
        // Sent on every ping rather than once at load: the deck resolves asynchronously, so the
        // first pings can carry an empty list. The server keeps the last value greater than zero,
        // which is also what lets it tell that someone reached the final slide.
        totalSlideCount: slidesRef.current.length || undefined,
      })
      .catch(() => undefined);
  }

  function runEffect(effect: TutorEffect) {
    switch (effect.kind) {
      case "LOAD_LESSON":
        void (async () => {
          try {
            const { lesson, embedUrl: url, slides } = await api.getLessonByLinkToken(link.token);
            slidesRef.current = slides;
            ctxRef.current = {
              slides,
              introWaitMs: lesson.introWaitMs,
              breathPauseMs: lesson.breathPauseMs,
              finalQuestionWaitMs: lesson.finalQuestionWaitMs,
              resumeSlideIndex: learningSession.lastSlideIndex ?? 0,
            };
            if (mountedRef.current) {
              setEmbedUrl(url);
              dispatch({ type: "LESSON_LOADED" });
              // Runs alongside the intro/first slides - never awaited, so a slow or failing
              // TTS here can't hold the lesson up.
              void prefetchFillers();
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
        void playText(effect.text, effect.withFoundLead);
        break;
      case "WAIT_READY_TIMEOUT":
        timerRef.current = setTimeout(() => dispatch({ type: "INTRO_TIMEOUT" }), effect.ms);
        break;
      case "LOAD_SLIDE":
        persistProgress(effect.slideIndex);
        void loadSlideAudio(effect.slideIndex, effect.withResumeBridge);
        break;
      case "WAIT_REMAINING":
        timerRef.current = setTimeout(() => dispatch({ type: "SLIDE_DURATION_ENDED" }), effect.ms);
        break;
      case "START_RECORDING":
        // Stamped synchronously, before any await: measuring from recorder.start() instead
        // would charge a slow mic hand-off against the recipient's hold time and trip the
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
    // Restored here (not in a useState initializer) to avoid an SSR/hydration mismatch on the
    // volume slider - the server has no localStorage, so both render at 1 first, then this snaps
    // to the saved value before any audio starts.
    try {
      const stored = window.localStorage.getItem(AI_VOLUME_STORAGE_KEY);
      if (stored !== null) changeVolume(Number(stored));
    } catch {
      // localStorage unavailable - fall back to the default volume of 1.
    }
    dispatch({ type: "JOIN" });
    // No "mark started" call any more: the row was created by the join screen, and StartedAt was
    // stamped there. Reaching the room is not a separate event worth a second round-trip.
    //
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

  // dispatch/runEffect close over refs + the stable `link`/`learnerKey` props only, so freezing
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
    aiVolume: volume,
    setAiVolume: changeVolume,
  };
}
