import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useOnline } from '@/shared/hooks/useOnline';
import { AppError } from '@/shared/api/AppError';
import { getSessionToken } from '@/shared/api/sessionToken';
import { saveAnswers, sendAnswersKeepalive } from '../api/answersApi';
import {
  answersForTest,
  filterVisibleAnswers,
  markSent,
  pendingByTestCode,
  readAnswerStore,
  toWireItem,
  upsertAnswer,
  writeAnswerStore,
  type AnswerPayload,
  type AnswerStore,
  type StoredAnswer,
} from '../lib/answerQueue';

export type AutosaveStatus = 'saved' | 'saving';

const DEBOUNCE_MS = 1500;
const PERIODIC_FLUSH_MS = 10_000;

export interface UseAutosaveOptions {
  /** Joriy test bloki kodi (masalan `BIG5`) — yangi javoblar shu kod bilan navbatga qo'yiladi. */
  testCode: string;
  /** `410 SESSION_EXPIRED` kelganda chaqiriladi — chaqiruvchi tomon landing'ga qaytaradi. */
  onSessionExpired?: () => void;
  /**
   * `docs/18` §6.2 — HOZIR ko'rinadigan savol ID'lari. Berilsa, shu to'plamdan TASHQARIDAGI
   * (bo'lim/savol shartiga ko'ra yashirilgan) yozuvlar yuborilmaydi va "saqlanmoqda" holatiga
   * hisobga olinmaydi — aks holda backend `400 QUESTION_NOT_VISIBLE` qaytarardi va
   * "Saqlanmoqda…" indikatori abadiy osilib qolardi. `undefined` — cheklovsiz (bo'limsiz oqim,
   * mavjud xatti-harakat).
   */
  visibleQuestionIds?: ReadonlySet<string>;
}

export interface UseAutosaveResult {
  /** Joriy testga tegishli mahalliy javoblar (`questionId -> {value|text|selectedValues}`) — server javobidan ustun. */
  localAnswers: Record<string, AnswerPayload>;
  /** "Saqlandi ✓" / "Saqlanmoqda…" indikatori uchun (docs/11 E-3). Offline holati alohida — `useOnline()`. */
  status: AutosaveStatus;
  /** Javobni darhol mahalliy keshga yozadi (UI hech qachon kutmaydi) va yuborishni navbatga qo'yadi. */
  setAnswer: (questionId: string, payload: AnswerPayload, durationMs: number) => void;
  /**
   * Navbatni darhol yuborishga urinadi (debounce/interval'ni kutmasdan) va **yuborish
   * YAKUNLANGANDA** hal bo'ladigan promise qaytaradi:
   *
   * - `true` — chaqirilgan paytda navbatda bo'lgan BARCHA (ko'rinadigan) javoblar serverga
   *   yetib bordi (yoki navbat allaqachon bo'sh edi);
   * - `false` — yetib bormadi (tarmoq xatosi, oflayn, `410`). Javoblar `pending: true`
   *   holida navbatda qoladi va keyingi urinishda qayta yuboriladi — YO'QOLMAYDI.
   *
   * Chaqiruvchi shu natijaga qarab davom etadi: `POST .../complete` faqat `true` bo'lganda
   * yuboriladi (aks holda backend `400 VALIDATION_ERROR unansweredCount` qaytaradi — P30-2).
   * Ketma-ket chaqiruvlar zanjirlanadi: agar oldingi yuborish hali tugamagan bo'lsa, yangi
   * chaqiruv uni kutadi va shundan keyin qolgan navbatni yuboradi.
   */
  flush: () => Promise<boolean>;
}

