import { SparkleIcon } from "@/components/ui/icons";
import { Spinner } from "@/components/ui/Spinner";

type Props = {
  speaking: boolean;
  thinking: boolean;
  loading: boolean;
};

export function AiTile({ speaking, thinking, loading }: Props) {
  return (
    <div
      className={`relative aspect-video w-full overflow-hidden rounded-xl border bg-room-panel transition-shadow ${
        speaking ? "border-room-accent shadow-speaking" : "border-room-border"
      }`}
    >
      <div className="flex h-full w-full items-center justify-center bg-room-panelAlt">
        <div
          className={`flex h-16 w-16 items-center justify-center rounded-full bg-room-accentSoft text-room-accent ${
            speaking ? "animate-pulse-soft" : ""
          }`}
        >
          {loading ? <Spinner className="h-6 w-6" /> : <SparkleIcon className="h-8 w-8" />}
        </div>
      </div>

      <div className="absolute inset-x-0 bottom-0 flex items-center justify-between gap-2 px-3 py-2">
        <p className="truncate text-sm font-medium text-room-text">School Bright Support</p>
        <span className="shrink-0 text-xs text-room-muted">
          {thinking
            ? "กำลังประมวลผล..."
            : loading
              ? "กำลังเตรียมเนื้อหา..."
              : speaking
                ? "กำลังพูด..."
                : "รอฟังอยู่"}
        </span>
      </div>
    </div>
  );
}
