export type UserSummaryDto = {
  id: number;
  email: string;
  userName: string | null;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
};

export type RoleDto = {
  id: number;
  name: string;
  /** Upper-invariant key (e.g. ADMIN); matches EMS RoleKeyPermissions.RoleKey. */
  normalizedName: string;
  description: string | null;
  isSystem: boolean;
};

/** Flat list from <c>GET /api/Menus</c> (children not populated). */
export type MenuFlatDto = {
  id: number;
  key: string;
  label: string;
  routePath: string;
  parentMenuId: number | null;
  sortOrder: number;
  iconKey: string | null;
  children: [];
};
