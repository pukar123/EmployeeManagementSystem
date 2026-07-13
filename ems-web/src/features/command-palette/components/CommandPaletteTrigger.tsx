"use client";

import { Search } from "lucide-react";

import { cn } from "@/lib/utils";
import { useCommandPalette } from "../hooks/useCommandPalette";

type CommandPaletteTriggerProps = {
  className?: string;
  compact?: boolean;
};

export function CommandPaletteTrigger({ className, compact = false }: CommandPaletteTriggerProps) {
  const { openPalette } = useCommandPalette();

  if (compact) {
    return (
      <button
        type="button"
        onClick={openPalette}
        className={cn(
          "z-99999 flex h-10 w-10 items-center justify-center rounded-xl border border-border bg-card text-muted-foreground shadow-soft transition-colors hover:bg-muted hover:text-foreground lg:hidden",
          className,
        )}
        aria-label="Open command palette"
      >
        <Search className="size-4" aria-hidden />
      </button>
    );
  }

  return (
    <button
      type="button"
      onClick={openPalette}
      className={cn(
        "relative h-11 w-full rounded-xl border border-input bg-card/80 py-2.5 pr-14 pl-11 text-left text-sm text-muted-foreground shadow-soft transition-colors hover:border-primary/40 hover:bg-card focus:border-primary/50 focus:ring-2 focus:ring-primary/20 focus:outline-none xl:w-[400px]",
        className,
      )}
      aria-label="Open command palette"
    >
      <Search
        className="pointer-events-none absolute top-1/2 left-4 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <span>Search or type command...</span>
      <span className="absolute top-1/2 right-2.5 inline-flex -translate-y-1/2 items-center gap-0.5 rounded-lg border border-border bg-muted/60 px-2 py-1 text-[10px] font-medium tracking-wide text-muted-foreground">
        <span>⌘</span>
        <span>K</span>
      </span>
    </button>
  );
}
