import type { CheckpointPrompt } from "@/types/domain";

export const checkpointPrompts: CheckpointPrompt[] = [
  { id: "understand-check", text: "ส่วนนี้เข้าใจไหมคะ?" },
  { id: "have-question", text: "มีคำถามเกี่ยวกับส่วนนี้ไหมคะ?" },
  { id: "need-repeat", text: "ต้องการให้ทบทวนอีกครั้งไหมคะ?" },
];

export function getCheckpointPromptText(id: string): string {
  return checkpointPrompts.find((p) => p.id === id)?.text ?? checkpointPrompts[0].text;
}
