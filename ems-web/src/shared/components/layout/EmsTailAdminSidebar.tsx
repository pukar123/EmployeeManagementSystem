"use client";

import { createElement, useEffect, useRef, useState, type ReactNode } from "react";
import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { UserCircle } from "lucide-react";
import { useSidebar } from "@/context/SidebarContext";
import { useNavigationMenus } from "@/features/navigation/hooks/useNavigationMenus";
import type { MenuDto } from "@/features/navigation/types";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { useAuth } from "@/providers/AuthProvider";
import { Button } from "@/shared/components/Button";
import { cn } from "@/lib/utils";
import { resolveOrganizationLogoUrl } from "@/shared/utils/organization-logo-url";
import { getNavIcon } from "./nav-icon-map";
import { getEmsNavItems, isNavActive, type EmsNavItem } from "./ems-nav-items";
import { ChevronDownIcon } from "@/icons/index";

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

function navItemClass(active: boolean, showLabels: boolean) {
  return cn(
    "menu-item group relative",
    active ? "menu-item-active" : "menu-item-inactive",
    showLabels ? "lg:justify-start" : "lg:justify-center",
    active && "before:absolute before:left-0 before:top-1/2 before:h-6 before:w-1 before:-translate-y-1/2 before:rounded-full before:bg-brand-gradient",
  );
}

function NavItemTooltip({ label, show, children }: { label: string; show: boolean; children: ReactNode }) {
  if (!show) return <>{children}</>;
  return <span title={label} className="block w-full">{children}</span>;
}

function NavStatic({
  items,
  pathname,
  showLabels,
}: {
  items: readonly EmsNavItem[];
  pathname: string;
  showLabels: boolean;
}) {
  return (
    <ul className="flex flex-col gap-1">
      {items.map((item) => {
        const active = isNavActive(pathname, item.href);
        const Icon = item.icon;
        return (
          <li key={item.href}>
            <NavItemTooltip label={item.label} show={!showLabels}>
              <Link href={item.href} className={navItemClass(active, showLabels)}>
                <span className={active ? "menu-item-icon-active" : "menu-item-icon-inactive"}>
                  <Icon className="size-[22px] shrink-0" aria-hidden />
                </span>
                {showLabels ? <span className="menu-item-text">{item.label}</span> : null}
              </Link>
            </NavItemTooltip>
          </li>
        );
      })}
    </ul>
  );
}

function NavFromApi({ menus, pathname, showLabels }: { menus: MenuDto[]; pathname: string; showLabels: boolean }) {
  const safeMenus = dedupeMenus(menus);

  return (
    <ul className="flex flex-col gap-1">
      {safeMenus.map((menu) => (
        <NavMenuEntry key={menu.id} menu={menu} pathname={pathname} showLabels={showLabels} />
      ))}
    </ul>
  );
}

function NavMenuEntry({
  menu,
  pathname,
  showLabels,
}: {
  menu: MenuDto;
  pathname: string;
  showLabels: boolean;
}) {
  const hasChildren = menu.children.length > 0;
  const activeSelf = !hasChildren && isNavActive(pathname, menu.routePath);
  const childActive = hasChildren && menu.children.some((c) => isNavActive(pathname, c.routePath));

  if (!hasChildren) {
    return (
      <li>
        <NavItemTooltip label={menu.label} show={!showLabels}>
          <Link href={menu.routePath} className={navItemClass(activeSelf, showLabels)}>
            <span className={activeSelf ? "menu-item-icon-active" : "menu-item-icon-inactive"}>
              {createElement(getNavIcon(menu.iconKey), { className: "size-[22px] shrink-0" })}
            </span>
            {showLabels ? <span className="menu-item-text">{menu.label}</span> : null}
          </Link>
        </NavItemTooltip>
      </li>
    );
  }

  return (
    <NavMenuEntryWithChildren
      menu={menu}
      pathname={pathname}
      childActive={childActive}
      showLabels={showLabels}
    />
  );
}

