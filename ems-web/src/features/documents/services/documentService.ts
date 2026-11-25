import { emsHttpClient, postFormData } from "@/shared/api/http-client";
import type {
  CreateDocumentPayload,
  Document,
  DocumentListParams,
  DocumentType,
  PagedDocuments,
  UpdateDocumentRequest,
} from "../types/document.types";
import { triggerBlobDownload } from "@/shared/api/download-file";
import { downloadDocumentFile } from "../utils/document-download";

const PATH = "/api/Documents";

function toQueryParams(params: DocumentListParams): Record<string, number> {
  const query: Record<string, number> = { page: params.page, pageSize: params.pageSize };
  if (params.documentTypeId != null && params.documentTypeId > 0) query.documentTypeId = params.documentTypeId;
  return query;
}

export const documentService = {
  getTypes: async (): Promise<DocumentType[]> => {
    const { data } = await emsHttpClient.get<DocumentType[]>(`${PATH}/types`);
    return data;
  },

  /** One page of an employee's documents (self, their direct manager or a document admin). */
  listByEmployee: async (employeeId: number, params: DocumentListParams): Promise<PagedDocuments> => {
    const { data } = await emsHttpClient.get<PagedDocuments>(PATH, {
      params: { employeeId, ...toQueryParams(params) },
    });
    return data;
  },

  /** One page of every document, including those with no owner. Document admins only. */
  list: async (params: DocumentListParams): Promise<PagedDocuments> => {
    const { data } = await emsHttpClient.get<PagedDocuments>(PATH, { params: toQueryParams(params) });
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

  download: async (doc: Pick<Document, "downloadUrl" | "originalFileName">): Promise<void> => {
    const { blob, fileName } = await downloadDocumentFile(doc);
    triggerBlobDownload(blob, fileName);
  },
};
