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
  documentTypeId: number;
  documentTypeName: string;
  name: string;
  issueDate: string | null;
  expiryDate: string | null;
  fileKind: DocumentFileKind;
  originalFileName: string;
  contentType: string;
  storedRelativePath: string;
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
