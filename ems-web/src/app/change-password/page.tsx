"use client";

import Image from "next/image";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/providers/AuthProvider";
import { getErrorMessage } from "@/shared/api/http-client";

const inputClass =
  "shadow-theme-xs h-11 w-full rounded-lg border border-gray-200 bg-transparent px-4 py-2.5 text-sm text-gray-800 placeholder:text-gray-400 focus:border-brand-300 focus:ring-3 focus:ring-brand-500/10 focus:outline-hidden dark:border-gray-800 dark:bg-white/[0.03] dark:text-white/90 dark:placeholder:text-white/30 dark:focus:border-brand-800";

export default function ChangePasswordPage() {
  const router = useRouter();
  const { completePasswordChange } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!currentPassword || !newPassword) {
      setError("Current and new passwords are required.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("New password and confirmation do not match.");
      return;
    }

    setSubmitting(true);
    try {
      await completePasswordChange(currentPassword, newPassword);
      router.replace("/");
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-[85vh] flex-1 flex-col justify-center bg-gray-50 px-4 py-16 dark:bg-gray-900 sm:px-6">
      <div className="mx-auto w-full max-w-md">
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
        <div className="rounded-2xl border border-gray-200 bg-white p-8 shadow-theme-lg dark:border-gray-800 dark:bg-white/[0.03]">
          <h1 className="text-title-sm font-semibold text-gray-800 sm:text-title-md dark:text-white/90">
            Change password
          </h1>
          <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
            Update your temporary password before continuing.
          </p>
          <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-5">
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="font-medium text-gray-700 dark:text-gray-300">Current password</span>
              <input
                type="password"
                autoComplete="current-password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                className={inputClass}
                required
              />
            </label>
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="font-medium text-gray-700 dark:text-gray-300">New password</span>
              <input
                type="password"
                autoComplete="new-password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className={inputClass}
                required
              />
            </label>
            <label className="flex flex-col gap-1.5 text-sm">
              <span className="font-medium text-gray-700 dark:text-gray-300">Confirm new password</span>
              <input
                type="password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className={inputClass}
                required
              />
            </label>
            {error ? (
              <p className="text-sm text-error-500 dark:text-error-400" role="alert">
                {error}
              </p>
            ) : null}
            <button
              type="submit"
              disabled={submitting}
              className="flex w-full items-center justify-center rounded-lg bg-brand-500 px-4 py-3 text-sm font-medium text-white shadow-theme-xs transition hover:bg-brand-600 disabled:opacity-60"
            >
              {submitting ? "Saving…" : "Save password"}
            </button>
          </form>
        </div>
      </div>
    </main>
  );
}
