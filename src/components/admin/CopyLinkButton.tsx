"use client";

import { useState } from "react";

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
    <button
      onClick={handleCopy}
      className="rounded-md border border-room-border bg-room-panelAlt px-2.5 py-1.5 text-xs text-room-text hover:border-room-accent/60"
    >
      {copied ? "คัดลอกแล้ว" : "คัดลอกลิงก์"}
    </button>
  );
}