/**
 * Autosave — docs/10-frontend-arxitektura.md 4.2-bo'lim, CLAUDE.md MAXSUS DIQQAT 1-band,
 * `docs/18` §4.2/§6.2 (tarmoqlanuvchi so'rovnoma — `value`/`text`/`selectedValues` shakli va
 * yashirilgan savol javobini yubormaslik).
 *
 * - Javob darhol mahalliy keshga (`answerQueue.ts`, `localStorage`) yoziladi — UI hech qachon
 *   tarmoqni kutmaydi.
 * - Yuborish: debounce 1.5s (har `setAnswer`dan keyin qayta boshlanadi) + har 10s + `flush()`
 *   qo'lda chaqirilganda (masalan sahifa/test almashganda) + oflayndan onlaynga o'tganda.
 * - Xato (tarmoq, 429, 5xx) — yozuv navbatda (`pending: true`) qoladi, foydalanuvchi
 *   bloklanmaydi, keyingi urinishda avtomatik qayta yuboriladi.
 * - `410 SESSION_EXPIRED` — navbat bilan urinish to'xtaydi, `onSessionExpired` chaqiriladi.
 * - `beforeunload` va `visibilitychange` (→ `hidden`, mobilda `beforeunload`dan ishonchliroq) —
 *   `fetch(..., { keepalive: true })` bilan oxirgi (hali yuborilmagan, ko'rinadigan) paket
 *   yuboriladi.
 * - `visibleQuestionIds` berilgan bo'lsa, undan TASHQARIDAGI yozuvlar (hozir yashirilgan
 *   bo'lim/savol) hech qachon yuborilmaydi — lekin navbatdan o'chirilmaydi ham: qayta
 *   ko'rinsa (o'quvchi javobini qaytadan tanlasa) keyingi flush ularni yuboradi.
 *
 * Eslatma (React Compiler purity): render vaqtida hisoblanadigan qiymatlar (`localAnswers`,
 * `status`) haqiqiy `useState`dan olinadi (render davomida `ref.current` o'qish/yozish
 * taqiqlangan). `storeRef` — navbatning HAQIQIY manbai; u faqat effekt/callback'lar ichida
 * (`updateStore`, interval, `beforeunload`/`visibilitychange`, `flush`) o'qiladi va yoziladi —
 * bular render EMAS, shu sabab xavfsiz. `store` holati esa uning render uchun ko'zgusi.
 * (Ilgari `storeRef` alohida effekt orqali sinxronlanardi — bu bir render kechikish berardi va
 * `flush()` eng oxirgi javobni ko'rmasligi mumkin edi.)
 */
