import { z } from "zod";

export const MAX_DOCUMENT_FILE_BYTES = 16 * 1024 * 1024;

const allowedExtensions = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"];

function isAllowedFile(file: File): boolean {
  const name = file.name.toLowerCase();
  return allowedExtensions.some((ext) => name.endsWith(ext));
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
    .refine((file) => file.size <= MAX_DOCUMENT_FILE_BYTES, "File must be 16 MB or smaller")
    .refine(isAllowedFile, "File must be PDF, Word, or an image"),
});

export type UploadDocumentFormValues = z.infer<typeof uploadDocumentFormSchema>;

export const editDocumentFormSchema = z.object({
  name: z.string().trim().min(1, "Name is required"),
  documentTypeId: z.coerce.number().int().positive("Document type is required"),
  issueDate: optionalDateString,
  expiryDate: optionalDateString,
});

export type EditDocumentFormValues = z.infer<typeof editDocumentFormSchema>;
