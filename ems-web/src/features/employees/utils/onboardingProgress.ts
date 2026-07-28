import type { OnboardingChecklistItemCategoryValue } from "@/features/onboarding/types/onboarding.types";

export const onboardingCategoryLabels: Record<OnboardingChecklistItemCategoryValue, string> = {
  1: "Documents",
  2: "Equipment",
  3: "Account setup",
  4: "Induction",
  5: "Manager intro",
};

export function formatOnboardingCategory(category: OnboardingChecklistItemCategoryValue): string {
  return onboardingCategoryLabels[category] ?? "Other";
}

export type OnboardingProgressSummaryInput = {
  totalCount: number;
  completedCount: number;
  overdueCount: number;
};

export function computeOnboardingProgressSummary(input: OnboardingProgressSummaryInput) {
  const { totalCount, completedCount, overdueCount } = input;
  const percentComplete =
    totalCount === 0 ? 0 : Math.round((completedCount * 100) / totalCount);

  return {
    totalCount,
    completedCount,
    overdueCount,
    percentComplete,
    hasChecklist: totalCount > 0,
    isComplete: totalCount > 0 && completedCount === totalCount,
  };
}

export function isOnboardingItemOverdue(
  status: number | null,
  dueAtUtc: string | null,
  now = new Date(),
): boolean {
  if (status === 4 || !dueAtUtc) return false;
  return new Date(dueAtUtc) < now;
}
