export function greetingScript(teacherName?: string): string {
  const who = teacherName ? `สวัสดีค่ะคุณครู${teacherName}` : "สวัสดีค่ะคุณครู";
  return `${who} ยินดีต้อนรับเข้าสู่ห้องสอนการใช้งานระบบนะคะ พร้อมเริ่มหรือยังคะ?`;
}

export const checkpointContinueScript = "ถ้าไม่มีคำถาม ขออนุญาตไปต่อนะคะ";

export const noiseClarifyScript = "เมื่อสักครู่ต้องการถามอะไรไหมคะ?";

export const noiseContinueScript = "งั้นขออนุญาตไปต่อนะคะ";

export const stillNotUnderstoodPromptScript = "คุณครูติดตรงส่วนไหนคะ บอกได้เลยนะคะ";

export function simplifiedExplanationScript(scriptText: string): string {
  return `ลองอธิบายให้เข้าใจง่ายขึ้นอีกครั้งนะคะ ${scriptText}`;
}

export function reviewIntroScript(previousStepTitle: string, previousScriptText: string): string {
  return `ขอย้อนทบทวนขั้นตอนก่อนหน้าอีกครั้งนะคะ เรื่อง${previousStepTitle} ${previousScriptText}`;
}

export const reviewReturnScript = "ทีนี้ขอกลับมาที่ขั้นตอนเดิมต่อนะคะ";

export const answerReturnScript = "กลับไปที่เนื้อหาเดิมต่อนะคะ";

export const summaryAndFinalQaScript =
  "สรุปแล้ว คุณครูได้เรียนรู้ขั้นตอนการเข้าสู่ระบบครบทุกขั้นตอนแล้วค่ะ มีคำถามเพิ่มเติมไหมคะ?";

export const closingScript = "ขอบคุณค่ะ พบกันใหม่นะคะ";
