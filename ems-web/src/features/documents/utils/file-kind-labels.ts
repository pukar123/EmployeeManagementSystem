import { DocumentFileKind } from "../types/document.types";

const labels: Record<DocumentFileKind, string> = {
  [DocumentFileKind.None]: "Unknown",
  [DocumentFileKind.Pdf]: "PDF",
  [DocumentFileKind.Word]: "Word",
  [DocumentFileKind.Image]: "Image",
};

export function fileKindLabel(kind: DocumentFileKind): string {
  return labels[kind] ?? "Unknown";
}
