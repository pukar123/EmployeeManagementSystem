import type { TaskPriority } from "@/features/tasks/types/task.types";

export const OnboardingChecklistItemCategory = {
  Documents: 1,
  Equipment: 2,
  AccountSetup: 3,
  Induction: 4,
  ManagerIntro: 5,
} as const;

export type OnboardingChecklistItemCategoryValue =
  (typeof OnboardingChecklistItemCategory)[keyof typeof OnboardingChecklistItemCategory];

export type OnboardingChecklistTemplateItem = {
  id: number;
  title: string;
  description: string | null;
  category: OnboardingChecklistItemCategoryValue;
  sortOrder: number;
  defaultDueDaysFromStart: number | null;
  defaultPriority: TaskPriority | null;
  isRequired: boolean;
};

export type OnboardingChecklistTemplate = {
  id: number;
  organizationId: number;
  name: string;
  description: string | null;
  isActive: boolean;
  isDefault: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  items: OnboardingChecklistTemplateItem[];
};

export type OnboardingChecklistTemplateItemInput = {
  id?: number;
  title: string;
  description?: string | null;
  category: OnboardingChecklistItemCategoryValue;
  sortOrder: number;
  defaultDueDaysFromStart?: number | null;
  defaultPriority?: TaskPriority | null;
  isRequired?: boolean;
};

export type CreateOnboardingChecklistTemplatePayload = {
  organizationId: number;
  name: string;
  description?: string | null;
  isActive?: boolean;
  isDefault?: boolean;
  items: OnboardingChecklistTemplateItemInput[];
};

export type UpdateOnboardingChecklistTemplatePayload = {
  name: string;
  description?: string | null;
  isActive?: boolean;
  isDefault?: boolean;
  items: OnboardingChecklistTemplateItemInput[];
};

export type EmployeeOnboardingProgressItem = {
  templateItemId: number;
  category: OnboardingChecklistItemCategoryValue;
  title: string;
  isRequired: boolean;
  sortOrder: number;
  taskId: number | null;
  status: number | null;
  dueAtUtc: string | null;
  isOverdue: boolean;
};

export type EmployeeOnboardingProgress = {
  templateId: number | null;
  templateName: string | null;
  generatedAtUtc: string | null;
  totalCount: number;
  completedCount: number;
  overdueCount: number;
  percentComplete: number;
  items: EmployeeOnboardingProgressItem[];
};
