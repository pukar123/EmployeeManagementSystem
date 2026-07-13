"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRef } from "react";
import { useForm, type Resolver } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import { getErrorMessage } from "@/shared/api/http-client";
import { useDocumentMutations } from "../hooks";
import { useDocumentTypes } from "../hooks/useDocumentTypes";
import {
  uploadDocumentFormSchema,
  type UploadDocumentFormValues,
} from "../types/document-form.schema";
import type { DocumentType } from "../types/document.types";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

const FILE_ACCEPT =
  ".pdf,.doc,.docx,.jpg,.jpeg,.png,.gif,.webp,.bmp,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,image/*";

type UploadDocumentModalProps = {
  open: boolean;
  employeeId: number;
  onClose: () => void;
  onUploaded: () => void;
};

export function UploadDocumentModal({ open, employeeId, onClose, onUploaded }: UploadDocumentModalProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { create } = useDocumentMutations(employeeId);
  const typesQuery = useDocumentTypes(open);
  const types = typesQuery.data ?? [];

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<UploadDocumentFormValues>({
    resolver: zodResolver(uploadDocumentFormSchema) as Resolver<UploadDocumentFormValues>,
    defaultValues: {
      name: "",
      documentTypeId: undefined,
      issueDate: "",
      expiryDate: "",
    },
  });

  const selectedFile = watch("file");

  const closeAndReset = () => {
    reset();
    if (fileInputRef.current) fileInputRef.current.value = "";
    onClose();
  };

  const onSubmit = handleSubmit((values) => {
    create.mutate(
      {
        employeeId,
        documentTypeId: values.documentTypeId,
        name: values.name.trim(),
        issueDate: values.issueDate || undefined,
        expiryDate: values.expiryDate || undefined,
        file: values.file,
      },
      {
        onSuccess: () => {
          toast.success("Document uploaded.");
          closeAndReset();
          onUploaded();
        },
        onError: (e) => toast.error(getErrorMessage(e)),
      },
    );
  });

  return (
    <Modal
      open={open}
      title="Upload document"
      onClose={closeAndReset}
      className="max-w-lg"
      footer={
        <>
          <Button type="button" variant="secondary" onClick={closeAndReset} disabled={create.isPending}>
            Cancel
          </Button>
          <Button type="submit" form="upload-document-form" disabled={create.isPending || typesQuery.isLoading}>
            {create.isPending ? "Uploading…" : "Upload"}
          </Button>
        </>
      }
    >
      <form id="upload-document-form" className="space-y-4" onSubmit={(e) => void onSubmit(e)}>
        <div>
          <label htmlFor="doc-upload-name" className="text-sm font-medium text-foreground">
            Name
          </label>
          <input id="doc-upload-name" className={inputClass} {...register("name")} />
          {errors.name ? <p className="mt-1 text-xs text-destructive">{errors.name.message}</p> : null}
        </div>

        <div>
          <label htmlFor="doc-upload-type" className="text-sm font-medium text-foreground">
            Document type
          </label>
          <select
            id="doc-upload-type"
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
            <label htmlFor="doc-upload-issue" className="text-sm font-medium text-foreground">
              Issue date
            </label>
            <input id="doc-upload-issue" type="date" className={inputClass} {...register("issueDate")} />
          </div>
          <div>
            <label htmlFor="doc-upload-expiry" className="text-sm font-medium text-foreground">
              Expiry date
            </label>
            <input id="doc-upload-expiry" type="date" className={inputClass} {...register("expiryDate")} />
          </div>
        </div>

        <div>
          <span className="text-sm font-medium text-foreground">File</span>
          <input
            ref={fileInputRef}
            type="file"
            accept={FILE_ACCEPT}
            className="sr-only"
            id="doc-upload-file"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) setValue("file", file, { shouldValidate: true });
            }}
          />
          <div className="mt-1 flex flex-wrap items-center gap-2">
            <label
              htmlFor="doc-upload-file"
              className="cursor-pointer rounded-lg border border-input bg-background px-3 py-1.5 text-xs font-medium text-foreground shadow-sm hover:bg-muted/50 dark:bg-card dark:hover:bg-muted/60"
            >
              Choose file
            </label>
            <span className="text-sm text-muted-foreground">
              {selectedFile instanceof File ? selectedFile.name : "No file chosen"}
            </span>
          </div>
          <p className="mt-1 text-xs text-muted-foreground">PDF, Word, or image up to 16 MB.</p>
          {errors.file ? <p className="mt-1 text-xs text-destructive">{errors.file.message as string}</p> : null}
        </div>
      </form>
    </Modal>
  );
}
