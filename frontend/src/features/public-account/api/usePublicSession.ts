import { useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getPublicAccessToken,
  publicUserRequest,
  refreshPublicAccessToken,
} from '@/shared/api/publicUserClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { PublicUser } from '@/shared/api/types';
import { usePublicUserStore } from '../store/publicUserStore';

/**
 * Sahifa yangilanganidan keyin sessiyani tiklaydi: refresh cookie (`docs/07` §2a.2) bilan
 * yangi access token oladi va `GET /api/me` (§5.1) orqali profilni tortadi.
 * `null` — tiklab bo'lmadi (cookie yo'q/yaroqsiz), ya'ni foydalanuvchi anonim.
 */
async function restorePublicSession(): Promise<PublicUser | null> {
  const refreshed = await refreshPublicAccessToken();
  if (!refreshed) {
    return null;
  }
  return publicUserRequest<PublicUser>('/api/me');
}

export interface PublicSession {
  isRestoring: boolean;
  isAuthenticated: boolean;
  user: PublicUser | null;
}

/**
 * Ommaviy foydalanuvchi sessiyasining joriy holati.
 *
 * Bir necha joyda chaqiriladi (`MarketingLayout` navigatsiyasi, `PublicUserRoute` guard'i,
 * kabinet sahifalari) — TanStack Query bitta `queryKey` uchun bitta so'rov yuborgani sabab
 * tiklash HAR HOLDA bir marta bajariladi.
 */
export function usePublicSession(): PublicSession {
  const status = usePublicUserStore((state) => state.status);
  const user = usePublicUserStore((state) => state.user);
  const setSession = usePublicUserStore((state) => state.setSession);
  const clear = usePublicUserStore((state) => state.clear);

  const query = useQuery({
    queryKey: QUERY_KEYS.publicUserSession(),
    queryFn: restorePublicSession,
    enabled: status === 'restoring',
    retry: false,
    staleTime: Infinity,
  });

  useEffect(() => {
    if (status !== 'restoring') return;

    if (query.isSuccess) {
      const token = getPublicAccessToken();
      if (query.data && token) {
        setSession(token, query.data);
      } else {
        clear();
      }
    } else if (query.isError) {
      // Tarmoq uzilishi ham shu yerga tushadi. Sessiyani "yo'q" deb belgilaymiz, lekin
      // refresh cookie'si serverda o'z kuchida qoladi — foydalanuvchi sahifani yangilasa
      // yoki qaytadan kirsa sessiyasi tiklanadi.
      clear();
    }
  }, [status, query.isSuccess, query.isError, query.data, setSession, clear]);

  return {
    isRestoring: status === 'restoring',
    isAuthenticated: status === 'authenticated',
    user,
  };
}

/**
 * Chiqish — `POST /api/auth/telegram/logout` (`docs/07` §2a.3, har doim `204`) va lokal
 * holatni tozalash. So'rov muvaffaqiyatsiz bo'lsa ham lokal sessiya baribir tozalanadi:
 * foydalanuvchi "chiqdim" deb o'ylab, aslida kirgan holda qolib ketmasligi kerak.
 */
export function usePublicLogout(): () => Promise<void> {
  const clear = usePublicUserStore((state) => state.clear);
  const queryClient = useQueryClient();

  return async () => {
    try {
      await publicUserRequest<void>('/api/auth/telegram/logout', { method: 'POST' });
    } catch {
      // Idempotent endpoint — xato faqat tarmoq darajasida bo'lishi mumkin.
    } finally {
      clear();
      queryClient.removeQueries({ queryKey: ['public-user'] });
    }
  };
}
