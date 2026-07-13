"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useEmployeeOnboardingProgress } from "@/features/onboarding/hooks";
import type { EmployeeOnboardingProgressItem } from "@/features/onboarding/types/onboarding.types";
import { useUpdateTaskStatus } from "@/features/tasks/hooks/useUpdateTaskStatus";
import { taskService } from "@/features/tasks/services/taskService";
import { taskStatusLabels, formatTaskDateTime } from "@/features/tasks/utils/taskDisplay";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { cn } from "@/shared/utils/cn";
import {
  computeOnboardingProgressSummary,
  formatOnboardingCategory,
} from "../utils/onboardingProgress";

type EmployeeOnboardingProgressProps = {
  employeeId: number;
  canManage: boolean;
  isPreboarding?: boolean;
  variant?: "full" | "compact";
};

export function EmployeeOnboardingProgress({
  employeeId,
  canManage,
  isPreboarding = false,
  variant = "full",
}: EmployeeOnboardingProgressProps) {
  const progressQuery = useEmployeeOnboardingProgress(employeeId);
  const updateStatus = useUpdateTaskStatus();
  const [editingTaskId, setEditingTaskId] = useState<number | null>(null);
  const [dueDateDraft, setDueDateDraft] = useState("");

  const summary = useMemo(() => {
    if (!progressQuery.data) return null;
    return computeOnboardingProgressSummary(progressQuery.data);
  }, [progressQuery.data]);

  if (progressQuery.isLoading) {
    return (
      <div className="flex justify-center py-6">
        <Spinner />
      </div>
    );
  }

  const progress = progressQuery.data;
  if (!progress || !summary?.hasChecklist) {
    if (!isPreboarding) return null;
    return (
      <section className={variant === "compact" ? "rounded-xl border border-border p-4" : "rounded-xl border border-border p-6"}>
        <h3 className="text-lg font-semibold text-foreground">Onboarding progress</h3>
        <p className="mt-2 text-sm text-muted-foreground">No onboarding checklist has been generated for this employee.</p>
      </section>
    );
  }

  const completeItem = async (item: EmployeeOnboardingProgressItem) => {
    if (!item.taskId || item.status === 4) return;
    try {
      await updateStatus.mutateAsync({ id: item.taskId, status: 4 });
      await progressQuery.refetch();
      toast.success("Checklist item marked complete.");
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const startEditDueDate = (item: EmployeeOnboardingProgressItem) => {
    if (!item.taskId) return;
    setEditingTaskId(item.taskId);
    setDueDateDraft(item.dueAtUtc ? item.dueAtUtc.slice(0, 10) : "");
  };

  const saveDueDate = async (item: EmployeeOnboardingProgressItem) => {
    if (!item.taskId) return;
    try {
      const dueAtUtc = dueDateDraft ? new Date(`${dueDateDraft}T00:00:00.000Z`).toISOString() : null;
      await taskService.updateTask(item.taskId, {
        title: item.title,
        description: null,
        startAtUtc: null,
        dueAtUtc,
        priority: null,
      });
      setEditingTaskId(null);
      await progressQuery.refetch();
      toast.success("Due date updated.");
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const content = (
    <>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-foreground">Onboarding progress</h3>
          {progress.templateName ? (
            <p className="text-sm text-muted-foreground">
              Template: {progress.templateName}
              {progress.generatedAtUtc ? ` · Generated ${formatTaskDateTime(progress.generatedAtUtc)}` : ""}
            </p>
          ) : null}
        </div>
        <div className="text-right text-sm">
          <p className="font-medium text-foreground">{summary.percentComplete}% complete</p>
          <p className="text-muted-foreground">
            {summary.completedCount}/{summary.totalCount} items
            {summary.overdueCount > 0 ? ` · ${summary.overdueCount} overdue` : ""}
          </p>
        </div>
      </div>

      <div className="mt-4 h-2 overflow-hidden rounded-full bg-muted">
        <div
          className="h-full rounded-full bg-primary transition-all"
          style={{ width: `${summary.percentComplete}%` }}
        />
      </div>

      {variant === "compact" ? (
        <p className="mt-3 text-sm text-muted-foreground">
          {summary.isComplete
            ? "All onboarding tasks are complete."
            : `${summary.totalCount - summary.completedCount} onboarding task(s) remaining.`}
        </p>
      ) : (
        <ul className="mt-4 space-y-3">
          {progress.items.map((item) => (
            <li
              key={item.templateItemId}
              className={cn(
                "rounded-lg border border-border p-4",
                item.isOverdue ? "border-destructive/40 bg-destructive/5" : "bg-background",
              )}
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="font-medium text-foreground">{item.title}</p>
                  <p className="text-xs text-muted-foreground">
                    {formatOnboardingCategory(item.category)}
                    {item.isRequired ? " · Required" : " · Optional"}
                  </p>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Due: {item.dueAtUtc ? formatTaskDateTime(item.dueAtUtc) : "Not set"}
                    {item.status != null ? ` · ${taskStatusLabels[item.status as 1 | 2 | 3 | 4]}` : ""}
                  </p>
                </div>
                {canManage && item.taskId ? (
                  <div className="flex flex-wrap gap-2">
                    {item.status !== 4 ? (
                      <Button
                        type="button"
                        variant="secondary"
                        disabled={updateStatus.isPending}
                        onClick={() => void completeItem(item)}
                      >
                        Mark complete
                      </Button>
                    ) : null}
                    {editingTaskId === item.taskId ? (
                      <>
                        <input
                          type="date"
                          value={dueDateDraft}
                          onChange={(e) => setDueDateDraft(e.target.value)}
                          className="rounded-lg border border-input px-2 py-1 text-sm"
                        />
                        <Button type="button" onClick={() => void saveDueDate(item)}>
                          Save due date
                        </Button>
                        <Button type="button" variant="secondary" onClick={() => setEditingTaskId(null)}>
                          Cancel
                        </Button>
                      </>
                    ) : (
                      <Button type="button" variant="secondary" onClick={() => startEditDueDate(item)}>
                        Edit due date
                      </Button>
                    )}
                  </div>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      )}
    </>
  );

  if (variant === "compact") {
    return <section className="rounded-xl border border-border p-4">{content}</section>;
  }

  return <section className="space-y-4 rounded-xl border border-border p-6">{content}</section>;
}
