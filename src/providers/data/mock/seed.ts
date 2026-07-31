import "server-only";
import type { LessonConfig } from "@/types/domain";
import { MOCK_PRESENTATION_ID } from "@/providers/slides/mock-slides-provider";
import { getLessonTimingDefaults } from "@/config/server-defaults";

// Only "login-mobile" has a real mock deck (see mock-slides-provider.ts). The other two
// mirror the topics picked out in the admin UI - inactive until real content is added,
// exactly like MockSlidesContentProvider's single deck.
export function seedLessonConfigs(): LessonConfig[] {
  const now = new Date().toISOString();
  const timing = getLessonTimingDefaults();

  const base = {
    description: undefined as string | undefined,
    slidesEmbedUrl: null as string | null,
    ...timing,
    createdAt: now,
    updatedAt: now,
  };

  return [
    {
      ...base,
      id: "lesson-login-mobile",
      slug: "login-mobile",
      title: "วิธีการ Login (mobile)",
      description: "สอนขั้นตอนเข้าสู่ระบบผ่านแอปพลิเคชันมือถือ",
      slidesSourceUrl: `https://docs.google.com/presentation/d/${MOCK_PRESENTATION_ID}/edit`,
      presentationId: MOCK_PRESENTATION_ID,
      slideConfigs: [
        { slideObjectId: "slide-1", slideIndex: 0, videoDurationMs: null },
        { slideObjectId: "slide-2", slideIndex: 1, videoDurationMs: null },
        { slideObjectId: "slide-3", slideIndex: 2, videoDurationMs: null },
        { slideObjectId: "slide-4", slideIndex: 3, videoDurationMs: null },
        { slideObjectId: "slide-5", slideIndex: 4, videoDurationMs: null },
        { slideObjectId: "slide-6", slideIndex: 5, videoDurationMs: null },
      ],
      isActive: true,
    },
    {
      ...base,
      id: "lesson-login-web",
      slug: "login-web",
      title: "วิธีการ Login (Web)",
      slidesSourceUrl: "",
      presentationId: null,
      slideConfigs: [],
      isActive: false,
    },
    {
      ...base,
      id: "lesson-forgot-password",
      slug: "forgot-password",
      title: "ลืมรหัสผ่าน",
      slidesSourceUrl: "",
      presentationId: null,
      slideConfigs: [],
      isActive: false,
    },
  ];
}
