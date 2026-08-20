import { describe, expect, it } from "vitest";
import { createInitialRuntime, tutorReducer, type TutorContext } from "@/tutor/tutor-reducer";
import type { TeachingSlide } from "@/types/domain";
import { QUESTION_FAILED_TEXT } from "@/config/response-texts";
import { notReadyScript, readyConfirmScript } from "@/tutor/scripts";

const slides: TeachingSlide[] = [
  { slideObjectId: "s1", index: 0, speakerNotes: "Slide one", videoDurationMs: 0 },
  { slideObjectId: "s2", index: 1, speakerNotes: "Slide two", videoDurationMs: 5_000 },
];

const ctx: TutorContext = {
  slides,
  introWaitMs: 5_000,
  breathPauseMs: 1_000,
  finalQuestionWaitMs: 5_000,
  resumeSlideIndex: 0,
};

function toSlideSpeaking(startIndex = 0) {
  let r = createInitialRuntime();
  r = tutorReducer(r, { type: "JOIN" }, ctx).runtime;
  r = tutorReducer(r, { type: "LESSON_LOADED" }, ctx).runtime;
  r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime; // -> ready
  r = tutorReducer(r, { type: "START" }, ctx).runtime; // -> slide-loading(0)
  for (let i = 0; i < startIndex; i++) {
    r = tutorReducer(r, { type: "SLIDE_READY" }, ctx).runtime;
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime; // -> waiting-slide-duration
    r = tutorReducer(r, { type: "SLIDE_DURATION_ENDED" }, ctx).runtime; // -> next slide-loading
  }
  r = tutorReducer(r, { type: "SLIDE_READY" }, ctx).runtime; // -> slide-speaking
  return r;
}

describe("tutorReducer: startup sequence", () => {
  it("goes idle -> preparing -> intro-speaking -> ready -> slide-loading on JOIN/LESSON_LOADED/TTS_ENDED/START", () => {
    let r = createInitialRuntime();
    expect(r.state).toBe("idle");

    const join = tutorReducer(r, { type: "JOIN" }, ctx);
    expect(join.runtime.state).toBe("preparing");
    expect(join.effect).toEqual({ kind: "LOAD_LESSON" });
    r = join.runtime;

    const loaded = tutorReducer(r, { type: "LESSON_LOADED" }, ctx);
    expect(loaded.runtime.state).toBe("intro-speaking");
    expect(loaded.runtime.afterSpeech).toBe("ENTER_READY");
    r = loaded.runtime;

    const introDone = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 2000 }, ctx);
    expect(introDone.runtime.state).toBe("ready");
    expect(introDone.effect).toEqual({ kind: "WAIT_READY_TIMEOUT", ms: 5000 });
    r = introDone.runtime;

    const started = tutorReducer(r, { type: "START" }, ctx);
    expect(started.runtime.state).toBe("slide-loading");
    expect(started.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: 0 });
  });
});

describe("tutorReducer: resuming where the learner left off", () => {
  function startWith(context: TutorContext) {
    let r = createInitialRuntime();
    r = tutorReducer(r, { type: "JOIN" }, context).runtime;
    r = tutorReducer(r, { type: "LESSON_LOADED" }, context).runtime;
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, context).runtime; // -> ready
    return tutorReducer(r, { type: "START" }, context);
  }

  it("opens on the stored slide instead of the first one", () => {
    const started = startWith({ ...ctx, resumeSlideIndex: 1 });
    expect(started.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: 1 });
    expect(started.runtime.currentSlideIndex).toBe(1);
  });

  it("clamps a stored slide that no longer exists to the last one", () => {
    // A deck can lose slides between two visits - resuming past the end would load nothing.
    const started = startWith({ ...ctx, resumeSlideIndex: 99 });
    expect(started.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: slides.length - 1 });
  });

  it("still starts at 0 for someone who has never been here", () => {
    const started = startWith({ ...ctx, resumeSlideIndex: 0 });
    expect(started.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: 0 });
  });
});

