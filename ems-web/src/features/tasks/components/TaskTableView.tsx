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
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="min-w-full divide-y divide-border text-left text-sm ">
        <thead className="bg-muted/50">
          <tr>
            <th className="px-4 py-3 font-medium text-muted-foreground">Title</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Employee</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Status</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Priority</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Start</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Due</th>
            <th className="px-4 py-3 font-medium text-muted-foreground">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {tasks.map((task) => (
            <tr key={task.id} className="bg-card hover:bg-muted/40">
              <td className="px-4 py-3">
                <div className="font-medium text-foreground">{task.title}</div>
                <div className="text-xs text-muted-foreground">{task.description ?? "No description"}</div>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{employeeLabelById.get(task.employeeId) ?? "Unknown"}</td>
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
                          : "bg-muted text-muted-foreground",
                  )}
                >
                  {taskStatusLabels[task.status]}
                </span>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{task.priority ? taskPriorityLabels[task.priority] : "—"}</td>
              <td className="px-4 py-3 text-muted-foreground">{formatTaskDateTime(task.startAtUtc)}</td>
              <td className="px-4 py-3 text-muted-foreground">{formatTaskDateTime(task.dueAtUtc)}</td>
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
      {tasks.length === 0 ? <p className="p-6 text-center text-sm text-muted-foreground">No tasks yet (or no matches).</p> : null}
    </div>
  );
}
