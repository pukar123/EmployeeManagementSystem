"use client";

import { Button } from "@/shared/components/Button";

type CheckInOutCardProps = {
  hasOpenSession: boolean;
  pending: boolean;
  onCheckIn: () => void;
  onCheckOut: () => void;
};

export function CheckInOutCard({ hasOpenSession, pending, onCheckIn, onCheckOut }: CheckInOutCardProps) {
  return (
    <div className="rounded-xl border border-border bg-card p-5 shadow-sm dark:bg-card">
      <h2 className="text-base font-semibold text-foreground">Check-in / Check-out</h2>
      <p className="mt-1 text-sm text-muted-foreground">
        {hasOpenSession ? "You are currently checked in." : "You are currently checked out."}
      </p>
      <div className="mt-4 flex gap-2">
        <Button type="button" onClick={onCheckIn} disabled={hasOpenSession || pending}>
          Check in
        </Button>
        <Button type="button" variant="secondary" onClick={onCheckOut} disabled={!hasOpenSession || pending}>
          Check out
        </Button>
      </div>
    </div>
  );
}
