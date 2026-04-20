import { z } from "zod";

export const departmentTransferSchema = z.object({
  newDepartmentId: z.coerce.number().int().positive("New department is required"),
  effectiveFromUtc: z.string().min(1, "Effective date is required"),
  reason: z.string().trim().max(500, "Reason is too long").optional(),
});

export const positionTransferSchema = z.object({
  newJobPositionId: z.coerce.number().int().positive("New position is required"),
  effectiveFromUtc: z.string().min(1, "Effective date is required"),
  reason: z.string().trim().max(500, "Reason is too long").optional(),
});

export type DepartmentTransferValues = z.infer<typeof departmentTransferSchema>;
export type PositionTransferValues = z.infer<typeof positionTransferSchema>;