describe("tutorReducer: slide duration = max(ttsDuration, videoDuration) + breathPause", () => {
  it("waits only the breath pause when TTS outlasts the (zero) video duration", () => {
    const r = toSlideSpeaking(0); // slide s1, videoDurationMs 0
    const { effect } = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 3000 }, ctx);
    expect(effect).toEqual({ kind: "WAIT_REMAINING", ms: 0 + 1000 });
  });

  it("waits out the remaining video time plus breath pause when video outlasts TTS", () => {
    const r = toSlideSpeaking(1); // slide s2, videoDurationMs 5000
    const { effect } = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 2000 }, ctx);
    expect(effect).toEqual({ kind: "WAIT_REMAINING", ms: 3000 + 1000 });
  });

  it("never waits a negative amount when TTS already exceeds the video duration", () => {
    const r = toSlideSpeaking(1); // videoDurationMs 5000
    const { effect } = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 9000 }, ctx);
    expect(effect).toEqual({ kind: "WAIT_REMAINING", ms: 0 + 1000 });
  });
});

describe("tutorReducer: end of deck -> final question window", () => {
  it("speaks the final-question prompt once the last slide's wait ends", () => {
    let r = toSlideSpeaking(1);
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime; // waiting-slide-duration
    const ended = tutorReducer(r, { type: "SLIDE_DURATION_ENDED" }, ctx);
    expect(ended.runtime.state).toBe("intro-speaking");
    expect(ended.runtime.afterSpeech).toBe("WAIT_FINAL_QUESTION");
    expect(ended.runtime.completedAllSlides).toBe(true);
  });
});

describe("tutorReducer: Push-to-Talk", () => {
  it("only allows PUSH_TO_TALK_START while a slide is actively teaching", () => {
    const speaking = toSlideSpeaking(0);
    const started = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx);
    expect(started.runtime.state).toBe("push-to-talk-recording");
    expect(started.effect).toEqual({ kind: "START_RECORDING" });

    const ready = createInitialRuntime();
    const rejected = tutorReducer(ready, { type: "PUSH_TO_TALK_START" }, ctx);
    expect(rejected.runtime.state).toBe("idle");
    expect(rejected.effect).toEqual({ kind: "NONE" });
  });

  it("moves to processing-question and stops recording on PUSH_TO_TALK_END", () => {
    const speaking = toSlideSpeaking(0);
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const ended = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx);
    expect(ended.runtime.state).toBe("processing-question");
    expect(ended.effect).toEqual({ kind: "STOP_RECORDING_AND_SEND" });
  });
});

