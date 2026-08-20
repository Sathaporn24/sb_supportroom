import { describe, expect, it } from "vitest";
import { isValidLearnerName, LEARNER_NAME_MAX_LENGTH } from "@/utils/learner-name";

describe("isValidLearnerName", () => {
  it("accepts a trimmed name from 1 through 80 characters", () => {
    expect(isValidLearnerName("  สมศรี  ")).toBe(true);
    expect(isValidLearnerName("ก".repeat(LEARNER_NAME_MAX_LENGTH))).toBe(true);
  });

  it("rejects blank names and names longer than 80 characters", () => {
    expect(isValidLearnerName("   ")).toBe(false);
    expect(isValidLearnerName("ก".repeat(LEARNER_NAME_MAX_LENGTH + 1))).toBe(false);
  });
});
