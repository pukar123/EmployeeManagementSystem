import type { Department } from "@/features/departments/types/department.types";
import type { EmployeeDirectoryItem } from "@/features/employees/types/employee.types";
import type { JobPosition } from "@/features/job-positions/types/job-position.types";
import type { Site } from "@/features/sites/types/site.types";
import type { TaskItem } from "@/features/tasks/types/task.types";
import { matchesTaskSearch } from "@/features/tasks/utils/taskDisplay";
import { getNavIcon } from "@/shared/components/layout/nav-icon-map";
import { Briefcase, Building2, ListTodo, MapPin, Users } from "lucide-react";

import type { CommandPaletteItem } from "../types/command-palette.types";
import type { FlatMenuItem } from "./flattenMenus";

const MAX_RESULTS_PER_GROUP = 8;

export function normalizeSearchQuery(query: string): string {
  return query.trim().toLowerCase();
}

export function matchesSearchText(haystack: string, query: string): boolean {
  const normalized = normalizeSearchQuery(query);
  if (!normalized) {
    return true;
  }
  return haystack.toLowerCase().includes(normalized);
}

export function buildRouteItems(menus: FlatMenuItem[], query: string): CommandPaletteItem[] {
  return menus
    .filter((menu) => matchesSearchText(`${menu.label} ${menu.routePath}`, query))
    .slice(0, MAX_RESULTS_PER_GROUP)
    .map((menu) => ({
      id: menu.id,
      group: "pages" as const,
      label: menu.label,
      href: menu.routePath,
      keywords: [menu.routePath],
      icon: getNavIcon(menu.iconKey),
    }));
}

export function buildEmployeeItems(employees: EmployeeDirectoryItem[]): CommandPaletteItem[] {
  return employees.slice(0, MAX_RESULTS_PER_GROUP).map((employee) => {
    const name = `${employee.firstName} ${employee.lastName}`.trim();
    const subtitle = [employee.employeeNumber, employee.departmentName, employee.jobPositionTitle]
      .filter(Boolean)
      .join(" · ");

    return {
      id: `employee-${employee.id}`,
      group: "employees" as const,
      label: name,
      subtitle,
      href: `/employees/${employee.id}`,
      keywords: [employee.email, employee.employeeNumber, employee.departmentName ?? "", employee.jobPositionTitle ?? ""],
      icon: Users,
    };
  });
}

export function filterDepartments(departments: Department[], query: string): CommandPaletteItem[] {
  return departments
    .filter((department) => matchesSearchText(`${department.name} ${department.code ?? ""}`, query))
    .slice(0, MAX_RESULTS_PER_GROUP)
    .map((department) => ({
      id: `department-${department.id}`,
      group: "departments" as const,
      label: department.name,
      subtitle: department.code ?? undefined,
      href: "/departments",
      keywords: [department.code ?? ""],
      icon: Building2,
    }));
}

export function filterPositions(positions: JobPosition[], query: string): CommandPaletteItem[] {
  return positions
    .filter((position) => matchesSearchText(`${position.title} ${position.code ?? ""} ${position.description ?? ""}`, query))
    .slice(0, MAX_RESULTS_PER_GROUP)
    .map((position) => ({
      id: `position-${position.id}`,
      group: "positions" as const,
      label: position.title,
      subtitle: position.code ?? undefined,
      href: "/positions",
      keywords: [position.code ?? "", position.description ?? ""],
      icon: Briefcase,
    }));
}

export function filterSites(sites: Site[], query: string): CommandPaletteItem[] {
  return sites
    .filter((site) => !site.isDeleted && matchesSearchText(`${site.siteName} ${site.siteLocation} ${site.siteDescription ?? ""}`, query))
    .slice(0, MAX_RESULTS_PER_GROUP)
    .map((site) => ({
      id: `site-${site.siteId}`,
      group: "sites" as const,
      label: site.siteName,
      subtitle: site.siteLocation,
      href: "/sites",
      keywords: [site.siteLocation, site.siteDescription ?? ""],
      icon: MapPin,
    }));
}

export function filterTasks(
  tasks: TaskItem[],
  query: string,
  employeeLabelById: Map<number, string>,
): CommandPaletteItem[] {
  return tasks
    .filter((task) => matchesTaskSearch(task, query, employeeLabelById.get(task.employeeId) ?? ""))
    .slice(0, MAX_RESULTS_PER_GROUP)
    .map((task) => ({
      id: `task-${task.id}`,
      group: "tasks" as const,
      label: task.title,
      subtitle: employeeLabelById.get(task.employeeId),
      href: "/tasks",
      keywords: [task.description ?? ""],
      icon: ListTodo,
    }));
}

export function groupCommandItems(items: CommandPaletteItem[]): Map<CommandPaletteItem["group"], CommandPaletteItem[]> {
  const groups = new Map<CommandPaletteItem["group"], CommandPaletteItem[]>();
  for (const item of items) {
    const existing = groups.get(item.group) ?? [];
    existing.push(item);
    groups.set(item.group, existing);
  }
  return groups;
}
