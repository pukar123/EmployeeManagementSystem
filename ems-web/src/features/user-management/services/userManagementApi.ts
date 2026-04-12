import { httpClient } from "@/shared/api/http-client";
import type { MenuFlatDto, RoleDto, RolePermissionItemDto, UserSummaryDto } from "../types";

export async function fetchUsers(): Promise<UserSummaryDto[]> {
  const { data } = await httpClient.get<UserSummaryDto[]>("/api/Users");
  return data;
}

export async function fetchRoles(): Promise<RoleDto[]> {
  const { data } = await httpClient.get<RoleDto[]>("/api/Roles");
  return data;
}

export async function fetchMenusFlat(): Promise<MenuFlatDto[]> {
  const { data } = await httpClient.get<MenuFlatDto[]>("/api/Menus");
  return data;
}

export async function fetchRolePermissions(roleId: number): Promise<RolePermissionItemDto[]> {
  const { data } = await httpClient.get<RolePermissionItemDto[]>(
    `/api/RolePermissions/for-role/${roleId}`,
  );
  return data;
}

export async function saveRolePermissions(
  roleId: number,
  permissions: RolePermissionItemDto[],
): Promise<void> {
  await httpClient.put(`/api/RolePermissions/for-role/${roleId}`, {
    permissions,
  });
}
