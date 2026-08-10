export function generateId(prefix: string): string {
  return `${prefix}-${crypto.randomUUID()}`;
}

export function generatePublicToken(): string {
  return crypto.randomUUID();
}
