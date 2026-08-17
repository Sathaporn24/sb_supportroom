import { SparklesIcon } from "lucide-react";
import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/utils";

type Props = {
  speaking: boolean;
  thinking: boolean;
  loading: boolean;
};

export function AiTile({ speaking, thinking, loading }: Props) {
  return (
    <div
      className={cn(
        "relative aspect-video w-full overflow-hidden rounded-xl border bg-card transition-shadow",
        speaking ? "border-primary ring-3 ring-primary/60" : "border-border",
      )}
    >
      <div className="flex h-full w-full items-center justify-center bg-muted">
        <div
          className={cn(
            "flex size-16 items-center justify-center rounded-full bg-primary/10 text-primary",
            speaking && "animate-pulse",
          )}
        >
          {loading ? <Spinner className="size-6" /> : <SparklesIcon className="size-8" />}
        </div>
      </div>

      <div className="absolute inset-x-0 bottom-0 flex items-center justify-between gap-2 px-3 py-2">
        <p className="truncate text-sm font-medium text-foreground">School Bright Support</p>
        <span className="shrink-0 text-xs text-muted-foreground">
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
