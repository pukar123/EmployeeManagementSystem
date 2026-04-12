import { httpClient } from "@/shared/api/http-client";
import type { MenuFlatDto, RoleDto, RolePermissionItemDto, UserSummaryDto } from "../types";

export async function fetchUsers(): Promise<UserSummaryDto[]> {
  const { data } = await httpClient.get<UserSummaryDto[]>("/api/Users");
  return data;
}

export type CreateUserBody = {
  email: string;
  userName?: string | null;
  password: string;
  isActive: boolean;
};

export type UpdateUserBody = {
  email: string;
  userName?: string | null;
  isActive: boolean;
};

export type AdminSetPasswordBody = {
  newPassword: string;
};

export async function createUser(body: CreateUserBody): Promise<UserSummaryDto> {
  const { data } = await httpClient.post<UserSummaryDto>("/api/Users", body);
  return data;
}

export async function updateUser(id: number, body: UpdateUserBody): Promise<UserSummaryDto> {
  const { data } = await httpClient.put<UserSummaryDto>(`/api/Users/${id}`, body);
  return data;
}

export async function adminSetPassword(id: number, body: AdminSetPasswordBody): Promise<void> {
  await httpClient.post(`/api/Users/${id}/password`, body);
}

export async function fetchUserRoles(userId: number): Promise<RoleDto[]> {
  const { data } = await httpClient.get<RoleDto[]>(`/api/Users/${userId}/roles`);
  return data;
}

export async function setUserRoles(userId: number, roleIds: number[]): Promise<void> {
  await httpClient.put(`/api/Users/${userId}/roles`, { roleIds });
}

export async function fetchRoles(): Promise<RoleDto[]> {
  const { data } = await httpClient.get<RoleDto[]>("/api/Roles");
  return data;
}

export type CreateRoleBody = { name: string; description?: string | null };

export type UpdateRoleBody = { name: string; description?: string | null };

export async function createRole(body: CreateRoleBody): Promise<RoleDto> {
  const { data } = await httpClient.post<RoleDto>("/api/Roles", body);
  return data;
}

export async function updateRole(id: number, body: UpdateRoleBody): Promise<RoleDto> {
  const { data } = await httpClient.put<RoleDto>(`/api/Roles/${id}`, body);
  return data;
}

export async function deleteRole(id: number): Promise<void> {
  await httpClient.delete(`/api/Roles/${id}`);
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
