import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useSearchParams } from 'react-router';
import DashboardPage from './DashboardPage';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';

/**
 * `GET /api/admin/dashboard/stats` javobi — backend `AdminDashboardStatsDto` shakli
 * (`docs/07` 3.6-bo'lim). `satisfies` tufayli maydon nomi yoki tipi backenddan uzilsa
 * `tsc` xato beradi.
 */
const FULL_STATS = {
  totals: {
    schools: 42,
    activeSchools: 39,
    students: 5820,
    completedAssessments: 5104,
    pendingAnalysis: 12,
    needsAttention: 318,
  },
  last30Days: {
    newStudents: 740,
    completed: 688,
    avgDurationMinutes: 28.4,
    avgReliability: 79.2,
    dropOffRate: 0.17,
  },
  personalityDistribution: [
    { type: 'INTJ', count: 184 },
    { type: 'ENFP', count: 120 },
  ],
  activityDistribution: [
    { level: 'Passive', count: 410 },
    { level: 'HighlyActive', count: 90 },
  ],
  hollandTop: [
    { code: 'SA', count: 621 },
    { code: 'IRA', count: 340 },
  ],
  recentAssessments: [
    {
      assessmentId: 'assess-1',
      studentName: 'Aliyev Sardor',
      schoolName: '12-son maktab',
      completedAt: '2026-08-30T10:00:00Z',
      status: 'Analyzed',
    },
  ],
  funnel: {
    linkViews: 1000,
    registered: 400,
    started: 380,
    completed: 300,
    analyzed: 290,
  },
  schoolBreakdown: [
    {
      schoolId: 'school-1',
      name: '12-son maktab',
      region: "Farg'ona",
      linkViews: 120,
      registered: 90,
      completed: 70,
      completionRate: 0.778, // ulush (0..1) — UI `× 100` qilib `78%` ko'rsatadi
      lastActivityAt: '2026-08-30T10:00:00Z',
    },
  ],
} satisfies Schemas['AdminDashboardStatsDto'];

interface FetchMockOptions {
  stats?: Schemas['AdminDashboardStatsDto'];
  statsErrorStatus?: number;
  /** `GET /api/admin/schools/link-health` javobi — "havola ishlamaydi" banneri (2026-09-03). */
  linkHealth?: Schemas['AdminSchoolsLinkHealthDto'];
}