describe("tutorReducer: answering the readiness prompt by voice", () => {
  function toReady() {
    let r = createInitialRuntime();
    r = tutorReducer(r, { type: "JOIN" }, ctx).runtime;
    r = tutorReducer(r, { type: "LESSON_LOADED" }, ctx).runtime;
    return tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime; // -> ready
  }

  it("allows push-to-talk from the ready prompt and remembers where it came from", () => {
    const recording = tutorReducer(toReady(), { type: "PUSH_TO_TALK_START" }, ctx);
    expect(recording.runtime.state).toBe("push-to-talk-recording");
    expect(recording.runtime.interruptedFrom).toBe("ready");
  });

  it('starts the deck after acknowledging a spoken "พร้อมแล้ว"', () => {
    const recording = tutorReducer(toReady(), { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    const confirmed = tutorReducer(processing, { type: "READINESS_ANSWERED", ready: true }, ctx);
    expect(confirmed.effect).toEqual({ kind: "SPEAK", text: readyConfirmScript });
    expect(confirmed.runtime.afterSpeech).toBe("START_FIRST_SLIDE");

    // No resume bridge: this is the first slide, not a return to something interrupted.
    const started = tutorReducer(confirmed.runtime, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx);
    expect(started.runtime.state).toBe("slide-loading");
    expect(started.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: 0 });
  });

  it('waits with no auto-start timer after "ยังไม่พร้อม"', () => {
    const recording = tutorReducer(toReady(), { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    const declined = tutorReducer(processing, { type: "READINESS_ANSWERED", ready: false }, ctx);
    expect(declined.effect).toEqual({ kind: "SPEAK", text: notReadyScript });

    // Crucially no WAIT_READY_TIMEOUT here - starting anyway 5s after being told "not yet"
    // would override the answer the teacher just gave.
    const waiting = tutorReducer(declined.runtime, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx);
    expect(waiting.runtime.state).toBe("ready");
    expect(waiting.effect).toEqual({ kind: "NONE" });
  });

  it("returns to the ready prompt, not slide 1, when the readiness reply is unusable", () => {
    const recording = tutorReducer(toReady(), { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    const resumed = tutorReducer(processing, { type: "NO_SPEECH" }, ctx);
    expect(resumed.runtime.state).toBe("ready");
    expect(resumed.effect).toEqual({ kind: "WAIT_READY_TIMEOUT", ms: 5000 });
  });
});

describe("tutorReducer: no_speech resumes silently, real failures speak up", () => {
  it("restarts the current slide with no extra speech on NO_SPEECH", () => {
    const speaking = toSlideSpeaking(0);
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;
    const result = tutorReducer(processing, { type: "NO_SPEECH" }, ctx);
    expect(result.runtime.state).toBe("restarting-slide");
    expect(result.runtime.currentSlideIndex).toBe(0);
    // No SPEAK effect, and no resume bridge either: the lesson was never actually
    // interrupted, so announcing a return would be narrating something that didn't happen.
    expect(result.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: 0, withResumeBridge: false });
  });

  it("speaks up on QUESTION_FAILED rather than resuming silently like NO_SPEECH", () => {
    const speaking = toSlideSpeaking(1);
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    // An upload/API failure (expired key, upstream outage) has to be audible - a silent
    // resume here is indistinguishable from the push-to-talk button simply not working.
    const failed = tutorReducer(processing, { type: "QUESTION_FAILED" }, ctx);
    expect(failed.runtime.state).toBe("answer-speaking");
    expect(failed.effect).toEqual({ kind: "SPEAK", text: QUESTION_FAILED_TEXT });

    // ...and it still lands back on the interrupted slide once the apology finishes.
    const resumed = tutorReducer(failed.runtime, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx);
    expect(resumed.runtime.state).toBe("restarting-slide");
    expect(resumed.runtime.currentSlideIndex).toBe(1);
  });
});

describe("tutorReducer: answered questions restart the current slide", () => {
  it("speaks the answer, then restarts the same slide once the answer finishes", () => {
    const speaking = toSlideSpeaking(1);
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    const answered = tutorReducer(
      processing,
      { type: "QUESTION_ANSWERED", transcript: "คำถามทดสอบ", answer: "คำตอบทดสอบ", answerStatus: "answered" },
      ctx,
    );
    expect(answered.runtime.state).toBe("answer-speaking");
    expect(answered.runtime.afterSpeech).toBe("RESUME_AFTER_ANSWER");
    expect(answered.effect).toEqual({ kind: "SPEAK", text: "คำตอบทดสอบ", withFoundLead: true });
    expect(answered.runtime.questions).toHaveLength(1);

    const restarted = tutorReducer(answered.runtime, { type: "TTS_ENDED", elapsedMs: 1500 }, ctx);
    expect(restarted.runtime.state).toBe("restarting-slide");
    expect(restarted.runtime.currentSlideIndex).toBe(1);
    // Answering did interrupt the lesson, so the narration leads with a hand-back line.
    expect(restarted.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: 1, withResumeBridge: true });
  });

  it("shows the referenced slide while answering, then returns to the interrupted slide", () => {
    const speaking = toSlideSpeaking(1); // teaching s2
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    const answered = tutorReducer(
      processing,
      {
        type: "QUESTION_ANSWERED",
        transcript: "ถามเรื่องหน้าแรก",
        answer: "คำตอบจากสไลด์แรก",
        answerStatus: "answered",
        relatedSlideObjectId: "s1",
      },
      ctx,
    );
    expect(answered.runtime.answerSlideIndex).toBe(0);
    expect(answered.runtime.currentSlideIndex).toBe(1); // lesson position untouched

    const restarted = tutorReducer(answered.runtime, { type: "TTS_ENDED", elapsedMs: 1500 }, ctx);
    expect(restarted.runtime.answerSlideIndex).toBeNull();
    // withResumeBridge: coming back from a different slide needs a spoken hand-back, or the
    // narration restarts mid-topic with no signal that we returned.
    expect(restarted.effect).toEqual({ kind: "LOAD_SLIDE", slideIndex: 1, withResumeBridge: true });
  });

  it("does not jump when the answer references the slide already on screen", () => {
    const speaking = toSlideSpeaking(1);
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    const answered = tutorReducer(
      processing,
      { type: "QUESTION_ANSWERED", transcript: "q", answer: "a", answerStatus: "answered", relatedSlideObjectId: "s2" },
      ctx,
    );
    expect(answered.runtime.answerSlideIndex).toBeNull();
  });

  it("ignores an unknown relatedSlideObjectId rather than blanking the embed", () => {
    const speaking = toSlideSpeaking(0);
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    const answered = tutorReducer(
      processing,
      {
        type: "QUESTION_ANSWERED",
        transcript: "q",
        answer: "a",
        answerStatus: "answered",
        relatedSlideObjectId: "does-not-exist",
      },
      ctx,
    );
    expect(answered.runtime.answerSlideIndex).toBeNull();
  });

  it("clears the referenced slide when the answer comes in the final-question window", () => {
    let r = toSlideSpeaking(1);
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime;
    r = tutorReducer(r, { type: "SLIDE_DURATION_ENDED" }, ctx).runtime;
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime; // final-question-window

    const recording = tutorReducer(r, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;
    const answered = tutorReducer(
      processing,
      { type: "QUESTION_ANSWERED", transcript: "q", answer: "a", answerStatus: "answered", relatedSlideObjectId: "s1" },
      ctx,
    );
    expect(answered.runtime.answerSlideIndex).toBe(0);

    const back = tutorReducer(answered.runtime, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx);
    expect(back.runtime.state).toBe("final-question-window");
    expect(back.runtime.answerSlideIndex).toBeNull();
  });

  it('never leads with "เจอแล้ว" on an answer that found nothing', () => {
    const speaking = toSlideSpeaking(0);
    const recording = tutorReducer(speaking, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;

    for (const answerStatus of ["not_found", "out_of_scope"] as const) {
      const answered = tutorReducer(
        processing,
        { type: "QUESTION_ANSWERED", transcript: "q", answer: "ไม่พบข้อมูลค่ะ", answerStatus },
        ctx,
      );
      // Announcing a find and then saying nothing was found contradicts itself.
      expect(answered.effect).toEqual({ kind: "SPEAK", text: "ไม่พบข้อมูลค่ะ" });
    }
  });

  it("returns to the final-question window instead of a slide once all slides are done", () => {
    let r = toSlideSpeaking(1);
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime;
    r = tutorReducer(r, { type: "SLIDE_DURATION_ENDED" }, ctx).runtime; // intro-speaking, WAIT_FINAL_QUESTION
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime; // final-question-window

    const recording = tutorReducer(r, { type: "PUSH_TO_TALK_START" }, ctx).runtime;
    const processing = tutorReducer(recording, { type: "PUSH_TO_TALK_END" }, ctx).runtime;
    const answered = tutorReducer(
      processing,
      { type: "QUESTION_ANSWERED", transcript: "q", answer: "a", answerStatus: "answered" },
      ctx,
    );
    expect(answered.runtime.afterSpeech).toBe("WAIT_FINAL_QUESTION");
  });
});

describe("tutorReducer: ending a session", () => {
  it("persists completedAllSlides=false when the teacher leaves mid-lesson", () => {
    const speaking = toSlideSpeaking(0);
    const ended = tutorReducer(speaking, { type: "END_SESSION" }, ctx);
    expect(ended.runtime.state).toBe("completed");
    expect(ended.effect).toEqual({ kind: "PERSIST_END", completedAllSlides: false });
  });

  it("persists completedAllSlides=true after the closing statement finishes", () => {
    let r = toSlideSpeaking(1);
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime;
    r = tutorReducer(r, { type: "SLIDE_DURATION_ENDED" }, ctx).runtime;
    r = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx).runtime; // final-question-window
    r = tutorReducer(r, { type: "FINAL_QUESTION_TIMEOUT" }, ctx).runtime; // intro-speaking, FINISH_SESSION
    const done = tutorReducer(r, { type: "TTS_ENDED", elapsedMs: 1000 }, ctx);
    expect(done.runtime.state).toBe("completed");
    expect(done.effect).toEqual({ kind: "PERSIST_END", completedAllSlides: true });
  });
});
