"use client";

import Link from "next/link";
import { formatDistanceToNow } from "date-fns";
import { Badge } from "@/shared/components";
import type { NotificationItem } from "../types/notification.types";

type NotificationItemRowProps = {
  notification: NotificationItem;
  onMarkRead: (id: number) => void;
  isMarkingRead: boolean;
};

function severityVariant(severity: NotificationItem["severity"]) {
  switch (severity) {
    case "Critical":
    case "High":
      return "danger" as const;
    case "Low":
      return "muted" as const;
    default:
      return "default" as const;
  }
}

export function NotificationItemRow({
  notification,
  onMarkRead,
  isMarkingRead,
}: NotificationItemRowProps) {
  const createdLabel = formatDistanceToNow(new Date(notification.createdAtUtc), { addSuffix: true });

  return (
    <li
      className={`rounded-xl border px-3 py-2.5 transition-colors ${
        notification.isRead
          ? "border-border/60 bg-card/40"
          : "border-primary/20 bg-primary/5"
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0 flex-1">
          <div className="mb-1 flex flex-wrap items-center gap-2">
            <p className="truncate text-sm font-semibold text-foreground">{notification.title}</p>
            <Badge variant={severityVariant(notification.severity)} className="text-[10px] uppercase">
              {notification.severity}
            </Badge>
            {!notification.isRead ? (
              <Badge variant="primary" className="text-[10px] uppercase">
                New
              </Badge>
            ) : null}
          </div>
          <p className="text-xs text-muted-foreground">{notification.body}</p>
          <p className="mt-1 text-[11px] text-muted-foreground">{createdLabel}</p>
          {notification.actionUrl ? (
            <Link
              href={notification.actionUrl}
              className="mt-2 inline-block text-xs font-medium text-primary underline-offset-4 hover:underline"
            >
              View details
            </Link>
          ) : null}
        </div>
        {!notification.isRead ? (
          <button
            type="button"
            disabled={isMarkingRead}
            onClick={() => onMarkRead(notification.id)}
            className="shrink-0 text-xs font-medium text-primary hover:underline disabled:opacity-50"
          >
            Mark read
          </button>
        ) : null}
      </div>
    </li>
  );
}
