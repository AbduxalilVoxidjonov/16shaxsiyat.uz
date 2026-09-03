import type {
  AssessmentTestItemDto,
  AssessmentTestResults,
  AssessmentTestResultsRaw,
} from './types';

/** Tizim metodikalari — `docs/03`, ko'rsatish tartibi `docs/11` A-5 bilan bir xil. */
export const SYSTEM_TEST_CODES = ['MBTI16', 'BIG5', 'RIASEC', 'ACTIVITY'] as const;

/**
 * Bitta test qatorining holati:
 * - `scored` — ball/natija hisoblangan va bor;
 * - `notScored` — anketa `Survey` rejimida, ya'ni **ballanmaydi** (`docs/06` qarorlar
 *   jurnali, 2026-09-02: "`Survey` javoblari saqlanadi, `TestResult` ball yozilmaydi").
 *   Bu holat `0` ball sifatida KO'RSATILMAYDI;
 * - `noData` — natija yo'q (yechilmagan yoki hali hisoblanmagan).
 */
export type TestSummaryState = 'scored' | 'notScored' | 'noData';

export interface TestSummaryRow {
  testCode: string;
  /** Backend bergan nom (`tests[].nameUz`); yo'q bo'lsa UI kod bo'yicha i18n nomini oladi. */
  name: string | null;
  state: TestSummaryState;
  /** `MBTI16` → `INTJ`, `RIASEC` → `IRA`; boshqalarda `null`. */
  resultCode: string | null;
  /** Natija kodining o'zbekcha nomi (`TypeCatalog.NameUz`), bo'lmasa `null`. */
  resultName: string | null;
  /** Yig'ma indeks (yetuklik yoki aktivlik). Hisoblanmagan bo'lsa `null` — `0` EMAS. */
  index: number | null;
  indexKind: 'maturity' | 'activity' | null;
}

/**
 * `results` obyektini bitta shaklga keltiradi. Shartnoma kaliti — katta harfli `MBTI16`
 * (`docs/07` 3.2), lekin eski javoblarda camelCase (`mbti16`) uchraydi; ikkalasi ham
 * qabul qilinadi (`types.ts` dagi `AssessmentTestResultsRaw` izohiga qarang).
 */
export function normalizeTestResults(
  raw: AssessmentTestResultsRaw | null | undefined,
): AssessmentTestResults {
  return {
    mbti16: raw?.MBTI16 ?? raw?.mbti16 ?? null,
    big5: raw?.BIG5 ?? raw?.big5 ?? null,
    riasec: raw?.RIASEC ?? raw?.riasec ?? null,
    activity: raw?.ACTIVITY ?? raw?.activity ?? null,
  };
}

function systemRow(code: string, results: AssessmentTestResults): TestSummaryRow | null {
  switch (code) {
    case 'MBTI16':
      return results.mbti16
        ? {
            testCode: code,
            name: null,
            state: 'scored',
            resultCode: results.mbti16.resultCode || null,
            resultName: results.mbti16.typeName || null,
            index: null,
            indexKind: null,
          }
        : null;
    case 'BIG5':
      return results.big5
        ? {
            testCode: code,
            name: null,
            state: 'scored',
            resultCode: null,
            resultName: results.big5.maturityLevel ?? null,
            index: results.big5.maturityIndex ?? null,
            indexKind: 'maturity',
          }
        : null;
    case 'RIASEC':
      return results.riasec
        ? {
            testCode: code,
            name: null,
            state: 'scored',
            resultCode: results.riasec.resultCode || null,
            resultName: null,
            index: null,
            indexKind: null,
          }
        : null;
    case 'ACTIVITY':
      return results.activity
        ? {
            testCode: code,
            name: null,
            state: 'scored',
            resultCode: null,
            resultName: results.activity.activityLevel ?? null,
            index: results.activity.activityIndex ?? null,
            indexKind: 'activity',
          }
        : null;
    default:
      return null;
  }
}

/**
 * Sessiyadagi har bir test uchun bitta qator quradi.
 *
 * `tests` berilgan bo'lsa (backend `AssessmentTest` ro'yxatini qaytara boshlaganda) tartib
 * va tarkib o'shandan olinadi — dastur ixtiyoriy anketalarni ham o'z ichiga olishi mumkin.
 * Berilmagan bo'lsa 4 ta tizim bloki ko'rsatiladi: yechilmagani "ma'lumot yo'q" bo'lib
 * qoladi, `0` ball bilan emas.
 */
export function buildTestSummaryRows(
  results: AssessmentTestResults,
  tests?: AssessmentTestItemDto[] | null,
): TestSummaryRow[] {
  const ordered: string[] = [];
  const push = (code: string) => {
    if (code && !ordered.includes(code)) ordered.push(code);
  };

  const testByCode = new Map<string, AssessmentTestItemDto>();
  for (const test of tests ?? []) {
    testByCode.set(test.testCode, test);
    push(test.testCode);
  }

  for (const code of SYSTEM_TEST_CODES) {
    if (systemRow(code, results) !== null || (tests?.length ?? 0) === 0) {
      push(code);
    }
  }

  return ordered.map((code) => {
    const definition = testByCode.get(code);
    const scored = systemRow(code, results);
    if (scored) {
      return { ...scored, name: definition?.nameUz ?? null };
    }
    return {
      testCode: code,
      name: definition?.nameUz ?? null,
      state: definition?.scoringMode === 'Survey' ? 'notScored' : 'noData',
      resultCode: null,
      resultName: null,
      index: null,
      indexKind: null,
    };
  });
}
