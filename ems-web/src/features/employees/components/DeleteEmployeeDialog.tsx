"use client";

import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { getErrorMessage } from "@/shared/api/http-client";
import { useDeleteEmployee } from "../hooks";
import type { Employee } from "../types/employee.types";

type DeleteEmployeeDialogProps = {
  employee: Employee | null;
  open: boolean;
  onClose: () => void;
  onDeleted: () => void;
};

export function DeleteEmployeeDialog({ employee, open, onClose, onDeleted }: DeleteEmployeeDialogProps) {
  const deleteMutation = useDeleteEmployee();

  const handleDelete = () => {
    if (!employee) return;
    deleteMutation.mutate(employee.id, {
      onSuccess: () => {
        toast.success("Employee deleted");
        onClose();
        onDeleted();
      },
      onError: (e) => toast.error(getErrorMessage(e)),
    });
  };

  return (
    <Modal
      open={open}
      title="Delete employee"
      onClose={onClose}
      footer={
        <>
          <Button type="button" variant="secondary" onClick={onClose} disabled={deleteMutation.isPending}>
            Cancel
          </Button>
          <Button type="button" variant="danger" onClick={handleDelete} disabled={deleteMutation.isPending}>
            {deleteMutation.isPending ? "Deleting…" : "Delete"}
          </Button>
        </>
      }
    >
      {employee ? (
        <p className="text-sm text-muted-foreground dark:text-muted-foreground">
          Are you sure you want to delete{" "}
          <strong className="text-foreground dark:text-white">
            {employee.firstName} {employee.lastName}
          </strong>{" "}
          ({employee.email})? This cannot be undone.
        </p>
      ) : null}
    </Modal>
  );
}
