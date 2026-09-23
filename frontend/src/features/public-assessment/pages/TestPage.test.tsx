import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, typedResponse, type Schemas } from '@/test/apiMock';
import TestPage from './TestPage';
import { readAnswerStore } from '../lib/answerQueue';
import { useSessionStore } from '../store/sessionStore';
import type { BranchingQuestion, BranchingTestQuestionsData, PublicSection } from '@/shared/api/branchingTypes';

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

// docs/18-tarmoqlanuvchi-sorovnoma.md §6.2 — bo'lim-qadam rejimi. Fikstura namunaviy oqimga
// (docs/18 §0) o'xshaydi: 1-bo'lim (hamma), 1.6-filtr → 2-A/2-B/2-C, 3-bo'lim (hamma).
function branchingQuestion(
  overrides: Partial<BranchingQuestion> & Pick<BranchingQuestion, 'id' | 'code' | 'order' | 'text' | 'type' | 'sectionId'>,
): BranchingQuestion {
  return {
    isRequired: true,
    options: null,
    currentValue: null,
    placeholder: null,
    inputPattern: null,
    maxLength: null,
    minSelections: null,
    maxSelections: null,
    visibility: null,
    currentText: null,
    currentValues: null,
    ...overrides,
  };
}

const BRANCHING_SECTIONS: PublicSection[] = [
  { id: 's1', code: 'S1', title: 'Asosiy ma\'lumotlar', description: null, order: 1, visibility: null },
  {
    id: 's2a',
    code: 'S2A',
    title: 'Intellect o\'quvchilari uchun',
    description: null,
    order: 2,
    visibility: { match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [1] }] },
  },
  {
    id: 's2b',
    code: 'S2B',
    title: 'Boshqa markaz uchun',
    description: null,
    order: 3,
    visibility: { match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [2] }] },
  },
  {
    id: 's2c',
    code: 'S2C',
    title: 'Potensial lidlar uchun',
    description: null,
    order: 4,
    visibility: { match: 'All', conditions: [{ questionCode: 'Q1_6', operator: 'Equals', values: [3] }] },
  },
  { id: 's3', code: 'S3', title: 'Yakuniy bo\'lim', description: null, order: 5, visibility: null },
];

const BRANCHING_QUESTIONS: BranchingQuestion[] = [
  branchingQuestion({ id: 'q1', code: 'Q1_1', order: 1, sectionId: 's1', text: 'F.I.Sh.', type: 'ShortText' }),
  branchingQuestion({
    id: 'q6',
    code: 'Q1_6',
    order: 6,
    sectionId: 's1',
    text: "Qo'shimcha kursga qatnashasizmi?",
    type: 'SingleChoice',
    options: [
      { id: 'o1', text: 'Intellect', value: 1, order: 1 },
      { id: 'o2', text: 'Boshqa markaz', value: 2, order: 2 },
      { id: 'o3', text: 'Qatnashmayman', value: 3, order: 3 },
    ],
  }),
  branchingQuestion({ id: 'q2a', code: 'Q2A_1', order: 10, sectionId: 's2a', text: '2-A savoli', type: 'ShortText' }),
  branchingQuestion({ id: 'q2b', code: 'Q2B_1', order: 20, sectionId: 's2b', text: '2-B savoli', type: 'ShortText' }),
  branchingQuestion({ id: 'q2c', code: 'Q2C_1', order: 30, sectionId: 's2c', text: '2-C savoli', type: 'ShortText' }),
  branchingQuestion({
    id: 'q3',
    code: 'Q3_1',
    order: 40,
    sectionId: 's3',
    text: '3-bo\'lim savoli',
    type: 'ShortText',
    isRequired: false,
  }),
];

function branchingQuestionsPage(
  overrides: Partial<BranchingTestQuestionsData> = {},
): BranchingTestQuestionsData {
  return {
    testCode: 'SURVEY',
    page: 1,
    pageSize: 60,
    totalPages: 1,
    totalQuestions: BRANCHING_QUESTIONS.length,
    scaleLabels: null,
    sections: BRANCHING_SECTIONS,
    questions: BRANCHING_QUESTIONS,
    ...overrides,
  };
}

