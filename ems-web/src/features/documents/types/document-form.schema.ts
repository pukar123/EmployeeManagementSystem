import { z } from "zod";
import { DOCUMENT_UPLOAD_RULE, hasAllowedType } from "@/shared/constants/upload-rules";

/** 15 MB, the server's limit for employee documents (B10). */
export const MAX_DOCUMENT_FILE_BYTES = DOCUMENT_UPLOAD_RULE.maxBytes;

function isAllowedFile(file: File): boolean {
  return hasAllowedType(file, DOCUMENT_UPLOAD_RULE);
}

const optionalDateString = z.string().optional().or(z.literal(""));

export const uploadDocumentFormSchema = z.object({
  name: z.string().trim().min(1, "Name is required"),
  documentTypeId: z.coerce.number().int().positive("Document type is required"),
  issueDate: optionalDateString,
  expiryDate: optionalDateString,
  file: z
    .custom<File>((val) => val instanceof File, "A file is required")
    .refine((file) => file.size > 0, "A file is required")
    .refine((file) => file.size <= MAX_DOCUMENT_FILE_BYTES, "File must be 15 MB or smaller")
    .refine(isAllowedFile, "File must be a PDF, Word (.docx), PNG or JPEG file"),
});

export type UploadDocumentFormValues = z.infer<typeof uploadDocumentFormSchema>;

export const editDocumentFormSchema = z.object({
  name: z.string().trim().min(1, "Name is required"),
  documentTypeId: z.coerce.number().int().positive("Document type is required"),
  issueDate: optionalDateString,
  expiryDate: optionalDateString,
});

export type EditDocumentFormValues = z.infer<typeof editDocumentFormSchema>;
