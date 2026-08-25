import { defineConfig } from "vitest/config";
import path from "path";
import { fileURLToPath } from "url";

const dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  // tsconfig.json sets "jsx": "preserve" for Next's own SWC compiler - Vite's oxc transform
  // otherwise reads that same tsconfig setting and leaves JSX untransformed, which breaks
  // import analysis for any .tsx test. This overrides oxc's JSX handling for the test run only.
  oxc: {
    jsx: { runtime: "automatic" },
  },
  test: {
    environment: "node",
    // Component tests opt into jsdom per-file via a `@vitest-environment jsdom` docblock
    // (see AskAiDrawer.test.tsx) - pure-logic tests stay on the faster "node" default above.
    include: ["src/**/*.test.ts", "src/**/*.test.tsx"],
  },
  resolve: {
    alias: {
      "@": path.resolve(dirname, "./src"),
      "server-only": path.resolve(dirname, "./src/test/server-only-shim.ts"),
    },
  },
});
