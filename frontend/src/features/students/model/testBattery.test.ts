import { describe, expect, it } from 'vitest';
import type { AssessmentBatteryTestBlock } from '@/shared/api/assessmentBatteryTypes';
import { buildPresentTestCodes } from './testBattery';

function test(code: string): AssessmentBatteryTestBlock {
  return { code, nameUz: code, status: 'Completed', scoringMode: 'Scored' };
}

describe('buildPresentTestCodes', () => {
  it("4 ta tizim kodini to'g'ri tanlaydi, boshqa (Custom/Survey) kodlarni e'tiborsiz qoldiradi", () => {
    const codes = buildPresentTestCodes([
      test('MBTI16'),
      test('BIG5'),
      { ...test('INTELLECT-SURVEY'), scoringMode: 'Survey' },
    ]);

    expect(codes.has('MBTI16')).toBe(true);
    expect(codes.has('BIG5')).toBe(true);
    expect(codes.has('RIASEC')).toBe(false);
    expect(codes.has('ACTIVITY')).toBe(false);
    expect(codes.size).toBe(2);
  });

  it("tests undefined/null bo'lsa — BO'SH to'plam (hech narsa borligi taxmin qilinmaydi)", () => {
    expect(buildPresentTestCodes(undefined).size).toBe(0);
    expect(buildPresentTestCodes(null).size).toBe(0);
  });

  it("tests bo'sh massiv bo'lsa — BO'SH to'plam", () => {
    expect(buildPresentTestCodes([]).size).toBe(0);
  });

  it("bitta ham tizim kodi bo'lmasa (faqat so'rovnoma) — BO'SH to'plam", () => {
    const codes = buildPresentTestCodes([{ ...test('INTELLECT-SURVEY'), scoringMode: 'Survey' }]);
    expect(codes.size).toBe(0);
  });
});
