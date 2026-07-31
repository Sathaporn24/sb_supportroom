"use client";

import { useEffect, useRef } from "react";
import { MicOffIcon } from "@/components/ui/icons";

type Props = {
  stream: MediaStream | null;
  cameraOn: boolean;
  micOn: boolean;
  speaking: boolean;
  teacherName?: string;
};

export function TeacherTile({ stream, cameraOn, micOn, speaking, teacherName }: Props) {
  const videoRef = useRef<HTMLVideoElement | null>(null);

  useEffect(() => {
    if (videoRef.current) {
      videoRef.current.srcObject = cameraOn ? stream : null;
    }
  }, [stream, cameraOn]);

  const initial = teacherName?.trim()?.[0] ?? "ค";
  const displayName = teacherName || `คุณครู${initial}`;

  return (
    <div
      className={`relative aspect-video w-full overflow-hidden rounded-xl border bg-room-panel transition-shadow ${
        speaking ? "border-room-accent shadow-speaking" : "border-room-border"
      }`}
    >
      {cameraOn && stream ? (
        <video ref={videoRef} autoPlay muted playsInline className="h-full w-full object-cover" />
      ) : (
        <div className="flex h-full w-full items-center justify-center bg-room-panelAlt">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-room-accentSoft text-2xl font-semibold text-room-accent">
            {initial}
          </div>
        </div>
      )}

      <div
        className={`absolute inset-x-0 bottom-0 flex items-center justify-between gap-2 px-3 py-2 ${
          cameraOn ? "bg-gradient-to-t from-black/60 to-transparent" : ""
        }`}
      >
        <p className={`truncate text-sm font-medium ${cameraOn ? "text-white" : "text-room-text"}`}>{displayName}</p>
        {!micOn && (
          <span
            className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-full ${
              cameraOn ? "bg-red-500/80" : "bg-red-500/15"
            }`}
          >
            <MicOffIcon className={`h-3.5 w-3.5 ${cameraOn ? "text-white" : "text-red-600"}`} />
          </span>
        )}
      </div>
    </div>
  );
}
