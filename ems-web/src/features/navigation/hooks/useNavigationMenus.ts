"use client";

import { useQuery } from "@tanstack/react-query";
import { fetchNavigationMenus } from "../services/navigationService";

export function useNavigationMenus(enabled: boolean) {
  return useQuery({
    queryKey: ["navigation", "menus"],
    queryFn: fetchNavigationMenus,
    enabled,
    staleTime: 60_000,
  });
}
