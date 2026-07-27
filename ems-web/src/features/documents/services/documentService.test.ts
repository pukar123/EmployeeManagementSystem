import type { AxiosAdapter, InternalAxiosRequestConfig } from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/shared/auth/auth-storage", () => ({
  getAccessToken: vi.fn(() => "access-token"),
}));

type PostFormDataMock = (path: string, formData: FormData) => Promise<unknown>;

const postFormDataMock = vi.fn<PostFormDataMock>(async () => ({
  id: 1,
  employeeId: 5,
  documentTypeId: 2,
  documentTypeName: "Contract",
  name: "Employment contract",
  issueDate: null,
  expiryDate: null,
  fileKind: 1,
  originalFileName: "contract.pdf",
  contentType: "application/pdf",
  storedRelativePath: "/attachments/Employee/5/Documents/x.pdf",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
}));

vi.mock("@/shared/api/http-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/shared/api/http-client")>();
  return {
    ...actual,
    postFormData: (path: string, formData: FormData) => postFormDataMock(path, formData),
  };
});

import { emsHttpClient } from "@/shared/api/http-client";
import { documentService } from "./documentService";
import { documentFileApiUrl, downloadDocumentFile } from "../utils/document-download";
import { fileKindLabel } from "../utils/file-kind-labels";
import { DocumentFileKind } from "../types/document.types";

const EMS_ORIGIN = "http://ems.test";

type Captured = { method: string; url: string; data?: unknown; params?: unknown };

function installCapture(client: typeof emsHttpClient, bucket: Captured[]): void {
  const adapter: AxiosAdapter = async (config: InternalAxiosRequestConfig) => {
    const method = (config.method ?? "get").toUpperCase();
    const baseURL = config.baseURL ?? "";
    const path = config.url ?? "";
    const url = `${baseURL.replace(/\/$/, "")}${path.startsWith("/") ? path : `/${path}`}`;
    bucket.push({ method, url, data: config.data, params: config.params });
    return {
      data: method === "GET" && path.includes("/types") ? [{ id: 1, name: "Contract", isActive: true }] : {},
      status: 200,
      statusText: "OK",
      headers: {},
      config,
    };
  };
  client.defaults.adapter = adapter;
}

describe("documentService", () => {
  const requests: Captured[] = [];

  beforeEach(() => {
    requests.length = 0;
    postFormDataMock.mockClear();
    installCapture(emsHttpClient, requests);
  });

  it("routes list and types requests to EMS", async () => {
    await documentService.getTypes();
    await documentService.listByEmployee(5);

    expect(requests.map((r) => ({ method: r.method, url: r.url }))).toEqual([
      { method: "GET", url: `${EMS_ORIGIN}/api/Documents/types` },
      { method: "GET", url: `${EMS_ORIGIN}/api/Documents` },
    ]);
    expect(requests[1]?.params).toEqual({ employeeId: 5 });
  });

  it("routes update and delete to EMS", async () => {
    await documentService.update(3, {
      name: "Updated",
      documentTypeId: 1,
      employeeId: 5,
      issueDate: null,
      expiryDate: null,
    });
    await documentService.delete(3);

    expect(requests.map((r) => ({ method: r.method, url: r.url }))).toEqual([
      { method: "PUT", url: `${EMS_ORIGIN}/api/Documents/3` },
      { method: "DELETE", url: `${EMS_ORIGIN}/api/Documents/3` },
    ]);
  });

  it("builds multipart create payload with expected field names", async () => {
    const file = new File(["pdf"], "contract.pdf", { type: "application/pdf" });
    await documentService.create({
      employeeId: 5,
      documentTypeId: 2,
      name: "Employment contract",
      issueDate: "2026-01-15",
      expiryDate: "2027-01-15",
      file,
    });

    expect(postFormDataMock).toHaveBeenCalledOnce();
    const [path, formData] = postFormDataMock.mock.calls[0];
    expect(path).toBe("/api/Documents");
    expect(formData.get("employeeId")).toBe("5");
    expect(formData.get("documentTypeId")).toBe("2");
    expect(formData.get("name")).toBe("Employment contract");
    expect(formData.get("issueDate")).toBe("2026-01-15");
    expect(formData.get("expiryDate")).toBe("2027-01-15");
    expect(formData.get("file")).toBe(file);
  });
});

describe("document download helpers", () => {
  it("builds authenticated file URL", () => {
    expect(documentFileApiUrl(42)).toBe(`${EMS_ORIGIN}/api/Documents/42/file`);
  });

  it("downloads blob with filename from content-disposition", async () => {
    const blob = new Blob(["content"], { type: "application/pdf" });
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        blob: async () => blob,
        headers: {
          get: (name: string) =>
            name.toLowerCase() === "content-disposition" ? 'attachment; filename="signed.pdf"' : null,
        },
      })),
    );

    const result = await downloadDocumentFile(7, "fallback.pdf");
    expect(result.blob).toBe(blob);
    expect(result.fileName).toBe("signed.pdf");

    vi.unstubAllGlobals();
  });
});

describe("fileKindLabel", () => {
  it("maps known kinds to display labels", () => {
    expect(fileKindLabel(DocumentFileKind.Pdf)).toBe("PDF");
    expect(fileKindLabel(DocumentFileKind.Word)).toBe("Word");
    expect(fileKindLabel(DocumentFileKind.Image)).toBe("Image");
  });
});
