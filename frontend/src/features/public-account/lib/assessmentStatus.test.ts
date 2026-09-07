import { describe, expect, it } from 'vitest';
import type { MyAssessment } from '@/shared/api/types';
import { findUnfinishedAssessment, historyActionFor, isUnfinishedAssessment } from './assessmentStatus';

function item(status: string, resultAvailable = false, id = status): MyAssessment {
  return {
    id,
    status,
    startedAt: '2026-09-01T09:00:00Z',
    completedAt: null,
    programCode: 'PERSONALITY_PROFILE',
    programName: 'Shaxsiyat profili',
    resultAvailable,
  };
}

describe('assessmentStatus', () => {
  it("tugallanmagan holatlar — Draft, InProgress, Abandoned; qolgani yo'q", () => {
    expect(isUnfinishedAssessment('Draft')).toBe(true);
    expect(isUnfinishedAssessment('InProgress')).toBe(true);
    expect(isUnfinishedAssessment('Abandoned')).toBe(true);
    expect(isUnfinishedAssessment('Completed')).toBe(false);
    expect(isUnfinishedAssessment('Analyzed')).toBe(false);
    expect(isUnfinishedAssessment('Nimadir')).toBe(false);
  });

  it('holat → qator amali', () => {
    expect(historyActionFor(item('InProgress'))).toBe('resume');
    // Tugallanmagan sessiyada `resultAvailable` kelsa ham natija emas, davom ettirish.
    expect(historyActionFor(item('Draft', true))).toBe('resume');
    expect(historyActionFor(item('Analyzed', true))).toBe('result');
    expect(historyActionFor(item('Analyzed', false))).toBe('pending');
    expect(historyActionFor(item('Completed'))).toBe('pending');
    expect(historyActionFor(item('Analyzing'))).toBe('pending');
    expect(historyActionFor(item('AnalysisFailed'))).toBe('pending');
    expect(historyActionFor(item('Expired'))).toBe('none');
  });

  it("birinchi (eng yangi) tugallanmagan sessiyani topadi, bo'lmasa null", () => {
    expect(findUnfinishedAssessment([item('Analyzed', true), item('InProgress', false, 'a-2')])?.id).toBe(
      'a-2',
    );
    expect(findUnfinishedAssessment([item('Analyzed', true), item('Completed')])).toBeNull();
    expect(findUnfinishedAssessment([])).toBeNull();
  });
});
