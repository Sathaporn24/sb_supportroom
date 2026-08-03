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
// Deliberately plain. "เป็นคำถามที่ดีมากเลยค่ะ" on every single question reads as flattery
// on a script, and "ขอเวลาประมวลผล" is what a machine does, not what a person says - both
// were here and both are why the opening sounded off.
export const PROCESSING_FILLER_TEXTS = [
  "ได้ค่ะ ขอเวลาสักครู่นะคะ",
  "รับทราบค่ะ รอสักครู่นะคะ",
  "สักครู่นะคะ กำลังหาคำตอบให้อยู่ค่ะ",
  "โอเคค่ะ ขอเช็กข้อมูลสักครู่นะคะ",
  "ได้เลยค่ะ ขอดูข้อมูลแป๊บนึงนะคะ",
  "ขอบคุณสำหรับคำถามค่ะ กำลังหาคำตอบให้อยู่นะคะ",
  "กำลังตรวจสอบข้อมูลจากบทเรียนให้อยู่นะคะ",
  "เข้าใจคำถามแล้วค่ะ ขอเวลาเตรียมคำตอบสักครู่นะคะ",
  "ขอค้นข้อมูลในบทเรียนสักครู่นะคะ",
  "ได้ค่ะ ขอเปิดดูรายละเอียดแป๊บนึงนะคะ",
] as const;

// One filler line only covers 1.3-2.1s of a ~9s wait (measured against the real service),
// so these top up the rest. Three rungs at ~0.9-1.3s each, with the gap below between
// them, lands the last one right around when the answer typically arrives.
//
// They are ordered stages, not one bag drawn from at random. A person filling their own
// silence doesn't repeat the same noise - they hum, then say how it's going, then say
// they're nearly there. Drawing randomly from a flat pool (the previous behaviour) can
// hand back the same hum twice in a row, which is exactly what a stuck loop sounds like.
// Each rung here implies more elapsed time than the one before it.
//
// "ติ๊ก ต๊อก" used to sit in this list and is the main reason it sounded wrong: a ticking
// clock is onomatopoeia, something written in a comic panel, not a sound a person makes
// out loud while looking something up. Thai fillers that speakers genuinely produce while
// thinking are hums - อืม / หืม (see thaipod101.com/blog/2021/09/09/thai-filler-words).
// เอ่อ is equally common but reads as struggling for words rather than looking something
// up, so it's left out of an assistant that's supposed to sound competent.
//
// Stage 1 carries no polite particle on purpose. "อืมค่ะ" is a reply; a bare hum is a
// thought, and a thought is what should be audible while the answer is still cooking.
//
// Rate is the only prosody lever available: the service rejects SSML outright (<break>
// and nested <prosody> both close the connection), and repeated characters barely help -
// measured, "อืมมมมมม" runs only ~0.2s longer than "อืม", so hums are slowed instead.
export const PROCESSING_FILLER_STAGES: ReadonlyArray<ReadonlyArray<{ text: string; rate?: string }>> = [
  // 1 - just thinking.
  [
    { text: "อืมม", rate: "-45%" },
    { text: "หืมม", rate: "-45%" },
    { text: "อืม อืมม", rate: "-40%" },
  ],
  // 2 - still on it, said out loud.
  [
    { text: "ขอดูอีกนิดนึงนะคะ" },
    { text: "กำลังดูให้อยู่นะคะ" },
    { text: "ขออีกนิดนึงนะคะ" },
  ],
  // 3 - nearly there. Also the rung a long wait sits on, so its variants have to bear
  // being heard more than once.
  [
    { text: "ใกล้ได้แล้วค่ะ" },
    { text: "อีกนิดเดียวค่ะ" },
    { text: "เกือบได้แล้วนะคะ" },
  ],
];

// Beat of silence between waiting sounds. Running them back to back sounds like one
// continuous stream of noise; the gap is what makes them read as "still working" rather
// than filler for its own sake.
//
// A range rather than one number: a fixed gap turns the sounds into a metronome, and
// nothing about a person waiting on a lookup is metronomic.
export const PROCESSING_FILLER_GAP_MIN_MS = 900;
export const PROCESSING_FILLER_GAP_MAX_MS = 1_900;

// Prefixed to an answer that actually found something, so it lands as a discovery instead
// of cutting in cold after the last waiting sound.
export const ANSWER_FOUND_LEADS = [
  "อ๋อ ได้แล้วค่ะ",
  "ได้คำตอบแล้วค่ะ",
  "อ้อ เจอแล้วค่ะ",
  "เจอแล้วค่ะ",
  "โอเคค่ะ เจอแล้ว",
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
