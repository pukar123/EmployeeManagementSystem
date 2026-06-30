import { httpClient } from "@/shared/api/http-client";

import type {

  ArchiveEmployeeRequest,

  AssignEmployeeUserRolesRequest,

  ChangeEmploymentStatusRequest,

  CreateEmployeeRequest,

  EmployeeDirectoryQuery,

  EmployeeEffectiveRole,

  EmployeeHistoryResponse,

  Employee,

  EmployeeProfile,

  LinkEmployeeUserRequest,

  PagedEmployeeDirectory,

  PossibleDuplicateEmployee,

  ProvisionEmployeeUserResponse,

  SetEmployeeDirectRolesRequest,

  TerminateEmployeeRequest,

  TransferEmployeeDepartmentRequest,

  TransferEmployeeManagerRequest,

  TransferEmployeePositionRequest,

  UpdateEmployeeRequest,

} from "../types/employee.types";



const EMPLOYEES_PATH = "/api/Employees";

const EMPLOYEE_HISTORY_PATH = "/api/EmployeeHistory";



function toQueryParams(query: EmployeeDirectoryQuery): Record<string, string | number | boolean> {

  const params: Record<string, string | number | boolean> = {

    organizationId: query.organizationId,

    page: query.page ?? 1,

    pageSize: query.pageSize ?? 25,

    isArchived: query.isArchived ?? false,

    sortBy: query.sortBy ?? "name",

    sortDirection: query.sortDirection ?? "asc",

  };

  if (query.search?.trim()) params.search = query.search.trim();

  if (query.employmentStatus != null) params.employmentStatus = query.employmentStatus;

  if (query.departmentId != null) params.departmentId = query.departmentId;

  if (query.jobPositionId != null) params.jobPositionId = query.jobPositionId;

  if (query.managerId != null) params.managerId = query.managerId;

  if (query.siteId != null) params.siteId = query.siteId;

  if (query.loginLinkStatus) params.loginLinkStatus = query.loginLinkStatus;

  return params;

}



export const employeeService = {

  getEmployees: async (): Promise<Employee[]> => {

    const { data } = await httpClient.get<Employee[]>(EMPLOYEES_PATH);

    return data;

  },



  getEmployeeDirectory: async (query: EmployeeDirectoryQuery): Promise<PagedEmployeeDirectory> => {

    const { data } = await httpClient.get<PagedEmployeeDirectory>(`${EMPLOYEES_PATH}/directory`, {

      params: toQueryParams(query),

    });

    return data;

  },



  exportEmployeeDirectoryCsv: async (query: EmployeeDirectoryQuery): Promise<Blob> => {

    const response = await httpClient.get<Blob>(`${EMPLOYEES_PATH}/export`, {

      params: { ...toQueryParams(query), format: "csv" },

      responseType: "blob",

    });

    return response.data;

  },



  getPossibleDuplicates: async (params: {

    organizationId: number;

    email?: string;

    firstName?: string;

    lastName?: string;

    phoneNumber?: string;

    dateOfBirth?: string;

  }): Promise<PossibleDuplicateEmployee[]> => {

    const { data } = await httpClient.get<PossibleDuplicateEmployee[]>(`${EMPLOYEES_PATH}/possible-duplicates`, {

      params,

    });

    return data;

  },



  getEmployeeById: async (id: number): Promise<Employee> => {

    const { data } = await httpClient.get<Employee>(`${EMPLOYEES_PATH}/${id}`);

    return data;

  },



  getEmployeeProfile: async (id: number): Promise<EmployeeProfile> => {

    const { data } = await httpClient.get<EmployeeProfile>(`${EMPLOYEES_PATH}/${id}/profile`);

    return data;

  },



  getEmployeeHistory: async (employeeId: number): Promise<EmployeeHistoryResponse> => {

    const { data } = await httpClient.get<EmployeeHistoryResponse>(`${EMPLOYEE_HISTORY_PATH}/${employeeId}`);

    return data;

  },



  createEmployee: async (body: CreateEmployeeRequest): Promise<Employee> => {

    const { data } = await httpClient.post<Employee>(EMPLOYEES_PATH, body);

    return data;

  },



  provisionEmployeeUser: async (employeeId: number): Promise<ProvisionEmployeeUserResponse> => {

    const { data } = await httpClient.post<ProvisionEmployeeUserResponse>(`${EMPLOYEES_PATH}/${employeeId}/provision-user`);

    return data;

  },



  linkEmployeeUser: async (employeeId: number, body: LinkEmployeeUserRequest): Promise<ProvisionEmployeeUserResponse> => {

    const { data } = await httpClient.post<ProvisionEmployeeUserResponse>(`${EMPLOYEES_PATH}/${employeeId}/link-user`, body);

    return data;

  },



  assignEmployeeUserRoles: async (employeeId: number, body: AssignEmployeeUserRolesRequest): Promise<void> => {

    await httpClient.put(`${EMPLOYEES_PATH}/${employeeId}/linked-user/roles`, body);

  },



  getEmployeeEffectiveRoles: async (employeeId: number): Promise<EmployeeEffectiveRole[]> => {

    const { data } = await httpClient.get<EmployeeEffectiveRole[]>(`${EMPLOYEES_PATH}/${employeeId}/roles/effective`);

    return data;

  },



  setEmployeeDirectRoles: async (employeeId: number, body: SetEmployeeDirectRolesRequest): Promise<void> => {

    await httpClient.put(`${EMPLOYEES_PATH}/${employeeId}/roles/direct-overrides`, body);

  },



  transferDepartment: async (employeeId: number, body: TransferEmployeeDepartmentRequest): Promise<Employee> => {

    const { data } = await httpClient.post<Employee>(`${EMPLOYEES_PATH}/${employeeId}/transfers/department`, body);

    return data;

  },



  transferPosition: async (employeeId: number, body: TransferEmployeePositionRequest): Promise<Employee> => {

    const { data } = await httpClient.post<Employee>(`${EMPLOYEES_PATH}/${employeeId}/transfers/position`, body);

    return data;

  },



  transferManager: async (employeeId: number, body: TransferEmployeeManagerRequest): Promise<Employee> => {

    const { data } = await httpClient.post<Employee>(`${EMPLOYEES_PATH}/${employeeId}/transfers/manager`, body);

    return data;

  },



  updateEmployee: async (id: number, body: UpdateEmployeeRequest): Promise<Employee> => {

    const { data } = await httpClient.put<Employee>(`${EMPLOYEES_PATH}/${id}`, body);

    return data;

  },



  terminateEmployee: async (id: number, body: TerminateEmployeeRequest): Promise<Employee> => {

    const { data } = await httpClient.post<Employee>(`${EMPLOYEES_PATH}/${id}/terminate`, body);

    return data;

  },



  archiveEmployee: async (id: number, body?: ArchiveEmployeeRequest): Promise<void> => {

    if (body?.reason) {

      await httpClient.post(`${EMPLOYEES_PATH}/${id}/archive`, body);

      return;

    }

    await httpClient.delete(`${EMPLOYEES_PATH}/${id}`, { data: body });

  },



  restoreEmployee: async (id: number): Promise<Employee> => {

    const { data } = await httpClient.post<Employee>(`${EMPLOYEES_PATH}/${id}/restore`);

    return data;

  },



  changeEmploymentStatus: async (id: number, body: ChangeEmploymentStatusRequest): Promise<Employee> => {

    const { data } = await httpClient.post<Employee>(`${EMPLOYEES_PATH}/${id}/employment-status`, body);

    return data;

  },

};

