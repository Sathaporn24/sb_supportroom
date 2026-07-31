import type { SessionSummary } from "@/types/domain";
import { readJson, writeJson } from "@/utils/storage";
import { STORAGE_KEYS, type ReportRepository } from "@/providers/data/repository-types";

function readAll(): Record<string, SessionSummary> {
  return readJson<Record<string, SessionSummary>>(STORAGE_KEYS.reports, {});
}

function writeAll(reports: Record<string, SessionSummary>): void {
  writeJson(STORAGE_KEYS.reports, reports);
}

export class LocalStorageReportRepository implements ReportRepository {
  async getBySessionId(sessionId: string): Promise<SessionSummary | null> {
    return readAll()[sessionId] ?? null;
  }

  async save(summary: SessionSummary): Promise<SessionSummary> {
    const all = readAll();
    all[summary.sessionId] = summary;
    writeAll(all);
    return summary;
  }

  async reset(): Promise<void> {
    writeAll({});
  }
}
