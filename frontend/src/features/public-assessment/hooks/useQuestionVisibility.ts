import { useCallback, useEffect, useRef } from 'react';

/**
 * `durationMs` uchun yuqori chegara — QA topilmasi (P21): agar savol ochiq turgan holda
 * o'quvchi uzoq tanaffus qilsa (telefonni qo'yib ketsa), viewport'ga kirgan vaqtdan javob
 * berilgan vaqtgacha bo'lgan farq cheksiz o'sib ketaveradi. Hozircha `reliabilityScore` faqat
 * `< 900ms` "juda tez javob" signalini ishlatadi (docs/03), shu sabab bu bugun ball buzmaydi,
 * lekin kelajakda "o'rtacha javob vaqti" kabi tahlil qo'shilsa, chegarasiz qiymatlar uni
 * ma'nosiz qilib qo'yardi. 10 daqiqa — bitta savolga sarflanadigan oqilona maksimal vaqt.
 */
export const MAX_DURATION_MS = 10 * 60 * 1000;

export interface UseQuestionVisibilityResult {
  /** `LikertQuestion`ning konteyner elementiga (`data-question-id` bilan) beriladigan `ref`. */
  registerNode: (node: HTMLElement | null) => void;
  /** Savol birinchi marta ko'ringan `performance.now()` vaqti; hali kuzatilmagan bo'lsa `fallback`. */
  getVisibleSince: (questionId: string, fallback: number) => number;
  /**
   * `durationMs` — savol ko'ringan vaqtdan HOZIRGA qadar (CLAUDE.md MAXSUS DIQQAT 5-band).
   * `performance.now()` chaqiruvi ataylab shu (allaqachon `useCallback` bilan xavfsiz chegara
   * hisoblangan) funksiya ichida — chaqiruvchi tomonda (sahifa komponenti) to'g'ridan-to'g'ri
   * chaqirilsa React Compiler buni "render vaqtida impure chaqiruv" deb belgilaydi.
   */
  getDurationSince: (questionId: string, fallback: number) => number;
  /** Yangi sahifa (10 ta savol) yuklanganda eski vaqt belgilarini tozalaydi. */
  reset: () => void;
}

/**
 * Har savol birinchi marta viewport'ga kirgan vaqtni saqlaydi — `durationMs` shu vaqtdan javob
 * berilgan vaqtgacha hisoblanadi, sahifa yuklangan vaqtdan EMAS (docs/10, 4.3-bo'lim; CLAUDE.md
 * MAXSUS DIQQAT 5-band — noto'g'ri o'lchansa backend ishonchlilik indeksi buziladi).
 *
 * `IntersectionObserver` mavjud bo'lmagan muhitda (masalan ba'zi eski test/render muhitlari)
 * elementga birinchi ulanish payti fallback sifatida ishlatiladi — bu faqat ehtiyot chorasi,
 * haqiqiy brauzerlarda har doim mavjud.
 */
export function useQuestionVisibility(): UseQuestionVisibilityResult {
  const timestampsRef = useRef<Map<string, number>>(new Map());
  const observerRef = useRef<IntersectionObserver | null>(null);
  const nodesRef = useRef<Map<string, HTMLElement>>(new Map());

  useEffect(() => {
    if (typeof IntersectionObserver === 'undefined') {
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;
          const id = (entry.target as HTMLElement).dataset['questionId'];
          if (id && !timestampsRef.current.has(id)) {
            timestampsRef.current.set(id, performance.now());
          }
        }
      },
      { threshold: 0.5 },
    );
    observerRef.current = observer;
    for (const node of nodesRef.current.values()) {
      observer.observe(node);
    }
    return () => observer.disconnect();
  }, []);

  const registerNode = useCallback((node: HTMLElement | null) => {
    const id = node?.dataset['questionId'];
    if (!node || !id) return;
    nodesRef.current.set(id, node);
    if (observerRef.current) {
      observerRef.current.observe(node);
      return;
    }
    // `IntersectionObserver` yo'q (fallback) — ulangan payt "ko'ringan" deb hisoblanadi.
    if (!timestampsRef.current.has(id)) {
      timestampsRef.current.set(id, performance.now());
    }
  }, []);

  const getVisibleSince = useCallback((questionId: string, fallback: number) => {
    return timestampsRef.current.get(questionId) ?? fallback;
  }, []);

  const getDurationSince = useCallback((questionId: string, fallback: number) => {
    const visibleSince = timestampsRef.current.get(questionId) ?? fallback;
    const raw = Math.max(0, Math.round(performance.now() - visibleSince));
    return Math.min(raw, MAX_DURATION_MS);
  }, []);

  const reset = useCallback(() => {
    timestampsRef.current.clear();
    nodesRef.current.clear();
  }, []);

  return { registerNode, getVisibleSince, getDurationSince, reset };
}
