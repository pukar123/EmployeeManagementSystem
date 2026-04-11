export const siteKeys = {
  all: ["sites"] as const,
  list: () => [...siteKeys.all, "list"] as const,
};
