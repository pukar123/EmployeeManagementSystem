import { Button } from "@/shared/components/Button";

type TasksToolbarProps = {
  viewMode: "table" | "calendar";
  onViewModeChange: (mode: "table" | "calendar") => void;
  onAssignTask: () => void;
};

export function TasksToolbar({ viewMode, onViewModeChange, onAssignTask }: TasksToolbarProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Tasks</h1>
        <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
          Assign work to employees and track progress through the task workflow.
        </p>
      </div>
      <div className="flex flex-wrap gap-2">
        <Button type="button" variant={viewMode === "table" ? "primary" : "secondary"} onClick={() => onViewModeChange("table")}>
          Table
        </Button>
        <Button type="button" variant={viewMode === "calendar" ? "primary" : "secondary"} onClick={() => onViewModeChange("calendar")}>
          Calendar
        </Button>
        <Button type="button" onClick={onAssignTask}>
          Assign task
        </Button>
      </div>
    </div>
  );
}
