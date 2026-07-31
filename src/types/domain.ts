export type MediaKind = "image" | "video";

export type DemoMedia = {
  id: string;
  kind: MediaKind;
  src: string;
  label: string;
};

export type QuestionScope = "IN_LESSON" | "SYSTEM_BASIC" | "OUT_OF_SCOPE" | "UNKNOWN";

export type Faq = {
  id: string;
  question: string;
  keywords: string[];
  answer: string;
  scope: QuestionScope;
  relatedMediaId?: string;
  active: boolean;
};

export type Segment = {
  id: string;
  order: number;
  scriptText: string;
  mediaId: string;
  mockSpeakDurationMs: number;
};

export type Step = {
  id: string;
  title: string;
  order: number;
  checkpointEnabled: boolean;
  checkpointPromptId: string;
  segments: Segment[];
};

export type Lesson = {
  id: string;
  code: string;
  title: string;
  language: string;
  steps: Step[];
  faqs: Faq[];
};

export type TrainingSessionStatus = "NOT_STARTED" | "IN_PROGRESS" | "ENDED" | "EXPIRED";

export type TrainingSession = {
  id: string;
  token: string;
  lessonSnapshot: Lesson;
  teacherName?: string;
  schoolName?: string;
  createdAt: string;
  expiresAt: string;
  startedAt?: string;
  endedAt?: string;
  disconnectedAt?: string;
  completedAllSteps: boolean;
  lastStepIndex: number;
  lastSegmentIndex: number;
};

export type CreateSessionInput = {
  teacherName?: string;
  schoolName?: string;
  expiresAt: string;
};

export type SummaryQuestion = {
  question: string;
  answer?: string;
  scope: QuestionScope;
  resolved: boolean;
};

export type SessionSummary = {
  sessionId: string;
  completedAllSteps: boolean;
  lastStepIndex: number;
  lastStepTitle?: string;
  questions: SummaryQuestion[];
  repeatedPoints: string[];
  unresolvedItems: string[];
  startedAt?: string;
  endedAt: string;
};

export type CheckpointPrompt = {
  id: string;
  text: string;
};
