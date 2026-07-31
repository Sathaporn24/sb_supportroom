export function estimateSpeakingDurationMs(text: string, wordsPerMinute: number): number {
  const approxWordCount = Math.max(text.trim().length / 5, 1);
  const minutes = approxWordCount / wordsPerMinute;
  const ms = Math.round(minutes * 60_000);
  return Math.min(Math.max(ms, 1200), 12_000);
}
