import { emsHttpClient, postFormData } from "@/shared/api/http-client";
import type {
  CreateDocumentPayload,
  Document,
  DocumentType,
  UpdateDocumentRequest,
} from "../types/document.types";
import { downloadDocumentFile, triggerBlobDownload } from "../utils/document-download";

const PATH = "/api/Documents";

export const documentService = {
  getTypes: async (): Promise<DocumentType[]> => {
    const { data } = await emsHttpClient.get<DocumentType[]>(`${PATH}/types`);
    return data;
  },

  listByEmployee: async (employeeId: number): Promise<Document[]> => {
    const { data } = await emsHttpClient.get<Document[]>(PATH, { params: { employeeId } });
    return data;
  },

  create: async (payload: CreateDocumentPayload): Promise<Document> => {
    const formData = new FormData();
    formData.append("employeeId", String(payload.employeeId));
    formData.append("documentTypeId", String(payload.documentTypeId));
    formData.append("name", payload.name);
    if (payload.issueDate) formData.append("issueDate", payload.issueDate);
    if (payload.expiryDate) formData.append("expiryDate", payload.expiryDate);
    formData.append("file", payload.file);
    return postFormData<Document>(PATH, formData);
  },

  update: async (id: number, body: UpdateDocumentRequest): Promise<Document> => {
    const { data } = await emsHttpClient.put<Document>(`${PATH}/${id}`, body);
    return data;
  },

  delete: async (id: number): Promise<void> => {
    await emsHttpClient.delete(`${PATH}/${id}`);
  },

  download: async (documentId: number, fallbackFileName: string): Promise<void> => {
    const { blob, fileName } = await downloadDocumentFile(documentId, fallbackFileName);
    triggerBlobDownload(blob, fileName);
  },
};
