import { Button } from "@/shared/components/Button";

type ShiftsToolbarProps = {
  viewMode: "table" | "calendar";
  onViewModeChange: (mode: "table" | "calendar") => void;
  onScheduleShift: () => void;
};

export function ShiftsToolbar({ viewMode, onViewModeChange, onScheduleShift }: ShiftsToolbarProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h1 className="text-2xl font-semibold text-foreground">Shifts</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Schedule employee shifts. Scheduled work appears in the Employee Portal.
        </p>
      </div>
      <div className="flex flex-wrap gap-2">
        <Button type="button" variant={viewMode === "table" ? "primary" : "secondary"} onClick={() => onViewModeChange("table")}>
          Table
        </Button>
        <Button type="button" variant={viewMode === "calendar" ? "primary" : "secondary"} onClick={() => onViewModeChange("calendar")}>
          Calendar
        </Button>
        <Button type="button" onClick={onScheduleShift}>
          Schedule shift
        </Button>
      </div>
    </div>
  );
}
