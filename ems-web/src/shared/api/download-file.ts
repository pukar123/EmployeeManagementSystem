import axios from "axios";
import { emsHttpClient, getErrorMessage, type ApiErrorBody } from "@/shared/api/http-client";

export type DownloadedFile = {
  blob: Blob;
  fileName: string;
};

/** Reads the file name from a `Content-Disposition` header, preferring the UTF-8 `filename*` form. */
export function parseContentDispositionFileName(header: string | null | undefined): string | null {
  if (!header) return null;
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1]);
    } catch {
      return utf8Match[1];
    }
  }
  const plainMatch = /filename="?([^";]+)"?/i.exec(header);
  return plainMatch?.[1] ?? null;
}

async function readErrorMessage(error: unknown): Promise<string> {
  if (axios.isAxiosError(error) && error.response) {
    const data: unknown = error.response.data;
    let text: string | null = null;
    if (typeof Blob !== "undefined" && data instanceof Blob) {
      text = await data.text();
    } else if (typeof data === "string") {
      text = data;
    } else if (data && typeof data === "object" && "message" in data) {
      const m = (data as ApiErrorBody).message;
      if (typeof m === "string" && m) return m;
    }

    if (text) {
      try {
        const body = JSON.parse(text) as unknown;
        if (body && typeof body === "object" && "message" in body) {
          const m = (body as ApiErrorBody).message;
          if (typeof m === "string" && m) return m;
        }
      } catch {
        // Not JSON: fall through to the status message.
      }
    }
    return `Download failed (${error.response.status})`;
  }
  return getErrorMessage(error);
}

/**
 * Downloads a private file from an authorised EMS.API endpoint (for example a response's `downloadUrl`).
 * Goes through `emsHttpClient`, so the bearer token is attached and an expired token is refreshed.
 * Never put these URLs in a plain link or `<img src>`: the browser would not send the token.
 */
export async function downloadApiFile(path: string, fallbackFileName: string): Promise<DownloadedFile> {
  try {
    const response = await emsHttpClient.get<Blob>(path, { responseType: "blob" });
    const header = response.headers["content-disposition"] as string | undefined;
    const fileName = parseContentDispositionFileName(header) ?? fallbackFileName;
    return { blob: response.data, fileName };
  } catch (error) {
    throw new Error(await readErrorMessage(error));
  }
}

/** Saves a blob in the browser under `fileName`. */
export function triggerBlobDownload(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  // Revoke after the click has been handled, so Safari does not cancel the download.
  setTimeout(() => URL.revokeObjectURL(url), 0);
}
