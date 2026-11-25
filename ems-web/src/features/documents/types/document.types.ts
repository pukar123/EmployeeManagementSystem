export enum DocumentFileKind {
  None = 0,
  Pdf = 1,
  Word = 2,
  Image = 3,
}

export type DocumentType = {
  id: number;
  name: string;
  isActive: boolean;
};

export type Document = {
  id: number;
  employeeId: number | null;
  /** "First Last" of the owning employee; null when the document has no owner. */
  employeeDisplayName: string | null;
  documentTypeId: number;
  documentTypeName: string;
  name: string;
  issueDate: string | null;
  expiryDate: string | null;
  fileKind: DocumentFileKind;
  originalFileName: string;
  contentType: string;
  /** Authorised download endpoint relative to the API origin; request it with `downloadApiFile`, never a plain link. */
  downloadUrl: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateDocumentPayload = {
  employeeId: number;
  documentTypeId: number;
  name: string;
  issueDate?: string;
  expiryDate?: string;
  file: File;
};

export type UpdateDocumentRequest = {
  name: string;
  documentTypeId: number;
  employeeId?: number | null;
  issueDate?: string | null;
  expiryDate?: string | null;
};

/** One page of documents from `GET /api/Documents` (B09: the list is always paged). */
export type PagedDocuments = {
  items: Document[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

/** Paging and filter for a documents list. The server allows a page size of 1 to 100 (default 25). */
export type DocumentListParams = {
  page: number;
  pageSize: number;
  documentTypeId?: number | null;
};
