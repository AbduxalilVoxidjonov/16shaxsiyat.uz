import { describe, expect, it } from 'vitest';
import {
  formatSchoolEntryCode,
  isCompleteSchoolEntryCode,
  normalizeSchoolEntryCode,
} from './schoolEntryCode';

describe('schoolEntryCode', () => {
  it('katta harf, defis, bo‘shliq va en-dash ni yechadi', () => {
    expect(normalizeSchoolEntryCode('7k3m-9xq2')).toBe('7K3M9XQ2');
    expect(normalizeSchoolEntryCode(' 7k3m 9xq2 ')).toBe('7K3M9XQ2');
    expect(normalizeSchoolEntryCode('7K3M–9XQ2')).toBe('7K3M9XQ2');
  });

  it('8 belgidan ortig‘ini kesadi', () => {
    expect(normalizeSchoolEntryCode('7K3M9XQ2ABC')).toBe('7K3M9XQ2');
  });

  it('ko‘rsatish uchun 4 belgidan keyin defis qo‘yadi', () => {
    expect(formatSchoolEntryCode('7K3M9XQ2')).toBe('7K3M-9XQ2');
    expect(formatSchoolEntryCode('7K3M9')).toBe('7K3M-9');
    expect(formatSchoolEntryCode('7K3')).toBe('7K3');
  });

  it('to‘liqlikni uzunlik bo‘yicha aniqlaydi', () => {
    expect(isCompleteSchoolEntryCode('7K3M9XQ2')).toBe(true);
    expect(isCompleteSchoolEntryCode('7K3M9XQ')).toBe(false);
  });
});
