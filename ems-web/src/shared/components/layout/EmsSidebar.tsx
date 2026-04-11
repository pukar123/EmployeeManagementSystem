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
  SidebarRail,
} from "@/components/ui/sidebar";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { resolveOrganizationLogoUrl } from "@/shared/utils/organization-logo-url";
import { getEmsNavItems, isNavActive } from "./ems-nav-items";

export function EmsSidebar() {
  const pathname = usePathname();
  const { needsSetup, currentOrganization } = useOrganizationContext();
  const items = getEmsNavItems(needsSetup);
  const logoSrc = resolveOrganizationLogoUrl(currentOrganization?.logoRelativePath);

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
              {items.map((item) => {
                const active = isNavActive(pathname, item.href);
                const Icon = item.icon;
                return (
                  <SidebarMenuItem key={item.href}>
                    <SidebarMenuButton
                      isActive={active}
                      tooltip={item.label}
                      render={<Link href={item.href} />}
                    >
                      <Icon />
                      <span>{item.label}</span>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                );
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
