// @vitest-environment jsdom
import { createElement } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { LessonTrashList } from "./LessonTrashList";
import type { LessonTrashItem } from "@/types/domain";

const listTrashedLessons = vi.fn();

vi.mock("@/lib/api-client", () => ({
  listTrashedLessons: (...args: unknown[]) => listTrashedLessons(...args),
  restoreLesson: vi.fn(),
  ApiClientError: class ApiClientError extends Error {},
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

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

// LT-7 - this tab is strictly read-only apart from restore/permanent-delete: it must never grow
// an edit/upload/move/link/bulk affordance, and each of those two actions is itself gated by role.
describe("LessonTrashList - LT-2/LT-7 role and action visibility", () => {
  it("shows restore but not permanent-delete for admin", async () => {
    listTrashedLessons.mockResolvedValue({ lessons: [TRASH_ITEM] });
    render(createElement(LessonTrashList, { role: "admin", refreshToken: 0, onLessonRestored: vi.fn() }));

    await waitFor(() => expect(screen.getByTestId("lesson-trash-row-lesson-1-restore-button")).toBeTruthy());
    expect(screen.queryByTestId("lesson-trash-row-lesson-1-permanent-delete-button")).toBeNull();
  });

  it("shows both restore and permanent-delete for owner", async () => {
    listTrashedLessons.mockResolvedValue({ lessons: [TRASH_ITEM] });
    render(createElement(LessonTrashList, { role: "owner", refreshToken: 0, onLessonRestored: vi.fn() }));

    await waitFor(() => expect(screen.getByTestId("lesson-trash-row-lesson-1-restore-button")).toBeTruthy());
    expect(screen.getByTestId("lesson-trash-row-lesson-1-permanent-delete-button")).toBeTruthy();
  });

  it("shows neither action for cs", async () => {
    listTrashedLessons.mockResolvedValue({ lessons: [TRASH_ITEM] });
    render(createElement(LessonTrashList, { role: "cs", refreshToken: 0, onLessonRestored: vi.fn() }));

    await waitFor(() => expect(screen.getByTestId("lesson-trash-row-lesson-1-status-badge")).toBeTruthy());
    expect(screen.queryByTestId("lesson-trash-row-lesson-1-restore-button")).toBeNull();
    expect(screen.queryByTestId("lesson-trash-row-lesson-1-permanent-delete-button")).toBeNull();
  });

  it("disables every action once purging has started, even for owner", async () => {
    listTrashedLessons.mockResolvedValue({ lessons: [{ ...TRASH_ITEM, purgeState: "purging" as const }] });
    render(createElement(LessonTrashList, { role: "owner", refreshToken: 0, onLessonRestored: vi.fn() }));

    await waitFor(() => expect(screen.getByText("ไม่มีการดำเนินการ")).toBeTruthy());
    expect(screen.queryByTestId("lesson-trash-row-lesson-1-restore-button")).toBeNull();
    expect(screen.queryByTestId("lesson-trash-row-lesson-1-permanent-delete-button")).toBeNull();
  });
});
