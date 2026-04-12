"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
} from "@/components/ui/sidebar";
import { useNavigationMenus } from "@/features/navigation/hooks/useNavigationMenus";
import type { MenuDto } from "@/features/navigation/types";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { resolveOrganizationLogoUrl } from "@/shared/utils/organization-logo-url";
import { getNavIcon } from "./nav-icon-map";
import { getEmsNavItems, isNavActive, type EmsNavItem } from "./ems-nav-items";

function NavFromApi({ menus, pathname }: { menus: MenuDto[]; pathname: string }) {
  return (
    <>
      {menus.map((menu) => (
        <NavMenuEntry key={menu.id} menu={menu} pathname={pathname} />
      ))}
    </>
  );
}

function NavMenuEntry({ menu, pathname }: { menu: MenuDto; pathname: string }) {
  const Icon = getNavIcon(menu.iconKey);
  const hasChildren = menu.children.length > 0;
  const activeSelf = !hasChildren && isNavActive(pathname, menu.routePath);
  const childActive = hasChildren && menu.children.some((c) => isNavActive(pathname, c.routePath));

  if (!hasChildren) {
    return (
      <SidebarMenuItem>
        <SidebarMenuButton
          isActive={activeSelf}
          tooltip={menu.label}
          render={<Link href={menu.routePath} />}
        >
          <Icon />
          <span>{menu.label}</span>
        </SidebarMenuButton>
      </SidebarMenuItem>
    );
  }

  return (
    <SidebarMenuItem>
      <SidebarMenuButton
        isActive={childActive}
        tooltip={menu.label}
        render={<Link href={menu.routePath} />}
      >
        <Icon />
        <span>{menu.label}</span>
      </SidebarMenuButton>
      <SidebarMenuSub>
        {menu.children.map((child) => {
          const CIcon = getNavIcon(child.iconKey);
          const cActive = isNavActive(pathname, child.routePath);
          return (
            <SidebarMenuSubItem key={child.id}>
              <SidebarMenuSubButton isActive={cActive} render={<Link href={child.routePath} />}>
                <CIcon />
                <span>{child.label}</span>
              </SidebarMenuSubButton>
            </SidebarMenuSubItem>
          );
        })}
      </SidebarMenuSub>
    </SidebarMenuItem>
  );
}

function NavStatic({ items, pathname }: { items: readonly EmsNavItem[]; pathname: string }) {
  return (
    <>
      {items.map((item) => {
        const active = isNavActive(pathname, item.href);
        const Icon = item.icon;
        return (
          <SidebarMenuItem key={item.href}>
            <SidebarMenuButton isActive={active} tooltip={item.label} render={<Link href={item.href} />}>
              <Icon />
              <span>{item.label}</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        );
      })}
    </>
  );
}

export function EmsSidebar() {
  const pathname = usePathname();
  const { needsSetup, currentOrganization } = useOrganizationContext();
  const logoSrc = resolveOrganizationLogoUrl(currentOrganization?.logoRelativePath);
  const navQuery = useNavigationMenus(!needsSetup);

  const staticItems = getEmsNavItems(needsSetup);
  const showApiNav = !needsSetup && navQuery.data && navQuery.data.length > 0;

  return (
    <Sidebar collapsible="icon" variant="sidebar">
      <SidebarHeader className="border-b border-sidebar-border">
        <div className="flex items-center gap-2 px-2 py-1">
          {currentOrganization && logoSrc ? (
            <img
              src={logoSrc}
              alt=""
              className="size-8 shrink-0 rounded-md border border-sidebar-border object-contain"
            />
          ) : null}
          <div className="flex min-w-0 flex-col group-data-[collapsible=icon]:hidden">
            <span className="truncate text-sm font-semibold text-sidebar-foreground">EMS</span>
            {currentOrganization ? (
              <span className="truncate text-xs text-muted-foreground">{currentOrganization.name}</span>
            ) : null}
          </div>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Navigation</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {needsSetup || navQuery.isLoading ? (
                <NavStatic items={staticItems} pathname={pathname} />
              ) : showApiNav ? (
                <NavFromApi menus={navQuery.data!} pathname={pathname} />
              ) : (
                <NavStatic items={staticItems} pathname={pathname} />
              )}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
