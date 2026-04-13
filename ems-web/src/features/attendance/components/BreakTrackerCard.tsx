"use client";

import { Button } from "@/shared/components/Button";

type BreakTrackerCardProps = {
  hasOpenSession: boolean;
  hasOpenBreak: boolean;
  pending: boolean;
  onStartBreak: () => void;
  onEndBreak: () => void;
};

export function BreakTrackerCard({
  hasOpenSession,
  hasOpenBreak,
  pending,
  onStartBreak,
  onEndBreak,
}: BreakTrackerCardProps) {
  return (
    <div className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-700 dark:bg-zinc-950">
      <h2 className="text-base font-semibold text-zinc-900 dark:text-zinc-50">Break tracker</h2>
      <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
        {hasOpenBreak ? "Break is active." : "No active break."}
      </p>
      <div className="mt-4 flex gap-2">
        <Button type="button" variant="secondary" onClick={onStartBreak} disabled={!hasOpenSession || hasOpenBreak || pending}>
          Start break
        </Button>
        <Button type="button" variant="secondary" onClick={onEndBreak} disabled={!hasOpenBreak || pending}>
          End break
        </Button>
      </div>
    </div>
  );
}
