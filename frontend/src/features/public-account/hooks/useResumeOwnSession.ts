import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { AppError } from '@/shared/api/AppError';
import { ROUTES } from '@/shared/config/routes';
import { useStartOwnSession } from '../api/useStartOwnSession';
import { buildReadyPayload } from '../lib/profileState';
import { mapStartSessionErrorCode } from '../lib/startSessionErrors';
import { useEnterSession } from './useEnterSession';

export interface ResumeOwnSession {
  /** `POST /api/me/sessions` `{}` — tugallanmagan sessiyani qaytaradi va test oqimiga o'tadi. */
  resume: () => Promise<void>;
  isPending: boolean;
  /** Oxirgi urinish xatosi (foydalanuvchiga tayyor matn); yangi urinishda tozalanadi. */
  error: string | null;
}

/**
 * Kabinetdagi "Davom ettirish" (`docs/07` §5.4): tugallanmagan sessiya bo'lsa server yangisini
 * yaratmaydi — o'shani `200`, `resumed: true` va YANGI `sessionToken` bilan qaytaradi (boshqa
 * qurilmadan kirilganda ham ishlaydi). Tana `{}` — shaxsiy ma'lumot yuborilmaydi
 * (`buildReadyPayload`, `ready` holati bilan bir xil).
 *
 * Xatolar (`docs/07` §5.4 jadvali): `400 VALIDATION_ERROR` — rozilik shu orada eskirgan yoki
 * profil yetishmaydi → `/kabinet/test` (u `consent` rejimini o'zi aniqlaydi, faqat yetishmaganini
 * so'raydi va sessiyani ochadi); qolgani (`NO_PROGRAM_AVAILABLE`, `PUBLIC_SPACE_NOT_CONFIGURED`,
 * `SCHOOL_INACTIVE`, `RATE_LIMITED`…) — `startSessionErrors` xaritasidan matn.
 */
export function useResumeOwnSession(): ResumeOwnSession {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const startSession = useStartOwnSession();
  const enterSession = useEnterSession();
  const [error, setError] = useState<string | null>(null);

  async function resume(): Promise<void> {
    setError(null);
    try {
      const result = await startSession.mutateAsync(buildReadyPayload());
      enterSession(result);
    } catch (caught) {
      if (caught instanceof AppError && caught.code === 'VALIDATION_ERROR') {
        navigate(ROUTES.account.startTest);
        return;
      }
      setError(
        caught instanceof AppError
          ? mapStartSessionErrorCode(caught, t)
          : t('account.register.errors.generic'),
      );
    }
  }

  return { resume, isPending: startSession.isPending, error };
}
