import type {
  GetLessonContentInput,
  ResolvePresentationInput,
  ResolvedPresentation,
  SlidesContentProvider,
} from "@/providers/slides/types";
import type { SlidesLessonContent } from "@/types/domain";
import { isValidGoogleSlidesUrl, parseGoogleSlidesUrl } from "@/utils/google-slides-url";

// A fixed mock deck so the demo can be driven end to end with no Google credentials.
// Speaker notes are plain text (no [WAIT]/[CHECKPOINT] syntax), matching the rule that
// CS never writes commands into Slides notes.
const MOCK_DECKS: Record<string, { title: string; slides: SlidesLessonContent["slides"] }> = {
  "mock-login-mobile": {
    title: "วิธีการ Login (mobile)",
    slides: [
      {
        slideObjectId: "slide-1",
        index: 0,
        speakerNotes:
          "ตอนนี้คุณครูอยู่ที่หน้าเข้าสู่ระบบของแอปพลิเคชันนะคะ ก่อนเริ่มใช้งาน ให้ตรวจสอบว่าเปิดแอปเวอร์ชันล่าสุดอยู่ค่ะ",
      },
      {
        slideObjectId: "slide-2",
        index: 1,
        speakerNotes: "ด้านบนของหน้าจอจะเป็นส่วนสำหรับเลือกโรงเรียนของคุณครูค่ะ แตะเพื่อค้นหาชื่อโรงเรียนได้เลย",
      },
      {
        slideObjectId: "slide-3",
        index: 2,
        speakerNotes: "ช่องถัดไปใช้สำหรับกรอกชื่อผู้ใช้งานที่โรงเรียนกำหนดให้ อาจเป็นชื่อผู้ใช้งานหรือเบอร์โทรศัพท์ก็ได้ค่ะ",
      },
      {
        slideObjectId: "slide-4",
        index: 3,
        speakerNotes: "จากนั้นกรอกรหัสผ่านในช่องถัดไป โดยตรวจสอบตัวพิมพ์เล็กตัวพิมพ์ใหญ่ให้ถูกต้องค่ะ",
      },
      {
        slideObjectId: "slide-5",
        index: 4,
        speakerNotes:
          "เมื่อข้อมูลครบแล้ว แตะปุ่มเข้าสู่ระบบได้เลยค่ะ หากเข้าสู่ระบบไม่สำเร็จ ให้ตรวจสอบข้อมูลอีกครั้ง หรือติดต่อผู้ดูแลระบบของโรงเรียนค่ะ",
      },
      {
        slideObjectId: "slide-6",
        index: 5,
        speakerNotes: "สรุปแล้ว คุณครูเลือกโรงเรียน กรอกชื่อผู้ใช้งาน กรอกรหัสผ่าน แล้วแตะเข้าสู่ระบบ เป็นอันเสร็จค่ะ",
      },
    ],
  },
};

export const MOCK_PRESENTATION_ID = "mock-login-mobile";

export class MockSlidesContentProvider implements SlidesContentProvider {
  async resolvePresentation(input: ResolvePresentationInput): Promise<ResolvedPresentation> {
    if (!input.slidesSourceUrl) {
      return {
        presentationId: MOCK_PRESENTATION_ID,
        embedUrl: input.slidesEmbedUrl || "",
        isEmbedOnly: false,
        warning: "ไม่มี Source URL - ใช้เนื้อหาจำลองแทน (Mock Mode)",
      };
    }
    if (!isValidGoogleSlidesUrl(input.slidesSourceUrl)) {
      return {
        presentationId: MOCK_PRESENTATION_ID,
        embedUrl: input.slidesEmbedUrl || "",
        isEmbedOnly: false,
        warning: "URL ที่วางไม่ใช่ลิงก์ Google Slides ที่ถูกต้อง - ใช้เนื้อหาจำลองแทน (Mock Mode)",
      };
    }
    const parsed = parseGoogleSlidesUrl(input.slidesSourceUrl);
    return {
      presentationId: MOCK_PRESENTATION_ID,
      // Never echo slidesSourceUrl back as the embed target: it's almost certainly not
      // a real presentation (Mock Mode serves a fixed fake deck regardless of the URL
      // typed in), so treating it as embeddable would point the iframe at a real
      // docs.google.com URL that 404s instead of showing the Mock placeholder.
      embedUrl: input.slidesEmbedUrl || "",
      isEmbedOnly: parsed.isPublished,
      warning: "Mock Mode: ไม่ได้เรียก Google Slides API จริง เนื้อหาที่แสดงเป็นสไลด์จำลอง",
    };
  }

  async getLessonContent(input: GetLessonContentInput): Promise<SlidesLessonContent> {
    const deck = MOCK_DECKS[input.presentationId] ?? MOCK_DECKS[MOCK_PRESENTATION_ID];
    return {
      presentationId: input.presentationId,
      title: deck.title,
      embedUrl: "",
      slides: deck.slides,
      syncedAt: new Date().toISOString(),
    };
  }
}
