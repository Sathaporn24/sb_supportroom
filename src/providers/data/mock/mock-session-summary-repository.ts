import "server-only";
import type { SessionSummaryRepository } from "@/providers/data/repository-types";
import type { SessionSummary } from "@/types/domain";
import { getMockStore } from "@/providers/data/mock/store";

export class MockSessionSummaryRepository implements SessionSummaryRepository {
  async getBySessionId(sessionId: string): Promise<SessionSummary | null> {
    return getMockStore().sessionSummaries.get(sessionId) ?? null;
  }

  async save(summary: SessionSummary): Promise<SessionSummary> {
    getMockStore().sessionSummaries.set(summary.sessionId, summary);
    return summary;
  }
}
