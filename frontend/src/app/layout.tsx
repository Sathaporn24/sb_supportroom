import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";

export const metadata: Metadata = {
  title: "SupportRoom AI",
  description: "ห้องสอนการใช้งานระบบแบบสนทนาโต้ตอบ (Mock Demo)",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="th">
      <body className="min-h-screen bg-room-bg text-room-text antialiased">{children}</body>
    </html>
  );
}
