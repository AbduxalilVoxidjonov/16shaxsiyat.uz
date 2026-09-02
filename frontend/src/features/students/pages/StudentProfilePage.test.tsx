import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import axe from 'axe-core';
import { ToastProvider } from '@/shared/ui/Toast';
import StudentProfilePage from './StudentProfilePage';

/** `widgets/a11y.test.tsx`dagi bilan bir xil sabab — jsdom'da ma'nosiz qoidalar o'chirilgan. */
async function expectNoAxeViolations(container: Element): Promise<void> {
  const results = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
  expect(results.violations).toEqual([]);
}

const STUDENT = {
  id: 'student-1',
  fullName: 'Aliyev Sardor Bekzodovich',
  birthDate: '2010-04-17',
  age: 16,
  gender: 'Male',
  grade: 9,
  classLetter: 'B',
  phone: '+998901234567',
  parentPhone: '+998911112233',
  email: null,
  school: { id: 'school-1', name: "12-son maktab, Qo'qon" },
  consentGivenAt: '2026-08-01T10:00:00Z',
  createdAt: '2026-08-01T10:00:00Z',
};

const ASSESSMENT_SUMMARY = {
  id: 'assessment-1',
  status: 'Analyzed',
  startedAt: '2026-08-30T09:00:00Z',
  completedAt: '2026-08-30T09:29:00Z',
  durationMinutes: 29,
  reliabilityScore: 82.5,
  reliabilityFlag: 'Reliable',
  isLatest: true,
};

const MBTI16_RESULT = {
  resultCode: 'INTJ',
  typeName: 'Loyihachi',
  axes: {
    EI: { pct: 28.3, letter: 'I', borderline: false },
    SN: { pct: 71.6, letter: 'N', borderline: false },
    TF: { pct: 33.3, letter: 'T', borderline: false },
    JP: { pct: 64.1, letter: 'J', borderline: false },
  },
  borderlineAxes: [],
};

const BIG5_RESULT = {
  factors: {
    O: { raw: 38, pct: 70, level: 'Yuqori' },
    C: { raw: 41, pct: 77.5, level: 'Yuqori' },
    E: { raw: 24, pct: 35, level: 'Past' },
    A: { raw: 35, pct: 62.5, level: 'Yuqori' },
    N: { raw: 22, pct: 30, level: 'Past' },
  },
  stabilityPct: 70,
  maturityIndex: 68.4,
  maturityLevel: 'Yaxshi',
};

const RIASEC_RESULT = {
  resultCode: 'IRA',
  types: { R: 62, I: 88, ART: 71, SOC: 40, ENT: 35, CONV: 48 },
  differentiation: 53,
  consistency: 'High',
  careerFields: [{ name: 'Muhandislik', professions: ['Dasturchi'] }],
};

const ACTIVITY_RESULT = {
  scales: { MOT: 74, SELF: 68, SOCA: 52, ENG: 60 },
  activityIndex: 65.2,
  activityLevel: 'Moderate',
  needsAttention: false,
};

const AI_ANALYSIS = {
  id: 'ai-1',
  status: 'Succeeded',
  provider: 'Gemini',
  model: 'gemini-2.5-flash',
  promptVersion: 'v1.0',
  createdAt: '2026-08-31T10:00:00Z',
  summary: "Bu — o'quvchining namunaviy portreti.",
  personalityPortrait: 'Tahliliy fikrlaydi.',
  strengths: [{ title: 'Tahliliy fikrlash', description: 'Tavsif', evidence: 'Asos' }],
  growthAreas: [{ title: "O'sish zonasi", description: 'Tavsif', actionStep: 'Qadam' }],
  learningStyle: "Mustaqil o'qish",
  motivationProfile: 'Aniq maqsad',
  activityAssessment: "O'rtacha faol",
  careerSuggestions: [{ field: 'IT', why: 'Sabab', nextSteps: ['Kurs'] }],
  studentRecommendations: ['Tavsiya'],
  teacherNotes: ['Eslatma'],
  parentNotes: ['Eslatma'],
  attentionFlags: [],
  disclaimer: 'Bu tahlil tashxis emas.',
};

function buildProfileResponse(overrides: {
  assessments?: unknown[];
  latestAssessment?: unknown;
} = {}) {
  return {
    student: STUDENT,
    assessments: overrides.assessments ?? [ASSESSMENT_SUMMARY],
    latestAssessment:
      overrides.latestAssessment === undefined
        ? {
            id: 'assessment-1',
            results: {
              MBTI16: MBTI16_RESULT,
              BIG5: BIG5_RESULT,
              RIASEC: RIASEC_RESULT,
              ACTIVITY: ACTIVITY_RESULT,
            },
            aiAnalysis: AI_ANALYSIS,
            aiHistory: [],
          }
        : overrides.latestAssessment,
  };
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

function isStatusEnvelope(value: unknown): value is { status: number; body: unknown } {
  return typeof value === 'object' && value !== null && 'status' in value && 'body' in value;
}

function renderPage(profileResponses: unknown[], initialEntry = '/admin/students/student-1') {
  const responses = [...profileResponses];
  let lastResponse: unknown = responses[0];
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/students/student-1')) {
      const next = responses.length > 0 ? responses.shift() : lastResponse;
      lastResponse = next;
      if (isStatusEnvelope(next)) {
        return Promise.resolve(jsonResponse(next.body, next.status));
      }
      return Promise.resolve(jsonResponse(next));
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialEntry]}>
          <Routes>
            <Route path="/admin/students/:id" element={<StudentProfilePage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
  return Object.assign(fetchMock, { container: view.container });
}

