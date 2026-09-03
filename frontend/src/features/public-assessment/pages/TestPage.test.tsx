import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import TestPage from './TestPage';
import { readAnswerStore } from '../lib/answerQueue';
import { useSessionStore } from '../store/sessionStore';

const SCALE_LABELS = [
  { value: 1, label: "Umuman qo'shilmayman" },
  { value: 2, label: "Qo'shilmayman" },
  { value: 3, label: 'Bilmadim' },
  { value: 4, label: "Qo'shilaman" },
  { value: 5, label: "To'liq qo'shilaman" },
] satisfies Schemas['PublicScaleLabelDto'][];

function sessionStateBody(
  overrides: Partial<Schemas['GetSessionStateResult']> = {},
): Schemas['GetSessionStateResult'] {
  return {
    assessmentId: 'assessment-1',
    status: 'InProgress',
    student: { firstNameShort: 'Sardor', grade: 9 },
    expiresAt: '2026-09-10T00:00:00Z',
    currentTestCode: 'BIG5',
    // `name`/`estimatedMinutes` — P36: backend `PublicTestSummaryDto`ga qo'shdi, `TestPage`
    // endi test nomini shu yerdan oladi (`sessionStore.testCatalog` OLIB TASHLANDI).
    tests: [
      { code: 'MBTI16', name: '16 tipli shaxsiyat modeli', status: 'Completed', answered: 60, total: 60, order: 1, estimatedMinutes: 9 },
      { code: 'BIG5', name: 'Shaxsiyatning 5 omili', status: 'InProgress', answered: 0, total: 3, order: 2, estimatedMinutes: 8 },
      { code: 'RIASEC', name: 'Kasb qiziqishlari', status: 'Locked', answered: 0, total: 48, order: 3, estimatedMinutes: 7 },
      { code: 'ACTIVITY', name: 'Aktivlik va motivatsiya', status: 'Locked', answered: 0, total: 32, order: 4, estimatedMinutes: 5 },
    ],
    progressPercent: 25,
    hasPersonalityBattery: true,
    ...overrides,
  };
}

function questionsPage(
  overrides: Partial<Schemas['GetTestQuestionsResult']> = {},
): Schemas['GetTestQuestionsResult'] {
  return {
    testCode: 'BIG5',
    page: 1,
    pageSize: 3,
    totalPages: 1,
    totalQuestions: 3,
    scaleLabels: SCALE_LABELS,
    questions: [
      {
        id: 'q1',
        code: 'B5-Q01',
        order: 1,
        text: 'Savol bir',
        type: 'Likert5',
        isRequired: true,
        options: null,
        currentValue: null,
      },
      {
        id: 'q2',
        code: 'B5-Q02',
        order: 2,
        text: 'Savol ikki',
        type: 'Likert5',
        isRequired: true,
        options: null,
        currentValue: null,
      },
      {
        id: 'q3',
        code: 'B5-Q03',
        order: 3,
        text: 'Savol uch',
        type: 'Likert5',
        isRequired: true,
        options: null,
        currentValue: null,
      },
    ],
    ...overrides,
  };
}

interface MockOptions {
  sessionState?: () => Response | Promise<Response>;
  startTest?: (testCode: string) => Response | Promise<Response>;
  questions?: () => Response | Promise<Response>;
  /** Autosave (`POST .../answers`) javobi — P30-2 poygasini boshqarish uchun. */
  answers?: () => Response | Promise<Response>;
}

