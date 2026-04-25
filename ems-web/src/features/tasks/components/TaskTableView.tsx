import { Button } from "@/shared/components/Button";
import { cn } from "@/shared/utils/cn";
import type { TaskItem, TaskWorkflowStatus } from "../types/task.types";
import { formatTaskDateTime, taskPriorityLabels, taskStatusLabels } from "../utils/taskDisplay";

type TaskTableViewProps = {
  tasks: TaskItem[];
  employeeLabelById: Map<number, string>;
  onStatusChange: (task: TaskItem, nextStatus: TaskWorkflowStatus) => void;
  onDelete: (task: TaskItem) => void;
};

export function TaskTableView({ tasks, employeeLabelById, onStatusChange, onDelete }: TaskTableViewProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-700">
      <table className="min-w-full divide-y divide-zinc-200 text-left text-sm dark:divide-zinc-700">
        <thead className="bg-zinc-50 dark:bg-zinc-900/50">
          <tr>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Title</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Employee</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Status</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Priority</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Start</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Due</th>
            <th className="px-4 py-3 font-medium text-zinc-700 dark:text-zinc-300">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-zinc-200 dark:divide-zinc-700">
          {tasks.map((task) => (
            <tr key={task.id} className="bg-white hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900">
              <td className="px-4 py-3">
                <div className="font-medium text-zinc-900 dark:text-zinc-100">{task.title}</div>
                <div className="text-xs text-zinc-500 dark:text-zinc-400">{task.description ?? "No description"}</div>
              </td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{employeeLabelById.get(task.employeeId) ?? "Unknown"}</td>
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
                  {taskStatusLabels[task.status]}
                </span>
              </td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{task.priority ? taskPriorityLabels[task.priority] : "—"}</td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{formatTaskDateTime(task.startAtUtc)}</td>
              <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">{formatTaskDateTime(task.dueAtUtc)}</td>
              <td className="px-4 py-3">
                <div className="flex flex-wrap gap-2">
                  {task.status !== 2 ? (
                    <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => onStatusChange(task, 2)}>
                      Start
                    </Button>
                  ) : null}
                  {task.status !== 3 ? (
                    <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => onStatusChange(task, 3)}>
                      Block
                    </Button>
                  ) : null}
                  {task.status !== 4 ? (
                    <Button type="button" variant="secondary" className="!py-1 !text-xs" onClick={() => onStatusChange(task, 4)}>
                      Complete
                    </Button>
                  ) : null}
                  <Button type="button" variant="danger" className="!py-1 !text-xs" onClick={() => onDelete(task)}>
                    Delete
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {tasks.length === 0 ? <p className="p-6 text-center text-sm text-zinc-500">No tasks yet (or no matches).</p> : null}
    </div>
  );
}
