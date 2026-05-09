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
  "mt-1 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100";

const textareaClass =
  "mt-1 w-full min-h-[120px] resize-y rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm focus:border-zinc-500 focus:outline-none focus:ring-1 focus:ring-zinc-500 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100";

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
      <div className="rounded-xl border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-700 dark:bg-zinc-950">
        <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-50">Set up your organization</h1>
        <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
          This instance supports a single organization. Enter your company details to continue.
        </p>

        <form onSubmit={(e) => void handleSubmit(e)} className="mt-8 space-y-8">
          <div className="flex flex-col gap-8 md:flex-row md:items-start">
            <div className="flex flex-col items-start gap-2 md:shrink-0">
              <p className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Logo</p>
              <div className="flex h-32 w-32 items-center justify-center overflow-hidden rounded-xl border border-zinc-200 bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-900">
                {previewSrc ? (
                  <img src={previewSrc} alt="" className="max-h-full max-w-full object-contain" />
                ) : (
                  <span className="px-2 text-center text-xs text-zinc-400">No logo</span>
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
                className="cursor-pointer rounded-lg border border-zinc-300 bg-white px-3 py-1.5 text-xs font-medium text-zinc-800 shadow-sm hover:bg-zinc-50 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-zinc-800"
              >
                Choose image
              </label>
            </div>

            <div className="min-w-0 flex-1 space-y-4">
              <div>
                <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
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
                <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
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
                <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
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
                <label className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
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
              <label className="flex items-center gap-2 text-sm text-zinc-800 dark:text-zinc-200">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="rounded border-zinc-300 dark:border-zinc-600"
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
