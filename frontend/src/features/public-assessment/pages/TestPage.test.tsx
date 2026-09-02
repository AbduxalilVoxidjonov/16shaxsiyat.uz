import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import TestPage from './TestPage';
import { useSessionStore } from '../store/sessionStore';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function problemResponse(code: string, status: number): Response {
  return jsonResponse({ code, title: 'Xato', status, type: `https://studentroadmap/errors/${code}` }, status);
}

const SCALE_LABELS = [
  { value: 1, label: "Umuman qo'shilmayman" },
  { value: 2, label: "Qo'shilmayman" },
  { value: 3, label: 'Bilmadim' },
  { value: 4, label: "Qo'shilaman" },
  { value: 5, label: "To'liq qo'shilaman" },
];

function sessionStateBody(overrides: Record<string, unknown> = {}) {
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
    ...overrides,
  };
}

function questionsPage(overrides: Record<string, unknown> = {}) {
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
}

function mockFetch({ sessionState, startTest, questions }: MockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';

    if (url.includes('/sessions/me')) {
      return Promise.resolve(sessionState ? sessionState() : jsonResponse(sessionStateBody()));
    }
    if (url.includes('/start') && method === 'POST') {
      const match = /\/tests\/([^/]+)\/start/.exec(url);
      const testCode = match?.[1] ?? 'BIG5';
      return Promise.resolve(
        startTest
          ? startTest(testCode)
          : jsonResponse({ testCode, status: 'InProgress', pageSize: 3, totalPages: 1 }),
      );
    }
    if (url.includes('/questions')) {
      return Promise.resolve(questions ? questions() : jsonResponse(questionsPage()));
    }
    if (url.includes('/answers') && method === 'POST') {
      return Promise.resolve(jsonResponse({ savedCount: 1, answered: 1, total: 3 }));
    }
    if (url.includes('/complete') && method === 'POST') {
      return Promise.resolve(
        jsonResponse({ testCode: 'BIG5', status: 'Completed', nextTestCode: 'RIASEC', allTestsCompleted: false }),
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
          : jsonResponse({ testCode, status: 'InProgress', pageSize: 3, totalPages: 1 }),
    });
    renderTestPage('/t/demo-school/test/RIASEC');

    // `sessionStateBody().currentTestCode` — 'BIG5'
    expect(await screen.findByRole('heading', { name: 'Shaxsiyatning 5 omili' })).toBeInTheDocument();
  });

  it("sahifa yangilanganda (resume) allaqachon javob berilgan savollar 'currentValue' orqali joyida ko'rinadi", async () => {
    seedSession();
    mockFetch({
      questions: () =>
        jsonResponse(
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
