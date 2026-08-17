"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";

export function CopyLinkButton({ url }: { url: string }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      // Clipboard API unavailable - user can still select the URL text manually.
    }
  }

  return (
    <Button variant="outline" size="sm" onClick={handleCopy}>
      {copied ? "คัดลอกแล้ว" : "คัดลอกลิงก์"}
    </Button>
  );
}
