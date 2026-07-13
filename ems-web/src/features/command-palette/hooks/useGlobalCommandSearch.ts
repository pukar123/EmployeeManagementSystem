"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { getErrorMessage } from "@/shared/api/http-client";
import { useDebouncedValue } from "@/shared/hooks/useDebouncedValue";
import { emsNavItemsSetup } from "@/shared/components/layout/ems-nav-items";
import { useNavigationMenus } from "@/features/navigation/hooks/useNavigationMenus";
import { useEmployeeCapabilities } from "@/features/employees/hooks/useEmployeeCapabilities";
import { useOrganizationContext } from "@/providers/OrganizationProvider";

import { searchCommandPaletteEntities } from "../services/commandPaletteSearchService";
import { commandPaletteKeys } from "../services/query-keys";
import type { CommandPaletteGroup, CommandPaletteItem, CommandPaletteSearchResult } from "../types/command-palette.types";
import { buildQuickActions } from "../utils/buildQuickActions";
import {
  buildAllowedRoutes,
  flattenNavigationMenus,
  flattenStaticNavItems,
} from "../utils/flattenMenus";
import { buildRouteItems, groupCommandItems } from "../utils/matchItems";

const GROUP_ORDER: CommandPaletteGroup[] = [
  "quick-actions",
  "pages",
  "employees",
  "departments",
  "positions",
  "sites",
  "tasks",
];

export function useGlobalCommandSearch(query: string, enabled: boolean) {
  const debouncedQuery = useDebouncedValue(query, 300);
  const { organizationId, needsSetup } = useOrganizationContext();
  const { capabilities } = useEmployeeCapabilities();
  const navQuery = useNavigationMenus(!needsSetup && enabled);

  const flatMenus = useMemo(() => {
    if (needsSetup) {
      return flattenStaticNavItems(emsNavItemsSetup);
    }
    if (navQuery.data) {
      return flattenNavigationMenus(navQuery.data);
    }
    return [];
  }, [needsSetup, navQuery.data]);

  const allowedRoutes = useMemo(() => buildAllowedRoutes(flatMenus), [flatMenus]);

  const searchContext = useMemo(
    () => ({
      organizationId,
      needsSetup,
      canViewEmployees: capabilities.view,
      canManageEmployees: capabilities.manage,
      allowedRoutes,
    }),
    [allowedRoutes, capabilities.manage, capabilities.view, needsSetup, organizationId],
  );

  const entityQuery = useQuery({
    queryKey: commandPaletteKeys.search(organizationId, debouncedQuery, capabilities.view),
    queryFn: () => searchCommandPaletteEntities(debouncedQuery, searchContext),
    enabled: enabled && debouncedQuery.trim().length >= 2 && organizationId != null,
    staleTime: 30_000,
  });

  const result = useMemo<CommandPaletteSearchResult>(() => {
    const errors: Partial<Record<CommandPaletteGroup, string>> = {};
    if (entityQuery.isError) {
      errors.employees = getErrorMessage(entityQuery.error);
    }

    const quickActions = buildQuickActions({
      allowedRoutes,
      canManageEmployees: capabilities.manage,
      query: debouncedQuery,
    });

    const pages = buildRouteItems(flatMenus, debouncedQuery);
    const entityItems: CommandPaletteItem[] = entityQuery.data
      ? [
          ...entityQuery.data.employees,
          ...entityQuery.data.departments,
          ...entityQuery.data.positions,
          ...entityQuery.data.sites,
          ...entityQuery.data.tasks,
        ]
      : [];

    const items = [...quickActions, ...pages, ...entityItems];

    return {
      items,
      isLoading: entityQuery.isFetching && debouncedQuery.trim().length >= 2,
      errors,
    };
  }, [
    allowedRoutes,
    capabilities.manage,
    debouncedQuery,
    entityQuery.data,
    entityQuery.error,
    entityQuery.isError,
    entityQuery.isFetching,
    flatMenus,
  ]);

  const groupedItems = useMemo(() => groupCommandItems(result.items), [result.items]);

  const orderedGroups = useMemo(
    () => GROUP_ORDER.filter((group) => (groupedItems.get(group)?.length ?? 0) > 0),
    [groupedItems],
  );

  return {
    debouncedQuery,
    groupedItems,
    orderedGroups,
    isLoading: result.isLoading || (navQuery.isLoading && !needsSetup),
    errors: result.errors,
    hasResults: result.items.length > 0,
    navError: navQuery.isError ? getErrorMessage(navQuery.error) : null,
  };
}
