"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { toast } from "@/components/ui/toast";

export function CopyLinkButton({ url }: { url: string }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      toast.add({ title: "คัดลอกลิงก์แล้ว", type: "success" });
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      // Clipboard API unavailable - user can still select the URL text manually.
      toast.add({ title: "คัดลอกลิงก์ไม่สำเร็จ", description: "กรุณาคัดลอกด้วยตนเอง", type: "error" });
    }
  }

  return (
    <Button variant="outline" size="sm" onClick={handleCopy}>
      {copied ? "คัดลอกแล้ว" : "คัดลอกลิงก์"}
    </Button>
  );
}
