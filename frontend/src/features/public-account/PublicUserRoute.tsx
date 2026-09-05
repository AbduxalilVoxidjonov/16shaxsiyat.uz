import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { ROUTES } from '@/shared/config/routes';
import { Spinner } from '@/shared/ui/Spinner';
import { usePublicSession } from './api/usePublicSession';

/**
 * Kabinet sahifalarini himoya qiladi (`docs/07` §5 — barcha endpointlar `Bearer` talab
 * qiladi).
 *
 * 1. Sessiya tiklanayotgan bo'lsa (`restoring`) — spinner; login'ga DARHOL qaytarilmaydi,
 *    aks holda haqiqatan kirgan foydalanuvchi ham har sahifa yangilanganda bir lahzaga
 *    kirish sahifasini ko'rib qolardi.
 * 2. Anonim bo'lsa — `/kirish` ga, qaytish yo'li `?returnUrl=` bilan (superadmin
 *    oqimidagi `ProtectedRoute` bilan bir xil konvensiya).
 */
export function PublicUserRoute({ children }: { children: ReactNode }) {
  const location = useLocation();
  const { isRestoring, isAuthenticated } = usePublicSession();

  if (isRestoring) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner size={28} />
      </div>
    );
  }

  if (!isAuthenticated) {
    const returnUrl = encodeURIComponent(`${location.pathname}${location.search}`);
    return <Navigate to={`${ROUTES.account.login}?returnUrl=${returnUrl}`} replace />;
  }

  return <>{children}</>;
}
