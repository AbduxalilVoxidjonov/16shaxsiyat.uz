import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router';
import axe from 'axe-core';
import { ToastProvider } from '@/shared/ui/Toast';
import { pagedResponse, type Schemas } from '@/test/apiMock';
import AssessmentsPage from './AssessmentsPage';
import type { AssessmentListItemDto } from '../model/types';

async function expectNoAxeViolations(container: Element): Promise<void> {
  const results = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
  expect(results.violations).toEqual([]);
}

/**
 * `GET /api/admin/assessments` qatori — backend `AdminAssessmentListItemDto`.
 *
 * `programId`/`programName` endi sxemada BOR: `programId` — DOIM bor
 * (`Assessment.ProgramId` domenda majburiy); `programName` esa dastur yozuvi topilmasa
 * `null` — jadval bunda "—" ko'rsatadi (pastdagi testlar shu holatni tekshiradi, shu sabab
 * bu yerda ataylab `null`).
 */
const ROW = {
  id: 'assessment-1',
  studentId: 'student-1',
  studentName: 'Aliyev Sardor Bekzodovich',
  schoolId: 'school-1',
  schoolName: "12-son maktab, Qo'qon",
  status: 'Analyzed',
  startedAt: '2026-08-30T09:00:00Z',
  completedAt: '2026-08-30T09:29:00Z',
  durationMinutes: 29,
  reliabilityScore: 82.5,
  reliabilityFlag: 'Reliable',
  programId: 'program-1',
  programName: null,
} satisfies AssessmentListItemDto;

const UNFINISHED_ROW = {
  ...ROW,
  id: 'assessment-2',
  studentName: 'Karimova Nilufar',
  status: 'InProgress',
  completedAt: null,
  durationMinutes: null,
  reliabilityScore: null,
  reliabilityFlag: null,
} satisfies AssessmentListItemDto;

/** Maktab filtri ro'yxati — backend `AdminSchoolListItemDto` ning TO'LIQ qatori. */
const SCHOOL_ROW = {
  id: 'school-1',
  name: '12-son maktab',
  region: "Farg'ona",
  district: "Qo'qon",
  slug: '12-maktab-qokon',
  publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon?k=abc123token',
  isActive: true,
  studentCount: 42,
  completedCount: 17,
  lastActivityAt: '2026-08-30T10:00:00Z',
} satisfies Schemas['AdminSchoolListItemDto'];

/** Detal sahifasi o'rniga — qatordan uzatilgan navigatsiya holatini ko'rsatuvchi qo'g'irchoq. */
function LocationStateProbe() {
  const location = useLocation();
  return <div data-testid="detail-state">{JSON.stringify(location.state)}</div>;
}

function renderPage(items: AssessmentListItemDto[], initialEntry = '/admin/assessments') {
  const requestedUrls: string[] = [];
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    requestedUrls.push(url);
    if (url.includes('/api/admin/schools')) {
      return Promise.resolve(pagedResponse<'AdminSchoolListItemDto'>([SCHOOL_ROW]));
    }
    if (url.includes('/api/admin/assessments')) {
      return Promise.resolve(pagedResponse<'AdminAssessmentListItemDto'>(items));
    }
    return Promise.reject(new Error(`kutilmagan so'rov: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialEntry]}>
          <Routes>
            <Route path="/admin/assessments" element={<AssessmentsPage />} />
            <Route path="/admin/assessments/:id" element={<LocationStateProbe />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
  return Object.assign(fetchMock, { container: view.container, requestedUrls });
}

function assessmentRequests(urls: string[]): string[] {
  return urls.filter((url) => url.includes('/api/admin/assessments'));
}

describe('AssessmentsPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sessiya qatorlarini o'quvchi, maktab, holat, vaqt va ishonchlilik bilan ko'rsatadi", async () => {
    renderPage([ROW]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getByText("12-son maktab, Qo'qon")).toBeInTheDocument();
    // Holat filtri ro'yxatida ham shu matn bor — jadval ichida qidiriladi.
    const table = within(screen.getByRole('table', { name: "Sessiyalar ro'yxati" }));
    expect(table.getByText('Tahlil qilingan')).toBeInTheDocument();
    expect(screen.getByText('30.08.2026 09:00')).toBeInTheDocument();
    expect(screen.getByText('30.08.2026 09:29')).toBeInTheDocument();
    expect(screen.getByText('Ishonchli')).toBeInTheDocument();
  });

  it("axe a11y tekshiruvi buzilishsiz o'tadi", async () => {
    const fetchMock = renderPage([ROW]);
    await screen.findByText('Aliyev Sardor Bekzodovich');
    await expectNoAxeViolations(fetchMock.container);
  });

  it("ma'lumot yo'q maydonlar `0` emas, belgi bilan ko'rsatiladi", async () => {
    renderPage([UNFINISHED_ROW]);

    expect(await screen.findByText('Karimova Nilufar')).toBeInTheDocument();
    // Yakunlanmagan vaqt, ishonchlilik va dastur — uchalasi ham "—".
    expect(screen.getAllByText('—').length).toBeGreaterThanOrEqual(3);
    expect(screen.queryByText('0')).not.toBeInTheDocument();
    expect(screen.queryByText('0.0')).not.toBeInTheDocument();
  });

  it("boshqaruv panelidan kelgan `?status=Analyzing` filtri DARHOL qo'llanadi", async () => {
    const fetchMock = renderPage([ROW], '/admin/assessments?status=Analyzing');

    await screen.findByText('Aliyev Sardor Bekzodovich');
    expect(assessmentRequests(fetchMock.requestedUrls)[0]).toContain('status=Analyzing');
    expect(screen.getByLabelText('Holat')).toHaveValue('Analyzing');
  });

  it("holat filtri URL va so'rovni yangilaydi", async () => {
    const user = userEvent.setup();
    const fetchMock = renderPage([ROW]);

    await screen.findByText('Aliyev Sardor Bekzodovich');
    await user.selectOptions(screen.getByLabelText('Holat'), 'Abandoned');

    await waitFor(() => {
      expect(
        assessmentRequests(fetchMock.requestedUrls).some((url) => url.includes('status=Abandoned')),
      ).toBe(true);
    });
    // Filtr o'zgarganda birinchi sahifaga qaytiladi.
    const lastRequest = assessmentRequests(fetchMock.requestedUrls).at(-1) ?? '';
    expect(lastRequest).toContain('page=1');
  });

  it("qatorga bosilganda detal sahifasiga sessiya qatori bilan o'tiladi", async () => {
    const user = userEvent.setup();
    renderPage([ROW]);

    await user.click(await screen.findByText('Aliyev Sardor Bekzodovich'));

    const state = await screen.findByTestId('detail-state');
    expect(JSON.parse(state.textContent ?? 'null')).toMatchObject({
      assessment: { id: 'assessment-1', status: 'Analyzed', studentId: 'student-1' },
    });
  });

  it("natija bo'lmasa bo'sh holat ko'rsatiladi", async () => {
    renderPage([]);

    expect(await screen.findByText('Sessiya topilmadi')).toBeInTheDocument();
  });
});
