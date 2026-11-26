import axios from "axios";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("@/shared/auth/auth-storage", () => ({
  saveAuthResponse: vi.fn(),
  clearAuth: vi.fn(),
  getAccessToken: vi.fn(() => "access-token"),
  getRefreshToken: vi.fn(() => "refresh-token"),
  setAuthChangeHandler: vi.fn(),
}));

import { FILE_TOO_LARGE_MESSAGE, getErrorMessage, postFormData } from "@/shared/api/http-client";

function axiosErrorWith(status: number, data: unknown) {
  const config = { baseURL: "http://elsewhere.test", url: "/api/Documents" };
  return new axios.AxiosError("Request failed with status code " + status, "ERR_BAD_REQUEST", config as never, null, {
    status,
    data,
    headers: {},
    statusText: "Error",
    config: config as never,
  });
}

describe("getErrorMessage for uploads (B10)", () => {
  it("shows the server message for a 413 with a message", () => {
    const error = axiosErrorWith(413, { code: "file_too_large", message: "The file is too large. The maximum size is 15 MB." });
    expect(getErrorMessage(error)).toBe("The file is too large. The maximum size is 15 MB.");
  });

  it("shows the friendly text for a 413 without a body", () => {
    expect(getErrorMessage(axiosErrorWith(413, ""))).toBe(FILE_TOO_LARGE_MESSAGE);
    expect(getErrorMessage(axiosErrorWith(413, undefined))).toBe("The file is too large to upload.");
  });

  it("shows the friendly text for a 413 with a proxy page", () => {
    expect(getErrorMessage(axiosErrorWith(413, "<html><body>413 Request Entity Too Large</body></html>"))).toBe(
      FILE_TOO_LARGE_MESSAGE,
    );
  });

  it("still shows the server message for a 400", () => {
    const error = axiosErrorWith(400, {
      code: "file_type_not_allowed",
      message: "This file type is not allowed. Allowed types: PDF, PNG, JPEG.",
    });
    expect(getErrorMessage(error)).toBe("This file type is not allowed. Allowed types: PDF, PNG, JPEG.");
  });
});

describe("postFormData errors (B10)", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  async function messageFor(status: number, body: string): Promise<string> {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => new Response(body, { status })),
    );
    try {
      await postFormData("/api/Documents", new FormData());
    } catch (error) {
      return getErrorMessage(error);
    }
    throw new Error("postFormData should have failed");
  }

  it("uses the friendly text for a 413 with no body", async () => {
    expect(await messageFor(413, "")).toBe(FILE_TOO_LARGE_MESSAGE);
  });

  it("uses the server message for a 413 with a message", async () => {
    const body = JSON.stringify({ code: "request_too_large", message: "The file is too large. The maximum size is 5 MB." });
    expect(await messageFor(413, body)).toBe("The file is too large. The maximum size is 5 MB.");
  });

  it("uses the server message for a 400", async () => {
    const body = JSON.stringify({ code: "file_empty", message: "The file is empty." });
    expect(await messageFor(400, body)).toBe("The file is empty.");
  });
});
