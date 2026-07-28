"use client";

import { useQuery } from "@tanstack/react-query";
import {
  fetchMyEmployeeCapabilities,
  type EmployeeAccessCapabilities,
} from "../services/employeeAccessApi";

const defaultCapabilities: EmployeeAccessCapabilities = {
  view: false,
  manage: false,
  access: false,
  export: false,
};

export function useEmployeeCapabilities() {
  const query = useQuery({
    queryKey: ["employee-access", "me"],
    queryFn: fetchMyEmployeeCapabilities,
    staleTime: 60_000,
  });

  return {
    capabilities: query.data ?? defaultCapabilities,
    isLoading: query.isLoading,
    isError: query.isError,
    refetch: query.refetch,
  };
}
