import { renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AppError } from '@/shared/api/AppError';
import { useCatalogErrorMessage } from './useCatalogErrorMessage';

/**
 * Egasi topgan jonli xato (2026-09-11): savolga javob berilgan bo'lsa DB o'chirishga ruxsat
 * bermaydi, lekin bu holat avval tutilmagan va `500 INTERNAL_ERROR` ko'rinardi. Backend endi
 * uchta aniq kod bilan `409` qaytaradi — shu uchalasi uchun ekranga tushunarli o'zbekcha matn
 * chiqishi va `QUESTION_REFERENCED_BY_VISIBILITY`da backend ko'rsatgan havola qiluvchi
 * savol/bo'lim kodi YO'QOLMASLIGI tekshiriladi.
 */
describe('useCatalogErrorMessage — yangi savol o\'chirish xatolari', () => {
  it('QUESTION_IN_USE — "Faol emas" muqobilini taklif qiladigan matn', () => {
    const { result } = renderHook(() => useCatalogErrorMessage());
    const error = new AppError({
      code: 'QUESTION_IN_USE',
      message: 'Savolga javob berilgan.',
      status: 409,
    });

    const message = result.current(error);

    expect(message).toContain("javob berilgan");
    expect(message).toContain('Faol emas');
    // Xom kod ekranga chiqmasin (`CLAUDE.md` 11-qoida).
    expect(message).not.toContain('QUESTION_IN_USE');
  });

  it('QUESTION_REFERENCED_BY_VISIBILITY — backend ko\'rsatgan havola qiluvchi kod YO\'QOLMAYDI', () => {
    const { result } = renderHook(() => useCatalogErrorMessage());
    const error = new AppError({
      code: 'QUESTION_REFERENCED_BY_VISIBILITY',
      message: "ST-Q05 savolining ko'rsatish sharti ST-Q02 ga tayanadi.",
      status: 409,
    });

    const message = result.current(error);

    expect(message).toContain('ST-Q05');
    expect(message).toContain('ST-Q02');
    expect(message).not.toContain('QUESTION_REFERENCED_BY_VISIBILITY');
  });

  it('QUESTION_REFERENCED_BY_VISIBILITY — backend `detail` bo\'sh bo\'lsa umumiy matnga tushadi (chalkash bo\'sh joy chiqmaydi)', () => {
    const { result } = renderHook(() => useCatalogErrorMessage());
    const error = new AppError({ code: 'QUESTION_REFERENCED_BY_VISIBILITY', message: '', status: 409 });

    const message = result.current(error);

    expect(message.trim().length).toBeGreaterThan(0);
  });

  it("REFERENCED_RECORD_EXISTS — umumiy zaxira xabari", () => {
    const { result } = renderHook(() => useCatalogErrorMessage());
    const error = new AppError({
      code: 'REFERENCED_RECORD_EXISTS',
      message: 'Referenced record exists.',
      status: 409,
    });

    const message = result.current(error);

    expect(message).toContain("bog'liq");
    expect(message).not.toContain('REFERENCED_RECORD_EXISTS');
  });
});
