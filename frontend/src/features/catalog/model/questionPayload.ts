import type { components } from '@/shared/api/schema';
import type { VisibilityRule } from '@/shared/lib/visibility';
import type { AdminQuestionPayloadFields } from '@/shared/api/branchingTypes';
import type { QuestionOptionFormValue } from './questionOptions';
import {
  isChoiceQuestionType,
  isSurveyOnlyQuestionType,
  isTextQuestionType,
  QUESTION_TYPE_VALUES,
  type CatalogQuestionItem,
  type QuestionType,
} from './types';

/**
 * Savol tahrirlash formasining qiymatlari. `scale`/`direction`/`weight` formada HAR DOIM
 * bo'ladi (tizim testida ular faqat o'qish uchun ko'rsatiladi), lekin so'rov tanasiga
 * tushishi `isSystem` ga bog'liq — `buildQuestionUpdatePayload` ga qarang.
 *
 * `docs/18` §2.2–§2.3 kengaytmasi — bari IXTIYORIY: eski chaqiruvchilar (testlar) ularni
 * bermasa ham compile bo'ladi, payload quruvchisi ularni turga qarab e'tiborsiz qoldiradi.
 */
export interface QuestionFormValues {
  textUz: string;
  textRu: string;
  textEn: string;
  order: number;
  isActive: boolean;
  isRequired: boolean;
  scale: string;
  direction: 1 | -1;
  weight: number;
  sectionCode?: string;
  placeholder?: string;
  inputPattern?: string;
  maxLength?: number;
  minSelections?: number;
  maxSelections?: number;
  options?: QuestionOptionFormValue[];
  visibility?: VisibilityRule | null;
}

/**
 * `PUT /api/admin/catalog/questions/{id}` so'rov tanasi — backend
 * `UpdateTestQuestionRequest` dan re-export. `direction` sxemada `int?`, domenda faqat
 * `+1`/`-1` — toraytirildi. Tizim savolida `scale`/`direction`/`weight` `undefined` bo'ladi
 * va `JSON.stringify` ularni tanaga umuman qo'shmaydi (pastdagi izohga qarang).
 *
 * `docs/18` §5 kengaytmasi (`AdminQuestionPayloadFields`) HALI sxemada yo'q — vaqtinchalik
 * qo'lda yozilgan (`shared/api/branchingTypes.ts`).
 */
export type UpdateQuestionPayload = Omit<
  components['schemas']['UpdateTestQuestionRequest'],
  'direction'
> & { direction?: 1 | -1 } & AdminQuestionPayloadFields;

/**
 * `POST /api/admin/catalog/tests/{id}/questions` so'rov tanasi — backend
 * `CreateTestQuestionRequest` dan re-export; `type`/`direction` toraytirildi.
 */
export type CreateQuestionPayload = Omit<
  components['schemas']['CreateTestQuestionRequest'],
  'type' | 'direction'
> & { type: QuestionType; direction: 1 | -1 } & AdminQuestionPayloadFields;

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function numberOrNull(value: number | undefined): number | null | undefined {
  if (value === undefined || Number.isNaN(value)) return null;
  return value;
}

/**
 * Turga mos kelmagan `docs/18` §2.3 maydonlari (`placeholder`/`inputPattern`/`maxLength`/
 * `minSelections`/`maxSelections`/`options`) so'rov tanasiga UMUMAN qo'shilmasin — masalan
 * `Likert5` savolida `options[]` yuborish ma'nosiz va backendni chalkashtiradi.
 */
function typeSpecificFields(
  type: QuestionType,
  form: QuestionFormValues,
): Pick<
  AdminQuestionPayloadFields,
  'placeholder' | 'inputPattern' | 'maxLength' | 'minSelections' | 'maxSelections' | 'options'
> {
  const fields: Pick<
    AdminQuestionPayloadFields,
    'placeholder' | 'inputPattern' | 'maxLength' | 'minSelections' | 'maxSelections' | 'options'
  > = {
    placeholder: undefined,
    inputPattern: undefined,
    maxLength: undefined,
    minSelections: undefined,
    maxSelections: undefined,
    options: undefined,
  };

  if (isTextQuestionType(type)) {
    fields.placeholder = form.placeholder ? trimmedOrNull(form.placeholder) : null;
    fields.maxLength = numberOrNull(form.maxLength);
    if (type === 'ShortText' || type === 'Phone') {
      fields.inputPattern = form.inputPattern ? trimmedOrNull(form.inputPattern) : null;
    }
  }

  if (type === 'MultiChoice') {
    fields.minSelections = numberOrNull(form.minSelections);
    fields.maxSelections = numberOrNull(form.maxSelections);
  }

  if (isChoiceQuestionType(type)) {
    fields.options = (form.options ?? []).map((option) => ({
      textUz: option.textUz.trim(),
      value: option.value,
      displayOrder: option.displayOrder,
    }));
  }

  return fields;
}

