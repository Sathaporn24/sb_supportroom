import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/app/**/*.{ts,tsx}",
    "./src/components/**/*.{ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        room: {
          bg: "#ffffff",
          panel: "#fffaf5",
          panelAlt: "#fff1e2",
          border: "#f3d9bb",
          accent: "#f97316",
          accentSoft: "#ffe4c7",
          text: "#3a2617",
          muted: "#9a7b5c",
        },
      },
      boxShadow: {
        speaking: "0 0 0 3px rgba(249,115,22,0.65)",
      },
      keyframes: {
        pulseSoft: {
          "0%, 100%": { opacity: "1" },
          "50%": { opacity: "0.55" },
        },
      },
      animation: {
        "pulse-soft": "pulseSoft 1.6s ease-in-out infinite",
      },
    },
  },
  plugins: [],
};

export default config;
