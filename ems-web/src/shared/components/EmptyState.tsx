import { Inbox } from "lucide-react";
import { cn } from "@/shared/utils/cn";
import type { ReactNode } from "react";

type EmptyStateProps = {
  message?: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
  className?: string;
};

export function EmptyState({
  message = "No records found.",
  description,
  icon,
  action,
  className,
}: EmptyStateProps) {
  return (
    <div className={cn("flex flex-col items-center justify-center gap-3 px-6 py-12 text-center", className)}>
      <div className="rounded-2xl bg-muted/60 p-4 text-muted-foreground">
        {icon ?? <Inbox className="size-8" aria-hidden />}
      </div>
      <div className="space-y-1">
        <p className="text-sm font-medium text-foreground">{message}</p>
        {description ? <p className="text-xs text-muted-foreground">{description}</p> : null}
      </div>
      {action}
    </div>
  );
}
