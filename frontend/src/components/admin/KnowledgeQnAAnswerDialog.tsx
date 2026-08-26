"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";
import { ApiClientError } from "@/lib/api-client";
import type {
  DuplicateQnAResponse,
  KnowledgeCategory,
  KnowledgeQnA,
  KnowledgeQnAQueueItem,
  KnowledgeScopeType,
  LessonConfig,
} from "@/types/domain";
import { scopeLabel } from "@/components/admin/DocumentUploadList";
import { formatDateTimeTh } from "@/utils/format";
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

type Props =
  | {
      mode: "create";
      open: boolean;
      /** The queue rows this Q&A will close (QQ-7 - one Q&A can close several at once). */
      items: KnowledgeQnAQueueItem[];
      onClose: () => void;
      onSaved: () => void;
    }
  | {
      mode: "edit";
      open: boolean;
      /** KL-14 - the row being edited. Scope is shown but not editable here: UpdateKnowledgeQnADto
       * only carries Question/Answer (R-19/R7.6 - editing that surface is explicitly off-limits
       * this phase), so a scope change still has to go through move/delete+recreate elsewhere. */
      qna: KnowledgeQnA;
      onClose: () => void;
      onSaved: () => void;
    };

/**
 * R5's core write path: CS writes the right answer directly at the point a gap was found. In
 * create mode, scope prefills to "lesson" of the first selected question (QQ-8) but is always
 * changeable before saving - there is no auto-save, the CS must press "บันทึกคำตอบ" (R5.7: usable
 * immediately once they do). In edit mode (KL-14) the same layout is reused with Question/Answer
 * editable and scope shown read-only, saving via `updateKnowledgeQnA`.
 *
 * KL-23/KL-26 (mati Q-H2, 2026-08-25) - Q&A duplicate detection is a pre-save gate, not a
 * post-save warning: `create` mode's save can 409 before anything is written. `duplicates` holds
 * that 409's payload (a list - KL-23 rule (6)) while the form's own state keeps whatever the CS
 * had typed so it can be resent unchanged with `confirmDuplicate: true`. "แก้ใบเดิมแทน" switches
 * this same dialog to edit the picked duplicate row in place (`editingDuplicate`) - no navigation,
 * no new fetch, using the row already present in the 409 payload.
 */
