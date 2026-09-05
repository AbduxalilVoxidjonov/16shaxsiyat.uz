import { create } from 'zustand';
import {
  hasPublicSessionHint,
  setOnPublicSessionExpired,
  setOnPublicTokenRefreshed,
  setPublicAccessToken,
  setPublicSessionHint,
} from '@/shared/api/publicUserClient';
import type { PublicUser } from '@/shared/api/types';

/**
 * Ommaviy (Telegram orqali kirgan) foydalanuvchi sessiyasi — **persist YO'Q**.
 *
 * Access token React qatlamida shu store'da, HTTP qatlamida esa `publicUserClient.ts`
 * ichidagi modul o'zgaruvchisida; `setSession`/`clear` ikkalasini sinxron tutadi.
 * Nega `localStorage`ga yozilmasligi — `publicUserClient.ts` boshidagi izohda (XSS +
 * refresh cookie'si baribir ishonchliroq manba).
 */

/**
 * `restoring` — sahifa endi ochildi va oldingi sessiya `POST /api/auth/telegram/refresh`
 * bilan tiklanmoqda. Guard shu holatda login'ga qaytarishni KECHIKTIRADI, aks holda
 * haqiqatan kirgan foydalanuvchi ham har yangilashda bir lahzaga kirish sahifasini
 * ko'rib qolardi (superadmin oqimidagi `isRestoring` bilan bir xil sabab).
 */
export type PublicSessionStatus = 'restoring' | 'authenticated' | 'anonymous';

interface PublicUserState {
  status: PublicSessionStatus;
  accessToken: string | null;
  user: PublicUser | null;
  setSession: (token: string, user: PublicUser) => void;
  setUser: (user: PublicUser) => void;
  clear: () => void;
}

export const usePublicUserStore = create<PublicUserState>((set) => ({
  // Belgisi yo'q brauzerda tiklashga urinib o'tirmaymiz — anonim tashrifchiga har
  // sahifada 401 bilan tugaydigan so'rov yubormaslik uchun (`storageKeys.ts` izohi).
  status: hasPublicSessionHint() ? 'restoring' : 'anonymous',
  accessToken: null,
  user: null,
  setSession: (token, user) => {
    setPublicAccessToken(token);
    setPublicSessionHint(true);
    set({ status: 'authenticated', accessToken: token, user });
  },
  setUser: (user) => {
    set({ user });
  },
  clear: () => {
    setPublicAccessToken(null);
    setPublicSessionHint(false);
    set({ status: 'anonymous', accessToken: null, user: null });
  },
}));

// Refresh ham `401` bergan — sessiya tugadi, store tozalanadi.
setOnPublicSessionExpired(() => {
  usePublicUserStore.getState().clear();
});

// Fon refresh'i tokenni almashtirganda store ham yangilanadi — faqat sessiya ALLAQACHON
// o'rnatilgan bo'lsa: tiklash paytida tokenni `user` bilan birga `setSession` qo'yadi.
setOnPublicTokenRefreshed((token) => {
  if (usePublicUserStore.getState().status === 'authenticated') {
    usePublicUserStore.setState({ accessToken: token });
  }
});
