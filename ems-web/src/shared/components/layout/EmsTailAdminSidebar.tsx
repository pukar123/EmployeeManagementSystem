"use client";

import { createElement, useEffect, useRef, useState, type ReactNode } from "react";
import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { useSidebar } from "@/context/SidebarContext";
import { useNavigationMenus } from "@/features/navigation/hooks/useNavigationMenus";
import type { MenuDto } from "@/features/navigation/types";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { cn } from "@/lib/utils";
import { resolveOrganizationLogoUrl } from "@/shared/utils/organization-logo-url";
import { getNavIcon } from "./nav-icon-map";
import { getEmsNavItems, isNavActive, type EmsNavItem } from "./ems-nav-items";
import { HorizontaLDots, ChevronDownIcon } from "@/icons/index";

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
    <ul className="flex flex-col gap-4">
      {items.map((item) => {
        const active = isNavActive(pathname, item.href);
        const Icon = item.icon;
        return (
          <li key={item.href}>
            <Link
              href={item.href}
              className={`menu-item group ${active ? "menu-item-active" : "menu-item-inactive"} ${
                showLabels ? "lg:justify-start" : "lg:justify-center"
              }`}
            >
              <span className={active ? "menu-item-icon-active" : "menu-item-icon-inactive"}>
                <Icon className="size-[22px] shrink-0" aria-hidden />
              </span>
              {showLabels ? <span className="menu-item-text">{item.label}</span> : null}
            </Link>
          </li>
        );
      })}
    </ul>
  );
}

function NavFromApi({ menus, pathname, showLabels }: { menus: MenuDto[]; pathname: string; showLabels: boolean }) {
  const safeMenus = dedupeMenus(menus);

  return (
    <ul className="flex flex-col gap-4">
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
        <Link
          href={menu.routePath}
          className={`menu-item group ${activeSelf ? "menu-item-active" : "menu-item-inactive"} ${
            showLabels ? "lg:justify-start" : "lg:justify-center"
          }`}
        >
          <span className={activeSelf ? "menu-item-icon-active" : "menu-item-icon-inactive"}>
            {createElement(getNavIcon(menu.iconKey), { className: "size-[22px] shrink-0" })}
          </span>
          {showLabels ? <span className="menu-item-text">{menu.label}</span> : null}
        </Link>
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
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className={`menu-item group cursor-pointer ${
          open || childActive ? "menu-item-active" : "menu-item-inactive"
        } ${showLabels ? "lg:justify-start" : "lg:justify-center"}`}
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
                open ? "rotate-180 text-brand-500" : "",
              )}
            />
          </>
        ) : null}
      </button>
      {showLabels ? (
        <div
          className="overflow-hidden transition-all duration-300"
          style={{ height: open ? `${height}px` : "0px" }}
        >
          <div ref={contentRef}>
            <ul className="mt-2 ml-9 space-y-1">
              {menu.children.map((child) => {
                const cActive = isNavActive(pathname, child.routePath);
                return (
                  <li key={child.id}>
                    <Link
                      href={child.routePath}
                      className={`menu-dropdown-item ${cActive ? "menu-dropdown-item-active" : "menu-dropdown-item-inactive"}`}
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
    <ul className="flex flex-col gap-4">
      {Array.from({ length: 8 }).map((_, i) => (
        <li key={i} className="h-10 animate-pulse rounded-lg bg-gray-100 dark:bg-gray-800" />
      ))}
    </ul>
  );
}

function NavLoadError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/40 dark:text-amber-200">
      <p className="font-medium">Navigation unavailable</p>
      <p className="mt-1 text-amber-800/90 dark:text-amber-300/90">Could not load menus from the server.</p>
      <Button type="button" variant="secondary" className="mt-2 h-8 text-xs" onClick={onRetry}>
        Retry
      </Button>
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
      <p className="px-2 text-xs text-gray-500 dark:text-gray-400">
        No menu items are assigned to your roles.
      </p>
    );
  }

  return (
    <aside
      className={`fixed top-0 left-0 z-50 flex h-screen flex-col border-r border-gray-200 bg-white px-5 text-gray-900 transition-all duration-300 ease-in-out dark:border-gray-800 dark:bg-gray-900 ${
        isExpanded || isMobileOpen ? "w-[290px]" : isHovered ? "w-[290px]" : "w-[90px]"
      } ${isMobileOpen ? "translate-x-0" : "-translate-x-full"} mt-16 lg:mt-0 lg:translate-x-0`}
      onMouseEnter={() => !isExpanded && setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div
        className={`flex py-8 ${!isExpanded && !isHovered ? "lg:justify-center" : "justify-start"}`}
      >
        <Link href="/" className="flex items-center gap-2">
          {currentOrganization && logoSrc ? (
            <>
              <img
                src={logoSrc}
                alt=""
                className="size-9 shrink-0 rounded-md border border-gray-200 object-contain dark:border-gray-700"
              />
              {showLabels ? (
                <div className="flex min-w-0 flex-col">
                  <span className="truncate text-sm font-semibold text-gray-900 dark:text-white">EMS</span>
                  <span className="truncate text-xs text-gray-500 dark:text-gray-400">
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
                width={150}
                height={40}
              />
              <Image
                className="hidden dark:block"
                src="/images/logo/logo-dark.svg"
                alt="EMS"
                width={150}
                height={40}
              />
            </>
          ) : (
            <Image src="/images/logo/logo-icon.svg" alt="EMS" width={32} height={32} />
          )}
        </Link>
      </div>

      <div className="no-scrollbar flex flex-col overflow-y-auto duration-300 ease-linear">
        <nav className="mb-6">
          <div className="flex flex-col gap-4">
            <div>
              <h2
                className={`mb-4 flex text-xs leading-[20px] tracking-wide text-gray-400 uppercase ${
                  !isExpanded && !isHovered ? "lg:justify-center" : "justify-start"
                }`}
              >
                {showLabels ? "Menu" : <HorizontaLDots />}
              </h2>
              {navBody}
            </div>
          </div>
        </nav>
      </div>
    </aside>
  );
}
