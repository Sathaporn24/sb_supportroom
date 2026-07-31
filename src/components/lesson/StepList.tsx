"use client";

import type { Step } from "@/types/domain";
import { mediaSeed } from "@/mocks/media.mock";
import { checkpointPrompts } from "@/config/checkpoint-prompts";

type Props = {
  steps: Step[];
  onChange: (steps: Step[]) => void;
};

function moveItem<T extends { order: number }>(list: T[], index: number, direction: -1 | 1): T[] {
  const target = index + direction;
  if (target < 0 || target >= list.length) {
    return list;
  }
  const next = [...list];
  const a = next[index];
  const b = next[target];
  if (!a || !b) {
    return list;
  }
  next[index] = b;
  next[target] = a;
  return next.map((item, i) => ({ ...item, order: i }));
}

export function StepList({ steps, onChange }: Props) {
  function updateStep(stepIndex: number, patch: Partial<Step>) {
    const next = steps.map((s, i) => (i === stepIndex ? { ...s, ...patch } : s));
    onChange(next);
  }

  function moveStep(stepIndex: number, direction: -1 | 1) {
    onChange(moveItem(steps, stepIndex, direction));
  }

  function moveSegment(stepIndex: number, segIndex: number, direction: -1 | 1) {
    const step = steps[stepIndex];
    const nextSegments = moveItem(step.segments, segIndex, direction);
    updateStep(stepIndex, { segments: nextSegments });
  }

  function updateSegment(stepIndex: number, segIndex: number, patch: Partial<Step["segments"][number]>) {
    const step = steps[stepIndex];
    const nextSegments = step.segments.map((seg, i) => (i === segIndex ? { ...seg, ...patch } : seg));
    updateStep(stepIndex, { segments: nextSegments });
  }

  return (
    <div className="space-y-3">
      {steps.map((step, stepIndex) => (
        <div key={step.id} className="rounded-xl border border-room-border bg-room-panel">
          <div className="flex items-center justify-between gap-3 px-4 py-3">
            <span className="text-sm font-semibold text-room-text">
              ขั้นตอนที่ {stepIndex + 1}: {step.title}
            </span>
            <span className="flex items-center gap-1">
              <button
                type="button"
                aria-label="เลื่อนขั้นตอนขึ้น"
                onClick={() => moveStep(stepIndex, -1)}
                disabled={stepIndex === 0}
                className="rounded-md border border-room-border px-2 py-1 text-xs text-room-muted hover:text-room-text disabled:opacity-30"
              >
                ขึ้น
              </button>
              <button
                type="button"
                aria-label="เลื่อนขั้นตอนลง"
                onClick={() => moveStep(stepIndex, 1)}
                disabled={stepIndex === steps.length - 1}
                className="rounded-md border border-room-border px-2 py-1 text-xs text-room-muted hover:text-room-text disabled:opacity-30"
              >
                ลง
              </button>
            </span>
          </div>

          <div className="space-y-4 border-t border-room-border px-4 py-4">
            <label className="block text-sm">
              <span className="mb-1 block text-room-muted">ชื่อขั้นตอน</span>
              <input
                value={step.title}
                onChange={(e) => updateStep(stepIndex, { title: e.target.value })}
                className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-room-text outline-none focus:border-room-accent"
              />
            </label>

            <div className="flex flex-wrap items-center gap-4">
              <label className="flex items-center gap-2 text-sm text-room-text">
                <input
                  type="checkbox"
                  checked={step.checkpointEnabled}
                  onChange={(e) => updateStep(stepIndex, { checkpointEnabled: e.target.checked })}
                  className="h-4 w-4 rounded border-room-border"
                />
                เปิด Checkpoint
              </label>
              {step.checkpointEnabled && (
                <label className="flex items-center gap-2 text-sm text-room-text">
                  คำถาม Checkpoint
                  <select
                    value={step.checkpointPromptId}
                    onChange={(e) => updateStep(stepIndex, { checkpointPromptId: e.target.value })}
                    className="rounded-lg border border-room-border bg-room-bg px-2 py-1.5 text-room-text outline-none focus:border-room-accent"
                  >
                    {checkpointPrompts.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.text}
                      </option>
                    ))}
                  </select>
                </label>
              )}
            </div>

            <div className="space-y-3">
              {step.segments.map((segment, segIndex) => (
                <div key={segment.id} className="rounded-lg border border-room-border bg-room-panelAlt p-3">
                  <div className="mb-2 flex items-center justify-between">
                    <span className="text-xs font-medium text-room-muted">ส่วนที่ {segIndex + 1}</span>
                    <span className="flex items-center gap-1">
                      <button
                        type="button"
                        aria-label="เลื่อนส่วนขึ้น"
                        onClick={() => moveSegment(stepIndex, segIndex, -1)}
                        disabled={segIndex === 0}
                        className="rounded-md border border-room-border px-2 py-0.5 text-xs text-room-muted hover:text-room-text disabled:opacity-30"
                      >
                        ขึ้น
                      </button>
                      <button
                        type="button"
                        aria-label="เลื่อนส่วนลง"
                        onClick={() => moveSegment(stepIndex, segIndex, 1)}
                        disabled={segIndex === step.segments.length - 1}
                        className="rounded-md border border-room-border px-2 py-0.5 text-xs text-room-muted hover:text-room-text disabled:opacity-30"
                      >
                        ลง
                      </button>
                    </span>
                  </div>
                  <textarea
                    value={segment.scriptText}
                    onChange={(e) => updateSegment(stepIndex, segIndex, { scriptText: e.target.value })}
                    rows={2}
                    className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-sm text-room-text outline-none focus:border-room-accent"
                  />
                  <label className="mt-2 flex items-center gap-2 text-xs text-room-muted">
                    สื่อประกอบ
                    <select
                      value={segment.mediaId}
                      onChange={(e) => updateSegment(stepIndex, segIndex, { mediaId: e.target.value })}
                      className="rounded-lg border border-room-border bg-room-bg px-2 py-1 text-xs text-room-text outline-none focus:border-room-accent"
                    >
                      {mediaSeed.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.label}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
              ))}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
