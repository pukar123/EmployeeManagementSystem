"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { SidebarTrigger } from "@/components/ui/sidebar";
import { useAuth } from "@/providers/AuthProvider";
import { ThemeToggle } from "./ThemeToggle";

export function EmsHeader() {
  const router = useRouter();
  const { logout, user } = useAuth();

  async function handleSignOut() {
    await logout();
    router.replace("/login");
  }

  return (
    <header className="flex h-14 shrink-0 items-center gap-2 border-b border-ems-header-border bg-ems-header px-4 text-ems-header-foreground">
      <SidebarTrigger className="text-ems-header-foreground" />
      <Separator orientation="vertical" className="h-6 bg-ems-header-border/60" />
      <div className="flex flex-1 items-center justify-end gap-2">
        {user ? (
          <span className="hidden max-w-[200px] truncate text-xs opacity-90 sm:inline">{user.email}</span>
        ) : null}
        <ThemeToggle className="text-ems-header-foreground" />
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="border-sidebar-border bg-background/90 text-foreground shadow-sm hover:bg-background"
          onClick={() => void handleSignOut()}
        >
          Sign out
        </Button>
      </div>
    </header>
  );
}
