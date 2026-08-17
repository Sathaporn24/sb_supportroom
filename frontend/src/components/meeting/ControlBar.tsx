import { MessageSquareIcon, MicIcon, MicOffIcon, PhoneOffIcon, VideoIcon, VideoOffIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PushToTalkButton, type PushToTalkStatus } from "@/components/meeting/PushToTalkButton";
import { VolumeControl } from "@/components/meeting/VolumeControl";

type Props = {
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
  micOn,
  cameraOn,
  pushToTalkStatus,
  aiVolume,
  onChangeAiVolume,
  onToggleMic,
  onToggleCamera,
  onToggleChat,
  onLeave,
  onPushToTalkStart,
  onPushToTalkEnd,
}: Props) {
  return (
    <div className="flex shrink-0 flex-wrap items-center justify-center gap-3 border-t bg-card px-4 py-3">
      <Button
        variant={micOn ? "outline" : "destructive"}
        size="icon-lg"
        className="rounded-full"
        title={micOn ? "ปิดไมค์" : "เปิดไมค์"}
        aria-label={micOn ? "ปิดไมค์" : "เปิดไมค์"}
        onClick={onToggleMic}
      >
        {micOn ? <MicIcon /> : <MicOffIcon />}
      </Button>
      <Button
        variant={cameraOn ? "outline" : "destructive"}
        size="icon-lg"
        className="rounded-full"
        title={cameraOn ? "ปิดกล้อง" : "เปิดกล้อง"}
        aria-label={cameraOn ? "ปิดกล้อง" : "เปิดกล้อง"}
        onClick={onToggleCamera}
      >
        {cameraOn ? <VideoIcon /> : <VideoOffIcon />}
      </Button>
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
