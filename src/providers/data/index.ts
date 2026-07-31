import { LocalStorageLessonRepository } from "@/providers/data/local-storage-lesson-repository";
import { LocalStorageSessionRepository } from "@/providers/data/local-storage-session-repository";
import { LocalStorageReportRepository } from "@/providers/data/local-storage-report-repository";
import type { LessonRepository, ReportRepository, SessionRepository } from "@/providers/data/repository-types";

// Phase 1 only wires the local-storage implementations. Swapping NEXT_PUBLIC_DATA_PROVIDER
// to a future value should only require adding a case here (e.g. Api* repositories) -
// UI and tutor code must keep depending on the interfaces, never on this factory's internals.
export const lessonRepository: LessonRepository = new LocalStorageLessonRepository();
export const sessionRepository: SessionRepository = new LocalStorageSessionRepository();
export const reportRepository: ReportRepository = new LocalStorageReportRepository();

export async function resetAllDemoData(): Promise<void> {
  await lessonRepository.resetLoginLesson();
  await sessionRepository.reset();
  await reportRepository.reset();
}
