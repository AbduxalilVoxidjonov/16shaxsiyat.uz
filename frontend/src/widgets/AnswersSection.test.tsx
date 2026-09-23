import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { typedResponse } from '@/test/apiMock';
import type { AssessmentAnswersDto, RawAnswerDto } from '@/shared/api/assessmentAnswersTypes';
import { AnswersSection } from './AnswersSection';

/**
 * `docs/11` A-5/A-6 — savolma-savol javoblar (`features/students` VA `features/assessments`
 * ikkalasida ham ochiladigan `widgets/` bloki, P52-A). Eng muhim tekshiruv: TESKARI savolga
 * berilgan `5` jadvalda `1` bo'lib ko'rinishi (`docs/03` §1). Xom `5` psixologni butunlay
 * teskari xulosaga olib boradi.
 *
 * `jsonResponse<'AdminAssessmentAnswersDto'>` EMAS, `typedResponse<AssessmentAnswersDto>`
 * ishlatiladi — sxema P52-B (`selectedValues`/`selectedOptionTexts`/`scoringMode`/
 * `textValue`, nullable `rawValue`/`effectiveValue`/`isFastAnswer`) dan ESKIRGAN
 * (`shared/api/assessmentAnswersTypes.ts` izohiga qarang).
 */
const THRESHOLDS = {
  fastAnswerDurationMs: 900,
  straightLiningMinRunLength: 12,
  shortSessionMinutes: 6,
};

function answer(overrides: Partial<RawAnswerDto> & { questionCode: string }): RawAnswerDto {
  return {
    questionId: `q-${overrides.questionCode}`,
    testCode: 'BIG5',
    questionText: `${overrides.questionCode} savoli`,
    rawValue: 3,
    selectedOptionText: null,
    selectedOptionTexts: null,
    selectedValues: null,
    textValue: null,
    durationMs: 3000,
    revisionCount: 0,
    answeredAt: '2026-08-30T09:10:00Z',
    questionType: 'Likert5',
    scoringMode: 'Scored',
    scale: 'C',
    scaleNameUz: 'Vijdonlilik',
    scaleDirection: 1,
    weight: 1,
    effectiveValue: 3,
    isFastAnswer: false,
    straightLiningBlockIndex: null,
    ...overrides,
  };
}

function response(answers: RawAnswerDto[], overrides: Partial<AssessmentAnswersDto> = {}): AssessmentAnswersDto {
  return {
    answers,
    session: {
      answeredCount: answers.length,
      fastAnswerCount: answers.filter((a) => a.isFastAnswer).length,
      straightLiningBlockCount: 0,
      allSameAnswer: false,
      shortSession: false,
      totalDurationSeconds: 1800,
      reliabilityScore: 62,
      reliabilityFlag: 'Questionable',
    },
    scales: [],
    thresholds: THRESHOLDS,
    ...overrides,
  };
}

function renderSection(
  assessmentId: string | null = 'assessment-1',
  testNames?: Readonly<Record<string, string>>,
) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <AnswersSection assessmentId={assessmentId} testNames={testNames} />
    </QueryClientProvider>,
  );
}

/**
 * Bo'lim va test bloklari YIG'ILMAGAN (egasining talabi, 2026-09-23) — hech narsa bosilmaydi,
 * faqat jadval yuklanishini kutamiz.
 */
async function waitForTables() {
  await screen.findAllByRole('table');
  return userEvent.setup();
}

