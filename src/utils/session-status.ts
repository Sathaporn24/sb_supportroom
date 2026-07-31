import type { SessionStatus, TrainingSession } from "@/types/domain";

export function getSessionStatus(session: TrainingSession, now: Date = new Date()): SessionStatus {
  if (session.endedAt) {
    return "ENDED";
  }
  const expired = new Date(session.expiresAt).getTime() <= now.getTime();
  if (expired && !session.startedAt) {
    return "EXPIRED";
  }
  if (session.startedAt) {
    return "IN_PROGRESS";
  }
  return "NOT_STARTED";
}

export function isSessionJoinable(session: TrainingSession, now: Date = new Date()): boolean {
  if (session.endedAt) {
    return false;
  }
  if (session.startedAt) {
    return true;
  }
  return new Date(session.expiresAt).getTime() > now.getTime();
}

export const sessionStatusLabels: Record<SessionStatus, string> = {
  NOT_STARTED: "ยังไม่เปิด",
  IN_PROGRESS: "กำลังสอน",
  ENDED: "จบแล้ว",
  EXPIRED: "หมดอายุ",
};
