import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import type { PublicTestCatalogItem } from '@/shared/api/types';

export interface SessionState {
  sessionToken: string | null;
  slug: string | null;
  assessmentId: string | null;
  /**
   * Maktab test katalogi (`docs/07` 1.1-bo'lim `GetSchoolInfoResult.tests`) — faqat ommaviy
   * metadata (`code`/`name`/`questionCount`/`estimatedMinutes`/`order`), PII yo'q. Landing/
   * Registration sahifalari yuklaganda saqlab qo'yadi, chunki `GET /schools/{slug}?k=` faqat
   * shu 2 sahifada `accessToken` (`k`) orqali chaqiriladi — test/blok-yakuni sahifalarida
   * (`/test/:testCode`, `.../done`) token yo'q, lekin test nomi va qolgan bloklar vaqtini
   * ko'rsatish uchun shu katalog kerak (E-3 header, E-4 "qolgan bloklar").
   *
   * // TODO: backend `GetSessionStateResult.tests[]`ga `name`/`estimatedMinutes` qo'shgach
   * // olib tashlanadi (PM P21 hisobotiga javobi, 4-band) — hozir vaqtinchalik: o'quvchi
   * // to'g'ridan-to'g'ri test havolasiga kirsa yoki `localStorage` tozalansa test nomi
   * // ko'rsatilmay qoladi (`TestPage`/`TestCompletePage` `testCode`ga qaytib tushadi).
   */
  testCatalog: PublicTestCatalogItem[] | null;
  setSession: (token: string, slug: string, assessmentId: string) => void;
  setTestCatalog: (catalog: PublicTestCatalogItem[]) => void;
  clear: () => void;
}

/**
 * O'quvchi sessiyasi — Zustand + persist (`localStorage`), docs/10-frontend-arxitektura.md,
 * 4.1-bo'lim. Persist kaliti ({@link STORAGE_KEYS.session}) `shared/api/sessionToken.ts`
 * bilan "shartnoma" — ikkalasi shu nomdan foydalanadi (arxitektura izohiga qarang).
 */
export const useSessionStore = create<SessionState>()(
  persist(
    (set) => ({
      sessionToken: null,
      slug: null,
      assessmentId: null,
      testCatalog: null,
      setSession: (token, slug, assessmentId) => {
        set({ sessionToken: token, slug, assessmentId });
      },
      setTestCatalog: (catalog) => {
        set({ testCatalog: catalog });
      },
      clear: () => {
        set({ sessionToken: null, slug: null, assessmentId: null, testCatalog: null });
      },
    }),
    { name: STORAGE_KEYS.session },
  ),
);