export function KnowledgeQnAAnswerDialog(props: Props) {
  const { mode, open, onClose, onSaved } = props;
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState("");
  const [scopeType, setScopeType] = useState<KnowledgeScopeType>("lesson");
  const [scopeId, setScopeId] = useState<string | undefined>(undefined);
  const [categories, setCategories] = useState<KnowledgeCategory[]>([]);
  const [lessons, setLessons] = useState<LessonConfig[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [duplicates, setDuplicates] = useState<KnowledgeQnA[] | null>(null);
  const [editingDuplicate, setEditingDuplicate] = useState<KnowledgeQnA | null>(null);

  const primaryItem = mode === "create" ? props.items[0] : undefined;

  useEffect(() => {
    if (!open) return;
    setError(null);
    setDuplicates(null);
    setEditingDuplicate(null);
    if (mode === "edit") {
      setQuestion(props.qna.question);
      setAnswer(props.qna.answer);
      setScopeType(props.qna.scopeType);
      setScopeId(props.qna.scopeId);
    } else {
      setQuestion(primaryItem?.transcript ?? "");
      setAnswer("");
      if (primaryItem?.lessonId) {
        setScopeType("lesson");
        setScopeId(primaryItem.lessonId);
      } else {
        setScopeType("company");
        setScopeId(undefined);
      }
    }
    void api.listKnowledgeCategories().then(({ categories: list }) => setCategories(list));
    void api.listLessons().then(({ lessons: list }) => setLessons(list));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, mode, mode === "edit" ? props.qna.id : primaryItem?.id]);

  function handleScopeTypeChange(next: KnowledgeScopeType) {
    setScopeType(next);
    setScopeId(next === "lesson" ? primaryItem?.lessonId : undefined);
  }

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      if (mode === "edit") {
        await api.updateKnowledgeQnA(props.qna.id, { question, answer });
        onSaved();
        return;
      }
      if (editingDuplicate) {
        // KL-26 - "แก้ใบเดิมแทน" saves through the same PUT KL-14 uses; the queue selection this
        // dialog opened with is left untouched (QQ-2/QQ-6 - editing never closes a queue question).
        await api.updateKnowledgeQnA(editingDuplicate.id, { question, answer });
        onSaved();
        return;
      }
      await api.createKnowledgeQnA({
        question,
        answer,
        scopeType,
        scopeId,
        sessionQuestionIds: props.items.map((item) => item.id),
      });
      onSaved();
    } catch (err) {
      // KL-23/KL-26 - a 409 here is a normal ApiClientError; the duplicate payload rides in
      // response.error.details, same envelope as the document-upload 409 (DocumentUploadList).
      if (mode === "create" && !editingDuplicate && err instanceof ApiClientError && err.status === 409) {
        const details = err.response.error.details as DuplicateQnAResponse;
        setDuplicates(details.duplicateByQuestion);
        return;
      }
      setError(err instanceof ApiClientError ? err.response.error.message : "บันทึกคำตอบไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  async function handleConfirmDuplicate() {
    if (mode !== "create") return;
    setSaving(true);
    setError(null);
    try {
      await api.createKnowledgeQnA({
        question,
        answer,
        scopeType,
        scopeId,
        sessionQuestionIds: props.items.map((item) => item.id),
        confirmDuplicate: true,
      });
      onSaved();
    } catch (err) {
      setError(err instanceof ApiClientError ? err.response.error.message : "บันทึกคำตอบไม่สำเร็จ");
    } finally {
      setSaving(false);
    }
  }

  function handleEditExistingInstead(existing: KnowledgeQnA) {
    setDuplicates(null);
    setEditingDuplicate(existing);
    setQuestion(existing.question);
    setAnswer(existing.answer);
    setScopeType(existing.scopeType);
    setScopeId(existing.scopeId);
  }

  const subcategories = categories
    .filter((c) => c.level === 2)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "th"));
  const parentsById = new Map(categories.filter((c) => c.level === 1).map((c) => [c.id, c]));

  const canSave =
    question.trim().length > 0 &&
    answer.trim().length > 0 &&
    (scopeType !== "category" || Boolean(scopeId)) &&
    (mode === "edit" || editingDuplicate || props.items.length > 0);

  const scopeEditable = mode === "create" && !editingDuplicate;

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-lg" data-testid="qna-answer-dialog">
        <DialogHeader>
          <DialogTitle>
            {mode === "edit" || editingDuplicate
              ? "แก้ไขคำถาม-คำตอบ"
              : `เขียนคำตอบ (${props.items.length} คำถามที่เลือก)`}
          </DialogTitle>
        </DialogHeader>

        {duplicates ? (
          <div className="flex flex-col gap-4" data-testid="qna-answer-duplicate-warning">
            <p className="text-sm">
              มีคำถามที่เหมือนกันเป๊ะอยู่แล้วในระบบ — <strong>ยังไม่ได้บันทึก</strong>
            </p>
            <ul className="flex flex-col gap-2">
              {duplicates.map((dup) => (
                <li
                  key={dup.id}
                  className="rounded-lg border p-2 text-sm"
                  data-testid={`qna-answer-duplicate-${dup.id}`}
                >
                  <p className="font-medium">{dup.question}</p>
                  <p className="mt-1 whitespace-pre-wrap text-muted-foreground">{dup.answer}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {scopeLabel(dup.scopeType, dup.scopeId, categories, lessons)} · บันทึกเมื่อ{" "}
                    {formatDateTimeTh(dup.createdAt)}
                  </p>
                  <div className="mt-2 flex justify-end">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleEditExistingInstead(dup)}
                      data-testid={`qna-answer-duplicate-${dup.id}-edit-existing-button`}
                    >
                      แก้ใบเดิมแทน
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
            {error && <p className="text-xs text-destructive">{error}</p>}
            <div className="flex justify-end gap-2">
              <Button variant="ghost" onClick={onClose} disabled={saving} data-testid="qna-answer-duplicate-cancel-button">
                ยกเลิก
              </Button>
              <Button
                onClick={handleConfirmDuplicate}
                disabled={saving}
                data-testid="qna-answer-duplicate-confirm-button"
              >
                {saving ? (
                  <>
                    <Spinner data-icon="inline-start" />
                    กำลังบันทึก...
                  </>
                ) : (
                  "ยืนยันบันทึกซ้ำ"
                )}
              </Button>
            </div>
          </div>
        ) : (
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
                disabled={!scopeEditable}
              >
                <Label className="font-normal">
                  <RadioGroupItem
                    value="lesson"
                    disabled={scopeEditable ? !primaryItem?.lessonId : false}
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
                <Select
                  value={scopeId ?? ""}
                  onValueChange={(value) => value && setScopeId(value)}
                  disabled={!scopeEditable}
                >
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
              {!scopeEditable && (
                <p className="text-xs text-muted-foreground">ไม่สามารถเปลี่ยนขอบเขตจากการแก้ไขได้</p>
              )}
            </div>

            {editingDuplicate && (
              <p className="text-xs text-muted-foreground" data-testid="qna-answer-edit-existing-caveat">
                กำลังแก้ไขคำถาม-คำตอบที่มีอยู่แล้ว — คำถามในคิวที่เลือกไว้ก่อนหน้านี้จะยังไม่ถูกปิดและยังอยู่ในคิวเหมือนเดิม
              </p>
            )}

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
        )}
      </DialogContent>
    </Dialog>
  );
}
