import { describe, expect, it } from 'vitest';
import { formatDate } from './formatDate';

describe('formatDate', () => {
  it('ISO sanani KK.OO.YYYY shaklida formatlaydi', () => {
    expect(formatDate('2026-08-30T10:00:00Z')).toBe('30.08.2026');
  });

  it('null/undefined uchun tire qaytaradi', () => {
    expect(formatDate(null)).toBe('—');
    expect(formatDate(undefined)).toBe('—');
  });

  it("noto'g'ri sana qatori uchun tire qaytaradi", () => {
    expect(formatDate('not-a-date')).toBe('—');
  });
});
