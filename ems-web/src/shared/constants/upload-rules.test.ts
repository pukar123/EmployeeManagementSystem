import { describe, expect, it } from "vitest";
import {
  DOCUMENT_UPLOAD_RULE,
  fileExtension,
  isAllowedLogoFile,
  LEAVE_ATTACHMENT_UPLOAD_RULE,
  LOGO_UPLOAD_RULE,
} from "@/shared/constants/upload-rules";

const MB = 1024 * 1024;

function fakeFile(name: string, type: string, size = 10) {
  return { name, type, size };
}

describe("upload rules (B10)", () => {
  it("mirrors the server's allowed types and sizes", () => {
    expect(DOCUMENT_UPLOAD_RULE.accept).toBe(
      ".pdf,.docx,.png,.jpg,.jpeg,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document,image/png,image/jpeg",
    );
    expect(DOCUMENT_UPLOAD_RULE.accept).not.toContain("image/*");
    expect(DOCUMENT_UPLOAD_RULE.maxBytes).toBe(15 * MB);
    expect(LEAVE_ATTACHMENT_UPLOAD_RULE.accept).toBe(".pdf,.png,.jpg,.jpeg,application/pdf,image/png,image/jpeg");
    expect(LEAVE_ATTACHMENT_UPLOAD_RULE.maxBytes).toBe(15 * MB);
    expect(LOGO_UPLOAD_RULE.accept).toBe("image/png,image/jpeg,image/webp,.png,.jpg,.jpeg,.webp");
    expect(LOGO_UPLOAD_RULE.maxBytes).toBe(5 * MB);
  });

  it("reads the last extension in lower case", () => {
    expect(fileExtension("A.PDF")).toBe(".pdf");
    expect(fileExtension("report.pdf.exe")).toBe(".exe");
    expect(fileExtension("noextension")).toBe("");
  });
});

describe("logo pre-check", () => {
  it.each([
    ["logo.png", "image/png"],
    ["logo.jpg", "image/jpeg"],
    ["logo.JPEG", "image/jpeg"],
    ["logo.webp", "image/webp"],
  ])("accepts %s", (name, type) => {
    expect(isAllowedLogoFile(fakeFile(name, type))).toBe(true);
  });

  it.each([
    ["logo.svg", "image/svg+xml"],
    ["logo.gif", "image/gif"],
    ["logo.png", "image/svg+xml"],
    ["logo.bmp", "image/bmp"],
  ])("rejects %s (%s)", (name, type) => {
    expect(isAllowedLogoFile(fakeFile(name, type))).toBe(false);
  });

  it("rejects a file over 5 MB and accepts exactly 5 MB", () => {
    expect(isAllowedLogoFile(fakeFile("logo.png", "image/png", 5 * MB + 1))).toBe(false);
    expect(isAllowedLogoFile(fakeFile("logo.png", "image/png", 5 * MB))).toBe(true);
  });

  it("rejects an empty file", () => {
    expect(isAllowedLogoFile(fakeFile("logo.png", "image/png", 0))).toBe(false);
  });
});
