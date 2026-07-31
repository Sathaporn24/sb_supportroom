import { afterEach, describe, expect, it, vi } from "vitest";

describe("getDefaultSessionExpiryHours", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.resetModules();
  });

  it("defaults to 24 hours when DEFAULT_SESSION_EXPIRY_HOURS is not set", async () => {
    vi.stubEnv("DEFAULT_SESSION_EXPIRY_HOURS", "");
    const { getDefaultSessionExpiryHours } = await import("@/config/server-defaults");
    expect(getDefaultSessionExpiryHours()).toBe(24);
  });

  it("honors an explicit override", async () => {
    vi.stubEnv("DEFAULT_SESSION_EXPIRY_HOURS", "48");
    const { getDefaultSessionExpiryHours } = await import("@/config/server-defaults");
    expect(getDefaultSessionExpiryHours()).toBe(48);
  });
});
