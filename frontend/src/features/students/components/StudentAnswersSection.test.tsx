import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { typedResponse } from '@/test/apiMock';
import type { AssessmentAnswersDto, RawAnswerDto } from '@/shared/api/assessmentAnswersTypes';
import type { AssessmentSummaryDto } from '../model/profileTypes';
import { StudentAnswersSection } from './StudentAnswersSection';

function summary(overrides: Partial<AssessmentSummaryDto> & { id: string }): AssessmentSummaryDto {
  return {
    status: 'Completed',
    startedAt: '2026-09-01T09:00:00Z',
    completedAt: '2026-09-01T09:30:00Z',
    durationMinutes: 30,
    reliabilityScore: null,
    reliabilityFlag: null,
    isLatest: false,
    programNameUz: "Maktab o'quvchilari uchun so'rovnoma",
    ...overrides,
  };
}

function answer(assessmentId: string, count: number): RawAnswerDto[] {
  return Array.from({ length: count }, (_, index) => ({
    questionId: `${assessmentId}-q${String(index)}`,
    questionCode: `${assessmentId}-Q${String(index)}`,
    testCode: 'INTELLECT-SURVEY',
    testNameUz: "Maktab o'quvchilari uchun so'rovnoma",
    questionText: `${assessmentId} savoli ${String(index)}`,
    rawValue: null,
    selectedOptionText: 'Variant',
    selectedOptionTexts: null,
    selectedValues: null,
    textValue: null,
    durationMs: 2000,
    revisionCount: 0,
    answeredAt: '2026-09-01T09:10:00Z',
    questionType: 'SingleChoice',
    scoringMode: 'Survey',
    scale: 'SURVEY',
    scaleNameUz: null,
    scaleDirection: 1,
    weight: 1,
    effectiveValue: null,
    isFastAnswer: null,
    straightLiningBlockIndex: null,
  }));
}

const ANSWER_COUNTS: Record<string, number> = { new: 2, old: 3 };

function stubFetch() {
  const fetchMock = vi.fn((input: RequestInfo | URL) => {
    const url = String(input);
    const id = /assessments\/([^/]+)\/answers/.exec(url)?.[1] ?? '';
    return Promise.resolve(
      typedResponse<AssessmentAnswersDto>({
        answers: answer(id, ANSWER_COUNTS[id] ?? 1),
        session: {
          answeredCount: 0,
          fastAnswerCount: 0,
          straightLiningBlockCount: 0,
          allSameAnswer: false,
          shortSession: false,
          totalDurationSeconds: null,
          reliabilityScore: null,
          reliabilityFlag: null,
        },
        scales: [],
        thresholds: {
          fastAnswerDurationMs: 900,
          straightLiningMinRunLength: 12,
          shortSessionMinutes: 6,
        },
      }),
    );
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function answerCalls(fetchMock: ReturnType<typeof stubFetch>, id: string) {
  return fetchMock.mock.calls.filter((call) => String(call[0]).includes(`/assessments/${id}/answers`));
}

function renderSection(assessments: AssessmentSummaryDto[], latestId: string | null) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <StudentAnswersSection assessments={assessments} latestAssessmentId={latestId} />
    </QueryClientProvider>,
  );
}

describe('StudentAnswersSection', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("bitta urinish — hozirgidek: yig'ish tugmasisiz, javoblar doim ochiq", async () => {
    stubFetch();
    renderSection([summary({ id: 'new', isLatest: true })], 'new');

    expect(await screen.findByText('new savoli 0')).toBeInTheDocument();
    expect(screen.queryByRole('button', { expanded: true })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { expanded: false })).not.toBeInTheDocument();
    expect(screen.queryByText(/urinish/)).not.toBeInTheDocument();
  });

  it("2+ urinish — so'nggisi ochiq, eskisi yig'ilgan; bosganda ochiladi va javoblari SHUNDA yuklanadi", async () => {
    const fetchMock = stubFetch();
    renderSection(
      [
        summary({
          id: 'new',
          isLatest: true,
          startedAt: '2026-09-20T10:00:00Z',
          completedAt: '2026-09-20T10:25:00Z',
        }),
        summary({ id: 'old', startedAt: '2026-09-01T09:00:00Z', completedAt: '2026-09-01T09:30:00Z' }),
      ],
      'new',
    );

    const newest = await screen.findByRole('button', { name: /2-urinish · 20\.09\.2026 10:25/ });
    const oldest = screen.getByRole('button', { name: /1-urinish · 01\.09\.2026 09:30/ });
    expect(newest).toHaveAttribute('aria-expanded', 'true');
    expect(oldest).toHaveAttribute('aria-expanded', 'false');
    expect(newest).toHaveAccessibleName(expect.stringContaining("Maktab o'quvchilari uchun so'rovnoma"));

    // So'nggi urinish javoblari va soni ko'rinadi, blok sarlavhasida test nomi.
    expect(await screen.findByText('new savoli 0')).toBeInTheDocument();
    expect(await screen.findByRole('button', { name: /\(2 ta javob\)/ })).toBe(newest);
    const panel = document.getElementById(newest.getAttribute('aria-controls') ?? '');
    expect(panel).not.toBeNull();
    expect(
      within(panel!).getByRole('heading', {
        level: 5,
        name: "Maktab o'quvchilari uchun so'rovnoma (2 ta javob)",
      }),
    ).toBeInTheDocument();

    // Yig'ilgan urinish uchun so'rov YUBORILMAGAN (lazy).
    expect(answerCalls(fetchMock, 'old')).toHaveLength(0);
    expect(screen.queryByText('old savoli 0')).not.toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(oldest);

    expect(oldest).toHaveAttribute('aria-expanded', 'true');
    expect(await screen.findByText('old savoli 2')).toBeInTheDocument();
    expect(answerCalls(fetchMock, 'old')).toHaveLength(1);
    expect(oldest).toHaveAccessibleName(expect.stringContaining('(3 ta javob)'));

    // Klaviatura bilan yopiladi.
    newest.focus();
    await user.keyboard('{Enter}');
    expect(newest).toHaveAttribute('aria-expanded', 'false');
    expect(screen.queryByText('new savoli 0')).not.toBeInTheDocument();
  });

  it("yakunlanmagan sessiyalar urinish sifatida sanalmaydi (1 yakunlangan + 1 tugallanmagan = bitta blok)", async () => {
    stubFetch();
    renderSection(
      [
        summary({ id: 'new', isLatest: true, status: 'InProgress', completedAt: null }),
        summary({ id: 'old' }),
      ],
      'new',
    );

    expect(await screen.findByText('new savoli 0')).toBeInTheDocument();
    expect(screen.queryByRole('button', { expanded: false })).not.toBeInTheDocument();
  });
});
