"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { OrganizationLogoUpload } from "@/features/organizations/components/OrganizationLogoUpload";
import { useUpdateOrganization } from "@/features/organizations/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

const textareaClass =
  "mt-1 w-full min-h-[120px] resize-y rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

export default function OrganizationSetupEditPage() {
  const router = useRouter();
  const { currentOrganization, needsSetup, organizationId } = useOrganizationContext();
  const updateMut = useUpdateOrganization(organizationId);

  const [name, setName] = useState<string | null>(null);
  const [code, setCode] = useState<string | null>(null);
  const [description, setDescription] = useState<string | null>(null);
  const [motto, setMotto] = useState<string | null>(null);
  const [isActive, setIsActive] = useState<boolean | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const resolvedName = name ?? currentOrganization?.name ?? "";
    const resolvedCode = code ?? currentOrganization?.code ?? "";
    const resolvedDescription = description ?? currentOrganization?.description ?? "";
    const resolvedMotto = motto ?? currentOrganization?.motto ?? "";
    const resolvedIsActive = isActive ?? currentOrganization?.isActive ?? true;

    if (!resolvedName.trim()) {
      toast.error("Organization name is required.");
      return;
    }

    try {
      await updateMut.mutateAsync({
        name: resolvedName.trim(),
        code: resolvedCode.trim() ? resolvedCode.trim() : null,
        isActive: resolvedIsActive,
        description: resolvedDescription.trim() ? resolvedDescription.trim() : null,
        motto: resolvedMotto.trim() ? resolvedMotto.trim() : null,
      });
      toast.success("Organization updated.");
      router.replace("/");
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  if (needsSetup || !currentOrganization || organizationId == null || organizationId <= 0) {
    return null;
  }

  const resolvedName = name ?? currentOrganization.name;
  const resolvedCode = code ?? currentOrganization.code ?? "";
  const resolvedDescription = description ?? currentOrganization.description ?? "";
  const resolvedMotto = motto ?? currentOrganization.motto ?? "";
  const resolvedIsActive = isActive ?? currentOrganization.isActive;

  return (
    <main className="mx-auto max-w-2xl flex-1 px-4 py-12 sm:px-6">
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm dark:bg-card">
        <h1 className="text-2xl font-semibold text-foreground">Organization settings</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Update your organization profile, logo, and active status. Configure{" "}
          <Link href="/organization/onboarding" className="text-primary hover:underline">
            onboarding checklists
          </Link>{" "}
          for preboarding employees.
        </p>

        <form onSubmit={(e) => void handleSubmit(e)} className="mt-8 space-y-8">
          <div className="flex flex-col gap-8 md:flex-row md:items-start">
            <div className="md:shrink-0">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Logo</p>
              <div className="mt-2">
                <OrganizationLogoUpload
                  organizationId={organizationId}
                  logoRelativePath={currentOrganization.logoRelativePath}
                />
              </div>
            </div>

            <div className="min-w-0 flex-1 space-y-4">
              <div>
                <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Name
                </label>
                <input
                  type="text"
                  value={resolvedName}
                  onChange={(e) => setName(e.target.value)}
                  className={inputClass}
                  required
                  autoFocus
                />
              </div>
              <div>
                <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Code
                </label>
                <input
                  type="text"
                  value={resolvedCode}
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
                  value={resolvedDescription}
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
                  value={resolvedMotto}
                  onChange={(e) => setMotto(e.target.value)}
                  className={inputClass}
                  placeholder="Optional tagline"
                />
              </div>
              <label className="flex items-center gap-2 text-sm text-foreground">
                <input
                  type="checkbox"
                  checked={resolvedIsActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="rounded border-input"
                />
                Active
              </label>
            </div>
          </div>
          <div className="flex flex-wrap gap-3 pt-2">
            <Button type="submit" disabled={updateMut.isPending}>
              {updateMut.isPending ? "Saving…" : "Save changes"}
            </Button>
            <Button type="button" variant="secondary" onClick={() => router.push("/")}>
              Cancel
            </Button>
          </div>
        </form>
      </div>
    </main>
  );
}
