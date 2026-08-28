"use client";

import { useEffect, useState } from "react";
import { fetchAuthenticatedImageUrl } from "@/lib/api-client";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

type Props = {
  /** Admin PDF page endpoint URL (NR-18) - either a preview session page or an already-persisted
   * document page. Fetched with auth internally via fetchAuthenticatedImageUrl, same as
   * SlideNarrationEditorCard. */
  imageSrc: string;
  alt: string;
  className?: string;
};

/** Shared by SlideNarrationEditorCard (per-page editor) and any upload UI that wants a quick
 * "here's what you just attached" preview (e.g. LessonForm's PDF picker) - both need the exact
 * same authenticated-blob-URL image loading, so it lives here once instead of twice. */
export function PdfPageThumbnail({ imageSrc, alt, className }: Props) {
  const [displaySrc, setDisplaySrc] = useState<string | null>(null);
  const [imageFailed, setImageFailed] = useState(false);

  useEffect(() => {
    let objectUrl: string | null = null;
    let cancelled = false;
    setDisplaySrc(null);
    setImageFailed(false);

    fetchAuthenticatedImageUrl(imageSrc)
      .then((url) => {
        if (cancelled) {
          URL.revokeObjectURL(url);
          return;
        }
        objectUrl = url;
        setDisplaySrc(url);
      })
      .catch(() => {
        if (!cancelled) setImageFailed(true);
      });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [imageSrc]);

  return (
    <div
      className={cn(
        "flex aspect-video items-center justify-center overflow-hidden rounded-lg border bg-muted",
        className,
      )}
    >
      {imageFailed ? (
        <p className="p-2 text-center text-xs text-muted-foreground">โหลดภาพตัวอย่างไม่สำเร็จ</p>
      ) : displaySrc ? (
        // eslint-disable-next-line @next/next/no-img-element -- backend-rendered PNG via an authenticated blob URL, not a next/image-optimizable static asset
        <img src={displaySrc} alt={alt} className="max-h-full max-w-full object-contain" />
      ) : (
        <Skeleton className="h-full w-full" />
      )}
    </div>
  );
}
