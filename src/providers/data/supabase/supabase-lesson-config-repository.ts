import "server-only";
import type { LessonConfigRepository } from "@/providers/data/repository-types";
import type { LessonConfig, SlideConfig } from "@/types/domain";
import { getSupabaseServiceClient } from "@/providers/data/supabase/client";

type LessonRow = {
  id: string;
  slug: string;
  title: string;
  description: string | null;
  slides_source_url: string;
  presentation_id: string | null;
  slides_embed_url: string | null;
  intro_wait_ms: number;
  breath_pause_ms: number;
  final_question_wait_ms: number;
  is_active: boolean;
  created_at: string;
  updated_at: string;
};

type SlideConfigRow = {
  slide_object_id: string;
  slide_index: number;
  video_duration_ms: number | null;
};

function toSlideConfig(row: SlideConfigRow): SlideConfig {
  return { slideObjectId: row.slide_object_id, slideIndex: row.slide_index, videoDurationMs: row.video_duration_ms };
}

function toLessonConfig(row: LessonRow, slideRows: SlideConfigRow[]): LessonConfig {
  return {
    id: row.id,
    slug: row.slug,
    title: row.title,
    description: row.description ?? undefined,
    slidesSourceUrl: row.slides_source_url,
    presentationId: row.presentation_id,
    slidesEmbedUrl: row.slides_embed_url,
    introWaitMs: row.intro_wait_ms,
    breathPauseMs: row.breath_pause_ms,
    finalQuestionWaitMs: row.final_question_wait_ms,
    slideConfigs: slideRows.map(toSlideConfig).sort((a, b) => a.slideIndex - b.slideIndex),
    isActive: row.is_active,
    createdAt: row.created_at,
    updatedAt: row.updated_at,
  };
}

// PREPARED — CREDENTIALS REQUIRED. Not tested against a real Supabase project yet.
// Apply supabase/migrations/0001_initial_schema.sql before switching DATA_PROVIDER=supabase.
export class SupabaseLessonConfigRepository implements LessonConfigRepository {
  async list(): Promise<LessonConfig[]> {
    const supabase = getSupabaseServiceClient();
    const { data: lessons, error } = await supabase.from("lessons").select("*").order("slug");
    if (error) throw error;

    const results: LessonConfig[] = [];
    for (const lesson of (lessons ?? []) as LessonRow[]) {
      const { data: slideRows, error: slideError } = await supabase
        .from("lesson_slide_configs")
        .select("*")
        .eq("lesson_id", lesson.id);
      if (slideError) throw slideError;
      results.push(toLessonConfig(lesson, (slideRows ?? []) as SlideConfigRow[]));
    }
    return results;
  }

  async getBySlug(slug: string): Promise<LessonConfig | null> {
    const supabase = getSupabaseServiceClient();
    const { data: lesson, error } = await supabase.from("lessons").select("*").eq("slug", slug).maybeSingle();
    if (error) throw error;
    if (!lesson) return null;

    const lessonRow = lesson as LessonRow;
    const { data: slideRows, error: slideError } = await supabase
      .from("lesson_slide_configs")
      .select("*")
      .eq("lesson_id", lessonRow.id);
    if (slideError) throw slideError;
    return toLessonConfig(lessonRow, (slideRows ?? []) as SlideConfigRow[]);
  }

  async save(config: LessonConfig): Promise<LessonConfig> {
    const supabase = getSupabaseServiceClient();
    const { data: saved, error } = await supabase
      .from("lessons")
      .upsert(
        {
          id: config.id,
          slug: config.slug,
          title: config.title,
          description: config.description ?? null,
          slides_source_url: config.slidesSourceUrl,
          presentation_id: config.presentationId,
          slides_embed_url: config.slidesEmbedUrl,
          intro_wait_ms: config.introWaitMs,
          breath_pause_ms: config.breathPauseMs,
          final_question_wait_ms: config.finalQuestionWaitMs,
          is_active: config.isActive,
        },
        { onConflict: "id" },
      )
      .select()
      .single();
    if (error) throw error;

    const { error: deleteError } = await supabase.from("lesson_slide_configs").delete().eq("lesson_id", config.id);
    if (deleteError) throw deleteError;

    if (config.slideConfigs.length > 0) {
      const { error: insertError } = await supabase.from("lesson_slide_configs").insert(
        config.slideConfigs.map((slide) => ({
          lesson_id: config.id,
          slide_object_id: slide.slideObjectId,
          slide_index: slide.slideIndex,
          video_duration_ms: slide.videoDurationMs,
        })),
      );
      if (insertError) throw insertError;
    }

    return toLessonConfig(
      saved as LessonRow,
      config.slideConfigs.map((slide) => ({
        slide_object_id: slide.slideObjectId,
        slide_index: slide.slideIndex,
        video_duration_ms: slide.videoDurationMs,
      })),
    );
  }
}
