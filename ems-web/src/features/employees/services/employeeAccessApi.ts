import { emsHttpClient } from "@/shared/api/http-client";

export type RoleCapabilityAccessDto = {
  roleKey: string;
  capabilityKeys: string[];
};

export type EmployeeAccessCapabilities = {
  view: boolean;
  manage: boolean;
  access: boolean;
  export: boolean;
};

export const EMPLOYEE_CAPABILITY_KEYS = [
  "employees.view",
  "employees.manage",
  "employees.access",
  "employees.export",
] as const;

export type EmployeeCapabilityKey = (typeof EMPLOYEE_CAPABILITY_KEYS)[number];

export const EMPLOYEE_CAPABILITY_LABELS: Record<EmployeeCapabilityKey, string> = {
  "employees.view": "View employees",
  "employees.manage": "Manage employees",
  "employees.access": "Employee account access",
  "employees.export": "Export employee directory",
};

/** manage, access, and export imply view */
export function expandCapabilityKeys(keys: ReadonlySet<string>): string[] {
  const next = new Set(keys);
  if (
    next.has("employees.manage") ||
    next.has("employees.access") ||
    next.has("employees.export")
  ) {
    next.add("employees.view");
  }
  return [...next].sort();
}

export async function fetchMyEmployeeCapabilities(): Promise<EmployeeAccessCapabilities> {
  const { data } = await emsHttpClient.get<EmployeeAccessCapabilities>("/api/employee-access/me");
  return data;
}

export async function fetchRoleCapabilities(roleKey: string): Promise<RoleCapabilityAccessDto> {
  const encoded = encodeURIComponent(roleKey);
  const { data } = await emsHttpClient.get<RoleCapabilityAccessDto>(`/api/RoleKeyCapabilities/${encoded}`);
  return data;
}

export async function setRoleCapabilities(roleKey: string, capabilityKeys: string[]): Promise<void> {
  const encoded = encodeURIComponent(roleKey);
  await emsHttpClient.put(`/api/RoleKeyCapabilities/${encoded}`, { capabilityKeys });
}
