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
  employeeDisplayName: "Eli Report",
  documentTypeName: "Contract",
  name: "Employment contract",
  issueDate: null,
  expiryDate: null,
  fileKind: 1,
  originalFileName: "contract.pdf",
  contentType: "application/pdf",
  downloadUrl: "/api/Documents/1/file",
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
import { downloadDocumentFile } from "../utils/document-download";
import { fileKindLabel } from "../utils/file-kind-labels";
import { documentKeys } from "./query-keys";
import { isForbiddenError, pagingState } from "../utils/document-paging";
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

  it("routes list and types requests to EMS with paging parameters", async () => {
    await documentService.getTypes();
    await documentService.listByEmployee(5, { page: 2, pageSize: 25 });
    await documentService.listByEmployee(5, { page: 1, pageSize: 50, documentTypeId: 3 });
    await documentService.list({ page: 1, pageSize: 100 });
    await documentService.list({ page: 3, pageSize: 25, documentTypeId: 4 });

    expect(requests.map((r) => ({ method: r.method, url: r.url }))).toEqual([
      { method: "GET", url: `${EMS_ORIGIN}/api/Documents/types` },
      { method: "GET", url: `${EMS_ORIGIN}/api/Documents` },
      { method: "GET", url: `${EMS_ORIGIN}/api/Documents` },
      { method: "GET", url: `${EMS_ORIGIN}/api/Documents` },
      { method: "GET", url: `${EMS_ORIGIN}/api/Documents` },
    ]);
    expect(requests[1]?.params).toEqual({ employeeId: 5, page: 2, pageSize: 25 });
    expect(requests[2]?.params).toEqual({ employeeId: 5, page: 1, pageSize: 50, documentTypeId: 3 });
    // The all-documents list never sends an employeeId.
    expect(requests[3]?.params).toEqual({ page: 1, pageSize: 100 });
    expect(requests[4]?.params).toEqual({ page: 3, pageSize: 25, documentTypeId: 4 });
  });

  it("returns the paged shape from the list endpoint", async () => {
    const paged = {
      items: [],
      totalCount: 30,
      page: 2,
      pageSize: 25,
      totalPages: 2,
    };
    emsHttpClient.defaults.adapter = async (config: InternalAxiosRequestConfig) => ({
      data: paged,
      status: 200,
      statusText: "OK",
      headers: {},
      config,
    });

    await expect(documentService.listByEmployee(5, { page: 2, pageSize: 25 })).resolves.toEqual(paged);
    await expect(documentService.list({ page: 2, pageSize: 25 })).resolves.toEqual(paged);
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
  it("requests the document's downloadUrl through emsHttpClient as a blob", async () => {
    const blob = new Blob(["content"], { type: "application/pdf" });
    const seen: Array<{ url: string; responseType?: string; authorization?: unknown }> = [];
    emsHttpClient.defaults.adapter = async (config: InternalAxiosRequestConfig) => {
      seen.push({
        url: `${(config.baseURL ?? "").replace(/\/$/, "")}${config.url ?? ""}`,
        responseType: config.responseType,
        authorization: config.headers?.Authorization,
      });
      return {
        data: blob,
        status: 200,
        statusText: "OK",
        headers: { "content-disposition": 'attachment; filename="signed.pdf"' },
        config,
      };
    };

    const result = await downloadDocumentFile({ downloadUrl: "/api/Documents/7/file", originalFileName: "fallback.pdf" });

    expect(seen).toEqual([
      { url: `${EMS_ORIGIN}/api/Documents/7/file`, responseType: "blob", authorization: "Bearer access-token" },
    ]);
    expect(result.blob).toBe(blob);
    expect(result.fileName).toBe("signed.pdf");
  });
});

describe("fileKindLabel", () => {
  it("maps known kinds to display labels", () => {
    expect(fileKindLabel(DocumentFileKind.Pdf)).toBe("PDF");
    expect(fileKindLabel(DocumentFileKind.Word)).toBe("Word");
    expect(fileKindLabel(DocumentFileKind.Image)).toBe("Image");
  });
});

describe("document paging helpers", () => {
  it("shows Previous/Next only when there is more than one page", () => {
    expect(pagingState(1, 1)).toEqual({ showPaging: false, canGoBack: false, canGoForward: false });
    expect(pagingState(1, 2)).toEqual({ showPaging: true, canGoBack: false, canGoForward: true });
    expect(pagingState(2, 2)).toEqual({ showPaging: true, canGoBack: true, canGoForward: false });
  });

  it("recognises a 403 refusal only", () => {
    expect(isForbiddenError({ response: { status: 403 } })).toBe(true);
    expect(isForbiddenError({ response: { status: 404 } })).toBe(false);
    expect(isForbiddenError(new Error("network"))).toBe(false);
    expect(isForbiddenError(null)).toBe(false);
  });

  it("puts the page in the query key under the employee prefix", () => {
    const pageKey = documentKeys.byEmployeePage(5, { page: 2, pageSize: 25 });
    expect(pageKey.slice(0, 3)).toEqual([...documentKeys.byEmployee(5)]);
    expect(pageKey[3]).toEqual({ page: 2, pageSize: 25 });
    expect(documentKeys.allListPage({ page: 1, pageSize: 25 }).slice(0, 2)).toEqual([...documentKeys.allList()]);
  });
});
