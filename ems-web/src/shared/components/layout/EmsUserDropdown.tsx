"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { KeyRound, LogOut, UserCircle } from "lucide-react";
import { Dropdown } from "@/components/tailadmin/dropdown/Dropdown";
import { DropdownItem } from "@/components/tailadmin/dropdown/DropdownItem";
import { useAuth } from "@/providers/AuthProvider";
import { cn } from "@/lib/utils";

export function EmsUserDropdown() {
  const [isOpen, setIsOpen] = useState(false);
  const router = useRouter();
  const { logout, user } = useAuth();

  function toggleDropdown(e: React.MouseEvent<HTMLButtonElement>) {
    e.stopPropagation();
    setIsOpen((prev) => !prev);
  }

  function closeDropdown() {
    setIsOpen(false);
  }

  async function handleSignOut() {
    closeDropdown();
    await logout();
    router.replace("/login");
  }

  const email = user?.email ?? "Signed in";

  return (
    <div className="relative">
      <button
        type="button"
        onClick={toggleDropdown}
        className="dropdown-toggle flex items-center gap-2 rounded-xl px-2 py-1.5 transition-colors hover:bg-muted/60"
      >
        <div className="rounded-full bg-brand-gradient p-[2px] shadow-brand-glow">
          <span className="flex size-10 items-center justify-center rounded-full bg-card text-muted-foreground">
            <UserCircle className="size-7" strokeWidth={1.25} />
          </span>
        </div>

        <span className="mr-1 hidden max-w-[160px] truncate text-sm font-medium text-foreground sm:block">
          {email}
        </span>

        <svg
          className={cn(
            "size-4 shrink-0 text-muted-foreground transition-transform duration-200",
            isOpen && "rotate-180",
          )}
          viewBox="0 0 18 20"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          aria-hidden
        >
          <path
            d="M4.3125 8.65625L9 13.3437L13.6875 8.65625"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </button>

      <Dropdown
        isOpen={isOpen}
        onClose={closeDropdown}
        className="absolute right-0 mt-3 flex w-[260px] flex-col rounded-2xl border border-border bg-card p-2 shadow-soft-lg"
      >
        <div className="border-b border-border px-3 py-3">
          <span className="block truncate text-sm font-semibold text-foreground">{email}</span>
          <span className="text-xs text-muted-foreground">Your account</span>
        </div>

        <ul className="flex flex-col gap-0.5 py-2">
          <li>
            <DropdownItem
              tag="a"
              href="/change-password"
              onItemClick={closeDropdown}
              className="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-foreground hover:bg-muted/60"
            >
              <KeyRound className="size-4 text-primary" />
              Change password
            </DropdownItem>
          </li>
        </ul>
        <DropdownItem
          tag="button"
          onItemClick={() => void handleSignOut()}
          className="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-error-600 hover:bg-error-50 dark:text-error-400 dark:hover:bg-error-500/10"
        >
          <LogOut className="size-4" />
          Sign out
        </DropdownItem>
      </Dropdown>
    </div>
  );
}
