/** Aligns with EMS.Application.DTOs.Site (camelCase JSON). */
export type Site = {
  siteId: number;
  siteName: string;
  siteDescription: string | null;
  siteLocation: string;
  isActive: boolean;
  isDeleted: boolean;
};

export type CreateSiteRequest = {
  siteName: string;
  siteDescription?: string | null;
  siteLocation: string;
  isActive: boolean;
};

export type UpdateSiteRequest = {
  siteName: string;
  siteDescription?: string | null;
  siteLocation: string;
  isActive: boolean;
};
