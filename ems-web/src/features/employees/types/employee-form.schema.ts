import { z } from "zod";
import { EmploymentStatus } from "./employment-status";

const optionalIdField = z
  .string()
  .transform((s) => {
    const t = s.trim();
    if (t === "") return null;
    const n = Number(t);
    return Number.isFinite(n) ? n : null;
  });

export const employeeFormSchema = z.object({
  organizationId: z.coerce.number().int().positive("Organization is required"),
  firstName: z.string().min(1, "First name is required"),
  lastName: z.string().min(1, "Last name is required"),
  email: z.string().email("Invalid email"),
  phoneNumber: z.string().optional(),
  dateOfBirth: z.string().min(1, "Date of birth is required"),
  dateJoined: z.string().min(1, "Date joined is required"),
  employmentStatus: z.coerce.number().refine(
    (v) =>
      v === EmploymentStatus.Active ||
      v === EmploymentStatus.Inactive ||
      v === EmploymentStatus.Terminated ||
      v === EmploymentStatus.Preboarding,
    { message: "Invalid status" },
  ),
  departmentId: optionalIdField,
  locationId: optionalIdField,
  managerId: optionalIdField,
  jobPositionId: optionalIdField,
});

/** Parsed / API-ready shape after Zod transforms. */
export type EmployeeFormValues = z.infer<typeof employeeFormSchema>;

/** Values held in react-hook-form before transforms (e.g. optional FK fields as strings). */
export type EmployeeFormInput = z.input<typeof employeeFormSchema>;
