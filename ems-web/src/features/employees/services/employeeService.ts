import { httpClient } from "@/shared/api/http-client";
import type {
  AssignEmployeeUserRolesRequest,
  CreateEmployeeRequest,
  Employee,
  ProvisionEmployeeUserResponse,
  UpdateEmployeeRequest,
} from "../types/employee.types";

const EMPLOYEES_PATH = "/api/Employees";

export const employeeService = {
  getEmployees: async (): Promise<Employee[]> => {
    const { data } = await httpClient.get<Employee[]>(EMPLOYEES_PATH);
    return data;
  },

  getEmployeeById: async (id: number): Promise<Employee> => {
    const { data } = await httpClient.get<Employee>(`${EMPLOYEES_PATH}/${id}`);
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

  assignEmployeeUserRoles: async (employeeId: number, body: AssignEmployeeUserRolesRequest): Promise<void> => {
    await httpClient.put(`${EMPLOYEES_PATH}/${employeeId}/linked-user/roles`, body);
  },

  updateEmployee: async (id: number, body: UpdateEmployeeRequest): Promise<Employee> => {
    const { data } = await httpClient.put<Employee>(`${EMPLOYEES_PATH}/${id}`, body);
    return data;
  },

  deleteEmployee: async (id: number): Promise<void> => {
    await httpClient.delete(`${EMPLOYEES_PATH}/${id}`);
  },
};
