import { MessageSquareIcon, PhoneOffIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PushToTalkButton, type PushToTalkStatus } from "@/components/meeting/PushToTalkButton";
import { VolumeControl } from "@/components/meeting/VolumeControl";

type Props = {
  // micOn/cameraOn/onToggleMic/onToggleCamera still come from the parent's tutor state and
  // use-local-media hook - kept in the type so callers don't need to change, but intentionally not
  // rendered below. Push-to-Talk is the only way a teacher speaks; the video-call-style mic/camera
  // toggles were decorative (isMicEnabled never gates anything) and the camera preview is unused.
  micOn: boolean;
  cameraOn: boolean;
  pushToTalkStatus: PushToTalkStatus;
  aiVolume: number;
  onChangeAiVolume: (volume: number) => void;
  onToggleMic: () => void;
  onToggleCamera: () => void;
  onToggleAskAi: () => void;
  onLeave: () => void;
  onPushToTalkStart: () => void;
  onPushToTalkEnd: () => void;
};

// RS-9 - priority order when space runs out on compact: talk > leave > ask-AI drawer > volume.
// Flex-wrap alone would let any button drop to a second row/get squeezed first; render order
// here doubles as visual priority (earlier = claims space first) and volume, the lowest
// priority, is the one placed last so it is the first to wrap onto its own row if it must.
export function ControlBar({
  pushToTalkStatus,
  aiVolume,
  onChangeAiVolume,
  onToggleAskAi,
  onLeave,
  onPushToTalkStart,
  onPushToTalkEnd,
}: Props) {
  return (
    <div className="flex shrink-0 flex-wrap items-center justify-center gap-3 border-t bg-card px-4 py-3 pb-[calc(0.75rem+env(safe-area-inset-bottom))]">
      <PushToTalkButton status={pushToTalkStatus} onStart={onPushToTalkStart} onEnd={onPushToTalkEnd} />
      <Button
        variant="destructive"
        size="icon-lg"
        className="size-11 rounded-full"
        title="ออกจากห้อง"
        aria-label="ออกจากห้อง"
        data-testid="room-leave-button"
        onClick={onLeave}
      >
        <PhoneOffIcon />
      </Button>
      <Button
        variant="outline"
        size="icon-lg"
        className="size-11 rounded-full"
        title="ถาม-ตอบกับ AI"
        aria-label="ถาม-ตอบกับ AI"
        data-testid="room-ask-ai-toggle-button"
        onClick={onToggleAskAi}
      >
        <MessageSquareIcon />
      </Button>
      <VolumeControl volume={aiVolume} onChange={onChangeAiVolume} />
    </div>
  );
}
