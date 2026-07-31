import { afterEach, describe, expect, it, vi } from "vitest";

describe("getProviderSelection", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.resetModules();
  });

  it("defaults every provider to mock when nothing is set", async () => {
    vi.stubEnv("DATA_PROVIDER", "");
    vi.stubEnv("SLIDES_PROVIDER", "");
    vi.stubEnv("TTS_PROVIDER", "");
    vi.stubEnv("VOICE_QUESTION_PROVIDER", "");
    const { getProviderSelection } = await import("@/config/env");
    expect(getProviderSelection()).toEqual({
      DATA_PROVIDER: "mock",
      SLIDES_PROVIDER: "mock",
      TTS_PROVIDER: "mock",
      VOICE_QUESTION_PROVIDER: "mock",
    });
  });

  it("honors an explicit real-provider selection", async () => {
    vi.stubEnv("SLIDES_PROVIDER", "google");
    const { getProviderSelection } = await import("@/config/env");
    expect(getProviderSelection().SLIDES_PROVIDER).toBe("google");
  });
});

describe("getGoogleServiceAccountEnv", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.resetModules();
  });

  it("throws a MissingEnvError listing every missing variable when credentials are absent", async () => {
    vi.stubEnv("GOOGLE_SERVICE_ACCOUNT_PROJECT_ID", "");
    vi.stubEnv("GOOGLE_SERVICE_ACCOUNT_EMAIL", "");
    vi.stubEnv("GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY", "");
    const { getGoogleServiceAccountEnv, MissingEnvError } = await import("@/config/env");
    expect(() => getGoogleServiceAccountEnv()).toThrow(MissingEnvError);
    try {
      getGoogleServiceAccountEnv();
      expect.unreachable();
    } catch (err) {
      expect(err).toBeInstanceOf(MissingEnvError);
      expect((err as InstanceType<typeof MissingEnvError>).missing).toEqual([
        "GOOGLE_SERVICE_ACCOUNT_PROJECT_ID",
        "GOOGLE_SERVICE_ACCOUNT_EMAIL",
        "GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY",
      ]);
    }
  });

  it("converts escaped \\n sequences in the private key to real newlines", async () => {
    vi.stubEnv("GOOGLE_SERVICE_ACCOUNT_PROJECT_ID", "proj");
    vi.stubEnv("GOOGLE_SERVICE_ACCOUNT_EMAIL", "sa@proj.iam.gserviceaccount.com");
    vi.stubEnv("GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY", "-----BEGIN KEY-----\\nabc\\n-----END KEY-----");
    const { getGoogleServiceAccountEnv } = await import("@/config/env");
    expect(getGoogleServiceAccountEnv().privateKey).toBe("-----BEGIN KEY-----\nabc\n-----END KEY-----");
  });
});
