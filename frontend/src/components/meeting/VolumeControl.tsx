"use client";

import { useEffect, useRef } from "react";
import { Volume1Icon, Volume2Icon, VolumeXIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Slider } from "@/components/ui/slider";

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
  // Remembers the level to jump back to when un-muting a volume that was dragged to (or muted to) 0.
  const lastNonZeroRef = useRef(volume > 0 ? volume : 1);

  useEffect(() => {
    if (volume > 0) lastNonZeroRef.current = volume;
  }, [volume]);

  const muted = volume <= 0;
  const Icon = muted ? VolumeXIcon : volume < 0.5 ? Volume1Icon : Volume2Icon;
  const percent = Math.round(volume * 100);

  function toggleMute() {
    onChange(muted ? lastNonZeroRef.current || 1 : 0);
  }

  return (
    <Popover>
      <PopoverTrigger
        render={
          <Button
            variant="outline"
            size="icon-lg"
            className="size-11 rounded-full"
            title={`เสียง AI (${percent}%)`}
            aria-label={`เสียง AI (${percent}%)`}
            data-testid="room-volume-trigger-button"
          />
        }
      >
        <Icon />
      </PopoverTrigger>
      <PopoverContent
        side="top"
        className="w-52 max-w-[calc(100vw-2rem)] flex-row items-center gap-3"
        aria-label="ปรับระดับเสียง AI"
      >
        <Button
          variant="ghost"
          size="icon-sm"
          onClick={toggleMute}
          aria-label={muted ? "เปิดเสียง AI" : "ปิดเสียง AI"}
          title={muted ? "เปิดเสียง AI" : "ปิดเสียง AI"}
          data-testid="room-volume-mute-button"
        >
          <Icon />
        </Button>
        <Slider
          value={percent}
          min={0}
          max={100}
          onValueChange={(value) => onChange((Array.isArray(value) ? value[0] : value) / 100)}
          aria-label="ระดับเสียง AI"
          data-testid="room-volume-slider"
          className="flex-1"
        />
        <span className="w-9 shrink-0 text-right text-xs tabular-nums text-muted-foreground">{percent}%</span>
      </PopoverContent>
    </Popover>
  );
}
