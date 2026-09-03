import { describe, expect, it } from 'vitest';
import { buildTestSummaryRows, normalizeTestResults } from './testSummary';
import type {
  ActivityResult,
  AssessmentTestItemDto,
  AssessmentTestResultsRaw,
  BigFiveResult,
  Mbti16Result,
} from './types';


/**
 * `AssessmentTestItemDto` — backend 7 ta MAJBURIY maydon yuboradi
 * (`AdminAssessmentTestItemDto`). Test faqat `testCode`/`nameUz`/`scoringMode` ni
 * tekshiradi, qolganlari shartnomani to'liq saqlash uchun realistik qiymat bilan
 * to'ldiriladi — mock backenddan uzilib qolmasin (`docs/12` §7.1).
 * `questionCount`/`answeredCount` ataylab NOLDAN farqli: UI ularni hali ko'rsatmaydi,
 * lekin "`0` ko'rinmasin" assert'lari bilan tasodifan chalkashmasin.
 */
function testItem(overrides: Partial<AssessmentTestItemDto> = {}): AssessmentTestItemDto {
  return {
    testDefinitionId: '11111111-1111-1111-1111-111111111111',
    testCode: 'MBTI16',
    nameUz: 'Shaxsiyat tipi',
    scoringMode: 'Scored',
    status: 'Completed',
    questionCount: 20,
    answeredCount: 20,
    ...overrides,
  };
}

// Fixture'lar backend natija bloklariga (`AdminMbti16Dto`/`AdminBig5Dto`/`AdminActivityDto`,
// `schema.d.ts` dan re-export) bog'lab qo'yilgan — shakl o'zgarsa `tsc` shu yerda qizaradi.
const MBTI16 = {
  resultCode: 'INTJ',
  typeName: 'Loyihachi',
  axes: {},
  borderlineAxes: [],
} satisfies Mbti16Result;

const BIG5 = {
  factors: {},
  stabilityPct: 70,
  maturityIndex: 68.4,
  maturityLevel: 'Yaxshi',
} satisfies BigFiveResult;

const ACTIVITY = {
  scales: {},
  activityIndex: 65.2,
  activityLevel: 'Moderate',
  needsAttention: false,
} satisfies ActivityResult;

describe('normalizeTestResults', () => {
  it("shartnoma kalitlarini (`MBTI16`) o'qiydi", () => {
    const raw: AssessmentTestResultsRaw = { MBTI16, BIG5 };
    const results = normalizeTestResults(raw);
    expect(results.mbti16?.resultCode).toBe('INTJ');
    expect(results.big5?.maturityIndex).toBe(68.4);
    expect(results.riasec).toBeNull();
    expect(results.activity).toBeNull();
  });

  it('eski camelCase kalitlarni ham qabul qiladi', () => {
    const results = normalizeTestResults({ mbti16: MBTI16, activity: ACTIVITY });
    expect(results.mbti16?.typeName).toBe('Loyihachi');
    expect(results.activity?.activityIndex).toBe(65.2);
  });

  it('`null`/`undefined` da yiqilmaydi va hammasini `null` qiladi', () => {
    expect(normalizeTestResults(null)).toEqual({
      mbti16: null,
      big5: null,
      riasec: null,
      activity: null,
    });
    expect(normalizeTestResults(undefined).mbti16).toBeNull();
  });
});

describe('buildTestSummaryRows', () => {
  it("test ro'yxati berilmasa 4 ta tizim blokini tartib bilan qaytaradi", () => {
    const rows = buildTestSummaryRows(normalizeTestResults({ MBTI16 }));
    expect(rows.map((row) => row.testCode)).toEqual(['MBTI16', 'BIG5', 'RIASEC', 'ACTIVITY']);
    const mbtiRow = rows.at(0);
    expect(mbtiRow?.state).toBe('scored');
    expect(mbtiRow?.resultCode).toBe('INTJ');
  });

  it("natijasi yo'q blok `noData` bo'ladi — `0` ball EMAS", () => {
    const rows = buildTestSummaryRows(normalizeTestResults({ MBTI16 }));
    const big5 = rows.find((row) => row.testCode === 'BIG5');
    expect(big5?.state).toBe('noData');
    expect(big5?.index).toBeNull();
    expect(big5?.resultCode).toBeNull();
  });

  it('`Survey` rejimidagi anketa `notScored` — ballanmaydi (nol EMAS)', () => {
    const tests: AssessmentTestItemDto[] = [
      testItem({ testCode: 'STRESS', nameUz: 'Stressga chidamlilik', scoringMode: 'Survey' }),
    ];
    const rows = buildTestSummaryRows(normalizeTestResults(null), tests);
    expect(rows).toHaveLength(1);
    expect(rows.at(0)).toMatchObject({
      testCode: 'STRESS',
      name: 'Stressga chidamlilik',
      state: 'notScored',
      index: null,
    });
  });

  it("`Scored` anketa hali hisoblanmagan bo'lsa `noData` bo'ladi", () => {
    const rows = buildTestSummaryRows(normalizeTestResults(null), [
      testItem({ testCode: 'STRESS', scoringMode: 'Scored' }),
    ]);
    expect(rows.at(0)?.state).toBe('noData');
  });

  it("test ro'yxati berilsa tartib va tarkib o'shandan olinadi", () => {
    const rows = buildTestSummaryRows(normalizeTestResults({ MBTI16, ACTIVITY }), [
      testItem({ testCode: 'ACTIVITY', scoringMode: 'Scored' }),
      testItem({ testCode: 'MBTI16', scoringMode: 'Scored' }),
      testItem({ testCode: 'STRESS', scoringMode: 'Survey' }),
    ]);
    expect(rows.map((row) => row.testCode)).toEqual(['ACTIVITY', 'MBTI16', 'STRESS']);
    // Dasturga kirmagan BIG5/RIASEC umuman ko'rsatilmaydi.
    expect(rows.map((row) => row.state)).toEqual(['scored', 'scored', 'notScored']);
  });

  it("yig'ma indeks `null` bo'lsa qatorda ham `null` qoladi (`0` ga aylanmaydi)", () => {
    const rows = buildTestSummaryRows(
      normalizeTestResults({ BIG5: { ...BIG5, maturityIndex: null, maturityLevel: null } }),
    );
    const big5 = rows.find((row) => row.testCode === 'BIG5');
    expect(big5?.state).toBe('scored');
    expect(big5?.index).toBeNull();
    expect(big5?.indexKind).toBe('maturity');
  });

  it('aktivlik natijasi indeks va darajani qaytaradi', () => {
    const rows = buildTestSummaryRows(normalizeTestResults({ ACTIVITY }));
    const activity = rows.find((row) => row.testCode === 'ACTIVITY');
    expect(activity).toMatchObject({ state: 'scored', index: 65.2, indexKind: 'activity' });
  });
});
