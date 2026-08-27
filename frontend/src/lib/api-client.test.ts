import { afterEach, describe, expect, it, vi } from "vitest";

import { askTextQuestion, askVoiceQuestion, requestLessonPermanentDelete } from "@/lib/api-client";

describe("question request timeout", () => {
  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it.each([
    [
      "text",
      () => askTextQuestion({ token: "token", learnerKey: "learner", text: "คำถาม" }),
    ],
    [
      "voice",
      () =>
        askVoiceQuestion({
          audioBlob: new Blob(["audio"], { type: "audio/webm" }),
          token: "token",
          learnerKey: "learner",
          durationMs: 2_000,
        }),
    ],
  ])("aborts a stalled %s question after the user-facing deadline", async (_channel, ask) => {
    vi.useFakeTimers();
    const fetchMock = vi.fn((_input: string | URL | Request, init?: RequestInit) =>
      new Promise<Response>((_resolve, reject) => {
        init?.signal?.addEventListener(
          "abort",
          () => reject(new DOMException("The operation was aborted", "AbortError")),
          { once: true },
        );
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    const pending = ask();
    const rejection = expect(pending).rejects.toMatchObject({ name: "AbortError" });

    await vi.advanceTimersByTimeAsync(45_000);

    await rejection;
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0]?.[1]?.signal?.aborted).toBe(true);
  });
});

describe("lesson permanent-delete request", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("treats the backend's empty 202 response as a successful queued request", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 202 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      requestLessonPermanentDelete("lesson-1", { confirmationTitle: "บทเรียนทดสอบ" }),
    ).resolves.toBeUndefined();

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/lessons/lesson-1/permanent-delete",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ confirmationTitle: "บทเรียนทดสอบ" }),
      }),
    );
  });
});
