import { useCallback } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useToast } from '@/shared/ui/useToast';
import { ROUTES } from '@/shared/config/routes';
import { PUBLIC_SPACE_SLUG } from '@/shared/config/publicSpace';
import { adoptSession } from '@/shared/api/sessionToken';
import { pickNextTestCode } from '@/shared/lib/nextTest';
import type { StartSessionResponse } from '@/shared/api/types';

/**
 * `POST /api/me/sessions` muvaffaqiyatidan keyingi YAGONA yo'l (`docs/07` §5.4):
 * 1. sessiyani o'quvchi oqimiga uzatish (`adoptSession` → `sessionStore`, `X-Session-Token`);
 * 2. `resumed: true` bo'lsa — "davom ettirildi" bildirishnomasi;
 * 3. `order` bo'yicha birinchi tugallanmagan blokga (`/t/ommaviy/test/:testCode`) o'tish,
 *    hammasi tugagan bo'lsa — yakuniy ekranga.
 *
 * Bir joyda, chunki uni ikki nuqta chaqiradi: `/kabinet/test` anketasi (yangi yoki `ready`)
 * va kabinetdagi "Davom ettirish" (tugallanmagan sessiya). Nusxa bo'lsa, masalan
 * `nextTest` qoidasi o'zgarganda biri eskirib qolardi.
 */
export function useEnterSession(): (result: StartSessionResponse) => void {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();

  return useCallback(
    (result: StartSessionResponse) => {
      adoptSession({
        sessionToken: result.sessionToken,
        slug: PUBLIC_SPACE_SLUG,
        assessmentId: result.assessmentId,
      });

      if (result.resumed) {
        toast.show({ variant: 'info', title: t('account.register.resumedNotice') });
      }

      const nextTestCode = pickNextTestCode(result.tests);
      navigate(
        nextTestCode
          ? ROUTES.public.test(PUBLIC_SPACE_SLUG, nextTestCode)
          : ROUTES.public.finish(PUBLIC_SPACE_SLUG),
      );
    },
    [navigate, t, toast],
  );
}
