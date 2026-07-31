export type TeachingTopic = {
  id: string;
  title: string;
  /** Whether real script/media content exists for this topic yet. */
  available: boolean;
};

// Presentational list for the "create session" picker. Only "available" topics have
// real lesson content behind them (via lessonRepository.getLoginLesson()) - the rest
// are placeholders signalling more topics will land here later.
export const teachingTopicsSeed: TeachingTopic[] = [
  { id: "login-mobile", title: "วิธีการ Login (mobile)", available: true },
  { id: "login-web", title: "วิธีการ Login (Web)", available: false },
  { id: "forgot-password", title: "ลืมรหัสผ่าน", available: false },
];
