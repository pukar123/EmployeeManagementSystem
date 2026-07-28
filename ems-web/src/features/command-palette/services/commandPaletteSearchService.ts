import { departmentService } from "@/features/departments/services/departmentService";
import { employeeService } from "@/features/employees/services/employeeService";
import { jobPositionService } from "@/features/job-positions/services/jobPositionService";
import { siteService } from "@/features/sites/services/siteService";
import { taskService } from "@/features/tasks/services/taskService";

import type { CommandPaletteSearchContext } from "../types/command-palette.types";
import { hasMenuRoute } from "../utils/flattenMenus";
import {
  buildEmployeeItems,
  filterDepartments,
  filterPositions,
  filterSites,
  filterTasks,
  normalizeSearchQuery,
} from "../utils/matchItems";

export type EntitySearchPayload = {
  employees: ReturnType<typeof buildEmployeeItems>;
  departments: ReturnType<typeof filterDepartments>;
  positions: ReturnType<typeof filterPositions>;
  sites: ReturnType<typeof filterSites>;
  tasks: ReturnType<typeof filterTasks>;
};

export async function searchCommandPaletteEntities(
  query: string,
  context: CommandPaletteSearchContext,
): Promise<EntitySearchPayload> {
  const normalizedQuery = normalizeSearchQuery(query);
  const shouldSearchEntities = normalizedQuery.length >= 2 && context.organizationId != null;

  if (!shouldSearchEntities) {
    return {
      employees: [],
      departments: [],
      positions: [],
      sites: [],
      tasks: [],
    };
  }

  const organizationId = context.organizationId!;
  const allowedRoutes = context.allowedRoutes;

  const [employeesResult, departmentsResult, positionsResult, sitesResult, tasksResult] = await Promise.allSettled([
    context.canViewEmployees && hasMenuRoute(allowedRoutes, "/employees")
      ? employeeService.getEmployeeDirectory({
          organizationId,
          search: normalizedQuery,
          page: 1,
          pageSize: 8,
          isArchived: false,
          sortBy: "name",
          sortDirection: "asc",
        })
      : Promise.resolve(null),
    hasMenuRoute(allowedRoutes, "/departments") ? departmentService.getDepartments() : Promise.resolve(null),
    hasMenuRoute(allowedRoutes, "/positions")
      ? jobPositionService.getByOrganization(organizationId)
      : Promise.resolve(null),
    hasMenuRoute(allowedRoutes, "/sites") ? siteService.getSites() : Promise.resolve(null),
    hasMenuRoute(allowedRoutes, "/tasks") ? taskService.getAll({}) : Promise.resolve(null),
  ]);

  const employees =
    employeesResult.status === "fulfilled" && employeesResult.value
      ? buildEmployeeItems(employeesResult.value.items)
      : [];

  const departments =
    departmentsResult.status === "fulfilled" && departmentsResult.value
      ? filterDepartments(departmentsResult.value, normalizedQuery)
      : [];

  const positions =
    positionsResult.status === "fulfilled" && positionsResult.value
      ? filterPositions(positionsResult.value, normalizedQuery)
      : [];

  const sites =
    sitesResult.status === "fulfilled" && sitesResult.value
      ? filterSites(sitesResult.value, normalizedQuery)
      : [];

  let tasks: EntitySearchPayload["tasks"] = [];
  if (tasksResult.status === "fulfilled" && tasksResult.value) {
    const employeeIds = new Set(tasksResult.value.map((task) => task.employeeId));
    const employeeLabelById = new Map<number, string>();

    if (employeeIds.size > 0 && context.canViewEmployees) {
      try {
        const directory = await employeeService.getEmployeeDirectory({
          organizationId,
          page: 1,
          pageSize: Math.min(employeeIds.size, 100),
          isArchived: false,
        });
        for (const employee of directory.items) {
          employeeLabelById.set(employee.id, `${employee.employeeNumber} - ${employee.firstName} ${employee.lastName}`.trim());
        }
      } catch {
        // Task labels are optional; search still works on title/description.
      }
    }

    tasks = filterTasks(tasksResult.value, normalizedQuery, employeeLabelById);
  }

  return {
    employees,
    departments,
    positions,
    sites,
    tasks,
  };
}