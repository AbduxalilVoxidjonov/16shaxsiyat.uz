import { type ReactNode, useEffect } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useTranslation } from 'react-i18next';
import { getAdminAccessToken } from '@/shared/api/adminClient';
import { AppError } from '@/shared/api/AppError';
import { ROUTES } from '@/shared/config/routes';
import { ErrorState } from '@/shared/ui/ErrorState';
import { Spinner } from '@/shared/ui/Spinner';
import { useMeQuery } from './api/useMe';
import { useAuthStore } from './store/authStore';

/**
 * Sessiyani tugatishga ASOS bo'ladigan yagona xato — serverning aniq `401`i (ya'ni
 * `adminClient` refresh'ni ham urinib ko'rgan va u ham rad etilgan).
 *
 * Tarmoq uzilishi (`status: 0`), `502`/`503` (API konteyneri qayta ishga tushayotgani) yoki
 * boshqa server xatosi sessiya yaroqsiz ekanini BILDIRMAYDI. Ilgari `ProtectedRoute` har
 * qanday xatoda `clear()` chaqirardi — natijada API bir soniya javob bermasa, haqiqiy
 * sessiyasi bor superadmin login sahifasiga uloqtirilardi va sahifani yangilagach yana
 * kirardi (egasi 2026-09-02 da aynan shuni xabar qildi).
 */
function isSessionRejected(error: unknown): boolean {
  return error instanceof AppError && error.status === 401;
}

/**
 * Admin route'larini himoya qiladi (docs/10, 3-bo'lim):
 * 1. Token yo'q bo'lsa `/admin/login` ga, `?returnUrl=` bilan.
 * 2. Sahifa yangilanganda (access token faqat xotirada, `docs/10` 5.1-bo'lim — o'chib ketadi)
 *    `GET /api/auth/me` bilan sessiyani tiklashga urinadi (`adminClient`ning 401→refresh
 *    mutex mantig'i orqali — `useMeQuery` izohiga qarang). Tekshiruv tugagunча login'ga
 *    **darhol** qaytarmaydi — aks holda haqiqiy sessiyasi bor foydalanuvchi ham bir lahzaga
 *    login sahifasini ko'rib qolardi.
 */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const location = useLocation();
  const isRestoring = useAuthStore((state) => state.isRestoring);
  const accessToken = useAuthStore((state) => state.accessToken);
  const setSession = useAuthStore((state) => state.setSession);
  const clear = useAuthStore((state) => state.clear);

  const meQuery = useMeQuery(isRestoring);

  useEffect(() => {
    if (!isRestoring) return;
    if (meQuery.isSuccess) {
      const token = getAdminAccessToken();
      if (token) {
        setSession(token, meQuery.data);
      } else {
        clear();
      }
    } else if (meQuery.isError && isSessionRejected(meQuery.error)) {
      clear();
    }
  }, [
    isRestoring,
    meQuery.isSuccess,
    meQuery.isError,
    meQuery.error,
    meQuery.data,
    setSession,
    clear,
  ]);

  // Tiklash vaqtinchalik sabab bilan uzilgan: sessiyani O'CHIRMAYMIZ va login'ga ham
  // yubormaymiz — foydalanuvchi qayta urinadi va refresh cookie hamon o'z kuchida.
  if (isRestoring && meQuery.isError && !isSessionRejected(meQuery.error)) {
    return (
      <div className="flex min-h-dvh items-center justify-center p-4">
        <ErrorState
          title={t('auth.restore.errorTitle')}
          description={t('auth.restore.errorDescription')}
          onRetry={() => void meQuery.refetch()}
        />
      </div>
    );
  }

  if (isRestoring) {
    return (
      <div className="flex min-h-dvh items-center justify-center">
        <Spinner size={28} />
      </div>
    );
  }

  if (!accessToken) {
    const returnUrl = encodeURIComponent(`${location.pathname}${location.search}`);
    return <Navigate to={`${ROUTES.admin.login}?returnUrl=${returnUrl}`} replace />;
  }

  return <>{children}</>;
}
