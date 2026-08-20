"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { MicIcon, MicOffIcon, VideoIcon, VideoOffIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { getLearnerName, getOrCreateLearnerKey, grantRoomEntry, peekLearnerKey, setLearnerName } from "@/utils/learner-key";
import { isValidLearnerName, LEARNER_NAME_MAX_LENGTH } from "@/utils/learner-name";
import { useLocalMedia } from "@/hooks/use-local-media";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { Spinner } from "@/components/ui/spinner";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import type { LearningResumeState } from "@/types/domain";

const errorMessages: Record<string, string> = {
  denied: "ไม่ได้รับอนุญาตให้เข้าถึงกล้องหรือไมโครโฟน กรุณาอนุญาตการใช้งานจากเบราว์เซอร์แล้วลองใหม่อีกครั้งค่ะ",
  "not-found": "ไม่พบกล้องหรือไมโครโฟนในอุปกรณ์นี้ค่ะ",
  unsupported: "เบราว์เซอร์นี้ไม่รองรับการใช้งานกล้องหรือไมโครโฟนค่ะ",
  unknown: "เกิดข้อผิดพลาดในการเข้าถึงกล้องหรือไมโครโฟน กรุณาลองใหม่อีกครั้งค่ะ",
};

/**
 * The join screen is the only place that can enforce "ask before continuing someone's lesson".
 * The server cannot tell an answered confirmation from an unanswered one - the learner key is
 * valid either way - so this screen never skips itself, no matter how recently this browser was
 * here. A shared school computer hands the same key to whoever sits down next.
 */
export default function JoinPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [state, setState] = useState<LearningResumeState | "loading" | null>("loading");
  /** null = still deciding (confirmation screen); "form" = the learner asked for a name form. */
  const [mode, setMode] = useState<"confirm" | "form">("form");
  const [name, setName] = useState("");
  const [joining, setJoining] = useState(false);
  const [joinError, setJoinError] = useState<string | null>(null);
  const media = useLocalMedia();

  useEffect(() => {
    let active = true;
    void api
      .getLearningResumeState(params.token, peekLearnerKey())
      .then((resume) => {
        if (!active) return;

        // Nothing to resume and nothing finished, on a link that is past its date: there is no
        // screen worth showing.
        if (!resume.resumable && !resume.lastEnded && resume.linkExpired) {
          router.replace("/link-expired");
          return;
        }
        setState(resume);
        setMode(resume.resumable ? "confirm" : "form");
        setName(resume.resumable?.recipientName ?? resume.lastEnded?.recipientName ?? getLearnerName(params.token) ?? "");
      })
      .catch(() => {
        if (active) router.replace("/link-expired");
      });
    return () => {
      active = false;
    };
  }, [params.token, router]);

  function enterRoom() {
    media.stopStream();
    grantRoomEntry(params.token);
    router.push(`/room/${params.token}`);
  }

  const trimmedName = name.trim();
  const canJoin = isValidLearnerName(name) && !joining;

  async function handleJoin() {
    if (!canJoin || state === "loading" || state === null) return;
    setJoining(true);
    setJoinError(null);
    try {
      // Creating the session here rather than inside the room means the room always opens onto a
      // session that already exists - no half-joined state to reason about if the request fails.
      // Restart, not join: someone who chose "เริ่มเรียนใหม่ในชื่ออื่น" on a browser that already
      // has a run must get their own row, and join would hand back the other person's.
      const input = { recipientName: trimmedName, learnerKey: getOrCreateLearnerKey() };
      if (state.resumable || state.lastEnded) {
        await api.restartLearningSession(params.token, input);
      } else {
        await api.joinLearningSession(params.token, input);
      }
      setLearnerName(params.token, trimmedName);
      enterRoom();
    } catch (err) {
      setJoining(false);
      setJoinError(err instanceof Error ? err.message : "เข้าห้องเรียนไม่สำเร็จ กรุณาลองใหม่อีกครั้งค่ะ");
    }
  }

  if (state === "loading") {
    return (
      <main className="flex min-h-screen items-center justify-center p-6">
        <LoadingBlock label="กำลังโหลดข้อมูลห้องเรียน..." />
      </main>
    );
  }
  if (!state) {
    return null;
  }

  const { link, lessonTitle, resumable, lastEnded, linkExpired } = state;

  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-lg">
        <CardContent className="flex flex-col gap-5">
          <div>
            <p className="text-xs text-muted-foreground">ห้องเรียนการใช้งานระบบ</p>
            <h1 className="text-lg font-semibold">{lessonTitle}</h1>
            {link.recipientOrgName && (
              <p className="mt-1 text-sm text-muted-foreground">{link.recipientOrgName}</p>
            )}
          </div>

          {mode === "confirm" && resumable ? (
            <div className="flex flex-col gap-4">
              <div className="rounded-lg border bg-muted p-4">
                <p className="text-sm font-medium">คุณคือ &ldquo;{resumable.recipientName}&rdquo; ใช่ไหมคะ</p>
                <p className="mt-1 text-sm text-muted-foreground">
                  มีการเรียนที่ยังไม่จบค้างอยู่{formatProgress(resumable)} — ถ้าใช่ กดเรียนต่อได้เลยค่ะ
                </p>
              </div>

              <Button className="w-full" onClick={enterRoom}>
                ใช่ เรียนต่อจากเดิม
              </Button>

              {/* A different person on the same computer. Their own row, under the same browser
                  key - the previous run is never touched, closed, or renamed. */}
              <Button
                variant="outline"
                className="w-full"
                disabled={linkExpired}
                onClick={() => {
                  // Cleared on purpose: this is a different person, and prefilling the previous
                  // learner's name is how two people end up sharing one row.
                  setName("");
                  setMode("form");
                }}
              >
                ไม่ใช่ เริ่มเรียนใหม่ในชื่ออื่น
              </Button>
              {linkExpired && (
                <p className="text-xs text-muted-foreground">
                  ลิงก์นี้หมดอายุแล้ว เริ่มการเรียนใหม่ไม่ได้ แต่เรียนรอบที่ค้างอยู่ต่อจนจบได้ค่ะ
                </p>
              )}
            </div>
          ) : lastEnded && mode === "form" && !resumable ? (
            <div className="flex flex-col gap-4">
              <div className="rounded-lg border bg-muted p-4">
                <p className="text-sm font-medium">คุณเรียนบทเรียนนี้จบแล้ว</p>
                <p className="mt-1 text-sm text-muted-foreground">เรียนซ้ำอีกรอบได้ รอบเดิมยังเก็บไว้ครบค่ะ</p>
              </div>

              <Button
                variant="outline"
                className="w-full"
                onClick={() => router.push(`/session-ended/${params.token}`)}
              >
                ดูสรุปการเรียนรอบที่แล้ว
              </Button>

              {linkExpired ? (
                <p className="text-xs text-muted-foreground">ลิงก์นี้หมดอายุแล้ว เริ่มเรียนรอบใหม่ไม่ได้ค่ะ</p>
              ) : (
                <NameForm
                  name={name}
                  onNameChange={setName}
                  onSubmit={handleJoin}
                  canJoin={canJoin}
                  joining={joining}
                  joinError={joinError}
                  submitLabel="เรียนอีกครั้ง"
                />
              )}
            </div>
          ) : (
            <>
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

              <NameForm
                name={name}
                onNameChange={setName}
                onSubmit={handleJoin}
                canJoin={canJoin}
                joining={joining}
                joinError={joinError}
                submitLabel="เข้าห้องเรียน"
              />
            </>
          )}
        </CardContent>
      </Card>
    </main>
  );
}

