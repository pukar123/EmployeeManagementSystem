"use client";

import { useEffect } from "react";

import { useCommandPaletteStore } from "../store/command-palette-store";

export function useCommandPalette() {
  const open = useCommandPaletteStore((state) => state.open);
  const setOpen = useCommandPaletteStore((state) => state.setOpen);
  const openPalette = useCommandPaletteStore((state) => state.openPalette);
  const closePalette = useCommandPaletteStore((state) => state.closePalette);
  const togglePalette = useCommandPaletteStore((state) => state.togglePalette);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        openPalette();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [openPalette]);

  return {
    open,
    setOpen,
    openPalette,
    closePalette,
    togglePalette,
  };
}
