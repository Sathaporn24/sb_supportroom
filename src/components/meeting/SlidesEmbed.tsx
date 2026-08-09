import type { TeachingSlide } from "@/types/domain";
import { getApiBaseUrl } from "@/lib/api-client";
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

// Two content sources render differently: a PDF-sourced lesson gets a plain per-page <img>
// (see the early return below); a Google-Slides-sourced lesson uses the published/embed URL's
// #slide=id.<objectId> fragment to jump to a specific slide. Cross-origin means we can't listen
// for "did it actually navigate" for the iframe case - so we force a full iframe reload (via
// `key`) whenever the slide changes, per docs/SYSTEM_ARCHITECTURE.md's noted limitation.
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

  // PDF-sourced lessons have no embed iframe - the resolved slide carries its own per-page
  // image URL instead (populated by PdfSlidesRenderer on the backend). Check this before the
  // Mock-mode fallback below, since a PDF lesson legitimately has no embedUrl at all.
  if (currentSlide?.slideUrl) {
    const imageSrc = `${getApiBaseUrl()}${currentSlide.slideUrl}`;
    return (
      <div className="relative flex h-full min-h-[280px] w-full items-center justify-center overflow-hidden rounded-xl border border-room-border bg-white">
        {/* eslint-disable-next-line @next/next/no-img-element -- backend-rendered PNG, not a next/image-optimizable static asset */}
        <img key={imageSrc} src={imageSrc} alt={`สไลด์ ${currentSlide.index + 1}`} className="max-h-full max-w-full object-contain" />
        {isReference && (
          <div className="pointer-events-none absolute left-3 top-3 rounded-full bg-black/70 px-3 py-1 text-xs text-white shadow">
            ย้อนกลับมาที่สไลด์ {currentSlide.index + 1} เพื่อตอบคำถาม
            {resumeSlideNumber ? ` · เดี๋ยวกลับไปต่อที่สไลด์ ${resumeSlideNumber}` : ""}
          </div>
        )}
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

  // Google's /embed iframe always renders its own bottom toolbar (page number, prev/next
  // arrows, "Google Slides" link) and lets clicks on it navigate slides - neither of which
  // fits a passive "shared screen" that only the tutor engine should drive. There is no
  // official flag to turn the toolbar off, so it's clipped by rendering the iframe taller
  // than its visible box (overflow-hidden crops the extra height off the bottom) and a
  // transparent overlay on top eats every click before it reaches the iframe.
  return (
    <div className="relative h-full min-h-[280px] w-full overflow-hidden rounded-xl border border-room-border bg-white">
      <iframe
        key={src}
        src={src}
        title="Shared Screen"
        className="absolute inset-x-0 top-0 w-full border-0"
        style={{ height: "calc(100% + 40px)" }}
        sandbox="allow-scripts allow-same-origin"
        allow="autoplay"
        tabIndex={-1}
      />
      <div className="absolute inset-0" aria-hidden="true" />
      {isReference && (
        <div className="pointer-events-none absolute left-3 top-3 rounded-full bg-black/70 px-3 py-1 text-xs text-white shadow">
          ย้อนกลับมาที่สไลด์ {currentSlide.index + 1} เพื่อตอบคำถาม
          {resumeSlideNumber ? ` · เดี๋ยวกลับไปต่อที่สไลด์ ${resumeSlideNumber}` : ""}
        </div>
      )}
    </div>
  );
}
