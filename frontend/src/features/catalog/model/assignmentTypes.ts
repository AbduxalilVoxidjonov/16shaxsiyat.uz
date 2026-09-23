import type { components } from '@/shared/api/schema';
import {
  REGISTRATION_MODE_VALUES,
  type RegistrationMode,
} from '@/shared/api/registrationModeTypes';
import { isProgramState, type ProgramState } from '@/shared/lib/programState';
import type { TestDefinitionStatus } from './types';

/**
 * Test biriktirish — `docs/07` §3.4.1 (2026-09-23 egasi qarori: "Dasturlar" bo'limi olib
 * tashlandi, test KIMGA ochiqligi test ichida boshqariladi).
 *
 * DTO `schema.d.ts` dan re-export; enum maydonlari (`testStatus`, `registrationMode`,
 * `state`) sxemada oddiy `string` — `docs/10` §6.2 2-naqsh bilan toraytiriladi.
 */
type AssignmentEnums = {
  testStatus: TestDefinitionStatus;
  registrationMode: RegistrationMode;
  state: ProgramState | null;
};

export type TestAssignment = Omit<
  components['schemas']['AdminTestAssignmentDto'],
  keyof AssignmentEnums
> &
  AssignmentEnums;

export type UpdateTestAssignmentRequest = Omit<
  components['schemas']['UpdateTestAssignmentRequest'],
  'registrationMode' | 'schoolIds'
> & {
  schoolIds: string[];
  registrationMode: RegistrationMode;
};

/** Javobdagi xom qiymatlarni toraytiradi — noma'lum `state` → `null` ("ma'lumot yo'q"). */
export function normalizeTestAssignment(
  raw: components['schemas']['AdminTestAssignmentDto'],
): TestAssignment {
  const testStatus: TestDefinitionStatus =
    raw.testStatus === 'Published' || raw.testStatus === 'Archived' ? raw.testStatus : 'Draft';
  const registrationMode: RegistrationMode = (
    REGISTRATION_MODE_VALUES as readonly string[]
  ).includes(raw.registrationMode)
    ? (raw.registrationMode as RegistrationMode)
    : 'Full';
  return {
    ...raw,
    testStatus,
    registrationMode,
    state: isProgramState(raw.state) ? raw.state : null,
  };
}

/** "Kimga ochiq" — kartadagi radio. `all` ⟺ `isPublic: true`. */
export type AssignmentAudience = 'all' | 'selected';

/** Kartaning forma holati (saqlanmagan o'zgarishlar shu yerda). */
export interface TestAssignmentFormState {
  audience: AssignmentAudience;
  schoolIds: string[];
  isInPublicSpace: boolean;
  registrationMode: RegistrationMode;
}

export function toAssignmentFormState(assignment: TestAssignment): TestAssignmentFormState {
  return {
    audience: assignment.isPublic ? 'all' : 'selected',
    schoolIds: [...assignment.schoolIds],
    isInPublicSpace: assignment.isInPublicSpace,
    registrationMode: assignment.registrationMode,
  };
}

/**
 * `PUT` tanasi. "Barcha maktablarga" tanlansa `schoolIds` BO'SH yuboriladi — ko'rinmaydigan
 * (yashirin) biriktirma qolmasin: admin ekranda ko'rgani saqlanadi (WYSIWYG). `isInPublicSpace`
 * har doim aniq qiymat bilan yuboriladi (`null` — "o'zgarmaydi" — kerak emas: karta joriy
 * holatni GET'dan oladi).
 */
export function buildAssignmentPayload(form: TestAssignmentFormState): UpdateTestAssignmentRequest {
  const isPublic = form.audience === 'all';
  return {
    isPublic,
    schoolIds: isPublic ? [] : [...form.schoolIds],
    registrationMode: form.registrationMode,
    isInPublicSpace: form.isInPublicSpace,
  };
}

/** Forma saqlangan holatdan farq qiladimi ("Saqlash" tugmasi shunga qarab yoqiladi). */
export function isAssignmentFormDirty(
  form: TestAssignmentFormState,
  saved: TestAssignmentFormState,
): boolean {
  if (form.audience !== saved.audience) return true;
  if (form.isInPublicSpace !== saved.isInPublicSpace) return true;
  if (form.registrationMode !== saved.registrationMode) return true;
  if (form.audience === 'all') return false;
  if (form.schoolIds.length !== saved.schoolIds.length) return true;
  const savedSet = new Set(saved.schoolIds);
  return form.schoolIds.some((id) => !savedSet.has(id));
}

/**
 * Kartadagi holat ko'rsatkichi — FAQAT backend maydonlaridan (`state`, `isAvailable`,
 * biriktirma bor-yo'qligi); frontend ko'rinish mezonini qayta ixtiro qilmaydi.
 */
export type AssignmentStatusKind =
  'archived' | 'unassigned' | 'draft' | 'paused' | 'active' | 'unknown';

export function resolveAssignmentStatus(assignment: TestAssignment): AssignmentStatusKind {
  if (assignment.testStatus === 'Archived' || assignment.state === 'Archived') return 'archived';
  const hasTargets =
    assignment.isPublic || assignment.schoolIds.length > 0 || assignment.isInPublicSpace;
  if (!assignment.isConfigured || !hasTargets) return 'unassigned';
  if (assignment.isAvailable) return 'active';
  switch (assignment.state) {
    case 'Draft':
      return 'draft';
    case 'Paused':
      return 'paused';
    case 'Active':
      // `Active`, lekin `isAvailable: false` — backend mezoni bo'yicha hech kimga ochiq emas.
      return 'unassigned';
    default:
      return 'unknown';
  }
}
