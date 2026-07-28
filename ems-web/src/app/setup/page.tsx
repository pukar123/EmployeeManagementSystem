"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { useCreateOrganization } from "@/features/organizations/hooks";
import { organizationKeys } from "@/features/organizations/services/query-keys";
import { organizationService } from "@/features/organizations/services/organizationService";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

const textareaClass =
  "mt-1 w-full min-h-[120px] resize-y rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

export default function SetupPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { needsSetup } = useOrganizationContext();
  const createMut = useCreateOrganization();

  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [description, setDescription] = useState("");
  const [motto, setMotto] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const logoPreview = useMemo(() => (logoFile ? URL.createObjectURL(logoFile) : null), [logoFile]);

  useEffect(() => {
    return () => {
      if (logoPreview) URL.revokeObjectURL(logoPreview);
    };
  }, [logoPreview]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      toast.error("Organization name is required.");
      return;
    }

    try {
      const created = await createMut.mutateAsync({
        name: name.trim(),
        code: code.trim() ? code.trim() : null,
        isActive,
        description: description.trim() ? description.trim() : null,
        motto: motto.trim() ? motto.trim() : null,
      });
      if (logoFile) {
        await organizationService.uploadOrganizationLogo(created.id, logoFile);
        void queryClient.invalidateQueries({ queryKey: organizationKeys.list() });
      }
      toast.success("Organization created.");
      router.replace("/");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  if (!needsSetup) {
    return null;
  }

  const previewSrc = logoPreview ?? null;

  return (
    <main className="mx-auto max-w-2xl flex-1 px-4 py-12 sm:px-6">
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm dark:bg-card">
        <h1 className="text-2xl font-semibold text-foreground">Set up your organization</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          This instance supports a single organization. Enter your company details to continue.
        </p>

        <form onSubmit={(e) => void handleSubmit(e)} className="mt-8 space-y-8">
          <div className="flex flex-col gap-8 md:flex-row md:items-start">
            <div className="flex flex-col items-start gap-2 md:shrink-0">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Logo</p>
              <div className="flex h-32 w-32 items-center justify-center overflow-hidden rounded-xl border border-border bg-muted/50 dark:border-border dark:bg-card">
                {previewSrc ? (
                  <img src={previewSrc} alt="" className="max-h-full max-w-full object-contain" />
                ) : (
                  <span className="px-2 text-center text-xs text-muted-foreground">No logo</span>
                )}
              </div>
              <input
                type="file"
                accept="image/png,image/jpeg,image/jpg,image/webp,image/gif,image/svg+xml"
                className="sr-only"
                id="setup-org-logo"
                onChange={(e) => {
                  const f = e.target.files?.[0] ?? null;
                  setLogoFile(f);
                  e.target.value = "";
                }}
                disabled={createMut.isPending}
              />
              <label
                htmlFor="setup-org-logo"
                className="cursor-pointer rounded-lg border border-input bg-background px-3 py-1.5 text-xs font-medium text-foreground shadow-sm hover:bg-muted/50 dark:bg-card dark:text-foreground dark:hover:bg-muted/60"
              >
                Choose image
              </label>
            </div>

            <div className="min-w-0 flex-1 space-y-4">
              <div>
                <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Name
                </label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className={inputClass}
                  required
                  autoFocus
                  placeholder="Acme Corp"
                />
              </div>
              <div>
                <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Code
                </label>
                <input
                  type="text"
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  className={inputClass}
                  placeholder="Optional short code"
                />
              </div>
              <div>
                <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Description
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  className={textareaClass}
                  placeholder="What does your organization do?"
                  rows={4}
                />
              </div>
              <div>
                <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Motto
                </label>
                <input
                  type="text"
                  value={motto}
                  onChange={(e) => setMotto(e.target.value)}
                  className={inputClass}
                  placeholder="Optional tagline"
                />
              </div>
              <label className="flex items-center gap-2 text-sm text-foreground">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="rounded border-input"
                />
                Active
              </label>
            </div>
          </div>
          <div className="pt-2">
            <Button type="submit" disabled={createMut.isPending} className="w-full sm:w-auto">
              {createMut.isPending ? "Creating…" : "Continue"}
            </Button>
          </div>
        </form>
      </div>
    </main>
  );
}
