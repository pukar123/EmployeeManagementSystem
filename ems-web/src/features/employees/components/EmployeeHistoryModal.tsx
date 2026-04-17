"use client";

import { Modal } from "@/shared/components/Modal";
import { Spinner } from "@/shared/components/Spinner";
import type {
  DepartmentHistoryItem,
  Employee,
  EmployeeHistoryResponse,
  ManagerHistoryItem,
  PositionHistoryItem,
} from "../types/employee.types";

type EmployeeHistoryModalProps = {
  open: boolean;
  employee: Employee | null;
  history: EmployeeHistoryResponse | undefined;
  isLoading: boolean;
  isError: boolean;
  errorMessage: string | null;
  onClose: () => void;
};

function sortByEffectiveThenCreated<T extends { effectiveFromUtc: string; createdAtUtc: string }>(items: T[]): T[] {
  return [...items].sort((a, b) => {
    const effectiveDiff = new Date(b.effectiveFromUtc).getTime() - new Date(a.effectiveFromUtc).getTime();
    if (effectiveDiff !== 0) return effectiveDiff;
    return new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
  });
}

function actorLabel(item: { changedByUserName: string | null; changedByEmail: string | null }): string {
  if (item.changedByUserName) return item.changedByUserName;
  if (item.changedByEmail) return item.changedByEmail;
  return "System";
}

function PositionRow({ item }: { item: PositionHistoryItem }) {
  return (
    <li className="rounded-lg border border-zinc-200 p-3 dark:border-zinc-700">
      <p className="text-sm text-zinc-900 dark:text-zinc-100">
        Position: <span className="font-medium">{item.previousJobPositionId ?? "—"}</span>
        {" -> "}
        <span className="font-medium">{item.newJobPositionId ?? "—"}</span>
      </p>
      <p className="mt-1 text-xs text-zinc-500">Effective: {new Date(item.effectiveFromUtc).toLocaleString()}</p>
      <p className="mt-1 text-xs text-zinc-500">Reason: {item.reason || "—"}</p>
      <p className="mt-1 text-xs text-zinc-500">By: {actorLabel(item)}</p>
    </li>
  );
}

function DepartmentRow({ item }: { item: DepartmentHistoryItem }) {
  return (
    <li className="rounded-lg border border-zinc-200 p-3 dark:border-zinc-700">
      <p className="text-sm text-zinc-900 dark:text-zinc-100">
        Department: <span className="font-medium">{item.previousDepartmentId ?? "—"}</span>
        {" -> "}
        <span className="font-medium">{item.newDepartmentId ?? "—"}</span>
      </p>
      <p className="mt-1 text-xs text-zinc-500">Effective: {new Date(item.effectiveFromUtc).toLocaleString()}</p>
      <p className="mt-1 text-xs text-zinc-500">Reason: {item.reason || "—"}</p>
      <p className="mt-1 text-xs text-zinc-500">By: {actorLabel(item)}</p>
    </li>
  );
}

function ManagerRow({ item }: { item: ManagerHistoryItem }) {
  return (
    <li className="rounded-lg border border-zinc-200 p-3 dark:border-zinc-700">
      <p className="text-sm text-zinc-900 dark:text-zinc-100">
        Manager: <span className="font-medium">{item.previousManagerId ?? "—"}</span>
        {" -> "}
        <span className="font-medium">{item.newManagerId ?? "—"}</span>
      </p>
      <p className="mt-1 text-xs text-zinc-500">Effective: {new Date(item.effectiveFromUtc).toLocaleString()}</p>
      <p className="mt-1 text-xs text-zinc-500">Reason: {item.reason || "—"}</p>
      <p className="mt-1 text-xs text-zinc-500">By: {actorLabel(item)}</p>
    </li>
  );
}

function HistorySection({
  title,
  emptyLabel,
  children,
}: {
  title: string;
  emptyLabel: string;
  children: React.ReactNode[];
}) {
  return (
    <section className="space-y-2">
      <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{title}</h3>
      {children.length > 0 ? <ul className="space-y-2">{children}</ul> : <p className="text-xs text-zinc-500">{emptyLabel}</p>}
    </section>
  );
}

export function EmployeeHistoryModal({
  open,
  employee,
  history,
  isLoading,
  isError,
  errorMessage,
  onClose,
}: EmployeeHistoryModalProps) {
  return (
    <Modal open={open} onClose={onClose} title={`History${employee ? ` — ${employee.firstName} ${employee.lastName}` : ""}`} className="max-w-3xl">
      {isLoading ? (
        <div className="flex justify-center py-12">
          <Spinner />
        </div>
      ) : isError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
          {errorMessage || "Could not load history."}
        </div>
      ) : !history ? (
        <p className="text-sm text-zinc-500">No history recorded yet.</p>
      ) : (
        <div className="space-y-6">
          <HistorySection title="Position History" emptyLabel="No history recorded yet.">
            {sortByEffectiveThenCreated(history.positionHistory).map((item) => (
              <PositionRow key={item.id} item={item} />
            ))}
          </HistorySection>

          <HistorySection title="Department History" emptyLabel="No history recorded yet.">
            {sortByEffectiveThenCreated(history.departmentHistory).map((item) => (
              <DepartmentRow key={item.id} item={item} />
            ))}
          </HistorySection>

          <HistorySection title="Manager History" emptyLabel="No history recorded yet.">
            {sortByEffectiveThenCreated(history.managerHistory).map((item) => (
              <ManagerRow key={item.id} item={item} />
            ))}
          </HistorySection>
        </div>
      )}
    </Modal>
  );
}