/** The only field on this screen. No email, no phone, no organization - CS already recorded the
 *  organization on the link (CORE_FEATURE_SPEC §5.1). */
function NameForm({
  name,
  onNameChange,
  onSubmit,
  canJoin,
  joining,
  joinError,
  submitLabel,
}: {
  name: string;
  onNameChange: (value: string) => void;
  onSubmit: () => Promise<void>;
  canJoin: boolean;
  joining: boolean;
  joinError: string | null;
  submitLabel: string;
}) {
  return (
    <>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="learner-name">ชื่อของคุณ</Label>
        <Input
          id="learner-name"
          type="text"
          value={name}
          maxLength={LEARNER_NAME_MAX_LENGTH}
          autoComplete="name"
          placeholder="เช่น ครูสมศรี"
          onChange={(e) => onNameChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") void onSubmit();
          }}
        />
        {name.trim().length > LEARNER_NAME_MAX_LENGTH && (
          <p className="text-xs text-destructive">กรุณากรอกชื่อ (ไม่เกิน 80 ตัวอักษร)</p>
        )}
      </div>

      {joinError && <p className="text-sm text-destructive">{joinError}</p>}

      <Button className="w-full" disabled={!canJoin} onClick={() => void onSubmit()}>
        {joining ? (
          <>
            <Spinner data-icon="inline-start" />
            กำลังเข้าห้องเรียน...
          </>
        ) : (
          submitLabel
        )}
      </Button>
    </>
  );
}

/** " (สไลด์ 7/20)" when we know both numbers - it is what tells the learner this really is their
 *  own unfinished lesson rather than someone else's. */
function formatProgress(session: { lastSlideIndex: number | null; totalSlideCount: number | null }): string {
  if (session.lastSlideIndex === null) return "";
  const current = session.lastSlideIndex + 1;
  return session.totalSlideCount ? ` (สไลด์ ${current}/${session.totalSlideCount})` : ` (สไลด์ที่ ${current})`;
}
