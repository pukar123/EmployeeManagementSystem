"use client";

import { GlobalCommandPalette } from "@/features/command-palette/components/GlobalCommandPalette";
import { useSidebar } from "@/context/SidebarContext";
import Backdrop from "@/layout/Backdrop";
import { EmsTailAdminHeader } from "./EmsTailAdminHeader";
import { EmsTailAdminSidebar } from "./EmsTailAdminSidebar";

export function EmsTailAdminShell({ children }: { children: React.ReactNode }) {
  const { isExpanded, isHovered, isMobileOpen } = useSidebar();

  const mainContentMargin = isMobileOpen ? "ml-0" : isExpanded || isHovered ? "lg:ml-[280px]" : "lg:ml-[88px]";

  return (
    <div className="min-h-screen bg-background xl:flex">
      <GlobalCommandPalette />
      <EmsTailAdminSidebar />
      <Backdrop />
      <div className={`flex-1 transition-all duration-300 ease-in-out ${mainContentMargin}`}>
        <EmsTailAdminHeader />
        <div className="mx-auto max-w-(--breakpoint-2xl) p-4 md:p-6 lg:p-8">{children}</div>
      </div>
    </div>
  );
}
