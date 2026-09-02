import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MAX_DURATION_MS, useQuestionVisibility } from './useQuestionVisibility';

/**
 * jsdom `IntersectionObserver`ni amalga oshirmaydi (shu sabab ilova ham fallback bilan
 * ishlaydi) — "haqiqiy kuzatuv" yo'lini sinash uchun soxta implementatsiya, `trigger()` orqali
 * qo'lda "viewport'ga kirdi" hodisasini chaqiradi.
 */
class FakeIntersectionObserver implements IntersectionObserver {
  static instances: FakeIntersectionObserver[] = [];
  readonly root = null;
  readonly rootMargin = '';
  readonly thresholds: number[] = [];
  callback: IntersectionObserverCallback;
  observed = new Set<Element>();

  constructor(callback: IntersectionObserverCallback) {
    this.callback = callback;
    FakeIntersectionObserver.instances.push(this);
  }

  observe(target: Element): void {
    this.observed.add(target);
  }

  unobserve(target: Element): void {
    this.observed.delete(target);
  }

  disconnect(): void {
    this.observed.clear();
  }

  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }

  trigger(target: Element, isIntersecting: boolean): void {
    this.callback([{ isIntersecting, target } as IntersectionObserverEntry], this);
  }
}

function createQuestionNode(questionId: string): HTMLDivElement {
  const node = document.createElement('div');
  node.dataset['questionId'] = questionId;
  return node;
}

describe('useQuestionVisibility', () => {
  beforeEach(() => {
    FakeIntersectionObserver.instances = [];
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("savol ko'ringandan keyin getDurationSince KO'RINISH vaqtidan hisoblaydi, sahifa yuklangan vaqtdan EMAS (QA band 2)", () => {
    vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver);
    const { result } = renderHook(() => useQuestionVisibility());
    const node = createQuestionNode('q1');

    act(() => {
      result.current.registerNode(node);
    });
    const observer = FakeIntersectionObserver.instances[0];
    if (!observer) throw new Error('observer yaratilmadi');

    const nowSpy = vi.spyOn(performance, 'now');
    nowSpy.mockReturnValueOnce(5000); // savol viewport'ga kirgan payt
    act(() => {
      observer.trigger(node, true);
    });

    nowSpy.mockReturnValueOnce(8000); // javob berilgan payt
    const pageLoadFallback = 1000; // "sahifa yuklangan vaqt" — chetlab o'tilishi kerak
    const duration = result.current.getDurationSince('q1', pageLoadFallback);

    // 8000 - 5000 (ko'rinish vaqti), 8000 - 1000 (sahifa yuklangan vaqt) EMAS.
    expect(duration).toBe(3000);
  });

  it("savol hali ko'rinmagan (intersect bo'lmagan) bo'lsa berilgan fallback'dan hisoblaydi", () => {
    vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver);
    const { result } = renderHook(() => useQuestionVisibility());
    const node = createQuestionNode('q1');

    act(() => {
      result.current.registerNode(node); // kuzatuvga olindi, lekin hali intersect bo'lmagan
    });

    const nowSpy = vi.spyOn(performance, 'now').mockReturnValue(9000);
    const duration = result.current.getDurationSince('q1', 2000);

    expect(duration).toBe(7000); // 9000 - fallback(2000)
    nowSpy.mockRestore();
  });

  it("IntersectionObserver mavjud bo'lmagan muhitda ulangan payt darhol 'ko'ringan' deb belgilanadi (fallback)", () => {
    vi.stubGlobal('IntersectionObserver', undefined);
    const { result } = renderHook(() => useQuestionVisibility());
    const node = createQuestionNode('q1');

    const nowSpy = vi.spyOn(performance, 'now');
    nowSpy.mockReturnValueOnce(4000); // registerNode chaqirilgan payt — "ko'ringan" deb qabul qilinadi
    act(() => {
      result.current.registerNode(node);
    });

    nowSpy.mockReturnValueOnce(6500);
    const duration = result.current.getDurationSince('q1', 999); // fallback e'tiborga olinmaydi

    expect(duration).toBe(2500);
  });

  it("MAX_DURATION_MS (10 daqiqa) dan uzoq tanaffusdan keyin yuqori chegara qo'llanadi (QA band 1)", () => {
    vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver);
    const { result } = renderHook(() => useQuestionVisibility());
    const node = createQuestionNode('q1');

    act(() => {
      result.current.registerNode(node);
    });
    const observer = FakeIntersectionObserver.instances[0];
    if (!observer) throw new Error('observer yaratilmadi');

    const nowSpy = vi.spyOn(performance, 'now');
    nowSpy.mockReturnValueOnce(0);
    act(() => {
      observer.trigger(node, true);
    });

    // 15 daqiqadan keyin javob berildi (masalan o'quvchi telefonni qo'yib ketgan).
    nowSpy.mockReturnValueOnce(MAX_DURATION_MS + 5 * 60 * 1000);
    const duration = result.current.getDurationSince('q1', 0);

    expect(duration).toBe(MAX_DURATION_MS);
  });

  it("chegaradan sal past qiymatlar o'zgarishsiz qaytadi (chegara faqat oshib ketganda qisqartiradi)", () => {
    vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver);
    const { result } = renderHook(() => useQuestionVisibility());
    const node = createQuestionNode('q1');

    act(() => {
      result.current.registerNode(node);
    });
    const observer = FakeIntersectionObserver.instances[0];
    if (!observer) throw new Error('observer yaratilmadi');

    const nowSpy = vi.spyOn(performance, 'now');
    nowSpy.mockReturnValueOnce(0);
    act(() => {
      observer.trigger(node, true);
    });

    nowSpy.mockReturnValueOnce(3120); // oddiy, chegaradan ancha past javob vaqti
    expect(result.current.getDurationSince('q1', 0)).toBe(3120);
  });

  it("reset() eski vaqt belgilarini tozalaydi — keyingi sahifada qaytadan fallback'dan hisoblanadi", () => {
    vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver);
    const { result } = renderHook(() => useQuestionVisibility());
    const node = createQuestionNode('q1');

    const nowSpy = vi.spyOn(performance, 'now');
    act(() => {
      result.current.registerNode(node);
    });
    const observer = FakeIntersectionObserver.instances[0];
    if (!observer) throw new Error('observer yaratilmadi');
    nowSpy.mockReturnValueOnce(1000);
    act(() => {
      observer.trigger(node, true);
    });

    act(() => {
      result.current.reset();
    });

    nowSpy.mockReturnValueOnce(9000);
    const duration = result.current.getDurationSince('q1', 4000); // 'q1' kuzatuvi tozalangan — fallback ishlaydi

    expect(duration).toBe(5000); // 9000 - 4000
  });
});
