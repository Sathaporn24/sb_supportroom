export type ParsedSlidesUrl = {
  /** Only present for "source" edit URLs - this is what the Slides API needs. */
  presentationId: string | null;
  /** True for /presentation/d/e/<publishedId>/... URLs, whose ID cannot be used with the API. */
  isPublished: boolean;
};

const SOURCE_ID_PATTERN = /\/presentation\/d\/([a-zA-Z0-9_-]+)/;
const PUBLISHED_ID_PATTERN = /\/presentation\/d\/e\/([a-zA-Z0-9_-]+)/;

export function isValidGoogleSlidesUrl(url: string): boolean {
  try {
    const parsed = new URL(url);
    return parsed.hostname === "docs.google.com" && parsed.pathname.includes("/presentation/");
  } catch {
    return false;
  }
}

/**
 * Source ("edit") URLs and published ("pub"/"embed") URLs encode different, mutually
 * incompatible identifiers - a published ID cannot be used with the Google Slides API.
 * This is why LessonConfig stores slidesSourceUrl and slidesEmbedUrl separately.
 */
export function parseGoogleSlidesUrl(url: string): ParsedSlidesUrl {
  const publishedMatch = url.match(PUBLISHED_ID_PATTERN);
  if (publishedMatch) {
    return { presentationId: null, isPublished: true };
  }
  const sourceMatch = url.match(SOURCE_ID_PATTERN);
  if (sourceMatch) {
    return { presentationId: sourceMatch[1], isPublished: false };
  }
  return { presentationId: null, isPublished: false };
}

export function buildEmbedUrlFromPresentationId(presentationId: string): string {
  return `https://docs.google.com/presentation/d/${presentationId}/embed?start=false&loop=false&delayms=60000`;
}
