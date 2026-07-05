"use client";

import Image from "next/image";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/providers/AuthProvider";
import { ApiAvailabilityAlert } from "@/shared/components/ApiAvailabilityAlert";
import { Button } from "@/shared/components/Button";

const inputClass =
  "form-input h-11";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [email, setEmail] = useState("admin@localhost");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<unknown>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email.trim(), password);
      router.replace("/");
    } catch (err) {
      setError(err);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="relative flex min-h-screen flex-1 flex-col justify-center overflow-hidden bg-background px-4 py-16 sm:px-6">
      <div
        className="pointer-events-none absolute -top-32 left-1/2 size-96 -translate-x-1/2 rounded-full bg-brand-gradient opacity-20 blur-3xl"
        aria-hidden
      />
      <div className="relative mx-auto w-full max-w-md">
        <div className="mb-8 flex justify-center">
          <Image
            src="/images/logo/logo.svg"
            alt="EMS"
            width={180}
            height={48}
            className="dark:hidden"
            priority
          />
          <Image
            src="/images/logo/logo-dark.svg"
            alt="EMS"
            width={180}
            height={48}
            className="hidden dark:block"
            priority
          />
        </div>
        <div className="rounded-3xl border border-border bg-card p-8 shadow-soft-lg">
          <p className="text-xs font-semibold uppercase tracking-widest text-primary">Welcome</p>
          <h1 className="mt-2 text-2xl font-bold tracking-tight text-foreground">Sign in to EMS</h1>
          <p className="mt-2 text-sm text-muted-foreground">Use your account to continue.</p>
          <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-5">
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="form-label normal-case">Email</span>
              <input
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className={inputClass}
              />
            </label>
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="form-label normal-case">Password</span>
              <input
                type="password"
                autoComplete="current-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={inputClass}
              />
            </label>
            {error != null ? <ApiAvailabilityAlert error={error} /> : null}
            <Button type="submit" className="w-full" loading={submitting} disabled={submitting}>
              {submitting ? "Signing in…" : "Sign in"}
            </Button>
          </form>
        </div>
      </div>
    </main>
  );
}
