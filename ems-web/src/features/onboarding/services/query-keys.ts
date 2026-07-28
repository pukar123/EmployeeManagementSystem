export const onboardingKeys = {
  all: ["onboarding"] as const,
  templates: (organizationId: number) => [...onboardingKeys.all, "templates", organizationId] as const,
  employeeProgress: (employeeId: number) => [...onboardingKeys.all, "employee-progress", employeeId] as const,
};