export function useAutosave({
  testCode,
  onSessionExpired,
  visibleQuestionIds,
}: UseAutosaveOptions): UseAutosaveResult {
  const isOnline = useOnline();
  const [store, setStore] = useState<AnswerStore>(() => readAnswerStore());
  const storeRef = useRef<AnswerStore>(store);
  const [isSending, setIsSending] = useState(false);
  const debounceRef = useRef<number | null>(null);
  const onSessionExpiredRef = useRef(onSessionExpired);
  const visibleQuestionIdsRef = useRef(visibleQuestionIds);

  useEffect(() => {
    onSessionExpiredRef.current = onSessionExpired;
  }, [onSessionExpired]);

  useEffect(() => {
    visibleQuestionIdsRef.current = visibleQuestionIds;
  }, [visibleQuestionIds]);

  const updateStore = useCallback((updater: (prev: AnswerStore) => AnswerStore) => {
    // `setStore` funksional yangilagichi EMAS: navbat `storeRef`da sinxron yangilanishi shart,
    // aks holda o'sha hodisadan keyin darhol chaqirilgan `flush()` eski navbatni ko'radi.
    const next = updater(storeRef.current);
    storeRef.current = next;
    writeAnswerStore(next);
    setStore(next);
  }, []);

  /** Bitta test bloki navbatini yuboradi. `true` — YUBORILISHI KERAK BO'LGANLARNING hammasi serverga yetib bordi. */
  const flushGroup = useCallback(
    async (groupTestCode: string, items: readonly StoredAnswer[]): Promise<boolean> => {
      const sendable =
        groupTestCode === testCode
          ? filterVisibleAnswers(items, visibleQuestionIdsRef.current)
          : items;
      if (sendable.length === 0) return true;
      try {
        await saveAnswers(groupTestCode, sendable.map(toWireItem));
        updateStore((prev) => markSent(prev, sendable.map((item) => item.questionId)));
        return true;
      } catch (error) {
        if (error instanceof AppError && error.status === 410) {
          onSessionExpiredRef.current?.();
          return false;
        }
        // Boshqa xatolar — yozuv `pending: true` holida navbatda qoladi, keyingi
        // debounce/interval/online chaqiruvida qayta uriniladi (foydalanuvchi bloklanmaydi).
        return false;
      }
    },
    [testCode, updateStore],
  );

  /** Bitta yuborish sikli — chaqirilgan paytdagi butun navbatni yuboradi. Hech qachon `throw` qilmaydi. */
  const runFlush = useCallback(async (): Promise<boolean> => {
    const entries = Object.entries(pendingByTestCode(storeRef.current));
    if (entries.length === 0) return true; // navbat bo'sh — hammasi allaqachon saqlangan
    if (!navigator.onLine) return false; // oflayn — navbat joyida qoladi, `online` hodisasida yuboriladi
    setIsSending(true);
    try {
      const results = await Promise.all(entries.map(([code, items]) => flushGroup(code, items)));
      return results.every(Boolean);
    } finally {
      setIsSending(false);
    }
  }, [flushGroup]);

  // Yuborishlar ZANJIRI: bir vaqtda faqat bitta sikl ishlaydi (ilgari `sendingRef` bilan
  // ikkinchi chaqiruv shunchaki TASHLAB YUBORILARDI — chaqiruvchi "yuborildi" deb o'ylardi,
  // aslida oxirgi javoblar navbatda qolardi). Endi ikkinchi chaqiruv birinchisini KUTADI va
  // shundan keyin qolgan navbatni (shu jumladan orada qo'shilgan yangi javoblarni) yuboradi.
  const flushChainRef = useRef<Promise<void>>(Promise.resolve());

  const flush = useCallback((): Promise<boolean> => {
    const result = flushChainRef.current.then(runFlush);
    flushChainRef.current = result.then(
      () => undefined,
      () => undefined,
    );
    return result;
  }, [runFlush]);

  const setAnswer = useCallback(
    (questionId: string, payload: AnswerPayload, durationMs: number) => {
      updateStore((prev) => upsertAnswer(prev, { testCode, questionId, durationMs, ...payload }));
      if (debounceRef.current !== null) {
        window.clearTimeout(debounceRef.current);
      }
      debounceRef.current = window.setTimeout(() => {
        debounceRef.current = null;
        void flush();
      }, DEBOUNCE_MS);
    },
    [testCode, updateStore, flush],
  );

  // Har 10 soniyada — docs/10, 4.2-bo'lim.
  useEffect(() => {
    const interval = window.setInterval(() => {
      void flush();
    }, PERIODIC_FLUSH_MS);
    return () => window.clearInterval(interval);
  }, [flush]);

  // Sahifa/test almashganda va unmount bo'lganda — qolgan navbat darhol yuboriladi.
  useEffect(() => {
    return () => {
      if (debounceRef.current !== null) {
        window.clearTimeout(debounceRef.current);
      }
      void flush();
    };
  }, [flush]);

  // Oflayndan onlaynga o'tganda avtomatik yuborish (docs/10, 4.2-bo'lim).
  const wasOnlineRef = useRef(isOnline);
  useEffect(() => {
    if (!wasOnlineRef.current && isOnline) {
      void flush();
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
        const sendable =
          code === testCode ? filterVisibleAnswers(items, visibleQuestionIdsRef.current) : items;
        sendAnswersKeepalive(code, sendable.map(toWireItem), token);
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
  }, [testCode]);

  const localAnswers = useMemo(() => answersForTest(store, testCode), [store, testCode]);
  const status: AutosaveStatus = useMemo(() => {
    const hasPending = Object.values(store).some((entry) => {
      if (!entry.pending) return false;
      if (entry.testCode === testCode && visibleQuestionIds && !visibleQuestionIds.has(entry.questionId)) {
        return false; // hozir yashirin — yuborilmaydi, "saqlanmoqda" holatini abadiy bloklamasin
      }
      return true;
    });
    return isSending || hasPending ? 'saving' : 'saved';
  }, [store, isSending, testCode, visibleQuestionIds]);

  return { localAnswers, status, setAnswer, flush };
}