function branchingSessionStateBody(
  overrides: Partial<Schemas['GetSessionStateResult']> = {},
): Schemas['GetSessionStateResult'] {
  return {
    assessmentId: 'assessment-1',
    status: 'InProgress',
    student: { firstNameShort: 'Sardor', grade: 9 },
    expiresAt: '2026-09-10T00:00:00Z',
    currentTestCode: 'SURVEY',
    tests: [
      { code: 'SURVEY', name: "So'rovnoma", status: 'InProgress', answered: 0, total: 6, order: 1, estimatedMinutes: 5 },
    ],
    progressPercent: 0,
    hasPersonalityBattery: false,
    ...overrides,
  };
}

describe("TestPage — bo'lim-qadam rejimi (docs/18)", () => {
  beforeEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  function mockBranchingFetch(overrides: MockOptions = {}) {
    return mockFetch({
      sessionState: () => jsonResponse<'GetSessionStateResult'>(branchingSessionStateBody()),
      startTest: (testCode) =>
        jsonResponse<'StartTestResult'>({ testCode, status: 'InProgress', pageSize: 60, totalPages: 1 }),
      questions: () => typedResponse<BranchingTestQuestionsData>(branchingQuestionsPage()),
      ...overrides,
    });
  }

  function renderBranching() {
    return renderTestPage('/t/demo-school/test/SURVEY');
  }

  async function fillFirstSection(user: ReturnType<typeof userEvent.setup>, optionName: string) {
    await screen.findByText('F.I.Sh.');
    await user.type(screen.getByLabelText(/F\.I\.Sh\./), 'Aliyev Vali');
    await user.click(screen.getByRole('radio', { name: optionName }));
    await user.click(screen.getByRole('button', { name: 'Keyingi' }));
  }

  it("1.6 = 'Intellect' tanlansa 2-A ko'rinadi, 2-B/2-C ko'rinmaydi, oxirida 3-bo'lim bor", async () => {
    seedSession();
    mockBranchingFetch();
    const user = userEvent.setup();
    renderBranching();

    await fillFirstSection(user, 'Intellect');

    expect(await screen.findByRole('heading', { name: "Intellect o'quvchilari uchun" })).toBeInTheDocument();
    expect(screen.getByText('2-A savoli')).toBeInTheDocument();
    expect(screen.queryByText('2-B savoli')).not.toBeInTheDocument();
    expect(screen.queryByText('2-C savoli')).not.toBeInTheDocument();

    await user.type(screen.getByLabelText(/2-A savoli/), 'javobim');
    await user.click(screen.getByRole('button', { name: 'Keyingi' }));

    expect(await screen.findByRole('heading', { name: "Yakuniy bo'lim" })).toBeInTheDocument();
    expect(screen.getByText("3-bo'lim savoli")).toBeInTheDocument();
  });

  it("so'rovnoma faqat oldinga yuriladi: keyingi bo'limga o'tilgach 'Orqaga' tugmasi yo'q", async () => {
    seedSession();
    mockBranchingFetch();
    const user = userEvent.setup();
    renderBranching();

    await screen.findByText('F.I.Sh.');
    expect(screen.queryByRole('button', { name: 'Orqaga' })).not.toBeInTheDocument();

    await fillFirstSection(user, 'Intellect');

    expect(await screen.findByRole('heading', { name: "Intellect o'quvchilari uchun" })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Orqaga' })).not.toBeInTheDocument();
  });

  it("1.6 = 'Boshqa markaz' tanlansa 2-B ko'rinadi, 2-A/2-C ko'rinmaydi, oxirida 3-bo'lim bor", async () => {
    seedSession();
    mockBranchingFetch();
    const user = userEvent.setup();
    renderBranching();

    await fillFirstSection(user, 'Boshqa markaz');

    expect(await screen.findByRole('heading', { name: 'Boshqa markaz uchun' })).toBeInTheDocument();
    expect(screen.getByText('2-B savoli')).toBeInTheDocument();
    expect(screen.queryByText('2-A savoli')).not.toBeInTheDocument();
    expect(screen.queryByText('2-C savoli')).not.toBeInTheDocument();

    await user.type(screen.getByLabelText(/2-B savoli/), 'javobim');
    await user.click(screen.getByRole('button', { name: 'Keyingi' }));

    expect(await screen.findByRole('heading', { name: "Yakuniy bo'lim" })).toBeInTheDocument();
  });

  it("1.6 = 'Qatnashmayman' tanlansa 2-C ko'rinadi, 2-A/2-B ko'rinmaydi, oxirida 3-bo'lim bor", async () => {
    seedSession();
    mockBranchingFetch();
    const user = userEvent.setup();
    renderBranching();

    await fillFirstSection(user, 'Qatnashmayman');

    expect(await screen.findByRole('heading', { name: 'Potensial lidlar uchun' })).toBeInTheDocument();
    expect(screen.getByText('2-C savoli')).toBeInTheDocument();
    expect(screen.queryByText('2-A savoli')).not.toBeInTheDocument();
    expect(screen.queryByText('2-B savoli')).not.toBeInTheDocument();

    await user.type(screen.getByLabelText(/2-C savoli/), 'javobim');
    await user.click(screen.getByRole('button', { name: 'Keyingi' }));

    expect(await screen.findByRole('heading', { name: "Yakuniy bo'lim" })).toBeInTheDocument();
  });

  it("bo'sh (ixtiyoriy) 3-savolni to'ldirmasdan 'Keyingi' bosilsa testni yakunlaydi", async () => {
    seedSession();
    const fetchMock = mockBranchingFetch();
    const user = userEvent.setup();
    renderBranching();

    await fillFirstSection(user, 'Intellect');
    await screen.findByRole('heading', { name: "Intellect o'quvchilari uchun" });
    await user.type(screen.getByLabelText(/2-A savoli/), 'javobim');
    await user.click(screen.getByRole('button', { name: 'Keyingi' }));

    await screen.findByRole('heading', { name: "Yakuniy bo'lim" });
    await user.click(screen.getByRole('button', { name: 'Keyingi' })); // 3.1 ixtiyoriy — bo'sh qoldiriladi

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining('/tests/SURVEY/complete'),
        expect.objectContaining({ method: 'POST' }),
      );
    });
  });

  it("hozir yashirin bo'limga tegishli, oldindan navbatga qo'yilgan javob so'rovga QO'SHILMAYDI (docs/18 §6.2)", async () => {
    seedSession();
    // `q2b` (2-B bo'limi) — hali yuborilmagan, mahalliy navbatda turgan javob deb faraz
    // qilamiz (masalan oldingi urinishda). Joriy render 1.6 ni HALI javobsiz boshlaydi, ya'ni
    // na 2-A, na 2-B ko'rinadi.
    localStorage.setItem(
      'shaxsiyat.pendingAnswers',
      JSON.stringify({
        q2b: { testCode: 'SURVEY', questionId: 'q2b', text: 'eski javob', durationMs: 10, pending: true },
      }),
    );
    const fetchMock = mockBranchingFetch();
    const user = userEvent.setup();
    renderBranching();

    // 1.6 = Intellect (1) — 2-A ko'rinadi, 2-B YO'Q. `q2b` hamon ko'rinmas bo'lib qoladi.
    await fillFirstSection(user, 'Intellect');
    await screen.findByRole('heading', { name: "Intellect o'quvchilari uchun" });

    await waitFor(() => {
      expect(countCalls(fetchMock, '/answers')).toBeGreaterThan(0);
    });

    for (const call of fetchMock.mock.calls) {
      const [url, init] = call as [string, RequestInit | undefined];
      if (!String(url).includes('/answers') || init?.method !== 'POST') continue;
      const body = JSON.parse(init.body as string) as { answers: { questionId: string }[] };
      expect(body.answers.map((a) => a.questionId)).not.toContain('q2b');
    }

    // Javob YO'QOLMAGAN — hali ham mahalliy navbatda turibdi (keyin 2-B qayta ko'rinsa yuboriladi).
    expect(readAnswerStore()['q2b']).toMatchObject({ text: 'eski javob' });
  });

  it("'Boshqa (kiriting)' varianti tanlansa ostida matn maydoni paydo bo'ladi, boshqa variant tanlansa yo'qoladi", async () => {
    seedSession();
    const singleSection: PublicSection[] = [
      { id: 's1', code: 'S1', title: "Qiziqish", description: null, order: 1, visibility: null },
    ];
    const questions: BranchingQuestion[] = [
      branchingQuestion({
        id: 'fav',
        code: 'FAV',
        order: 1,
        sectionId: 's1',
        text: 'Sevimli faningiz?',
        type: 'SingleChoice',
        options: [
          { id: 'o1', text: 'Matematika', value: 1, order: 1 },
          { id: 'o2', text: 'Boshqa (kiriting)', value: 2, order: 2 },
        ],
      }),
      branchingQuestion({
        id: 'favOther',
        code: 'FAV_OTHER',
        order: 2,
        sectionId: 's1',
        text: 'Qaysi fan?',
        type: 'ShortText',
        isRequired: false,
        visibility: { match: 'All', conditions: [{ questionCode: 'FAV', operator: 'Equals', values: [2] }] },
      }),
    ];
    mockBranchingFetch({
      questions: () =>
        typedResponse<BranchingTestQuestionsData>(
          branchingQuestionsPage({ sections: singleSection, questions, totalQuestions: 2 }),
        ),
    });
    const user = userEvent.setup();
    renderBranching();

    await screen.findByText('Sevimli faningiz?');
    expect(screen.queryByText('Qaysi fan?')).not.toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Boshqa (kiriting)' }));
    expect(await screen.findByText('Qaysi fan?')).toBeInTheDocument();

    await user.click(screen.getByRole('radio', { name: 'Matematika' }));
    await waitFor(() => {
      expect(screen.queryByText('Qaysi fan?')).not.toBeInTheDocument();
    });
  });

  /**
   * P52 BLOKLOVCHI regressiyasi (jonliqda topilgan, tuzatildi 6f81dc0). Matn savoliga bitta
   * harf yozilgach keyingilari kirmasdi: bo'lim sarlavhasiga fokus ko'chiruvchi effektning
   * bog'liqligida `visibility` (`draftAnswers`ga tayanadigan `useMemo`, HAR harfda yangi
   * obyekt) turar edi — effekt har harfda qayta ishga tushib fokusni inputdan sarlavhaga
   * olib qochardi.
   *
   * MUHIM: `user.type()` EMAS — u sintetik hodisalarni to'g'ridan-to'g'ri nishonlangan
   * elementga yo'naltiradi va `document.activeElement`ga E'TIBOR BERMAYDI, shu sabab bu
   * xatoni ushlamas edi (E2E'dagi `fill()` bilan bir xil ko'r nuqta). `user.keyboard()`
   * — HAQIQIY klaviatura kabi HOZIRGI FOKUSDAGI elementga yo'naltiradi, shu sabab fokus
   * boshqa joyga o'g'irlansa keyingi harflar YO'QOLADI — aynan shu narsa tekshiriladi.
   *
   * Mutatsiya sinovi (qo'lda tasdiqlangan): `TestPage.tsx`dagi fokus effektining
   * bog'liqligini vaqtincha `[currentSectionId, visibility]`ga qaytarsangiz bu test YIQILADI.
   */
  it("matn maydoniga ketma-ket yozilganda fokus MAYDONDA qoladi va butun matn kiradi (P52, 6f81dc0)", async () => {
    seedSession();
    mockBranchingFetch();
    const user = userEvent.setup();
    renderBranching();

    await screen.findByText('F.I.Sh.');
    const input = screen.getByLabelText(/F\.I\.Sh\./);
    await user.click(input);
    expect(input).toHaveFocus();

    const text = 'Aliyev Vali Davronovich';
    for (const char of text) {
      await user.keyboard(char);
      // Har harfdan keyin fokus HAMON shu maydonda — aks holda keyingi harflar yo'qoladi.
      expect(input).toHaveFocus();
    }

    expect(input).toHaveValue(text);
  });
});
