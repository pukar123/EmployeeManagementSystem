import { useMemo, useState } from "react";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { defaultManagerTeamQuery, type ManagerTeamQuery } from "../types/manager-team.types";
import { useManagerTeam } from "./useManagerTeam";

export function useManagerTeamController() {
  const { organizationId } = useOrganizationContext();
  const [filterState, setFilterState] = useState<ManagerTeamQuery | null>(null);
  const [selectedManagerId, setSelectedManagerId] = useState<number | null>(null);
  const [adminMode, setAdminMode] = useState(false);

  const baseQuery = useMemo(() => {
    if (!organizationId) return null;
    return filterState ?? defaultManagerTeamQuery(organizationId);
  }, [organizationId, filterState]);

  const effectiveQuery = useMemo(() => {
    if (!baseQuery) return null;
    if (adminMode && selectedManagerId == null) return null;
    return {
      ...baseQuery,
      organizationId,
      managerId: adminMode ? selectedManagerId : undefined,
    } as ManagerTeamQuery;
  }, [baseQuery, organizationId, adminMode, selectedManagerId]);

  const dashboardQuery = useManagerTeam(effectiveQuery);

  const updateQuery = (patch: Partial<ManagerTeamQuery>) => {
    setFilterState((prev) => {
      if (!organizationId) return prev;
      const base = prev ?? defaultManagerTeamQuery(organizationId);
      return { ...base, ...patch };
    });
  };

  const resetFilters = () => {
    if (!organizationId) return;
    setFilterState(defaultManagerTeamQuery(organizationId));
  };

  return {
    organizationId,
    query: baseQuery,
    effectiveQuery,
    dashboardQuery,
    selectedManagerId,
    setSelectedManagerId,
    adminMode,
    setAdminMode,
    updateQuery,
    resetFilters,
  };
}
