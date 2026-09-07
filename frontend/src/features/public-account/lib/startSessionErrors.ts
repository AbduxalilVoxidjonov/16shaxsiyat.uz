import type { TFunction } from 'i18next';
import type { AppError } from '@/shared/api/AppError';

/**
 * `POST /api/me/sessions` va `PUT /api/me/profile` domen xatolari (`docs/07` §5.4 jadvali) →
 * foydalanuvchiga tushunarli matn. Forma (`PublicRegistrationForm`) va "tayyor" karta
 * (`PublicRegistrationPage` `ready` holati) BIR XIL xaritadan foydalanadi — ikki nusxa bo'lsa
 * vaqt o'tib ajralib ketardi. `fallback` — kod xaritada yo'q va server xabar bermagan holat
 * uchun matn: sessiya ochishda "Testni boshlashda xatolik", saqlashda "Saqlashda xatolik".
 */
export function mapStartSessionErrorCode(
  error: AppError,
  t: TFunction,
  fallback: string = t('account.register.errors.generic'),
): string {
  const codeMessages: Record<string, string> = {
    PROGRAM_REQUIRED: t('account.register.errors.programRequired'),
    NO_PROGRAM_AVAILABLE: t('account.register.errors.noProgram'),
    PUBLIC_SPACE_NOT_CONFIGURED: t('account.register.errors.notConfigured'),
    SCHOOL_INACTIVE: t('account.register.errors.inactive'),
    DUPLICATE_ASSESSMENT: t('account.register.errors.duplicate'),
    RATE_LIMITED: t('account.register.errors.rateLimited'),
    NOT_FOUND: t('account.register.errors.noProgram'),
  };

  return codeMessages[error.code] ?? error.message ?? fallback;
}
