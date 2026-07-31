"use client";

import { useEffect, useRef } from "react";
import { CameraOffIcon, MicOffIcon } from "@/components/ui/icons";

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

  return (
    <div
      className={`relative flex items-center gap-3 overflow-hidden rounded-xl border bg-room-panel p-4 transition-shadow ${
        speaking ? "border-room-accent shadow-speaking" : "border-room-border"
      }`}
    >
      {cameraOn && stream ? (
        <video ref={videoRef} autoPlay muted playsInline className="h-12 w-12 shrink-0 rounded-full object-cover" />
      ) : (
        <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-room-panelAlt text-room-text">
          <CameraOffIcon className="h-5 w-5 text-room-muted" />
        </div>
      )}
      <div className="min-w-0">
        <p className="truncate text-sm font-semibold text-room-text">{teacherName || `คุณครู${initial}`}</p>
        <p className="flex items-center gap-1 text-xs text-room-muted">
          {!micOn && <MicOffIcon className="h-3.5 w-3.5 text-red-400" />}
          {micOn ? "ไมค์เปิดอยู่" : "ปิดไมค์อยู่"}
        </p>
      </div>
    </div>
  );
}
