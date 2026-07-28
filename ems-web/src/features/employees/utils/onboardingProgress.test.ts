import { describe, expect, it } from "vitest";
import {
  computeOnboardingProgressSummary,
  formatOnboardingCategory,
  isOnboardingItemOverdue,
} from "./onboardingProgress";

describe("onboarding progress helpers", () => {
  it("formats checklist categories", () => {
    expect(formatOnboardingCategory(1)).toBe("Documents");
    expect(formatOnboardingCategory(5)).toBe("Manager intro");
  });

  it("computes percent complete with rounding", () => {
    const summary = computeOnboardingProgressSummary({
      totalCount: 3,
      completedCount: 1,
      overdueCount: 0,
    });

    expect(summary.percentComplete).toBe(33);
    expect(summary.hasChecklist).toBe(true);
    expect(summary.isComplete).toBe(false);
  });

  it("returns zero percent for empty checklist", () => {
    const summary = computeOnboardingProgressSummary({
      totalCount: 0,
      completedCount: 0,
      overdueCount: 0,
    });

    expect(summary.percentComplete).toBe(0);
    expect(summary.hasChecklist).toBe(false);
  });

  it("detects overdue incomplete items", () => {
    const now = new Date("2026-07-10T12:00:00.000Z");
    expect(isOnboardingItemOverdue(1, "2026-07-09T00:00:00.000Z", now)).toBe(true);
    expect(isOnboardingItemOverdue(4, "2026-07-09T00:00:00.000Z", now)).toBe(false);
    expect(isOnboardingItemOverdue(1, null, now)).toBe(false);
  });
});
