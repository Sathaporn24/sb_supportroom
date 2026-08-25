"use client";

import { useEffect, useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { XIcon } from "lucide-react";
import * as api from "@/lib/api-client";
import { consumeRoomEntry, getLearnerName, peekLearnerKey } from "@/utils/learner-key";
import { useTutorSession } from "@/hooks/use-tutor-session";
import { useLocalMedia } from "@/hooks/use-local-media";
import { AiTile } from "@/components/meeting/AiTile";
import { ParticipantTile } from "@/components/meeting/ParticipantTile";
import { SlidesEmbed } from "@/components/meeting/SlidesEmbed";
import { ControlBar } from "@/components/meeting/ControlBar";
import { AskAiDrawer } from "@/components/meeting/AskAiDrawer";
import { Button } from "@/components/ui/button";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import type { PushToTalkStatus } from "@/components/meeting/PushToTalkButton";
import type { LearningSession, PublicTrainingLink } from "@/types/domain";
import type { TutorState } from "@/tutor/types";

type LoadState = "loading" | "ready";

type RoomData = { link: PublicTrainingLink; learningSession: LearningSession; learnerKey: string };

export default function RoomPage() {
  const params = useParams<{ token: string }>();
  const router = useRouter();
  const [data, setData] = useState<RoomData | null>(null);
  const [loadState, setLoadState] = useState<LoadState>("loading");

  // Caches the one-shot grant's result across React Strict Mode's dev-only double effect
  // invocation (mount -> cleanup -> mount again, to catch non-idempotent effects). Without this,
  // the second invocation finds the sessionStorage flag already consumed by the first and bounces
  // straight back to /join - not a race in production, but the effect itself was not idempotent,
  // which Strict Mode exists to catch. null = not checked yet, so a real second mount (navigating
  // away and back) still re-consumes correctly.
  const entryGrantedRef = useRef<boolean | null>(null);

  useEffect(() => {
    let active = true;
    void (async () => {
      // Missing either one means this browser never completed the join screen, so there is no
      // learner to attribute anything to. The room cannot invent one - send them to type a name.
      const learnerKey = peekLearnerKey();
      const storedName = getLearnerName(params.token);
      if (!learnerKey || !storedName) {
        router.replace(`/join/${params.token}`);
        return;
      }

      // Opening /room directly - a bookmark, a reload, a pasted URL - must not walk into whatever
      // run this browser's key points at. That is the silent resume the join screen exists to
      // prevent, and the room has no way to ask the question itself. The grant is one-shot, so
      // every fresh arrival goes back through the confirmation.
      if (entryGrantedRef.current === null) {
        entryGrantedRef.current = consumeRoomEntry(params.token);
      }
      if (!entryGrantedRef.current) {
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
      <main className="flex min-h-[100dvh] items-center justify-center p-6">
        <LoadingBlock label="กำลังโหลดห้องเรียน..." />
      </main>
    );
  }

  return <RoomContent {...data} />;
}

// TQ-24 (U1) - "ready" removed. This is a SECOND list, separate from the reducer's
// PUSH_TO_TALK_STATES (tutor/tutor-reducer.ts) - the UI decides whether the button even looks
// pressable from this one, and the reducer decides what pressing it actually does from its own.
// Both lists dropped "ready" together deliberately: removing it from only one leaves a button
// that either looks enabled but does nothing (UI list still has it) or looks disabled while the
// reducer would have accepted it anyway (reducer list still has it) - see design.md TQ-24.
const PUSH_TO_TALK_ENABLED_STATES = ["slide-speaking", "waiting-slide-duration", "final-question-window"];

/** TQ-20 - matrix for the typed-question input/send button, keyed by runtime.state. The drawer
 * component itself never sees runtime.state (RS-7/CX-6) - this is the one place that translates
 * the tutor state machine into "can type" / "can send" / "why not". */
function textQuestionAvailability(state: TutorState): {
  inputEnabled: boolean;
  sendEnabled: boolean;
  disabledHint?: string;
} {
  switch (state) {
    case "slide-speaking":
    case "waiting-slide-duration":
    case "final-question-window":
      return { inputEnabled: true, sendEnabled: true };
    case "processing-question":
    case "answer-speaking":
      // Draft can still be typed while waiting for the previous answer, but sending would
      // stack a second question - same "no queueing" rule as push-to-talk.
      return { inputEnabled: true, sendEnabled: false };
    case "ready":
      return {
        inputEnabled: false,
        sendEnabled: false,
        disabledHint: "เลือกพร้อม/ยังไม่พร้อมด้านบนก่อนนะคะ",
      };
    default:
      return { inputEnabled: false, sendEnabled: false };
  }
}

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
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [slideFullscreen, setSlideFullscreen] = useState(false);

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

  // RS-6 - chosen behavior: auto-close fullscreen once a question starts being answered, rather
  // than keeping End/Push-to-Talk reachable through some overlaid z-index arrangement. This
  // guarantees there is never a state where the recipient is stuck looking at the slide with no
  // way to press "end" - the overlay only exists while the lesson is passively narrating.
  useEffect(() => {
    if (slideFullscreen && (isProcessing || isAnswering)) setSlideFullscreen(false);
  }, [slideFullscreen, isProcessing, isAnswering]);

  const pushToTalkStatus: PushToTalkStatus = (() => {
    if (runtime.state === "push-to-talk-recording") return "recording";
    if (isProcessing) return "processing";
    if (isAnswering) return "answering";
    if (PUSH_TO_TALK_ENABLED_STATES.includes(runtime.state)) return "idle";
    return "disabled";
  })();

  const { inputEnabled, sendEnabled, disabledHint } = textQuestionAvailability(runtime.state);

  if (runtime.state === "error") {
    return (
      <main className="flex min-h-[100dvh] flex-col items-center justify-center gap-3 p-6 text-center">
        <p>เกิดข้อผิดพลาดระหว่างเตรียมห้องสอน</p>
        <p className="text-sm text-muted-foreground">{runtime.errorMessage || loadError}</p>
      </main>
    );
  }

  return (
    <div className="relative flex h-[100dvh] flex-col overflow-hidden bg-background">
      <header className="flex shrink-0 items-center justify-between border-b bg-card px-4 py-3">
        <p className="text-sm font-semibold">School Bright Support</p>
        <p className="text-xs text-muted-foreground">
          เชื่อมต่ออยู่
          {runtime.state === "paused" && " · พักการสอนชั่วคราว"}
        </p>
      </header>

      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4 lg:flex-row">
        <div className="relative min-h-0 flex-1">
          <SlidesEmbed
            embedUrl={embedUrl}
            currentSlide={currentSlide}
            totalSlides={totalSlides}
            loading={runtime.state === "idle" || runtime.state === "preparing"}
            isReference={isShowingReferencedSlide}
            resumeSlideNumber={resumeSlideNumber}
            fullscreen={slideFullscreen}
            onToggleFullscreen={() => setSlideFullscreen((v) => !v)}
          />
          {runtime.state === "ready" && (
            <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 rounded-xl bg-black/40 p-4">
              <Button
                className="h-11"
                data-testid="room-ready-start-button"
                onClick={() => sendEvent({ type: "START" })}
              >
                พร้อมแล้ว เริ่มเรียนเลย
              </Button>
              <Button
                className="h-11"
                variant="outline"
                data-testid="room-ready-not-ready-button"
                onClick={() => sendEvent({ type: "NOT_READY" })}
              >
                ยังไม่พร้อม
              </Button>
            </div>
          )}
        </div>
        <div className="flex shrink-0 gap-2 lg:w-72 lg:flex-col lg:gap-4">
          <div className="w-28 flex-none lg:w-auto lg:flex-1">
            <AiTile speaking={runtime.isAiSpeaking} thinking={isProcessing} loading={isAiPreparing} />
          </div>
          <div className="w-28 flex-none lg:w-auto lg:flex-1">
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
        <div className="flex shrink-0 items-center justify-center gap-3 border-t border-primary/30 bg-primary/10 px-4 py-2 text-center text-sm">
          <p>{runtime.micNotice}</p>
          <Button
            variant="ghost"
            size="icon-xs"
            data-testid="room-mic-notice-dismiss-button"
            onClick={() => sendEvent({ type: "CLEAR_MIC_NOTICE" })}
            aria-label="ปิดข้อความแจ้งเตือน"
          >
            <XIcon />
          </Button>
        </div>
      )}

      <AskAiDrawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        questions={runtime.questions}
        onSubmitQuestion={(text) => sendEvent({ type: "SUBMIT_TEXT_QUESTION", text })}
        inputEnabled={inputEnabled}
        sendEnabled={sendEnabled}
        disabledHint={disabledHint}
        failedQuestionText={runtime.failedQuestionText}
        onFailedQuestionTextConsumed={() => sendEvent({ type: "CLEAR_FAILED_QUESTION_TEXT" })}
      />

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
        onToggleAskAi={() => setDrawerOpen((v) => !v)}
        onLeave={() => sendEvent({ type: "END_SESSION" })}
        onPushToTalkStart={() => sendEvent({ type: "PUSH_TO_TALK_START" })}
        onPushToTalkEnd={() => sendEvent({ type: "PUSH_TO_TALK_END" })}
      />
    </div>
  );
}
