"use client";

import Image from "next/image";
import Link from "next/link";
import { useState } from "react";
import { Button } from "@/shared/components/Button";
import { ApiAvailabilityAlert } from "@/shared/components/ApiAvailabilityAlert";
import { getErrorMessage } from "@/shared/api/http-client";
import { authService } from "@/features/auth/services/authService";

const inputClass = "form-input h-11";

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<unknown>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await authService.forgotPassword(email.trim());
      setSubmitted(true);
    } catch (err) {
      setError(err);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="relative flex min-h-screen flex-1 flex-col justify-center overflow-hidden bg-background px-4 py-16 sm:px-6">
      <div className="relative mx-auto w-full max-w-md">
        <div className="mb-8 flex justify-center">
          <Image src="/images/logo/logo.svg" alt="EMS" width={180} height={48} className="dark:hidden" priority />
          <Image src="/images/logo/logo-dark.svg" alt="EMS" width={180} height={48} className="hidden dark:block" priority />
        </div>
        <div className="rounded-3xl border border-border bg-card p-8 shadow-soft-lg">
          <p className="text-xs font-semibold uppercase tracking-widest text-primary">Account recovery</p>
          <h1 className="mt-2 text-2xl font-bold tracking-tight text-foreground">Forgot your password?</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Enter your email address and we’ll send a password reset link if an account exists.
          </p>
          {submitted ? (
            <div className="mt-8 space-y-4">
              <p className="rounded-lg border border-success-200 bg-success-50 p-4 text-sm text-success-700 dark:border-success-900/50 dark:bg-success-950/30 dark:text-success-300">
                If an account exists for that email, a reset link has been sent. Check your inbox and spam folder.
              </p>
              <Link href="/login" className="block text-center text-sm font-medium text-primary hover:underline">
                Return to sign in
              </Link>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-5">
              <label className="flex flex-col gap-1.5 text-sm">
                <span className="form-label normal-case">Email</span>
                <input
                  type="email"
                  autoComplete="email"
                  required
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  className={inputClass}
                />
              </label>
              {error != null ? <ApiAvailabilityAlert error={error} /> : null}
              <Button type="submit" className="w-full" loading={submitting} disabled={submitting}>
                {submitting ? "Sending…" : "Send reset link"}
              </Button>
              <Link href="/login" className="text-center text-sm font-medium text-primary hover:underline">
                Return to sign in
              </Link>
            </form>
          )}
          {error != null && !submitted ? (
            <p className="sr-only" role="alert">{getErrorMessage(error)}</p>
          ) : null}
        </div>
      </div>
    </main>
  );
}
