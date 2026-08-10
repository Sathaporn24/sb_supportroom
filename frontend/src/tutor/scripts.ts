export function introScript(teacherName?: string): string {
  const who = teacherName ? `สวัสดีค่ะคุณครู${teacherName}` : "สวัสดีค่ะคุณครู";
  return `${who} วันนี้จะพาไปเรียนรู้การใช้งานระบบทีละขั้นตอนนะคะ พร้อมเริ่มหรือยังคะ?`;
}

// Spoken back when the teacher answers the readiness prompt out loud instead of clicking
// the start button - without a reply, talking to the room feels like it went nowhere.
export const readyConfirmScript = "ดีค่ะ งั้นเราเริ่มกันเลยนะคะ";
export const notReadyScript = "ได้ค่ะ ไม่ต้องรีบนะคะ พร้อมเมื่อไหร่กดปุ่มพูดแล้วบอกได้เลยค่ะ";

export const finalQuestionScript = "วันนี้เราเรียนครบทุกขั้นตอนแล้วค่ะ มีคำถามเพิ่มเติมไหมคะ?";

export const closingScript = "ขอบคุณค่ะ พบกันใหม่นะคะ";
