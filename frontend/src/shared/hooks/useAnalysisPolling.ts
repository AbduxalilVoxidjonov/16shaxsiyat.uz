import { useEffect, useState } from 'react';

/**
 * AI tahlil natijasini kutish (polling) — `POST …/rerun-analysis` darhol `202` qaytaradi va
 * ish FON NAVBATIDA bajariladi (`docs/09` 8-bo'lim, P18), shu sabab sahifa natijani o'zi
 * so'rab olishi kerak.
 *
 * Ikki ekran ham (`/admin/students/:id`, `/admin/assessments/:id`) shu hookdan foydalanadi.
 *
 * **Nega cheklov bor.** Cheksiz `refetchInterval` ochiq qolgan yorliqda batareyani va
 * serverni behuda yeydi: navbat tiqilib qolsa yoki fon ishi yiqilsa sessiya `Analyzing`
 * holatida ABADIY qolishi mumkin, sahifa esa har 4 soniyada so'rab turaveradi. Cheklovga
 * yetgach so'rash to'xtaydi va foydalanuvchiga tushunarli xabar ko'rsatiladi
 * (`timedOut`) — jimgina to'xtash "sahifa qotib qoldi" degan taassurot qoldiradi.
 */

/**
 * 4 s — `docs/11` A-5 "odatda 1 daqiqa vaqt ketadi" deb va'da qiladi, ya'ni kutish odatda
 * ~15 so'rov. 3 s ortiqcha yuk, 5 s dan keyin esa tahlil tayyor bo'lgani sezilarli
 * kechikish bilan ko'rinadi.
 */
export const ANALYSIS_POLL_INTERVAL_MS = 4000;

/**
 * 3 daqiqa — va'da qilingan 1 daqiqadan uch barobar ko'p. Bu vaqt ichida javob kelmasa
 * muammo navbatda/provayderda, sahifani so'ratishda emas.
 */
export const ANALYSIS_POLL_LIMIT_MS = 180_000;

export interface AnalysisPolling {
  /** TanStack Query `refetchInterval` qiymati — `false` bo'lsa so'ralmaydi. */
  refetchInterval: number | false;
  /** Cheklovga yetildi: so'rash to'xtatildi, foydalanuvchiga sabab ko'rsatiladi. */
  timedOut: boolean;
}

function documentHidden(): boolean {
  return typeof document !== 'undefined' && document.hidden;
}

/**
 * @param isAnalyzing Tahlil hozir fon navbatidami (sessiya `Analyzing` holatida).
 */
export function useAnalysisPolling(isAnalyzing: boolean): AnalysisPolling {
  const [timedOut, setTimedOut] = useState(false);
  const [wasAnalyzing, setWasAnalyzing] = useState(isAnalyzing);
  const [hidden, setHidden] = useState(documentHidden);

  // Tahlil tugadi (yoki yangisi boshlandi) — hisoblagich nolga qaytadi, keyingi tahlil
  // to'liq cheklovga ega bo'ladi. Bu RENDER paytida moslanadi (React'ning "props
  // o'zgarganda holatni moslash" naqshi), effektda emas — effektdagi `setState` kaskadli
  // qayta render keltirib chiqaradi.
  if (wasAnalyzing !== isAnalyzing) {
    setWasAnalyzing(isAnalyzing);
    if (timedOut) {
      setTimedOut(false);
    }
  }

  // Sahifa fonga o'tganda (boshqa yorliq/minimallashtirilgan oyna) so'rash to'xtaydi —
  // hech kim ko'rmayotgan ekranni yangilab turishning ma'nosi yo'q. Qaytib kelinganda
  // `visibilitychange` yana yoqadi va TanStack darhol bitta so'rov yuboradi.
  useEffect(() => {
    const handleVisibilityChange = () => {
      setHidden(document.hidden);
    };
    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, []);

  useEffect(() => {
    if (!isAnalyzing) return;
    const timer = setTimeout(() => {
      setTimedOut(true);
    }, ANALYSIS_POLL_LIMIT_MS);
    return () => {
      clearTimeout(timer);
    };
  }, [isAnalyzing]);

  return {
    refetchInterval: isAnalyzing && !timedOut && !hidden ? ANALYSIS_POLL_INTERVAL_MS : false,
    timedOut: isAnalyzing && timedOut,
  };
}
