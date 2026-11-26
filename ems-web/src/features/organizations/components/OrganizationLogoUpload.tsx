"use client";

import { useState } from "react";
import { useUploadOrganizationLogo } from "../hooks";
import { resolveOrganizationLogoUrl } from "@/shared/utils/organization-logo-url";
import { getErrorMessage } from "@/shared/api/http-client";
import {
  isAllowedLogoFile,
  LOGO_FILE_INVALID_MESSAGE,
  LOGO_UPLOAD_RULE,
} from "@/shared/constants/upload-rules";
import { toast } from "sonner";

type Props = {
  organizationId: number;
  logoRelativePath: string | null;
};

export function OrganizationLogoUpload({ organizationId, logoRelativePath }: Props) {
  const uploadMut = useUploadOrganizationLogo(organizationId);
  const src = resolveOrganizationLogoUrl(logoRelativePath);
  // A logo that no longer loads (for example a legacy SVG, which is no longer served) shows the placeholder.
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  const showImage = Boolean(src) && failedSrc !== src;
  const inputId = `org-logo-upload-${organizationId}`;

  const onChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    // Checked here only to save a round trip; the server checks the file's content as well.
    if (!isAllowedLogoFile(file)) {
      toast.error(LOGO_FILE_INVALID_MESSAGE);
      return;
    }
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
        {showImage && src ? (
          <img
            src={src}
            alt=""
            className="max-h-full max-w-full object-contain"
            onError={() => setFailedSrc(src)}
          />
        ) : (
          <span className="px-2 text-center text-xs text-muted-foreground">No logo</span>
        )}
      </div>
      <input
        type="file"
        accept={LOGO_UPLOAD_RULE.accept}
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
      <p className="text-xs text-muted-foreground">{LOGO_UPLOAD_RULE.hint}</p>
    </div>
  );
}
