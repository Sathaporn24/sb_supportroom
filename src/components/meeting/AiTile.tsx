import { SparkleIcon } from "@/components/ui/icons";

type Props = {
  speaking: boolean;
  thinking: boolean;
};

export function AiTile({ speaking, thinking }: Props) {
  return (
    <div
      className={`flex items-center gap-3 rounded-xl border bg-room-panel p-4 transition-shadow ${
        speaking ? "border-room-accent shadow-speaking" : "border-room-border"
      }`}
    >
      <div
        className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-room-accentSoft text-room-accent ${
          speaking ? "animate-pulse-soft" : ""
        }`}
      >
        <SparkleIcon />
      </div>
      <div className="min-w-0">
        <p className="truncate text-sm font-semibold text-room-text">School Bright Support</p>
        <p className="text-xs text-room-muted">
          {thinking ? "กำลังประมวลผล..." : speaking ? "กำลังพูด..." : "รอฟังอยู่"}
        </p>
      </div>
    </div>
  );
}
