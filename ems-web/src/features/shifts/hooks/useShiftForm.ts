import { useCallback, useState } from "react";
import type { ShiftFormFields } from "../types/shift-form.schema";
import { parseCreateShiftForm, parseUpdateShiftForm } from "../types/shift-form.schema";
import type { CreateShiftRequest, ShiftItem, ShiftStatus, UpdateShiftRequest } from "../types/shift.types";
import { toDatetimeLocalValue } from "../utils/shiftDisplay";

type UseShiftFormOptions = {
  organizationId: number | null;
};

const emptyFields = (): ShiftFormFields => ({
  employeeId: undefined,
  title: "",
  description: "",
  startAtLocal: "",
  endAtLocal: "",
  siteId: null,
  status: 0,
});

export function useShiftForm({ organizationId }: UseShiftFormOptions) {
  const [formOpen, setFormOpen] = useState(false);
  const [editingShift, setEditingShift] = useState<ShiftItem | null>(null);
  const [fields, setFields] = useState<ShiftFormFields>(emptyFields);
  const [status, setStatus] = useState<ShiftStatus>(0);

  const resetForm = useCallback(() => {
    setFields(emptyFields());
    setStatus(0);
    setEditingShift(null);
  }, []);

  const openCreate = useCallback(() => {
    resetForm();
    setFormOpen(true);
  }, [resetForm]);

  const openEdit = useCallback((shift: ShiftItem) => {
    setEditingShift(shift);
    setStatus(shift.status);
    setFields({
      employeeId: shift.employeeId,
      title: shift.title,
      description: shift.description ?? "",
      startAtLocal: toDatetimeLocalValue(shift.startAtUtc),
      endAtLocal: toDatetimeLocalValue(shift.endAtUtc),
      siteId: shift.siteId,
      status: shift.status,
    });
    setFormOpen(true);
  }, []);

  const closeForm = useCallback(() => {
    setFormOpen(false);
    resetForm();
  }, [resetForm]);

  const buildCreatePayload = useCallback((): CreateShiftRequest | null => {
    if (organizationId == null) {
      return null;
    }

    const result = parseCreateShiftForm(fields, organizationId);
    if (!result.success) {
      return null;
    }

    return result.payload;
  }, [fields, organizationId]);

  const buildUpdatePayload = useCallback((): UpdateShiftRequest | null => {
    const result = parseUpdateShiftForm(fields, status);
    if (!result.success) {
      return null;
    }

    return result.payload;
  }, [fields, status]);

  return {
    formOpen,
    editingShift,
    fields,
    setFields,
    status,
    setStatus,
    openCreate,
    openEdit,
    closeForm,
    buildCreatePayload,
    buildUpdatePayload,
    parseCreateShiftForm: () => parseCreateShiftForm(fields, organizationId ?? 0),
    parseUpdateShiftForm: () => parseUpdateShiftForm(fields, status),
  };
}
