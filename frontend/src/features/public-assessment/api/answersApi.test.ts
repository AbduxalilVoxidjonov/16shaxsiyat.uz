import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { jsonResponse } from '@/test/apiMock';
import { sendAnswersKeepalive } from './answersApi';
import type { BranchingSaveAnswerEntry } from '@/shared/api/branchingTypes';

/** `POST /sessions/tests/{testCode}/answers` javobi — `docs/07` 1.6-bo'lim. */
function okResponse(): Response {
  return jsonResponse<'SaveAnswersResult'>({ savedCount: 1, answered: 1, total: 1 });
}

describe('sendAnswersKeepalive', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(okResponse()));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("oddiy fetch'ni keepalive:true va X-Session-Token header bilan chaqiradi (sendBeacon EMAS — PM tuzatishi)", () => {
    sendAnswersKeepalive('BIG5', [{ questionId: 'q1', value: 4, durationMs: 10 }], 'sess-token-1');

    expect(fetch).toHaveBeenCalledTimes(1);
    const [url, init] = vi.mocked(fetch).mock.calls[0] as [string, RequestInit];

    expect(url).toContain('/api/public/sessions/tests/BIG5/answers');
    expect(init.method).toBe('POST');
    expect(init.keepalive).toBe(true);
    const headers = new Headers(init.headers);
    expect(headers.get('X-Session-Token')).toBe('sess-token-1');
    expect(headers.get('Content-Type')).toContain('json');
  });

  it("so'rov tanasida faqat questionId/value/durationMs bor — token JSON tanasiga QO'SHILMAYDI", () => {
    sendAnswersKeepalive('BIG5', [{ questionId: 'q1', value: 4, durationMs: 10 }], 'sess-token-1');

    const [, init] = vi.mocked(fetch).mock.calls[0] as [string, RequestInit];
    const body = JSON.parse(init.body as string) as { answers: unknown[] };

    expect(body).toEqual({ answers: [{ questionId: 'q1', value: 4, durationMs: 10 }] });
    expect(JSON.stringify(body)).not.toContain('sessionToken');
    expect(JSON.stringify(body)).not.toContain('sess-token-1');
  });

  it("bo'sh massiv bo'lsa fetch chaqirilmaydi", () => {
    sendAnswersKeepalive('BIG5', [], 'sess-token-1');
    expect(fetch).not.toHaveBeenCalled();
  });

  it("katta navbat ~64KB umumiy keepalive byudjetini hisobga olib bo'laklarga bo'linadi", () => {
    // Har biri ~120 bayt (uzun `questionId` bilan) — 400 ta ~48KB, byudjet (~32KB) dan oshadi,
    // shu sabab hammasi emas, faqat byudjetga sig'gani yuboriladi va bir nechta so'rovga bo'linadi.
    const longId = 'q'.repeat(80);
    const answers: BranchingSaveAnswerEntry[] = Array.from({ length: 400 }, (_, index) => ({
      questionId: `${longId}-${String(index)}`,
      value: (index % 5) + 1,
      durationMs: 1000 + index,
    }));

    sendAnswersKeepalive('BIG5', answers, 'sess-token-1');

    const calls = vi.mocked(fetch).mock.calls;
    expect(calls.length).toBeGreaterThan(1); // bir nechta bo'lakka bo'lingan

    let sentCount = 0;
    for (const [, init] of calls) {
      const body = JSON.parse((init as RequestInit).body as string) as { answers: unknown[] };
      expect(JSON.stringify(body).length).toBeLessThan(9000); // har bo'lak byudjet ichida
      sentCount += body.answers.length;
    }
    // Umumiy byudjet cheklovi tufayli HAMMASI emas, faqat bir qismi yuborilgan bo'lishi kerak —
    // qolgani mahalliy navbatda (localStorage) qoladi va keyingi tashrifda qayta yuboriladi.
    expect(sentCount).toBeLessThan(answers.length);
    expect(sentCount).toBeGreaterThan(0);
  });

  it("fetch reject bo'lsa (masalan sahifa allaqachon yopilgan) xato uloqtirilmaydi", () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    expect(() =>
      sendAnswersKeepalive('BIG5', [{ questionId: 'q1', value: 4, durationMs: 10 }], 'sess-token-1'),
    ).not.toThrow();
  });
});
