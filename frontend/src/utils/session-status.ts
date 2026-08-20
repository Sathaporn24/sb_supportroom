import type {
  AnswerStatus,
  LearningSessionStatus,
  LinkStatus,
  ReviewResult,
} from "@/types/domain";

/**
 * The backend computes link status and isStalled and sends them down, so nothing here recomputes
 * them - these helpers exist for the places that only hold an expiry date (an optimistic check
 * before a request, a table cell) and for the Thai labels.
 */
export function isLinkUsable(link: { expiresAt: string }, now: Date = new Date()): boolean {
  return new Date(link.expiresAt).getTime() > now.getTime();
}

/** UI-only threshold for flagging a link as "about to expire" in the admin table - not a business
 * rule, just makes an approaching deadline visible before it lapses. */
const LINK_EXPIRING_SOON_THRESHOLD_MS = 2 * 60 * 60 * 1000;

export function isLinkExpiringSoon(link: { expiresAt: string }, now: Date = new Date()): boolean {
  const remainingMs = new Date(link.expiresAt).getTime() - now.getTime();
  return remainingMs > 0 && remainingMs <= LINK_EXPIRING_SOON_THRESHOLD_MS;
}

export const linkStatusLabels: Record<LinkStatus, string> = {
  ACTIVE: "ใช้งานได้",
  EXPIRED: "หมดอายุ",
};

export const LINK_EXPIRING_SOON_LABEL = "ใกล้หมดอายุ";

export const learningSessionStatusLabels: Record<LearningSessionStatus, string> = {
  IN_PROGRESS: "กำลังเรียน",
  ENDED: "เรียนจบแล้ว",
};

/** Shown instead of the plain status when isStalled - see CORE_FEATURE_SPEC §2.6. */
export const STALLED_LABEL = "หยุดกลางคัน";

export function learningSessionStatusLabel(session: {
  status: LearningSessionStatus;
  isStalled: boolean;
}): string {
  return session.isStalled ? STALLED_LABEL : learningSessionStatusLabels[session.status];
}

export const answerStatusLabels: Record<AnswerStatus, string> = {
  answered: "ตอบแล้ว",
  not_found: "ไม่พบข้อมูล",
  out_of_scope: "นอกเรื่อง",
  no_speech: "ไม่มีคำพูด",
  transcription_failed: "ถอดเสียงไม่ได้",
};

export const reviewResultLabels: Record<ReviewResult, string> = {
  correct: "ตอบถูก",
  incorrect: "ตอบผิด",
};
