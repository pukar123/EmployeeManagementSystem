import { useCallback } from "react";
import { toast } from "sonner";
import { getErrorMessage } from "@/shared/api/http-client";
import type { CreateShiftRequest, ShiftItem, UpdateShiftRequest } from "../types/shift.types";
import { useCreateShift } from "./useCreateShift";
import { useDeleteShift } from "./useDeleteShift";
import { useUpdateShift } from "./useUpdateShift";

type UseShiftActionsOptions = {
  onSaveSuccess?: () => void;
};

export function useShiftActions({ onSaveSuccess }: UseShiftActionsOptions = {}) {
  const createMut = useCreateShift();
  const updateMut = useUpdateShift();
  const deleteMut = useDeleteShift();

  const createShift = useCallback(
    async (payload: CreateShiftRequest) => {
      try {
        await createMut.mutateAsync(payload);
        toast.success("Shift scheduled.");
        onSaveSuccess?.();
      } catch (err) {
        toast.error(getErrorMessage(err));
      }
    },
    [createMut, onSaveSuccess],
  );

  const updateShift = useCallback(
    async (id: number, payload: UpdateShiftRequest) => {
      try {
        await updateMut.mutateAsync({ id, body: payload });
        toast.success("Shift updated.");
        onSaveSuccess?.();
      } catch (err) {
        toast.error(getErrorMessage(err));
      }
    },
    [onSaveSuccess, updateMut],
  );

  const deleteShift = useCallback(
    async (shift: ShiftItem) => {
      if (!window.confirm(`Delete shift "${shift.title}"?`)) {
        return;
      }

      try {
        await deleteMut.mutateAsync(shift.id);
        toast.success("Shift deleted.");
      } catch (err) {
        toast.error(getErrorMessage(err));
      }
    },
    [deleteMut],
  );

  return {
    createShift,
    updateShift,
    deleteShift,
    isSaving: createMut.isPending || updateMut.isPending,
  };
}
