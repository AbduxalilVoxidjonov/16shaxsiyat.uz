import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { AppError, NETWORK_ERROR_CODE, UNKNOWN_ERROR_CODE } from '@/shared/api/AppError';

/**
 * Backend `ProblemDetails.code` → foydalanuvchiga tushunarli o'zbekcha xabar. Xom `code`
 * hech qachon ekranga chiqmaydi (`CLAUDE.md` 11-qoida, `docs/06` 6-bo'lim).
 */
const CODE_MESSAGE_KEYS: Record<string, string> = {
  SYSTEM_TEST_LOCKED: 'catalog.errors.systemTestLocked',
  TEST_NOT_PUBLISHABLE: 'catalog.errors.testNotPublishable',
  TEST_DEFINITION_INVALID_TRANSITION: 'catalog.errors.invalidTransition',
  TEST_IN_USE: 'catalog.errors.testInUse',
  TEST_CODE_DUPLICATE: 'catalog.errors.testCodeDuplicate',
  TEST_NAME_DUPLICATE: 'catalog.errors.testNameDuplicate',
  QUESTION_CODE_DUPLICATE: 'catalog.errors.questionCodeDuplicate',
  SCALE_CODE_DUPLICATE: 'catalog.errors.scaleCodeDuplicate',
  SCALE_IN_USE: 'catalog.errors.scaleInUse',
  UNIQUE_CONSTRAINT_CONFLICT: 'catalog.errors.uniqueConstraintConflict',
  // `docs/18` §5 — tarmoqlanuvchi so'rovnoma kengaytmasi.
  SECTION_IN_USE: 'catalog.errors.sectionInUse',
  SECTION_CODE_DUPLICATE: 'catalog.errors.sectionCodeDuplicate',
  QUESTION_TYPE_NOT_SCORABLE: 'catalog.errors.questionTypeNotScorable',
  BRANCHING_NOT_ALLOWED_IN_SCORED: 'catalog.errors.branchingNotAllowedInScored',
  QUESTION_NOT_VISIBLE: 'catalog.errors.questionNotVisible',
  // Egasi topgan jonli xato (2026-09-11): javob berilgan savolni o'chirishga urinilganda
  // avval tushunarsiz `500 INTERNAL_ERROR` chiqardi — backend endi shu uch kod bilan `409`
  // qaytaradi (parallel backend vazifasi, shartnoma shu vazifaning topshirig'ida berilgan).
  QUESTION_IN_USE: 'catalog.errors.questionInUse',
  REFERENCED_RECORD_EXISTS: 'catalog.errors.referencedRecordExists',
  IMPORT_FILE_INVALID: 'catalog.errors.importFileInvalid',
  PAYLOAD_TOO_LARGE: 'catalog.errors.payloadTooLarge',
  NOT_FOUND: 'catalog.errors.notFound',
  FORBIDDEN: 'catalog.errors.forbidden',
  RATE_LIMITED: 'catalog.errors.rateLimited',
  CONCURRENCY_CONFLICT: 'catalog.errors.concurrencyConflict',
  [NETWORK_ERROR_CODE]: 'catalog.errors.network',
  [UNKNOWN_ERROR_CODE]: 'catalog.errors.generic',
  INTERNAL_ERROR: 'catalog.errors.generic',
};

/**
 * Katalog mutatsiyalari uchun yagona xato matni quruvchi. Ma'lum `code` uchun aniq o'zbekcha
 * matn; noma'lum kod bo'lsa backend bergan sarlavha (u ham o'zbekcha — `ExceptionHandling
 * Middleware`/`ToProblem` `title` ni domen xabaridan oladi); u ham bo'lmasa umumiy xabar.
 */
export function useCatalogErrorMessage(): (error: unknown) => string {
  const { t } = useTranslation();

  return useCallback(
    (error: unknown) => {
      if (!(error instanceof AppError)) {
        return t('catalog.errors.generic');
      }

      // `QUESTION_REFERENCED_BY_VISIBILITY` — backend `detail`'ida HAVOLA QILUVCHI savol/bo'lim
      // kodi bor (masalan "ST-Q05 savolining ko'rsatish sharti shu savolga tayanadi"). Bu kod
      // foydalanuvchi UCHUN ASOSIY ma'lumot — u aynan qaysi shartni o'zgartirishi kerakligini
      // bilishi kerak, shu sabab umumiy matnga YO'QOTILMAY, backend xabari ichiga qo'yiladi
      // (`CLAUDE.md` 11-qoidasi: `code` ekranga chiqmaydi, lekin `detail` — inson o'qiy oladigan
      // matn — chiqishi mumkin va kerak).
      if (error.code === 'QUESTION_REFERENCED_BY_VISIBILITY') {
        const detail = error.message.trim();
        return t('catalog.errors.questionReferencedByVisibility', {
          detail: detail.length > 0 ? detail : t('catalog.errors.generic'),
        });
      }

      const key = CODE_MESSAGE_KEYS[error.code];
      if (key) {
        return t(key);
      }

      return error.message.trim().length > 0 ? error.message : t('catalog.errors.generic');
    },
    [t],
  );
}
