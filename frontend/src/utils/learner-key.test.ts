import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  consumeRoomEntry,
  getOrCreateLearnerKey,
  grantRoomEntry,
  peekLearnerKey,
} from "@/utils/learner-key";

const GENERATED_KEY = "11111111-2222-4333-8444-555555555555";

class MemoryStorage {
  private readonly values = new Map<string, string>();

  get length(): number {
    return this.values.size;
  }

  clear(): void {
    this.values.clear();
  }

  getItem(key: string): string | null {
    return this.values.get(key) ?? null;
  }

  key(index: number): string | null {
    return [...this.values.keys()][index] ?? null;
  }

  removeItem(key: string): void {
    this.values.delete(key);
  }

  setItem(key: string, value: string): void {
    this.values.set(key, value);
  }
}

describe("learner key", () => {
  let localStorage: MemoryStorage;
  let sessionStorage: MemoryStorage;

  beforeEach(() => {
    localStorage = new MemoryStorage();
    sessionStorage = new MemoryStorage();
    vi.stubGlobal("window", { localStorage, sessionStorage });
    vi.stubGlobal("crypto", { randomUUID: vi.fn(() => GENERATED_KEY) });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("creates one cryptographic browser-wide key and reuses it", () => {
    expect(peekLearnerKey()).toBeNull();

    expect(getOrCreateLearnerKey()).toBe(GENERATED_KEY);
    expect(getOrCreateLearnerKey()).toBe(GENERATED_KEY);
    expect(peekLearnerKey()).toBe(GENERATED_KEY);
    expect(localStorage.getItem("supportroom.learnerKey")).toBe(GENERATED_KEY);
    expect(crypto.randomUUID).toHaveBeenCalledTimes(1);
  });

  it("does not fall back to a predictable key when randomUUID is unavailable", () => {
    vi.stubGlobal("crypto", {});

    expect(() => getOrCreateLearnerKey()).toThrow("กรุณาเปิดผ่าน HTTPS");
    expect(peekLearnerKey()).toBeNull();
  });

  it("consumes a room-entry grant only once", () => {
    grantRoomEntry("token-a");

    expect(consumeRoomEntry("token-a")).toBe(true);
    expect(consumeRoomEntry("token-a")).toBe(false);
  });
});
