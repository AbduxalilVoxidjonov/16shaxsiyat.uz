import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useOnline } from '@/shared/hooks/useOnline';
import { AppError } from '@/shared/api/AppError';
import { getSessionToken } from '@/shared/api/sessionToken';
import { saveAnswers, sendAnswersKeepalive } from '../api/answersApi';
import {
  markSent,
  pendingByTestCode,
  readAnswerStore,
  upsertAnswer,
  valuesForTest,
  writeAnswerStore,
  type AnswerStore,
} from '../lib/answerQueue';

export type AutosaveStatus = 'saved' | 'saving';

const DEBOUNCE_MS = 1500;
const PERIODIC_FLUSH_MS = 10_000;

export interface UseAutosaveOptions {
  /** Joriy test bloki kodi (masalan `BIG5`) — yangi javoblar shu kod bilan navbatga qo'yiladi. */
  testCode: string;
  /** `410 SESSION_EXPIRED` kelganda chaqiriladi — chaqiruvchi tomon landing'ga qaytaradi. */
  onSessionExpired?: () => void;
}

export interface UseAutosaveResult {
  /** Joriy testga tegishli mahalliy qiymatlar (`questionId -> value`) — server javobidan ustun. */
  localValues: Record<string, number>;
  /** "Saqlandi ✓" / "Saqlanmoqda…" indikatori uchun (docs/11 E-3). Offline holati alohida — `useOnline()`. */
  status: AutosaveStatus;
  /** Javobni darhol mahalliy keshga yozadi (UI hech qachon kutmaydi) va yuborishni navbatga qo'yadi. */
  setAnswer: (questionId: string, value: number, durationMs: number) => void;
  /** Darhol yuborishga urinadi (debounce/interval'ni kutmasdan) — natijani kutmaydi, xato bloklamaydi. */
  flush: () => void;
}

/**
 * Autosave — docs/10-frontend-arxitektura.md 4.2-bo'lim, CLAUDE.md MAXSUS DIQQAT 1-band.
 *
 * - Javob darhol mahalliy keshga (`answerQueue.ts`, `localStorage`) yoziladi — UI hech qachon
 *   tarmoqni kutmaydi.
 * - Yuborish: debounce 1.5s (har `setAnswer`dan keyin qayta boshlanadi) + har 10s + `flush()`
 *   qo'lda chaqirilganda (masalan sahifa/test almashganda) + oflayndan onlaynga o'tganda.
 * - Xato (tarmoq, 429, 5xx) — yozuv navbatda (`pending: true`) qoladi, foydalanuvchi
 *   bloklanmaydi, keyingi urinishda avtomatik qayta yuboriladi.
 * - `410 SESSION_EXPIRED` — navbat bilan urinish to'xtaydi, `onSessionExpired` chaqiriladi.
 * - `beforeunload` va `visibilitychange` (→ `hidden`, mobilda `beforeunload`dan ishonchliroq) —
 *   `fetch(..., { keepalive: true })` bilan oxirgi (hali yuborilmagan) paket yuboriladi.
 *
 * Eslatma (React Compiler purity): render vaqtida hisoblanadigan qiymatlar (`localValues`,
 * `status`) haqiqiy `useState`dan olinadi (render davomida `ref.current` o'qish/yozish
 * taqiqlangan). `storeRef` faqat effekt/callback'lar ichida (interval, `beforeunload`/
 * `visibilitychange`, `flush`) ishlatiladi — bular render EMAS, shu sabab xavfsiz; u alohida
 * effekt orqali `store` holatiga sinxronlanadi.
 */
