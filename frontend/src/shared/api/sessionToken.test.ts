import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { adoptSession, getSessionToken, setSessionAdopter } from './sessionToken';

/**
 * `adoptSession`ning 2-yo'li — `sessionStore` moduli yuklanmagan holat (`localStorage`ga
 * bevosita yozish). 1-yo'l (adapter) `sessionStore.test.ts`da `setSession` orqali sinaladi.
 */
describe("sessionToken.adoptSession (localStorage yo'li)", () => {
  beforeEach(() => {
    localStorage.clear();
    setSessionAdopter(null);
  });

  afterEach(() => {
    localStorage.clear();
    setSessionAdopter(null);
  });

  it("maktab sessiyasi (accessToken bilan) turganda Telegram sessiyasi uning ustidan yozadi, accessToken null bo'ladi", () => {
    localStorage.setItem(
      STORAGE_KEYS.session,
      JSON.stringify({
        version: 0,
        state: {
          sessionToken: 'school-tok',
          slug: 'demo-school',
          assessmentId: 'a-1',
          accessToken: 'k-1',
          selectedProgramCode: 'CAREER_SURVEY',
        },
      }),
    );

    adoptSession({ sessionToken: 'tg-tok', slug: 'ommaviy', assessmentId: 'a-2' });

    const persisted = JSON.parse(localStorage.getItem(STORAGE_KEYS.session) as string) as {
      state: Record<string, unknown>;
    };
    expect(persisted.state).toMatchObject({
      sessionToken: 'tg-tok',
      slug: 'ommaviy',
      assessmentId: 'a-2',
      accessToken: null,
      // Sessiyaga bog'liq bo'lmagan maydonlar saqlanadi.
      selectedProgramCode: 'CAREER_SURVEY',
    });
    expect(getSessionToken()).toBe('tg-tok');
  });

  it('boshqa token bilan qabul qilinganda eski javob navbati tozalanadi, xuddi shu token bilan qoladi', () => {
    localStorage.setItem(
      STORAGE_KEYS.session,
      JSON.stringify({
        version: 0,
        state: { sessionToken: 'tok-1', slug: 'ommaviy', assessmentId: 'a-1' },
      }),
    );
    localStorage.setItem(STORAGE_KEYS.pendingAnswers, JSON.stringify({ q1: { pending: true } }));

    adoptSession({ sessionToken: 'tok-1', slug: 'ommaviy', assessmentId: 'a-1' });
    expect(localStorage.getItem(STORAGE_KEYS.pendingAnswers)).not.toBeNull();

    adoptSession({ sessionToken: 'tok-2', slug: 'ommaviy', assessmentId: 'a-2' });
    expect(localStorage.getItem(STORAGE_KEYS.pendingAnswers)).toBeNull();
  });
});
