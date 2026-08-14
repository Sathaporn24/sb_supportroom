"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { isLinkUsable } from "@/utils/session-status";
import { getLearnerName, getOrCreateLearnerKey, peekLearnerKey, setLearnerName } from "@/utils/learner-key";
import { useLocalMedia } from "@/hooks/use-local-media";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { IconButton } from "@/components/ui/IconButton";
import { LoadingBlock } from "@/components/ui/LoadingBlock";
import { Spinner } from "@/components/ui/Spinner";
import { CameraIcon, CameraOffIcon, MicIcon, MicOffIcon } from "@/components/ui/icons";
import type { PublicTrainingLink } from "@/types/domain";

const errorMessages: Record<string, string> = {
  denied: "ไม่ได้รับอนุญาตให้เข้าถึงกล้องหรือไมโครโฟน กรุณาอนุญาตการใช้งานจากเบราว์เซอร์แล้วลองใหม่อีกครั้งค่ะ",
  "not-found": "ไม่พบกล้องหรือไมโครโฟนในอุปกรณ์นี้ค่ะ",
  unsupported: "เบราว์เซอร์นี้ไม่รองรับการใช้งานกล้องหรือไมโครโฟนค่ะ",
  unknown: "เกิดข้อผิดพลาดในการเข้าถึงกล้องหรือไมโครโฟน กรุณาลองใหม่อีกครั้งค่ะ",
};

/** Matches DtoLimits.RecipientNameMaxLength - the server rejects anything longer. */
const NAME_MAX_LENGTH = 100;

export default function JoinPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [link, setLink] = useState<PublicTrainingLink | null | "loading">("loading");
  const [lessonTitle, setLessonTitle] = useState("");
  const [name, setName] = useState("");
  const [joining, setJoining] = useState(false);
  const [joinError, setJoinError] = useState<string | null>(null);
  const media = useLocalMedia();

  useEffect(() => {
    let active = true;
    void api
      .getTrainingLinkByToken(params.token)
      .then(async ({ link: found, lessonTitle: title }) => {
        if (!active) return;

        // Returning learners skip this screen entirely, exactly as CORE_FEATURE_SPEC §5.1 says.
        // Join is idempotent and now returns an existing run even after link expiry, so someone
        // who started in time can reconnect and finish without being locked out by the clock.
        const existingKey = peekLearnerKey(params.token);
        const existingName = getLearnerName(params.token);
        if (existingKey && existingName) {
          try {
            const { learningSession } = await api.joinLearningSession(params.token, {
              recipientName: existingName,
              learnerKey: existingKey,
            });
            if (!active) return;
            router.replace(
              learningSession.status === "ENDED"
                ? `/session-ended/${params.token}`
                : `/room/${params.token}`,
            );
            return;
          } catch {
            // A cleared DB plus an expired link, for example, is honestly an expired-link case.
            // For an active link fall through and let the learner join again with the saved name.
            if (!isLinkUsable(found)) {
              router.replace("/link-expired");
              return;
            }
          }
        }

        if (!isLinkUsable(found)) {
          router.replace("/link-expired");
          return;
        }
        setLink(found);
        setLessonTitle(title);
        // A returning learner never retypes their name - the field is prefilled from the last
        // time this browser joined this link.
        setName(existingName ?? "");
      })
      .catch(() => {
        if (active) router.replace("/link-expired");
      });
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.token]);

  const trimmedName = name.trim();
  const canJoin = trimmedName.length > 0 && !joining;

  async function handleJoin() {
    if (!canJoin) return;
    setJoining(true);
    setJoinError(null);
    try {
      // Creating the session here rather than inside the room means the room always opens onto a
      // session that already exists - no half-joined state to reason about if the request fails.
      await api.joinLearningSession(params.token, {
        recipientName: trimmedName,
        learnerKey: getOrCreateLearnerKey(params.token),
      });
      setLearnerName(params.token, trimmedName);
      media.stopStream();
      router.push(`/room/${params.token}`);
    } catch (err) {
      setJoining(false);
      setJoinError(err instanceof Error ? err.message : "เข้าห้องเรียนไม่สำเร็จ กรุณาลองใหม่อีกครั้งค่ะ");
    }
  }

  if (link === "loading") {
    return (
      <main className="flex min-h-screen items-center justify-center p-6">
        <LoadingBlock label="กำลังโหลดข้อมูลห้องเรียน..." />
      </main>
    );
  }
  if (!link) {
    return null;
  }

  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-lg space-y-5">
        <div>
          <p className="text-xs text-room-muted">ห้องเรียนการใช้งานระบบ</p>
          <h1 className="text-lg font-semibold text-room-text">{lessonTitle}</h1>
          {link.recipientOrgName && <p className="mt-1 text-sm text-room-muted">{link.recipientOrgName}</p>}
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

        {/* The only field on this screen. No email, no phone, no organization - CS already
            recorded the organization on the link (CORE_FEATURE_SPEC §5.1). */}
        <div className="space-y-1">
          <label htmlFor="learner-name" className="text-sm font-medium text-room-text">
            ชื่อของคุณ
          </label>
          <input
            id="learner-name"
            type="text"
            value={name}
            maxLength={NAME_MAX_LENGTH}
            autoComplete="name"
            placeholder="เช่น ครูสมศรี"
            onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void handleJoin();
            }}
            className="w-full rounded-lg border border-room-border bg-room-panelAlt px-3 py-2 text-sm text-room-text outline-none focus:border-room-accent"
          />
        </div>

        {joinError && <p className="text-sm text-red-600">{joinError}</p>}

        <Button className="w-full" disabled={!canJoin} onClick={() => void handleJoin()}>
          {joining ? "กำลังเข้าห้องเรียน..." : "เข้าห้องเรียน"}
        </Button>
      </Card>
    </main>
  );
}
