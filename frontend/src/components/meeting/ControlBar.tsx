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
  onToggleChat: () => void;
  onLeave: () => void;
  onPushToTalkStart: () => void;
  onPushToTalkEnd: () => void;
};

export function ControlBar({
  pushToTalkStatus,
  aiVolume,
  onChangeAiVolume,
  onToggleChat,
  onLeave,
  onPushToTalkStart,
  onPushToTalkEnd,
}: Props) {
  return (
    <div className="flex shrink-0 flex-wrap items-center justify-center gap-3 border-t bg-card px-4 py-3">
      <PushToTalkButton status={pushToTalkStatus} onStart={onPushToTalkStart} onEnd={onPushToTalkEnd} />
      <VolumeControl volume={aiVolume} onChange={onChangeAiVolume} />
      <Button
        variant="outline"
        size="icon-lg"
        className="rounded-full"
        title="แชต"
        aria-label="แชต"
        onClick={onToggleChat}
      >
        <MessageSquareIcon />
      </Button>
      <Button
        variant="destructive"
        size="icon-lg"
        className="rounded-full"
        title="ออกจากห้อง"
        aria-label="ออกจากห้อง"
        onClick={onLeave}
      >
        <PhoneOffIcon />
      </Button>
    </div>
  );
}
