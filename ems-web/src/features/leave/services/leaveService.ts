import { postFormData, httpClient } from "@/shared/api/http-client";
import type {
  LeaveAdminSummary,
  BulkLeaveImportPayload,
  BulkLeaveImportResult,
  CreateLeaveRequestPayload,
  LeaveBalance,
  LeavePolicyRule,
  LeaveRequest,
  LeaveType,
  UpdateLeaveRequestPayload,
  CreateLeaveTypePayload,
  UpdateLeaveTypePayload,
} from "../types/leave.types";

export const leaveService = {
  getLeaveTypes: async (organizationId: number): Promise<LeaveType[]> => {
    const { data } = await httpClient.get<LeaveType[]>("/api/LeaveTypes", { params: { organizationId } });
    return data;
  },


  createLeaveType: async (payload: CreateLeaveTypePayload): Promise<LeaveType> => {
    const { data } = await httpClient.post<LeaveType>("/api/LeaveTypes", payload);
    return data;
  },

  updateLeaveType: async (id: number, payload: UpdateLeaveTypePayload): Promise<LeaveType> => {
    const { data } = await httpClient.put<LeaveType>(`/api/LeaveTypes/${id}`, payload);
    return data;
  },

  deleteLeaveType: async (id: number): Promise<void> => {
    await httpClient.delete(`/api/LeaveTypes/${id}`);
  },

  getLeaveBalances: async (employeeId: number): Promise<LeaveBalance[]> => {
    const { data } = await httpClient.get<LeaveBalance[]>(`/api/LeaveBalances/employee/${employeeId}`);
    return data;
  },

  getLeaveRequests: async (employeeId: number): Promise<LeaveRequest[]> => {
    const { data } = await httpClient.get<LeaveRequest[]>(`/api/LeaveRequests/employee/${employeeId}`);
    return data;
  },

  getAdminSummary: async (organizationId: number, asOfDateUtc?: string): Promise<LeaveAdminSummary> => {
    const { data } = await httpClient.get<LeaveAdminSummary>("/api/LeaveRequests/admin/summary", {
      params: { organizationId, asOfDateUtc },
    });
    return data;
  },

  createLeaveRequest: async (payload: CreateLeaveRequestPayload): Promise<LeaveRequest> => {
    const { data } = await httpClient.post<LeaveRequest>("/api/LeaveRequests", payload);
    return data;
  },

  updateLeaveRequest: async (id: number, payload: UpdateLeaveRequestPayload): Promise<LeaveRequest> => {
    const { data } = await httpClient.put<LeaveRequest>(`/api/LeaveRequests/${id}`, payload);
    return data;
  },

  cancelLeaveRequest: async (id: number): Promise<LeaveRequest> => {
    const { data } = await httpClient.post<LeaveRequest>(`/api/LeaveRequests/${id}/cancel`);
    return data;
  },

  getPolicyRules: async (leaveTypeId: number): Promise<LeavePolicyRule[]> => {
    const { data } = await httpClient.get<LeavePolicyRule[]>(`/api/LeavePolicyRules/leave-type/${leaveTypeId}`);
    return data;
  },

  bulkImport: async (payload: BulkLeaveImportPayload): Promise<BulkLeaveImportResult> => {
    const { data } = await httpClient.post<BulkLeaveImportResult>("/api/LeaveImports/bulk", payload);
    return data;
  },

  uploadAttachment: async (leaveRequestId: number, file: File) => {
    const formData = new FormData();
    formData.append("leaveRequestId", String(leaveRequestId));
    formData.append("file", file);
    return postFormData<{ id: number; fileName: string; storagePath: string }>(
      "/api/LeaveAttachments",
      formData,
    );
  },
};
