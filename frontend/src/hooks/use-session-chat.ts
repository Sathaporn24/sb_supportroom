"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { getApiBaseUrl, getChatMessages } from "@/lib/api-client";
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

// sessionId is no longer a parameter: history hydration keys on the token too now, so the token
// is the only identifier this hook needs.
export function useSessionChat(
  token: string,
  senderRole: ChatSenderRole,
  senderName?: string,
) {
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([]);
  const [liveQuestions, setLiveQuestions] = useState<SessionQuestion[]>([]);
  const [connectionState, setConnectionState] = useState<ChatConnectionState>("connecting");
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!token) {
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
      void connection.invoke("JoinSession", token).catch(() => {});
    });
    connection.onclose(() => setConnectionState("disconnected"));

    // Kept so teardown can wait for start() to settle before stopping. Calling stop() while
    // negotiate is still in flight makes SignalR log "The connection was stopped during
    // negotiation" - which React StrictMode provokes on every mount in dev, and which Next's
    // error overlay then shows full-screen on top of the room.
    const started = connection
      .start()
      .then(() => {
        if (cancelled) {
          return;
        }
        setConnectionState("connected");
        return connection.invoke("JoinSession", token);
      })
      .catch(() => {
        if (!cancelled) {
          setConnectionState("disconnected");
        }
      });

    return () => {
      cancelled = true;
      connectionRef.current = null;
      // `started` never rejects (it ends in .catch), so this always runs; stop() on an
      // already-failed connection is a no-op.
      void started.then(() => connection.stop());
    };
  }, [token]);

  // History hydration - separate from the live socket so a CS agent joining mid-session (or a
  // reconnect) still sees everything said before they connected.
  useEffect(() => {
    if (!token) {
      return;
    }
    let cancelled = false;
    getChatMessages(token)
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
  }, [token]);

  const sendChatMessage = useCallback(
    async (text: string) => {
      const connection = connectionRef.current;
      if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        throw new Error("การเชื่อมต่อแชทยังไม่พร้อม กรุณาลองใหม่อีกครั้ง");
      }
      await connection.invoke("SendChatMessage", token, senderRole, senderName ?? null, text);
    },
    [token, senderRole, senderName],
  );

  return { chatMessages, liveQuestions, connectionState, sendChatMessage };
}
