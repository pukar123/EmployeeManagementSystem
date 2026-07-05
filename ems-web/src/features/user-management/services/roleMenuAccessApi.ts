import { emsHttpClient } from "@/shared/api/http-client";
import type { MenuFlatDto } from "../types";

export type RoleMenuAccessDto = {
  roleKey: string;
  menuIds: number[];
};

export async function fetchMenusFlat(): Promise<MenuFlatDto[]> {
  const { data } = await emsHttpClient.get<MenuFlatDto[]>("/api/Menus");
  return data;
}

export async function fetchRoleMenuAccess(roleKey: string): Promise<RoleMenuAccessDto> {
  const encoded = encodeURIComponent(roleKey);
  const { data } = await emsHttpClient.get<RoleMenuAccessDto>(`/api/RoleKeyPermissions/${encoded}`);
  return data;
}

export async function setRoleMenuAccess(roleKey: string, menuIds: number[]): Promise<void> {
  const encoded = encodeURIComponent(roleKey);
  await emsHttpClient.put(`/api/RoleKeyPermissions/${encoded}`, { menuIds });
}
