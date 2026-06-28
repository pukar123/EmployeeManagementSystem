import { cn } from "@/shared/utils/cn";
import type { HTMLAttributes } from "react";

const variantStyles = {
  default: "bg-secondary text-secondary-foreground",
  primary: "bg-primary/15 text-primary dark:bg-primary/20",
  success: "bg-success-50 text-success-700 dark:bg-success-500/15 dark:text-success-400",
  warning: "bg-warning-50 text-warning-700 dark:bg-warning-500/15 dark:text-warning-400",
  danger: "bg-error-50 text-error-700 dark:bg-error-500/15 dark:text-error-400",
  info: "bg-brand-50 text-brand-700 dark:bg-brand-500/15 dark:text-brand-300",
  muted: "bg-muted text-muted-foreground",
} as const;

type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  variant?: keyof typeof variantStyles;
};

export function Badge({ className, variant = "default", ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium",
        variantStyles[variant],
        className,
      )}
      {...props}
    />
  );
}

export function StatusPill({
  label,
  variant = "muted",
  className,
}: {
  label: string;
  variant?: keyof typeof variantStyles;
  className?: string;
}) {
  return (
    <Badge variant={variant} className={cn("font-semibold", className)}>
      {label}
    </Badge>
  );
}
