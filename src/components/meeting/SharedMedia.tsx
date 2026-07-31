"use client";

import { useEffect, useState } from "react";
import type { DemoMedia } from "@/types/domain";

type Props = {
  media?: DemoMedia;
};

export function SharedMedia({ media }: Props) {
  const [status, setStatus] = useState<"loading" | "ready" | "error">(media ? "loading" : "ready");

  const mediaId = media?.id;
  useEffect(() => {
    setStatus(mediaId ? "loading" : "ready");
  }, [mediaId]);

  if (!media) {
    return (
      <div className="flex h-full min-h-[280px] items-center justify-center rounded-xl border border-room-border bg-room-panel text-room-muted">
        กำลังเตรียมสื่อการสอน...
      </div>
    );
  }

  return (
    <div className="relative flex h-full min-h-[280px] items-center justify-center overflow-hidden rounded-xl border border-room-border bg-room-panel">
      {status === "loading" && (
        <div className="absolute inset-0 flex items-center justify-center text-room-muted">กำลังโหลด...</div>
      )}
      {status === "error" && (
        <div className="absolute inset-0 flex items-center justify-center text-room-muted">
          ไม่สามารถแสดงสื่อได้ในขณะนี้
        </div>
      )}
      {media.kind === "video" ? (
        <video
          key={media.id}
          src={media.src}
          autoPlay
          muted
          loop
          playsInline
          className={`h-full w-full object-contain transition-opacity ${status === "ready" ? "opacity-100" : "opacity-0"}`}
          onCanPlay={() => setStatus("ready")}
          onError={() => setStatus("error")}
        />
      ) : (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          key={media.id}
          src={media.src}
          alt={media.label}
          className={`h-full w-full object-contain transition-opacity ${status === "ready" ? "opacity-100" : "opacity-0"}`}
          onLoad={() => setStatus("ready")}
          onError={() => setStatus("error")}
        />
      )}
    </div>
  );
}
