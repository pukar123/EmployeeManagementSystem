import { userManagementHttpClient } from "@/shared/api/http-client";
import type { RoleDto, UserSummaryDto } from "../types";

export async function fetchUsers(): Promise<UserSummaryDto[]> {
  const { data } = await userManagementHttpClient.get<UserSummaryDto[]>("/api/Users");
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
  const { data } = await userManagementHttpClient.post<UserSummaryDto>("/api/Users", body);
  return data;
}

export async function updateUser(id: number, body: UpdateUserBody): Promise<UserSummaryDto> {
  const { data } = await userManagementHttpClient.put<UserSummaryDto>(`/api/Users/${id}`, body);
  return data;
}

export async function adminSetPassword(id: number, body: AdminSetPasswordBody): Promise<void> {
  await userManagementHttpClient.post(`/api/Users/${id}/password`, body);
}

export async function fetchUserRoles(userId: number): Promise<RoleDto[]> {
  const { data } = await userManagementHttpClient.get<RoleDto[]>(`/api/Users/${userId}/roles`);
  return data;
}

export async function setUserRoles(userId: number, roleIds: number[]): Promise<void> {
  await userManagementHttpClient.put(`/api/Users/${userId}/roles`, { roleIds });
}

export async function fetchRoles(): Promise<RoleDto[]> {
  const { data } = await userManagementHttpClient.get<RoleDto[]>("/api/Roles");
  return data;
}

export type CreateRoleBody = { name: string; description?: string | null };

export type UpdateRoleBody = { name: string; description?: string | null };

export async function createRole(body: CreateRoleBody): Promise<RoleDto> {
  const { data } = await userManagementHttpClient.post<RoleDto>("/api/Roles", body);
  return data;
}

export async function updateRole(id: number, body: UpdateRoleBody): Promise<RoleDto> {
  const { data } = await userManagementHttpClient.put<RoleDto>(`/api/Roles/${id}`, body);
  return data;
}

export async function deleteRole(id: number): Promise<void> {
  await userManagementHttpClient.delete(`/api/Roles/${id}`);
}
