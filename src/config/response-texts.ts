export const OUT_OF_SCOPE_TEXT =
  "ขออภัยค่ะ เรื่องนี้อยู่นอกขอบเขตการสอนระบบ ตอนนี้ขออนุญาตกลับไปที่หัวข้อเดิมนะคะ";

export const UNKNOWN_ANSWER_TEXT =
  "ขออภัยค่ะ ยังไม่มีข้อมูลที่ยืนยันได้สำหรับคำถามนี้ ระบบจะบันทึกไว้ให้ทีม CS ตรวจสอบค่ะ";

// Spoken when the question round-trip itself fails (upload error, expired API key,
// upstream outage). Resuming silently here is indistinguishable from a dead button - an
// expired Gemini key once burned hours of debugging for exactly that reason.
export const QUESTION_FAILED_TEXT =
  "ขออภัยค่ะ ระบบขัดข้องชั่วคราว ยังไม่สามารถตอบคำถามนี้ได้ ขออนุญาตกลับไปที่บทเรียนก่อนนะคะ";
