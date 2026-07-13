import { describe, expect, it } from "vitest";

import type { Department } from "@/features/departments/types/department.types";
import type { TaskItem } from "@/features/tasks/types/task.types";
import { filterDepartments, filterTasks, matchesSearchText } from "./matchItems";

const departments: Department[] = [
  {
    id: 1,
    organizationId: 1,
    name: "Engineering",
    code: "ENG",
    parentDepartmentId: null,
    isActive: true,
  },
  {
    id: 2,
    organizationId: 1,
    name: "Finance",
    code: "FIN",
    parentDepartmentId: null,
    isActive: true,
  },
];

const tasks: TaskItem[] = [
  {
    id: 10,
    employeeId: 5,
    organizationId: 1,
    assignedByUserId: 1,
    title: "Prepare onboarding pack",
    description: "Welcome kit",
    status: 1,
    priority: 2,
    assignedAtUtc: "2026-01-01T00:00:00Z",
    startAtUtc: null,
    dueAtUtc: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
  },
];

describe("matchItems", () => {
  it("matches search text case-insensitively", () => {
    expect(matchesSearchText("Engineering Team", "eng")).toBe(true);
    expect(matchesSearchText("Finance", "hr")).toBe(false);
  });

  it("filters departments by name or code", () => {
    const results = filterDepartments(departments, "fin");
    expect(results).toHaveLength(1);
    expect(results[0]?.label).toBe("Finance");
  });

  it("filters tasks using task search helper", () => {
    const results = filterTasks(tasks, "onboarding", new Map([[5, "EMP001 - Alex"]]));
    expect(results).toHaveLength(1);
    expect(results[0]?.label).toBe("Prepare onboarding pack");
  });
});
