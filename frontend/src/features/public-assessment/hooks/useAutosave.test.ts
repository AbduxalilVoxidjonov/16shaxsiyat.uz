import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AppError } from '@/shared/api/AppError';
import { readAnswerStore } from '../lib/answerQueue';
import { useAutosave } from './useAutosave';

const { saveAnswersMock, sendAnswersKeepaliveMock, getSessionTokenMock } = vi.hoisted(() => ({
  saveAnswersMock: vi.fn(),
  sendAnswersKeepaliveMock: vi.fn(),
  getSessionTokenMock: vi.fn((): string | null => 'sess-token-1'),
}));

vi.mock('../api/answersApi', () => ({
  saveAnswers: saveAnswersMock,
  sendAnswersKeepalive: sendAnswersKeepaliveMock,
}));

vi.mock('@/shared/api/sessionToken', () => ({
  getSessionToken: getSessionTokenMock,
}));

function setOnline(value: boolean) {
  Object.defineProperty(window.navigator, 'onLine', { configurable: true, value });
}

describe('useAutosave', () => {
  beforeEach(() => {
    localStorage.clear();
    setOnline(true);
    saveAnswersMock.mockReset().mockResolvedValue({ savedCount: 1, answered: 1, total: 10 });
    sendAnswersKeepaliveMock.mockReset();
    getSessionTokenMock.mockReset().mockReturnValue('sess-token-1');
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
    localStorage.clear();
    setOnline(true);
  });

  it("setAnswer chaqirilgandan so'ng qiymat DARHOL localStorage'ga yoziladi (tarmoqni kutmasdan)", () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 1200);
    });

    // Hali tarmoqqa yuborilmagan (debounce hali tugamagan), lekin mahalliy keshda bor.
    expect(saveAnswersMock).not.toHaveBeenCalled();
    expect(readAnswerStore()).toMatchObject({ q1: { value: 4, durationMs: 1200, pending: true } });
    expect(result.current.localAnswers).toEqual({ q1: { value: 4 } });
    expect(result.current.status).toBe('saving');
  });

  it('1.5s debounce tugagach navbatni yuboradi', async () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 1200);
    });

    await act(async () => {
      vi.advanceTimersByTime(1499);
    });
    expect(saveAnswersMock).not.toHaveBeenCalled();

    await act(async () => {
      vi.advanceTimersByTime(1);
    });

    expect(saveAnswersMock).toHaveBeenCalledWith('BIG5', [
      { questionId: 'q1', value: 4, durationMs: 1200 },
    ]);
    expect(result.current.status).toBe('saved');
  });

  it("ketma-ket setAnswer chaqiruvlari debounce'ni qayta boshlaydi — faqat bitta so'rov, oxirgi qiymat bilan", async () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 2 }, 100);
    });
    await act(async () => {
      vi.advanceTimersByTime(1000);
    });
    act(() => {
      result.current.setAnswer('q1', { value: 5 }, 300); // 1000ms ichida qayta tahrirlandi — taymer qayta boshlanadi
    });
    await act(async () => {
      vi.advanceTimersByTime(1000);
    });
    expect(saveAnswersMock).not.toHaveBeenCalled(); // jami 2000ms o'tdi, lekin oxirgi debounce hali 1500ms to'lmagan

    await act(async () => {
      vi.advanceTimersByTime(500);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
    expect(saveAnswersMock).toHaveBeenCalledWith('BIG5', [
      { questionId: 'q1', value: 5, durationMs: 300 },
    ]);
  });

  it("har 10s'da navbatni avtomatik yuboradi (debounce'ni kutmasdan)", async () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 3 }, 50);
    });
    // Debounce (1.5s) allaqachon yuborgan bo'lishi mumkin — buni oldini olish uchun
    // saveAnswers darhol muvaffaqiyatsiz bo'lsin, keyin faqat interval orqali qayta urinsin.
    saveAnswersMock.mockRejectedValueOnce(new Error('network'));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1500);
    });
    expect(saveAnswersMock).toHaveBeenCalledTimes(1);

    saveAnswersMock.mockResolvedValueOnce({ savedCount: 1, answered: 1, total: 10 });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(2);
    expect(result.current.status).toBe('saved');
  });

  it("tarmoq xatosida (masalan reject) yozuv navbatda qoladi va foydalanuvchi bloklanmaydi — keyingi urinishda qayta yuboriladi", async () => {
    saveAnswersMock.mockRejectedValueOnce(new Error('network down'));
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 1 }, 10);
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1500);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
    // Xato bo'lsa ham UI/holat bloklanmaydi — javob hali mahalliy keshda, "saving" holatida.
    expect(result.current.localAnswers).toEqual({ q1: { value: 1 } });
    expect(result.current.status).toBe('saving');
    expect(readAnswerStore()['q1']?.pending).toBe(true);

    saveAnswersMock.mockResolvedValueOnce({ savedCount: 1, answered: 1, total: 10 });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(2);
    expect(result.current.status).toBe('saved');
  });

  it("oflayn holatda yuborishga urinmaydi, navbat to'planadi", async () => {
    setOnline(false);
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 10);
      result.current.setAnswer('q2', { value: 2 }, 20);
    });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(15_000); // debounce + interval — ikkalasi ham o'tadi
    });

    expect(saveAnswersMock).not.toHaveBeenCalled();
    expect(result.current.status).toBe('saving');
    expect(Object.keys(readAnswerStore())).toEqual(['q1', 'q2']);
  });

  it("oflayndan onlaynga o'tganda navbat avtomatik yuboriladi", async () => {
    setOnline(false);
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 10);
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(2000);
    });
    expect(saveAnswersMock).not.toHaveBeenCalled();

    await act(async () => {
      setOnline(true);
      window.dispatchEvent(new Event('online'));
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(saveAnswersMock).toHaveBeenCalledWith('BIG5', [
      { questionId: 'q1', value: 4, durationMs: 10 },
    ]);
  });

  it("410 SESSION_EXPIRED kelsa onSessionExpired chaqiriladi va yozuv qayta-qayta urinilmaydi", async () => {
    saveAnswersMock.mockRejectedValueOnce(
      new AppError({ code: 'SESSION_EXPIRED', message: 'tugagan', status: 410 }),
    );
    const onSessionExpired = vi.fn();
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5', onSessionExpired }));

    act(() => {
      result.current.setAnswer('q1', { value: 1 }, 10);
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1500);
    });

    expect(onSessionExpired).toHaveBeenCalledTimes(1);
    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
  });

  it("flush() darhol chaqirishga urinadi (masalan sahifa almashganda) — debounce'ni kutmasdan", async () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 3 }, 5);
    });
    await act(async () => {
      void result.current.flush();
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
  });

  // P30-2 poygasi: `TestPage` oxirgi sahifada `POST .../complete` ni AYNAN shu promise hal
  // bo'lgandan keyin yuboradi. Promise so'rov tugashidan oldin hal bo'lsa, poyga qaytadi.
  it("flush() qaytargan promise faqat so'rov TUGAGANDA hal bo'ladi (true)", async () => {
    let release: ((value: unknown) => void) | undefined;
    saveAnswersMock.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          release = resolve;
        }),
    );
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 3 }, 5);
    });

    let settled: boolean | 'pending' = 'pending';
    let flushPromise!: Promise<boolean>;
    act(() => {
      flushPromise = result.current.flush();
    });
    void flushPromise.then((ok) => {
      settled = ok;
    });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(0);
    });
    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
    expect(settled).toBe('pending'); // so'rov hali tugamagan — chaqiruvchi KUTISHI shart

    await act(async () => {
      release?.({ savedCount: 1, answered: 1, total: 10 });
      await flushPromise;
    });

    expect(settled).toBe(true);
    expect(readAnswerStore()['q1']?.pending).toBe(false);
  });

  it("flush() yuborish yiqilsa false qaytaradi va javob navbatda QOLADI (yo'qolmaydi)", async () => {
    saveAnswersMock.mockRejectedValueOnce(new Error('network down'));
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 2 }, 7);
    });

    let ok: boolean | undefined;
    await act(async () => {
      ok = await result.current.flush();
    });

    expect(ok).toBe(false);
    expect(readAnswerStore()['q1']).toMatchObject({ value: 2, pending: true });
    expect(result.current.localAnswers).toEqual({ q1: { value: 2 } });
  });

  it('oflaynda flush() false qaytaradi va tarmoqqa umuman urinmaydi', async () => {
    setOnline(false);
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 10);
    });

    let ok: boolean | undefined;
    await act(async () => {
      ok = await result.current.flush();
    });

    expect(ok).toBe(false);
    expect(saveAnswersMock).not.toHaveBeenCalled();
    expect(readAnswerStore()['q1']?.pending).toBe(true);
  });

  it("navbat bo'sh bo'lsa flush() so'rovsiz darhol true qaytaradi", async () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    let ok: boolean | undefined;
    await act(async () => {
      ok = await result.current.flush();
    });

    expect(ok).toBe(true);
    expect(saveAnswersMock).not.toHaveBeenCalled();
  });

  it('ketma-ket flush() chaqiruvlari ZANJIRLANADI — ikkinchisi birinchisini kutadi va orada qo\'shilgan javobni ham yuboradi', async () => {
    let release: (() => void) | undefined;
    saveAnswersMock.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          release = () => resolve({ savedCount: 1, answered: 1, total: 10 });
        }),
    );
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 3 }, 5);
    });
    let first!: Promise<boolean>;
    act(() => {
      first = result.current.flush();
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(0); // birinchi so'rov yo'lga chiqdi (va "havoda" qoldi)
    });
    expect(saveAnswersMock).toHaveBeenCalledTimes(1);

    // Birinchi so'rov hali "havoda" — shu paytda yangi javob qo'shiladi va yana flush.
    act(() => {
      result.current.setAnswer('q2', { value: 4 }, 6);
    });
    let secondDone = false;
    let second!: Promise<boolean>;
    act(() => {
      second = result.current.flush();
    });
    void second.then(() => {
      secondDone = true;
    });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(0);
    });
    expect(saveAnswersMock).toHaveBeenCalledTimes(1); // ikkinchi sikl birinchisini kutmoqda
    expect(secondDone).toBe(false);

    await act(async () => {
      release?.();
      await second;
    });

    expect(await first).toBe(true);
    expect(await second).toBe(true);
    expect(saveAnswersMock).toHaveBeenCalledTimes(2);
    expect(saveAnswersMock).toHaveBeenLastCalledWith('BIG5', [
      { questionId: 'q2', value: 4, durationMs: 6 },
    ]);
    expect(readAnswerStore()['q2']?.pending).toBe(false);
  });

  it("unmount bo'lganda qolgan navbatni yuborishga urinadi", async () => {
    const { result, unmount } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 3 }, 5);
    });

    await act(async () => {
      unmount();
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
  });

  it("beforeunload'da sendAnswersKeepalive (fetch keepalive) orqali navbatdagi javoblarni yuboradi", () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 10);
    });

    window.dispatchEvent(new Event('beforeunload'));

    expect(sendAnswersKeepaliveMock).toHaveBeenCalledWith(
      'BIG5',
      [{ questionId: 'q1', value: 4, durationMs: 10 }],
      'sess-token-1',
    );
  });

  it("sahifa fonga o'tganda (visibilitychange → hidden) ham navbatni yuboradi — mobilda beforeunload'dan ishonchliroq", () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 10);
    });

    Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'hidden' });
    document.dispatchEvent(new Event('visibilitychange'));

    expect(sendAnswersKeepaliveMock).toHaveBeenCalledWith(
      'BIG5',
      [{ questionId: 'q1', value: 4, durationMs: 10 }],
      'sess-token-1',
    );
  });

  it("sessiya tokeni yo'q bo'lsa beforeunload'da sendAnswersKeepalive chaqirilmaydi", () => {
    getSessionTokenMock.mockReturnValue(null);

    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));
    act(() => {
      result.current.setAnswer('q1', { value: 4 }, 10);
    });

    window.dispatchEvent(new Event('beforeunload'));

    expect(sendAnswersKeepaliveMock).not.toHaveBeenCalled();
  });

  // docs/18 §4.2 — matn/ko'p-tanlov javob shakli.
  describe('docs/18 §4.2 — text/selectedValues javob shakli', () => {
    it("setAnswer({ text }) — wire elementida FAQAT text (value/selectedValues yo'q)", async () => {
      const { result } = renderHook(() => useAutosave({ testCode: 'SURVEY' }));

      act(() => {
        result.current.setAnswer('q1', { text: 'Karimov Ali' }, 800);
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(1500);
      });

      expect(saveAnswersMock).toHaveBeenCalledWith('SURVEY', [
        { questionId: 'q1', text: 'Karimov Ali', durationMs: 800 },
      ]);
      expect(result.current.localAnswers).toEqual({ q1: { text: 'Karimov Ali' } });
    });

    it("setAnswer({ selectedValues }) — wire elementida FAQAT selectedValues", async () => {
      const { result } = renderHook(() => useAutosave({ testCode: 'SURVEY' }));

      act(() => {
        result.current.setAnswer('q1', { selectedValues: [1, 3] }, 50);
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(1500);
      });

      expect(saveAnswersMock).toHaveBeenCalledWith('SURVEY', [
        { questionId: 'q1', selectedValues: [1, 3], durationMs: 50 },
      ]);
    });
  });

  // docs/18 §6.2 — "yashirilgan savolning mahalliy javobi yuborilmaydi — autosave navbatidan chiqariladi".
  describe('docs/18 §6.2 — visibleQuestionIds', () => {
    it("visibleQuestionIds berilgan bo'lsa, undan tashqaridagi savol flush()da YUBORILMAYDI", async () => {
      const { result } = renderHook(() =>
        useAutosave({ testCode: 'SURVEY', visibleQuestionIds: new Set(['q1']) }),
      );

      act(() => {
        result.current.setAnswer('q1', { value: 1 }, 10); // ko'rinadi
        result.current.setAnswer('q2', { value: 2 }, 20); // yashirin (2-A/2-B tarmog'i)
      });

      let ok: boolean | undefined;
      await act(async () => {
        ok = await result.current.flush();
      });

      // Faqat ko'rinadigan savol yuborildi — backend `QUESTION_NOT_VISIBLE` (400) qaytarmaydi.
      expect(saveAnswersMock).toHaveBeenCalledTimes(1);
      expect(saveAnswersMock).toHaveBeenCalledWith('SURVEY', [
        { questionId: 'q1', value: 1, durationMs: 10 },
      ]);
      // Yashirin javob hali ham mahalliy keshda — yo'qolmagan, faqat yuborilmagan.
      expect(readAnswerStore()['q2']).toMatchObject({ value: 2, pending: true });
      // Yuborilishi KERAK bo'lganlar (ko'rinadiganlar) muvaffaqiyatli bo'lgani uchun `true`.
      expect(ok).toBe(true);
    });

    it("yashirin savolning pending javobi 'Saqlanmoqda…' holatini abadiy osiltirmaydi", () => {
      const { result } = renderHook(() =>
        useAutosave({ testCode: 'SURVEY', visibleQuestionIds: new Set(['q1']) }),
      );

      act(() => {
        result.current.setAnswer('q2', { value: 2 }, 20); // yashirin, hech qachon yuborilmaydi
      });

      // `q1` hali javobsiz, faqat yashirin `q2` navbatda — status "saqlandi" ko'rsatishi kerak.
      expect(result.current.status).toBe('saved');
    });

    it("bo'lim qayta ko'ringanda (visibleQuestionIds kengaytirilganda) avval yashirilgan javob YUBORILADI", async () => {
      const { result, rerender } = renderHook(
        ({ visibleQuestionIds }: { visibleQuestionIds: ReadonlySet<string> }) =>
          useAutosave({ testCode: 'SURVEY', visibleQuestionIds }),
        { initialProps: { visibleQuestionIds: new Set(['q1']) } },
      );

      act(() => {
        result.current.setAnswer('q2', { value: 2 }, 20); // hozircha yashirin
      });
      await act(async () => {
        await result.current.flush();
      });
      expect(saveAnswersMock).not.toHaveBeenCalled();

      rerender({ visibleQuestionIds: new Set(['q1', 'q2']) }); // q2 endi ko'rinadi

      await act(async () => {
        await result.current.flush();
      });

      expect(saveAnswersMock).toHaveBeenCalledWith('SURVEY', [
        { questionId: 'q2', value: 2, durationMs: 20 },
      ]);
    });

    it("visibleQuestionIds berilmasa (bo'limsiz oqim) hech narsa filtrlanmaydi — mavjud xatti-harakat", async () => {
      const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

      act(() => {
        result.current.setAnswer('q1', { value: 4 }, 10);
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(1500);
      });

      expect(saveAnswersMock).toHaveBeenCalledWith('BIG5', [
        { questionId: 'q1', value: 4, durationMs: 10 },
      ]);
    });
  });
});
