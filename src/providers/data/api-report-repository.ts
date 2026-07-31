import type { SessionSummary } from "@/types/domain";
import type { ReportRepository } from "@/providers/data/repository-types";

// TODO(Phase 4): implement against a real backend once a database is chosen.
// Must satisfy the same ReportRepository contract so app code does not change.
export class ApiReportRepository implements ReportRepository {
  async getBySessionId(_sessionId: string): Promise<SessionSummary | null> {
    throw new Error("ApiReportRepository is not implemented in the mock phase.");
  }

  async save(_summary: SessionSummary): Promise<SessionSummary> {
    throw new Error("ApiReportRepository is not implemented in the mock phase.");
  }

  async reset(): Promise<void> {
    throw new Error("ApiReportRepository is not implemented in the mock phase.");
  }
}
