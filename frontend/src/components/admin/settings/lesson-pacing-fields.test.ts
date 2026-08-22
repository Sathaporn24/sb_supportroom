import { describe, expect, it } from "vitest";
import {
  LESSON_PACING_FIELD_RANGES,
  parseLessonPacingField,
  type LessonPacingFieldName,
} from "@/components/admin/settings/lesson-pacing-fields";

const FIELDS = Object.keys(LESSON_PACING_FIELD_RANGES) as LessonPacingFieldName[];

describe("parseLessonPacingField (SP-14/SP-7)", () => {
  it.each(FIELDS)("%s: 0 is accepted", (field) => {
    expect(parseLessonPacingField(field, "0")).toEqual({ ok: true, value: 0 });
  });

  it.each(FIELDS)("%s: the top of its range is accepted", (field) => {
    const { max } = LESSON_PACING_FIELD_RANGES[field];
    expect(parseLessonPacingField(field, String(max))).toEqual({ ok: true, value: max });
  });

  it.each(FIELDS)("%s: one above the top of its range is rejected", (field) => {
    const { max } = LESSON_PACING_FIELD_RANGES[field];
    const result = parseLessonPacingField(field, String(max + 1));
    expect(result.ok).toBe(false);
  });

  it.each(FIELDS)("%s: an empty field is rejected, not silently turned into 0", (field) => {
    const result = parseLessonPacingField(field, "");
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error).not.toContain("0");
    }
  });

  it.each(FIELDS)("%s: whitespace-only is treated the same as empty", (field) => {
    expect(parseLessonPacingField(field, "   ").ok).toBe(false);
  });
});
