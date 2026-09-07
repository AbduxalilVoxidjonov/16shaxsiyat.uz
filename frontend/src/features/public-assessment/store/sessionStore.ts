import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { setSessionAdopter } from '@/shared/api/sessionToken';
import { clearAnswerStore } from '../lib/answerQueue';

/**
 * Havola bilan kelganda (`/t/:slug?k=`) faol o'rindan chetlatilgan, lekin xuddi shu havola va
 * maktab bo'lgani uchun "Davom ettirish" sifatida TAKLIF qilinadigan oldingi sessiya.
 * Persist QILINMAYDI (`partialize`) — faqat shu tab ichida, shu tashrif davomida yashaydi.
 */
export interface ResumableSession {
  sessionToken: string;
  slug: string;
  assessmentId: string;
  accessToken: string;
}

export interface SessionState {
  sessionToken: string | null;
  slug: string | null;
  assessmentId: string | null;
  /**
   * Sessiya ochilgan maktab havolasining tokeni (`?k=`). Telegram (kabinet) sessiyasida `null`.
   * Havola bilan qayta kelganda oldingi sessiya "shu havolaniki"mi yoki boshqa
   * havola/maktabniki ekanini ajratish uchun (`startFresh`).
   */
  accessToken: string | null;
  resumable: ResumableSession | null;
  /**
   * Landing (E-1) ekranida bir nechta dastur bo'lganda o'quvchi tanlagan dastur kodi
   * (`docs/06` 8-bo'lim, `prompts/36`). `selectedProgramSlug` bilan birga saqlanadi — boshqa
   * maktab havolasi ochilganda eski tanlov noto'g'ri dasturga sizib ketmasligi uchun
   * o'qiyotgan tomon (`LandingPage`/`RegistrationPage`) `selectedProgramSlug === slug`ni
   * tekshiradi. Sahifa yangilanganda ham saqlanadi (`persist`) — DoD talabi.
   */
  selectedProgramCode: string | null;
  selectedProgramSlug: string | null;
  setSession: (
    token: string,
    slug: string,
    assessmentId: string,
    accessToken?: string | null,
  ) => void;
  setSelectedProgram: (slug: string, programCode: string) => void;
  /**
   * Havola/kod bilan YANGI kirish (`/t/:slug?k=`) = toza boshlanish. Nega: bir qurilma —
   * bir necha o'quvchi (maktab kompyuteri, sinf planshetи). Ilgari oldingi o'quvchining
   * tokeni store'da qolardi va keyingisi uning sessiyasida davom etib ketardi.
   *
   * - Faol sessiya (token/slug/assessmentId) HAR DOIM tozalanadi — qaysi maktab (`slug`)
   *   yoki Telegram (`ommaviy`) sessiyasi bo'lishidan qat'i nazar.
   * - Agar u xuddi shu maktab va xuddi shu havola (`k`) ostida ochilgan bo'lsa — `resumable`ga
   *   ko'chiriladi: `LandingPage` uni ixtiyoriy "Davom ettirish" tugmasi sifatida ko'rsatadi
   *   (standart yo'l — "Boshlash", toza anketa). Boshqa havola/maktabniki bo'lsa — tashlanadi.
   * - Yangi `accessToken` (`k`) saqlanadi.
   *
   * Idempotent: faol sessiya YO'Q bo'lsa (masalan StrictMode'da effekt ikki marta ishlaganda
   * yoki sahifa yangilanganda) mavjud `resumable` — agar u shu slug/`k`ga mos bo'lsa — saqlanib
   * qoladi, mos bo'lmasa tashlanadi.
   */
  startFresh: (slug: string, accessToken: string) => void;
  /** "Davom ettirish" bosilganda — `resumable` yana faol sessiyaga qaytariladi. */
  restoreResumable: () => void;
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
    (set, get) => ({
      sessionToken: null,
      slug: null,
      assessmentId: null,
      accessToken: null,
      resumable: null,
      selectedProgramCode: null,
      selectedProgramSlug: null,
      setSession: (token, slug, assessmentId, accessToken = null) => {
        // Javob navbati (`answerQueue`) sessiyaga EMAS, faqat `questionId`ga bog'langan, katalog
        // savollari esa hamma o'quvchi uchun bir xil. Shu sabab BOSHQA token = boshqa sessiya
        // (yangi o'quvchi) bo'lganda eski navbat tozalanadi — aks holda oldingi o'quvchining
        // javoblari ekranda "belgilangan" ko'rinardi va yangi sessiyaga yuborilardi.
        // `resumed: true` (server o'sha `SessionToken`ni qaytaradi) — token o'zgarmaydi, navbat
        // (shu o'quvchining hali yuborilmagan javoblari) saqlanib qoladi.
        if (get().sessionToken !== token) {
          clearAnswerStore();
        }
        set({ sessionToken: token, slug, assessmentId, accessToken, resumable: null });
      },
      setSelectedProgram: (slug, programCode) => {
        set({ selectedProgramSlug: slug, selectedProgramCode: programCode });
      },
      startFresh: (slug, accessToken) => {
        set((state) => {
          if (!state.sessionToken) {
            const keepResumable =
              state.resumable !== null &&
              state.resumable.slug === slug &&
              state.resumable.accessToken === accessToken;
            return { accessToken, resumable: keepResumable ? state.resumable : null };
          }
          const sameLink =
            state.slug === slug && state.accessToken === accessToken && state.assessmentId;
          return {
            sessionToken: null,
            slug: null,
            assessmentId: null,
            accessToken,
            resumable: sameLink
              ? {
                  sessionToken: state.sessionToken,
                  slug,
                  assessmentId: state.assessmentId as string,
                  accessToken,
                }
              : null,
          };
        });
      },
      restoreResumable: () => {
        set((state) => {
          if (!state.resumable) return {};
          const { sessionToken, slug, assessmentId, accessToken } = state.resumable;
          return { sessionToken, slug, assessmentId, accessToken, resumable: null };
        });
      },
      clear: () => {
        set({
          sessionToken: null,
          slug: null,
          assessmentId: null,
          accessToken: null,
          resumable: null,
        });
      },
    }),
    {
      name: STORAGE_KEYS.session,
      // `resumable` — taklif, sessiya emas: `localStorage`ga yozilmaydi, aks holda bir hafta
      // oldingi o'quvchining sessiyasi keyingi o'quvchiga "Davom ettirish" bo'lib chiqardi.
      partialize: (state) => ({
        sessionToken: state.sessionToken,
        slug: state.slug,
        assessmentId: state.assessmentId,
        accessToken: state.accessToken,
        selectedProgramCode: state.selectedProgramCode,
        selectedProgramSlug: state.selectedProgramSlug,
      }),
    },
  ),
);

/**
 * Maktabsiz (kabinet) sessiyasini qabul qilish — `shared/api/sessionToken.ts` dagi neytral
 * uzatish nuqtasi. `features/public-account` `POST /api/me/sessions` bilan sessiya ochadi,
 * lekin uni SHU store olib boradi; ikki feature bir-birini import qilmagani uchun
 * (`docs/10` §2) bog'lanish shu ro'yxatdan o'tish orqali.
 *
 * Bu modul yuklanmagan bo'lsa uzatish `localStorage` orqali ketadi va store keyin
 * hydration bilan xuddi shu holatga keladi — maktab oqimi uchun HECH NARSA o'zgarmaydi.
 * Telegram sessiyasida maktab havolasi yo'q — `accessToken` `null` bo'ladi.
 */
setSessionAdopter(({ sessionToken, slug, assessmentId, accessToken }) => {
  useSessionStore.getState().setSession(sessionToken, slug, assessmentId, accessToken ?? null);
});
