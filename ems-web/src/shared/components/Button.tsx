import { cn } from "@/shared/utils/cn";
import type { ButtonHTMLAttributes } from "react";
import { Spinner } from "./Spinner";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "danger" | "ghost";
  size?: "sm" | "md";
  loading?: boolean;
};

export function Button({
  className,
  variant = "primary",
  size = "md",
  loading = false,
  disabled,
  type = "button",
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={cn(
        "inline-flex items-center justify-center gap-2 rounded-xl font-medium transition-all duration-200",
        "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
        "disabled:pointer-events-none disabled:opacity-50",
        size === "md" && "px-4 py-2.5 text-sm",
        size === "sm" && "px-3 py-1.5 text-xs",
        variant === "primary" &&
          "bg-brand-gradient text-white shadow-brand-glow hover:brightness-110 active:scale-[0.98]",
        variant === "secondary" &&
          "border border-border bg-card text-foreground shadow-soft hover:bg-muted/60",
        variant === "danger" && "bg-error-600 text-white hover:bg-error-700 shadow-soft",
        variant === "ghost" && "text-muted-foreground hover:bg-muted hover:text-foreground",
        className,
      )}
      {...props}
    >
      {loading ? <Spinner className="size-4" /> : null}
      {children}
    </button>
  );
}
