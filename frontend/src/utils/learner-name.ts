export const LEARNER_NAME_MAX_LENGTH = 80;

export function isValidLearnerName(name: string): boolean {
  const trimmedName = name.trim();
  return trimmedName.length > 0 && trimmedName.length <= LEARNER_NAME_MAX_LENGTH;
}
