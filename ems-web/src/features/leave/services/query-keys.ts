export const leaveKeys = {
  all: ["leave"] as const,
  types: (organizationId: number) => [...leaveKeys.all, "types", organizationId] as const,
  balances: (employeeId: number) => [...leaveKeys.all, "balances", employeeId] as const,
  requests: (employeeId: number) => [...leaveKeys.all, "requests", employeeId] as const,
  policyRules: (leaveTypeId: number) => [...leaveKeys.all, "policyRules", leaveTypeId] as const,
};