function NavMenuEntryWithChildren({
  menu,
  pathname,
  childActive,
  showLabels,
}: {
  menu: MenuDto;
  pathname: string;
  childActive: boolean;
  showLabels: boolean;
}) {
  const [open, setOpen] = useState(childActive);
  const contentRef = useRef<HTMLDivElement>(null);
  const [height, setHeight] = useState(0);

  useEffect(() => {
    if (open && contentRef.current) {
      setHeight(contentRef.current.scrollHeight);
    } else {
      setHeight(0);
    }
  }, [open, menu.children]);

  return (
    <li>
      <NavItemTooltip label={menu.label} show={!showLabels}>
        <button
          type="button"
          onClick={() => setOpen((o) => !o)}
          className={cn(
            navItemClass(open || childActive, showLabels),
            "cursor-pointer",
          )}
        >
          <span className={open || childActive ? "menu-item-icon-active" : "menu-item-icon-inactive"}>
            {createElement(getNavIcon(menu.iconKey), { className: "size-[22px] shrink-0" })}
          </span>
          {showLabels ? (
            <>
              <span className="menu-item-text">{menu.label}</span>
              <ChevronDownIcon
                className={cn(
                  "ml-auto h-5 w-5 shrink-0 transition-transform duration-200",
                  open ? "rotate-180 text-brand-500" : "text-muted-foreground",
                )}
              />
            </>
          ) : null}
        </button>
      </NavItemTooltip>
      {showLabels ? (
        <div
          className="overflow-hidden transition-all duration-300 ease-in-out"
          style={{ height: open ? `${height}px` : "0px" }}
        >
          <div ref={contentRef}>
            <ul className="mt-1 ml-4 space-y-0.5 border-l-2 border-brand-200/60 pl-3 dark:border-brand-500/30">
              {menu.children.map((child) => {
                const cActive = isNavActive(pathname, child.routePath);
                return (
                  <li key={child.id}>
                    <Link
                      href={child.routePath}
                      className={cn(
                        "menu-dropdown-item",
                        cActive ? "menu-dropdown-item-active" : "menu-dropdown-item-inactive",
                      )}
                    >
                      <span className="flex items-center gap-2">
                        {createElement(getNavIcon(child.iconKey), { className: "size-4 shrink-0" })}
                        {child.label}
                      </span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        </div>
      ) : null}
    </li>
  );
}

function NavLoadingSkeleton() {
  return (
    <ul className="flex flex-col gap-2">
      {Array.from({ length: 8 }).map((_, i) => (
        <li key={i} className="h-11 animate-pulse rounded-xl bg-muted/60" />
      ))}
    </ul>
  );
}

function NavLoadError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="rounded-xl border border-warning-200 bg-warning-50 px-3 py-2 text-xs text-warning-900 dark:border-warning-900/50 dark:bg-warning-950/40 dark:text-warning-200">
      <p className="font-medium">Navigation unavailable</p>
      <p className="mt-1 opacity-90">Could not load menus from the server.</p>
      <Button type="button" variant="secondary" size="sm" className="mt-2" onClick={onRetry}>
        Retry
      </Button>
    </div>
  );
}

function SidebarUserCard({ showLabels }: { showLabels: boolean }) {
  const { user } = useAuth();
  const email = user?.email ?? "Signed in";

  return (
    <div
      className={cn(
        "mt-auto flex items-center gap-3 rounded-xl border border-sidebar-border bg-sidebar-accent/50 p-3",
        !showLabels && "lg:justify-center lg:p-2",
      )}
    >
      <div className="relative shrink-0">
        <div className="rounded-full bg-brand-gradient p-[2px]">
          <span className="flex size-9 items-center justify-center rounded-full bg-sidebar text-sidebar-foreground">
            <UserCircle className="size-6" strokeWidth={1.5} />
          </span>
        </div>
      </div>
      {showLabels ? (
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-sidebar-foreground">{email}</p>
          <p className="text-xs text-muted-foreground">Employee Management</p>
        </div>
      ) : null}
    </div>
  );
}

export function EmsTailAdminSidebar() {
  const pathname = usePathname();
  const queryClient = useQueryClient();
  const { isExpanded, isMobileOpen, isHovered, setIsHovered } = useSidebar();
  const showLabels = isExpanded || isHovered || isMobileOpen;

  const { needsSetup, currentOrganization } = useOrganizationContext();
  const logoSrc = resolveOrganizationLogoUrl(currentOrganization?.logoRelativePath);
  const navQuery = useNavigationMenus(!needsSetup);

  const staticItems = getEmsNavItems(needsSetup);

  const retryNav = () => {
    void queryClient.invalidateQueries({ queryKey: ["navigation", "menus"] });
  };

  let navBody: ReactNode;
  if (needsSetup) {
    navBody = <NavStatic items={staticItems} pathname={pathname} showLabels={showLabels} />;
  } else if (navQuery.isLoading) {
    navBody = <NavLoadingSkeleton />;
  } else if (navQuery.isError) {
    navBody = <NavLoadError onRetry={retryNav} />;
  } else if (navQuery.data && navQuery.data.length > 0) {
    navBody = <NavFromApi menus={navQuery.data} pathname={pathname} showLabels={showLabels} />;
  } else {
    navBody = (
      <p className="px-2 text-xs text-muted-foreground">
        No menu items are assigned to your roles.
      </p>
    );
  }

  return (
    <aside
      className={cn(
        "fixed top-0 left-0 z-50 flex h-screen flex-col border-r border-sidebar-border bg-sidebar text-sidebar-foreground transition-all duration-300 ease-in-out",
        isExpanded || isMobileOpen ? "w-[280px] px-4" : isHovered ? "w-[280px] px-4" : "w-[88px] px-3",
        isMobileOpen ? "translate-x-0" : "-translate-x-full",
        "mt-16 lg:mt-0 lg:translate-x-0",
      )}
      onMouseEnter={() => !isExpanded && setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div
        className={cn(
          "flex shrink-0 py-6",
          !isExpanded && !isHovered ? "lg:justify-center" : "justify-start",
        )}
      >
        <Link href="/" className="flex items-center gap-3">
          {currentOrganization && logoSrc ? (
            <>
              <img
                src={logoSrc}
                alt=""
                className="size-10 shrink-0 rounded-xl border border-sidebar-border object-contain shadow-soft"
              />
              {showLabels ? (
                <div className="flex min-w-0 flex-col">
                  <span className="truncate text-sm font-bold text-sidebar-foreground">EMS</span>
                  <span className="truncate text-xs text-muted-foreground">
                    {currentOrganization.name}
                  </span>
                </div>
              ) : null}
            </>
          ) : showLabels ? (
            <>
              <Image
                className="dark:hidden"
                src="/images/logo/logo.svg"
                alt="EMS"
                width={140}
                height={36}
              />
              <Image
                className="hidden dark:block"
                src="/images/logo/logo-dark.svg"
                alt="EMS"
                width={140}
                height={36}
              />
            </>
          ) : (
            <div className="rounded-xl bg-brand-gradient p-2 shadow-brand-glow">
              <Image src="/images/logo/logo-icon.svg" alt="EMS" width={28} height={28} className="brightness-0 invert" />
            </div>
          )}
        </Link>
      </div>

      <div className="custom-scrollbar flex min-h-0 flex-1 flex-col overflow-y-auto">
        <nav className="mb-4 flex-1">
          <div className="flex flex-col gap-2">
            <div>
              <h2
                className={cn(
                  "mb-3 flex text-[11px] font-semibold tracking-widest text-muted-foreground uppercase",
                  !isExpanded && !isHovered ? "lg:justify-center" : "justify-start",
                )}
              >
                {showLabels ? "Navigation" : "···"}
              </h2>
              {navBody}
            </div>
          </div>
        </nav>
        <SidebarUserCard showLabels={showLabels} />
      </div>
    </aside>
  );
}
