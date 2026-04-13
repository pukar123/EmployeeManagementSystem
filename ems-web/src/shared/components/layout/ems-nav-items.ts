import type { LucideIcon } from "lucide-react";
import {
  Briefcase,
  Building2,
  Clock3,
  Home,
  MapPin,
  PlusCircle,
  Settings,
  Users,
} from "lucide-react";

export type EmsNavItem = {
  href: string;
  label: string;
  icon: LucideIcon;
};

/** Routes when organization setup is required (matches AppNav `needsSetup` branch). */
export const emsNavItemsSetup: readonly EmsNavItem[] = [
  { href: "/", label: "Home", icon: Home },
  { href: "/setup", label: "Create organization", icon: PlusCircle },
] as const;

/** Main app routes when organization exists (matches AppNav `mainLinks` + Organization). */
export const emsNavItemsMain: readonly EmsNavItem[] = [
  { href: "/", label: "Home", icon: Home },
  { href: "/employees", label: "Employees", icon: Users },
  { href: "/departments", label: "Departments", icon: Building2 },
  { href: "/attendance", label: "Attendance", icon: Clock3 },
  { href: "/positions", label: "Positions", icon: Briefcase },
  { href: "/sites", label: "Sites", icon: MapPin },
  { href: "/organization/setup", label: "Organization", icon: Settings },
] as const;

export function getEmsNavItems(needsSetup: boolean): readonly EmsNavItem[] {
  return needsSetup ? emsNavItemsSetup : emsNavItemsMain;
}

export function isNavActive(pathname: string, href: string): boolean {
  if (href === "/") return pathname === "/";
  return pathname === href || pathname.startsWith(`${href}/`);
}
