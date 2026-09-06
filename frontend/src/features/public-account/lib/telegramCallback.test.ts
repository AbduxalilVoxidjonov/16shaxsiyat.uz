import { afterEach, describe, expect, it, vi } from 'vitest';
import { readTelegramCallbackParams, stripTelegramCallbackParams } from './telegramCallback';

/** Telegram qaytaradigan to'liq callback (barcha ixtiyoriy maydonlar bilan). */
const FULL_QUERY =
  'id=123456789&first_name=Ali&last_name=Valiyev&username=alivali' +
  '&photo_url=https%3A%2F%2Ft.me%2Fi%2Fuserpic%2F320%2Fali.jpg' +
  `&auth_date=1767225600&hash=${'a'.repeat(64)}`;

describe('readTelegramCallbackParams', () => {
  it("barcha maydonlarni O'ZGARTIRMASDAN o'qiydi (kalitlar `snake_case` qoladi)", () => {
    const body = readTelegramCallbackParams(new URLSearchParams(FULL_QUERY));

    expect(body).toEqual({
      id: 123456789,
      first_name: 'Ali',
      last_name: 'Valiyev',
      username: 'alivali',
      photo_url: 'https://t.me/i/userpic/320/ali.jpg',
      auth_date: 1767225600,
      hash: 'a'.repeat(64),
    });
  });

  it('Telegram bermagan maydonni UMUMAN yubormaydi (imzo buzilmasligi uchun)', () => {
    const body = readTelegramCallbackParams(
      new URLSearchParams(`id=1&first_name=Ali&auth_date=1767225600&hash=${'b'.repeat(64)}`),
    );

    // `undefined` qiymatli kalit ham bo'lmasligi kerak — `JSON.stringify` uni tashlab
    // ketardi, lekin `toEqual` bilan aniq tekshirib qo'yamiz.
    expect(Object.keys(body ?? {}).sort()).toEqual(['auth_date', 'first_name', 'hash', 'id']);
    expect(body).not.toHaveProperty('username');
    expect(body).not.toHaveProperty('photo_url');
    expect(body).not.toHaveProperty('last_name');
  });

  it("Telegram bo'sh satr bilan bergan maydonni AYNAN shundayligicha saqlaydi", () => {
    // Bu holatda `data_check_string` ichida ham `last_name=` bor — ya'ni maydonni
    // tashlab yuborish imzoni buzardi.
    const body = readTelegramCallbackParams(
      new URLSearchParams(`id=1&first_name=Ali&last_name=&auth_date=1&hash=${'c'.repeat(64)}`),
    );

    expect(body?.last_name).toBe('');
  });

  it("majburiy maydon bo'lmasa `null` qaytaradi (oddiy tashrif)", () => {
    expect(readTelegramCallbackParams(new URLSearchParams(''))).toBeNull();
    expect(readTelegramCallbackParams(new URLSearchParams('returnUrl=%2Fkabinet'))).toBeNull();
    expect(readTelegramCallbackParams(new URLSearchParams('id=1&auth_date=1'))).toBeNull();
    expect(
      readTelegramCallbackParams(new URLSearchParams(`id=1&hash=${'d'.repeat(64)}`)),
    ).toBeNull();
  });

  it("`id`/`auth_date` son bo'lmasa `null` qaytaradi", () => {
    expect(
      readTelegramCallbackParams(
        new URLSearchParams(`id=abc&auth_date=1767225600&hash=${'e'.repeat(64)}`),
      ),
    ).toBeNull();
    expect(
      readTelegramCallbackParams(new URLSearchParams(`id=1&auth_date=x&hash=${'e'.repeat(64)}`)),
    ).toBeNull();
  });
});

describe('stripTelegramCallbackParams', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    window.history.replaceState(null, '', '/');
  });

  it('Telegram parametrlarini manzildan olib tashlaydi, qolganini saqlaydi', () => {
    window.history.replaceState(null, '', `/kirish?returnUrl=%2Fkabinet%2Ftest&${FULL_QUERY}`);

    stripTelegramCallbackParams();

    expect(window.location.pathname).toBe('/kirish');
    expect(window.location.search).toBe('?returnUrl=%2Fkabinet%2Ftest');
    expect(window.location.href).not.toContain('Ali');
    expect(window.location.href).not.toContain('123456789');
  });

  it("`pushState` EMAS, `replaceState` — 'orqaga' bosilsa ham ma'lumot qaytmaydi", () => {
    const replaceSpy = vi.spyOn(window.history, 'replaceState');
    const pushSpy = vi.spyOn(window.history, 'pushState');
    window.history.replaceState(null, '', `/kirish?${FULL_QUERY}`);
    replaceSpy.mockClear();

    stripTelegramCallbackParams();

    expect(replaceSpy).toHaveBeenCalledTimes(1);
    expect(replaceSpy.mock.calls[0]?.[2]).toBe('/kirish');
    expect(pushSpy).not.toHaveBeenCalled();
  });
});
