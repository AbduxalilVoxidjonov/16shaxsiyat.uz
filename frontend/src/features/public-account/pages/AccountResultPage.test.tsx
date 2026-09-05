import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { setPublicAccessToken } from '@/shared/api/publicUserClient';
import AccountResultPage from './AccountResultPage';
import { usePublicUserStore } from '../store/publicUserStore';

const RESULT = {
  personalityType: 'INTJ',
  typeName: 'Loyihachi',
  shortDescription: "Uzoqni ko'zlaydigan, mustaqil rejalashtiruvchi",
  topStrengths: ['Tahliliy fikrlash', 'Mustaqillik'],
  careerFields: ['Muhandislik'],
  note: 'Bu natija tashxis emas — hozirgi holatingiz surati.',
} satisfies Schemas['GetStudentResultResult'];

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/kabinet/natijalar/assessment-1']}>
        <Routes>
          <Route path="/kabinet/natijalar/:assessmentId" element={<AccountResultPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AccountResultPage', () => {
  beforeEach(() => {
    usePublicUserStore.getState().setSession('access-1', {
      id: 'user-1',
      username: null,
      firstName: 'Ali',
      lastName: null,
      photoUrl: null,
      createdAt: '2026-09-01T10:00:00Z',
      lastLoginAt: '2026-09-05T10:00:00Z',
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
    localStorage.clear();
  });

  it("tayyor natijani maktab oqimidagi bilan BIR XIL ko'rinishda chiqaradi", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse<'GetStudentResultResult'>(RESULT));
    vi.stubGlobal('fetch', fetchMock);

    renderPage();

    expect(await screen.findByText('INTJ')).toBeInTheDocument();
    expect(screen.getByText('Loyihachi')).toBeInTheDocument();
    expect(screen.getByText('Tahliliy fikrlash')).toBeInTheDocument();
    expect(screen.getByText(RESULT.note)).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/me/assessments/assessment-1/result'),
      expect.anything(),
    );

    // Taqiqlangan maydonlar hech qachon chiqmaydi (CLAUDE.md 9-qoida).
    expect(screen.queryByText(/NeedsAttention/i)).not.toBeInTheDocument();
  });

  it("202 (tahlil navbatda) uchun kutish holatini va qayta urinishni ko'rsatadi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('NOT_READY', 202)));

    renderPage();

    expect(await screen.findByText('Natija hali tayyor emas')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it('403 (natija yopiq) uchun tushunarli xabar chiqadi', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('FORBIDDEN', 403)));

    renderPage();

    expect(await screen.findByText("Natija ko'rsatilmaydi")).toBeInTheDocument();
  });

  it("404 (yo'q yoki begona) uchun bitta umumiy xabar chiqadi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('NOT_FOUND', 404)));

    renderPage();

    expect(await screen.findByText('Natija topilmadi')).toBeInTheDocument();
  });

  it("bo'sh `personalityType` (batareyasiz dastur) da bo'sh karta emas, izoh ko'rsatiladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse<'GetStudentResultResult'>({
          personalityType: '',
          typeName: '',
          shortDescription: '',
          topStrengths: [],
          careerFields: [],
          note: 'Bu natija tashxis emas.',
        }),
      ),
    );

    renderPage();

    expect(await screen.findByText("Bu dastur uchun natija yo'q")).toBeInTheDocument();
  });
});
