export function introScript(teacherName?: string): string {
  const who = teacherName ? `สวัสดีค่ะคุณครู${teacherName}` : "สวัสดีค่ะคุณครู";
  return `${who} วันนี้จะพาไปเรียนรู้การใช้งานระบบทีละขั้นตอนนะคะ พร้อมเริ่มหรือยังคะ?`;
}

export const finalQuestionScript = "วันนี้เราเรียนครบทุกขั้นตอนแล้วค่ะ มีคำถามเพิ่มเติมไหมคะ?";

export const closingScript = "ขอบคุณค่ะ พบกันใหม่นะคะ";
