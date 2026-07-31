import { describe, expect, it } from "vitest";
import { buildEmbedUrlFromPresentationId, isValidGoogleSlidesUrl, parseGoogleSlidesUrl } from "@/utils/google-slides-url";

describe("parseGoogleSlidesUrl", () => {
  it("extracts presentationId from a source (edit) URL", () => {
    const result = parseGoogleSlidesUrl("https://docs.google.com/presentation/d/1AbCdEfGhIj/edit#slide=id.p1");
    expect(result.presentationId).toBe("1AbCdEfGhIj");
    expect(result.isPublished).toBe(false);
  });

  it("recognizes published URLs and refuses to treat their id as a presentationId", () => {
    const result = parseGoogleSlidesUrl(
      "https://docs.google.com/presentation/d/e/2PACX-1vTestPublishedId/pub?start=false",
    );
    expect(result.presentationId).toBeNull();
    expect(result.isPublished).toBe(true);
  });

  it("returns nulls for an unrecognized URL shape", () => {
    const result = parseGoogleSlidesUrl("https://example.com/not-slides");
    expect(result.presentationId).toBeNull();
    expect(result.isPublished).toBe(false);
  });
});

describe("isValidGoogleSlidesUrl", () => {
  it("accepts a docs.google.com presentation URL", () => {
    expect(isValidGoogleSlidesUrl("https://docs.google.com/presentation/d/abc123/edit")).toBe(true);
  });

  it("rejects non-Google or non-presentation URLs", () => {
    expect(isValidGoogleSlidesUrl("https://example.com/presentation/d/abc123/edit")).toBe(false);
    expect(isValidGoogleSlidesUrl("https://docs.google.com/document/d/abc123/edit")).toBe(false);
    expect(isValidGoogleSlidesUrl("not a url")).toBe(false);
  });
});

describe("buildEmbedUrlFromPresentationId", () => {
  it("builds a stable embed URL", () => {
    expect(buildEmbedUrlFromPresentationId("abc123")).toBe(
      "https://docs.google.com/presentation/d/abc123/embed?start=false&loop=false&delayms=60000",
    );
  });
});
