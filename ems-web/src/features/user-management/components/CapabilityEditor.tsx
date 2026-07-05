"use client";

import { useCallback, useState } from "react";
import { Button } from "@/shared/components/Button";
import {
  EMPLOYEE_CAPABILITY_KEYS,
  EMPLOYEE_CAPABILITY_LABELS,
  expandCapabilityKeys,
  type EmployeeCapabilityKey,
} from "@/features/employees/services/employeeAccessApi";

type CapabilityEditorProps = {
  roleKey: string;
  initialKeys: string[];
  isSaving: boolean;
  onSave: (keys: string[]) => void;
};

function CapabilityEditor({ roleKey, initialKeys, isSaving, onSave }: CapabilityEditorProps) {
  const [selected, setSelected] = useState(() => new Set(initialKeys));

  const toggle = useCallback((key: EmployeeCapabilityKey) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
        if (key === "employees.view") {
          next.delete("employees.manage");
          next.delete("employees.access");
          next.delete("employees.export");
        }
      } else {
        next.add(key);
        if (key === "employees.manage" || key === "employees.access" || key === "employees.export") {
          next.add("employees.view");
        }
      }
      return next;
    });
  }, []);

  return (
    <div className="space-y-4 rounded-xl border border-border bg-card p-5">
      <p className="text-sm font-medium text-foreground">Role: {roleKey}</p>
      <ul className="space-y-3">
        {EMPLOYEE_CAPABILITY_KEYS.map((key) => (
          <li key={key}>
            <label className="flex cursor-pointer items-start gap-3">
              <input type="checkbox" checked={selected.has(key)} onChange={() => toggle(key)} className="mt-1" />
              <span>
                <span className="block text-sm font-medium text-foreground">{EMPLOYEE_CAPABILITY_LABELS[key]}</span>
                <span className="block text-xs text-muted-foreground">{key}</span>
              </span>
            </label>
          </li>
        ))}
      </ul>
      <p className="text-xs text-muted-foreground">Manage, account access, and export automatically include view.</p>
      <Button type="button" disabled={isSaving} onClick={() => onSave(expandCapabilityKeys(selected))}>
        Save capabilities
      </Button>
    </div>
  );
}

export { CapabilityEditor };
