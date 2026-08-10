// Client + server safe constants. These are the same numbers as the DEFAULT_* env vars
// (see src/config/server-defaults.ts) - kept here as plain constants so client
// components have a sensible fallback without needing NEXT_PUBLIC_-prefixed env vars.
export const tutorConfig = {
  mockWordsPerMinute: 125,
  defaultIntroWaitMs: 5_000,
  defaultBreathPauseMs: 500,
  defaultFinalQuestionWaitMs: 5_000,
  defaultSessionExpiryHours: 24,
} as const;
