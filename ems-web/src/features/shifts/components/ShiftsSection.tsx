"use client";

import { useCallback } from "react";
import { toast } from "sonner";
import { useCreateActionParam } from "@/features/command-palette/hooks/useCreateActionParam";
import { getErrorMessage } from "@/shared/api/http-client";
import { Spinner } from "@/shared/components/Spinner";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { useShiftActions } from "../hooks/useShiftActions";
import { useShiftCalendarEvents } from "../hooks/useShiftCalendarEvents";
import { useShiftForm } from "../hooks/useShiftForm";
import { useShiftsController } from "../hooks/useShiftsController";
import { ShiftCalendarView } from "./ShiftCalendarView";
import { ShiftFilters } from "./ShiftFilters";
import { ShiftFormModal } from "./ShiftFormModal";
import { ShiftTableView } from "./ShiftTableView";
import { ShiftsToolbar } from "./ShiftsToolbar";

type ShiftsSectionProps = {
  initialViewMode?: "table" | "calendar";
};

export function ShiftsSection({ initialViewMode = "table" }: ShiftsSectionProps) {
  const { organizationId } = useOrganizationContext();
  const inputClassName =
    "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card dark:text-foreground";

  const {
    search,
    setSearch,
    viewMode,
    setViewMode,
    filterEmployeeId,
    setFilterEmployeeId,
    filterStatus,
    setFilterStatus,
    filterStartDate,
    setFilterStartDate,
    filterEndDate,
    setFilterEndDate,
    setCalendarRange,
    employees,
    sites,
    employeeLabelById,
    siteLabelById,
    shifts,
    shiftsQuery,
    employeesQuery,
    sitesQuery,
  } = useShiftsController({
    initialViewMode,
    organizationId,
  });

  const shiftForm = useShiftForm({ organizationId });
  const shiftActions = useShiftActions({ onSaveSuccess: shiftForm.closeForm });
  const calendarEvents = useShiftCalendarEvents({ shifts, employeeLabelById });

  useCreateActionParam(shiftForm.openCreate);

  const handleSubmit = useCallback(async () => {
    if (organizationId == null) {
      toast.error("Organization is not loaded yet.");
      return;
    }

    if (shiftForm.editingShift) {
      const parsed = shiftForm.parseUpdateShiftForm();
      if (!parsed.success) {
        toast.error(parsed.message);
        return;
      }

      await shiftActions.updateShift(shiftForm.editingShift.id, parsed.payload);
      return;
    }

    const parsed = shiftForm.parseCreateShiftForm();
    if (!parsed.success) {
      toast.error(parsed.message);
      return;
    }

    await shiftActions.createShift(parsed.payload);
  }, [organizationId, shiftActions, shiftForm]);

  if (shiftsQuery.isLoading || employeesQuery.isLoading || sitesQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (employeesQuery.isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
        Could not load employees.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <ShiftsToolbar viewMode={viewMode} onViewModeChange={setViewMode} onScheduleShift={shiftForm.openCreate} />
      <ShiftFilters
        search={search}
        onSearchChange={setSearch}
        employees={employees}
        selectedEmployeeId={filterEmployeeId}
        onSelectedEmployeeChange={setFilterEmployeeId}
        selectedStatus={filterStatus}
        onSelectedStatusChange={setFilterStatus}
        startDate={filterStartDate}
        onStartDateChange={setFilterStartDate}
        endDate={filterEndDate}
        onEndDateChange={setFilterEndDate}
        showDateFilters={viewMode === "table"}
        inputClassName={inputClassName}
      />

      {shiftsQuery.isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
          {getErrorMessage(shiftsQuery.error)}
        </div>
      ) : viewMode === "calendar" ? (
        <ShiftCalendarView events={calendarEvents} employeeLabelById={employeeLabelById} onRangeChange={setCalendarRange} />
      ) : (
        <ShiftTableView
          shifts={shifts}
          employeeLabelById={employeeLabelById}
          siteLabelById={siteLabelById}
          onEdit={shiftForm.openEdit}
          onDelete={shiftActions.deleteShift}
        />
      )}

      <ShiftFormModal
        open={shiftForm.formOpen}
        mode={shiftForm.editingShift ? "edit" : "create"}
        employees={employees}
        sites={sites}
        fields={shiftForm.fields}
        onFieldsChange={shiftForm.setFields}
        status={shiftForm.status}
        onStatusChange={shiftForm.setStatus}
        editingShift={shiftForm.editingShift}
        onSubmit={handleSubmit}
        onClose={shiftForm.closeForm}
        isSubmitting={shiftActions.isSaving}
        inputClassName={inputClassName}
      />
    </div>
  );
}
