export const tutorConfig = {
  readyAutoContinueMs: 5_000,
  checkpointSilenceMs: 5_000,
  interruptionClarifyMs: 5_000,
  finalQuestionSilenceMs: 10_000,
  reconnectGraceMs: 15 * 60_000,
  defaultLinkExpiryHours: 24,
  mockWordsPerMinute: 125,
} as const;

export const providerConfig = {
  aiProvider: process.env.NEXT_PUBLIC_AI_PROVIDER ?? "mock",
  ttsProvider: process.env.NEXT_PUBLIC_TTS_PROVIDER ?? "mock",
  sttProvider: process.env.NEXT_PUBLIC_STT_PROVIDER ?? "mock",
  dataProvider: process.env.NEXT_PUBLIC_DATA_PROVIDER ?? "local-storage",
  enableDemoControls: process.env.NEXT_PUBLIC_ENABLE_DEMO_CONTROLS === "true",
} as const;
