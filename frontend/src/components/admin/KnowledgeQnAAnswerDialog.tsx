"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type { KnowledgeCategory, KnowledgeQnAQueueItem, KnowledgeScopeType } from "@/types/domain";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { Textarea } from "@/components/ui/textarea";

// DtoLimits.QnAQuestionMaxLength/QnAAnswerMaxLength (backend).
const QUESTION_MAX_LENGTH = 1000;
const ANSWER_MAX_LENGTH = 5000;

type Props = {
  open: boolean;
  /** The queue rows this Q&A will close (QQ-7 - one Q&A can close several at once). */
  items: KnowledgeQnAQueueItem[];
  onClose: () => void;
  onSaved: () => void;
};

/**
 * R5's core write path: CS writes the right answer directly at the point a gap was found. Scope
 * prefills to "lesson" of the first selected question (QQ-8) but is always changeable before
 * saving - there is no auto-save, the CS must press "บันทึกคำตอบ" (R5.7: usable immediately once
 * they do).
 */
export function KnowledgeQnAAnswerDialog({ open, items, onClose, onSaved }: Props) {
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState("");
  const [scopeType, setScopeType] = useState<KnowledgeScopeType>("lesson");
  const [scopeId, setScopeId] = useState<string | undefined>(undefined);
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const primaryItem = items[0];

  useEffect(() => {
    if (!open) return;
    setQuestion(primaryItem?.transcript ?? "");
    setAnswer("");
    setError(null);
    if (primaryItem?.lessonId) {
      setScopeType("lesson");
      setScopeId(primaryItem.lessonId);
    } else {
      setScopeType("company");
      setScopeId(undefined);
    }
    void api.listKnowledgeCategories().then(({ categories: list }) => setCategories(list));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, primaryItem?.id]);

  function handleScopeTypeChange(next: KnowledgeScopeType) {
    setScopeType(next);
    setScopeId(next === "lesson" ? primaryItem?.lessonId : undefined);
  }

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      await api.createKnowledgeQnA({
        question,
        answer,
        scopeType,
        scopeId,
        sessionQuestionIds: items.map((item) => item.id),
      });
      onSaved();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "บันทึกคำตอบไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  const subcategories = categories
    .filter((c) => c.level === 2)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));
  const parentsById = new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c]));

  const canSave =
    question.trim().length > 0 &&
    answer.trim().length > 0 &&
    (scopeType !== "category" || Boolean(scopeId)) &&
    items.length > 0;

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-lg" data-testid="qna-answer-dialog">
        <DialogHeader>
          <DialogTitle>เขียนคำตอบ ({items.length} คำถามที่เลือก)</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="qna-question">คำถาม (แก้ให้เป็นคำถามทั่วไปได้)</Label>
            <Textarea
              id="qna-question"
              data-testid="qna-answer-question-input"
              value={question}
              maxLength={QUESTION_MAX_LENGTH}
              rows={2}
              onChange={(e) => setQuestion(e.target.value)}
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="qna-answer">คำตอบที่ถูก</Label>
            <Textarea
              id="qna-answer"
              data-testid="qna-answer-answer-input"
              value={answer}
              maxLength={ANSWER_MAX_LENGTH}
              rows={4}
              onChange={(e) => setAnswer(e.target.value)}
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label>ใช้ตอบได้ในขอบเขต</Label>
            <RadioGroup
              value={scopeType}
              onValueChange={(value) => handleScopeTypeChange(value as KnowledgeScopeType)}
              className="flex flex-row gap-4"
            >
              <Label className="font-normal">
                <RadioGroupItem
                  value="lesson"
                  disabled={!primaryItem?.lessonId}
                  data-testid="qna-answer-scope-lesson-radio"
                />
                เฉพาะบทเรียนนี้
              </Label>
              <Label className="font-normal">
                <RadioGroupItem value="category" data-testid="qna-answer-scope-category-radio" />
                ทั้งหมวด
              </Label>
              <Label className="font-normal">
                <RadioGroupItem value="company" data-testid="qna-answer-scope-company-radio" />
                ทั้งบริษัท
              </Label>
            </RadioGroup>
            {scopeType === "category" && (
              <Select value={scopeId ?? ""} onValueChange={(value) => value && setScopeId(value)}>
                <SelectTrigger className="w-full" data-testid="qna-answer-category-select">
                  <SelectValue placeholder="เลือกหมวด">
                    {(value: string) => {
                      const sub = subcategories.find((c) => c.id === value);
                      return sub ? `${parentsById.get(sub.parentId ?? "")?.name ?? "?"} › ${sub.name}` : "เลือกหมวด";
                    }}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  {subcategories.map((sub) => (
                    <SelectItem key={sub.id} value={sub.id}>
                      {parentsById.get(sub.parentId ?? "")?.name ?? "?"} › {sub.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>

          {error && <p className="text-xs text-destructive">{error}</p>}

          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={onClose} disabled={saving} data-testid="qna-answer-cancel-button">
              ยกเลิก
            </Button>
            <Button onClick={handleSave} disabled={!canSave || saving} data-testid="qna-answer-save-button">
              {saving ? (
                <>
                  <Spinner data-icon="inline-start" />
                  กำลังบันทึก...
                </>
              ) : (
                "บันทึกคำตอบ"
              )}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
