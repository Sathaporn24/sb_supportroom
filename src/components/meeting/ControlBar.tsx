import { IconButton } from "@/components/ui/IconButton";
import { CameraIcon, CameraOffIcon, ChatIcon, LeaveIcon, MicIcon, MicOffIcon } from "@/components/ui/icons";
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
    <div className="flex shrink-0 flex-wrap items-center justify-center gap-3 border-t border-room-border bg-room-panel px-4 py-3">
      <IconButton
        label={micOn ? "ปิดไมค์" : "เปิดไมค์"}
        active={micOn}
        icon={micOn ? <MicIcon /> : <MicOffIcon />}
        onClick={onToggleMic}
      />
      <IconButton
        label={cameraOn ? "ปิดกล้อง" : "เปิดกล้อง"}
        active={cameraOn}
        icon={cameraOn ? <CameraIcon /> : <CameraOffIcon />}
        onClick={onToggleCamera}
      />
      <PushToTalkButton status={pushToTalkStatus} onStart={onPushToTalkStart} onEnd={onPushToTalkEnd} />
      <VolumeControl volume={aiVolume} onChange={onChangeAiVolume} />
      <IconButton label="แชต" icon={<ChatIcon />} onClick={onToggleChat} />
      <IconButton label="ออกจากห้อง" danger icon={<LeaveIcon />} onClick={onLeave} />
    </div>
  );
}