function mockFetch(options: FetchMockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/schools/link-health')) {
      return Promise.resolve(
        jsonResponse<'AdminSchoolsLinkHealthDto'>(
          options.linkHealth ?? { activeSchoolCount: 3, brokenSchoolCount: 0, schools: [] },
        ),
      );
    }
    if (url.includes('/api/admin/dashboard/stats')) {
      if (options.statsErrorStatus) {
        return Promise.resolve(problemResponse('INTERNAL_ERROR', options.statsErrorStatus));
      }
      return Promise.resolve(
        jsonResponse<'AdminDashboardStatsDto'>(options.stats ?? FULL_STATS),
      );
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function UrlProbe() {
  const [searchParams] = useSearchParams();
  return <div data-testid="url-probe">{searchParams.toString()}</div>;
}

function renderDashboardPage(initialEntry = '/admin') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <UrlProbe />
        <DashboardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('KPI kartalar, oxirgi 30 kun va taqsimotlar render bo\'ladi', async () => {
    mockFetch();
    renderDashboardPage();

    expect(await screen.findByText('42')).toBeInTheDocument(); // schools
    expect(screen.getByText('5820')).toBeInTheDocument(); // students
    expect(screen.getByText('5104')).toBeInTheDocument(); // completedAssessments
    expect(screen.getByText('12')).toBeInTheDocument(); // pendingAnalysis
    expect(screen.getByText('318')).toBeInTheDocument(); // needsAttention
    expect(screen.getByText('39 faol / 42 jami')).toBeInTheDocument();

    // Oxirgi 30 kun — avgDurationMinutes/avgReliability ma'lumot bor bo'lsa raqam.
    expect(screen.getByText('740')).toBeInTheDocument();
    expect(screen.getByText('28.4 daqiqa')).toBeInTheDocument();
    expect(screen.getByText('79.2')).toBeInTheDocument();
    expect(screen.getByText('17%')).toBeInTheDocument();

    // Taqsimotlar (sr-only jadval alternativida ham bir xil matn bo'lgani uchun
    // ko'rinadigan ro'yxat ichida qidiramiz).
    const personalityList = within(screen.getByTestId('personality-distribution-list'));
    expect(personalityList.getByText('INTJ')).toBeInTheDocument();
    expect(personalityList.getByText('184')).toBeInTheDocument();
    const hollandList = within(screen.getByTestId('holland-top-list'));
    expect(hollandList.getByText('SA')).toBeInTheDocument();

    // So'nggi sessiyalar.
    expect(screen.getByText('Aliyev Sardor')).toBeInTheDocument();
  });

  it("voronka son va foizlarni to'g'ri hisoblaydi, son asosiy ko'rsatiladi", async () => {
    mockFetch();
    renderDashboardPage();

    await screen.findByText('42');

    const step = screen.getByTestId('funnel-step-registered');
    expect(within(step).getByText('400')).toBeInTheDocument();
    expect(within(step).getByText('oldingidan 40%')).toBeInTheDocument();
  });

  it('maktablar kesimida completionRate ulushdan foizga o\'giriladi (0.778 → 78%)', async () => {
    // Regressiya: backend `completed / registered` ulushini (0..1) qaytaradi, foizni emas.
    // `× 100` unutilganda 50% yakunlagan maktab jadvalda `1%` bo'lib ko'rinardi.
    mockFetch();
    renderDashboardPage();

    expect(await screen.findByText('78%')).toBeInTheDocument();
    expect(screen.queryByText('1%')).not.toBeInTheDocument();
  });

  it("maktablar kesimida completionRate null bo'lganda (registered=0) '—' ko'rsatiladi, 0% emas", async () => {
    mockFetch({
      stats: {
        ...FULL_STATS,
        schoolBreakdown: [
          {
            schoolId: 'school-2',
            name: 'Yangi maktab',
            region: 'Toshkent',
            linkViews: 5,
            registered: 0,
            completed: 0,
            completionRate: null,
            lastActivityAt: null,
          },
          ...FULL_STATS.schoolBreakdown,
        ],
      },
    });
    renderDashboardPage();

    await screen.findByText('42');

    // "12-son maktab" nomi so'nggi sessiyalar ro'yxatida ham bor — jadvalga scope qilamiz.
    const breakdownTable = screen.getByRole('table', { name: 'Maktablar kesimi jadvali' });

    const row = within(breakdownTable).getByText('Yangi maktab').closest('tr');
    expect(row).not.toBeNull();
    // completionRate va lastActivityAt ikkalasi ham null — ikkala katakda ham '—'.
    expect(within(row!).getAllByText('—')).toHaveLength(2);
    expect(within(row!).queryByText('0%')).not.toBeInTheDocument();

    // Boshqa qatorda haqiqiy foiz hali ham ko'rsatiladi (null bilan aralashmagan).
    const otherRow = within(breakdownTable).getByText('12-son maktab').closest('tr');
    expect(within(otherRow!).getByText('78%')).toBeInTheDocument();
  });

  it("oldingi bosqich 0 bo'lganda foiz ko'rsatilmaydi (nol bo'linish yo'q), ilova yiqilmaydi", async () => {
    mockFetch({
      stats: {
        ...FULL_STATS,
        funnel: { linkViews: 0, registered: 0, started: 5, completed: 2, analyzed: 1 },
      },
    });
    renderDashboardPage();

    await screen.findByText('42');

    const startedStep = screen.getByTestId('funnel-step-started');
    expect(within(startedStep).getByText('5')).toBeInTheDocument();
    expect(within(startedStep).getByText("oldingi bosqich ma'lumoti yo'q")).toBeInTheDocument();
    expect(within(startedStep).queryByText(/NaN/)).not.toBeInTheDocument();
    expect(within(startedStep).queryByText(/Infinity/)).not.toBeInTheDocument();
  });

  it("tanlangan sana oralig'ida faollik bo'lmasa (funnel barcha 0, schoolBreakdown bo'sh) sahifa yiqilmaydi, tegishli bo'lim tushunarli bo'sh holatini ko'rsatadi", async () => {
    // Haqiqiy degradatsiya holati — `funnel`/`schoolBreakdown` backend'da endi HAR DOIM
    // keladi (majburiy maydon, PM tasdig'i 2026-09-02), shu sabab "maydon umuman yo'q"
    // holati endi haqiqiy emas. O'rniga: tor sana oralig'ida hali faollik bo'lmagan
    // (`funnel` barcha bosqichi 0) va shu oralig'da maktablar kesimi bo'sh (`[]`) holat.
    mockFetch({
      stats: {
        ...FULL_STATS,
        funnel: { linkViews: 0, registered: 0, started: 0, completed: 0, analyzed: 0 },
        schoolBreakdown: [],
      },
    });
    renderDashboardPage();

    // KPI kartalar hali ham ko'rinadi — sahifa yiqilmagan.
    expect(await screen.findByText('42')).toBeInTheDocument();
    expect(screen.getByText("Bu davrda faollik hali yo'q")).toBeInTheDocument();
    expect(screen.getByText("Hali maktab yo'q")).toBeInTheDocument();
    // Nol bilan to'la voronka/jadval o'rniga bo'sh holat ko'rsatilgani uchun
    // bosqich qatorlari umuman render bo'lmaydi.
    expect(screen.queryByTestId('funnel-step-linkViews')).not.toBeInTheDocument();
    // Qolgan bo'limlar baribir render bo'ladi.
    expect(screen.getByText('Aliyev Sardor')).toBeInTheDocument();
  });

  it("avgDurationMinutes/avgReliability/dropOffRate null bo'lsa 0 emas, '—' ko'rsatiladi", async () => {
    mockFetch({
      stats: {
        ...FULL_STATS,
        last30Days: {
          ...FULL_STATS.last30Days,
          avgDurationMinutes: null,
          avgReliability: null,
          dropOffRate: null,
        },
      },
    });
    renderDashboardPage();

    await screen.findByText('42');
    const dashes = screen.getAllByText('—');
    // avgDurationMinutes, avgReliability, dropOffRate — uchalasi ham '—'.
    expect(dashes.length).toBeGreaterThanOrEqual(3);
    expect(screen.queryByText('0.0 daqiqa')).not.toBeInTheDocument();
    expect(screen.queryByText('0%')).not.toBeInTheDocument();
  });

  it("hali maktab yo'q bo'lsa (schools=0) tushunarli bo'sh holat va keyingi qadam ko'rsatiladi", async () => {
    mockFetch({
      stats: {
        ...FULL_STATS,
        totals: { ...FULL_STATS.totals, schools: 0 },
      },
    });
    renderDashboardPage();

    expect(await screen.findByText("Hali maktab yo'q")).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Birinchi maktabni qo\'shish' }),
    ).toBeInTheDocument();
    // Nol bilan to'la KPI kartalar ko'rsatilmaydi.
    expect(screen.queryByText('Yakunlangan sessiyalar')).not.toBeInTheDocument();
  });

  it("sana filtri tugmasi bosilganda URL'ga yoziladi va so'rov shu oraliq bilan yuboriladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderDashboardPage();

    await screen.findByText('42');
    fetchMock.mockClear();

    await user.click(screen.getByRole('button', { name: 'Oxirgi 7 kun' }));

    await waitFor(() => {
      const url = screen.getByTestId('url-probe').textContent ?? '';
      expect(url).toContain('from=');
      expect(url).toContain('to=');
    });
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => {
          const requestUrl = String(input);
          return (
            requestUrl.includes('/api/admin/dashboard/stats?') &&
            requestUrl.includes('from=') &&
            requestUrl.includes('to=')
          );
        }),
      ).toBe(true);
    });
  });

  it("chuqur havoladan (deep-link) sana oralig'i to'g'ri o'qiladi va so'rovga qo'shiladi", async () => {
    const fetchMock = mockFetch();
    renderDashboardPage('/admin?from=2026-08-01&to=2026-08-31');

    await screen.findByText('42');

    expect(screen.getByLabelText('Sanadan')).toHaveValue('2026-08-01');
    expect(screen.getByLabelText('Sanagacha')).toHaveValue('2026-08-31');

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => {
          const requestUrl = String(input);
          return (
            requestUrl.includes('/api/admin/dashboard/stats?') &&
            requestUrl.includes('from=2026-08-01') &&
            requestUrl.includes('to=2026-08-31')
          );
        }),
      ).toBe(true);
    });
  });

  /**
   * 2026-09-03: admin dasturni o'chirgach hech qanday belgi ko'rmagan edi. Boshqaruv paneli —
   * u har kuni ochadigan sahifa, shu sabab signal shu yerda.
   */
  it("dastursiz maktab bo'lsa banner soni, nomi va sababi bilan chiqadi", async () => {
    mockFetch({
      linkHealth: {
        activeSchoolCount: 3,
        brokenSchoolCount: 2,
        schools: [
          {
            id: 'school-1',
            name: '12-son maktab',
            linkHealth: {
              status: 'NoProgramAssigned',
              availableProgramCount: 0,
              usableProgramCount: 0,
            },
          },
        ],
      },
    });
    renderDashboardPage();

    // Banner ichida qidiramiz — maktab nomi sahifaning boshqa bo'limlarida ham uchraydi.
    const banner = await screen.findByRole('alert');
    expect(within(banner).getByText('2 ta maktab havolasi ishlamaydi')).toBeInTheDocument();
    expect(within(banner).getByText(/12-son maktab/)).toBeInTheDocument();
    expect(within(banner).getByText(/Maktabga dastur biriktirilmagan/)).toBeInTheDocument();
    expect(within(banner).getByText('va yana 1 ta')).toBeInTheDocument();
  });

  it("hamma maktab joyida bo'lsa banner CHIQMAYDI", async () => {
    mockFetch();
    renderDashboardPage();

    await screen.findByText('Boshqaruv paneli');

    await waitFor(() => {
      expect(screen.queryByText(/maktab havolasi ishlamaydi/)).not.toBeInTheDocument();
    });
  });

  it('server xato bersa qayta urinish tugmasi bilan xato holati ko\'rsatiladi', async () => {
    const fetchMock = mockFetch({ statsErrorStatus: 500 });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderDashboardPage();

    const retryButton = await screen.findByRole('button', { name: 'Qayta urinish' });
    expect(screen.getByText('Statistika yuklanmadi')).toBeInTheDocument();
    const callsBeforeRetry = fetchMock.mock.calls.length;

    await user.click(retryButton);

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsBeforeRetry);
    });
  });
});
