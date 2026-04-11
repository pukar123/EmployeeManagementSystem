"use client";

import { cn } from "@/lib/utils";
import { SidebarProvider } from "@/components/ui/sidebar";
import { EmsHeader } from "./EmsHeader";
import { EmsSidebar } from "./EmsSidebar";

/** Content column beside the sidebar. Uses a div (not `SidebarInset`'s main) so route `page.tsx` files can keep a single `<main>`. */
export function EmsDashboardShell({ children }: { children: React.ReactNode }) {
  return (
    <SidebarProvider defaultOpen>
      <EmsSidebar />
      <div
        data-slot="ems-dashboard-inset"
        className={cn(
          "relative flex max-h-svh min-h-0 w-full min-w-0 flex-1 flex-col overflow-hidden bg-background",
          "md:peer-data-[variant=inset]:m-2 md:peer-data-[variant=inset]:ml-0 md:peer-data-[variant=inset]:rounded-xl md:peer-data-[variant=inset]:shadow-sm md:peer-data-[variant=inset]:peer-data-[state=collapsed]:ml-2",
        )}
      >
        <EmsHeader />
        <div className="flex min-h-0 flex-1 flex-col overflow-auto">{children}</div>
      </div>
    </SidebarProvider>
  );
}
