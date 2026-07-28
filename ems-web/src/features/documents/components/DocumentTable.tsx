"use client";

import { Button } from "@/shared/components/Button";
import { Badge } from "@/shared/components/Badge";
import {
  DataTable,
  DataTableBody,
  DataTableCell,
  DataTableElement,
  DataTableHead,
  DataTableHeaderCell,
  DataTableRow,
} from "@/shared/components/DataTable";
import { cn } from "@/shared/utils/cn";
import type { Document } from "../types/document.types";
import { fileKindLabel } from "../utils/file-kind-labels";
import { formatDocumentDate, isPastDate } from "../utils/format-document-date";

type DocumentTableProps = {
  documents: Document[];
  canManage: boolean;
  downloadBusyId: number | null;
  onDownload: (doc: Document) => void;
  onEdit: (doc: Document) => void;
  onDelete: (doc: Document) => void;
};

export function DocumentTable({
  documents,
  canManage,
  downloadBusyId,
  onDownload,
  onEdit,
  onDelete,
}: DocumentTableProps) {
  return (
    <div className="overflow-x-auto">
      <DataTable isEmpty={documents.length === 0} emptyMessage="No documents uploaded yet.">
        <DataTableElement>
          <DataTableHead>
            <tr>
              <DataTableHeaderCell>Name</DataTableHeaderCell>
              <DataTableHeaderCell>Type</DataTableHeaderCell>
              <DataTableHeaderCell>File kind</DataTableHeaderCell>
              <DataTableHeaderCell>File</DataTableHeaderCell>
              <DataTableHeaderCell>Issue date</DataTableHeaderCell>
              <DataTableHeaderCell>Expiry date</DataTableHeaderCell>
              <DataTableHeaderCell>Actions</DataTableHeaderCell>
            </tr>
          </DataTableHead>
          <DataTableBody>
            {documents.map((doc) => (
              <DataTableRow key={doc.id}>
                <DataTableCell className="font-medium text-foreground">{doc.name}</DataTableCell>
                <DataTableCell>{doc.documentTypeName}</DataTableCell>
                <DataTableCell>
                  <Badge variant="muted">{fileKindLabel(doc.fileKind)}</Badge>
                </DataTableCell>
                <DataTableCell>
                  <span className="block max-w-[12rem] truncate" title={doc.originalFileName}>
                    {doc.originalFileName}
                  </span>
                </DataTableCell>
                <DataTableCell>{formatDocumentDate(doc.issueDate)}</DataTableCell>
                <DataTableCell>
                  <span
                    className={cn(isPastDate(doc.expiryDate) && "font-medium text-destructive")}
                    title={isPastDate(doc.expiryDate) ? "Expired" : undefined}
                  >
                    {formatDocumentDate(doc.expiryDate)}
                  </span>
                </DataTableCell>
                <DataTableCell>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      disabled={downloadBusyId === doc.id}
                      onClick={() => onDownload(doc)}
                    >
                      {downloadBusyId === doc.id ? "Downloading…" : "Download"}
                    </Button>
                    {canManage ? (
                      <>
                        <Button type="button" variant="secondary" size="sm" onClick={() => onEdit(doc)}>
                          Edit
                        </Button>
                        <Button type="button" variant="danger" size="sm" onClick={() => onDelete(doc)}>
                          Delete
                        </Button>
                      </>
                    ) : null}
                  </div>
                </DataTableCell>
              </DataTableRow>
            ))}
          </DataTableBody>
        </DataTableElement>
      </DataTable>
    </div>
  );
}
