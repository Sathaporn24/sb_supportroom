import "server-only";
import { createClient, type SupabaseClient } from "@supabase/supabase-js";
import { getSupabaseEnv } from "@/config/env";

let cachedClient: SupabaseClient | null = null;

// PREPARED — CREDENTIALS REQUIRED. Uses the service role key, so this must only ever
// be imported from server-side code (Route Handlers / repositories) - never a Client
// Component. See docs/SUPABASE_SETUP_AND_SCHEMA.md.
export function getSupabaseServiceClient(): SupabaseClient {
  if (cachedClient) {
    return cachedClient;
  }
  const { url, serviceRoleKey } = getSupabaseEnv();
  cachedClient = createClient(url, serviceRoleKey, {
    auth: { persistSession: false },
  });
  return cachedClient;
}
