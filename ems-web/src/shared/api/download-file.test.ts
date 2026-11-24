import { AxiosError, type AxiosAdapter, type InternalAxiosRequestConfig } from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/shared/auth/auth-storage", () => ({
  getAccessToken: vi.fn(() => "access-token"),
  getRefreshToken: vi.fn(() => null),
  clearAuth: vi.fn(),
  saveAuthResponse: vi.fn(),
}));

import { emsHttpClient } from "@/shared/api/http-client";
import { downloadApiFile, parseContentDispositionFileName, triggerBlobDownload } from "./download-file";

const EMS_ORIGIN = "http://ems.test";

type Seen = { url: string; method: string; responseType?: string; authorization?: unknown };

function respondWith(
  seen: Seen[],
  reply: (config: InternalAxiosRequestConfig) => { status: number; data: unknown; headers?: Record<string, string> },
): void {
  const adapter: AxiosAdapter = async (config) => {
    seen.push({
      url: `${(config.baseURL ?? "").replace(/\/$/, "")}${config.url ?? ""}`,
      method: (config.method ?? "get").toUpperCase(),
      responseType: config.responseType,
      authorization: config.headers?.Authorization,
    });
    const { status, data, headers } = reply(config);
    const response = { data, status, statusText: String(status), headers: headers ?? {}, config };
    if (status >= 400) {
      throw new AxiosError(`Request failed with status code ${status}`, "ERR_BAD_RESPONSE", config, null, response);
    }
    return response;
  };
  emsHttpClient.defaults.adapter = adapter;
}

describe("downloadApiFile", () => {
  const seen: Seen[] = [];

  beforeEach(() => {
    seen.length = 0;
  });

  it("requests the path through emsHttpClient as a blob with the bearer token", async () => {
    const blob = new Blob(["pdf"], { type: "application/pdf" });
    respondWith(seen, () => ({ status: 200, data: blob }));

    const result = await downloadApiFile("/api/LeaveAttachments/12/file", "evidence.pdf");

    expect(seen).toEqual([
      {
        url: `${EMS_ORIGIN}/api/LeaveAttachments/12/file`,
        method: "GET",
        responseType: "blob",
        authorization: "Bearer access-token",
      },
    ]);
    expect(result.blob).toBe(blob);
  });

  it("takes the file name from filename* when present", async () => {
    respondWith(seen, () => ({
      status: 200,
      data: new Blob(["x"]),
      headers: { "content-disposition": "attachment; filename=\"plain.pdf\"; filename*=UTF-8''Vertrag%20%C3%BC.pdf" },
    }));

    const result = await downloadApiFile("/api/Documents/1/file", "fallback.pdf");

    expect(result.fileName).toBe("Vertrag ü.pdf");
  });

  it("takes the file name from plain filename", async () => {
    respondWith(seen, () => ({
      status: 200,
      data: new Blob(["x"]),
      headers: { "content-disposition": 'attachment; filename="contract.pdf"' },
    }));

    const result = await downloadApiFile("/api/Documents/1/file", "fallback.pdf");

    expect(result.fileName).toBe("contract.pdf");
  });

  it("uses the fallback name when there is no content-disposition header", async () => {
    respondWith(seen, () => ({ status: 200, data: new Blob(["x"]) }));

    const result = await downloadApiFile("/api/Documents/1/file", "fallback.pdf");

    expect(result.fileName).toBe("fallback.pdf");
  });

  it("throws the API message from a JSON error blob", async () => {
    respondWith(seen, () => ({
      status: 400,
      data: new Blob([JSON.stringify({ message: "Leave request was not found." })], { type: "application/json" }),
    }));

    await expect(downloadApiFile("/api/LeaveAttachments/9/file", "x.pdf")).rejects.toThrow(
      "Leave request was not found.",
    );
  });

  it("throws a status message when the error body has no message", async () => {
    respondWith(seen, () => ({ status: 404, data: new Blob([""]) }));

    await expect(downloadApiFile("/api/Documents/9/file", "x.pdf")).rejects.toThrow("Download failed (404)");
  });
});

describe("parseContentDispositionFileName", () => {
  it("returns null without a header", () => {
    expect(parseContentDispositionFileName(null)).toBeNull();
    expect(parseContentDispositionFileName(undefined)).toBeNull();
  });
});

describe("triggerBlobDownload", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it("clicks a download link and revokes the object URL after the click", () => {
    vi.useFakeTimers();
    const click = vi.fn();
    const anchor = { href: "", download: "", click };
    vi.stubGlobal("document", { createElement: vi.fn(() => anchor) });
    const createObjectURL = vi.fn(() => "blob:ems/1");
    const revokeObjectURL = vi.fn();
    vi.stubGlobal("URL", { createObjectURL, revokeObjectURL });

    triggerBlobDownload(new Blob(["x"]), "contract.pdf");

    expect(anchor.download).toBe("contract.pdf");
    expect(anchor.href).toBe("blob:ems/1");
    expect(click).toHaveBeenCalledOnce();
    expect(revokeObjectURL).not.toHaveBeenCalled();
    vi.runAllTimers();
    expect(revokeObjectURL).toHaveBeenCalledWith("blob:ems/1");
  });
});
