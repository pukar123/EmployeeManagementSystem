import { useCallback, useState } from "react";
import { toast } from "sonner";
import type { CreateTaskRequest, TaskPriority } from "../types/task.types";

type UseTaskFormOptions = {
  organizationId: number | null;
};

const defaultPriority: TaskPriority = 2;

export function useTaskForm({ organizationId }: UseTaskFormOptions) {
  const [formOpen, setFormOpen] = useState(false);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [startAtUtc, setStartAtUtc] = useState("");
  const [dueAtUtc, setDueAtUtc] = useState("");
  const [priority, setPriority] = useState<TaskPriority>(defaultPriority);

  const resetForm = useCallback(() => {
    setSelectedEmployeeId(null);
    setTitle("");
    setDescription("");
    setStartAtUtc("");
    setDueAtUtc("");
    setPriority(defaultPriority);
  }, []);

  const closeForm = useCallback(() => {
    setFormOpen(false);
    resetForm();
  }, [resetForm]);

  const buildCreatePayload = useCallback((): CreateTaskRequest | null => {
    if (selectedEmployeeId == null) {
      toast.error("Please select an employee.");
      return null;
    }

    if (!title.trim()) {
      toast.error("Title is required.");
      return null;
    }

    if (startAtUtc && dueAtUtc && new Date(startAtUtc) > new Date(dueAtUtc)) {
      toast.error("Start date must be before or equal to due date.");
      return null;
    }

    return {
      employeeId: selectedEmployeeId,
      organizationId,
      title: title.trim(),
      description: description.trim() ? description.trim() : null,
      startAtUtc: startAtUtc ? new Date(startAtUtc).toISOString() : null,
      dueAtUtc: dueAtUtc ? new Date(dueAtUtc).toISOString() : null,
      priority,
    };
  }, [description, dueAtUtc, organizationId, priority, selectedEmployeeId, startAtUtc, title]);

  return {
    formOpen,
    setFormOpen,
    selectedEmployeeId,
    setSelectedEmployeeId,
    title,
    setTitle,
    description,
    setDescription,
    startAtUtc,
    setStartAtUtc,
    dueAtUtc,
    setDueAtUtc,
    priority,
    setPriority,
    closeForm,
    buildCreatePayload,
  };
}
