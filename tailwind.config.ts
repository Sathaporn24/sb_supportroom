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
          bg: "#0b0f14",
          panel: "#111823",
          panelAlt: "#161f2c",
          border: "#243040",
          accent: "#22c55e",
          accentSoft: "#16351f",
          text: "#e6edf5",
          muted: "#8ea0b5",
        },
      },
      boxShadow: {
        speaking: "0 0 0 3px rgba(34,197,94,0.65)",
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
