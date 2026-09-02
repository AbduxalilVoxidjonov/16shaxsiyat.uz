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

      const key = CODE_MESSAGE_KEYS[error.code];
      if (key) {
        return t(key);
      }

      return error.message.trim().length > 0 ? error.message : t('catalog.errors.generic');
    },
    [t],
  );
}