function mockFetch({ sessionState, startTest, questions, answers }: MockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';

    if (url.includes('/sessions/me')) {
      return Promise.resolve(sessionState ? sessionState() : jsonResponse<'GetSessionStateResult'>(sessionStateBody()));
    }
    if (url.includes('/start') && method === 'POST') {
      const match = /\/tests\/([^/]+)\/start/.exec(url);
      const testCode = match?.[1] ?? 'BIG5';
      return Promise.resolve(
        startTest
          ? startTest(testCode)
          : jsonResponse<'StartTestResult'>({ testCode, status: 'InProgress', pageSize: 3, totalPages: 1 }),
      );
    }
    if (url.includes('/questions')) {
      return Promise.resolve(questions ? questions() : jsonResponse<'GetTestQuestionsResult'>(questionsPage()));
    }
    if (url.includes('/answers') && method === 'POST') {
      return Promise.resolve(
        answers ? answers() : jsonResponse<'SaveAnswersResult'>({ savedCount: 1, answered: 1, total: 3 }),
      );
    }
    if (url.includes('/complete') && method === 'POST') {
      return Promise.resolve(
        jsonResponse<'CompleteTestResult'>({ testCode: 'BIG5', status: 'Completed', nextTestCode: 'RIASEC', allTestsCompleted: false }),
      );
    }
    return Promise.reject(new Error(`unexpected fetch: ${method} ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderTestPage(initialPath = '/t/demo-school/test/BIG5') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialPath]}>
          <Routes>
            <Route path="/t/:slug" element={<div>LANDING_STUB</div>} />
            <Route path="/t/:slug/test/:testCode" element={<TestPage />} />
            <Route path="/t/:slug/test/:testCode/done" element={<div>DONE_STUB</div>} />
            <Route path="/t/:slug/finish" element={<div>FINISH_STUB</div>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function seedSession() {
  useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1');
}

/** `fetch` mock'iga berilgan URL bo'lagini o'z ichiga olgan chaqiruvlar soni. */
function countCalls(fetchMock: ReturnType<typeof mockFetch>, fragment: string): number {
  return fetchMock.mock.calls.filter((call) => String(call[0]).includes(fragment)).length;
}

/** Uchta savolga (`questionsPage()`) javob beradi — oxirgi sahifa to'liq to'ldiriladi. */
async function answerAllThree(user: ReturnType<typeof userEvent.setup>) {
  const q1 = within(screen.getByText('Savol bir').closest('fieldset') as HTMLElement);
  const q2 = within(screen.getByText('Savol ikki').closest('fieldset') as HTMLElement);
  const q3 = within(screen.getByText('Savol uch').closest('fieldset') as HTMLElement);

  await user.click(q1.getByRole('radio', { name: "Qo'shilaman" }));
  await user.click(q2.getByRole('radio', { name: 'Bilmadim' }));
  await user.click(q3.getByRole('radio', { name: "To'liq qo'shilaman" }));
}

describe('TestPage', () => {
  beforeEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  it("sessiya yo'q bo'lsa landing'ga qaytaradi", async () => {
    mockFetch();
    renderTestPage();
    expect(await screen.findByText('LANDING_STUB')).toBeInTheDocument();
  });

  it("test nomi (sessiya holatidan), blok indikatori va savollarni ko'rsatadi", async () => {
    seedSession();
    mockFetch();
    renderTestPage();

    expect(await screen.findByRole('heading', { name: 'Shaxsiyatning 5 omili' })).toBeInTheDocument();
    expect(screen.getByText('2/4 blok')).toBeInTheDocument();
    expect(screen.getByText('Savol bir')).toBeInTheDocument();
    expect(screen.getByText('Savol ikki')).toBeInTheDocument();
    expect(screen.getByText('Savol uch')).toBeInTheDocument();
  });

  it("barcha savolga javob berilmasdan 'Keyingi' bosilsa to'ldirilmagan savolni belgilaydi va sahifani o'zgartirmaydi", async () => {
    seedSession();
    mockFetch();
    const user = userEvent.setup();
    renderTestPage();

    await screen.findByText('Savol bir');
    await user.click(screen.getByRole('button', { name: 'Keyingi' }));

    expect(await screen.findAllByText('Bu savolga javob bering.')).toHaveLength(3);
    expect(screen.getByText('Savol bir')).toBeInTheDocument(); // hali shu sahifada
  });

  it("barcha savolga javob berilib 'Keyingi' bosilsa (oxirgi sahifa) testni yakunlaydi va keyingi blok ekraniga o'tadi", async () => {
    seedSession();
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderTestPage();

    await screen.findByText('Savol bir');

    const q1 = within(screen.getByText('Savol bir').closest('fieldset') as HTMLElement);
    const q2 = within(screen.getByText('Savol ikki').closest('fieldset') as HTMLElement);
    const q3 = within(screen.getByText('Savol uch').closest('fieldset') as HTMLElement);

    await user.click(q1.getByRole('radio', { name: "Qo'shilaman" }));
    await user.click(q2.getByRole('radio', { name: 'Bilmadim' }));
    await user.click(q3.getByRole('radio', { name: "To'liq qo'shilaman" }));

    await user.click(screen.getByRole('button', { name: 'Keyingi' }));

    expect(await screen.findByText('DONE_STUB')).toBeInTheDocument();

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining('/tests/BIG5/complete'),
        expect.objectContaining({ method: 'POST' }),
      );
    });
  });

  /**
   * P30-2 (BLOKLOVCHI) regressiyasi. Oxirgi sahifada tez javob berilib darhol "Keyingi"
   * bosilganda `POST .../complete` autosave paketidan OLDIN yetib borar edi va backend
   * `400 VALIDATION_ERROR (unansweredCount)` qaytarardi — o'quvchi testni yakunlay olmasdi.
   * Bu yerda autosave javobi ataylab "havoda" ushlab turiladi: `complete` u tugagunicha
   * YUBORILMASLIGI shart.
   */
  it("oxirgi sahifada `complete` faqat autosave serverga yetib BORGANDAN keyin yuboriladi (P30-2)", async () => {
    seedSession();
    let releaseAnswers: (() => void) | undefined;
    const answersGate = new Promise<void>((resolve) => {
      releaseAnswers = resolve;
    });
    const fetchMock = mockFetch({
      answers: async () => {
        await answersGate;
        return jsonResponse<'SaveAnswersResult'>({ savedCount: 3, answered: 3, total: 3 });
      },
    });
    const user = userEvent.setup();
    renderTestPage();

    await screen.findByText('Savol bir');
    await answerAllThree(user);

    const nextButton = screen.getByRole('button', { name: 'Keyingi' });
    await user.click(nextButton);

    // Autosave so'rovi yo'lga chiqdi, lekin hali tugamadi.
    await waitFor(() => {
      expect(countCalls(fetchMock, '/answers')).toBeGreaterThan(0);
    });
    expect(countCalls(fetchMock, '/complete')).toBe(0);
    // Foydalanuvchi kutayotganini KO'RADI va ikki marta bosa OLMAYDI.
    expect(nextButton).toBeDisabled();
    expect(nextButton).toHaveAttribute('aria-busy', 'true');
    await user.click(nextButton);
    expect(countCalls(fetchMock, '/complete')).toBe(0);

    releaseAnswers?.();

    expect(await screen.findByText('DONE_STUB')).toBeInTheDocument();
    expect(countCalls(fetchMock, '/complete')).toBe(1);
  });

  it("autosave yiqilsa `complete` YUBORILMAYDI, xato xabari chiqadi va javoblar navbatda qoladi", async () => {
    seedSession();
    const fetchMock = mockFetch({ answers: () => Promise.reject(new Error('network down')) });
    const user = userEvent.setup();
    renderTestPage();

    await screen.findByText('Savol bir');
    await answerAllThree(user);
    await user.click(screen.getByRole('button', { name: 'Keyingi' }));

    expect(await screen.findByText('Javoblar serverga yuborilmadi')).toBeInTheDocument();
    expect(countCalls(fetchMock, '/complete')).toBe(0);
    expect(screen.getByText('Savol bir')).toBeInTheDocument(); // hali shu sahifada

    // Javoblar YO'QOLMAGAN — mahalliy navbatda `pending: true` holida turibdi (P21).
    const store = readAnswerStore();
    expect(store['q1']).toMatchObject({ value: 4, pending: true });
    expect(store['q3']).toMatchObject({ value: 5, pending: true });
  });

  it("sessiya muddati tugagan bo'lsa (410) landing'ga qaytaradi", async () => {
    seedSession();
    mockFetch({ sessionState: () => problemResponse('SESSION_EXPIRED', 410) });
    renderTestPage();

    expect(await screen.findByText('LANDING_STUB')).toBeInTheDocument();
    await waitFor(() => {
      expect(useSessionStore.getState().sessionToken).toBeNull();
    });
  });

  it("409 TEST_NOT_UNLOCKED kelsa sessiyaning joriy testiga yo'naltiradi", async () => {
    seedSession();
    // RIASEC hali ochilmagan (409) — faqat shu testCode uchun; BIG5 (haqiqiy joriy test)
    // muvaffaqiyatli boshlanadi, aks holda qayta yo'naltirish siklga aylanib qolardi.
    mockFetch({
      startTest: (testCode) =>
        testCode === 'RIASEC'
          ? problemResponse('TEST_NOT_UNLOCKED', 409)
          : jsonResponse<'StartTestResult'>({ testCode, status: 'InProgress', pageSize: 3, totalPages: 1 }),
    });
    renderTestPage('/t/demo-school/test/RIASEC');

    // `sessionStateBody().currentTestCode` — 'BIG5'
    expect(await screen.findByRole('heading', { name: 'Shaxsiyatning 5 omili' })).toBeInTheDocument();
  });

  it("sahifa yangilanganda (resume) allaqachon javob berilgan savollar 'currentValue' orqali joyida ko'rinadi", async () => {
    seedSession();
    mockFetch({
      questions: () =>
        jsonResponse<'GetTestQuestionsResult'>(
          questionsPage({
            questions: [
              {
                id: 'q1',
                code: 'B5-Q01',
                order: 1,
                text: 'Savol bir',
                type: 'Likert5',
                isRequired: true,
                options: null,
                currentValue: 5,
              },
              {
                id: 'q2',
                code: 'B5-Q02',
                order: 2,
                text: 'Savol ikki',
                type: 'Likert5',
                isRequired: true,
                options: null,
                currentValue: null,
              },
              {
                id: 'q3',
                code: 'B5-Q03',
                order: 3,
                text: 'Savol uch',
                type: 'Likert5',
                isRequired: true,
                options: null,
                currentValue: null,
              },
            ],
          }),
        ),
    });
    renderTestPage();

    await screen.findByText('Savol bir');
    const q1 = within(screen.getByText('Savol bir').closest('fieldset') as HTMLElement);
    expect(q1.getByRole('radio', { name: "To'liq qo'shilaman" })).toBeChecked();
  });
});
