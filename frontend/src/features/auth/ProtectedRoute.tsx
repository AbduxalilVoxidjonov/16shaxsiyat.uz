import { type ReactNode, useEffect } from 'react';
import { Navigate, useLocation } from 'react-router';
import { getAdminAccessToken } from '@/shared/api/adminClient';
import { ROUTES } from '@/shared/config/routes';
import { Spinner } from '@/shared/ui/Spinner';
import { useMeQuery } from './api/useMe';
import { useAuthStore } from './store/authStore';

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
    } else if (meQuery.isError) {
      clear();
    }
  }, [isRestoring, meQuery.isSuccess, meQuery.isError, meQuery.data, setSession, clear]);

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
