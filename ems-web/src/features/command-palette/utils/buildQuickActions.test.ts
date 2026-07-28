import { describe, expect, it } from "vitest";

import { buildQuickActions } from "./buildQuickActions";
import { buildAllowedRoutes, flattenNavigationMenus } from "./flattenMenus";
import type { MenuDto } from "@/features/navigation/types";

const menus: MenuDto[] = [
  {
    id: 1,
    key: "employees",
    label: "Employees",
    routePath: "/employees",
    parentMenuId: null,
    sortOrder: 1,
    iconKey: "users",
    children: [],
  },
  {
    id: 2,
    key: "departments",
    label: "Departments",
    routePath: "/departments",
    parentMenuId: null,
    sortOrder: 2,
    iconKey: "building2",
    children: [],
  },
  {
    id: 3,
    key: "tasks",
    label: "Tasks",
    routePath: "/tasks",
    parentMenuId: null,
    sortOrder: 3,
    iconKey: "layout-list",
    children: [],
  },
];

describe("buildQuickActions", () => {
  const allowedRoutes = buildAllowedRoutes(flattenNavigationMenus(menus));

  it("returns create actions only for allowed routes", () => {
    const actions = buildQuickActions({
      allowedRoutes,
      canManageEmployees: true,
      query: "",
    });

    expect(actions.map((action) => action.id)).toEqual(["add-employee", "create-department", "assign-task"]);
  });

  it("hides add employee when manage capability is missing", () => {
    const actions = buildQuickActions({
      allowedRoutes,
      canManageEmployees: false,
      query: "",
    });

    expect(actions.some((action) => action.id === "add-employee")).toBe(false);
  });

  it("filters quick actions by query text", () => {
    const actions = buildQuickActions({
      allowedRoutes,
      canManageEmployees: true,
      query: "shift",
    });

    expect(actions).toHaveLength(0);
  });
});
