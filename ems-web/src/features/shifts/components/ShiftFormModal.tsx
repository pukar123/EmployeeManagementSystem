import type { Employee } from "@/features/employees/types/employee.types";
import type { Site } from "@/features/sites/types/site.types";
import { Button } from "@/shared/components/Button";
import { Modal } from "@/shared/components/Modal";
import type { ShiftFormFields } from "../types/shift-form.schema";
import type { ShiftItem, ShiftStatus } from "../types/shift.types";
import { getEmployeeDisplayLabel, shiftStatusLabels } from "../utils/shiftDisplay";

const shiftStatusOptions: ShiftStatus[] = [0, 1, 2, 3];

type ShiftFormModalProps = {
  open: boolean;
  mode: "create" | "edit";
  employees: Employee[];
  sites: Site[];
  fields: ShiftFormFields;
  onFieldsChange: (fields: ShiftFormFields) => void;
  status: ShiftStatus;
  onStatusChange: (value: ShiftStatus) => void;
  editingShift: ShiftItem | null;
  onSubmit: () => void;
  onClose: () => void;
  isSubmitting: boolean;
  inputClassName: string;
};

export function ShiftFormModal({
  open,
  mode,
  employees,
  sites,
  fields,
  onFieldsChange,
  status,
  onStatusChange,
  editingShift,
  onSubmit,
  onClose,
  isSubmitting,
  inputClassName,
}: ShiftFormModalProps) {
  const title = mode === "create" ? "Schedule shift" : "Edit shift";

  return (
    <Modal open={open} title={title} onClose={onClose} className="max-w-lg">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          onSubmit();
        }}
        className="space-y-4"
      >
        {mode === "create" ? (
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Employee</label>
            <select
              value={fields.employeeId ?? ""}
              onChange={(e) =>
                onFieldsChange({
                  ...fields,
                  employeeId: e.target.value ? Number(e.target.value) : undefined,
                })
              }
              className={inputClassName}
              required
            >
              <option value="" disabled>
                Select employee
              </option>
              {employees.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {getEmployeeDisplayLabel(employee)}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Employee</label>
            <p className="mt-1 text-sm text-foreground">
              {editingShift
                ? employees
                    .filter((employee) => employee.id === editingShift.employeeId)
                    .map((employee) => getEmployeeDisplayLabel(employee))[0] ?? "Unknown employee"
                : "Unknown employee"}
            </p>
          </div>
        )}

        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Title</label>
          <input
            type="text"
            value={fields.title}
            onChange={(e) => onFieldsChange({ ...fields, title: e.target.value })}
            className={inputClassName}
            required
          />
        </div>

        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Description</label>
          <textarea
            value={fields.description ?? ""}
            onChange={(e) => onFieldsChange({ ...fields, description: e.target.value })}
            className={inputClassName}
            rows={3}
          />
        </div>

        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Site (optional)</label>
          <select
            value={fields.siteId ?? ""}
            onChange={(e) =>
              onFieldsChange({
                ...fields,
                siteId: e.target.value ? Number(e.target.value) : null,
              })
            }
            className={inputClassName}
          >
            <option value="">No site</option>
            {sites
              .filter((site) => site.isActive && !site.isDeleted)
              .map((site) => (
                <option key={site.siteId} value={site.siteId}>
                  {site.siteName}
                </option>
              ))}
          </select>
        </div>

        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Start</label>
          <input
            type="datetime-local"
            value={fields.startAtLocal}
            onChange={(e) => onFieldsChange({ ...fields, startAtLocal: e.target.value })}
            className={inputClassName}
            required
          />
        </div>

        <div>
          <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">End</label>
          <input
            type="datetime-local"
            value={fields.endAtLocal}
            onChange={(e) => onFieldsChange({ ...fields, endAtLocal: e.target.value })}
            className={inputClassName}
            required
          />
        </div>

        {mode === "edit" ? (
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Status</label>
            <select
              value={status}
              onChange={(e) => onStatusChange(Number(e.target.value) as ShiftStatus)}
              className={inputClassName}
            >
              {(shiftStatusOptions.map((value) => (
                <option key={value} value={value}>
                  {shiftStatusLabels[value]}
                </option>
              )))}
            </select>
          </div>
        ) : null}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Saving…" : mode === "create" ? "Schedule" : "Save changes"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
