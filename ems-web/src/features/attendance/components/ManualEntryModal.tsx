"use client";

import { useState } from "react";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import type { ManualAttendanceEntryRequest } from "../types/attendance.types";

type ManualEntryModalProps = {
  open: boolean;
  organizationId: number;
  employeeId: number;
  pending: boolean;
  onClose: () => void;
  onSubmit: (request: ManualAttendanceEntryRequest) => Promise<void>;
};

export function ManualEntryModal({
  open,
  organizationId,
  employeeId,
  pending,
  onClose,
  onSubmit,
}: ManualEntryModalProps) {
  const now = new Date();
  const [workDate, setWorkDate] = useState(now.toISOString().slice(0, 10));
  const [checkInTime, setCheckInTime] = useState("09:00");
  const [checkOutTime, setCheckOutTime] = useState("18:00");
  const [manualReason, setManualReason] = useState("");

  const toIso = (dateStr: string, timeStr: string) => new Date(`${dateStr}T${timeStr}:00`).toISOString();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit({
      organizationId,
      employeeId,
      workDate,
      checkInAtUtc: toIso(workDate, checkInTime),
      checkOutAtUtc: toIso(workDate, checkOutTime),
      manualReason: manualReason.trim() ? manualReason.trim() : null,
    });
  };

  return (
    <Modal open={open} title="Manual attendance entry" onClose={onClose} className="max-w-lg">
      <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
        <p className="text-sm text-muted-foreground">Manual entries are auto-approved in this version.</p>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Work date</label>
          <input
            type="date"
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
            value={workDate}
            onChange={(e) => setWorkDate(e.target.value)}
            required
          />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Check-in</label>
            <input
              type="time"
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
              value={checkInTime}
              onChange={(e) => setCheckInTime(e.target.value)}
              required
            />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Check-out</label>
            <input
              type="time"
              className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
              value={checkOutTime}
              onChange={(e) => setCheckOutTime(e.target.value)}
              required
            />
          </div>
        </div>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Reason (optional)</label>
          <textarea
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm dark:bg-card"
            rows={3}
            value={manualReason}
            onChange={(e) => setManualReason(e.target.value)}
          />
        </div>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={pending}>
            {pending ? "Saving..." : "Save entry"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
