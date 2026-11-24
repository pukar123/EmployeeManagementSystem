import { downloadApiFile, type DownloadedFile } from "@/shared/api/download-file";
import type { Document } from "../types/document.types";

export type DocumentDownloadResult = DownloadedFile;

/** Downloads a document through its authorised `downloadUrl`, with the original file name as a fallback. */
export function downloadDocumentFile(doc: Pick<Document, "downloadUrl" | "originalFileName">): Promise<DocumentDownloadResult> {
  return downloadApiFile(doc.downloadUrl, doc.originalFileName);
}
