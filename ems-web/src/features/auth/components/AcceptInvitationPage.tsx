"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { Button } from "@/shared/components/Button";
import { ApiAvailabilityAlert } from "@/shared/components/ApiAvailabilityAlert";
import { getErrorMessage } from "@/shared/api/http-client";
import { authService } from "@/features/auth/services/authService";

export function AcceptInvitationPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const token = searchParams.get("token") ?? "";
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!token) {
      toast.error("Invitation token is missing.");
      return;
    }
    if (password.length < 12) {
      toast.error("Password must be at least 12 characters.");
      return;
    }
    if (password !== confirm) {
      toast.error("Passwords do not match.");
      return;
    }
    setBusy(true);
    try {
      await authService.acceptInvitation(token, password);
      toast.success("Account activated. You can sign in now.");
      router.push("/login");
    } catch (err) {
      setError(err);
      toast.error(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="mx-auto flex min-h-[60vh] max-w-md flex-1 flex-col justify-center px-4 py-10">
      <h1 className="text-2xl font-semibold text-foreground">Accept invitation</h1>
      <p className="mt-2 text-sm text-muted-foreground">Set a password to activate your account.</p>
      {error != null ? <ApiAvailabilityAlert error={error} className="mt-4" /> : null}
      <form onSubmit={(e) => void handleSubmit(e)} className="mt-8 space-y-4">
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">New password</label>
          <input
            type="password"
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
            required
            minLength={8}
          />
        </div>
        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Confirm password</label>
          <input
            type="password"
            className="mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            autoComplete="new-password"
            required
            minLength={8}
          />
        </div>
        <Button type="submit" disabled={busy} className="w-full">
          {busy ? "Activating…" : "Activate account"}
        </Button>
      </form>
    </main>
  );
}
