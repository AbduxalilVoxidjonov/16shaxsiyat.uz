import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { ROUTES } from '@/shared/config/routes';
import { useAuthStore } from './store/authStore';

/**
 * Admin route'larini himoya qiladi — token yo'q bo'lsa `/admin/login` ga,
 * `?returnUrl=` bilan (docs/10, 3-bo'lim).
 */
export function ProtectedRoute({ children }: { children: ReactNode }) {
  const accessToken = useAuthStore((state) => state.accessToken);
  const location = useLocation();

  if (!accessToken) {
    const returnUrl = encodeURIComponent(`${location.pathname}${location.search}`);
    return <Navigate to={`${ROUTES.admin.login}?returnUrl=${returnUrl}`} replace />;
  }

  return <>{children}</>;
}
