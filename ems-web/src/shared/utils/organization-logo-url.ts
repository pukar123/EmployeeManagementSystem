import { emsApiBaseUrl } from "@/shared/api/http-client";

/** Full URL for `<img src>` when the API stores a web-relative path (e.g. `/uploads/organizations/1/logo.png`). */
export function resolveOrganizationLogoUrl(logoRelativePath: string | null | undefined): string | null {
  if (!logoRelativePath) return null;
  const base = emsApiBaseUrl.replace(/\/$/, "");
  const path = logoRelativePath.startsWith("/") ? logoRelativePath : `/${logoRelativePath}`;
  return `${base}${path}`;
}
