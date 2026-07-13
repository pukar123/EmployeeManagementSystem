import { CalendarClock, ListTodo, Plus, UserPlus } from "lucide-react";

import type { CommandPaletteItem } from "../types/command-palette.types";
import { hasMenuRoute } from "./flattenMenus";

type QuickActionDefinition = {
  id: string;
  label: string;
  href: string;
  keywords: string[];
  icon: CommandPaletteItem["icon"];
  requiredRoute: string;
  requiresManageEmployees?: boolean;
};

const QUICK_ACTION_DEFINITIONS: QuickActionDefinition[] = [
  {
    id: "add-employee",
    label: "Add employee",
    href: "/employees?action=create",
    keywords: ["create", "new", "hire", "onboard"],
    icon: UserPlus,
    requiredRoute: "/employees",
    requiresManageEmployees: true,
  },
  {
    id: "create-department",
    label: "Create department",
    href: "/departments?action=create",
    keywords: ["create", "new", "org", "structure"],
    icon: Plus,
    requiredRoute: "/departments",
  },
  {
    id: "assign-task",
    label: "Assign task",
    href: "/tasks?action=create",
    keywords: ["create", "new", "todo", "work"],
    icon: ListTodo,
    requiredRoute: "/tasks",
  },
  {
    id: "schedule-shift",
    label: "Schedule shift",
    href: "/shifts?action=create",
    keywords: ["create", "new", "roster", "calendar"],
    icon: CalendarClock,
    requiredRoute: "/shifts",
  },
];

export function buildQuickActions(options: {
  allowedRoutes: Set<string>;
  canManageEmployees: boolean;
  query: string;
}): CommandPaletteItem[] {
  const normalizedQuery = options.query.trim().toLowerCase();

  return QUICK_ACTION_DEFINITIONS.filter((action) => {
    if (!hasMenuRoute(options.allowedRoutes, action.requiredRoute)) {
      return false;
    }
    if (action.requiresManageEmployees && !options.canManageEmployees) {
      return false;
    }
    if (!normalizedQuery) {
      return true;
    }

    const haystack = `${action.label} ${action.keywords.join(" ")}`.toLowerCase();
    return haystack.includes(normalizedQuery);
  }).map((action) => ({
    id: action.id,
    group: "quick-actions" as const,
    label: action.label,
    href: action.href,
    keywords: action.keywords,
    icon: action.icon,
  }));
}
