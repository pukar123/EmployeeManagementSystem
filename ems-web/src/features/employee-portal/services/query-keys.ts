export const employeePortalKeys = {
  all: ["employee-portal"] as const,
  summary: () => [...employeePortalKeys.all, "summary"] as const,
};