export function useAutosave({ testCode, onSessionExpired }: UseAutosaveOptions): UseAutosaveResult {
  const isOnline = useOnline();
  const [store, setStore] = useState<AnswerStore>(() => readAnswerStore());
  const storeRef = useRef<AnswerStore>(store);
  const sendingRef = useRef(false);
  const [isSending, setIsSending] = useState(false);
  const debounceRef = useRef<number | null>(null);
  const onSessionExpiredRef = useRef(onSessionExpired);

  useEffect(() => {
    storeRef.current = store;
  }, [store]);

  useEffect(() => {
    onSessionExpiredRef.current = onSessionExpired;
  }, [onSessionExpired]);

  const updateStore = useCallback((updater: (prev: AnswerStore) => AnswerStore) => {
    setStore((prev) => {
      const next = updater(prev);
      writeAnswerStore(next);
      return next;
    });
  }, []);

  const flushGroup = useCallback(
    async (groupTestCode: string, items: readonly { questionId: string; value: number; durationMs: number }[]) => {
      if (items.length === 0) return;
      try {
        // `items` — `StoredAnswer[]` (`testCode`/`pending` maydonlari bilan, `answerQueue.ts`),
        // backend shartnomasi (`SaveAnswersRequest`) esa faqat `questionId`/`value`/`durationMs`
        // kutadi — shu sabab yuborishdan oldin qisqartiriladi.
        await saveAnswers(
          groupTestCode,
          items.map(({ questionId, value, durationMs }) => ({ questionId, value, durationMs })),
        );
        updateStore((prev) =>
          markSent(
            prev,
            items.map((item) => item.questionId),
          ),
        );
      } catch (error) {
        if (error instanceof AppError && error.status === 410) {
          onSessionExpiredRef.current?.();
          return;
        }
        // Boshqa xatolar — yozuv `pending: true` holida navbatda qoladi, keyingi
        // debounce/interval/online chaqiruvida qayta uriniladi (foydalanuvchi bloklanmaydi).
      }
    },
    [updateStore],
  );

  const flush = useCallback(() => {
    if (sendingRef.current) return;
    if (!navigator.onLine) return;
    const groups = pendingByTestCode(storeRef.current);
    const entries = Object.entries(groups);
    if (entries.length === 0) return;
    sendingRef.current = true;
    setIsSending(true);
    void Promise.all(entries.map(([code, items]) => flushGroup(code, items))).finally(() => {
      sendingRef.current = false;
      setIsSending(false);
    });
  }, [flushGroup]);

  const setAnswer = useCallback(
    (questionId: string, value: number, durationMs: number) => {
      updateStore((prev) => upsertAnswer(prev, { testCode, questionId, value, durationMs }));
      if (debounceRef.current !== null) {
        window.clearTimeout(debounceRef.current);
      }
      debounceRef.current = window.setTimeout(() => {
        debounceRef.current = null;
        flush();
      }, DEBOUNCE_MS);
    },
    [testCode, updateStore, flush],
  );

  // Har 10 soniyada — docs/10, 4.2-bo'lim.
  useEffect(() => {
    const interval = window.setInterval(flush, PERIODIC_FLUSH_MS);
    return () => window.clearInterval(interval);
  }, [flush]);

  // Sahifa/test almashganda va unmount bo'lganda — qolgan navbat darhol yuboriladi.
  useEffect(() => {
    return () => {
      if (debounceRef.current !== null) {
        window.clearTimeout(debounceRef.current);
      }
      flush();
    };
  }, [flush]);

  // Oflayndan onlaynga o'tganda avtomatik yuborish (docs/10, 4.2-bo'lim).
  const wasOnlineRef = useRef(isOnline);
  useEffect(() => {
    if (!wasOnlineRef.current && isOnline) {
      flush();
    }
    wasOnlineRef.current = isOnline;
  }, [isOnline, flush]);

  // `beforeunload` + `visibilitychange` (→ `hidden`) — docs/10, 4.2-bo'lim (PM tuzatishi).
  // `navigator.sendBeacon` EMAS (maxsus header qo'ya olmaydi, `X-Session-Token`siz `401`) —
  // oddiy `fetch(..., { keepalive: true })`, u header qo'yishga ruxsat beradi va sahifa
  // yopilgandan keyin ham yakunlanadi. `visibilitychange` mobil brauzerlarda `beforeunload`dan
  // ancha ishonchliroq (ko'p mobil brauzer `beforeunload`ni umuman chaqirmaydi/kechiktiradi).
  useEffect(() => {
    function flushKeepalive() {
      const token = getSessionToken();
      if (!token) return;
      const groups = pendingByTestCode(storeRef.current);
      for (const [code, items] of Object.entries(groups)) {
        sendAnswersKeepalive(
          code,
          items.map(({ questionId, value, durationMs }) => ({ questionId, value, durationMs })),
          token,
        );
      }
    }
    function handleBeforeUnload() {
      flushKeepalive();
    }
    function handleVisibilityChange() {
      if (document.visibilityState === 'hidden') {
        flushKeepalive();
      }
    }
    window.addEventListener('beforeunload', handleBeforeUnload);
    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, []);

  const localValues = useMemo(() => valuesForTest(store, testCode), [store, testCode]);
  const status: AutosaveStatus = useMemo(() => {
    const hasPending = Object.values(store).some((entry) => entry.pending);
    return isSending || hasPending ? 'saving' : 'saved';
  }, [store, isSending]);

  return { localValues, status, setAnswer, flush };
}
