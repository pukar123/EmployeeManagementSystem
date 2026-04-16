"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { cn } from "@/shared/utils/cn";
import { useCreateTask, useDeleteTask, useTasks, useUpdateTaskStatus } from "../hooks";
import type { TaskItem, TaskPriority, TaskWorkflowStatus } from "../types/task.types";
import { useEmployees } from "@/features/employees/hooks";

const inputClass =
  "mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100";

const statusLabels: Record<TaskWorkflowStatus, string> = {
  1: "Assigned",
  2: "In Progress",
  3: "Blocked",
  4: "Completed",
};

const priorityLabels: Record<TaskPriority, string> = {
  1: "Low",
  2: "Medium",
  3: "High",
};

export function TasksSection() {
  const { organizationId } = useOrganizationContext();
  const { data, isLoading, isError, error } = useTasks();
  const { data: employees = [], isLoading: employeesLoading, isError: employeesError } = useEmployees();
  const createMut = useCreateTask();
  const statusMut = useUpdateTaskStatus();
  const deleteMut = useDeleteTask();

  const [search, setSearch] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [dueAtUtc, setDueAtUtc] = useState("");
  const [priority, setPriority] = useState<TaskPriority>(2);

  const employeeLabelById = useMemo(() => {
    const map = new Map<number, string>();
    for (const e of employees) {
      const name = `${e.firstName} ${e.lastName}`.trim();
      const empNo = e.employeeNumber?.trim();
      map.set(e.id, empNo ? `${empNo} — ${name}` : name);
    }
    return map;
  }, [employees]);

  const filtered = useMemo(() => {
    const list = data ?? [];
    const q = search.trim().toLowerCase();
    if (!q) return list;
    return list.filter((task) =>
      `${task.title} ${task.description ?? ""} ${(employeeLabelById.get(task.employeeId) ?? "").toString()} ${
        statusLabels[task.status]
      }`.toLowerCase().includes(q),
    );
  }, [data, search, employeeLabelById]);

  const closeForm = () => {
    setFormOpen(false);
    setSelectedEmployeeId(null);
    setTitle("");
    setDescription("");
    setDueAtUtc("");
    setPriority(2);
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedEmployeeId == null) {
      toast.error("Please select an employee.");
      return;
    }
    if (!title.trim()) {
      toast.error("Title is required.");
      return;
    }

    try {
      await createMut.mutateAsync({
        employeeId: selectedEmployeeId,
        organizationId,
        title: title.trim(),
        description: description.trim() ? description.trim() : null,
        dueAtUtc: dueAtUtc ? new Date(dueAtUtc).toISOString() : null,
        priority,
      });
      toast.success("Task assigned.");
      closeForm();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const handleStatusChange = async (task: TaskItem, nextStatus: TaskWorkflowStatus) => {
    try {
      await statusMut.mutateAsync({ id: task.id, status: nextStatus });
      toast.success("Task status updated.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const handleDelete = async (task: TaskItem) => {
    if (!window.confirm(`Delete task "${task.title}"?`)) return;
    try {
      await deleteMut.mutateAsync(task.id);
      toast.success("Task deleted.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  if (isLoading || employeesLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (employeesError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
        Could not load employees.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Tasks</h1>
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            Assign work to employees and track progress through the task workflow.
          </p>
        </div>
        <Button type="button" onClick={() => setFormOpen(true)}>
          Assign task
        </Button>
      </div>

      <div className="max-w-md">
        <label className="block text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Search
        </label>
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Title, employee, status"
          className={inputClass}
        />
      </div>

      {isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
          {getErrorMessage(error)}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-700">
          <table className="min-w-full divide-y divide-zinc-200 text-left text-sm dark:divide-zinc-700">
            <thead className="bg-zinc-50 dark:bg-zinc-900/50">
              <tr>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Title</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Employee</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Status</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Priority</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Due</th>
                <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-zinc-200 dark:divide-zinc-700">
              {filtered.map((task) => (
                <tr key={task.id} className="bg-white hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900">
                  <td className="px-4 py-3">
                    <div className="font-medium text-zinc-900 dark:text-zinc-100">{task.title}</div>
                    <div className="text-xs text-zinc-500 dark:text-zinc-400">{task.description ?? "No description"}</div>
                  </td>
                  <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">
                    {employeeLabelById.get(task.employeeId) ?? "Unknown"}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={cn(
                        "inline-flex rounded-full px-2 py-0.5 text-xs font-medium",
                        task.status === 4
                          ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200"
                          : task.status === 3
                            ? "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200"
                            : task.status === 2
                              ? "bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200"
                              : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300",
                      )}
                    >
                      {statusLabels[task.status]}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">
                    {task.priority ? priorityLabels[task.priority] : "—"}
                  </td>
                  <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">
                    {task.dueAtUtc ? new Date(task.dueAtUtc).toLocaleString() : "—"}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      {task.status !== 2 ? (
                        <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => void handleStatusChange(task, 2)}>
                          Start
                        </Button>
                      ) : null}
                      {task.status !== 3 ? (
                        <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => void handleStatusChange(task, 3)}>
                          Block
                        </Button>
                      ) : null}
                      {task.status !== 4 ? (
                        <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => void handleStatusChange(task, 4)}>
                          Complete
                        </Button>
                      ) : null}
                      <Button type="button" variant="danger" className="!py-1 !text-xs" onClick={() => void handleDelete(task)}>
                        Delete
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {filtered.length === 0 ? <p className="p-6 text-center text-sm text-zinc-500">No tasks yet (or no matches).</p> : null}
        </div>
      )}

      <Modal open={formOpen} title="Assign task" onClose={closeForm} className="max-w-lg">
        <form onSubmit={(e) => void handleCreate(e)} className="space-y-4">
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Employee</label>
            <select
              value={selectedEmployeeId ?? ""}
              onChange={(e) => setSelectedEmployeeId(e.target.value ? Number(e.target.value) : null)}
              className={inputClass}
              required
            >
              <option value="" disabled>
                Select employee
              </option>
              {employees.map((e) => {
                const name = `${e.firstName} ${e.lastName}`.trim();
                const empNo = e.employeeNumber?.trim();
                return (
                  <option key={e.id} value={e.id}>
                    {empNo ? `${empNo} — ${name}` : name}
                  </option>
                );
              })}
            </select>
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Title</label>
            <input type="text" value={title} onChange={(e) => setTitle(e.target.value)} className={inputClass} required />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Description</label>
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} className={inputClass} rows={3} />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Due date</label>
            <input type="datetime-local" value={dueAtUtc} onChange={(e) => setDueAtUtc(e.target.value)} className={inputClass} />
          </div>
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Priority</label>
            <select value={priority} onChange={(e) => setPriority(Number(e.target.value) as TaskPriority)} className={inputClass}>
              <option value={1}>Low</option>
              <option value={2}>Medium</option>
              <option value={3}>High</option>
            </select>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" onClick={closeForm}>
              Cancel
            </Button>
            <Button type="submit" disabled={createMut.isPending}>
              {createMut.isPending ? "Assigning…" : "Assign"}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
