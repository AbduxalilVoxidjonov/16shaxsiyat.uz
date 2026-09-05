import { useCallback } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTES } from '@/shared/config/routes';
import { PUBLIC_SPACE_SLUG } from '@/shared/config/publicSpace';
import { useToast } from '@/shared/ui/useToast';
import { useSessionStore } from '../store/sessionStore';
import { clearAnswerStore } from '../lib/answerQueue';

/**
 * `410 SESSION_EXPIRED` bo'lganda ishlatiladigan umumiy handler — CLAUDE.md MAXSUS DIQQAT
 * 7-band: sessiya/javob keshi tozalanadi, boshlanish ekraniga qaytariladi, tushunarli xabar
 * ko'rsatiladi.
 *
 * **Maktab oqimi uchun xatti-harakat O'ZGARMAGAN** — `/t/:slug` landing sahifasi. Yagona
 * istisno — ommaviy makon slug'i (`PUBLIC_SPACE_SLUG`, P47): u yerda maktab landing'i
 * MA'NOSIZ bo'lardi (`GET /api/public/schools/ommaviy?k=` maktab havolasi tokenini talab
 * qiladi va "Havola topilmadi" beradi), foydalanuvchining uyi esa — o'z kabineti. Slug
 * backendda ommaviy makon uchun band, ya'ni birorta maktab bu shoxga tusha olmaydi.
 */
export function useSessionExpiredGuard(slug: string): () => void {
  const navigate = useNavigate();
  const toast = useToast();
  const { t } = useTranslation();
  const clearSession = useSessionStore((state) => state.clear);

  return useCallback(() => {
    clearSession();
    clearAnswerStore();
    toast.show({ variant: 'warning', title: t('test.sessionExpiredTitle') });
    navigate(slug === PUBLIC_SPACE_SLUG ? ROUTES.account.home : ROUTES.public.landing(slug), {
      replace: true,
    });
  }, [clearSession, navigate, slug, t, toast]);
}
