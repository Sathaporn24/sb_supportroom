"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { MicIcon, MicOffIcon, VideoIcon, VideoOffIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { isSessionJoinable } from "@/utils/session-status";
import { useLocalMedia } from "@/hooks/use-local-media";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { Spinner } from "@/components/ui/spinner";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
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
      <Card className="w-full max-w-lg">
        <CardContent className="flex flex-col gap-5">
          <div>
            <p className="text-xs text-muted-foreground">ห้องสอนการใช้งานระบบ</p>
            <h1 className="text-lg font-semibold">{lessonTitle}</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              ผู้สอน: School Bright Support
              {session.recipientName ? ` · ${session.recipientName}` : ""}
              {session.recipientOrgName ? ` · ${session.recipientOrgName}` : ""}
            </p>
          </div>

          <div className="flex aspect-video items-center justify-center overflow-hidden rounded-xl border bg-muted">
            {media.requesting ? (
              <div className="flex flex-col items-center gap-2 text-muted-foreground">
                <Spinner className="size-6" />
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
              <VideoOffIcon className="size-10 text-muted-foreground" />
            )}
          </div>

          {media.error && <p className="text-sm text-destructive">{errorMessages[media.error]}</p>}

          <div className="flex items-center gap-3">
            <Button
              variant={media.micOn ? "outline" : "destructive"}
              size="icon-lg"
              className="rounded-full"
              title={media.micOn ? "ปิดไมค์" : "เปิดไมค์"}
              aria-label={media.micOn ? "ปิดไมค์" : "เปิดไมค์"}
              onClick={() => {
                if (!media.stream) {
                  void media.requestMedia(media.cameraOn, true);
                } else {
                  media.toggleMic();
                }
              }}
            >
              {media.micOn ? <MicIcon /> : <MicOffIcon />}
            </Button>
            <Button
              variant={media.cameraOn ? "outline" : "destructive"}
              size="icon-lg"
              className="rounded-full"
              title={media.cameraOn ? "ปิดกล้อง" : "เปิดกล้อง"}
              aria-label={media.cameraOn ? "ปิดกล้อง" : "เปิดกล้อง"}
              onClick={() => void media.toggleCamera()}
            >
              {media.cameraOn ? <VideoIcon /> : <VideoOffIcon />}
            </Button>
            {media.micOn && media.stream && (
              <Progress
                value={Math.round(media.micLevel * 100)}
                aria-label="ระดับเสียงไมโครโฟน"
                className="flex-1"
              />
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
        </CardContent>
      </Card>
    </main>
  );
}
