"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useOrganizationContext } from "@/providers/OrganizationProvider";
import { getErrorMessage } from "@/shared/api/http-client";
import { Button } from "@/shared/components/Button";
import { Spinner } from "@/shared/components/Spinner";
import { taskPriorityLabels } from "@/features/tasks/utils/taskDisplay";
import { useOnboardingTemplateMutations, useOnboardingTemplates } from "../hooks";
import {
  OnboardingChecklistItemCategory,
  type OnboardingChecklistItemCategoryValue,
  type OnboardingChecklistTemplate,
  type OnboardingChecklistTemplateItemInput,
} from "../types/onboarding.types";
import { formatOnboardingCategory } from "@/features/employees/utils/onboardingProgress";

type ItemFormState = OnboardingChecklistTemplateItemInput;

type TemplateFormState = {
  name: string;
  description: string;
  isActive: boolean;
  isDefault: boolean;
  items: ItemFormState[];
};

const inputClass =
  "mt-1 w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm focus:border-primary/50 focus:outline-none focus:ring-2 focus:ring-primary/20 dark:bg-card";

const defaultItem = (sortOrder: number): ItemFormState => ({
  title: "",
  description: "",
  category: OnboardingChecklistItemCategory.Documents,
  sortOrder,
  defaultDueDaysFromStart: null,
  defaultPriority: null,
  isRequired: true,
});

const defaultForm: TemplateFormState = {
  name: "",
  description: "",
  isActive: true,
  isDefault: false,
  items: [defaultItem(1)],
};

function toFormState(template: OnboardingChecklistTemplate): TemplateFormState {
  return {
    name: template.name,
    description: template.description ?? "",
    isActive: template.isActive,
    isDefault: template.isDefault,
    items: template.items.map((item) => ({
      id: item.id,
      title: item.title,
      description: item.description ?? "",
      category: item.category,
      sortOrder: item.sortOrder,
      defaultDueDaysFromStart: item.defaultDueDaysFromStart,
      defaultPriority: item.defaultPriority,
      isRequired: item.isRequired,
    })),
  };
}

