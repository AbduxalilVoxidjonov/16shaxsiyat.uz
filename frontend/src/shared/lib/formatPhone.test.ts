import { describe, expect, it } from 'vitest';
import { extractUzLocalDigits, formatUzLocalDigits, toE164UzPhone } from './formatPhone';

describe('extractUzLocalDigits', () => {
  it("raqam bo'lmagan belgilarni olib tashlaydi", () => {
    expect(extractUzLocalDigits('(90) 123-45-67')).toBe('901234567');
  });

  it("9 tadan ortiq raqamni qirqadi", () => {
    expect(extractUzLocalDigits('9012345678999')).toBe('901234567');
  });

  it("bo'sh matn uchun bo'sh qator qaytaradi", () => {
    expect(extractUzLocalDigits('')).toBe('');
  });
});

describe('formatUzLocalDigits', () => {
  it("kiritilgan qism bo'yicha bosqichma-bosqich formatlaydi", () => {
    expect(formatUzLocalDigits('')).toBe('');
    expect(formatUzLocalDigits('9')).toBe('(9');
    expect(formatUzLocalDigits('90')).toBe('(90)');
    expect(formatUzLocalDigits('901')).toBe('(90) 1');
    expect(formatUzLocalDigits('90123')).toBe('(90) 123');
    expect(formatUzLocalDigits('9012345')).toBe('(90) 123-45');
    expect(formatUzLocalDigits('901234567')).toBe('(90) 123-45-67');
  });

  it("formatlashdan oldin raqam bo'lmagan belgilarni tozalaydi", () => {
    expect(formatUzLocalDigits('90-123-45-67')).toBe('(90) 123-45-67');
  });
});

describe('toE164UzPhone', () => {
  it("to'liq 9 ta raqamni +998 bilan birlashtiradi", () => {
    expect(toE164UzPhone('901234567')).toBe('+998901234567');
  });

  it("to'liq bo'lmagan raqam uchun null qaytaradi", () => {
    expect(toE164UzPhone('9012345')).toBeNull();
    expect(toE164UzPhone('')).toBeNull();
  });
});
