"use client";

import { Button } from "@/shared/components/Button";
import type { AttendanceExportFormat } from "../types/attendance-analytics.types";

type AttendanceExportMenuProps = {
  pending: boolean;
  onExport: (format: AttendanceExportFormat) => void;
};

export function AttendanceExportMenu({ pending, onExport }: AttendanceExportMenuProps) {
  return (
    <div className="flex flex-wrap gap-2">
      <Button type="button" variant="secondary" disabled={pending} onClick={() => onExport("csv")}>
        Export CSV
      </Button>
      <Button type="button" variant="secondary" disabled={pending} onClick={() => onExport("xlsx")}>
        Export Excel
      </Button>
      <Button type="button" variant="secondary" disabled={pending} onClick={() => onExport("pdf")}>
        Export PDF
      </Button>
    </div>
  );
}
