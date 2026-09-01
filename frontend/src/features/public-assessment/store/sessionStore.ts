import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';

export interface SessionState {
  sessionToken: string | null;
  slug: string | null;
  assessmentId: string | null;
  setSession: (token: string, slug: string, assessmentId: string) => void;
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
      setSession: (token, slug, assessmentId) => {
        set({ sessionToken: token, slug, assessmentId });
      },
      clear: () => {
        set({ sessionToken: null, slug: null, assessmentId: null });
      },
    }),
    { name: STORAGE_KEYS.session },
  ),
);
