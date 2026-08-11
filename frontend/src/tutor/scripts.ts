// Deliberately neutral: no name, no honorific, no assumption about who is listening. These lines
// are spoken to whoever opened the join link, and this product is used by companies whose users
// are not teachers - "สวัสดีค่ะคุณครู{ชื่อ}" was correct for exactly one company. Per-company
// wording is a real option later (TD-012) but nothing needs it yet, so nothing is configurable.
export function introScript(): string {
  return "สวัสดีค่ะ วันนี้จะพาไปเรียนรู้การใช้งานระบบทีละขั้นตอนนะคะ พร้อมเริ่มหรือยังคะ?";
}

// Spoken back when the recipient answers the readiness prompt out loud instead of clicking
// the start button - without a reply, talking to the room feels like it went nowhere.
export const readyConfirmScript = "ดีค่ะ งั้นเราเริ่มกันเลยนะคะ";
export const notReadyScript = "ได้ค่ะ ไม่ต้องรีบนะคะ พร้อมเมื่อไหร่กดปุ่มพูดแล้วบอกได้เลยค่ะ";

export const finalQuestionScript = "วันนี้เราเรียนครบทุกขั้นตอนแล้วค่ะ มีคำถามเพิ่มเติมไหมคะ?";

export const closingScript = "ขอบคุณค่ะ พบกันใหม่นะคะ";
