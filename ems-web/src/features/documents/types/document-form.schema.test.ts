import { describe, expect, it } from "vitest";
import { uploadDocumentFormSchema } from "./document-form.schema";

const MB = 1024 * 1024;

function fileOf(name: string, type: string, size = 10): File {
  const file = new File(["x"], name, { type });
  Object.defineProperty(file, "size", { value: size });
  return file;
}

function validate(file: File) {
  return uploadDocumentFormSchema.safeParse({ name: "Contract", documentTypeId: 1, file });
}

function fileErrors(file: File): string[] {
  const result = validate(file);
  return result.success ? [] : result.error.issues.filter((i) => i.path[0] === "file").map((i) => i.message);
}

describe("uploadDocumentFormSchema file rules (B10)", () => {
  it.each([
    ["a.pdf", "application/pdf"],
    ["a.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
    ["a.png", "image/png"],
    ["a.jpg", "image/jpeg"],
    ["a.jpeg", "image/jpeg"],
    ["A.PDF", ""],
  ])("accepts %s", (name, type) => {
    expect(validate(fileOf(name, type)).success).toBe(true);
  });

  it.each([
    ["a.gif", "image/gif"],
    ["a.doc", "application/msword"],
    ["a.bmp", "image/bmp"],
    ["a.webp", "image/webp"],
    ["a.svg", "image/svg+xml"],
    ["a.pdf", "text/html"],
  ])("rejects %s (%s) with the friendly message", (name, type) => {
    expect(fileErrors(fileOf(name, type))).toContain("File must be a PDF, Word (.docx), PNG or JPEG file");
  });

  it("rejects a file over 15 MB and accepts exactly 15 MB", () => {
    expect(fileErrors(fileOf("a.pdf", "application/pdf", 15 * MB + 1))).toContain("File must be 15 MB or smaller");
    expect(validate(fileOf("a.pdf", "application/pdf", 15 * MB)).success).toBe(true);
  });
});
