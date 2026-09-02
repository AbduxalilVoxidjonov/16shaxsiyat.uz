import { create } from 'zustand';
import {
  setAdminAccessToken,
  setOnAdminSessionExpired,
  setOnAdminTokenRefreshed,
} from '@/shared/api/adminClient';
import type { AdminUser } from '../model/types';

export type { AdminUser };

interface AuthState {
  /**
   * `true` — ilova hali `GET /auth/me` orqali sessiyani tiklashga urinmoqda (sahifa
   * yangilanganidan keyingi birinchi render). `ProtectedRoute` shu bayroqqa qarab login'ga
   * qaytarishni **kechiktiradi** — aks holda haqiqatan tizimga kirgan foydalanuvchi ham
   * tekshiruv tugamasdan bir lahzaga login sahifasini ko'rib qolardi.
   */
  isRestoring: boolean;
  accessToken: string | null;
  user: AdminUser | null;
  setSession: (token: string, user: AdminUser) => void;
  setUser: (user: AdminUser) => void;
  clear: () => void;
}

/**
 * Superadmin sessiyasi — **persist YO'Q** (docs/10, 5.1-bo'lim; CLAUDE.md).
 * Access token React qatlamida shu Zustand store'da, HTTP qatlamida esa
 * `adminClient.ts` ichidagi modul o'zgaruvchisida saqlanadi; `setSession`/`clear`
 * ikkalasini sinxron tutadi.
 */
export const useAuthStore = create<AuthState>((set) => ({
  isRestoring: true,
  accessToken: null,
  user: null,
  setSession: (token, user) => {
    setAdminAccessToken(token);
    set({ accessToken: token, user, isRestoring: false });
  },
  setUser: (user) => {
    set({ user });
  },
  clear: () => {
    setAdminAccessToken(null);
    set({ accessToken: null, user: null, isRestoring: false });
  },
}));

// `adminClient` refresh ham 401 bersa shu yerga xabar beradi — sessiya avtomatik tozalanadi.
setOnAdminSessionExpired(() => {
  useAuthStore.getState().clear();
});

// Fon refresh'i tokenni almashtirganda store'ni ham yangilaymiz. Faqat sessiya ALLAQACHON
// o'rnatilgan bo'lsa — tiklash paytida (`isRestoring`) tokenni `ProtectedRoute` effekti
// `setSession` bilan `user` bilan birga qo'yadi, bu yerda erta qo'yish `user`siz render'ga
// olib kelardi.
setOnAdminTokenRefreshed((token) => {
  if (useAuthStore.getState().accessToken !== null) {
    useAuthStore.setState({ accessToken: token });
  }
});
