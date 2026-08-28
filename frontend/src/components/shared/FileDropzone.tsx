"use client";

import { useRef, useState, type DragEvent } from "react";
import { UploadIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/utils";

type Props = {
  /** Same value an <input type="file" accept> would take, e.g. ".pptx,.pdf,.docx,.xlsx". */
  accept: string;
  disabled?: boolean;
  /** True while a previously-selected file is being read/uploaded - swaps the whole box to a
   * spinner + status line instead of the pick affordance, since there is nothing to click while
   * busy. */
  busy?: boolean;
  busyLabel?: string;
  onFileSelected: (file: File) => void;
  testIdPrefix: string;
  /** e.g. "รองรับ .pptx, .pdf, .docx, .xlsx" - shown under the button. */
  hint?: string;
  className?: string;
};

/** Big drag-and-drop upload area (Figma redesign, 2026-08-28) - replaces the old hidden
 * `<input type=file>` + button pattern on both the Document Library upload and the PDF-lesson
 * create form. Selection-only: it never uploads anything itself, so create-mode's
 * preview-session flow and the library's immediate-upload flow can both drive it identically via
 * onFileSelected. */
export function FileDropzone({
  accept,
  disabled,
  busy,
  busyLabel = "กำลังอัปโหลด...",
  onFileSelected,
  testIdPrefix,
  hint,
  className,
}: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragActive, setDragActive] = useState(false);
  const inert = Boolean(disabled) || Boolean(busy);

  function handleFiles(files: FileList | null) {
    const file = files?.[0];
    if (file) onFileSelected(file);
  }

  return (
    <div
      className={cn(
        "flex min-h-56 flex-col items-center justify-center gap-2 rounded-xl border border-dashed p-6 text-center transition-colors",
        dragActive ? "border-primary bg-primary/5" : "border-input",
        inert && "opacity-60",
        className,
      )}
      onDragOver={(e: DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        if (!inert) setDragActive(true);
      }}
      onDragLeave={() => setDragActive(false)}
      onDrop={(e: DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        setDragActive(false);
        if (!inert) handleFiles(e.dataTransfer.files);
      }}
      data-testid={`${testIdPrefix}-dropzone`}
      data-dragactive={dragActive}
    >
      <input
        ref={inputRef}
        type="file"
        accept={accept}
        className="hidden"
        disabled={inert}
        onChange={(e) => {
          handleFiles(e.target.files);
          e.target.value = "";
        }}
        data-testid={`${testIdPrefix}-file-input`}
      />
      {busy ? (
        <>
          <Spinner className="size-6 text-primary" />
          <p className="text-sm font-medium text-foreground">{busyLabel}</p>
        </>
      ) : (
        <>
          <div className="flex size-10 items-center justify-center rounded-full bg-muted">
            <UploadIcon className="size-5 text-muted-foreground" />
          </div>
          <p className="text-sm font-semibold text-foreground">อัปโหลดไฟล์</p>
          <p className="text-xs text-muted-foreground">ลากไฟล์มาวางที่นี่เพื่ออัปโหลด หรือ</p>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="mt-1"
            disabled={inert}
            onClick={() => inputRef.current?.click()}
            data-testid={`${testIdPrefix}-button`}
          >
            เลือกไฟล์
          </Button>
          {hint && <p className="mt-2 text-xs text-muted-foreground">{hint}</p>}
        </>
      )}
    </div>
  );
}
