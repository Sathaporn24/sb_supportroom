"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { isSessionJoinable } from "@/utils/session-status";
import { useLocalMedia } from "@/hooks/use-local-media";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { IconButton } from "@/components/ui/IconButton";
import { LoadingBlock } from "@/components/ui/LoadingBlock";
import { Spinner } from "@/components/ui/Spinner";
import { CameraIcon, CameraOffIcon, MicIcon, MicOffIcon } from "@/components/ui/icons";
import type { TrainingSession } from "@/types/domain";

const errorMessages: Record<string, string> = {
  denied: "ไม่ได้รับอนุญาตให้เข้าถึงกล้องหรือไมโครโฟน กรุณาอนุญาตการใช้งานจากเบราว์เซอร์แล้วลองใหม่อีกครั้งค่ะ",
  "not-found": "ไม่พบกล้องหรือไมโครโฟนในอุปกรณ์นี้ค่ะ",
  unsupported: "เบราว์เซอร์นี้ไม่รองรับการใช้งานกล้องหรือไมโครโฟนค่ะ",
  unknown: "เกิดข้อผิดพลาดในการเข้าถึงกล้องหรือไมโครโฟน กรุณาลองใหม่อีกครั้งค่ะ",
};

export default function JoinPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [session, setSession] = useState<TrainingSession | null | "loading">("loading");
  const [lessonTitle, setLessonTitle] = useState("");
  const media = useLocalMedia();

  useEffect(() => {
    let active = true;
    void api
      .getSessionByToken(params.token)
      .then(({ session: found, lessonTitle: title }) => {
        if (!active) return;
        if (!isSessionJoinable(found)) {
          router.replace("/link-expired");
          return;
        }
        setSession(found);
        setLessonTitle(title);
      })
      .catch(() => {
        if (active) router.replace("/link-expired");
      });
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.token]);

  if (session === "loading") {
    return (
      <main className="flex min-h-screen items-center justify-center p-6">
        <LoadingBlock label="กำลังโหลดข้อมูลห้องสอน..." />
      </main>
    );
  }
  if (!session) {
    return null;
  }

  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-lg space-y-5">
        <div>
          <p className="text-xs text-room-muted">ห้องสอนการใช้งานระบบ</p>
          <h1 className="text-lg font-semibold text-room-text">{lessonTitle}</h1>
          <p className="mt-1 text-sm text-room-muted">
            ผู้สอน: School Bright Support
            {session.recipientName ? ` · ${session.recipientName}` : ""}
            {session.recipientOrgName ? ` · ${session.recipientOrgName}` : ""}
          </p>
        </div>

        <div className="flex aspect-video items-center justify-center overflow-hidden rounded-xl border border-room-border bg-room-panelAlt">
          {media.requesting ? (
            <div className="flex flex-col items-center gap-2 text-room-muted">
              <Spinner className="h-6 w-6" />
              <p className="text-xs">กำลังขอสิทธิ์เข้าถึงกล้อง/ไมโครโฟน...</p>
            </div>
          ) : media.cameraOn && media.stream ? (
            <video
              autoPlay
              muted
              playsInline
              className="h-full w-full object-cover"
              ref={(el) => {
                if (el) el.srcObject = media.stream;
              }}
            />
          ) : (
            <CameraOffIcon className="h-10 w-10 text-room-muted" />
          )}
        </div>

        {media.error && <p className="text-sm text-red-600">{errorMessages[media.error]}</p>}

        <div className="flex items-center gap-3">
          <IconButton
            label={media.micOn ? "ปิดไมค์" : "เปิดไมค์"}
            active={media.micOn}
            icon={media.micOn ? <MicIcon /> : <MicOffIcon />}
            onClick={() => {
              if (!media.stream) {
                void media.requestMedia(media.cameraOn, true);
              } else {
                media.toggleMic();
              }
            }}
          />
          <IconButton
            label={media.cameraOn ? "ปิดกล้อง" : "เปิดกล้อง"}
            active={media.cameraOn}
            icon={media.cameraOn ? <CameraIcon /> : <CameraOffIcon />}
            onClick={() => void media.toggleCamera()}
          />
          {media.micOn && media.stream && (
            <div className="h-2 flex-1 overflow-hidden rounded-full bg-room-panelAlt">
              <div
                className="h-full bg-room-accent transition-all"
                style={{ width: `${Math.round(media.micLevel * 100)}%` }}
              />
            </div>
          )}
        </div>

        <Button
          className="w-full"
          onClick={() => {
            media.stopStream();
            router.push(`/room/${params.token}`);
          }}
        >
          เข้าร่วมห้องสอน
        </Button>
      </Card>
    </main>
  );
}
