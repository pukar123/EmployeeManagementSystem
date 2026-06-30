"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useDepartments } from "@/features/departments/hooks";
import { useJobPositions } from "@/features/job-positions/hooks";
import { useLocations } from "@/features/locations/hooks";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { Button } from "@/shared/components/Button";
import { getErrorMessage } from "@/shared/api/http-client";
import { useCreateEmployee, useEmployees } from "../hooks";
import { employeeService } from "../services/employeeService";
import type { CreateEmployeeRequest, Employee, PossibleDuplicateEmployee } from "../types/employee.types";
import { EmploymentStatus } from "../types/employment-status";
import { dateInputToApiIso } from "../utils/date-format";
import { useUnsavedChangesWarning, confirmDiscardChanges } from "@/shared/hooks/useUnsavedChangesWarning";

type WizardStep = "personal" | "employment" | "organization" | "review";

type FormState = {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  dateJoined: string;
  employmentStatus: number;
  departmentId: string;
  locationId: string;
  managerId: string;
  jobPositionId: string;
};

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20";

type CreateEmployeeWizardProps = {
  onClose: () => void;
  onCreated: () => void;
};

export function CreateEmployeeWizard({ onClose, onCreated }: CreateEmployeeWizardProps) {
  const { organizationId } = useOrganizationContext();
  const createMutation = useCreateEmployee();
  const { data: departments = [] } = useDepartments();
  const { data: locations = [] } = useLocations();
  const { data: jobPositions = [] } = useJobPositions(organizationId);
  const { data: employees = [] } = useEmployees();

  const [step, setStep] = useState<WizardStep>("personal");
  const [dirty, setDirty] = useState(false);
  const [duplicates, setDuplicates] = useState<PossibleDuplicateEmployee[]>([]);
  const [createdEmployee, setCreatedEmployee] = useState<Employee | null>(null);
  const [form, setForm] = useState<FormState>({
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    dateOfBirth: "",
    dateJoined: new Date().toISOString().slice(0, 10),
    employmentStatus: EmploymentStatus.Preboarding,
    departmentId: "",
    locationId: "",
    managerId: "",
    jobPositionId: "",
  });

  useUnsavedChangesWarning(dirty && !createdEmployee);

  const managers = useMemo(
    () => employees.filter((e) => e.isActive && e.employmentStatus === EmploymentStatus.Active && !e.isArchived),
    [employees],
  );

  const patch = (values: Partial<FormState>) => {
    setDirty(true);
    setForm((prev) => ({ ...prev, ...values }));
  };

  const handleClose = () => {
    if (!confirmDiscardChanges(dirty && !createdEmployee)) return;
    onClose();
  };

  const buildPayload = (): CreateEmployeeRequest => ({
    organizationId: organizationId!,
    firstName: form.firstName.trim(),
    lastName: form.lastName.trim(),
    email: form.email.trim(),
    phoneNumber: form.phoneNumber.trim() || null,
    dateOfBirth: dateInputToApiIso(form.dateOfBirth),
    dateJoined: dateInputToApiIso(form.dateJoined),
    employmentStatus: form.employmentStatus as CreateEmployeeRequest["employmentStatus"],
    departmentId: form.departmentId ? Number(form.departmentId) : null,
    locationId: form.locationId ? Number(form.locationId) : null,
    managerId: form.managerId ? Number(form.managerId) : null,
    jobPositionId: form.jobPositionId ? Number(form.jobPositionId) : null,
  });

  const validateStep = (): string | null => {
    if (step === "personal") {
      if (!form.firstName.trim()) return "First name is required.";
      if (!form.lastName.trim()) return "Last name is required.";
      if (!form.email.trim()) return "Email is required.";
      if (!form.dateOfBirth) return "Date of birth is required.";
    }
    if (step === "employment") {
      if (!form.dateJoined) return "Date joined is required.";
    }
    return null;
  };

  const goNext = async () => {
    const error = validateStep();
    if (error) {
      toast.error(error);
      return;
    }

    if (step === "organization") {
      try {
        const matches = await employeeService.getPossibleDuplicates({
          organizationId: organizationId!,
          email: form.email,
          firstName: form.firstName,
          lastName: form.lastName,
          phoneNumber: form.phoneNumber || undefined,
          dateOfBirth: form.dateOfBirth ? dateInputToApiIso(form.dateOfBirth) : undefined,
        });
        setDuplicates(matches);
      } catch (err) {
        toast.error(getErrorMessage(err));
        return;
      }
      setStep("review");
      return;
    }

    setStep(step === "personal" ? "employment" : step === "employment" ? "organization" : "review");
  };

  const handleCreate = async () => {
    try {
      const created = await createMutation.mutateAsync(buildPayload());
      setCreatedEmployee(created);
      setDirty(false);
      onCreated();
      toast.success(`Employee ${created.employeeNumber} created.`);
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  if (createdEmployee) {
    return (
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          <strong>
            {createdEmployee.firstName} {createdEmployee.lastName}
          </strong>{" "}
          ({createdEmployee.employeeNumber}) was added successfully.
        </p>
        <div className="flex flex-wrap gap-2">
          <Link
            href={`/employees/${createdEmployee.id}`}
            className="inline-flex items-center rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
          >
            View profile
          </Link>
          <Button
            type="button"
            variant="secondary"
            onClick={async () => {
              try {
                await employeeService.provisionEmployeeUser(createdEmployee.id);
                toast.success("Sign-in invitation sent.");
              } catch (err) {
                toast.error(getErrorMessage(err));
              }
            }}
          >
            Send login invitation
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => {
              setCreatedEmployee(null);
              setStep("personal");
              setForm({
                firstName: "",
                lastName: "",
                email: "",
                phoneNumber: "",
                dateOfBirth: "",
                dateJoined: new Date().toISOString().slice(0, 10),
                employmentStatus: EmploymentStatus.Preboarding,
                departmentId: "",
                locationId: "",
                managerId: "",
                jobPositionId: "",
              });
              setDuplicates([]);
              setDirty(false);
            }}
          >
            Add another employee
          </Button>
          <Button type="button" variant="ghost" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <WizardSteps current={step} />
      {step === "personal" ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="First name (required)" value={form.firstName} onChange={(v) => patch({ firstName: v })} />
          <Field label="Last name (required)" value={form.lastName} onChange={(v) => patch({ lastName: v })} />
          <Field label="Email (required)" value={form.email} onChange={(v) => patch({ email: v })} type="email" />
          <Field label="Phone (optional)" value={form.phoneNumber} onChange={(v) => patch({ phoneNumber: v })} />
          <Field label="Date of birth (required)" value={form.dateOfBirth} onChange={(v) => patch({ dateOfBirth: v })} type="date" />
        </div>
      ) : null}
      {step === "employment" ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Date joined (required)" value={form.dateJoined} onChange={(v) => patch({ dateJoined: v })} type="date" />
          <div>
            <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Employment status</label>
            <select
              className={inputClass}
              value={form.employmentStatus}
              onChange={(e) => patch({ employmentStatus: Number(e.target.value) })}
            >
              <option value={EmploymentStatus.Preboarding}>Preboarding</option>
              <option value={EmploymentStatus.Active}>Active</option>
              <option value={EmploymentStatus.Inactive}>Inactive</option>
            </select>
          </div>
        </div>
      ) : null}
      {step === "organization" ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <SelectField
            label="Department (optional)"
            value={form.departmentId}
            onChange={(v) => patch({ departmentId: v })}
            options={departments.filter((d) => d.organizationId === organizationId).map((d) => ({ value: String(d.id), label: d.name }))}
          />
          <SelectField
            label="Position (optional)"
            value={form.jobPositionId}
            onChange={(v) => patch({ jobPositionId: v })}
            options={jobPositions.map((p) => ({ value: String(p.id), label: p.title }))}
          />
          <SelectField
            label="Manager (optional)"
            value={form.managerId}
            onChange={(v) => patch({ managerId: v })}
            options={managers.map((m) => ({
              value: String(m.id),
              label: `${m.firstName} ${m.lastName} (${m.employeeNumber})`,
            }))}
          />
          <SelectField
            label="Address location (optional)"
            value={form.locationId}
            onChange={(v) => patch({ locationId: v })}
            options={locations
              .filter((l) => l.organizationId === organizationId)
              .map((l) => ({ value: String(l.id), label: l.city ? `${l.name} — ${l.city}` : l.name }))}
          />
        </div>
      ) : null}
      {step === "review" ? (
        <div className="space-y-4 text-sm">
          <ReviewRow label="Name" value={`${form.firstName} ${form.lastName}`} />
          <ReviewRow label="Email" value={form.email} />
          <ReviewRow label="Status" value="Preboarding or selected status" />
          <ReviewRow label="Date joined" value={form.dateJoined} />
          {duplicates.length > 0 ? (
            <div className="rounded-lg border border-amber-500/40 bg-amber-500/10 p-3">
              <p className="font-medium text-amber-900 dark:text-amber-200">Possible duplicates found</p>
              <ul className="mt-2 space-y-1">
                {duplicates.map((d) => (
                  <li key={d.id}>
                    <Link href={`/employees/${d.id}`} className="text-primary hover:underline">
                      {d.employeeNumber}
                    </Link>
                    : {d.matchReason}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>
      ) : null}

      <div className="flex justify-between gap-2 border-t border-border pt-4">
        <Button type="button" variant="ghost" onClick={handleClose}>
          Cancel
        </Button>
        <div className="flex gap-2">
          {step !== "personal" ? (
            <Button
              type="button"
              variant="secondary"
              onClick={() =>
                setStep(step === "review" ? "organization" : step === "organization" ? "employment" : "personal")
              }
            >
              Back
            </Button>
          ) : null}
          {step === "review" ? (
            <Button type="button" disabled={createMutation.isPending} onClick={() => void handleCreate()}>
              {createMutation.isPending ? "Creating…" : "Confirm and create"}
            </Button>
          ) : (
            <Button type="button" onClick={() => void goNext()}>
              Continue
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

function WizardSteps({ current }: { current: WizardStep }) {
  const steps: { id: WizardStep; label: string }[] = [
    { id: "personal", label: "Personal" },
    { id: "employment", label: "Employment" },
    { id: "organization", label: "Organization" },
    { id: "review", label: "Review" },
  ];
  return (
    <ol className="flex flex-wrap gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
      {steps.map((s) => (
        <li
          key={s.id}
          className={current === s.id ? "rounded-full bg-primary/10 px-2 py-1 text-primary" : "px-2 py-1"}
        >
          {s.label}
        </li>
      ))}
    </ol>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <div>
      <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</label>
      <input className={inputClass} type={type} value={value} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

function SelectField({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
}) {
  return (
    <div>
      <label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</label>
      <select className={inputClass} value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">—</option>
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </div>
  );
}

function ReviewRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-border/60 pb-2">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium text-foreground">{value}</span>
    </div>
  );
}
