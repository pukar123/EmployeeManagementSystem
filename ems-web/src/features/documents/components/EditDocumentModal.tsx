"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useForm, type Resolver } from "react-hook-form";
import { toast } from "sonner";
import { dateInputToApiIso, toDateInputValue } from "@/features/employees/utils/date-format";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { getErrorMessage } from "@/shared/api/http-client";
import { useDocumentMutations } from "../hooks";
import { useDocumentTypes } from "../hooks/useDocumentTypes";
import {
  editDocumentFormSchema,
  type EditDocumentFormValues,
} from "../types/document-form.schema";
import type { Document, DocumentType } from "../types/document.types";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

type EditDocumentModalProps = {
  open: boolean;
  employeeId: number;
  document: Document | null;
  onClose: () => void;
  onUpdated: () => void;
};

export function EditDocumentModal({
  open,
  employeeId,
  document,
  onClose,
  onUpdated,
}: EditDocumentModalProps) {
  const { update } = useDocumentMutations(employeeId);
  const typesQuery = useDocumentTypes(open);
  const types = typesQuery.data ?? [];

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<EditDocumentFormValues>({
    resolver: zodResolver(editDocumentFormSchema) as Resolver<EditDocumentFormValues>,
    defaultValues: {
      name: "",
      documentTypeId: undefined,
      issueDate: "",
      expiryDate: "",
    },
  });

  useEffect(() => {
    if (!open || !document) return;
    reset({
      name: document.name,
      documentTypeId: document.documentTypeId,
      issueDate: toDateInputValue(document.issueDate ?? ""),
      expiryDate: toDateInputValue(document.expiryDate ?? ""),
    });
  }, [open, document, reset]);

  const onSubmit = handleSubmit((values) => {
    if (!document) return;
    update.mutate(
      {
        id: document.id,
        body: {
          name: values.name.trim(),
          documentTypeId: values.documentTypeId,
          employeeId,
          issueDate: values.issueDate ? dateInputToApiIso(values.issueDate) : null,
          expiryDate: values.expiryDate ? dateInputToApiIso(values.expiryDate) : null,
        },
      },
      {
        onSuccess: () => {
          toast.success("Document updated.");
          onClose();
          onUpdated();
        },
        onError: (e) => toast.error(getErrorMessage(e)),
      },
    );
  });

  return (
    <Modal
      open={open}
      title="Edit document"
      onClose={onClose}
      className="max-w-lg"
      footer={
        <>
          <Button type="button" variant="secondary" onClick={onClose} disabled={update.isPending}>
            Cancel
          </Button>
          <Button
            type="submit"
            form="edit-document-form"
            disabled={update.isPending || typesQuery.isLoading || !document}
          >
            {update.isPending ? "Saving…" : "Save changes"}
          </Button>
        </>
      }
    >
      {document ? (
        <form id="edit-document-form" className="space-y-4" onSubmit={(e) => void onSubmit(e)}>
          <div>
            <label htmlFor="doc-edit-name" className="text-sm font-medium text-foreground">
              Name
            </label>
            <input id="doc-edit-name" className={inputClass} {...register("name")} />
            {errors.name ? <p className="mt-1 text-xs text-destructive">{errors.name.message}</p> : null}
          </div>

          <div>
            <label htmlFor="doc-edit-type" className="text-sm font-medium text-foreground">
              Document type
            </label>
            <select
              id="doc-edit-type"
              className={inputClass}
              {...register("documentTypeId")}
              disabled={typesQuery.isLoading}
            >
              <option value="">Select type…</option>
              {types.map((t: DocumentType) => (
                <option key={t.id} value={t.id}>
                  {t.name}
                </option>
              ))}
            </select>
            {errors.documentTypeId ? (
              <p className="mt-1 text-xs text-destructive">{errors.documentTypeId.message}</p>
            ) : null}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label htmlFor="doc-edit-issue" className="text-sm font-medium text-foreground">
                Issue date
              </label>
              <input id="doc-edit-issue" type="date" className={inputClass} {...register("issueDate")} />
            </div>
            <div>
              <label htmlFor="doc-edit-expiry" className="text-sm font-medium text-foreground">
                Expiry date
              </label>
              <input id="doc-edit-expiry" type="date" className={inputClass} {...register("expiryDate")} />
            </div>
          </div>

          <p className="text-xs text-muted-foreground">
            Current file: {document.originalFileName}. Upload a new document to replace the file.
          </p>
        </form>
      ) : null}
    </Modal>
  );
}
