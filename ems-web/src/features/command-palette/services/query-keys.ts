export const commandPaletteKeys = {
  all: ["command-palette"] as const,
  search: (organizationId: number | null, query: string, canViewEmployees: boolean) =>
    [...commandPaletteKeys.all, "search", organizationId, query, canViewEmployees] as const,
};
