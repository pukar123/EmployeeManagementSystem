"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createElement } from "react";

import {
  Command,
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from "@/components/ui/command";
import { Spinner } from "@/shared/components/Spinner";

import { useCommandPalette } from "../hooks/useCommandPalette";
import { useGlobalCommandSearch } from "../hooks/useGlobalCommandSearch";
import { COMMAND_PALETTE_GROUP_LABELS } from "../types/command-palette.types";

export function GlobalCommandPalette() {
  const router = useRouter();
  const { open, setOpen, closePalette } = useCommandPalette();
  const [query, setQuery] = useState("");
  const { debouncedQuery, groupedItems, orderedGroups, isLoading, errors, hasResults, navError } =
    useGlobalCommandSearch(query, open);

  const handleOpenChange = (nextOpen: boolean) => {
    setOpen(nextOpen);
    if (!nextOpen) {
      setQuery("");
    }
  };

  const handleSelect = (href: string) => {
    closePalette();
    router.push(href);
  };

  return (
    <CommandDialog
      open={open}
      onOpenChange={handleOpenChange}
      title="Search EMS"
      description="Search employees, departments, pages, and quick actions"
      className="top-[20%] translate-y-0"
    >
      <Command shouldFilter={false}>
        <CommandInput
          placeholder="Search employees, departments, pages..."
          value={query}
          onValueChange={setQuery}
        />
        <CommandList>
          {isLoading ? (
            <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
              <Spinner className="size-4" />
              Searching...
            </div>
          ) : null}

          {!isLoading && !hasResults ? (
            <CommandEmpty>
              {debouncedQuery.trim()
                ? `No results for "${debouncedQuery.trim()}"`
                : "Type to search or pick a quick action"}
            </CommandEmpty>
          ) : null}

          {navError ? (
            <p className="px-3 py-2 text-xs text-destructive">Could not load navigation menus. {navError}</p>
          ) : null}

          {orderedGroups.map((group, index) => {
            const items = groupedItems.get(group) ?? [];
            const groupError = errors[group];

            return (
              <div key={group}>
                {index > 0 ? <CommandSeparator /> : null}
                <CommandGroup heading={COMMAND_PALETTE_GROUP_LABELS[group]}>
                  {groupError ? (
                    <p className="px-2 py-1.5 text-xs text-destructive">{groupError}</p>
                  ) : null}
                  {items.map((item) => (
                    <CommandItem
                      key={item.id}
                      value={item.id}
                      keywords={item.keywords}
                      onSelect={() => handleSelect(item.href)}
                    >
                      {createElement(item.icon, { className: "size-4 text-muted-foreground", "aria-hidden": true })}
                      <div className="min-w-0 flex-1">
                        <p className="truncate">{item.label}</p>
                        {item.subtitle ? (
                          <p className="truncate text-xs text-muted-foreground">{item.subtitle}</p>
                        ) : null}
                      </div>
                    </CommandItem>
                  ))}
                </CommandGroup>
              </div>
            );
          })}
        </CommandList>
      </Command>
    </CommandDialog>
  );
}
