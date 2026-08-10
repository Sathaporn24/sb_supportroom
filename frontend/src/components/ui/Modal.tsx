"use client";

import { useEffect } from "react";
import type { ReactNode } from "react";

type Props = {
  open: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
};

export function Modal({ open, onClose, title, children }: Props) {
  useEffect(() => {
    if (!open) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose();
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} aria-hidden="true" />
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="relative max-h-[90vh] w-full max-w-md overflow-y-auto rounded-xl border border-room-border bg-room-panel p-5 shadow-2xl"
      >
        {title && (
          <div className="mb-4 flex items-center justify-between gap-3">
            <h2 className="text-base font-semibold text-room-text">{title}</h2>
            <button
              onClick={onClose}
              aria-label="ปิดหน้าต่าง"
              className="shrink-0 rounded-md px-2 py-1 text-sm text-room-muted hover:bg-room-panelAlt hover:text-room-text"
            >
              ปิด
            </button>
          </div>
        )}
        {children}
      </div>
    </div>
  );
}
