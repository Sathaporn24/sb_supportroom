import { z } from "zod";

// Provider switches are server-only selectors (they decide which Route Handler logic
// runs) - they intentionally do NOT use the NEXT_PUBLIC_ prefix and are never read
// from client components. Mock is always the default so the app runs with no keys.
const providerSelectionSchema = z.object({
  DATA_PROVIDER: z.enum(["mock", "supabase"]).default("mock"),
  SLIDES_PROVIDER: z.enum(["mock", "google"]).default("mock"),
  TTS_PROVIDER: z.enum(["mock", "edge"]).default("mock"),
  VOICE_QUESTION_PROVIDER: z.enum(["mock", "gemini"]).default("mock"),
});

export type ProviderSelection = z.infer<typeof providerSelectionSchema>;

// A blank (but present) env var - e.g. `DATA_PROVIDER=` left empty in .env - must fall
// back to the default exactly like an unset one, not fail validation.
function orUndefined(value: string | undefined): string | undefined {
  return value ? value : undefined;
}

export function getProviderSelection(): ProviderSelection {
  return providerSelectionSchema.parse({
    DATA_PROVIDER: orUndefined(process.env.DATA_PROVIDER),
    SLIDES_PROVIDER: orUndefined(process.env.SLIDES_PROVIDER),
    TTS_PROVIDER: orUndefined(process.env.TTS_PROVIDER),
    VOICE_QUESTION_PROVIDER: orUndefined(process.env.VOICE_QUESTION_PROVIDER),
  });
}

export class MissingEnvError extends Error {
  constructor(public readonly missing: string[]) {
    super(
      `Missing required environment variable(s): ${missing.join(", ")}. ` +
        "See docs/ENVIRONMENT_SETUP.md for where to get each value.",
    );
    this.name = "MissingEnvError";
  }
}

function requireEnv(keys: string[]): Record<string, string> {
  const missing = keys.filter((key) => !process.env[key]);
  if (missing.length > 0) {
    throw new MissingEnvError(missing);
  }
  return Object.fromEntries(keys.map((key) => [key, process.env[key] as string]));
}

export function getGoogleServiceAccountEnv() {
  const values = requireEnv([
    "GOOGLE_SERVICE_ACCOUNT_PROJECT_ID",
    "GOOGLE_SERVICE_ACCOUNT_EMAIL",
    "GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY",
  ]);
  return {
    projectId: values.GOOGLE_SERVICE_ACCOUNT_PROJECT_ID,
    clientEmail: values.GOOGLE_SERVICE_ACCOUNT_EMAIL,
    // .env files can't hold real newlines - the key is pasted with literal "\n" sequences.
    privateKey: values.GOOGLE_SERVICE_ACCOUNT_PRIVATE_KEY.replace(/\\n/g, "\n"),
  };
}

export function getGeminiEnv() {
  const values = requireEnv(["GEMINI_API_KEY"]);
  // gemini-1.5-flash is retired - verified gemini-flash-latest works against the real API.
  return {
    apiKey: values.GEMINI_API_KEY,
    model: process.env.GEMINI_MODEL || "gemini-flash-latest",
  };
}

export function getSupabaseEnv() {
  const values = requireEnv([
    "NEXT_PUBLIC_SUPABASE_URL",
    "NEXT_PUBLIC_SUPABASE_ANON_KEY",
    "SUPABASE_SERVICE_ROLE_KEY",
  ]);
  return {
    url: values.NEXT_PUBLIC_SUPABASE_URL,
    anonKey: values.NEXT_PUBLIC_SUPABASE_ANON_KEY,
    serviceRoleKey: values.SUPABASE_SERVICE_ROLE_KEY,
  };
}

export const uploadLimits = {
  maxVoiceUploadMb: Number(process.env.MAX_VOICE_UPLOAD_MB ?? 5),
  minVoiceDurationMs: Number(process.env.MIN_VOICE_DURATION_MS ?? 300),
};
