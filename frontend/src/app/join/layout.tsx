import type { Viewport } from "next";
import type { ReactNode } from "react";

// RS-3 - same reasoning as app/room/layout.tsx: join/[token]/page.tsx is "use client" and cannot
// export `viewport` itself, and editing the root layout would also affect /admin/* (RS-1).
export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  interactiveWidget: "resizes-content",
};

export default function JoinLayout({ children }: { children: ReactNode }) {
  return children;
}
