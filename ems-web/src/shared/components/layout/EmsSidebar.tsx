"use client";

import { createElement, useState, type ReactNode } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { ChevronRight } from "lucide-react";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
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
  SidebarMenuSkeleton,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
} from "@/components/ui/sidebar";
import { useNavigationMenus } from "@/features/navigation/hooks/useNavigationMenus";
import type { MenuDto } from "@/features/navigation/types";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { cn } from "@/lib/utils";
import { resolveOrganizationLogoUrl } from "@/shared/utils/organization-logo-url";
import { getNavIcon } from "./nav-icon-map";
import { getEmsNavItems, isNavActive, type EmsNavItem } from "./ems-nav-items";

function normalizePath(path: string): string {
  return path.trim().replace(/\/+$/, "").toLowerCase() || "/";
}

function dedupeMenus(menus: MenuDto[]): MenuDto[] {
  const seen = new Set<string>();

  const walk = (items: MenuDto[], parentId: number | null): MenuDto[] => {
    return items.reduce<MenuDto[]>((acc, menu) => {
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
  };

  return walk(menus, null);
}

function NavFromApi({ menus, pathname }: { menus: MenuDto[]; pathname: string }) {
  const safeMenus = dedupeMenus(menus);

  return (
    <>
      {safeMenus.map((menu) => (
        <NavMenuEntry key={menu.id} menu={menu} pathname={pathname} />
      ))}
    </>
  );
}

function NavMenuEntry({ menu, pathname }: { menu: MenuDto; pathname: string }) {
  const hasChildren = menu.children.length > 0;
  const activeSelf = !hasChildren && isNavActive(pathname, menu.routePath);
  const childActive = hasChildren && menu.children.some((c) => isNavActive(pathname, c.routePath));

  if (!hasChildren) {
    return (
      <SidebarMenuItem>
        <div className="flex w-full min-w-0 items-center gap-0.5">
          <div
            className="h-8 w-8 shrink-0 group-data-[collapsible=icon]:hidden"
            aria-hidden
          />
          <SidebarMenuButton
            className="min-w-0 flex-1"
            isActive={activeSelf}
            tooltip={menu.label}
            render={<Link href={menu.routePath} />}
          >
            {createElement(getNavIcon(menu.iconKey))}
            <span>{menu.label}</span>
          </SidebarMenuButton>
        </div>
      </SidebarMenuItem>
    );
  }

  return <NavMenuEntryWithChildren menu={menu} pathname={pathname} childActive={childActive} />;
}

function NavMenuEntryWithChildren({
  menu,
  pathname,
  childActive,
}: {
  menu: MenuDto;
  pathname: string;
  childActive: boolean;
}) {
  const [open, setOpen] = useState(childActive);

  return (
    <Collapsible open={open} onOpenChange={setOpen} className="group/collapsible">
      <SidebarMenuItem>
        <div className="flex w-full min-w-0 items-center gap-0.5">
          <CollapsibleTrigger
            className={cn(
              "flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-sidebar-foreground outline-none hover:bg-sidebar-accent hover:text-sidebar-accent-foreground",
              "group-data-[collapsible=icon]:hidden",
            )}
            type="button"
            aria-expanded={open}
            aria-label={open ? "Collapse section" : "Expand section"}
          >
            <ChevronRight
              className={cn("size-4 shrink-0 transition-transform duration-200", open && "rotate-90")}
            />
          </CollapsibleTrigger>
          <SidebarMenuButton
            className="min-w-0 flex-1"
            isActive={childActive}
            tooltip={menu.label}
            render={<Link href={menu.routePath} />}
          >
            {createElement(getNavIcon(menu.iconKey))}
            <span>{menu.label}</span>
          </SidebarMenuButton>
        </div>
        <CollapsibleContent>
          <SidebarMenuSub className="ml-7 mr-3.5 pl-3.5">
            {menu.children.map((child) => {
              const cActive = isNavActive(pathname, child.routePath);
              return (
                <SidebarMenuSubItem key={child.id}>
                  <SidebarMenuSubButton isActive={cActive} render={<Link href={child.routePath} />}>
                    {createElement(getNavIcon(child.iconKey))}
                    <span>{child.label}</span>
                  </SidebarMenuSubButton>
                </SidebarMenuSubItem>
              );
            })}
          </SidebarMenuSub>
        </CollapsibleContent>
      </SidebarMenuItem>
    </Collapsible>
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
            <div className="flex w-full min-w-0 items-center gap-0.5">
              <div
                className="h-8 w-8 shrink-0 group-data-[collapsible=icon]:hidden"
                aria-hidden
              />
              <SidebarMenuButton
                className="min-w-0 flex-1"
                isActive={active}
                tooltip={item.label}
                render={<Link href={item.href} />}
              >
                <Icon />
                <span>{item.label}</span>
              </SidebarMenuButton>
            </div>
          </SidebarMenuItem>
        );
      })}
    </>
  );
}

function NavLoadingSkeleton() {
  return (
    <>
      {Array.from({ length: 8 }).map((_, i) => (
        <SidebarMenuItem key={i}>
          <SidebarMenuSkeleton showIcon />
        </SidebarMenuItem>
      ))}
    </>
  );
}

function NavLoadError({ onRetry }: { onRetry: () => void }) {
  return (
    <SidebarMenuItem>
      <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/40 dark:text-amber-200">
        <p className="font-medium">Navigation unavailable</p>
        <p className="mt-1 text-amber-800/90 dark:text-amber-300/90">Could not load menus from the server.</p>
        <Button type="button" variant="secondary" className="mt-2 h-8 text-xs" onClick={onRetry}>
          Retry
        </Button>
      </div>
    </SidebarMenuItem>
  );
}

export function EmsSidebar() {
  const pathname = usePathname();
  const queryClient = useQueryClient();
  const { needsSetup, currentOrganization } = useOrganizationContext();
  const logoSrc = resolveOrganizationLogoUrl(currentOrganization?.logoRelativePath);
  const navQuery = useNavigationMenus(!needsSetup);

  const staticItems = getEmsNavItems(needsSetup);

  const retryNav = () => {
    void queryClient.invalidateQueries({ queryKey: ["navigation", "menus"] });
  };

  let navBody: ReactNode;
  if (needsSetup) {
    navBody = <NavStatic items={staticItems} pathname={pathname} />;
  } else if (navQuery.isLoading) {
    navBody = <NavLoadingSkeleton />;
  } else if (navQuery.isError) {
    navBody = <NavLoadError onRetry={retryNav} />;
  } else if (navQuery.data && navQuery.data.length > 0) {
    navBody = <NavFromApi menus={navQuery.data} pathname={pathname} />;
  } else {
    navBody = (
      <SidebarMenuItem>
        <p className="px-2 text-xs text-muted-foreground">No menu items are assigned to your roles.</p>
      </SidebarMenuItem>
    );
  }

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
            <SidebarMenu>{navBody}</SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
