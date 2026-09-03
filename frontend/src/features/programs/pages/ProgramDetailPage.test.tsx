import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import ProgramDetailPage from './ProgramDetailPage';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';

/**
 * `GET /api/admin/programs/{id}` javobi — backend `AdminProgramDetailDto` shakli;
 * `overrides` ham shu sxema bilan cheklangan, shuning uchun testda yozilgan har qanday
 * maydon nomi backend shartnomasiga qarshi tekshiriladi.
 */
function programDetail(
  overrides: Partial<Schemas['AdminProgramDetailDto']> = {},
): Schemas['AdminProgramDetailDto'] {
  return {
    id: 'program-1',
    code: 'CUSTOM_1',
    nameUz: 'Maxsus dastur',
    descriptionUz: null,
    kind: 'Custom',
    visibility: 'Public',
    status: 'Draft',
    isActive: true,
    isSystem: false,
    displayOrder: 1,
    tests: [{ testDefinitionId: 't-1', code: 'MBTI16', nameUz: '16 tip', displayOrder: 1 }],
    assignedSchoolIds: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

function mockFetch(detail: Schemas['AdminProgramDetailDto']) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/programs/program-1')) {
      return Promise.resolve(jsonResponse<'AdminProgramDetailDto'>(detail));
    }
    // Katalog test variantlari (`useCatalogTestOptionsQuery`) — backend hali yo'q, 404 kutiladi.
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/programs/program-1']}>
          <Routes>
            <Route path="/admin/programs/:id" element={<ProgramDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('ProgramDetailPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("BIG5+ACTIVITY batareyasi to'liq bo'lmasa ogohlantiradi", async () => {
    mockFetch(programDetail());
    renderPage();

    expect(
      await screen.findByText(
        "Bu dasturda yetuklik va aktivlik indekslari hisoblanmaydi, AI hisoboti qisqartirilgan bo'ladi.",
      ),
    ).toBeInTheDocument();
  });

  it("BIG5 va ACTIVITY ikkalasi bo'lsa ogohlantirish ko'rsatilmaydi", async () => {
    mockFetch(
      programDetail({
        tests: [
          { testDefinitionId: 't-1', code: 'BIG5', nameUz: 'Big Five', displayOrder: 1 },
          { testDefinitionId: 't-2', code: 'ACTIVITY', nameUz: 'Aktivlik', displayOrder: 2 },
        ],
      }),
    );
    renderPage();

    await screen.findByText('Maxsus dastur');
    expect(
      screen.queryByText(
        "Bu dasturda yetuklik va aktivlik indekslari hisoblanmaydi, AI hisoboti qisqartirilgan bo'ladi.",
      ),
    ).not.toBeInTheDocument();
  });

  it("tizim dasturida testlar tarkibi qulflangani haqida xabar ko'rsatiladi", async () => {
    mockFetch(programDetail({ isSystem: true, kind: 'System', status: 'Published' }));
    renderPage();

    expect(
      await screen.findByText(
        "Tizim dasturining tarkibi (testlar) himoyalangan — o'zgartirilmaydi.",
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText("Test qo'shish")).not.toBeInTheDocument();
  });

  // REGRESSIYA (egasining 2026-09-03 dagi xabari): tizim dasturida ("Shaxsiyat profili")
  // hamma amal yashiringani uchun shaxsiyat testini ro'yxatda ko'rib turib OCHIB
  // BO'LMASDI. Ko'rish tahrirlash emas — u har doim ochiq bo'lishi kerak.
  it("tizim dasturida ham test nomi katalogdagi sahifasiga havola bo'ladi", async () => {
    mockFetch(programDetail({ isSystem: true, kind: 'System', status: 'Published' }));
    renderPage();

    const link = await screen.findByRole('link', { name: '16 tip' });
    expect(link).toHaveAttribute('href', '/admin/catalog/tests/t-1');
  });

  it("Custom dasturda ham test nomi havola bo'ladi", async () => {
    mockFetch(programDetail());
    renderPage();

    const link = await screen.findByRole('link', { name: '16 tip' });
    expect(link).toHaveAttribute('href', '/admin/catalog/tests/t-1');
  });
});
