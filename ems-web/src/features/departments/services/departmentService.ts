import { emsHttpClient } from "@/shared/api/http-client";
import type {
  CreateDepartmentRequest,
  Department,
  UpdateDepartmentRequest,
} from "../types/department.types";

const PATH = "/api/Departments";

export const departmentService = {
  getDepartments: async (): Promise<Department[]> => {
    const { data } = await emsHttpClient.get<Department[]>(PATH);
    return data;
  },

  createDepartment: async (body: CreateDepartmentRequest): Promise<Department> => {
    const { data } = await emsHttpClient.post<Department>(PATH, body);
    return data;
  },

  updateDepartment: async (id: number, body: UpdateDepartmentRequest): Promise<Department> => {
    const { data } = await emsHttpClient.put<Department>(`${PATH}/${id}`, body);
    return data;
  },

  deleteDepartment: async (id: number): Promise<void> => {
    await emsHttpClient.delete(`${PATH}/${id}`);
  },
};
