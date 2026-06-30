import type { EmploymentStatusValue } from "../types/employment-status";
import { employmentStatusLabels } from "../types/employment-status";
import { StatusPill } from "@/shared/components/Badge";

const statusVariant: Record<EmploymentStatusValue, "success" | "warning" | "danger" | "muted"> = {
  0: "success",
  1: "warning",
  2: "danger",
  3: "muted",
};

export function EmploymentStatusPill({ status }: { status: EmploymentStatusValue }) {
  return (
    <StatusPill
      label={employmentStatusLabels[status] ?? String(status)}
      variant={statusVariant[status] ?? "muted"}
    />
  );
}
