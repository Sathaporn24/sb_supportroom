import { XIcon } from "lucide-react";
import type { TeachingSlide } from "@/types/domain";
import { getApiBaseUrl } from "@/lib/api-client";
import { LoadingBlock } from "@/components/shared/LoadingBlock";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type Props = {
  embedUrl: string;
  currentSlide?: TeachingSlide;
  totalSlides: number;
  loading?: boolean;
  /** True while currentSlide is a slide the AI jumped back to in order to answer a question. */
  isReference?: boolean;
  /** Slide number (1-based) the lesson will resume on once the answer finishes. */
  resumeSlideNumber?: number;
  /** RS-6 - in-app fullscreen state, owned by the room page (not the Fullscreen API - Safari on
   * iPhone doesn't support requestFullscreen() on anything but <video>). */
  fullscreen?: boolean;
  onToggleFullscreen?: () => void;
};

/** RS-6 - the close button plus "tap anywhere to close" backdrop shared by both content
 * branches below. The close button sits above the backdrop (z-20 vs z-0) so it still wins the
 * tap in the corner where the two overlap. */
function FullscreenCloseOverlay({ onClose, dim }: { onClose: () => void; dim: boolean }) {
  return (
    <>
      <button
        type="button"
        onClick={onClose}
        aria-label="ปิดมุมมองเต็มจอ"
        data-testid="room-slide-fullscreen-backdrop-button"
        className={cn("absolute inset-0 z-0", dim && "bg-black")}
      />
      <Button
        type="button"
        variant="secondary"
        size="icon-lg"
        onClick={onClose}
        aria-label="ปิดมุมมองเต็มจอ"
        title="ปิดมุมมองเต็มจอ"
        data-testid="room-slide-fullscreen-close-button"
        className="absolute right-3 top-3 z-20 size-11 rounded-full"
      >
        <XIcon />
      </Button>
    </>
  );
}

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
  fullscreen = false,
  onToggleFullscreen,
}: Props) {
  if (loading && !currentSlide) {
    return (
      <div className="flex h-full min-h-[280px] w-full items-center justify-center rounded-xl border bg-card">
        <LoadingBlock label="กำลังโหลดบทเรียน..." />
      </div>
    );
  }

  // The wrapper is the only thing that changes shape between regular and fullscreen - neither
  // the iframe/img nor its `key` is unmounted or recreated, so entering/leaving fullscreen never
  // reloads the slide or loses playback position.
  const wrapperClassName = cn(
    "relative h-full min-h-[280px] w-full overflow-hidden rounded-xl border bg-white",
    fullscreen && "fixed inset-0 z-50 h-[100dvh] min-h-0 w-screen rounded-none border-none",
  );

  // PDF-sourced lessons have no embed iframe - the resolved slide carries its own per-page
  // image URL instead (populated by PdfSlidesRenderer on the backend). Check this before the
  // Mock-mode fallback below, since a PDF lesson legitimately has no embedUrl at all.
  if (currentSlide?.slideUrl) {
    const imageSrc = `${getApiBaseUrl()}${currentSlide.slideUrl}`;
    return (
      <div className={cn(wrapperClassName, "flex items-center justify-center")}>
        {fullscreen ? (
          <FullscreenCloseOverlay onClose={() => onToggleFullscreen?.()} dim />
        ) : (
          onToggleFullscreen && (
            <button
              type="button"
              onClick={onToggleFullscreen}
              aria-label="ขยายสไลด์เต็มจอ"
              data-testid="room-slide-fullscreen-toggle-button"
              className="absolute inset-0 z-10 cursor-zoom-in bg-transparent"
            />
          )
        )}
        {/* eslint-disable-next-line @next/next/no-img-element -- backend-rendered PNG, not a next/image-optimizable static asset */}
        <img
          key={imageSrc}
          src={imageSrc}
          alt={`สไลด์ ${currentSlide.index + 1}`}
          className={cn("pointer-events-none relative z-10 max-h-full max-w-full object-contain")}
        />
        {isReference && !fullscreen && (
          <div className="pointer-events-none absolute left-3 top-3 z-20 rounded-full bg-black/70 px-3 py-1 text-xs text-white shadow">
            ย้อนกลับมาที่สไลด์ {currentSlide.index + 1} เพื่อตอบคำถาม
            {resumeSlideNumber ? ` · เดี๋ยวกลับไปต่อที่สไลด์ ${resumeSlideNumber}` : ""}
          </div>
        )}
      </div>
    );
  }

  if (!embedUrl || !currentSlide) {
    return (
      <div className="flex h-full min-h-[280px] w-full flex-col items-center justify-center gap-2 rounded-xl border bg-card text-muted-foreground">
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
  // transparent overlay on top eats every click before it reaches the iframe. RS-6 reuses this
  // same overlay as the expand/close hit target - the iframe is cross-origin and cannot be
  // given a click handler directly.
  return (
    <div className={wrapperClassName}>
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
      {fullscreen ? (
        <FullscreenCloseOverlay onClose={() => onToggleFullscreen?.()} dim={false} />
      ) : (
        <button
          type="button"
          onClick={onToggleFullscreen}
          aria-label="ขยายสไลด์เต็มจอ"
          data-testid="room-slide-fullscreen-toggle-button"
          disabled={!onToggleFullscreen}
          className="absolute inset-0 z-10 disabled:cursor-default"
        />
      )}
      {isReference && !fullscreen && (
        <div className="pointer-events-none absolute left-3 top-3 z-20 rounded-full bg-black/70 px-3 py-1 text-xs text-white shadow">
          ย้อนกลับมาที่สไลด์ {currentSlide.index + 1} เพื่อตอบคำถาม
          {resumeSlideNumber ? ` · เดี๋ยวกลับไปต่อที่สไลด์ ${resumeSlideNumber}` : ""}
        </div>
      )}
    </div>
  );
}
