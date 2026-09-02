import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import StudentResultPage from './StudentResultPage';
import { useSessionStore } from '../store/sessionStore';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });
}

function problemResponse(code: string, status: number): Response {
  return jsonResponse({ code, title: 'Xato', status, type: `https://studentroadmap/errors/${code}` }, status);
}

const RESULT_BODY = {
  personalityType: 'INTJ',
  typeName: 'Loyihachi',
  shortDescription: 'Uzoqni ko\'zlaydigan, mustaqil rejalashtiruvchi',
  topStrengths: ['Tahliliy fikrlash', 'Mustaqillik', "Maqsadga yo'nalganlik"],
  careerFields: ['Muhandislik', 'IT', 'Ilmiy tadqiqot'],
  note: 'Bu natija tashxis emas — hozirgi holatingiz surati.',
};

function seedSession() {
  useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1');
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/t/demo-school/result']}>
          <Routes>
            <Route path="/t/:slug" element={<div>LANDING_STUB</div>} />
            <Route path="/t/:slug/result" element={<StudentResultPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('StudentResultPage', () => {
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
    renderPage();
    expect(await screen.findByText('LANDING_STUB')).toBeInTheDocument();
  });

  it("200 kelsa tip kartasi, 3 kuchli tomon, 3 yo'nalish va disclaimer ko'rsatadi — aktivlik/bayroq/xom ball YO'Q", async () => {
    seedSession();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(RESULT_BODY)));

    renderPage();

    expect(await screen.findByText('INTJ')).toBeInTheDocument();
    expect(screen.getByText('Loyihachi')).toBeInTheDocument();
    for (const strength of RESULT_BODY.topStrengths) {
      expect(screen.getByText(strength)).toBeInTheDocument();
    }
    for (const field of RESULT_BODY.careerFields) {
      expect(screen.getByText(field)).toBeInTheDocument();
    }
    expect(screen.getByText(RESULT_BODY.note)).toBeInTheDocument();

    // Taqiqlangan maydonlar hech qachon matn sifatida chiqmasligi kerak (CLAUDE.md 9-qoida).
    expect(screen.queryByText(/NeedsAttention/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/reliability/i)).not.toBeInTheDocument();
  });

  it("202 (hali tayyor emas) kelsa tushunarli xabar va qayta urinish tugmasini ko'rsatadi", async () => {
    seedSession();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('NOT_READY', 202)));

    renderPage();

    expect(await screen.findByText('Natija hali tayyor emas')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it("403 (ko'rsatish o'chirilgan) kelsa mos xabarni ko'rsatadi", async () => {
    seedSession();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('FORBIDDEN', 403)));

    renderPage();

    expect(await screen.findByText('Natija ko\'rsatilmaydi')).toBeInTheDocument();
  });

  it("410 (sessiya tugagan) kelsa landing'ga qaytaradi", async () => {
    seedSession();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('SESSION_EXPIRED', 410)));

    renderPage();

    expect(await screen.findByText('LANDING_STUB')).toBeInTheDocument();
  });
});
