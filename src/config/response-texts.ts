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

// One filler line only covers ~3s of a ~9s wait, so these top up the rest. They are
// sounds rather than more sentences on purpose: repeating "อีกสักครู่นะคะ" every few
// seconds reads as a stuck loop, while a drawn-out hum or a ticking clock reads as time
// visibly passing.
//
// Each carries its own rate because they want opposite treatment - a hum has to be
// stretched to drawl at all, while a tick-tock slowed to the same degree stops sounding
// like a clock. Rate is the only lever available: the service rejects SSML outright
// (<break> and nested <prosody> both close the connection), and repeated characters
// barely help - measured, "อืมมมมมม" runs only ~0.2s longer than "อืม".
//
// Multiple spaces collapse to one, so the gaps between ticks are newlines - the only
// thing that produces a long pause (measured: newline ~1.5s, comma ~0.4s, space ~0).
export const PROCESSING_FILLER_FOLLOWUPS = [
  { text: "ติ๊ก\nต๊อก\nติ๊ก\nต๊อก", rate: "-10%" },
  { text: "ติ๊ก, ต๊อก, ติ๊ก, ต๊อก", rate: "-25%" },
  { text: "อืมมม", rate: "-45%" },
  { text: "เอ่อออ", rate: "-45%" },
] as const;

// Spoken on the way back into the lesson after a question, so the jump from "answer" to
// "mid-sentence narration" isn't abrupt - especially when the answer moved the deck to a
// different slide and the teacher needs telling that we're back where we left off.
export const RESUME_BRIDGE_TEXTS = [
  "เรากลับมาที่ขั้นตอนที่ค้างไว้กันต่อเลยนะคะ",
  "กลับมาที่ขั้นตอนเมื่อกี้ที่ค้างไว้กันต่อนะคะ",
  "เรามาต่อกันที่ตรงที่ค้างไว้เลยนะคะ",
  "กลับมาเรียนต่อจากที่ค้างไว้กันนะคะ",
] as const;
