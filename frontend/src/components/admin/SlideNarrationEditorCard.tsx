"use client";

import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { fetchAuthenticatedImageUrl } from "@/lib/api-client";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";

// DtoLimits.NarrationTextMaxLength (backend) - 5000 characters is what Edge TTS can synthesize
// for one page without needing to be split up.
const NARRATION_MAX_LENGTH = 5000;

type SlideNarrationEditorCardProps = {
  /** What to show as the card's heading (e.g. "หน้า 3" or, for an excluded page, "หน้าที่ 7 ของ
   * ไฟล์" - EX-11). The two meanings differ once pages can be excluded (R4.7), so the caller
   * computes this rather than the card deriving a number itself. */
  pageLabel: string;
  /** Admin PDF page endpoint URL (NR-18) - either a preview session page (create phase) or an
   * already-persisted document page (narrations page). Fetched with auth internally, see
   * fetchAuthenticatedImageUrl. */
  imageSrc: string;
  value: string;
  onChange: (value: string) => void;
  badge?: ReactNode;
  /** Per-caller action row (e.g. the narrations page's immediate Save button) - the create
   * content phase passes nothing here since nothing is persisted until confirm (R4.6.5/R4.6.6). */
  footer?: ReactNode;
  disabled?: boolean;
  testIdPrefix: string;
  /** EX-3(ข)/EX-11/EX-9 (R4.7) - true when this page is currently excluded from teaching. Renders
   * a faded card + "ตัดออกแล้ว" badge + restore button, and makes the textarea read-only (server
   * also rejects the save per EX-12(ก) - this is belt-and-braces, not the only enforcement). */
  isExcluded?: boolean;
  /** Toggles exclusion for this page. Omit to hide the cut/restore button entirely (e.g. while
   * the surface doesn't support exclusion yet). */
  onToggleExcluded?: () => void;
  /** EX-8 (R4.7) - disables only the "ตัดหน้านี้ออก" direction on the last remaining page, so the
   * UI mirrors the server's hard "at least 1 page" floor; the server still enforces this
   * independently. Never blocks "เอากลับ". */
  excludeToggleDisabled?: boolean;
  /** True while this page's own toggle request is in flight - disables the button in either
   * direction to prevent a double-click racing the in-flight request. */
  toggleInFlight?: boolean;
};

/** Shared by the create-lesson content-management phase (Module J/NR-12) and the existing
 * /admin/lessons/[slug]/narrations editor (NR-1..NR-9) per R4.6.4/Q-J2 - both must look and
 * behave identically. Owns nothing about persistence: value/onChange are fully controlled by the
 * caller, which is exactly what differs between the two (client-only draft vs. immediate save). */
export function SlideNarrationEditorCard({
  pageLabel,
  imageSrc,
  value,
  onChange,
  badge,
  footer,
  disabled,
  testIdPrefix,
  isExcluded = false,
  onToggleExcluded,
  excludeToggleDisabled,
  toggleInFlight,
}: SlideNarrationEditorCardProps) {
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
    <Card size="sm" className={cn(isExcluded && "opacity-60")}>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-xs tracking-wide text-muted-foreground uppercase">
          {pageLabel}
          {isExcluded && <Badge variant="outline">ตัดออกแล้ว</Badge>}
          {badge}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 sm:flex-row">
        <div className="flex aspect-video w-full shrink-0 items-center justify-center overflow-hidden rounded-lg border bg-muted sm:w-[45%]">
          {imageFailed ? (
            <p className="p-2 text-center text-xs text-muted-foreground">โหลดภาพสไลด์ไม่สำเร็จ</p>
          ) : displaySrc ? (
            // eslint-disable-next-line @next/next/no-img-element -- backend-rendered PNG via an authenticated blob URL, not a next/image-optimizable static asset
            <img
              src={displaySrc}
              alt={pageLabel}
              className="max-h-full max-w-full object-contain"
            />
          ) : (
            <Skeleton className="h-full w-full" />
          )}
        </div>
        <div className="flex flex-1 flex-col gap-2">
          <Textarea
            value={value}
            maxLength={NARRATION_MAX_LENGTH}
            rows={4}
            disabled={disabled}
            readOnly={isExcluded}
            onChange={(e) => onChange(e.target.value)}
            data-testid={`${testIdPrefix}-textarea`}
          />
          <div className="flex items-center justify-between gap-2">
            <p className="text-xs text-muted-foreground">
              {value.length}/{NARRATION_MAX_LENGTH} ตัวอักษร
            </p>
            <div className="flex items-center gap-2">
              {onToggleExcluded && (
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={toggleInFlight || (!isExcluded && excludeToggleDisabled)}
                  onClick={onToggleExcluded}
                  data-testid={`${testIdPrefix}-toggle-excluded-button`}
                >
                  {isExcluded ? "เอากลับ" : "ตัดหน้านี้ออก"}
                </Button>
              )}
              {footer}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
