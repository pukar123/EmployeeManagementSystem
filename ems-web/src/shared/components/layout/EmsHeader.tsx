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
    <header className="flex h-14 shrink-0 items-center gap-2 border-b bg-background px-4">
      <SidebarTrigger />
      <Separator orientation="vertical" className="h-6" />
      <div className="flex flex-1 items-center justify-end gap-2">
        {user ? (
          <span className="hidden max-w-[200px] truncate text-xs text-muted-foreground sm:inline">{user.email}</span>
        ) : null}
        <ThemeToggle />
        <Button type="button" variant="outline" size="sm" onClick={() => void handleSignOut()}>
          Sign out
        </Button>
      </div>
    </header>
  );
}
