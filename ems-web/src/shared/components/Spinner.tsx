import { cn } from "@/shared/utils/cn";

export function Spinner({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        "size-8 animate-spin rounded-full border-2 border-muted border-t-primary",
        className,
      )}
      role="status"
      aria-label="Loading"
    />
  );
}
