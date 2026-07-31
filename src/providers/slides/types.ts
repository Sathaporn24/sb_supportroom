import type { SlidesLessonContent } from "@/types/domain";

export type ResolvePresentationInput = {
  slidesSourceUrl: string;
  slidesEmbedUrl?: string;
};

export type ResolvedPresentation = {
  presentationId: string | null;
  embedUrl: string;
  /** True when presentationId could not be derived and only display is possible. */
  isEmbedOnly: boolean;
  warning?: string;
};

export type GetLessonContentInput = {
  presentationId: string;
};

export interface SlidesContentProvider {
  resolvePresentation(input: ResolvePresentationInput): Promise<ResolvedPresentation>;
  getLessonContent(input: GetLessonContentInput): Promise<SlidesLessonContent>;
}
