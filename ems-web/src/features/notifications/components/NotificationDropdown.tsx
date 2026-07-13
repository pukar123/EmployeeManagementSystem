"use client";

import { Bell } from "lucide-react";
import { ApiAvailabilityAlert } from "@/shared/components/ApiAvailabilityAlert";
import { Button, EmptyState, Spinner } from "@/shared/components";
import { Dropdown } from "@/components/tailadmin/dropdown/Dropdown";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
} from "../hooks";
import { NotificationItemRow } from "./NotificationItem";

type NotificationDropdownProps = {
  isOpen: boolean;
  onClose: () => void;
};

export function NotificationDropdown({ isOpen, onClose }: NotificationDropdownProps) {
  const { data: notifications = [], isLoading, isError, error } = useNotifications({ take: 30 });
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  return (
    <Dropdown
      isOpen={isOpen}
      onClose={onClose}
      className="absolute right-0 mt-3 flex w-[min(24rem,calc(100vw-2rem))] flex-col rounded-2xl border border-border bg-card p-3 shadow-soft-lg"
    >
      <div className="mb-2 flex items-center justify-between gap-2 border-b border-border pb-2">
        <div>
          <p className="text-sm font-semibold text-foreground">Notifications</p>
          <p className="text-xs text-muted-foreground">Your recent activity</p>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          loading={markAllRead.isPending}
          disabled={notifications.every((n) => n.isRead)}
          onClick={() => markAllRead.mutate()}
        >
          Mark all read
        </Button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-8">
          <Spinner />
        </div>
      ) : isError ? (
        <ApiAvailabilityAlert error={error} />
      ) : notifications.length === 0 ? (
        <EmptyState
          icon={<Bell className="size-8" aria-hidden />}
          message="No notifications"
          description="You're all caught up."
        />
      ) : (
        <ul className="flex max-h-[24rem] flex-col gap-2 overflow-y-auto pr-1">
          {notifications.map((notification) => (
            <NotificationItemRow
              key={notification.id}
              notification={notification}
              onMarkRead={(id) => markRead.mutate(id)}
              isMarkingRead={markRead.isPending}
            />
          ))}
        </ul>
      )}
    </Dropdown>
  );
}
