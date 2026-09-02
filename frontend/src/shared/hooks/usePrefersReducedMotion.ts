import { useEffect, useState } from 'react';

const QUERY = '(prefers-reduced-motion: reduce)';

function readPreference(): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return false;
  return window.matchMedia(QUERY).matches;
}

/**
 * `prefers-reduced-motion` holatini kuzatadi — docs/11-ux-va-ekranlar.md, 4-bo'lim:
 * "`prefers-reduced-motion` — animatsiyalar o'chadi". Diagramma widget'lari (`widgets/`)
 * shu hook orqali Recharts animatsiyasini (`isAnimationActive`) va CSS `transition`larni
 * shartli o'chiradi. `matchMedia` ba'zi test muhitlarida (jsdom, `MediaQueryList.
 * addEventListener` yo'q eski polyfill'larda) to'liq emas — shu sabab har ikkala eski
 * (`addListener`) va yangi (`addEventListener`) API himoyalangan holda ishlatiladi.
 */
export function usePrefersReducedMotion(): boolean {
  const [prefersReduced, setPrefersReduced] = useState(readPreference);

  useEffect(() => {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;
    const mediaQueryList = window.matchMedia(QUERY);
    const handleChange = () => {
      setPrefersReduced(mediaQueryList.matches);
    };
    handleChange();

    if (typeof mediaQueryList.addEventListener === 'function') {
      mediaQueryList.addEventListener('change', handleChange);
      return () => {
        mediaQueryList.removeEventListener('change', handleChange);
      };
    }
    // Eski Safari (`addListener`/`removeListener`) — turlar `MediaQueryList`da ixtiyoriy.
    const legacy = mediaQueryList as unknown as {
      addListener?: (cb: () => void) => void;
      removeListener?: (cb: () => void) => void;
    };
    legacy.addListener?.(handleChange);
    return () => {
      legacy.removeListener?.(handleChange);
    };
  }, []);

  return prefersReduced;
}
