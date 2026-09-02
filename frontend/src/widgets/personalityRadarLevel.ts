/**
 * `factorPct` → daraja matni — docs/03-psixologik-metodikalar.md, 3.2-bo'lim "Daraja tasnifi"
 * jadvali bilan bir xil chegaralar. Big Five omillari (`O`,`C`,`E`,`A`,`N`) uchun daraja
 * backenddan keladi, lekin `stabilityPct` (`100 − N`) uchun alohida daraja backend javobida
 * yo'q (docs/07, 3.2 — faqat `N.level` bor) — shu sabab shu bitta hosila qiymat uchun jadval
 * bu yerda deterministik takrorlanadi (chegaraviy qiymatlar `PersonalityRadar.test.tsx`da
 * sinaladi). Alohida faylda — `react-refresh/only-export-components` komponent bo'lmagan
 * eksportni komponent faylida taqiqlaydi.
 */
export function classifyBigFiveLevel(pct: number, t: (key: string) => string): string {
  if (pct <= 20) return t('widgets.personalityRadar.level.veryLow');
  if (pct <= 40) return t('widgets.personalityRadar.level.low');
  if (pct <= 60) return t('widgets.personalityRadar.level.medium');
  if (pct <= 80) return t('widgets.personalityRadar.level.high');
  return t('widgets.personalityRadar.level.veryHigh');
}
