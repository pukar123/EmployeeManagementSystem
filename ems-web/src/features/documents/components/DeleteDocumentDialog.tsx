"use client";

import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { getErrorMessage } from "@/shared/api/http-client";
import { useDocumentMutations } from "../hooks";
import type { Document } from "../types/document.types";

type DeleteDocumentDialogProps = {
  document: Document | null;
  employeeId: number;
  open: boolean;
  onClose: () => void;
  onDeleted: () => void;
};

export function DeleteDocumentDialog({
  document,
  employeeId,
  open,
  onClose,
  onDeleted,
}: DeleteDocumentDialogProps) {
  const { remove } = useDocumentMutations(employeeId);

  const handleDelete = () => {
    if (!document) return;
    remove.mutate(document.id, {
      onSuccess: () => {
        toast.success("Document deleted.");
        onClose();
        onDeleted();
      },
      onError: (e) => toast.error(getErrorMessage(e)),
    });
  };

  return (
    <Modal
      open={open}
      title="Delete document"
      onClose={onClose}
      footer={
        <>
          <Button type="button" variant="secondary" onClick={onClose} disabled={remove.isPending}>
            Cancel
          </Button>
          <Button type="button" variant="danger" onClick={handleDelete} disabled={remove.isPending}>
            {remove.isPending ? "Deleting…" : "Delete"}
          </Button>
        </>
      }
    >
      {document ? (
        <p className="text-sm text-muted-foreground">
          Delete <strong className="text-foreground">{document.name}</strong>? The file will be removed and cannot be
          undone.
        </p>
      ) : null}
    </Modal>
  );
}
