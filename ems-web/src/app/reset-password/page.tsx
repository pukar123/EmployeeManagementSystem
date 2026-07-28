import { Suspense } from "react";
import { ResetPasswordPage } from "@/features/auth/components/ResetPasswordPage";

export default function Page() {
  return (
    <Suspense fallback={<main className="mx-auto max-w-md px-4 py-10">Loading…</main>}>
      <ResetPasswordPage />
    </Suspense>
  );
}
