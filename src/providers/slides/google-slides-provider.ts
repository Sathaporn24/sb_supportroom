import "server-only";
import { google, type slides_v1 } from "googleapis";
import type {
  GetLessonContentInput,
  ResolvePresentationInput,
  ResolvedPresentation,
  SlidesContentProvider,
} from "@/providers/slides/types";
import type { SlidesLessonContent } from "@/types/domain";
import { getGoogleServiceAccountEnv } from "@/config/env";
import { buildEmbedUrlFromPresentationId, isValidGoogleSlidesUrl, parseGoogleSlidesUrl } from "@/utils/google-slides-url";

// PREPARED — CREDENTIALS REQUIRED. Not exercised against a real Google Cloud project
// yet. See docs/GOOGLE_SLIDES_SETUP.md for how to create the service account and share
// a deck with it. Do not report this provider as "connected" until it has been tested
// with a real presentationId.
function getAuthClient() {
  const { clientEmail, privateKey } = getGoogleServiceAccountEnv();
  return new google.auth.JWT({
    email: clientEmail,
    key: privateKey,
    scopes: ["https://www.googleapis.com/auth/presentations.readonly"],
  });
}

function extractSpeakerNotes(slide: slides_v1.Schema$Page): string {
  const notesElements = slide.slideProperties?.notesPage?.pageElements ?? [];
  for (const element of notesElements) {
    if (element.shape?.placeholder?.type !== "BODY") {
      continue;
    }
    const textElements = element.shape.text?.textElements ?? [];
    const text = textElements.map((run) => run.textRun?.content ?? "").join("");
    return text.trim();
  }
  return "";
}

export class GoogleSlidesContentProvider implements SlidesContentProvider {
  async resolvePresentation(input: ResolvePresentationInput): Promise<ResolvedPresentation> {
    if (!isValidGoogleSlidesUrl(input.slidesSourceUrl)) {
      return {
        presentationId: null,
        embedUrl: input.slidesEmbedUrl || "",
        isEmbedOnly: false,
        warning: "URL ที่วางไม่ใช่ลิงก์ Google Slides ที่ถูกต้อง กรุณาวางลิงก์จาก docs.google.com/presentation/...",
      };
    }

    const parsed = parseGoogleSlidesUrl(input.slidesSourceUrl);
    if (!parsed.presentationId) {
      return {
        presentationId: null,
        embedUrl: input.slidesEmbedUrl || input.slidesSourceUrl,
        isEmbedOnly: true,
        warning:
          "ไม่สามารถอ่าน presentationId จาก URL นี้ได้ (อาจเป็นลิงก์ Published) จะใช้แสดงผลได้อย่างเดียว " +
          "กรุณาวาง Source URL แบบ /presentation/d/<id>/edit เพื่ออ่าน Speaker Notes",
      };
    }

    const auth = getAuthClient();
    const slides = google.slides({ version: "v1", auth });
    // Fetching minimal metadata confirms the service account has Viewer access.
    await slides.presentations.get({ presentationId: parsed.presentationId, fields: "presentationId" });

    return {
      presentationId: parsed.presentationId,
      embedUrl: input.slidesEmbedUrl || buildEmbedUrlFromPresentationId(parsed.presentationId),
      isEmbedOnly: false,
    };
  }

  async getLessonContent(input: GetLessonContentInput): Promise<SlidesLessonContent> {
    const auth = getAuthClient();
    const slides = google.slides({ version: "v1", auth });
    const { data } = await slides.presentations.get({ presentationId: input.presentationId });
    const slideList = data.slides ?? [];

    return {
      presentationId: input.presentationId,
      title: data.title ?? "",
      embedUrl: buildEmbedUrlFromPresentationId(input.presentationId),
      slides: slideList.map((slide, index) => ({
        slideObjectId: slide.objectId ?? `slide-${index}`,
        index,
        speakerNotes: extractSpeakerNotes(slide),
      })),
      syncedAt: new Date().toISOString(),
    };
  }
}