export function OnboardingTemplatesSection() {
  const { organizationId } = useOrganizationContext();
  const templatesQuery = useOnboardingTemplates(organizationId);
  const { createTemplate, updateTemplate, deleteTemplate } = useOnboardingTemplateMutations(organizationId);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<TemplateFormState>(defaultForm);

  const selectedTemplate = useMemo(() => {
    if (editingId == null) return null;
    return (templatesQuery.data ?? []).find((row) => row.id === editingId) ?? null;
  }, [editingId, templatesQuery.data]);

  const startCreate = () => {
    setEditingId(null);
    setForm(defaultForm);
  };

  const startEdit = (template: OnboardingChecklistTemplate) => {
    setEditingId(template.id);
    setForm(toFormState(template));
  };

  const updateItem = (index: number, patch: Partial<ItemFormState>) => {
    setForm((prev) => ({
      ...prev,
      items: prev.items.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    }));
  };

  const addItem = () => {
    setForm((prev) => ({
      ...prev,
      items: [...prev.items, defaultItem(prev.items.length + 1)],
    }));
  };

  const removeItem = (index: number) => {
    setForm((prev) => {
      const next = prev.items.filter((_, i) => i !== index);
      return {
        ...prev,
        items: next.map((item, i) => ({ ...item, sortOrder: i + 1 })),
      };
    });
  };

  const submit = async () => {
    if (!organizationId) return;
    if (!form.name.trim()) {
      toast.error("Template name is required.");
      return;
    }
    if (form.items.length === 0) {
      toast.error("At least one checklist item is required.");
      return;
    }
    if (form.items.some((item) => !item.title.trim())) {
      toast.error("Every checklist item needs a title.");
      return;
    }

    const items = form.items.map((item, index) => ({
      id: item.id,
      title: item.title.trim(),
      description: item.description?.trim() || undefined,
      category: item.category,
      sortOrder: index + 1,
      defaultDueDaysFromStart: item.defaultDueDaysFromStart ?? undefined,
      defaultPriority: item.defaultPriority ?? undefined,
      isRequired: item.isRequired ?? true,
    }));

    const payload = {
      organizationId,
      name: form.name.trim(),
      description: form.description.trim() || undefined,
      isActive: form.isActive,
      isDefault: form.isDefault,
      items,
    };

    try {
      if (editingId == null) {
        await createTemplate.mutateAsync(payload);
        toast.success("Onboarding template created.");
      } else {
        await updateTemplate.mutateAsync({
          id: editingId,
          payload: {
            name: payload.name,
            description: payload.description,
            isActive: payload.isActive,
            isDefault: payload.isDefault,
            items,
          },
        });
        toast.success("Onboarding template updated.");
      }
      startCreate();
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const remove = async () => {
    if (editingId == null) return;
    try {
      await deleteTemplate.mutateAsync(editingId);
      toast.success("Onboarding template deleted.");
      startCreate();
    } catch (error) {
      toast.error(getErrorMessage(error));
    }
  };

  const isSaving = createTemplate.isPending || updateTemplate.isPending || deleteTemplate.isPending;

  if (templatesQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <p className="text-sm text-muted-foreground">
          <Link href="/organization/setup" className="text-primary hover:underline">
            Organization settings
          </Link>
        </p>
        <h1 className="mt-2 text-2xl font-semibold text-foreground">Onboarding checklists</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Define organization templates for preboarding employees. Tasks are generated when a preboarding employee is
          created with a selected template.
        </p>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-medium">Configured templates</h2>
          <Button type="button" variant="secondary" onClick={startCreate}>
            New template
          </Button>
        </div>
        <div className="mt-3 overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="text-left text-muted-foreground">
                <th className="pb-2 pr-3">Name</th>
                <th className="pb-2 pr-3">Items</th>
                <th className="pb-2 pr-3">Default</th>
                <th className="pb-2 pr-3">Status</th>
                <th className="pb-2 pr-3">Action</th>
              </tr>
            </thead>
            <tbody>
              {(templatesQuery.data ?? []).map((row) => (
                <tr key={row.id} className="border-t border-border">
                  <td className="py-2 pr-3">{row.name}</td>
                  <td className="py-2 pr-3">{row.items.length}</td>
                  <td className="py-2 pr-3">{row.isDefault ? "Yes" : "—"}</td>
                  <td className="py-2 pr-3">{row.isActive ? "Active" : "Inactive"}</td>
                  <td className="py-2 pr-3">
                    <Button type="button" variant="secondary" onClick={() => startEdit(row)}>
                      Edit
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-4 dark:bg-card">
        <h2 className="text-lg font-medium">
          {editingId == null ? "Add template" : `Edit: ${selectedTemplate?.name ?? "Template"}`}
        </h2>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <label className="block md:col-span-2">
            <span className="text-xs font-medium text-muted-foreground">Name</span>
            <input
              value={form.name}
              onChange={(e) => setForm((prev) => ({ ...prev, name: e.target.value }))}
              className={inputClass}
            />
          </label>
          <label className="block md:col-span-2">
            <span className="text-xs font-medium text-muted-foreground">Description</span>
            <input
              value={form.description}
              onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
              className={inputClass}
            />
          </label>
          <label className="inline-flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
            />
            Active
          </label>
          <label className="inline-flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={form.isDefault}
              onChange={(e) => setForm((prev) => ({ ...prev, isDefault: e.target.checked }))}
            />
            Default template
          </label>
        </div>

        <div className="mt-6 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold text-foreground">Checklist items</h3>
            <Button type="button" variant="secondary" onClick={addItem}>
              Add item
            </Button>
          </div>
          {form.items.map((item, index) => (
            <div key={`${item.id ?? "new"}-${index}`} className="rounded-lg border border-border p-4">
              <div className="grid gap-3 md:grid-cols-2">
                <label className="block md:col-span-2">
                  <span className="text-xs font-medium text-muted-foreground">Title</span>
                  <input
                    value={item.title}
                    onChange={(e) => updateItem(index, { title: e.target.value })}
                    className={inputClass}
                  />
                </label>
                <label className="block md:col-span-2">
                  <span className="text-xs font-medium text-muted-foreground">Description</span>
                  <input
                    value={item.description ?? ""}
                    onChange={(e) => updateItem(index, { description: e.target.value })}
                    className={inputClass}
                  />
                </label>
                <label className="block">
                  <span className="text-xs font-medium text-muted-foreground">Category</span>
                  <select
                    value={item.category}
                    onChange={(e) =>
                      updateItem(index, { category: Number(e.target.value) as OnboardingChecklistItemCategoryValue })
                    }
                    className={inputClass}
                  >
                    {Object.entries(OnboardingChecklistItemCategory).map(([label, value]) => (
                      <option key={label} value={value}>
                        {formatOnboardingCategory(value as OnboardingChecklistItemCategoryValue)}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="block">
                  <span className="text-xs font-medium text-muted-foreground">Due days from start</span>
                  <input
                    type="number"
                    min={0}
                    value={item.defaultDueDaysFromStart ?? ""}
                    onChange={(e) =>
                      updateItem(index, {
                        defaultDueDaysFromStart: e.target.value === "" ? null : Number(e.target.value),
                      })
                    }
                    className={inputClass}
                    placeholder="Optional"
                  />
                </label>
                <label className="block">
                  <span className="text-xs font-medium text-muted-foreground">Priority</span>
                  <select
                    value={item.defaultPriority ?? ""}
                    onChange={(e) =>
                      updateItem(index, {
                        defaultPriority: e.target.value === "" ? null : (Number(e.target.value) as 1 | 2 | 3),
                      })
                    }
                    className={inputClass}
                  >
                    <option value="">None</option>
                    {Object.entries(taskPriorityLabels).map(([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="inline-flex items-center gap-2 self-end text-sm">
                  <input
                    type="checkbox"
                    checked={item.isRequired ?? true}
                    onChange={(e) => updateItem(index, { isRequired: e.target.checked })}
                  />
                  Required
                </label>
              </div>
              {form.items.length > 1 ? (
                <div className="mt-3">
                  <Button type="button" variant="secondary" onClick={() => removeItem(index)}>
                    Remove item
                  </Button>
                </div>
              ) : null}
            </div>
          ))}
        </div>

        <div className="mt-4 flex gap-2">
          <Button type="button" onClick={() => void submit()} disabled={isSaving}>
            {editingId == null ? "Create" : "Save"}
          </Button>
          {editingId != null ? (
            <Button type="button" variant="secondary" onClick={() => void remove()} disabled={isSaving}>
              Delete
            </Button>
          ) : null}
        </div>
      </div>
    </div>
  );
}
