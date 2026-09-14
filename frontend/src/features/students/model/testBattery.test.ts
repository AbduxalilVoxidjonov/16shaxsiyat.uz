import { describe, expect, it } from 'vitest';
import type { AssessmentBatteryTestBlock } from '@/shared/api/assessmentBatteryTypes';
import { buildPresentTestCodes } from './testBattery';

function test(
  code: string,
  batteryRole?: AssessmentBatteryTestBlock['batteryRole'],
): AssessmentBatteryTestBlock {
  return { code, nameUz: code, status: 'Completed', scoringMode: 'Scored', batteryRole };
}

describe('buildPresentTestCodes', () => {
  it("rol bo'yicha 4 ta tizim kaliti to'g'ri tanlanadi, `None` e'tiborsiz qoldiriladi", () => {
    const codes = buildPresentTestCodes([
      test('MBTI16', 'PersonalityType'),
      test('BIG5', 'Traits'),
      test('RIASEC', 'CareerInterest'),
      test('ACTIVITY', 'Activity'),
      { ...test('INTELLECT-SURVEY', 'None'), scoringMode: 'Survey' },
    ]);

    expect(codes.has('MBTI16')).toBe(true);
    expect(codes.has('BIG5')).toBe(true);
    expect(codes.has('RIASEC')).toBe(true);
    expect(codes.has('ACTIVITY')).toBe(true);
    expect(codes.size).toBe(4);
  });

  it("tests undefined/null bo'lsa — BO'SH to'plam (hech narsa borligi taxmin qilinmaydi)", () => {
    expect(buildPresentTestCodes(undefined).size).toBe(0);
    expect(buildPresentTestCodes(null).size).toBe(0);
  });

  it("tests bo'sh massiv bo'lsa — BO'SH to'plam", () => {
    expect(buildPresentTestCodes([]).size).toBe(0);
  });

  it("bitta ham shaxsiyat roli bo'lmasa (faqat so'rovnoma, `None`) — BO'SH to'plam", () => {
    const codes = buildPresentTestCodes([
      { ...test('INTELLECT-SURVEY', 'None'), scoringMode: 'Survey' },
    ]);
    expect(codes.size).toBe(0);
  });

  /**
   * Kod-review (2026-09-14): mezon `code` EMAS, `batteryRole` — kod versiyalansa
   * (`MBTI16-V2`) ham backend rolni to'g'ri qaytarsa karta ko'rinishi kerak.
   */
  it("kod versiyalangan bo'lsa ham (`MBTI16-V2`), rol `PersonalityType` bo'lsa — `MBTI16` mavjud deb topiladi", () => {
    const codes = buildPresentTestCodes([test('MBTI16-V2', 'PersonalityType')]);
    expect(codes.has('MBTI16')).toBe(true);
    expect(codes.size).toBe(1);
  });

  it("kod `MBTI16` bo'lsa ham, rol `None` bo'lsa — mavjud deb topilmaydi", () => {
    const codes = buildPresentTestCodes([test('MBTI16', 'None')]);
    expect(codes.has('MBTI16')).toBe(false);
    expect(codes.size).toBe(0);
  });

  it("`batteryRole` yo'q (eski backend/moslama) — xavfli taxmin qilinmaydi, BO'SH to'plam", () => {
    const codes = buildPresentTestCodes([test('MBTI16')]);
    expect(codes.size).toBe(0);
  });
});
