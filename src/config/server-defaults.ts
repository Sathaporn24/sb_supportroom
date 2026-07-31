import "server-only";
import { tutorConfig } from "@/config/tutor-config";

// A blank (but present) env var must fall back to the default exactly like an unset
// one - `Number(process.env.X ?? fallback)` would otherwise turn `X=""` into 0.
function numberEnv(value: string | undefined, fallback: number): number {
  if (!value) return fallback;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

/** Applied when CS creates a LessonConfig without overriding the timing fields. */
export function getLessonTimingDefaults() {
  return {
    introWaitMs: numberEnv(process.env.DEFAULT_INTRO_WAIT_MS, tutorConfig.defaultIntroWaitMs),
    breathPauseMs: numberEnv(process.env.DEFAULT_BREATH_PAUSE_MS, tutorConfig.defaultBreathPauseMs),
    finalQuestionWaitMs: numberEnv(process.env.DEFAULT_FINAL_QUESTION_WAIT_MS, tutorConfig.defaultFinalQuestionWaitMs),
  };
}

export function getDefaultSessionExpiryHours(): number {
  return numberEnv(process.env.DEFAULT_SESSION_EXPIRY_HOURS, tutorConfig.defaultSessionExpiryHours);
}
