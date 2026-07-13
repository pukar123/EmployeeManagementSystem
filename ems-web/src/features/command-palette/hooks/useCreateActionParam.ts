"use client";

import { useEffect, useRef } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

export function useCreateActionParam(onCreate: () => void, enabled = true) {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const onCreateRef = useRef(onCreate);

  useEffect(() => {
    onCreateRef.current = onCreate;
  }, [onCreate]);

  useEffect(() => {
    if (!enabled || searchParams.get("action") !== "create") {
      return;
    }

    onCreateRef.current();

    const params = new URLSearchParams(searchParams.toString());
    params.delete("action");
    const nextQuery = params.toString();
    router.replace(nextQuery ? `${pathname}?${nextQuery}` : pathname);
  }, [enabled, pathname, router, searchParams]);
}
