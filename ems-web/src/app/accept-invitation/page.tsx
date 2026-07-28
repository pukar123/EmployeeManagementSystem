import { Suspense } from "react";
import { AcceptInvitationPage } from "@/features/auth/components/AcceptInvitationPage";

export default function Page() {
  return (
    <Suspense fallback={<main className="mx-auto max-w-md px-4 py-10">Loading…</main>}>
      <AcceptInvitationPage />
    </Suspense>
  );
}
