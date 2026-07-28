import { describe, expect, it } from "vitest";

import type { MenuDto } from "@/features/navigation/types";
import { buildAllowedRoutes, flattenNavigationMenus, hasMenuRoute } from "./flattenMenus";

const sampleMenus: MenuDto[] = [
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
    key: "attendance",
    label: "Attendance",
    routePath: "/attendance",
    parentMenuId: null,
    sortOrder: 2,
    iconKey: "clock3",
    children: [
      {
        id: 3,
        key: "attendance-reports",
        label: "Reports",
        routePath: "/attendance/reports",
        parentMenuId: 2,
        sortOrder: 1,
        iconKey: null,
        children: [],
      },
      {
        id: 4,
        key: "attendance-reports-dup",
        label: "Reports",
        routePath: "/attendance/reports/",
        parentMenuId: 2,
        sortOrder: 2,
        iconKey: null,
        children: [],
      },
    ],
  },
];

describe("flattenNavigationMenus", () => {
  it("flattens leaf menu routes and removes duplicate siblings", () => {
    const flat = flattenNavigationMenus(sampleMenus);

    expect(flat.map((item) => item.routePath)).toEqual(["/employees", "/attendance/reports"]);
  });

  it("builds allowed route lookup with normalized paths", () => {
    const allowed = buildAllowedRoutes(flattenNavigationMenus(sampleMenus));

    expect(hasMenuRoute(allowed, "/employees")).toBe(true);
    expect(hasMenuRoute(allowed, "/employees/")).toBe(true);
    expect(hasMenuRoute(allowed, "/tasks")).toBe(false);
  });
});
