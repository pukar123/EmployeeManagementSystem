"use client";

import { useUploadOrganizationLogo } from "../hooks";
import { resolveOrganizationLogoUrl } from "@/shared/utils/organization-logo-url";
import { getErrorMessage } from "@/shared/api/http-client";
import { toast } from "sonner";

type Props = {
  organizationId: number;
  logoRelativePath: string | null;
};

export function OrganizationLogoUpload({ organizationId, logoRelativePath }: Props) {
  const uploadMut = useUploadOrganizationLogo(organizationId);
  const src = resolveOrganizationLogoUrl(logoRelativePath);
  const inputId = `org-logo-upload-${organizationId}`;

  const onChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    try {
      await uploadMut.mutateAsync(file);
      toast.success("Logo updated.");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  return (
    <div className="flex flex-col items-start gap-2">
      <div className="flex h-32 w-32 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-border bg-muted/50 dark:border-border dark:bg-card">
        {src ? (
          <img src={src} alt="" className="max-h-full max-w-full object-contain" />
        ) : (
          <span className="px-2 text-center text-xs text-muted-foreground">No logo</span>
        )}
      </div>
      <input
        type="file"
        accept="image/png,image/jpeg,image/jpg,image/webp,image/gif,image/svg+xml"
        className="sr-only"
        id={inputId}
        onChange={(e) => void onChange(e)}
        disabled={uploadMut.isPending}
      />
      <label
        htmlFor={inputId}
        className="cursor-pointer rounded-lg border border-input bg-background px-3 py-1.5 text-xs font-medium text-foreground shadow-sm hover:bg-muted/50 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-card dark:text-foreground dark:hover:bg-muted/60"
      >
        {uploadMut.isPending ? "Uploading…" : "Change logo"}
      </label>
    </div>
  );
}
