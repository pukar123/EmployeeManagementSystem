"use client";

import { useState } from "react";
import { Bell } from "lucide-react";
import { useAuth } from "@/providers/AuthProvider";
import { Badge } from "@/shared/components";
import { useUnreadNotificationCount } from "../hooks";
import { NotificationDropdown } from "./NotificationDropdown";

export function NotificationBell() {
  const { isAuthenticated } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const { data: unreadCount } = useUnreadNotificationCount();

  if (!isAuthenticated) return null;

  const count = unreadCount?.count ?? 0;
  const badgeLabel = count > 9 ? "9+" : String(count);

  function toggleDropdown(event: React.MouseEvent<HTMLButtonElement>) {
    event.stopPropagation();
    setIsOpen((prev) => !prev);
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={toggleDropdown}
        className="dropdown-toggle relative flex h-10 w-10 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground shadow-soft transition-colors hover:bg-muted hover:text-foreground"
        aria-label="Notifications"
      >
        <Bell className="size-5" />
        {count > 0 ? (
          <Badge
            variant="danger"
            className="absolute -right-1 -top-1 min-w-5 px-1 py-0 text-[10px] leading-4"
          >
            {badgeLabel}
          </Badge>
        ) : null}
      </button>
      <NotificationDropdown isOpen={isOpen} onClose={() => setIsOpen(false)} />
    </div>
  );
}
