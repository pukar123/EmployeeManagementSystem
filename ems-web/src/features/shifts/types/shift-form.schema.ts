import { z } from "zod";

export const shiftFormFieldsSchema = z
  .object({
    employeeId: z.coerce.number().int().positive("Employee is required").optional(),
    title: z.string().trim().min(1, "Title is required"),
    description: z.string().optional(),
    startAtLocal: z.string().min(1, "Start time is required"),
    endAtLocal: z.string().min(1, "End time is required"),
    siteId: z.coerce.number().int().positive().optional().nullable(),
    status: z.coerce.number().int().min(0).max(3).optional(),
  })
  .superRefine((values, ctx) => {
    if (!values.startAtLocal || !values.endAtLocal) {
      return;
    }

    const start = new Date(values.startAtLocal);
    const end = new Date(values.endAtLocal);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
      ctx.addIssue({
        code: "custom",
        message: "Enter valid start and end times.",
        path: ["startAtLocal"],
      });
      return;
    }

    if (start > end) {
      ctx.addIssue({
        code: "custom",
        message: "Start time must be before or equal to end time.",
        path: ["endAtLocal"],
      });
    }
  });

export type ShiftFormFields = z.infer<typeof shiftFormFieldsSchema>;

export function parseCreateShiftForm(
  values: ShiftFormFields,
  organizationId: number,
): { success: true; payload: import("./shift.types").CreateShiftRequest } | { success: false; message: string } {
  if (values.employeeId == null) {
    return { success: false, message: "Employee is required." };
  }

  const parsed = shiftFormFieldsSchema.safeParse(values);
  if (!parsed.success) {
    return { success: false, message: parsed.error.issues[0]?.message ?? "Invalid shift details." };
  }

  return {
    success: true,
    payload: {
      organizationId,
      employeeId: values.employeeId,
      siteId: values.siteId ?? null,
      title: parsed.data.title.trim(),
      description: parsed.data.description?.trim() ? parsed.data.description.trim() : null,
      startAtUtc: new Date(parsed.data.startAtLocal).toISOString(),
      endAtUtc: new Date(parsed.data.endAtLocal).toISOString(),
    },
  };
}

export function parseUpdateShiftForm(
  values: ShiftFormFields,
  status: import("./shift.types").ShiftStatus,
): { success: true; payload: import("./shift.types").UpdateShiftRequest } | { success: false; message: string } {
  const parsed = shiftFormFieldsSchema.safeParse({ ...values, status });
  if (!parsed.success) {
    return { success: false, message: parsed.error.issues[0]?.message ?? "Invalid shift details." };
  }

  return {
    success: true,
    payload: {
      siteId: values.siteId ?? null,
      title: parsed.data.title.trim(),
      description: parsed.data.description?.trim() ? parsed.data.description.trim() : null,
      startAtUtc: new Date(parsed.data.startAtLocal).toISOString(),
      endAtUtc: new Date(parsed.data.endAtLocal).toISOString(),
      status,
    },
  };
}
