// Vitest runs on plain Node, not through Next.js's webpack build, so the real
// "server-only" package (which throws unconditionally) would break every test that
// transitively imports server code. vitest.config.ts aliases "server-only" to this
// no-op file instead.
export {};
