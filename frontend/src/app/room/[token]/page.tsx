"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import * as api from "@/lib/api-client";
import { getLearnerName, peekLearnerKey } from "@/utils/learner-key";
import { useTutorSession } from "@/hooks/use-tutor-session";
import { useLocalMedia } from "@/hooks/use-local-media";
import { useSessionChat } from "@/hooks/use-session-chat";
import { AiTile } from "@/components/meeting/AiTile";
import { ParticipantTile } from "@/components/meeting/ParticipantTile";
import { SlidesEmbed } from "@/components/meeting/SlidesEmbed";
import { ControlBar } from "@/components/meeting/ControlBar";
import { ChatDrawer } from "@/components/meeting/ChatDrawer";
import { Button } from "@/components/ui/Button";
import { LoadingBlock } from "@/components/ui/LoadingBlock";
import type { PushToTalkStatus } from "@/components/meeting/PushToTalkButton";
import type { LearningSession, PublicTrainingLink } from "@/types/domain";

type LoadState = "loading" | "ready";

type RoomData = { link: PublicTrainingLink; learningSession: LearningSession; learnerKey: string };

export default function RoomPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [data, setData] = useState<RoomData | null>(null);
  const [loadState, setLoadState] = useState<LoadState>("loading");

  useEffect(() => {
    let active = true;
    void (async () => {
      // Missing either one means this browser never completed the join screen, so there is no
      // learner to attribute anything to. The room cannot invent one - send them to type a name.
      const learnerKey = peekLearnerKey(params.token);
      const storedName = getLearnerName(params.token);
      if (!learnerKey || !storedName) {
        router.replace(`/join/${params.token}`);
        return;
      }

      let link: PublicTrainingLink;
      try {
        link = (await api.getTrainingLinkByToken(params.token)).link;
      } catch {
        if (active) router.replace("/link-expired");
        return;
      }
      if (!active) return;
      try {
        // Idempotent: this browser already has a session on this link, so join hands the same one
        // back rather than starting a second. That is what makes a reconnect or a reopened tab
        // free - and what carries lastSlideIndex so the lesson picks up where it stopped. The
        // stored name only matters in the one case where the row is gone (an admin reset), where
        // it saves the learner from retyping it.
        const { learningSession } = await api.joinLearningSession(params.token, {
          recipientName: storedName,
          learnerKey,
        });
        if (!active) return;
        if (learningSession.status === "ENDED") {
          router.replace(`/session-ended/${params.token}`);
          return;
        }
        setData({ link, learningSession, learnerKey });
        setLoadState("ready");
      } catch {
        // The link loaded fine, so this is about the session, not the link. The join screen can
        // distinguish an active first join from an expired link and is the honest place to land.
        if (active) router.replace(`/join/${params.token}`);
      }
    })();
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.token]);

  if (loadState !== "ready" || !data) {
    return (
      <main className="flex min-h-screen items-center justify-center p-6">
        <LoadingBlock label="กำลังโหลดห้องเรียน..." />
      </main>
    );
  }

  return <RoomContent {...data} />;
}

// "ready" included so the teacher can answer the start prompt by voice, not just by click.
const PUSH_TO_TALK_ENABLED_STATES = [
  "ready",
  "slide-speaking",
  "waiting-slide-duration",
  "final-question-window",
];

function RoomContent({ link, learningSession, learnerKey }: RoomData) {
  const router = useRouter();
  const {
    runtime,
    embedUrl,
    loadError,
    currentSlide,
    isShowingReferencedSlide,
    resumeSlideNumber,
    totalSlides,
    sendEvent,
    aiVolume,
    setAiVolume,
  } = useTutorSession(link, learningSession, learnerKey);
  const media = useLocalMedia();
  const [chatOpen, setChatOpen] = useState(false);
  const chat = useSessionChat(link.token, learnerKey);

  useEffect(() => {
    if (runtime.state === "completed") {
      router.replace(`/session-ended/${link.token}`);
    }
  }, [runtime.state, router, link.token]);

  // Auto-dismiss the mic notice after a few seconds so it doesn't linger indefinitely if the
  // teacher doesn't press push-to-talk again (which would also clear it, per the reducer).
  useEffect(() => {
    if (!runtime.micNotice) return;
    const timer = setTimeout(() => sendEvent({ type: "CLEAR_MIC_NOTICE" }), 6000);
    return () => clearTimeout(timer);
  }, [runtime.micNotice, sendEvent]);

  const isProcessing = runtime.state === "processing-question";
  const isAnswering = runtime.state === "answer-speaking";
  const isAiPreparing = ["preparing", "slide-loading", "restarting-slide"].includes(runtime.state);

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
          <SlidesEmbed
            embedUrl={embedUrl}
            currentSlide={currentSlide}
            totalSlides={totalSlides}
            loading={runtime.state === "idle" || runtime.state === "preparing"}
            isReference={isShowingReferencedSlide}
            resumeSlideNumber={resumeSlideNumber}
          />
          {runtime.state === "ready" && (
            <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 rounded-xl bg-black/40">
              <Button onClick={() => sendEvent({ type: "START" })}>พร้อมแล้ว เริ่มเรียนเลย</Button>
              <p className="text-xs text-white/80">หรือกดปุ่ม &ldquo;กดค้างเพื่อพูด&rdquo; แล้วบอกว่าพร้อมแล้วก็ได้ค่ะ</p>
            </div>
          )}
        </div>
        <div className="flex shrink-0 gap-4 md:w-72 md:flex-col">
          <div className="flex-1 md:flex-none">
            <AiTile speaking={runtime.isAiSpeaking} thinking={isProcessing} loading={isAiPreparing} />
          </div>
          <div className="flex-1 md:flex-none">
            <ParticipantTile
              stream={media.stream}
              cameraOn={media.cameraOn}
              micOn={runtime.isMicEnabled}
              speaking={runtime.state === "push-to-talk-recording"}
              recipientName={learningSession.recipientName}
            />
          </div>
        </div>
      </div>

      {runtime.micNotice && (
        <div className="flex shrink-0 items-center justify-center gap-3 border-t border-amber-500/30 bg-amber-500/10 px-4 py-2 text-center text-sm text-amber-700">
          <p>{runtime.micNotice}</p>
          <button
            type="button"
            onClick={() => sendEvent({ type: "CLEAR_MIC_NOTICE" })}
            className="text-amber-700/70 hover:text-amber-700"
            aria-label="ปิดข้อความแจ้งเตือน"
          >
            ✕
          </button>
        </div>
      )}

      <ControlBar
        micOn={runtime.isMicEnabled}
        cameraOn={runtime.isCameraEnabled && media.cameraOn}
        pushToTalkStatus={pushToTalkStatus}
        aiVolume={aiVolume}
        onChangeAiVolume={setAiVolume}
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

      <ChatDrawer
        open={chatOpen}
        onClose={() => setChatOpen(false)}
        questions={runtime.questions}
        chatMessages={chat.chatMessages}
        onSendMessage={chat.sendChatMessage}
      />
    </div>
  );
}
