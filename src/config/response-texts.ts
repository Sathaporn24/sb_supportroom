export const OUT_OF_SCOPE_TEXT =
  "ขออภัยค่ะ เรื่องนี้อยู่นอกขอบเขตการสอนระบบ ตอนนี้ขออนุญาตกลับไปที่หัวข้อเดิมนะคะ";

export const UNKNOWN_ANSWER_TEXT =
  "ขออภัยค่ะ ยังไม่มีข้อมูลที่ยืนยันได้สำหรับคำถามนี้ ระบบจะบันทึกไว้ให้ทีม CS ตรวจสอบค่ะ";

// Spoken when the question round-trip itself fails (upload error, expired API key,
// upstream outage). Resuming silently here is indistinguishable from a dead button - an
// expired Gemini key once burned hours of debugging for exactly that reason.
export const QUESTION_FAILED_TEXT =
  "ขออภัยค่ะ ระบบขัดข้องชั่วคราว ยังไม่สามารถตอบคำถามนี้ได้ ขออนุญาตกลับไปที่บทเรียนก่อนนะคะ";

// Spoken the instant the talk button is released, so the Gemini round-trip (measured at
// ~9s against the real API) isn't dead air. Picked at random per question - one fixed line
// every time is what makes an assistant sound robotic.
// Spelling matters more than usual here: Edge TTS reads tone marks, so "ค่ะ" (statement)
// vs "คะ" (after นะ / in questions) is an actual pronunciation difference, not a typo.
export const PROCESSING_FILLER_TEXTS = [
  "ได้ค่ะ ขอเวลาสักครู่นะคะ",
  "รับทราบค่ะ รอสักครู่นะคะ",
  "สักครู่นะคะ กำลังหาคำตอบให้อยู่ค่ะ",
  "โอเคค่ะ ขอเช็กข้อมูลสักครู่นะคะ",
  "เป็นคำถามที่ดีมากเลยค่ะ ขอตรวจสอบข้อมูลสักครู่นะคะ",
  "คำถามน่าสนใจค่ะ ขอเวลาดูรายละเอียดสักครู่นะคะ",
  "ขอบคุณสำหรับคำถามค่ะ กำลังหาคำตอบให้อยู่นะคะ",
  "ได้ยินคำถามแล้วค่ะ ขอเวลาประมวลผลสักครู่นะคะ",
  "กำลังตรวจสอบข้อมูลจากบทเรียนให้อยู่นะคะ",
  "เข้าใจคำถามแล้วค่ะ ขอเวลาเตรียมคำตอบสักครู่นะคะ",
  "ขอค้นข้อมูลในบทเรียนสักครู่นะคะ",
  "รอสักครู่นะคะ อีกไม่นานค่ะ",
] as const;

// One filler line only covers ~3s of a ~9s wait, so these top it up until the answer
// lands. Kept short on purpose - they're a reassuring noise, not new information.
export const PROCESSING_FILLER_FOLLOWUPS = ["อีกสักครู่นะคะ", "ใกล้ได้แล้วค่ะ", "รออีกนิดนะคะ"] as const;
