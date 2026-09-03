import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import StudentResultPage from './StudentResultPage';
import { useSessionStore } from '../store/sessionStore';

const RESULT_BODY = {
  personalityType: 'INTJ',
  typeName: 'Loyihachi',
  shortDescription: 'Uzoqni ko\'zlaydigan, mustaqil rejalashtiruvchi',
  topStrengths: ['Tahliliy fikrlash', 'Mustaqillik', "Maqsadga yo'nalganlik"],
  careerFields: ['Muhandislik', 'IT', 'Ilmiy tadqiqot'],
  note: 'Bu natija tashxis emas — hozirgi holatingiz surati.',
} satisfies Schemas['GetStudentResultResult'];

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
    // Bitta javob BARCHA so'rovlarga qaytadi (`/sessions/me` ham) — bu testda sessiya holati
    // ahamiyatsiz: `hasPersonalityBattery` `undefined` bo'lgani uchun sahifa natijani ko'rsatadi.
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse<'GetStudentResultResult'>(RESULT_BODY)));

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

  // `prompts/36` MAXSUS DIQQAT 3-band — shaxsiyat batareyasisiz dastur: backend `200` bilan
  // bo'sh maydonlar qaytaradi (`GetStudentResultQueryHandler`), sahifa buni "natija yo'q"
  // holati sifatida ko'rsatishi kerak — bo'sh joy yoki "0" YO'Q.
  it("shaxsiyat batareyasi bo'lmagan dasturda (bo'sh personalityType) tip kartasi o'rniga tushunarli xabar ko'rsatadi", async () => {
    seedSession();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse<'GetStudentResultResult'>({
          personalityType: '',
          typeName: '',
          shortDescription: '',
          topStrengths: [],
          careerFields: [],
          note: 'Bu natija tashxis emas — hozirgi holatingiz surati.',
        }),
      ),
    );

    renderPage();

    expect(await screen.findByText("Bu dastur uchun natija yo'q")).toBeInTheDocument();
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });
  // ⚠️ REGRESSIYA (`prompts/36`): "batareya bormi" savoliga backend BAYROQ bilan javob beradi
  // (`GET /sessions/me` → `hasPersonalityBattery`). Bayroq `false` bo'lsa, natija endpointi
  // (eski sessiya keshi, boshqa dastur, xato scoring) TIP QAYTARGAN taqdirda ham shaxsiyat
  // widget'lari RENDER QILINMAYDI — "ma'lumot yo'q" holati "0"/bo'sh karta bilan
  // almashtirilmaydi (`docs/06` qarorlar jurnali, 2026-09-02).
  it("bayroq `false` bo'lsa tip kartasi/kuchli tomonlar render qilinmaydi — tushunarli holat ko'rsatiladi", async () => {
    seedSession();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockImplementation((input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes('/sessions/me')) {
          return Promise.resolve(
            jsonResponse<'GetSessionStateResult'>({
              assessmentId: 'assessment-1',
              status: 'Analyzed',
              student: { firstNameShort: 'Sardor', grade: 9 },
              expiresAt: '2026-09-10T00:00:00Z',
              currentTestCode: null,
              tests: [
                {
                  code: 'MBTI16',
                  name: '16 tipli shaxsiyat modeli',
                  status: 'Completed',
                  answered: 60,
                  total: 60,
                  order: 1,
                  estimatedMinutes: 9,
                },
              ],
              progressPercent: 100,
              hasPersonalityBattery: false,
            }),
          );
        }
        return Promise.resolve(jsonResponse<'GetStudentResultResult'>(RESULT_BODY));
      }),
    );

    renderPage();

    expect(await screen.findByText("Bu dastur uchun natija yo'q")).toBeInTheDocument();
    expect(screen.queryByText('INTJ')).not.toBeInTheDocument();
    expect(screen.queryByText('Loyihachi')).not.toBeInTheDocument();
    expect(screen.queryByText('Tahliliy fikrlash')).not.toBeInTheDocument();
  });
});
