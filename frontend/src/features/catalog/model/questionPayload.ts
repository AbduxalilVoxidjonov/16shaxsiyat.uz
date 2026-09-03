import type { components } from '@/shared/api/schema';
import { QUESTION_TYPE_VALUES, type CatalogQuestionItem, type QuestionType } from './types';

/**
 * Savol tahrirlash formasining qiymatlari. `scale`/`direction`/`weight` formada HAR DOIM
 * bo'ladi (tizim testida ular faqat o'qish uchun ko'rsatiladi), lekin so'rov tanasiga
 * tushishi `isSystem` ga bog'liq — `buildQuestionUpdatePayload` ga qarang.
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
}

/**
 * `PUT /api/admin/catalog/questions/{id}` so'rov tanasi — backend
 * `UpdateTestQuestionRequest` dan re-export. `direction` sxemada `int?`, domenda faqat
 * `+1`/`-1` — toraytirildi. Tizim savolida `scale`/`direction`/`weight` `undefined` bo'ladi
 * va `JSON.stringify` ularni tanaga umuman qo'shmaydi (pastdagi izohga qarang).
 */
export type UpdateQuestionPayload = Omit<
  components['schemas']['UpdateTestQuestionRequest'],
  'direction'
> & { direction?: 1 | -1 };

/**
 * `POST /api/admin/catalog/tests/{id}/questions` so'rov tanasi — backend
 * `CreateTestQuestionRequest` dan re-export; `type`/`direction` toraytirildi.
 */
export type CreateQuestionPayload = Omit<
  components['schemas']['CreateTestQuestionRequest'],
  'type' | 'direction'
> & { type: QuestionType; direction: 1 | -1 };

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
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
 * Sof (pure) funksiya: React, i18n yoki tarmoqqa bog'liq emas — to'g'ridan-to'g'ri unit test
 * qilinadi (`questionPayload.test.ts`).
 */
export function buildQuestionUpdatePayload(
  question: CatalogQuestionItem,
  form: QuestionFormValues,
  options: { isSystem: boolean },
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

  return {
    ...payload,
    scale: form.scale.trim() || question.scale,
    direction: form.direction,
    weight: form.weight,
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
  };
}

/**
 * Yangi savol so'rov tanasi — faqat `Custom` testda ishlatiladi (tizim testida savol
 * qo'shish tugmasi umuman ko'rsatilmaydi, backend ham `409 SYSTEM_TEST_LOCKED` beradi).
 */
export function buildQuestionCreatePayload(
  form: QuestionFormValues & { code: string; type: QuestionType },
): CreateQuestionPayload {
  return {
    code: form.code.trim().toUpperCase(),
    order: form.order,
    textUz: form.textUz.trim(),
    textRu: trimmedOrNull(form.textRu),
    textEn: trimmedOrNull(form.textEn),
    type: form.type,
    scale: form.scale.trim(),
    direction: form.direction,
    weight: form.weight,
    isRequired: form.isRequired,
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
