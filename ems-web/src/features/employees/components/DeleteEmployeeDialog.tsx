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
  const archiveMutation = useDeleteEmployee();

  const handleArchive = () => {
    if (!employee) return;
    archiveMutation.mutate(employee.id, {
      onSuccess: () => {
        toast.success("Employee archived");
        onClose();
        onDeleted();
      },
      onError: (e) => toast.error(getErrorMessage(e)),
    });
  };

  return (
    <Modal
      open={open}
      title="Archive employee"
      onClose={onClose}
      footer={
        <>
          <Button type="button" variant="secondary" onClick={onClose} disabled={archiveMutation.isPending}>
            Cancel
          </Button>
          <Button type="button" variant="danger" onClick={handleArchive} disabled={archiveMutation.isPending}>
            {archiveMutation.isPending ? "Archiving…" : "Archive"}
          </Button>
        </>
      }
    >
      {employee ? (
        <p className="text-sm text-muted-foreground dark:text-muted-foreground">
          Archive{" "}
          <strong className="text-foreground dark:text-white">
            {employee.firstName} {employee.lastName}
          </strong>{" "}
          ({employee.email})? The employee will disappear from active lists but remains retained according to your
          organization&apos;s policy. Repeating archive is safe if the record is already archived.
        </p>
      ) : null}
    </Modal>
  );
}
