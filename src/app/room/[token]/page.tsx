"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { sessionRepository } from "@/providers/data";
import { isSessionJoinable } from "@/utils/session-status";
import { tutorConfig, providerConfig } from "@/config/tutor-config";
import { useTutorSession } from "@/hooks/use-tutor-session";
import { useLocalMedia } from "@/hooks/use-local-media";
import { getMediaById } from "@/mocks/media.mock";
import { AiTile } from "@/components/meeting/AiTile";
import { TeacherTile } from "@/components/meeting/TeacherTile";
import { SharedMedia } from "@/components/meeting/SharedMedia";
import { ControlBar } from "@/components/meeting/ControlBar";
import { ChatDrawer } from "@/components/meeting/ChatDrawer";
import { DemoControlsDrawer } from "@/components/meeting/DemoControlsDrawer";
import type { TrainingSession } from "@/types/domain";

type LoadState = "loading" | "ready" | "blocked";

export default function RoomPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [session, setSession] = useState<TrainingSession | null>(null);
  const [loadState, setLoadState] = useState<LoadState>("loading");

  useEffect(() => {
    let active = true;
    void (async () => {
      const found = await sessionRepository.getByToken(params.token);
      if (!active) return;
      if (!found || found.endedAt || (!found.startedAt && !isSessionJoinable(found))) {
        router.replace(found?.endedAt ? "/session-ended" : "/link-expired");
        return;
      }
      if (found.disconnectedAt) {
        const elapsed = Date.now() - new Date(found.disconnectedAt).getTime();
        if (elapsed > tutorConfig.reconnectGraceMs) {
          router.replace("/session-ended");
          return;
        }
        const reconnected = { ...found, disconnectedAt: undefined };
        await sessionRepository.update(reconnected);
        setSession(reconnected);
        setLoadState("ready");
        return;
      }
      setSession(found);
      setLoadState("ready");
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

function RoomContent({ session }: { session: TrainingSession }) {
  const router = useRouter();
  const { runtime, sendAction, submitChatMessage } = useTutorSession(session);
  const media = useLocalMedia();
  const [chatOpen, setChatOpen] = useState(false);
  const [demoControlsOpen, setDemoControlsOpen] = useState(false);
  const [teacherSpeaking, setTeacherSpeaking] = useState(false);

  useEffect(() => {
    if (runtime.state === "ENDED") {
      router.replace("/session-ended");
    }
  }, [runtime.state, router]);

  const handleAskQuestion = (text: string) => {
    setTeacherSpeaking(true);
    submitChatMessage(text);
    window.setTimeout(() => setTeacherSpeaking(false), 1200);
  };

  const activeMedia = runtime.activeMediaId ? getMediaById(runtime.activeMediaId) : undefined;
  const isThinking = runtime.state === "ANSWERING" && !runtime.isAiSpeaking;

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-room-bg">
      <header className="flex shrink-0 items-center justify-between border-b border-room-border bg-room-panel px-4 py-3">
        <p className="text-sm font-semibold text-room-text">School Bright Support</p>
        <p className="text-xs text-room-muted">
          {runtime.connectionStatus === "connected" ? "เชื่อมต่ออยู่" : "การเชื่อมต่อขาดหาย กำลังพยายามเชื่อมต่อใหม่..."}
          {runtime.state === "PAUSED" && " · พักการสอนชั่วคราว"}
        </p>
      </header>

      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4 md:flex-row">
        <div className="min-h-0 flex-1">
          <SharedMedia media={activeMedia} />
        </div>
        <div className="flex shrink-0 gap-4 md:w-72 md:flex-col">
          <div className="flex-1 md:flex-none">
            <AiTile speaking={runtime.isAiSpeaking} thinking={isThinking} />
          </div>
          <div className="flex-1 md:flex-none">
            <TeacherTile
              stream={media.stream}
              cameraOn={media.cameraOn}
              micOn={runtime.isMicEnabled}
              speaking={teacherSpeaking}
              teacherName={session.teacherName}
            />
          </div>
        </div>
      </div>

      <ControlBar
        micOn={runtime.isMicEnabled}
        cameraOn={runtime.isCameraEnabled && media.cameraOn}
        onToggleMic={() => {
          sendAction({ type: "TOGGLE_MIC" });
          media.toggleMic();
        }}
        onToggleCamera={() => {
          sendAction({ type: "TOGGLE_CAMERA" });
          void media.toggleCamera();
        }}
        onToggleChat={() => setChatOpen((prev) => !prev)}
        onLeave={() => sendAction({ type: "LEAVE" })}
      />

      <ChatDrawer
        open={chatOpen}
        onClose={() => setChatOpen(false)}
        questions={runtime.questions}
        onSubmit={handleAskQuestion}
      />

      {providerConfig.enableDemoControls && (
        <DemoControlsDrawer
          open={demoControlsOpen}
          onToggle={() => setDemoControlsOpen((prev) => !prev)}
          sendAction={sendAction}
          submitChatMessage={handleAskQuestion}
        />
      )}
    </div>
  );
}
