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
      result.current.setAnswer('q1', 4, 1200);
    });

    // Hali tarmoqqa yuborilmagan (debounce hali tugamagan), lekin mahalliy keshda bor.
    expect(saveAnswersMock).not.toHaveBeenCalled();
    expect(readAnswerStore()).toMatchObject({ q1: { value: 4, durationMs: 1200, pending: true } });
    expect(result.current.localValues).toEqual({ q1: 4 });
    expect(result.current.status).toBe('saving');
  });

  it('1.5s debounce tugagach navbatni yuboradi', async () => {
    const { result } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', 4, 1200);
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
      result.current.setAnswer('q1', 2, 100);
    });
    await act(async () => {
      vi.advanceTimersByTime(1000);
    });
    act(() => {
      result.current.setAnswer('q1', 5, 300); // 1000ms ichida qayta tahrirlandi — taymer qayta boshlanadi
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
      result.current.setAnswer('q1', 3, 50);
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
      result.current.setAnswer('q1', 1, 10);
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1500);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
    // Xato bo'lsa ham UI/holat bloklanmaydi — javob hali mahalliy keshda, "saving" holatida.
    expect(result.current.localValues).toEqual({ q1: 1 });
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
      result.current.setAnswer('q1', 4, 10);
      result.current.setAnswer('q2', 2, 20);
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
      result.current.setAnswer('q1', 4, 10);
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
      result.current.setAnswer('q1', 1, 10);
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
      result.current.setAnswer('q1', 3, 5);
    });
    await act(async () => {
      result.current.flush();
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(saveAnswersMock).toHaveBeenCalledTimes(1);
  });

  it("unmount bo'lganda qolgan navbatni yuborishga urinadi", async () => {
    const { result, unmount } = renderHook(() => useAutosave({ testCode: 'BIG5' }));

    act(() => {
      result.current.setAnswer('q1', 3, 5);
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
      result.current.setAnswer('q1', 4, 10);
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
      result.current.setAnswer('q1', 4, 10);
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
      result.current.setAnswer('q1', 4, 10);
    });

    window.dispatchEvent(new Event('beforeunload'));

    expect(sendAnswersKeepaliveMock).not.toHaveBeenCalled();
  });
});
