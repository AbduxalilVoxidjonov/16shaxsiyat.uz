import { useCallback } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ROUTES } from '@/shared/config/routes';
import { useToast } from '@/shared/ui/useToast';
import { useSessionStore } from '../store/sessionStore';
import { clearAnswerStore } from '../lib/answerQueue';

/**
 * `410 SESSION_EXPIRED` bo'lganda ishlatiladigan umumiy handler — CLAUDE.md MAXSUS DIQQAT
 * 7-band: sessiya/javob keshi tozalanadi, landing'ga qaytariladi, tushunarli xabar ko'rsatiladi.
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
    navigate(ROUTES.public.landing(slug), { replace: true });
  }, [clearSession, navigate, slug, t, toast]);
}