describe('AnswersSection', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sessiya yo'q bo'lsa so'rov YUBORMAYDI va bo'sh holat ko'rsatiladi", () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    renderSection(null);

    expect(fetchMock).not.toHaveBeenCalled();
    expect(screen.getByText('Bu sessiya uchun javob topilmadi')).toBeInTheDocument();
  });

  it("topshirilgan testlar YIG'ILMAGAN holda tagma-tag chiqadi, sarlavhada katalog nomi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          typedResponse<AssessmentAnswersDto>(
            response([
              answer({ questionCode: 'BIG5-Q01' }),
              answer({
                questionCode: 'SURVEY-Q01',
                testCode: 'INTELLECT-SURVEY',
                questionType: 'ShortText',
                textValue: 'Javob matni',
                scoringMode: 'Survey',
                rawValue: null,
                effectiveValue: null,
                isFastAnswer: null,
              }),
              answer({ questionCode: 'ORPHAN-Q01', testCode: 'NO-NAME' }),
            ]),
          ),
        ),
      ),
    );

    renderSection('assessment-1', {
      BIG5: 'Katta beshlik',
      'INTELLECT-SURVEY': "Maktab o'quvchilari uchun so'rovnoma",
    });

    // Hech narsa bosilmaydi — uchala blokning savollari darhol ko'rinadi.
    expect(await screen.findByText('BIG5-Q01 savoli')).toBeInTheDocument();
    expect(screen.getByText('SURVEY-Q01 savoli')).toBeInTheDocument();
    expect(screen.getByText('ORPHAN-Q01 savoli')).toBeInTheDocument();
    expect(screen.getAllByRole('table')).toHaveLength(3);
    expect(screen.queryByRole('button', { name: /Savolma-savol javoblar/i })).not.toBeInTheDocument();

    // Sarlavhalar — katalog nomi, kod EMAS; nom bo'lmasa kod zaxira sifatida; tartib saqlanadi.
    const headings = screen.getAllByRole('heading', { level: 4 }).map((h) => h.textContent);
    expect(headings).toEqual([
      'Katta beshlik (1 ta javob)',
      "Maktab o'quvchilari uchun so'rovnoma (1 ta javob)",
      'NO-NAME (1 ta javob)',
    ]);
    expect(
      screen.getByRole('region', { name: /Maktab o'quvchilari uchun so'rovnoma/ }),
    ).toBeInTheDocument();
  });

  it("ishonchlilik signallari va filtrlar (test bloki, shkala, faqat belgilanganlar) YO'Q", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          typedResponse<AssessmentAnswersDto>(
            response([answer({ questionCode: 'BIG5-Q01', durationMs: 500, isFastAnswer: true })], {
              scales: [
                {
                  scale: 'C',
                  scaleNameUz: 'Vijdonlilik',
                  forwardCount: 6,
                  reverseCount: 4,
                  forwardAvgPct: 72.5,
                  reverseAvgPct: 31.25,
                  mismatchPct: 41.25,
                },
              ],
            }),
          ),
        ),
      ),
    );

    renderSection();
    await waitForTables();

    expect(screen.queryByText(/Ishonchlilik:/)).not.toBeInTheDocument();
    expect(screen.queryByText(/signallardan chiqadi/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Teskari savollar mosligi/)).not.toBeInTheDocument();
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
    expect(screen.queryByText('Faqat belgilanganlar')).not.toBeInTheDocument();
    expect(screen.queryByText('Test bloki')).not.toBeInTheDocument();
    // Qator darajasidagi belgilar jadvalda QOLADI.
    const row = screen.getByText('BIG5-Q01 savoli').closest('tr');
    expect(row).toHaveTextContent('900 ms dan tez');
    expect(row).toHaveTextContent('teskari savol farqi');
  });

  it('⚠️ teskari savolga berilgan 5 ni shkalaga tushgan 1 sifatida ko\'rsatadi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          typedResponse<AssessmentAnswersDto>(
            response([
              answer({ questionCode: 'BIG5-Q01', scaleDirection: -1, rawValue: 5, effectiveValue: 1 }),
              answer({ questionCode: 'BIG5-Q02', scaleDirection: 1, rawValue: 4, effectiveValue: 4 }),
            ]),
          ),
        ),
      ),
    );

    renderSection();
    await waitForTables();

    const reverseRow = (await screen.findByText('BIG5-Q01 savoli')).closest('tr');
    expect(reverseRow).not.toBeNull();
    // Xom qiymat ham ko'rinadi, lekin ASOSIY qiymat — teskari tuzatilgani.
    expect(reverseRow).toHaveTextContent('Teskari');
    expect(reverseRow).toHaveTextContent('xom javob 5 → teskari tuzatildi');
    expect(reverseRow?.querySelector('.font-semibold')?.textContent).toBe('1');

    const forwardRow = screen.getByText('BIG5-Q02 savoli').closest('tr');
    expect(forwardRow).toHaveTextContent("To'g'ri");
    expect(forwardRow?.querySelector('.font-semibold')?.textContent).toBe('4');
  });

  it('javobni SO\'Z bilan ko\'rsatadi (docs/03 §1 Likert yorlig\'i)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          typedResponse<AssessmentAnswersDto>(
            response([answer({ questionCode: 'BIG5-Q01', rawValue: 5, effectiveValue: 5 })]),
          ),
        ),
      ),
    );

    renderSection();
    await waitForTables();

    expect(await screen.findByText("To'liq qo'shilaman")).toBeInTheDocument();
  });

  it('tez javobni belgilaydi, chegaraning O\'ZINI (900 ms) belgilamaydi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          typedResponse<AssessmentAnswersDto>(
            response([
              answer({ questionCode: 'BIG5-Q01', durationMs: 899, isFastAnswer: true }),
              answer({ questionCode: 'BIG5-Q02', durationMs: 900, isFastAnswer: false }),
            ]),
          ),
        ),
      ),
    );

    renderSection();
    await waitForTables();

    const fastRow = (await screen.findByText('BIG5-Q01 savoli')).closest('tr');
    expect(fastRow).toHaveTextContent('900 ms dan tez');

    const boundaryRow = screen.getByText('BIG5-Q02 savoli').closest('tr');
    expect(boundaryRow).not.toHaveTextContent('900 ms dan tez');
  });

  it('straight-lining seriyasini ajratib ko\'rsatadi', async () => {
    const straightLined = Array.from({ length: 12 }, (_, index) =>
      answer({ questionCode: `BIG5-Q${String(index + 1).padStart(2, '0')}`, straightLiningBlockIndex: 1 }),
    );
    straightLined.push(answer({ questionCode: 'BIG5-Q13' }));

    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          typedResponse<AssessmentAnswersDto>(
            response(straightLined, {
              session: {
                answeredCount: 13,
                fastAnswerCount: 0,
                straightLiningBlockCount: 1,
                allSameAnswer: false,
                shortSession: false,
                totalDurationSeconds: 1800,
                reliabilityScore: 62,
                reliabilityFlag: 'Questionable',
              },
            }),
          ),
        ),
      ),
    );

    renderSection();
    await waitForTables();

    const markedRow = (await screen.findByText('BIG5-Q01 savoli')).closest('tr');
    expect(markedRow).toHaveTextContent('1-bir xil javob bloki');
    expect(markedRow?.className).toContain('bg-warning-50');

    const cleanRow = screen.getByText('BIG5-Q13 savoli').closest('tr');
    expect(cleanRow).not.toHaveTextContent('bir xil javob bloki');

  });

  it("shkala nomini ko'rsatadi (kod emas, NOM asosiy)", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          typedResponse<AssessmentAnswersDto>(response([answer({ questionCode: 'BIG5-Q01' })])),
        ),
      ),
    );

    renderSection();
    await waitForTables();

    expect(screen.getAllByText('Vijdonlilik').length).toBeGreaterThan(0);
  });

  describe("so'rovnoma (Survey) bloki — P52-B", () => {
    function surveyAnswer(overrides: Partial<RawAnswerDto> & { questionCode: string }): RawAnswerDto {
      return answer({
        testCode: 'CAREER_SURVEY',
        scale: 'SURVEY',
        scaleNameUz: null,
        scaleDirection: 1,
        weight: 1,
        effectiveValue: null,
        isFastAnswer: null,
        rawValue: null,
        scoringMode: 'Survey',
        ...overrides,
      });
    }

    it('MultiChoice javobida selectedOptionTexts vergul bilan ko\'rsatiladi, selectedValues raqamlari EMAS', async () => {
      vi.stubGlobal(
        'fetch',
        vi.fn(() =>
          Promise.resolve(
            typedResponse<AssessmentAnswersDto>(
              response([
                surveyAnswer({
                  questionCode: 'SURVEY-Q01',
                  questionType: 'MultiChoice',
                  selectedValues: [3, 1],
                  selectedOptionTexts: ['Matematika', 'Ingliz tili'],
                }),
              ]),
            ),
          ),
        ),
      );

      renderSection();
      await waitForTables();

      const row = (await screen.findByText('SURVEY-Q01 savoli')).closest('tr');
      expect(row).toHaveTextContent('Matematika, Ingliz tili');
      expect(row).not.toHaveTextContent('3, 1');
    });

    it('matn javobi (LongText) to\'liq holda saqlanadi, uzun bo\'lsa qisqartirilib "To\'liq ko\'rish" bilan ko\'rsatiladi', async () => {
      const longText = 'A'.repeat(300);
      vi.stubGlobal(
        'fetch',
        vi.fn(() =>
          Promise.resolve(
            typedResponse<AssessmentAnswersDto>(
              response([
                surveyAnswer({
                  questionCode: 'SURVEY-Q02',
                  questionType: 'LongText',
                  textValue: longText,
                }),
              ]),
            ),
          ),
        ),
      );

      renderSection();
      const user = await waitForTables();

      const row = (await screen.findByText('SURVEY-Q02 savoli')).closest('tr');
      expect(row).not.toBeNull();
      expect(row?.textContent).not.toContain(longText);
      const toggle = screen.getByRole('button', { name: "To'liq ko'rish" });
      await user.click(toggle);
      expect(row?.textContent).toContain(longText);
    });

    it("Survey blokida Likert ustunlari (shkala, yo'nalish, samarali qiymat) ko'rsatilmaydi", async () => {
      vi.stubGlobal(
        'fetch',
        vi.fn(() =>
          Promise.resolve(
            typedResponse<AssessmentAnswersDto>(
              response([
                surveyAnswer({
                  questionCode: 'SURVEY-Q03',
                  questionType: 'ShortText',
                  textValue: 'Dasturchi bo\'lishni xohlayman',
                }),
              ]),
            ),
          ),
        ),
      );

      renderSection();
      await waitForTables();

      expect(screen.queryByRole('columnheader', { name: 'Shkala' })).not.toBeInTheDocument();
      expect(screen.queryByRole('columnheader', { name: "Yo'nalish" })).not.toBeInTheDocument();
      expect(
        screen.queryByRole('columnheader', { name: 'Shkalaga tushgan qiymat' }),
      ).not.toBeInTheDocument();
      expect(await screen.findByText("Dasturchi bo'lishni xohlayman")).toBeInTheDocument();
    });

    it("soddalashtirilgan jadval — savol, javob, davomiylik, tahrirlar soni (Likert ustunlarisiz)", async () => {
      vi.stubGlobal(
        'fetch',
        vi.fn(() =>
          Promise.resolve(
            typedResponse<AssessmentAnswersDto>(
              response([
                surveyAnswer({
                  questionCode: 'SURVEY-Q04',
                  questionType: 'ShortText',
                  textValue: 'Javob',
                  durationMs: 4200,
                  revisionCount: 2,
                }),
              ]),
            ),
          ),
        ),
      );

      renderSection();
      await waitForTables();

      const row = (await screen.findByText('SURVEY-Q04 savoli')).closest('tr');
      expect(row).toHaveTextContent('4200 ms');
      // Tahrirlar soni ustuni — oxirgi katak, xom `2`.
      expect(row?.querySelectorAll('td')).toHaveLength(4);
      expect(row?.textContent).toContain('2');
    });
  });
});
