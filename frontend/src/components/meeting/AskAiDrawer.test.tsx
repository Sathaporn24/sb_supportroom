// @vitest-environment jsdom
import { createElement } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { AskAiDrawer } from "./AskAiDrawer";

// jsdom doesn't implement Element.scrollTo - AskAiDrawer calls it on open/new-question to
// keep the list pinned to the bottom, so it needs a no-op stand-in for these tests to run.
Element.prototype.scrollTo = vi.fn();

afterEach(() => {
  cleanup();
});

type DrawerProps = Parameters<typeof AskAiDrawer>[0];

function renderDrawer(overrides: Partial<DrawerProps> = {}) {
  const props: DrawerProps = {
    open: true,
    onClose: vi.fn(),
    questions: [],
    onSubmitQuestion: vi.fn(),
    inputEnabled: true,
    sendEnabled: true,
    failedQuestionText: null,
    onFailedQuestionTextConsumed: vi.fn(),
    ...overrides,
  };
  return { ...render(createElement(AskAiDrawer, props)), props };
}

// QA-03 residual - a typed question that failed to send used to restore its text into the
// draft field unconditionally, clobbering whatever the learner had already started typing for
// the *next* question while the failed one was still "processing-question". Regression guard
// for that clobber: draft must survive a failure that arrives after the learner has moved on.
describe("AskAiDrawer - QA-03 residual (failedQuestionText must not clobber an in-progress draft)", () => {
  it("keeps the learner's in-progress Q2 draft when Q1 fails after Q2 was already typed", () => {
    const onFailedQuestionTextConsumed = vi.fn();
    const { rerender, props } = renderDrawer({ onFailedQuestionTextConsumed });

    const input = screen.getByPlaceholderText("พิมพ์คำถาม...") as HTMLInputElement;
    fireEvent.change(input, { target: { value: "Q2 กำลังพิมพ์อยู่" } });
    expect(input.value).toBe("Q2 กำลังพิมพ์อยู่");

    // Q1 (already submitted and cleared earlier) now fails - the runtime hands its text back.
    rerender(
      createElement(AskAiDrawer, {
        ...props,
        onFailedQuestionTextConsumed,
        failedQuestionText: "Q1 ที่ส่งไม่สำเร็จ",
      }),
    );

    expect(input.value).toBe("Q2 กำลังพิมพ์อยู่");
    expect(onFailedQuestionTextConsumed).toHaveBeenCalledTimes(1);
  });

  it("restores the failed question text when the draft is still empty", () => {
    const onFailedQuestionTextConsumed = vi.fn();
    const { rerender, props } = renderDrawer({ onFailedQuestionTextConsumed });

    const input = screen.getByPlaceholderText("พิมพ์คำถาม...") as HTMLInputElement;
    expect(input.value).toBe("");

    rerender(
      createElement(AskAiDrawer, {
        ...props,
        onFailedQuestionTextConsumed,
        failedQuestionText: "Q1 ที่ส่งไม่สำเร็จ",
      }),
    );

    expect(input.value).toBe("Q1 ที่ส่งไม่สำเร็จ");
    expect(onFailedQuestionTextConsumed).toHaveBeenCalledTimes(1);
  });
});
