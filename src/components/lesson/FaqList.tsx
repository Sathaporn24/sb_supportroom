"use client";

import type { Faq } from "@/types/domain";
import { mediaSeed } from "@/mocks/media.mock";

type Props = {
  faqs: Faq[];
  onChange: (faqs: Faq[]) => void;
};

export function FaqList({ faqs, onChange }: Props) {
  function updateFaq(index: number, patch: Partial<Faq>) {
    onChange(faqs.map((faq, i) => (i === index ? { ...faq, ...patch } : faq)));
  }

  return (
    <div className="space-y-3">
      {faqs.map((faq, index) => (
        <div key={faq.id} className="space-y-2 rounded-lg border border-room-border bg-room-panelAlt p-3">
          <div className="flex items-center justify-between gap-3">
            <input
              value={faq.question}
              onChange={(e) => updateFaq(index, { question: e.target.value })}
              className="flex-1 rounded-lg border border-room-border bg-room-bg px-3 py-1.5 text-sm text-room-text outline-none focus:border-room-accent"
            />
            <label className="flex shrink-0 items-center gap-1.5 text-xs text-room-muted">
              <input
                type="checkbox"
                checked={faq.active}
                onChange={(e) => updateFaq(index, { active: e.target.checked })}
                className="h-4 w-4 rounded border-room-border"
              />
              ใช้งาน
            </label>
          </div>

          <textarea
            value={faq.answer}
            onChange={(e) => updateFaq(index, { answer: e.target.value })}
            rows={2}
            className="w-full rounded-lg border border-room-border bg-room-bg px-3 py-2 text-sm text-room-text outline-none focus:border-room-accent"
          />

          <div className="flex flex-wrap items-center gap-3 text-xs text-room-muted">
            <label className="flex items-center gap-1.5">
              คำสำคัญ (คั่นด้วย ,)
              <input
                value={faq.keywords.join(", ")}
                onChange={(e) =>
                  updateFaq(index, { keywords: e.target.value.split(",").map((k) => k.trim()).filter(Boolean) })
                }
                className="w-48 rounded-lg border border-room-border bg-room-bg px-2 py-1 text-room-text outline-none focus:border-room-accent"
              />
            </label>
            <label className="flex items-center gap-1.5">
              ขอบเขต
              <select
                value={faq.scope}
                onChange={(e) => updateFaq(index, { scope: e.target.value as Faq["scope"] })}
                className="rounded-lg border border-room-border bg-room-bg px-2 py-1 text-room-text outline-none focus:border-room-accent"
              >
                <option value="IN_LESSON">อยู่ในบทเรียน</option>
                <option value="SYSTEM_BASIC">ระบบพื้นฐาน</option>
                <option value="OUT_OF_SCOPE">นอกเรื่อง</option>
                <option value="UNKNOWN">ไม่ทราบ</option>
              </select>
            </label>
            <label className="flex items-center gap-1.5">
              สื่อที่เกี่ยวข้อง
              <select
                value={faq.relatedMediaId ?? ""}
                onChange={(e) => updateFaq(index, { relatedMediaId: e.target.value || undefined })}
                className="rounded-lg border border-room-border bg-room-bg px-2 py-1 text-room-text outline-none focus:border-room-accent"
              >
                <option value="">ไม่มี</option>
                {mediaSeed.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.label}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
      ))}
    </div>
  );
}
