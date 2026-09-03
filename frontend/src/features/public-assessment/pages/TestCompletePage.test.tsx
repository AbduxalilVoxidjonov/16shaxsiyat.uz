import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, type Schemas } from '@/test/apiMock';
import TestCompletePage from './TestCompletePage';
import { useSessionStore } from '../store/sessionStore';

// `PublicTestSummaryDto` shakli (docs/07 1.3-bo'lim, P36: `name`/`estimatedMinutes` bilan) —
// `TestPage` xuddi shu ro'yxatni `navigate(..., { state: { tests } })` orqali uzatadi.
const SESSION_TESTS = [
  { code: 'MBTI16', name: '16 tipli shaxsiyat modeli', status: 'Completed', answered: 60, total: 60, order: 1, estimatedMinutes: 9 },
  { code: 'BIG5', name: 'Shaxsiyatning 5 omili', status: 'Completed', answered: 50, total: 50, order: 2, estimatedMinutes: 8 },
  { code: 'RIASEC', name: 'Kasb qiziqishlari', status: 'InProgress', answered: 0, total: 48, order: 3, estimatedMinutes: 7 },
  { code: 'ACTIVITY', name: 'Aktivlik va motivatsiya', status: 'Locked', answered: 0, total: 32, order: 4, estimatedMinutes: 5 },
] satisfies Schemas['PublicTestSummaryDto'][];

function seedSession() {
  useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1');
}

function renderPage(initialPath: string, state?: unknown) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[{ pathname: initialPath, state }]}>
          <Routes>
            <Route path="/t/:slug" element={<div>LANDING_STUB</div>} />
            <Route path="/t/:slug/test/:testCode/done" element={<TestCompletePage />} />
            <Route path="/t/:slug/test/:testCode" element={<div>TEST_STUB</div>} />
            <Route path="/t/:slug/finish" element={<div>FINISH_STUB</div>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('TestCompletePage', () => {
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
    renderPage('/t/demo-school/test/BIG5/done', { nextTestCode: 'RIASEC', tests: SESSION_TESTS });
    expect(await screen.findByText('LANDING_STUB')).toBeInTheDocument();
  });

  it("location.state'dagi nextTestCode/tests bo'yicha qolgan bloklar va vaqtni ko'rsatadi (tabriknoma)", async () => {
    seedSession();
    renderPage('/t/demo-school/test/BIG5/done', { nextTestCode: 'RIASEC', tests: SESSION_TESTS });

    expect(await screen.findByText('Ajoyib! Blok tugadi')).toBeInTheDocument();
    // Qolgan: RIASEC (7) + ACTIVITY (5) = 12 daqiqa, 2 ta blok.
    expect(screen.getByText("Yana 2 ta blok qoldi · ~12 daqiqa")).toBeInTheDocument();
    expect(screen.getByText('Kasb qiziqishlari')).toBeInTheDocument();
    expect(screen.getByText('Aktivlik va motivatsiya')).toBeInTheDocument();
  });

  it("'Davom etish' bosilganda keyingi test blokiga o'tadi", async () => {
    seedSession();
    const user = userEvent.setup();
    renderPage('/t/demo-school/test/BIG5/done', { nextTestCode: 'RIASEC', tests: SESSION_TESTS });

    await user.click(await screen.findByRole('button', { name: 'Davom etish' }));

    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
  });

  it("sahifa yangilansa (location.state yo'q) sessiya holatidan qayta hisoblaydi", async () => {
    seedSession();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse<'GetSessionStateResult'>({
          assessmentId: 'assessment-1',
          status: 'InProgress',
          student: { firstNameShort: 'Sardor', grade: 9 },
          expiresAt: '2026-09-10T00:00:00Z',
          currentTestCode: 'RIASEC',
          tests: SESSION_TESTS,
          progressPercent: 55,
          hasPersonalityBattery: true,
        }),
      ),
    );

    renderPage('/t/demo-school/test/BIG5/done');

    expect(await screen.findByText('Kasb qiziqishlari')).toBeInTheDocument();
  });

  it("location.state'da faqat nextTestCode bo'lsa (tests yo'q) sessiya holatidan qayta hisoblaydi", async () => {
    // Eski (P36'gacha) `TestPage` faqat `nextTestCode`ni uzatgan bo'lishi mumkin edi —
    // `tests` yo'qligida ham to'g'ri ishlashi kerak (fallback fetch).
    seedSession();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse<'GetSessionStateResult'>({
          assessmentId: 'assessment-1',
          status: 'InProgress',
          student: { firstNameShort: 'Sardor', grade: 9 },
          expiresAt: '2026-09-10T00:00:00Z',
          currentTestCode: 'RIASEC',
          tests: SESSION_TESTS,
          progressPercent: 55,
          hasPersonalityBattery: true,
        }),
      ),
    );

    renderPage('/t/demo-school/test/BIG5/done', { nextTestCode: 'RIASEC' });

    expect(await screen.findByText('Kasb qiziqishlari')).toBeInTheDocument();
  });
});
