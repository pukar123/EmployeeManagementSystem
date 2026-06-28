"use client";

import Link from "next/link";
import { ThemeToggleButton } from "@/components/tailadmin/ThemeToggleButton";
import { EmsUserDropdown } from "./EmsUserDropdown";

export function EmployeePortalShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-50 border-b border-border/60 bg-background/80 px-4 py-3 backdrop-blur-xl md:px-6">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-4">
          <div className="flex min-w-0 flex-wrap items-center gap-x-4 gap-y-1">
            <span className="bg-brand-gradient bg-clip-text text-sm font-bold text-transparent">
              Employee portal
            </span>
            <Link
              href="/"
              className="text-sm font-medium text-primary underline-offset-4 hover:underline"
            >
              Main app
            </Link>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            <ThemeToggleButton />
            <EmsUserDropdown />
          </div>
        </div>
      </header>
      <div className="mx-auto max-w-(--breakpoint-2xl) p-4 md:p-6 lg:p-8">{children}</div>
    </div>
  );
}
