import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';

export interface SessionState {
  sessionToken: string | null;
  slug: string | null;
  assessmentId: string | null;
  /**
   * Landing (E-1) ekranida bir nechta dastur bo'lganda o'quvchi tanlagan dastur kodi
   * (`docs/06` 8-bo'lim, `prompts/36`). `selectedProgramSlug` bilan birga saqlanadi — boshqa
   * maktab havolasi ochilganda eski tanlov noto'g'ri dasturga sizib ketmasligi uchun
   * o'qiyotgan tomon (`LandingPage`/`RegistrationPage`) `selectedProgramSlug === slug`ni
   * tekshiradi. Sahifa yangilanganda ham saqlanadi (`persist`) — DoD talabi.
   */
  selectedProgramCode: string | null;
  selectedProgramSlug: string | null;
  setSession: (token: string, slug: string, assessmentId: string) => void;
  setSelectedProgram: (slug: string, programCode: string) => void;
  clear: () => void;
}

/**
 * O'quvchi sessiyasi — Zustand + persist (`localStorage`), docs/10-frontend-arxitektura.md,
 * 4.1-bo'lim. Persist kaliti ({@link STORAGE_KEYS.session}) `shared/api/sessionToken.ts`
 * bilan "shartnoma" — ikkalasi shu nomdan foydalanadi (arxitektura izohiga qarang).
 *
 * `testCatalog` (Landing/Registration'dan `GetSchoolInfoResult.tests`) OLIB TASHLANDI (P36):
 * `GetSessionStateResult.tests[]`/`StartSessionResult.tests[]` (`PublicTestSummaryDto`) endi
 * o'zi `name`/`estimatedMinutes` qaytaradi (backend P34) — bu maydonlar sessiyaning HAQIQIY
 * dasturi bilan qat'iy bog'liq (bir nechta dastur bo'lganda maktabning BARCHA nashr qilingan
 * testlari emas, faqat shu sessiyaga biriktirilgan dastur testlari). `TestPage`/
 * `TestCompletePage` endi shulardan foydalanadi — alohida katalog kerak emas.
 */
export const useSessionStore = create<SessionState>()(
  persist(
    (set) => ({
      sessionToken: null,
      slug: null,
      assessmentId: null,
      selectedProgramCode: null,
      selectedProgramSlug: null,
      setSession: (token, slug, assessmentId) => {
        set({ sessionToken: token, slug, assessmentId });
      },
      setSelectedProgram: (slug, programCode) => {
        set({ selectedProgramSlug: slug, selectedProgramCode: programCode });
      },
      clear: () => {
        set({ sessionToken: null, slug: null, assessmentId: null });
      },
    }),
    { name: STORAGE_KEYS.session },
  ),
);
