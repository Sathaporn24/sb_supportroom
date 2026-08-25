import type { Viewport } from "next";
import type { ReactNode } from "react";

// RS-3 - a layout of its own (not app/layout.tsx) because room/[token]/page.tsx is "use client"
// and a client component cannot export `viewport`, and because editing the root layout would
// also change /admin/* keyboard behavior, which RS-1 says this round must not touch.
export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  interactiveWidget: "resizes-content",
};

export default function RoomLayout({ children }: { children: ReactNode }) {
  return children;
}
