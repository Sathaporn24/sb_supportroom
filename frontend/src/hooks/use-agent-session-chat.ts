"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { getApiBaseUrl, getChatMessagesByLearningSession } from "@/lib/api-client";
import type { ChatMessage, SessionQuestion } from "@/types/domain";

/**
 * The CS-side twin of useSessionChat.
 *
 * A support agent has no learnerKey - that key lives in the learner's browser - so this addresses
 * a learning session by id and talks to the hub's agent methods instead.
 *
 * ⚠️ Unauthenticated, like the rest of /admin today (TD-002). The learning session id is an
 * unguessable GUID, which is what stands in for access control until auth lands.
 */
export type ChatConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";

function mergeById<T extends { id: string; createdAt: string }>(existing: T[], incoming: T[]): T[] {
  const byId = new Map(existing.map((item) => [item.id, item]));
  for (const item of incoming) {
    byId.set(item.id, item);
  }
  return [...byId.values()].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
}

export function useAgentSessionChat(learningSessionId: string, agentName = "ทีมซัพพอร์ต") {
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([]);
  const [liveQuestions, setLiveQuestions] = useState<SessionQuestion[]>([]);
  const [connectionState, setConnectionState] = useState<ChatConnectionState>("connecting");
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!learningSessionId) {
      return;
    }
    let cancelled = false;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${getApiBaseUrl()}/hubs/session`, { withCredentials: false })
      .withAutomaticReconnect()
      .build();
    connectionRef.current = connection;

    connection.on("ReceiveChatMessage", (message: ChatMessage) => {
      setChatMessages((prev) => mergeById(prev, [message]));
    });
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

  // History hydration - separate from the live socket so an agent opening a session mid-lesson
  // still sees everything said before they connected.
  useEffect(() => {
    if (!learningSessionId) {
      return;
    }
    let cancelled = false;
    getChatMessagesByLearningSession(learningSessionId)
      .then(({ messages }) => {
        if (!cancelled) setChatMessages((prev) => mergeById(prev, messages));
      })
      .catch(() => {
        // Live chat still works without history - not fatal.
      });
    return () => {
      cancelled = true;
    };
  }, [learningSessionId]);

  const sendChatMessage = useCallback(
    async (text: string) => {
      const connection = connectionRef.current;
      if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        throw new Error("การเชื่อมต่อแชทยังไม่พร้อม กรุณาลองใหม่อีกครั้ง");
      }
      await connection.invoke("SendChatMessageAsAgent", learningSessionId, agentName, text);
    },
    [learningSessionId, agentName],
  );

  return { chatMessages, liveQuestions, connectionState, sendChatMessage };
}
