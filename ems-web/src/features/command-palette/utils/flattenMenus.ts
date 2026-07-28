import type { MenuDto } from "@/features/navigation/types";
import type { EmsNavItem } from "@/shared/components/layout/ems-nav-items";

export type FlatMenuItem = {
  id: string;
  label: string;
  routePath: string;
  iconKey: string | null;
};

function normalizePath(path: string): string {
  return path.trim().replace(/\/+$/, "").toLowerCase() || "/";
}

function dedupeMenus(menus: MenuDto[]): MenuDto[] {
  const seen = new Set<string>();

  const walk = (items: MenuDto[], parentId: number | null): MenuDto[] =>
    items.reduce<MenuDto[]>((acc, menu) => {
      const key = `${parentId ?? "root"}|${normalizePath(menu.routePath)}|${menu.label.trim().toLowerCase()}`;
      if (seen.has(key)) {
        return acc;
      }

      seen.add(key);
      acc.push({
        ...menu,
        children: walk(menu.children, menu.id),
      });
      return acc;
    }, []);

  return walk(menus, null);
}

function flattenMenuTree(menus: MenuDto[]): FlatMenuItem[] {
  const items: FlatMenuItem[] = [];

  const walk = (nodes: MenuDto[]) => {
    for (const menu of nodes) {
      if (menu.children.length > 0) {
        walk(menu.children);
      } else if (menu.routePath.trim()) {
        items.push({
          id: `menu-${menu.id}`,
          label: menu.label,
          routePath: menu.routePath,
          iconKey: menu.iconKey,
        });
      }
    }
  };

  walk(menus);
  return items;
}

export function flattenNavigationMenus(menus: MenuDto[]): FlatMenuItem[] {
  return flattenMenuTree(dedupeMenus(menus));
}

export function flattenStaticNavItems(items: readonly EmsNavItem[]): FlatMenuItem[] {
  return items.map((item) => ({
    id: `static-${item.href}`,
    label: item.label,
    routePath: item.href,
    iconKey: null,
  }));
}

export function buildAllowedRoutes(items: FlatMenuItem[]): Set<string> {
  return new Set(items.map((item) => normalizePath(item.routePath)));
}

export function hasMenuRoute(allowedRoutes: Set<string>, routePath: string): boolean {
  return allowedRoutes.has(normalizePath(routePath));
}
