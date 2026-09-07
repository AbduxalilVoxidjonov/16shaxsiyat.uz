import type { QueryClient } from '@tanstack/react-query';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import { useSessionStore } from '../store/sessionStore';
import { clearAnswerStore } from './answerQueue';

/**
 * `QUERY_KEYS.publicTestQuestions(testCode, page)` kalitining umumiy prefiksi
 * (`['public', 'test-questions']`) — barcha bloklar/sahifalar keshini bir yo'la olib tashlash
 * uchun. Fabrikadan olinadi, alohida yozilmaydi: kalit o'zgarsa bu joy eskirib qolmasin.
 */
const TEST_QUESTIONS_PREFIX = QUERY_KEYS.publicTestQuestions('', 0).slice(0, 2);

/**
 * Sessiyaga BOG'LIQ TanStack Query keshini olib tashlaydi: `sessions/me`, savollar
 * (`currentValue` bilan), natija. Maktab ma'lumoti (`publicSchoolInfo`) sessiyaga bog'liq
 * emas — tegilmaydi.
 */
export function removeSessionQueries(queryClient: QueryClient): void {
  queryClient.removeQueries({ queryKey: QUERY_KEYS.publicSessionMe() });
  queryClient.removeQueries({ queryKey: TEST_QUESTIONS_PREFIX });
  queryClient.removeQueries({ queryKey: QUERY_KEYS.publicStudentResult() });
}

/**
 * Havola/kod bilan yangi kirish (`/t/:slug?k=`) = toza boshlanish. Nega: bir qurilma — bir
 * necha o'quvchi. Maktab kodi bilan kirgan HAR odam yangi odam deb hisoblanadi (egasining
 * qarori). Uch joyda oldingi o'quvchining izi qolishi mumkin edi va uchalasi ham tozalanadi:
 *
 * 1. `sessionStore` — token/slug/assessmentId (`startFresh`; xuddi shu havolaniki bo'lsa
 *    ixtiyoriy "Davom ettirish" taklifiga ko'chadi, store izohiga qarang);
 * 2. `answerQueue` (`localStorage`) — navbat sessiyaga EMAS, `questionId`ga bog'langan; katalog
 *    savollari hamma uchun bir xil, ya'ni oldingi o'quvchining javoblari yangi o'quvchida
 *    "belgilangan" ko'rinardi va (`pending: true` bo'lsa) YANGI sessiyaga yuborilardi;
 * 3. TanStack Query keshi — `sessions/me`, savollar (`currentValue`), natija.
 *
 * `?k=` BO'LMAGAN kelish (masalan test ichidan "orqaga") bu funksiyani chaqirmaydi — o'sha
 * o'quvchi o'z sessiyasida qoladi.
 */
export function beginFreshVisit(queryClient: QueryClient, slug: string, accessToken: string): void {
  useSessionStore.getState().startFresh(slug, accessToken);
  clearAnswerStore();
  removeSessionQueries(queryClient);
}
