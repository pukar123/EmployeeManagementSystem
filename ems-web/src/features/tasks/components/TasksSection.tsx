"use client";

import { useCallback } from "react";
import { useCreateActionParam } from "@/features/command-palette/hooks/useCreateActionParam";
import { getErrorMessage } from "@/shared/api/http-client";
import { Spinner } from "@/shared/components/Spinner";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { useTaskActions } from "../hooks/useTaskActions";
import { useTaskCalendarEvents } from "../hooks/useTaskCalendarEvents";
import { useTaskForm } from "../hooks/useTaskForm";
import { useTasksController } from "../hooks/useTasksController";
import { TaskCalendarView } from "./TaskCalendarView";
import { TaskFilters } from "./TaskFilters";
import { TaskFormModal } from "./TaskFormModal";
import { TaskTableView } from "./TaskTableView";
import { TasksToolbar } from "./TasksToolbar";

type TasksSectionProps = {
  initialViewMode?: "table" | "calendar";
};

export function TasksSection({ initialViewMode = "table" }: TasksSectionProps) {
  const { organizationId } = useOrganizationContext();
  const inputClassName =
    "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

  const {
    search,
    setSearch,
    viewMode,
    setViewMode,
    filterEmployeeId,
    setFilterEmployeeId,
    setCalendarRange,
    employees,
    employeeLabelById,
    tasks,
    tasksQuery,
    employeesQuery,
  } = useTasksController({
    initialViewMode,
  });

  const taskForm = useTaskForm({ organizationId });
  const taskActions = useTaskActions({ onCreateSuccess: taskForm.closeForm });
  const calendarEvents = useTaskCalendarEvents({ tasks, employeeLabelById });

  useCreateActionParam(() => taskForm.setFormOpen(true));

  const handleCreate = useCallback(async () => {
    const payload = taskForm.buildCreatePayload();
    if (!payload) {
      return;
    }

    await taskActions.createTask(payload);
  }, [taskActions, taskForm]);

  if (tasksQuery.isLoading || employeesQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (employeesQuery.isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
        Could not load employees.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <TasksToolbar viewMode={viewMode} onViewModeChange={setViewMode} onAssignTask={() => taskForm.setFormOpen(true)} />
      <TaskFilters
        search={search}
        onSearchChange={setSearch}
        employees={employees}
        selectedEmployeeId={filterEmployeeId}
        onSelectedEmployeeChange={setFilterEmployeeId}
        inputClassName={inputClassName}
      />

      {tasksQuery.isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
          {getErrorMessage(tasksQuery.error)}
        </div>
      ) : viewMode === "calendar" ? (
        <TaskCalendarView events={calendarEvents} employeeLabelById={employeeLabelById} onRangeChange={setCalendarRange} />
      ) : (
        <TaskTableView tasks={tasks} employeeLabelById={employeeLabelById} onStatusChange={taskActions.updateTaskStatus} onDelete={taskActions.deleteTask} />
      )}

      <TaskFormModal
        open={taskForm.formOpen}
        employees={employees}
        selectedEmployeeId={taskForm.selectedEmployeeId}
        onSelectedEmployeeIdChange={taskForm.setSelectedEmployeeId}
        title={taskForm.title}
        onTitleChange={taskForm.setTitle}
        description={taskForm.description}
        onDescriptionChange={taskForm.setDescription}
        startAtUtc={taskForm.startAtUtc}
        onStartAtUtcChange={taskForm.setStartAtUtc}
        dueAtUtc={taskForm.dueAtUtc}
        onDueAtUtcChange={taskForm.setDueAtUtc}
        priority={taskForm.priority}
        onPriorityChange={taskForm.setPriority}
        onSubmit={handleCreate}
        onClose={taskForm.closeForm}
        isSubmitting={taskActions.isCreating}
        inputClassName={inputClassName}
      />
    </div>
  );
}
