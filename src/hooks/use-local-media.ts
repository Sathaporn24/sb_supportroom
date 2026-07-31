"use client";

import { useCallback, useEffect, useRef, useState } from "react";

export type MediaPermissionError = "denied" | "not-found" | "unsupported" | "unknown" | null;

export function useLocalMedia() {
  const [stream, setStream] = useState<MediaStream | null>(null);
  const [cameraOn, setCameraOn] = useState(false);
  const [micOn, setMicOn] = useState(true);
  const [error, setError] = useState<MediaPermissionError>(null);
  const [micLevel, setMicLevel] = useState(0);
  const audioContextRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const rafRef = useRef<number | null>(null);

  const stopStream = useCallback(() => {
    stream?.getTracks().forEach((track) => track.stop());
    setStream(null);
    if (rafRef.current) {
      cancelAnimationFrame(rafRef.current);
      rafRef.current = null;
    }
    audioContextRef.current?.close().catch(() => undefined);
    audioContextRef.current = null;
    setMicLevel(0);
  }, [stream]);

  const requestMedia = useCallback(async (withCamera: boolean, withMic: boolean) => {
    if (typeof navigator === "undefined" || !navigator.mediaDevices?.getUserMedia) {
      setError("unsupported");
      return;
    }
    try {
      const nextStream = await navigator.mediaDevices.getUserMedia({
        video: withCamera,
        audio: withMic,
      });
      setStream((prev) => {
        prev?.getTracks().forEach((track) => track.stop());
        return nextStream;
      });
      setCameraOn(withCamera && nextStream.getVideoTracks().length > 0);
      setMicOn(withMic && nextStream.getAudioTracks().length > 0);
      setError(null);

      if (withMic && nextStream.getAudioTracks().length > 0) {
        const AudioContextCtor = window.AudioContext ?? (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
        const audioContext = new AudioContextCtor();
        const source = audioContext.createMediaStreamSource(nextStream);
        const analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        source.connect(analyser);
        audioContextRef.current = audioContext;
        analyserRef.current = analyser;

        const buffer = new Uint8Array(analyser.frequencyBinCount);
        const tick = () => {
          analyser.getByteFrequencyData(buffer);
          const avg = buffer.reduce((sum, v) => sum + v, 0) / buffer.length;
          setMicLevel(Math.min(1, avg / 90));
          rafRef.current = requestAnimationFrame(tick);
        };
        tick();
      }
    } catch (err) {
      const domError = err as DOMException;
      if (domError.name === "NotAllowedError" || domError.name === "SecurityError") {
        setError("denied");
      } else if (domError.name === "NotFoundError") {
        setError("not-found");
      } else {
        setError("unknown");
      }
    }
  }, []);

  const toggleCamera = useCallback(async () => {
    if (cameraOn) {
      stream?.getVideoTracks().forEach((track) => track.stop());
      setCameraOn(false);
      return;
    }
    await requestMedia(true, micOn);
  }, [cameraOn, micOn, requestMedia, stream]);

  const toggleMic = useCallback(() => {
    if (!stream) {
      setMicOn((prev) => !prev);
      return;
    }
    const next = !micOn;
    stream.getAudioTracks().forEach((track) => {
      track.enabled = next;
    });
    setMicOn(next);
  }, [micOn, stream]);

  useEffect(() => {
    return () => {
      stream?.getTracks().forEach((track) => track.stop());
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
      }
      audioContextRef.current?.close().catch(() => undefined);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return { stream, cameraOn, micOn, error, micLevel, requestMedia, toggleCamera, toggleMic, stopStream };
}
