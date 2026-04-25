import type { Employee } from "@/features/employees/types/employee.types";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import type { TaskPriority } from "../types/task.types";
import { getEmployeeDisplayLabel } from "../utils/taskDisplay";

type TaskFormModalProps = {
  open: boolean;
  employees: Employee[];
  selectedEmployeeId: number | null;
  onSelectedEmployeeIdChange: (value: number | null) => void;
  title: string;
  onTitleChange: (value: string) => void;
  description: string;
  onDescriptionChange: (value: string) => void;
  startAtUtc: string;
  onStartAtUtcChange: (value: string) => void;
  dueAtUtc: string;
  onDueAtUtcChange: (value: string) => void;
  priority: TaskPriority;
  onPriorityChange: (value: TaskPriority) => void;
  onSubmit: () => void;
  onClose: () => void;
  isSubmitting: boolean;
  inputClassName: string;
};

export function TaskFormModal({
  open,
  employees,
  selectedEmployeeId,
  onSelectedEmployeeIdChange,
  title,
  onTitleChange,
  description,
  onDescriptionChange,
  startAtUtc,
  onStartAtUtcChange,
  dueAtUtc,
  onDueAtUtcChange,
  priority,
  onPriorityChange,
  onSubmit,
  onClose,
  isSubmitting,
  inputClassName,
}: TaskFormModalProps) {
  return (
    <Modal open={open} title="Assign task" onClose={onClose} className="max-w-lg">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          onSubmit();
        }}
        className="space-y-4"
      >
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Employee</label>
          <select
            value={selectedEmployeeId ?? ""}
            onChange={(e) => onSelectedEmployeeIdChange(e.target.value ? Number(e.target.value) : null)}
            className={inputClassName}
            required
          >
            <option value="" disabled>
              Select employee
            </option>
            {employees.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {getEmployeeDisplayLabel(employee)}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Title</label>
          <input type="text" value={title} onChange={(e) => onTitleChange(e.target.value)} className={inputClassName} required />
        </div>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Description</label>
          <textarea value={description} onChange={(e) => onDescriptionChange(e.target.value)} className={inputClassName} rows={3} />
        </div>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Start date</label>
          <input type="datetime-local" value={startAtUtc} onChange={(e) => onStartAtUtcChange(e.target.value)} className={inputClassName} />
        </div>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Due date</label>
          <input type="datetime-local" value={dueAtUtc} onChange={(e) => onDueAtUtcChange(e.target.value)} className={inputClassName} />
        </div>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Priority</label>
          <select value={priority} onChange={(e) => onPriorityChange(Number(e.target.value) as TaskPriority)} className={inputClassName}>
            <option value={1}>Low</option>
            <option value={2}>Medium</option>
            <option value={3}>High</option>
          </select>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Assigning…" : "Assign"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
