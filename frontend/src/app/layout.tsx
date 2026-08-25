import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";
import { Kanit } from "next/font/google";
import { cn } from "@/lib/utils";
import { Toaster } from "@/components/ui/toast";

/** Replaced IBM Plex Sans Thai Looped (2026-08-25) - user asked for Kanit specifically. Before
 * that this was Geist, which has no Thai glyphs at all, so Thai text used to silently render in
 * whatever fallback the OS/browser picked, never a deliberately chosen typeface. */
const kanit = Kanit({
  subsets: ["thai", "latin"],
  weight: ["300", "400", "500", "600", "700"],
  variable: "--font-sans",
});

export const metadata: Metadata = {
  title: "SupportRoom AI",
  description: "ห้องสอนการใช้งานระบบแบบสนทนาโต้ตอบ (Mock Demo)",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="th" className={cn("font-sans", kanit.variable)}>
      <body className="min-h-screen antialiased">
        <Toaster>{children}</Toaster>
      </body>
    </html>
  );
}
