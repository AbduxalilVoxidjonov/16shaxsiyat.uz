import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import TestCompletePage from './TestCompletePage';
import { useSessionStore } from '../store/sessionStore';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });
}

const CATALOG = [
  { code: 'MBTI16', name: '16 tipli shaxsiyat modeli', questionCount: 60, estimatedMinutes: 9, order: 1 },
  { code: 'BIG5', name: 'Shaxsiyatning 5 omili', questionCount: 50, estimatedMinutes: 8, order: 2 },
  { code: 'RIASEC', name: 'Kasb qiziqishlari', questionCount: 48, estimatedMinutes: 7, order: 3 },
  { code: 'ACTIVITY', name: 'Aktivlik va motivatsiya', questionCount: 32, estimatedMinutes: 5, order: 4 },
];

function seedSession() {
  useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1');
  useSessionStore.getState().setTestCatalog(CATALOG);
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
    renderPage('/t/demo-school/test/BIG5/done', { nextTestCode: 'RIASEC' });
    expect(await screen.findByText('LANDING_STUB')).toBeInTheDocument();
  });

  it("location.state'dagi nextTestCode bo'yicha qolgan bloklar va vaqtni ko'rsatadi (tabriknoma)", async () => {
    seedSession();
    renderPage('/t/demo-school/test/BIG5/done', { nextTestCode: 'RIASEC' });

    expect(await screen.findByText('Ajoyib! Blok tugadi')).toBeInTheDocument();
    // Qolgan: RIASEC (7) + ACTIVITY (5) = 12 daqiqa, 2 ta blok.
    expect(screen.getByText("Yana 2 ta blok qoldi · ~12 daqiqa")).toBeInTheDocument();
    expect(screen.getByText('Kasb qiziqishlari')).toBeInTheDocument();
    expect(screen.getByText('Aktivlik va motivatsiya')).toBeInTheDocument();
  });

  it("'Davom etish' bosilganda keyingi test blokiga o'tadi", async () => {
    seedSession();
    const user = userEvent.setup();
    renderPage('/t/demo-school/test/BIG5/done', { nextTestCode: 'RIASEC' });

    await user.click(await screen.findByRole('button', { name: 'Davom etish' }));

    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
  });

  it("sahifa yangilansa (location.state yo'q) sessiya holatidan qayta hisoblaydi", async () => {
    seedSession();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({
          assessmentId: 'assessment-1',
          status: 'InProgress',
          student: { firstNameShort: 'Sardor', grade: 9 },
          expiresAt: '2026-09-10T00:00:00Z',
          currentTestCode: 'RIASEC',
          tests: [
            { code: 'MBTI16', status: 'Completed', answered: 60, total: 60, order: 1 },
            { code: 'BIG5', status: 'Completed', answered: 50, total: 50, order: 2 },
            { code: 'RIASEC', status: 'InProgress', answered: 0, total: 48, order: 3 },
            { code: 'ACTIVITY', status: 'Locked', answered: 0, total: 32, order: 4 },
          ],
          progressPercent: 55,
        }),
      ),
    );

    renderPage('/t/demo-school/test/BIG5/done');

    expect(await screen.findByText('Kasb qiziqishlari')).toBeInTheDocument();
  });
});
