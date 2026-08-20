import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";
import { Geist } from "next/font/google";
import { cn } from "@/lib/utils";
import { Toaster } from "@/components/ui/toast";

const geist = Geist({ subsets: ["latin"], variable: "--font-sans" });

export const metadata: Metadata = {
  title: "SupportRoom AI",
  description: "ห้องสอนการใช้งานระบบแบบสนทนาโต้ตอบ (Mock Demo)",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="th" className={cn("font-sans", geist.variable)}>
      <body className="min-h-screen antialiased">
        <Toaster>{children}</Toaster>
      </body>
    </html>
  );
}