/**
 * Savolni saqlash uchun so'rov tanasini quradi.
 *
 * **ENG MUHIM (BR-8, `CLAUDE.md` 9a):** backend `UpdateTestQuestionCommandHandler` da
 * `scale`/`direction`/`weight` dan HAR QANDAY biri berilgan bo'lsa (`null` emas)
 * `Question.UpdateScale` chaqiriladi, u esa tizim savolida `DomainException
 * ("SYSTEM_TEST_LOCKED")` → `409` beradi. Ya'ni "o'zgarmagan" qiymatni qaytarib yuborish ham
 * tahrirlashni butunlay buzadi. Shu sabab tizim savolida bu uch maydon `undefined` bo'ladi va
 * `JSON.stringify` ularni tanaga umuman qo'shmaydi. `Custom` testda esa aksincha — ular
 * yuborilishi SHART, aks holda shkala/yo'nalish/og'irlikni tahrirlab bo'lmaydi.
 *
 * **`docs/18` B-3:** tizim metodikasida bo'lim ham, shart ham qo'shilmaydi — shu sabab
 * `sectionCode`/`visibility` ham tizim savolida `undefined` qoladi (backend baribir
 * `409 SYSTEM_TEST_LOCKED` bilan rad etardi, lekin bu yerda oldindan oldini olamiz).
 *
 * Sof (pure) funksiya: React, i18n yoki tarmoqqa bog'liq emas — to'g'ridan-to'g'ri unit test
 * qilinadi (`questionPayload.test.ts`).
 */
export function buildQuestionUpdatePayload(
  question: CatalogQuestionItem,
  form: QuestionFormValues,
  options: { isSystem: boolean; allowBranching?: boolean },
): UpdateQuestionPayload {
  const payload: UpdateQuestionPayload = {
    textUz: form.textUz.trim(),
    textRu: trimmedOrNull(form.textRu),
    textEn: trimmedOrNull(form.textEn),
    isActive: form.isActive,
    order: form.order,
    isRequired: form.isRequired,
  };

  if (options.isSystem) {
    return payload;
  }

  const type = toQuestionType(question.type);

  return {
    ...payload,
    scale: form.scale.trim() || question.scale,
    direction: form.direction,
    weight: form.weight,
    // `docs/18` B-2: bo'lim/shart FAQAT `Survey` anketalarda — `Scored` testda bu ikkisi
    // `undefined` qoladi (so'rov tanasiga umuman qo'shilmaydi).
    ...(options.allowBranching
      ? {
          sectionCode: form.sectionCode?.trim() ? form.sectionCode.trim() : null,
          visibility: form.visibility ?? null,
        }
      : {}),
    ...typeSpecificFields(type, form),
  };
}

/** Savol qatoridan forma boshlang'ich qiymatlarini quradi (tahrirlash rejimi). */
export function toQuestionFormValues(question: CatalogQuestionItem): QuestionFormValues {
  return {
    textUz: question.textUz,
    textRu: question.textRu ?? '',
    textEn: question.textEn ?? '',
    order: question.order,
    isActive: question.isActive,
    isRequired: question.isRequired,
    scale: question.scale,
    direction: question.direction,
    weight: question.weight,
    sectionCode: '', // `sectionId` → kod anketa bo'limlar ro'yxatidan aniqlanadi (`QuestionEditorDialog`)
    placeholder: question.placeholder ?? '',
    inputPattern: question.inputPattern ?? '',
    maxLength: question.maxLength ?? Number.NaN,
    minSelections: question.minSelections ?? Number.NaN,
    maxSelections: question.maxSelections ?? Number.NaN,
    options: (question.options ?? []).map((option) => ({
      textUz: option.textUz,
      value: option.value,
      displayOrder: option.displayOrder,
    })),
    visibility: question.visibility ?? null,
  };
}

/**
 * Yangi savol so'rov tanasi — faqat `Custom` testda ishlatiladi (tizim testida savol
 * qo'shish tugmasi umuman ko'rsatilmaydi, backend ham `409 SYSTEM_TEST_LOCKED` beradi).
 */
export function buildQuestionCreatePayload(
  form: QuestionFormValues & { code: string; type: QuestionType },
  options: { allowBranching?: boolean } = {},
): CreateQuestionPayload {
  const isSurveyOnly = isSurveyOnlyQuestionType(form.type);

  return {
    code: form.code.trim().toUpperCase(),
    order: form.order,
    textUz: form.textUz.trim(),
    textRu: trimmedOrNull(form.textRu),
    textEn: trimmedOrNull(form.textEn),
    type: form.type,
    // `docs/18` §2.3: yangi turlarda `scale`/`direction`/`weight` ishlatilmaydi — yashirin
    // standart bilan to'ldiriladi (`SURVEY`/`+1`/`1`), forma bu maydonlarni ko'rsatmaydi.
    scale: isSurveyOnly ? 'SURVEY' : form.scale.trim(),
    direction: isSurveyOnly ? 1 : form.direction,
    weight: isSurveyOnly ? 1 : form.weight,
    isRequired: form.isRequired,
    // `docs/18` B-2: bo'lim/shart FAQAT `Survey` anketalarda.
    ...(options.allowBranching
      ? {
          sectionCode: form.sectionCode?.trim() ? form.sectionCode.trim() : null,
          visibility: form.visibility ?? null,
        }
      : {}),
    ...typeSpecificFields(form.type, form),
  };
}

/**
 * Backend savol turini (`string`) frontend birlashmasiga o'giradi. Noma'lum qiymat kelsa
 * `Likert5` ga qaytadi — bu faqat KO'RSATISH uchun ishlatiladi (`PUT questions/{id}` savol
 * turini umuman qabul qilmaydi), shu sabab yolg'on qiymat serverga yuborilmaydi.
 */
export function toQuestionType(value: string): QuestionType {
  return QUESTION_TYPE_VALUES.find((item) => item === value) ?? 'Likert5';
}
