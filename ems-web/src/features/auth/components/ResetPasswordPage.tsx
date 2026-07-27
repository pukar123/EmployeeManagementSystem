"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import { Button } from "@/shared/components/Button";
import { ApiAvailabilityAlert } from "@/shared/components/ApiAvailabilityAlert";
import { authService } from "@/features/auth/services/authService";

export function ResetPasswordPage() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [completed, setCompleted] = useState(false);
  const [error, setError] = useState<unknown>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    if (!token) {
      setError(new Error("This password reset link is missing its token."));
      return;
    }
    if (password !== confirmPassword) {
      setError(new Error("Passwords do not match."));
      return;
    }

    setSubmitting(true);
    try {
      await authService.resetPassword(token, password);
      setCompleted(true);
    } catch (err) {
      setError(err);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-1 flex-col justify-center px-4 py-16">
      <h1 className="text-2xl font-semibold text-foreground">Reset your password</h1>
      {completed ? (
        <div className="mt-6 space-y-4">
          <p className="rounded-lg border border-success-200 bg-success-50 p-4 text-sm text-success-700 dark:border-success-900/50 dark:bg-success-950/30 dark:text-success-300">
            Your password has been reset. You can now sign in with your new password.
          </p>
          <Link href="/login" className="block text-center text-sm font-medium text-primary hover:underline">
            Go to sign in
          </Link>
        </div>
      ) : (
        <>
          <p className="mt-2 text-sm text-muted-foreground">Choose a new password for your EMS account.</p>
          {error != null ? <ApiAvailabilityAlert error={error} className="mt-4" /> : null}
          <form onSubmit={handleSubmit} className="mt-8 space-y-5">
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="form-label normal-case">New password</span>
              <input
                type="password"
                autoComplete="new-password"
                required
                minLength={12}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                className="form-input h-11"
              />
            </label>
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="form-label normal-case">Confirm new password</span>
              <input
                type="password"
                autoComplete="new-password"
                required
                minLength={12}
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                className="form-input h-11"
              />
            </label>
            <p className="text-xs text-muted-foreground">
              Passwords must be at least 12 characters and include uppercase, lowercase, a number, and a symbol.
            </p>
            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting ? "Resetting…" : "Reset password"}
            </Button>
          </form>
        </>
      )}
    </main>
  );
}
