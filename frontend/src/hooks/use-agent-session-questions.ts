"use client";

import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { getApiBaseUrl } from "@/lib/api-client";
import { getAccessToken, getActiveCompanyId } from "@/lib/auth-session";
import type { SessionQuestion } from "@/types/domain";

/**
 * CS-side live feed of Push-to-Talk questions as they come in, so a session already in progress
 * updates on the review page without a manual refresh.
 *
 * A support agent has no learnerKey - that key lives in the learner's browser - so this addresses
 * a learning session by id and talks to the hub's agent methods instead.
 *
 * Agent hub calls carry the same JWT and company context as REST. The hub stays anonymous at the
 * transport level because learners have no account; its agent methods enforce authentication.
 */
export type QuestionConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";

function mergeById<T extends { id: string; createdAt: string }>(existing: T[], incoming: T[]): T[] {
  const byId = new Map(existing.map((item) => [item.id, item]));
  for (const item of incoming) {
    byId.set(item.id, item);
  }
  return [...byId.values()].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
}

export function useAgentSessionQuestions(learningSessionId: string) {
  const [liveQuestions, setLiveQuestions] = useState<SessionQuestion[]>([]);
  const [connectionState, setConnectionState] = useState<QuestionConnectionState>("connecting");
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!learningSessionId) {
      return;
    }
    let cancelled = false;

    const companyId = getActiveCompanyId();
    const hubUrl = `${getApiBaseUrl()}/hubs/session${companyId ? `?company=${encodeURIComponent(companyId)}` : ""}`;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        withCredentials: false,
        accessTokenFactory: () => getAccessToken() ?? "",
      })
      .withAutomaticReconnect()
      .build();
    connectionRef.current = connection;

    connection.on("ReceiveNewQuestion", (question: SessionQuestion) => {
      setLiveQuestions((prev) => mergeById(prev, [question]));
    });

    connection.onreconnecting(() => setConnectionState("reconnecting"));
    connection.onreconnected(() => {
      setConnectionState("connected");
      void connection.invoke("JoinSessionAsAgent", learningSessionId).catch(() => {});
    });
    connection.onclose(() => setConnectionState("disconnected"));

    connection
      .start()
      .then(() => {
        if (cancelled) return;
        setConnectionState("connected");
        return connection.invoke("JoinSessionAsAgent", learningSessionId);
      })
      .catch(() => {
        if (!cancelled) setConnectionState("disconnected");
      });

    return () => {
      cancelled = true;
      connectionRef.current = null;
      void connection.stop();
    };
  }, [learningSessionId]);

  return { liveQuestions, connectionState };
}
