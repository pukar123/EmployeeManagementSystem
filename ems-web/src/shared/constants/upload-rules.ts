/**
 * Upload rules per purpose (B10), mirroring the server's `UploadRules`. The server is the real check (it also
 * checks each file's content signature); the browser check only saves a round trip. Components and schemas import
 * from here; do not copy these lists.
 */

const ONE_MEGABYTE = 1024 * 1024;

export type UploadRule = {
  /** Value for the file input's `accept` attribute. */
  accept: string;
  /** Allowed extensions, lower case with the dot. */
  extensions: readonly string[];
  /** Allowed content types, lower case. An empty `File.type` is allowed when the extension matches. */
  contentTypes: readonly string[];
  maxBytes: number;
  /** Short help text shown under the picker. */
  hint: string;
};

export const DOCUMENT_UPLOAD_RULE: UploadRule = {
  accept:
    ".pdf,.docx,.png,.jpg,.jpeg,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document,image/png,image/jpeg",
  extensions: [".pdf", ".docx", ".png", ".jpg", ".jpeg"],
  contentTypes: [
    "application/pdf",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "image/png",
    "image/jpeg",
    "image/jpg",
  ],
  maxBytes: 15 * ONE_MEGABYTE,
  hint: "PDF, Word (.docx), PNG or JPEG, up to 15 MB.",
};

/** For a future leave attachment picker (none exists yet). */
export const LEAVE_ATTACHMENT_UPLOAD_RULE: UploadRule = {
  accept: ".pdf,.png,.jpg,.jpeg,application/pdf,image/png,image/jpeg",
  extensions: [".pdf", ".png", ".jpg", ".jpeg"],
  contentTypes: ["application/pdf", "image/png", "image/jpeg", "image/jpg"],
  maxBytes: 15 * ONE_MEGABYTE,
  hint: "PDF, PNG or JPEG, up to 15 MB.",
};

export const LOGO_UPLOAD_RULE: UploadRule = {
  accept: "image/png,image/jpeg,image/webp,.png,.jpg,.jpeg,.webp",
  extensions: [".png", ".jpg", ".jpeg", ".webp"],
  contentTypes: ["image/png", "image/jpeg", "image/jpg", "image/webp"],
  maxBytes: 5 * ONE_MEGABYTE,
  hint: "PNG, JPEG or WebP, up to 5 MB.",
};

export const LOGO_FILE_INVALID_MESSAGE = "Logo must be a PNG, JPEG or WebP image, 5 MB or smaller.";

/** Lower-case last extension of a file name, with the dot (`"A.PDF"` gives `".pdf"`), or `""`. */
export function fileExtension(name: string): string {
  const dot = name.lastIndexOf(".");
  return dot < 0 ? "" : name.slice(dot).toLowerCase();
}

/** True when the file's extension is allowed and its browser-reported type (if any) is allowed. */
export function hasAllowedType(file: Pick<File, "name" | "type">, rule: UploadRule): boolean {
  if (!rule.extensions.includes(fileExtension(file.name))) return false;
  const type = file.type.toLowerCase();
  return type === "" || rule.contentTypes.includes(type);
}

/** True when the file is not empty and not over the rule's maximum. */
export function hasAllowedSize(file: Pick<File, "size">, rule: UploadRule): boolean {
  return file.size > 0 && file.size <= rule.maxBytes;
}

/** Browser pre-check for a logo: PNG, JPEG or WebP, 5 MB or smaller. */
export function isAllowedLogoFile(file: Pick<File, "name" | "type" | "size">): boolean {
  return hasAllowedType(file, LOGO_UPLOAD_RULE) && hasAllowedSize(file, LOGO_UPLOAD_RULE);
}
