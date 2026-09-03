import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, type Schemas } from '@/test/apiMock';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import FinishPage from './FinishPage';
import { useSessionStore } from '../store/sessionStore';

function allTestsCompletedState(): Schemas['GetSessionStateResult'] {
  return {
    assessmentId: 'assessment-1',
    status: 'InProgress',
    student: { firstNameShort: 'Sardor', grade: 9 },
    expiresAt: '2026-09-10T00:00:00Z',
    currentTestCode: null,
    tests: [
      { code: 'MBTI16', name: '16 tipli shaxsiyat modeli', status: 'Completed', answered: 60, total: 60, order: 1, estimatedMinutes: 9 },
      { code: 'BIG5', name: 'Shaxsiyatning 5 omili', status: 'Completed', answered: 50, total: 50, order: 2, estimatedMinutes: 8 },
      { code: 'RIASEC', name: 'Kasb qiziqishlari', status: 'Completed', answered: 48, total: 48, order: 3, estimatedMinutes: 7 },
      { code: 'ACTIVITY', name: 'Aktivlik va motivatsiya', status: 'Completed', answered: 32, total: 32, order: 4, estimatedMinutes: 5 },
    ],
    progressPercent: 100,
    hasPersonalityBattery: true,
  };
}

function mockFetch({
  sessionState = allTestsCompletedState(),
  completeSession = { status: 'Analyzing', message: 'Tahlil qilinmoqda', showResultToStudent: true, resultAvailableAt: null },
}: {
  sessionState?: Schemas['GetSessionStateResult'];
  completeSession?: Schemas['CompleteSessionResult'];
} = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';
    if (url.includes('/sessions/me')) {
      return Promise.resolve(jsonResponse<'GetSessionStateResult'>(sessionState));
    }
    if (url.includes('/sessions/complete') && method === 'POST') {
      return Promise.resolve(jsonResponse<'CompleteSessionResult'>(completeSession));
    }
    return Promise.reject(new Error(`unexpected fetch: ${method} ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function seedSession() {
  useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1');
}

function renderPage(
  initialPath = '/t/demo-school/finish',
  queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } }),
) {
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialPath]}>
          <Routes>
            <Route path="/t/:slug" element={<div>LANDING_STUB</div>} />
            <Route path="/t/:slug/test/:testCode" element={<div>TEST_STUB</div>} />
            <Route path="/t/:slug/finish" element={<FinishPage />} />
            <Route path="/t/:slug/result" element={<div>RESULT_STUB</div>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('FinishPage', () => {
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
    renderPage();
    expect(await screen.findByText('LANDING_STUB')).toBeInTheDocument();
  });

  /**
   * REGRESSIYA (P30-2 bilan bir OILA — "yozishdan oldin o'qilgan holat"): `queryClient` da
   * `staleTime: 30_000`. Tez o'quvchi barcha bloklarni 30 soniyadan tez yechsa,
   * `GET /sessions/me` bir marta ham qayta so'ralmasdi va bu ekran TEST BOSHIDAGI eski
   * suratga qarab (`currentTestCode: 'MBTI16'`) allaqachon tugallangan blokka qaytarib
   * yborardi — u yerda `startTest` `409` qaytarib, o'quvchi bo'sh skeletda qamalib qolardi.
   * Endi ekran mount'da MAJBURIY qayta so'raydi va javob KELGUNCHA qaror qabul qilmaydi.
   */
  it('eski keshdagi `currentTestCode` bo\'yicha yo\'naltirmaydi — mount\'da qayta so\'raydi va YANGI javobni kutadi', async () => {
    seedSession();
    const fetchMock = mockFetch(); // server: hammasi tugagan (`currentTestCode: null`)

    // Ishlab chiqarishdagi kabi: uzoq `staleTime` + keshda ESKI surat.
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false, staleTime: 30_000 } },
    });
    queryClient.setQueryData(QUERY_KEYS.publicSessionMe(), {
      ...allTestsCompletedState(),
      currentTestCode: 'MBTI16',
      tests: allTestsCompletedState().tests.map((test) =>
        test.code === 'MBTI16' ? { ...test, status: 'InProgress' } : test,
      ),
    } satisfies Schemas['GetSessionStateResult']);

    renderPage('/t/demo-school/finish', queryClient);

    // Eski keshga qarab `TEST_STUB`ga qaytarib yubormaydi — yangi javobni kutadi.
    expect(await screen.findByText('Rahmat! Barcha savollarga javob berding.')).toBeInTheDocument();
    expect(screen.queryByText('TEST_STUB')).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/sessions/me'),
      expect.anything(),
    );
  });

  it("hali tugallanmagan test bo'lsa (currentTestCode bor) o'sha testga qaytaradi", async () => {
    seedSession();
    mockFetch({
      sessionState: {
        ...allTestsCompletedState(),
        currentTestCode: 'ACTIVITY',
        tests: allTestsCompletedState().tests.map((test) =>
          test.code === 'ACTIVITY' ? { ...test, status: 'InProgress' } : test,
        ),
      },
    });
    renderPage();
    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
  });

  it("barcha test tugagan bo'lsa POST /sessions/complete chaqiradi va rahmat xabarini ko'rsatadi", async () => {
    seedSession();
    const fetchMock = mockFetch();
    renderPage();

    expect(await screen.findByText('Rahmat! Barcha savollarga javob berding.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/sessions/complete'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it("showResultToStudent=true bo'lsa natija tugmasi darhol o'chiq bo'ladi", async () => {
    seedSession();
    mockFetch();
    renderPage();

    const button = await screen.findByRole('button', { name: "Natijani ko'rish" });
    expect(button).toBeDisabled();
  });

  it("showResultToStudent=false bo'lsa 'maktab psixologiga yuboriladi' xabarini ko'rsatadi (tugma yo'q)", async () => {
    seedSession();
    mockFetch({
      completeSession: {
        status: 'Analyzing',
        message: 'Tahlil qilinmoqda',
        showResultToStudent: false,
        resultAvailableAt: null,
      },
    });
    renderPage();

    expect(await screen.findByText('Natijalar maktab psixologiga yuboriladi.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "Natijani ko'rish" })).not.toBeInTheDocument();
  });

  // `prompts/36` MAXSUS DIQQAT 2/3-band — shaxsiyat batareyasi (MBTI16) yo'q dastur (masalan
  // faqat `Survey` blokli so'rovnoma): ball ko'rsatilmaydi, natija ekraniga taklif qilinmaydi.
  it("dasturda shaxsiyat batareyasi (MBTI16) bo'lmasa — 'javoblar saqlandi' xabari, natija tugmasi YO'Q", async () => {
    seedSession();
    mockFetch({
      sessionState: {
        ...allTestsCompletedState(),
        tests: [
          {
            code: 'CAREER_SURVEY_Q',
            name: "Kasb so'rovnomasi",
            status: 'Completed',
            answered: 20,
            total: 20,
            order: 1,
            estimatedMinutes: 4,
          },
        ],
        hasPersonalityBattery: false,
      },
      completeSession: { status: 'Analyzing', message: 'Tahlil qilinmoqda', showResultToStudent: true, resultAvailableAt: null },
    });
    renderPage();

    expect(await screen.findByText('Javoblaringiz saqlandi, rahmat.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "Natijani ko'rish" })).not.toBeInTheDocument();
    expect(screen.queryByText('Natijang tahlil qilinmoqda…')).not.toBeInTheDocument();
  });
  // ⚠️ REGRESSIYA: bayroq `hasPersonalityBattery` — YAGONA manba. Quyidagi ikki holat ataylab
  // metodika KODI bilan ZID: eski (satr solishtiruvchi) mantiq ikkalasida ham jimgina noto'g'ri
  // javob berardi va hech qanday xato ko'rinmasdi.
  it("bayroq `true` bo'lsa, `MBTI16` kodi YO'Q bo'lsa ham natija tugmasi ko'rsatiladi", async () => {
    seedSession();
    mockFetch({
      sessionState: {
        ...allTestsCompletedState(),
        tests: [
          {
            code: 'SHAXS_V2',
            name: 'Shaxsiyat profili v2',
            status: 'Completed',
            answered: 40,
            total: 40,
            order: 1,
            estimatedMinutes: 6,
          },
        ],
        hasPersonalityBattery: true,
      },
    });
    renderPage();

    expect(await screen.findByRole('button', { name: "Natijani ko'rish" })).toBeInTheDocument();
    expect(screen.queryByText('Javoblaringiz saqlandi, rahmat.')).not.toBeInTheDocument();
  });

  it("bayroq `false` bo'lsa, `MBTI16` kodi BOR bo'lsa ham natija tugmasi ko'rsatilmaydi", async () => {
    seedSession();
    mockFetch({
      sessionState: {
        ...allTestsCompletedState(),
        hasPersonalityBattery: false,
      },
    });
    renderPage();

    expect(await screen.findByText('Javoblaringiz saqlandi, rahmat.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "Natijani ko'rish" })).not.toBeInTheDocument();
  });
});
