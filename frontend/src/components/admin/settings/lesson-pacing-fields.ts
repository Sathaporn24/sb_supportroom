/**
 * Client-side parse + range check for the three company-level pacing fields (LP-8/SP-8). This is
 * a convenience only - the server (`UpdateCompanyLessonPacingDto`) owns the real validation and
 * its Thai error message wins on a 400 (SP-8). Ranges are declared once here so a future retune
 * only touches one file per side, per LP-8's own note.
 */

export const LESSON_PACING_FIELD_RANGES = {
  introWaitMs: { min: 0, max: 60_000 },
  breathPauseMs: { min: 0, max: 10_000 },
  finalQuestionWaitMs: { min: 0, max: 120_000 },
} as const;

export type LessonPacingFieldName = keyof typeof LESSON_PACING_FIELD_RANGES;

export type LessonPacingFieldParseResult =
  | { ok: true; value: number }
  | { ok: false; error: string };

/**
 * SP-7: at the company layer an empty field is never valid - there is no next layer to fall back
 * to. Deliberately does not use `Number(x) || 0`, which would turn an empty string into a
 * silently-accepted 0 (SP-7's own warning).
 */
export function parseLessonPacingField(
  field: LessonPacingFieldName,
  rawValue: string,
): LessonPacingFieldParseResult {
  const trimmed = rawValue.trim();
  if (trimmed === "") {
    return { ok: false, error: "กรุณากรอกค่า" };
  }

  const value = Number(trimmed);
  if (!Number.isFinite(value) || !Number.isInteger(value)) {
    return { ok: false, error: "กรุณากรอกตัวเลขจำนวนเต็ม" };
  }

  const { min, max } = LESSON_PACING_FIELD_RANGES[field];
  if (value < min || value > max) {
    return { ok: false, error: `ต้องอยู่ระหว่าง ${min}-${max} มิลลิวินาที` };
  }

  return { ok: true, value };
}
