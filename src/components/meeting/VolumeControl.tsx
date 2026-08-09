"use client";

import { useEffect, useRef, useState } from "react";
import { IconButton } from "@/components/ui/IconButton";
import { SpeakerIcon, SpeakerLowIcon, SpeakerMuteIcon } from "@/components/ui/icons";

type Props = {
  /** Current AI playback volume, 0-1. */
  volume: number;
  /** Called with the new volume (0-1). Applies live, without interrupting playback. */
  onChange: (volume: number) => void;
};

/**
 * Speaker button that opens a slider popover to set the AI's playback volume. Turning the AI
 * down here does NOT interrupt it (unlike push-to-talk, which stops it entirely) - the change
 * is applied to the clip currently playing. The mute toggle remembers the previous level so
 * un-muting restores it.
 */
export function VolumeControl({ volume, onChange }: Props) {
  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement | null>(null);
  // Remembers the level to jump back to when un-muting a volume that was dragged to (or muted to) 0.
  const lastNonZeroRef = useRef(volume > 0 ? volume : 1);

  useEffect(() => {
    if (volume > 0) lastNonZeroRef.current = volume;
  }, [volume]);

  // Close the popover on an outside click or Escape.
  useEffect(() => {
    if (!open) return;
    function onPointerDown(e: PointerEvent) {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) setOpen(false);
    }
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  const muted = volume <= 0;
  const Icon = muted ? SpeakerMuteIcon : volume < 0.5 ? SpeakerLowIcon : SpeakerIcon;
  const percent = Math.round(volume * 100);

  function toggleMute() {
    onChange(muted ? lastNonZeroRef.current || 1 : 0);
  }

  return (
    <div ref={wrapRef} className="relative">
      <IconButton
        label={`เสียง AI (${percent}%)`}
        icon={<Icon />}
        aria-haspopup="true"
        aria-expanded={open}
        onClick={() => setOpen((prev) => !prev)}
      />
      {open && (
        <div
          className="absolute bottom-full left-1/2 mb-3 flex w-52 -translate-x-1/2 items-center gap-3 rounded-xl border border-room-border bg-room-panel px-4 py-3 shadow-lg"
          role="group"
          aria-label="ปรับระดับเสียง AI"
        >
          <button
            type="button"
            onClick={toggleMute}
            aria-label={muted ? "เปิดเสียง AI" : "ปิดเสียง AI"}
            title={muted ? "เปิดเสียง AI" : "ปิดเสียง AI"}
            className="shrink-0 text-room-muted transition-colors hover:text-room-text focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-room-accent"
          >
            <Icon />
          </button>
          <input
            type="range"
            min={0}
            max={100}
            value={percent}
            onChange={(e) => onChange(Number(e.target.value) / 100)}
            aria-label="ระดับเสียง AI"
            className="h-1.5 flex-1 cursor-pointer accent-room-accent"
          />
          <span className="w-9 shrink-0 text-right text-xs tabular-nums text-room-muted">{percent}%</span>
        </div>
      )}
    </div>
  );
}
