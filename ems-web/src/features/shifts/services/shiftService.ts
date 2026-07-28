import { emsHttpClient } from "@/shared/api/http-client";
import type { CreateShiftRequest, ShiftItem, ShiftQueryParams, UpdateShiftRequest } from "../types/shift.types";

const PATH = "/api/Shifts";

export const shiftService = {
  getAll: async (query: ShiftQueryParams = {}): Promise<ShiftItem[]> => {
    const params = {
      ...(query.organizationId ? { organizationId: query.organizationId } : {}),
      ...(query.employeeId ? { employeeId: query.employeeId } : {}),
    };

    const { data } = await emsHttpClient.get<ShiftItem[]>(PATH, {
      params: Object.keys(params).length > 0 ? params : undefined,
    });
    return data;
  },

  getById: async (id: number): Promise<ShiftItem> => {
    const { data } = await emsHttpClient.get<ShiftItem>(`${PATH}/${id}`);
    return data;
  },

  createShift: async (body: CreateShiftRequest): Promise<ShiftItem> => {
    const { data } = await emsHttpClient.post<ShiftItem>(PATH, body);
    return data;
  },

  updateShift: async (id: number, body: UpdateShiftRequest): Promise<ShiftItem> => {
    const { data } = await emsHttpClient.put<ShiftItem>(`${PATH}/${id}`, body);
    return data;
  },

  deleteShift: async (id: number): Promise<void> => {
    await emsHttpClient.delete(`${PATH}/${id}`);
  },
};
