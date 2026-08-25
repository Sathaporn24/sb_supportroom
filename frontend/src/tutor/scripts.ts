// Deliberately neutral: no name, no honorific, no assumption about who is listening. These lines
// are spoken to whoever opened the join link, and this product is used by companies whose users
// are not teachers - "สวัสดีค่ะคุณครู{ชื่อ}" was correct for exactly one company. Per-company
// wording is a real option later (TD-012) but nothing needs it yet, so nothing is configurable.
export function introScript(): string {
  return "สวัสดีค่ะ วันนี้จะพาไปเรียนรู้การใช้งานระบบทีละขั้นตอนนะคะ พร้อมเริ่มหรือยังคะ?";
}

// Spoken after "ยังไม่พร้อม" (TQ-18/U1) - the recipient presses one of the two readiness
// buttons; the start button says "พร้อมแล้ว เริ่มเรียนเลย" and needs no separate spoken
// acknowledgement (it already reads as a confirmation). This one still needs a reply, or
// pressing "ยังไม่พร้อม" feels like it went nowhere - and the wording has to name the button
// the recipient actually sees, not something they can no longer do (talking or typing).
export const notReadyScript = "ได้ค่ะ ไม่ต้องรีบนะคะ พร้อมเมื่อไหร่กดปุ่มพร้อมแล้วได้เลยค่ะ";

export const finalQuestionScript = "วันนี้เราเรียนครบทุกขั้นตอนแล้วค่ะ มีคำถามเพิ่มเติมไหมคะ?";

export const closingScript = "ขอบคุณค่ะ พบกันใหม่นะคะ";
