import { IconButton } from "@/components/ui/IconButton";
import { CameraIcon, CameraOffIcon, ChatIcon, LeaveIcon, MicIcon, MicOffIcon } from "@/components/ui/icons";

type Props = {
  micOn: boolean;
  cameraOn: boolean;
  onToggleMic: () => void;
  onToggleCamera: () => void;
  onToggleChat: () => void;
  onLeave: () => void;
};

export function ControlBar({ micOn, cameraOn, onToggleMic, onToggleCamera, onToggleChat, onLeave }: Props) {
  return (
    <div className="flex items-center justify-center gap-3 border-t border-room-border bg-room-panel px-4 py-3">
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
      <IconButton label="แชต" icon={<ChatIcon />} onClick={onToggleChat} />
      <IconButton label="ออกจากห้อง" danger icon={<LeaveIcon />} onClick={onLeave} />
    </div>
  );
}
