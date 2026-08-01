import type { TeachingSlide } from "@/types/domain";
import { LoadingBlock } from "@/components/ui/LoadingBlock";

type Props = {
  embedUrl: string;
  currentSlide?: TeachingSlide;
  totalSlides: number;
  loading?: boolean;
  /** True while currentSlide is a slide the AI jumped back to in order to answer a question. */
  isReference?: boolean;
  /** Slide number (1-based) the lesson will resume on once the answer finishes. */
  resumeSlideNumber?: number;
};

// Google Slides' published/embed URL supports a #slide=id.<objectId> fragment to jump
// to a specific slide. Cross-origin means we can't listen for "did it actually
// navigate" - so we force a full iframe reload (via `key`) whenever the slide changes,
// per docs/SYSTEM_ARCHITECTURE.md's noted limitation.
export function SlidesEmbed({
  embedUrl,
  currentSlide,
  totalSlides,
  loading,
  isReference,
  resumeSlideNumber,
}: Props) {
  if (loading && !currentSlide) {
    return (
      <div className="flex h-full min-h-[280px] w-full items-center justify-center rounded-xl border border-room-border bg-room-panel">
        <LoadingBlock label="กำลังโหลดบทเรียน..." />
      </div>
    );
  }

  if (!embedUrl || !currentSlide) {
    return (
      <div className="flex h-full min-h-[280px] w-full flex-col items-center justify-center gap-2 rounded-xl border border-room-border bg-room-panel text-room-muted">
        <p className="text-sm">Mock Mode: ยังไม่มี Google Slides Embed จริง</p>
        {currentSlide && (
          <p className="text-xs">
            กำลังแสดงสไลด์ {currentSlide.index + 1} จาก {totalSlides}
          </p>
        )}
      </div>
    );
  }

  const src = `${embedUrl}#slide=id.${currentSlide.slideObjectId}`;

  return (
    <div className="relative h-full">
      <iframe
        key={src}
        src={src}
        title="Shared Screen"
        className="h-full min-h-[280px] w-full rounded-xl border border-room-border bg-white"
        sandbox="allow-scripts allow-same-origin allow-popups"
        allow="autoplay"
      />
      {isReference && (
        <div className="pointer-events-none absolute left-3 top-3 rounded-full bg-black/70 px-3 py-1 text-xs text-white shadow">
          ย้อนกลับมาที่สไลด์ {currentSlide.index + 1} เพื่อตอบคำถาม
          {resumeSlideNumber ? ` · เดี๋ยวกลับไปต่อที่สไลด์ ${resumeSlideNumber}` : ""}
        </div>
      )}
    </div>
  );
}