describe('StudentProfilePage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("to'liq ma'lumot bilan sarlavha, kartalar, diagrammalar va AI hisobotni ko'rsatadi", async () => {
    renderPage([buildProfileResponse()]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getByText(/12-son maktab/)).toBeInTheDocument();
    expect(screen.getByText('INTJ')).toBeInTheDocument();
    expect(screen.getByText('68.4')).toBeInTheDocument();
    expect(screen.getByText('IRA')).toBeInTheDocument();
    expect(screen.getByText("Bu — o'quvchining namunaviy portreti.")).toBeInTheDocument();

    // 'N' o'rniga "Emotsional barqarorlik" ko'rsatiladi (CLAUDE.md MAXSUS DIQQAT).
    expect(screen.getAllByText('Emotsional barqarorlik').length).toBeGreaterThan(0);
    expect(screen.queryByText(/neyrotizm/i)).not.toBeInTheDocument();

    // Har diagramma uchun yashirin jadval alternativi mavjud.
    expect(screen.getAllByRole('table', { hidden: true }).length).toBeGreaterThan(0);
  });

  it('axe a11y tekshiruvi buzilishsiz o\'tadi', async () => {
    const fetchMock = renderPage([buildProfileResponse()]);
    await screen.findByText('Aliyev Sardor Bekzodovich');
    await expectNoAxeViolations(fetchMock.container);
  });

  it("bo'sh/null AI ma'lumotida sahifa yiqilmaydi", async () => {
    renderPage([
      buildProfileResponse({
        latestAssessment: {
          id: 'assessment-1',
          results: {},
          aiAnalysis: null,
          aiHistory: [],
        },
      }),
    ]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getAllByText('—').length).toBeGreaterThan(0);
    expect(screen.getByText('Bu sessiya uchun AI tahlil hali mavjud emas.')).toBeInTheDocument();
  });

  it('Unreliable holatida sariq banner ko\'rinadi', async () => {
    renderPage([
      buildProfileResponse({
        assessments: [{ ...ASSESSMENT_SUMMARY, reliabilityFlag: 'Unreliable', reliabilityScore: 25 }],
      }),
    ]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(
      screen.getByText(/Javoblar juda tez berilgan, natija ishonchsiz bo'lishi mumkin/),
    ).toBeInTheDocument();
  });

  it("Analyzing holatida refetchInterval yoqiladi va Analyzed'ga o'tgach o'chadi", async () => {
    const analyzingResponse = buildProfileResponse({
      assessments: [{ ...ASSESSMENT_SUMMARY, status: 'Analyzing', reliabilityFlag: null, reliabilityScore: null }],
      latestAssessment: { id: 'assessment-1', results: {}, aiAnalysis: null, aiHistory: [] },
    });
    const analyzedResponse = buildProfileResponse();

    const fetchMock = renderPage([analyzingResponse, analyzedResponse]);

    expect(await screen.findByText('Tahlil tayyorlanmoqda')).toBeInTheDocument();
    expect(
      screen.getByText('Odatda 1 daqiqa vaqt ketadi. Sahifa avtomatik yangilanadi.'),
    ).toBeInTheDocument();
    const callsAfterFirstLoad = fetchMock.mock.calls.length;

    await vi.advanceTimersByTimeAsync(5100);

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsAfterFirstLoad);
    });
    expect(await screen.findByText('INTJ')).toBeInTheDocument();

    const callsAfterAnalyzed = fetchMock.mock.calls.length;
    await vi.advanceTimersByTimeAsync(15000);
    // `Analyzed`ga o'tgach `refetchInterval` o'chadi — qo'shimcha so'rov ketmaydi.
    expect(fetchMock.mock.calls.length).toBe(callsAfterAnalyzed);
  });

  it('404 kelsa "topilmadi" holati ko\'rsatiladi', async () => {
    renderPage([
      { status: 404, body: { code: 'NOT_FOUND', title: 'Topilmadi', status: 404 } },
    ]);

    expect(await screen.findByText('O\'quvchi topilmadi')).toBeInTheDocument();
  });

  it('server xatosida qayta urinish tugmasi bilan xato holati ko\'rsatiladi', async () => {
    const fetchMock = renderPage([
      { status: 500, body: { code: 'INTERNAL_ERROR', title: 'Xato', status: 500 } },
    ]);

    const retryButton = await screen.findByRole('button', { name: 'Qayta urinish' });
    const callsBeforeRetry = fetchMock.mock.calls.length;
    retryButton.click();

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsBeforeRetry);
    });
  });
});
