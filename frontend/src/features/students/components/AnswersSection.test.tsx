import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { jsonResponse } from '@/test/apiMock';
import { AnswersSection } from './AnswersSection';
import type { AssessmentAnswersDto, RawAnswerDto } from '../model/profileTypes';

/**
 * `docs/11` A-5 profil bo'limi — savolma-savol javoblar. Eng muhim tekshiruv:
 * TESKARI savolga berilgan `5` jadvalda `1` bo'lib ko'rinishi (`docs/03` §1). Xom `5`
 * psixologni butunlay teskari xulosaga olib boradi.
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
    durationMs: 3000,
    revisionCount: 0,
    answeredAt: '2026-08-30T09:10:00Z',
    questionType: 'Likert5',
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

function renderSection() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <AnswersSection assessmentId="assessment-1" />
    </QueryClientProvider>,
  );
}

/** Bo'lim yopiq boshlanadi — ochib, birinchi test blokini ham ochamiz. */
async function openSectionAndFirstGroup(testCode = 'BIG5') {
  const user = userEvent.setup();
  await user.click(screen.getByRole('button', { name: /Savolma-savol javoblar/i }));
  const groupButton = await screen.findByRole('button', { name: new RegExp(testCode) });
  await user.click(groupButton);
  return user;
}

describe('AnswersSection', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("bo'lim yopiq bo'lganda so'rov YUBORMAYDI (190 savol profil yuklanishini og'irlashtirmaydi)", () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    renderSection();

    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('⚠️ teskari savolga berilgan 5 ni shkalaga tushgan 1 sifatida ko\'rsatadi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          jsonResponse<'AdminAssessmentAnswersDto'>(
            response([
              answer({ questionCode: 'BIG5-Q01', scaleDirection: -1, rawValue: 5, effectiveValue: 1 }),
              answer({ questionCode: 'BIG5-Q02', scaleDirection: 1, rawValue: 4, effectiveValue: 4 }),
            ]),
          ),
        ),
      ),
    );

    renderSection();
    await openSectionAndFirstGroup();

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
          jsonResponse<'AdminAssessmentAnswersDto'>(response([answer({ questionCode: 'BIG5-Q01', rawValue: 5, effectiveValue: 5 })])),
        ),
      ),
    );

    renderSection();
    await openSectionAndFirstGroup();

    expect(await screen.findByText("To'liq qo'shilaman")).toBeInTheDocument();
  });

  it('tez javobni belgilaydi, chegaraning O\'ZINI (900 ms) belgilamaydi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          jsonResponse<'AdminAssessmentAnswersDto'>(
            response([
              answer({ questionCode: 'BIG5-Q01', durationMs: 899, isFastAnswer: true }),
              answer({ questionCode: 'BIG5-Q02', durationMs: 900, isFastAnswer: false }),
            ]),
          ),
        ),
      ),
    );

    renderSection();
    await openSectionAndFirstGroup();

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
          jsonResponse<'AdminAssessmentAnswersDto'>(
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
    await openSectionAndFirstGroup();

    const markedRow = (await screen.findByText('BIG5-Q01 savoli')).closest('tr');
    expect(markedRow).toHaveTextContent('1-bir xil javob bloki');
    expect(markedRow?.className).toContain('bg-warning-50');

    const cleanRow = screen.getByText('BIG5-Q13 savoli').closest('tr');
    expect(cleanRow).not.toHaveTextContent('bir xil javob bloki');

    // Sessiya darajasidagi signal — chegara (12) backend'dan kelgan `thresholds` dan.
    expect(screen.getByText(/1 ta bir xil javob bloki \(12 ta ketma-ket\)/)).toBeInTheDocument();
  });

  it("shkala nomini ko'rsatadi va sessiya signallarini bo'lim boshida beradi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          jsonResponse<'AdminAssessmentAnswersDto'>(
            response([answer({ questionCode: 'BIG5-Q01' })], {
              session: {
                answeredCount: 1,
                fastAnswerCount: 0,
                straightLiningBlockCount: 0,
                allSameAnswer: true,
                shortSession: true,
                totalDurationSeconds: 120,
                reliabilityScore: 30,
                reliabilityFlag: 'Unreliable',
              },
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
    await openSectionAndFirstGroup();

    expect(await screen.findByText(/Barcha javob bir xil/)).toBeInTheDocument();
    expect(screen.getByText(/Sessiya 6 daqiqadan qisqa/)).toBeInTheDocument();
    expect(screen.getByText(/Ishonchlilik: 30/)).toBeInTheDocument();
    expect(screen.getByText(/farq 41%/)).toBeInTheDocument();
    // Shkala nomi (`Artistik` naqshi) — jadvalda kod emas, NOM asosiy.
    expect(screen.getAllByText('Vijdonlilik').length).toBeGreaterThan(0);
  });

  it('"faqat belgilanganlar" filtri toza javoblarni yashiradi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          jsonResponse<'AdminAssessmentAnswersDto'>(
            response([
              answer({ questionCode: 'BIG5-Q01', durationMs: 500, isFastAnswer: true }),
              answer({ questionCode: 'BIG5-Q02' }),
            ]),
          ),
        ),
      ),
    );

    renderSection();
    const user = await openSectionAndFirstGroup();

    expect(await screen.findByText('BIG5-Q02 savoli')).toBeInTheDocument();

    await user.click(screen.getByLabelText('Faqat belgilanganlar'));

    await waitFor(() => {
      expect(screen.queryByText('BIG5-Q02 savoli')).not.toBeInTheDocument();
    });
    expect(screen.getByText('BIG5-Q01 savoli')).toBeInTheDocument();
  });
});
