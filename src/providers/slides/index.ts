import "server-only";
import { MockSlidesContentProvider } from "@/providers/slides/mock-slides-provider";
import { GoogleSlidesContentProvider } from "@/providers/slides/google-slides-provider";
import type { SlidesContentProvider } from "@/providers/slides/types";
import { getProviderSelection } from "@/config/env";

export function createSlidesContentProvider(): SlidesContentProvider {
  const { SLIDES_PROVIDER } = getProviderSelection();
  switch (SLIDES_PROVIDER) {
    case "google":
      return new GoogleSlidesContentProvider();
    case "mock":
    default:
      return new MockSlidesContentProvider();
  }
}
