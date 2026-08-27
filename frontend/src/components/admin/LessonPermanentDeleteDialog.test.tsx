// @vitest-environment jsdom
import { createElement } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { LessonPermanentDeleteDialog } from "./LessonPermanentDeleteDialog";
import type { LessonTrashItem } from "@/types/domain";

const requestLessonPermanentDelete = vi.fn();
const toastAdd = vi.fn();

vi.mock("@/lib/api-client", () => ({
  requestLessonPermanentDelete: (...args: unknown[]) => requestLessonPermanentDelete(...args),
  ApiClientError: class ApiClientError extends Error {},
}));

vi.mock("@/components/ui/toast", () => ({
  toast: { add: (...args: unknown[]) => toastAdd(...args) },
}));

const TRASH_ITEM: LessonTrashItem = {
  id: "lesson-1",
  slug: "lesson-1",
  title: "บทเรียนทดสอบ",
  categoryId: "category-1",
  deletedAt: "2026-01-01T00:00:00Z",
  scheduledPurgeAt: "2026-03-02T00:00:00Z",
  remainingDays: 20,
  urgency: "neutral",
  purgeState: "trash",
};

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("LessonPermanentDeleteDialog - LT-2/LT-10", () => {
  it("submits the typed confirmation title and reports queued success for an empty 202", async () => {
    requestLessonPermanentDelete.mockResolvedValue(undefined);
    const onClose = vi.fn();
    const onQueued = vi.fn();
    render(createElement(LessonPermanentDeleteDialog, { lesson: TRASH_ITEM, onClose, onQueued }));

    fireEvent.change(screen.getByTestId("lesson-permanent-delete-confirmation-input"), {
      target: { value: TRASH_ITEM.title },
    });
    fireEvent.click(screen.getByTestId("lesson-permanent-delete-confirm-button"));

    await waitFor(() => {
      expect(requestLessonPermanentDelete).toHaveBeenCalledWith(TRASH_ITEM.id, {
        confirmationTitle: TRASH_ITEM.title,
      });
    });
    expect(toastAdd).toHaveBeenCalledWith({ title: "ตั้งคิวลบถาวรบทเรียนแล้ว", type: "success" });
    expect(onQueued).toHaveBeenCalledOnce();
    expect(onClose).toHaveBeenCalledOnce();
  });
});
