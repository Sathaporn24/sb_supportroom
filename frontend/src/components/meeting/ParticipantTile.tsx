"use client";

import { useEffect, useRef } from "react";
import { MicOffIcon } from "lucide-react";
import { cn } from "@/lib/utils";

type Props = {
  stream: MediaStream | null;
  cameraOn: boolean;
  micOn: boolean;
  speaking: boolean;
  recipientName?: string;
};

export function ParticipantTile({ stream, cameraOn, micOn, speaking, recipientName }: Props) {
  const videoRef = useRef<HTMLVideoElement | null>(null);

  useEffect(() => {
    if (videoRef.current) {
      videoRef.current.srcObject = cameraOn ? stream : null;
    }
  }, [stream, cameraOn]);

  // Avatar-circle initial and the no-name fallback text are two independent things - reusing
  // one inside the other produced "ผู้เข้าร่วมผ" (the fallback initial glued onto its own fallback text).
  const trimmedName = recipientName?.trim();
  const initial = trimmedName?.[0] ?? "ผ";
  const displayName = trimmedName || "ผู้เข้าร่วม";

  return (
    <div
      className={cn(
        "relative aspect-video w-full overflow-hidden rounded-xl border bg-card transition-shadow",
        speaking ? "border-primary ring-3 ring-primary/60" : "border-border",
      )}
    >
      {cameraOn && stream ? (
        <video ref={videoRef} autoPlay muted playsInline className="h-full w-full object-cover" />
      ) : (
        <div className="flex h-full w-full items-center justify-center bg-muted">
          <div className="flex size-16 items-center justify-center rounded-full bg-primary/10 text-2xl font-semibold text-primary">
            {initial}
          </div>
        </div>
      )}

      <div
        className={cn(
          "absolute inset-x-0 bottom-0 flex items-center justify-between gap-2 px-3 py-2",
          cameraOn && "bg-gradient-to-t from-black/60 to-transparent",
        )}
      >
        <p className={cn("truncate text-sm font-medium", cameraOn ? "text-white" : "text-foreground")}>
          {displayName}
        </p>
        {!micOn && (
          <span
            className={cn(
              "flex size-6 shrink-0 items-center justify-center rounded-full",
              cameraOn ? "bg-destructive/80" : "bg-destructive/15",
            )}
          >
            <MicOffIcon className={cn("size-3.5", cameraOn ? "text-white" : "text-destructive")} />
          </span>
        )}
      </div>
    </div>
  );
}
