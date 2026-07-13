import { getAccessToken } from "@/shared/auth/auth-storage";
import { emsApiBaseUrl } from "@/shared/api/http-client";

export type DocumentDownloadResult = {
  blob: Blob;
  fileName: string;
};

function parseContentDispositionFileName(header: string | null): string | null {
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

export function documentFileApiUrl(documentId: number): string {
  const base = emsApiBaseUrl.replace(/\/$/, "");
  return `${base}/api/Documents/${documentId}/file`;
}

export async function downloadDocumentFile(
  documentId: number,
  fallbackFileName: string,
): Promise<DocumentDownloadResult> {
  const headers: HeadersInit = {};
  const token = getAccessToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const res = await fetch(documentFileApiUrl(documentId), { method: "GET", headers });
  if (!res.ok) {
    const text = await res.text();
    let message = `Download failed (${res.status})`;
    try {
      const body = JSON.parse(text) as unknown;
      if (body && typeof body === "object" && body !== null && "message" in body) {
        const m = (body as { message?: string }).message;
        if (typeof m === "string") message = m;
      }
    } catch {
      if (text.length > 0 && text.length < 500) message = text;
    }
    throw new Error(message);
  }

  const blob = await res.blob();
  const fileName =
    parseContentDispositionFileName(res.headers.get("content-disposition")) ?? fallbackFileName;
  return { blob, fileName };
}

export function triggerBlobDownload(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}
