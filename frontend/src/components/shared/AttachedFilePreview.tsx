"use client";

import { FileTextIcon, XIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PdfPageThumbnail } from "@/components/shared/PdfPageThumbnail";

type Props = {
  /** Page-1 render URL (NR-18/NR-10) - null while the caller hasn't got one yet (transient, since
   * it's set together with fileName in practice). */
  imageSrc: string | null;
  fileName: string;
  onRemove: () => void;
  testIdPrefix: string;
};

/** Compact "you just attached this" card (Figma redesign, 2026-08-28) - a fixed-width page
 * thumbnail with a PDF badge overlaid at the corner, not a full-width bar. Used once a PDF has
 * been read/uploaded in LessonForm's pdfField, replacing FileDropzone until onRemove clears it. */
export function AttachedFilePreview({ imageSrc, fileName, onRemove, testIdPrefix }: Props) {
  return (
    <div className="flex flex-col gap-1.5" data-testid={`${testIdPrefix}-attached-preview`}>
      <div className="relative w-36">
        <div className="aspect-[3/4] w-36 overflow-hidden rounded-lg border bg-muted shadow-sm">
          {imageSrc ? (
            <PdfPageThumbnail imageSrc={imageSrc} alt={fileName} className="h-full w-full rounded-none border-0" />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-xs text-muted-foreground">
              ไม่มีตัวอย่าง
            </div>
          )}
        </div>
        <Button
          type="button"
          variant="secondary"
          size="icon-xs"
          className="absolute -top-2 -right-2 rounded-full shadow"
          aria-label="ล้างไฟล์ที่แนบ"
          onClick={onRemove}
          data-testid={`${testIdPrefix}-clear-button`}
        >
          <XIcon />
        </Button>
        {/* No --destructive-foreground token exists in this theme (only a light bg-destructive/10
            tint is defined) - bg-destructive + text-white for a solid badge matches the existing
            precedent in PushToTalkButton's recording state, not a one-off raw color. */}
        <div className="absolute -right-2 -bottom-2 flex items-center gap-1 rounded-md bg-destructive px-1.5 py-1 text-xs font-bold text-white shadow">
          <FileTextIcon className="size-3" />
          PDF
        </div>
      </div>
      <p className="w-36 truncate text-xs text-muted-foreground" title={fileName}>
        {fileName}
      </p>
    </div>
  );
}
