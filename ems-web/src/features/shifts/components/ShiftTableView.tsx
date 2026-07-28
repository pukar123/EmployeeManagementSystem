import { Button } from "@/shared/components/Button";
import { cn } from "@/shared/utils/cn";
import type { ShiftItem, ShiftStatus } from "../types/shift.types";
import { formatShiftDateTime, shiftStatusLabels } from "../utils/shiftDisplay";

type ShiftTableViewProps = {
  shifts: ShiftItem[];
  employeeLabelById: Map<number, string>;
  siteLabelById: Map<number, string>;
  onEdit: (shift: ShiftItem) => void;
  onDelete: (shift: ShiftItem) => void;
};

const statusClassName: Record<ShiftStatus, string> = {
  0: "bg-muted text-muted-foreground",
  1: "bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200",
  2: "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200",
  3: "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200",
};

export function ShiftTableView({ shifts, employeeLabelById, siteLabelById, onEdit, onDelete }: ShiftTableViewProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="min-w-full divide-y divide-border text-left text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className="px-4 py-3 font-medium text-muted-foreground">Title</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Employee</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Site</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Status</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Start</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">End</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {shifts.map((shift) => (
            <tr key={shift.id} className="bg-card hover:bg-muted/40">
              <td className="px-4 py-3">
                <div className="font-medium text-foreground">{shift.title}</div>
                <div className="text-xs text-muted-foreground">{shift.description ?? "No description"}</div>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{employeeLabelById.get(shift.employeeId) ?? "Unknown"}</td>
              <td className="px-4 py-3 text-muted-foreground">
                {shift.siteId != null ? (siteLabelById.get(shift.siteId) ?? "Unknown site") : "—"}
              </td>
              <td className="px-4 py-3">
                <span className={cn("inline-flex rounded-full px-2 py-0.5 text-xs font-medium", statusClassName[shift.status])}>
                  {shiftStatusLabels[shift.status]}
                </span>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{formatShiftDateTime(shift.startAtUtc)}</td>
              <td className="px-4 py-3 text-muted-foreground">{formatShiftDateTime(shift.endAtUtc)}</td>
              <td className="px-4 py-3">
                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => onEdit(shift)}>
                    Edit
                  </Button>
                  <Button type="button" variant="danger" className="!py-1 !text-xs" onClick={() => onDelete(shift)}>
                    Delete
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {shifts.length === 0 ? <p className="p-6 text-center text-sm text-muted-foreground">No shifts yet (or no matches).</p> : null}
    </div>
  );
}
