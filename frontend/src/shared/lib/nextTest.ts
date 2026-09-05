import type { PublicTestSummary } from '@/shared/api/types';

/**
 * Sessiya ochilgach/tiklangach qaysi test blokiga o'tish kerakligini aniqlaydi:
 * `order` bo'yicha birinchi tugallanmagan test; hammasi tugagan bo'lsa `null`
 * (chaqiruvchi tomon bu holda yakuniy ekranga yo'naltiradi — `ROUTES.public.finish`).
 *
 * `shared/lib` da, chunki ikkala anketa ham ishlatadi: maktab oqimi
 * (`features/public-assessment`) va maktabsiz kabinet oqimi (`features/public-account`) —
 * ikkalasi ham javob sifatida aynan bir xil `StartSessionResult.tests[]` oladi.
 */
export function pickNextTestCode(tests: readonly PublicTestSummary[]): string | null {
  const next = [...tests].sort((a, b) => a.order - b.order).find((test) => test.status !== 'Completed');
  return next?.code ?? null;
}
