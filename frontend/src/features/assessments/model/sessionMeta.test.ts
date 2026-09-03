import { describe, expect, it } from 'vitest';
import {
  formatDateTime,
  isSessionMetaEmpty,
  parseAssessmentLocationState,
  resolveDurationMinutes,
  resolveSessionMeta,
} from './sessionMeta';
import type { AssessmentDetailDto } from './types';

const DETAIL: AssessmentDetailDto = {
  id: 'assessment-1',
  results: null,
  aiAnalysis: null,
  aiHistory: [],
};

describe('formatDateTime', () => {
  it('UTC sana va vaqtni `KK.OO.YYYY HH:MM` shaklida qaytaradi', () => {
    expect(formatDateTime('2026-08-30T09:05:00Z')).toBe('30.08.2026 09:05');
  });

  it("qiymat yo'q yoki noto'g'ri bo'lsa `null` qaytaradi (bo'sh satr EMAS)", () => {
    expect(formatDateTime(null)).toBeNull();
    expect(formatDateTime(undefined)).toBeNull();
    expect(formatDateTime('salom')).toBeNull();
  });
});

describe('resolveDurationMinutes', () => {
  it('backend bergan qiymat ustunlik qiladi', () => {
    expect(resolveDurationMinutes(29, '2026-08-30T09:00:00Z', '2026-08-30T10:00:00Z')).toBe(29);
  });

  it("qiymat yo'q bo'lsa boshlangan va yakunlangan vaqtdan hisoblaydi", () => {
    expect(resolveDurationMinutes(null, '2026-08-30T09:00:00Z', '2026-08-30T09:29:00Z')).toBe(29);
  });

  it("sessiya yakunlanmagan bo'lsa `null` — `0` EMAS", () => {
    expect(resolveDurationMinutes(null, '2026-08-30T09:00:00Z', null)).toBeNull();
    expect(resolveDurationMinutes(undefined, null, null)).toBeNull();
  });

  it("yakunlangan vaqt boshlanishdan oldin bo'lsa `null`", () => {
    expect(resolveDurationMinutes(null, '2026-08-30T10:00:00Z', '2026-08-30T09:00:00Z')).toBeNull();
  });
});

describe('resolveSessionMeta', () => {
  it('detal javobidagi maydonlar ustunlik qiladi', () => {
    const meta = resolveSessionMeta(
      { ...DETAIL, status: 'Analyzed', reliabilityScore: 82.5, reliabilityFlag: 'Reliable' },
      { assessment: { id: 'assessment-1', status: 'Completed', reliabilityScore: 10 } },
    );
    expect(meta.status).toBe('Analyzed');
    expect(meta.reliabilityScore).toBe(82.5);
    expect(meta.reliabilityFlag).toBe('Reliable');
  });

  it("detalda maydon yo'q bo'lsa ro'yxatdan kelgan qatordan oladi", () => {
    const meta = resolveSessionMeta(DETAIL, {
      assessment: {
        id: 'assessment-1',
        status: 'Completed',
        startedAt: '2026-08-30T09:00:00Z',
        completedAt: '2026-08-30T09:29:00Z',
        studentId: 'student-1',
        studentName: 'Aliyev Sardor',
        schoolId: 'school-1',
        schoolName: '12-son maktab',
        durationMinutes: null,
        reliabilityScore: 45,
        reliabilityFlag: 'Questionable',
      },
    });
    expect(meta.status).toBe('Completed');
    expect(meta.student).toEqual({ id: 'student-1', fullName: 'Aliyev Sardor' });
    expect(meta.school).toEqual({ id: 'school-1', name: '12-son maktab' });
    expect(meta.durationMinutes).toBe(29);
  });

  it('boshqa sessiyaning qatori ishlatilmaydi', () => {
    const meta = resolveSessionMeta(DETAIL, {
      assessment: { id: 'boshqa-sessiya', status: 'Completed', studentName: 'Kimdir' },
    });
    expect(meta.status).toBeNull();
    expect(meta.student).toBeNull();
  });

  it("hech qanday manba bo'lmasa hamma maydon `null`", () => {
    const meta = resolveSessionMeta(DETAIL, null);
    expect(isSessionMetaEmpty(meta)).toBe(true);
    expect(meta.durationMinutes).toBeNull();
    expect(meta.program).toBeNull();
  });
});

describe('parseAssessmentLocationState', () => {
  it("to'g'ri holatni o'qiydi", () => {
    const parsed = parseAssessmentLocationState({
      assessment: { id: 'assessment-1', status: 'Analyzed', reliabilityScore: 82.5 },
    });
    expect(parsed?.assessment?.id).toBe('assessment-1');
    expect(parsed?.assessment?.status).toBe('Analyzed');
  });

  it("noto'g'ri shakl va noma'lum enum qiymatlari jimgina tashlanadi", () => {
    expect(parseAssessmentLocationState(null)).toBeNull();
    expect(parseAssessmentLocationState('salom')).toBeNull();
    expect(parseAssessmentLocationState({ assessment: null })).toBeNull();
    expect(parseAssessmentLocationState({ assessment: { id: 123 } })).toBeNull();

    const parsed = parseAssessmentLocationState({
      assessment: {
        id: 'assessment-1',
        status: 'HACKED',
        reliabilityFlag: 'HACKED',
        reliabilityScore: '82',
      },
    });
    expect(parsed?.assessment?.status).toBeNull();
    expect(parsed?.assessment?.reliabilityFlag).toBeNull();
    expect(parsed?.assessment?.reliabilityScore).toBeNull();
  });
});
