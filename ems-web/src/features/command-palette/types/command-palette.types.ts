import type { LucideIcon } from "lucide-react";

export type CommandPaletteGroup =
  | "quick-actions"
  | "pages"
  | "employees"
  | "departments"
  | "positions"
  | "sites"
  | "tasks";

export const COMMAND_PALETTE_GROUP_LABELS: Record<CommandPaletteGroup, string> = {
  "quick-actions": "Quick actions",
  pages: "Pages",
  employees: "Employees",
  departments: "Departments",
  positions: "Positions",
  sites: "Sites",
  tasks: "Tasks",
};

export type CommandPaletteItem = {
  id: string;
  group: CommandPaletteGroup;
  label: string;
  href: string;
  subtitle?: string;
  keywords?: string[];
  icon: LucideIcon;
};

export type CommandPaletteSearchContext = {
  organizationId: number | null;
  needsSetup: boolean;
  canViewEmployees: boolean;
  canManageEmployees: boolean;
  allowedRoutes: Set<string>;
};

export type CommandPaletteSearchResult = {
  items: CommandPaletteItem[];
  isLoading: boolean;
  errors: Partial<Record<CommandPaletteGroup, string>>;
};
