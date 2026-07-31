"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { isSessionJoinable } from "@/utils/session-status";
import { useTutorSession } from "@/hooks/use-tutor-session";
import { useLocalMedia } from "@/hooks/use-local-media";
import { AiTile } from "@/components/meeting/AiTile";
import { TeacherTile } from "@/components/meeting/TeacherTile";
import { SlidesEmbed } from "@/components/meeting/SlidesEmbed";
import { ControlBar } from "@/components/meeting/ControlBar";
import { ChatDrawer } from "@/components/meeting/ChatDrawer";
import { Button } from "@/components/ui/Button";
import type { PushToTalkStatus } from "@/components/meeting/PushToTalkButton";
import type { TrainingSession } from "@/types/domain";

type LoadState = "loading" | "ready";

export default function RoomPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [session, setSession] = useState<TrainingSession | null>(null);
  const [loadState, setLoadState] = useState<LoadState>("loading");

  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const { session: found } = await api.getSessionByToken(params.token);
        if (!active) return;
        if (found.status === "ENDED") {
          router.replace("/session-ended");
          return;
        }
        if (!isSessionJoinable(found)) {
          router.replace("/link-expired");
          return;
        }
        setSession(found);
        setLoadState("ready");
      } catch {
        if (active) router.replace("/link-expired");
      }
    })();
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.token]);

  if (loadState !== "ready" || !session) {
    return (
      <main className="flex min-h-screen items-center justify-center p-6 text-room-muted">กำลังโหลดห้องสอน...</main>
    );
  }

  return <RoomContent session={session} />;
}

const PUSH_TO_TALK_ENABLED_STATES = ["slide-speaking", "waiting-slide-duration", "final-question-window"];

function RoomContent({ session }: { session: TrainingSession }) {
  const router = useRouter();
  const { runtime, embedUrl, loadError, currentSlide, totalSlides, sendEvent } = useTutorSession(session);
  const media = useLocalMedia();
  const [chatOpen, setChatOpen] = useState(false);

  useEffect(() => {
    if (runtime.state === "completed") {
      router.replace("/session-ended");
    }
  }, [runtime.state, router]);

  const isProcessing = runtime.state === "processing-question";
  const isAnswering = runtime.state === "answer-speaking";

  const pushToTalkStatus: PushToTalkStatus = (() => {
    if (runtime.state === "push-to-talk-recording") return "recording";
    if (isProcessing) return "processing";
    if (isAnswering) return "answering";
    if (PUSH_TO_TALK_ENABLED_STATES.includes(runtime.state)) return "idle";
    return "disabled";
  })();

  if (runtime.state === "error") {
    return (
      <main className="flex min-h-screen flex-col items-center justify-center gap-3 p-6 text-center">
        <p className="text-room-text">เกิดข้อผิดพลาดระหว่างเตรียมห้องสอน</p>
        <p className="text-sm text-room-muted">{runtime.errorMessage || loadError}</p>
      </main>
    );
  }

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-room-bg">
      <header className="flex shrink-0 items-center justify-between border-b border-room-border bg-room-panel px-4 py-3">
        <p className="text-sm font-semibold text-room-text">School Bright Support</p>
        <p className="text-xs text-room-muted">
          เชื่อมต่ออยู่
          {runtime.state === "paused" && " · พักการสอนชั่วคราว"}
        </p>
      </header>

      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4 md:flex-row">
        <div className="relative min-h-0 flex-1">
          <SlidesEmbed embedUrl={embedUrl} currentSlide={currentSlide} totalSlides={totalSlides} />
          {runtime.state === "ready" && (
            <div className="absolute inset-0 flex items-center justify-center rounded-xl bg-black/40">
              <Button onClick={() => sendEvent({ type: "START" })}>พร้อมแล้ว เริ่มเรียนเลย</Button>
            </div>
          )}
        </div>
        <div className="flex shrink-0 gap-4 md:w-72 md:flex-col">
          <div className="flex-1 md:flex-none">
            <AiTile speaking={runtime.isAiSpeaking} thinking={isProcessing} />
          </div>
          <div className="flex-1 md:flex-none">
            <TeacherTile
              stream={media.stream}
              cameraOn={media.cameraOn}
              micOn={runtime.isMicEnabled}
              speaking={runtime.state === "push-to-talk-recording"}
              teacherName={session.teacherName}
            />
          </div>
        </div>
      </div>

      <ControlBar
        micOn={runtime.isMicEnabled}
        cameraOn={runtime.isCameraEnabled && media.cameraOn}
        pushToTalkStatus={pushToTalkStatus}
        onToggleMic={() => sendEvent({ type: "TOGGLE_MIC" })}
        onToggleCamera={() => {
          sendEvent({ type: "TOGGLE_CAMERA" });
          void media.toggleCamera();
        }}
        onToggleChat={() => setChatOpen((prev) => !prev)}
        onLeave={() => sendEvent({ type: "END_SESSION" })}
        onPushToTalkStart={() => sendEvent({ type: "PUSH_TO_TALK_START" })}
        onPushToTalkEnd={() => sendEvent({ type: "PUSH_TO_TALK_END" })}
      />

      <ChatDrawer open={chatOpen} onClose={() => setChatOpen(false)} questions={runtime.questions} />
    </div>
  );
}
