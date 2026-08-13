"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { getApiBaseUrl, getOwnChatMessages } from "@/lib/api-client";
import type { ChatMessage, ChatSenderRole, SessionQuestion } from "@/types/domain";

// Owns the HubConnection (browser API) - per architecture rule 3 this lives in a hook, never
// in src/tutor/. Used by both the room and the CS admin session page: one hook
// instance per participant, senderRole fixed at call time so callers just pass text.

export type ChatConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";

function mergeById<T extends { id: string }>(existing: T[], incoming: T[]): T[] {
  const byId = new Map(existing.map((item) => [item.id, item]));
  for (const item of incoming) {
    byId.set(item.id, item);
  }
  return [...byId.values()].sort((a, b) =>
    (a as unknown as { createdAt: string }).createdAt.localeCompare((b as unknown as { createdAt: string }).createdAt),
  );
}

/**
 * Keyed on (token, learnerKey), never the token alone. The SignalR group is one learning session,
 * not one link - a token-keyed group would put every learner who holds the same link in the same
 * room and fan each person's chat and questions out to all of them.
 */
export function useSessionChat(
  token: string,
  learnerKey: string,
  senderRole: ChatSenderRole,
  senderName?: string,
) {
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([]);
  const [liveQuestions, setLiveQuestions] = useState<SessionQuestion[]>([]);
  const [connectionState, setConnectionState] = useState<ChatConnectionState>("connecting");
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!token || !learnerKey) {
      return;
    }
    let cancelled = false;

    // withCredentials:false sidesteps SignalR's negotiate-wants-credentials default - the app
    // has no cookies/auth, so there's nothing to send, and it avoids needing AllowCredentials()
    // on a CORS policy that already lists explicit origins.
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
      void connection.invoke("JoinSession", token, learnerKey).catch(() => {});
    });
    connection.onclose(() => setConnectionState("disconnected"));

    connection
      .start()
      .then(() => {
        if (cancelled) {
          return;
        }
        setConnectionState("connected");
        return connection.invoke("JoinSession", token, learnerKey);
      })
      .catch(() => {
        if (!cancelled) {
          setConnectionState("disconnected");
        }
      });

    return () => {
      cancelled = true;
      connectionRef.current = null;
      void connection.stop();
    };
  }, [token, learnerKey]);

  // History hydration - separate from the live socket so a CS agent joining mid-session (or a
  // reconnect) still sees everything said before they connected.
  useEffect(() => {
    if (!token || !learnerKey) {
      return;
    }
    let cancelled = false;
    getOwnChatMessages(token, learnerKey)
      .then(({ messages }) => {
        if (!cancelled) {
          setChatMessages((prev) => mergeById(prev, messages));
        }
      })
      .catch(() => {
        // Live chat still works without history - not fatal.
      });
    return () => {
      cancelled = true;
    };
  }, [token, learnerKey]);

  const sendChatMessage = useCallback(
    async (text: string) => {
      const connection = connectionRef.current;
      if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        throw new Error("การเชื่อมต่อแชทยังไม่พร้อม กรุณาลองใหม่อีกครั้ง");
      }
      await connection.invoke("SendChatMessage", token, learnerKey, senderRole, senderName ?? null, text);
    },
    [token, learnerKey, senderRole, senderName],
  );

  return { chatMessages, liveQuestions, connectionState, sendChatMessage };
}
