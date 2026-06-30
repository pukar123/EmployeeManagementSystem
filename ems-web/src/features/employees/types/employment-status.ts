/** Mirrors EMS.Domain.Enums.EmploymentStatus (JSON is numeric). */
export const EmploymentStatus = {
  Active: 0,
  Inactive: 1,
  Terminated: 2,
  Preboarding: 3,
} as const;

export type EmploymentStatusValue = (typeof EmploymentStatus)[keyof typeof EmploymentStatus];

export const employmentStatusLabels: Record<EmploymentStatusValue, string> = {
  0: "Active",
  1: "Inactive",
  2: "Terminated",
  3: "Preboarding",
};
