"use client";

import { useEffect, useState } from "react";
import * as api from "@/lib/api-client";

export type AdminReviewCounts = {
  /** Rows in the Q&A review queue - QQ-1/QQ-4 (getQnaQueue). */
  queueCount: number;
  /** Open Q&A-vs-document conflict flags - QQ-10 (listQnaConflicts). */
  conflictCount: number;
  loading: boolean;
};

/**
 * Shared by the sidebar badges and the dashboard stat pills so both read the same numbers - there
 * is no dedicated count endpoint, so this uses the length of the same lists those screens render.
 */
export function useAdminReviewCounts(): AdminReviewCounts {
  const [queueCount, setQueueCount] = useState(0);
  const [conflictCount, setConflictCount] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    void Promise.all([api.getQnaQueue(), api.listQnaConflicts()])
      .then(([{ queue }, { conflicts }]) => {
        if (cancelled) return;
        setQueueCount(queue.length);
        setConflictCount(conflicts.length);
      })
      .catch(() => {
        if (cancelled) return;
        setQueueCount(0);
        setConflictCount(0);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return { queueCount, conflictCount, loading };
}
