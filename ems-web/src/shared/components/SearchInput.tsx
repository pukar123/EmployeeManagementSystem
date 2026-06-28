import { Search } from "lucide-react";
import { cn } from "@/shared/utils/cn";
import type { InputHTMLAttributes } from "react";

type SearchInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  containerClassName?: string;
};

export function SearchInput({ className, containerClassName, ...props }: SearchInputProps) {
  return (
    <div className={cn("relative max-w-md", containerClassName)}>
      <Search
        className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <input
        type="search"
        className={cn(
          "w-full rounded-xl border border-input bg-background py-2.5 pr-3 pl-10 text-sm text-foreground shadow-soft",
          "placeholder:text-muted-foreground",
          "focus:border-primary/50 focus:ring-2 focus:ring-primary/20 focus:outline-none",
          "dark:bg-card",
          className,
        )}
        {...props}
      />
    </div>
  );
}
