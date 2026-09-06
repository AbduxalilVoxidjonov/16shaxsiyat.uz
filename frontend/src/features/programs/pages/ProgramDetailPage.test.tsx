import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
    state: 'Draft',
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
    mockFetch(programDetail({ isSystem: true, kind: 'System', state: 'Active' }));
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
    mockFetch(programDetail({ isSystem: true, kind: 'System', state: 'Active' }));
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

  // ── YAGONA holat (2026-09-06) ─────────────────────────────────────────────────────────
  // Egasi ro'yxatda bitta dasturni bir vaqtda "Arxiv" ham, "Faol" ham bo'lib ko'rgan edi.
  // Endi belgi bitta, tugmalar esa domen ruxsat etgan o'tishlarga qat'iy mos.

  it("faqat BITTA holat belgisi ko'rsatiladi", async () => {
    mockFetch(programDetail({ state: 'Paused' }));
    renderPage();

    expect(await screen.findByText("To'xtatilgan")).toBeInTheDocument();
    expect(screen.queryByText('Nashr etilgan')).not.toBeInTheDocument();
    expect(screen.queryByText('Nofaol')).not.toBeInTheDocument();
  });

  it("arxivlangan dasturda \"Faollashtirish\" tugmasi yo'q, faqat \"Arxivdan tiklash\" bor", async () => {
    mockFetch(programDetail({ state: 'Archived' }));
    renderPage();

    expect(await screen.findByText('Arxiv')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Faollashtirish' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "To'xtatish" })).not.toBeInTheDocument();
    // Arxivdan nashr ham, qayta arxivlash ham taklif qilinmaydi — yagona yo'l tiklash.
    expect(screen.queryByRole('button', { name: 'Nashr qilish' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Arxivlash' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Arxivdan tiklash' })).toBeInTheDocument();
  });

  // ── Arxivdan tiklash (2026-09-06) ────────────────────────────────────────────────────
  // Egasining asosiy dasturi (`PERSONALITY_PROFILE`) arxivda qolib ketgan edi va uni faqat
  // nusxa olib "tiklash" mumkin edi.

  it("\"Arxivdan tiklash\" tasdiq oynasi oqibatni aniq aytadi: To'xtatilgan, keyin Faollashtirish", async () => {
    mockFetch(programDetail({ state: 'Archived', nameUz: 'Shaxsiyat profili' }));
    const user = userEvent.setup();
    renderPage();

    await user.click(await screen.findByRole('button', { name: 'Arxivdan tiklash' }));

    // jsdom native `<dialog>`ni `open` qilmaydi, shu sabab (mavjud dialog testlaridagidek)
    // rol emas, MATN bo'yicha so'raladi.
    expect(await screen.findByText('Dasturni arxivdan tiklash')).toBeInTheDocument();
    expect(screen.getByText('Shaxsiyat profili', { selector: 'p' })).toBeInTheDocument();
    expect(screen.getByText(/Dastur 'To'xtatilgan' holatiga qaytadi/)).toBeInTheDocument();
    expect(screen.getByText(/keyin 'Faollashtirish' bosiladi/)).toBeInTheDocument();
    expect(screen.getByText('Tiklash')).toBeEnabled();
  });

  it("tiklash tasdiqlansa POST /restore yuboriladi, holat 'To'xtatilgan' bo'lib \"Faollashtirish\" chiqadi", async () => {
    // Holatli mock: `restore` dan keyin detal so'rovi `Paused` qaytaradi (sahifa
    // `invalidateQueries` orqali qayta o'qiydi) — muvaffaqiyatdan keyingi UI shu bilan tekshiriladi.
    let state: Schemas['AdminProgramDetailDto']['state'] = 'Archived';
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/api/admin/programs/program-1/restore') && init?.method === 'POST') {
        state = 'Paused';
        return Promise.resolve(jsonResponse<'AdminProgramDetailDto'>(programDetail({ state })));
      }
      if (url.endsWith('/api/admin/programs/program-1')) {
        return Promise.resolve(jsonResponse<'AdminProgramDetailDto'>(programDetail({ state })));
      }
      return Promise.resolve(problemResponse('NOT_FOUND', 404));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();
    renderPage();

    await user.click(await screen.findByRole('button', { name: 'Arxivdan tiklash' }));
    await user.click(await screen.findByText('Tiklash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          (call) =>
            String(call[0]).endsWith('/api/admin/programs/program-1/restore') &&
            (call[1] as RequestInit | undefined)?.method === 'POST',
        ),
      ).toBe(true);
    });

    // Natija `Paused` — `Active` EMAS: badge "To'xtatilgan", tugma "Faollashtirish".
    expect(await screen.findByText("To'xtatilgan")).toBeInTheDocument();
    expect(screen.queryByText('Arxiv')).not.toBeInTheDocument();
    expect(await screen.findByRole('button', { name: 'Faollashtirish' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Arxivdan tiklash' })).not.toBeInTheDocument();
    // Tiklangan dasturni yana arxivlash mumkin.
    expect(screen.getByRole('button', { name: 'Arxivlash' })).toBeInTheDocument();
  });

  it("tiklash xatosi (409 PROGRAM_INVALID_TRANSITION) oynada ko'rsatiladi, holat o'zgarmaydi", async () => {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith('/restore') && init?.method === 'POST') {
        return Promise.resolve(
          problemResponse('PROGRAM_INVALID_TRANSITION', 409, "Dastur 'Active' holatidan 'Paused' ga o'ta olmaydi."),
        );
      }
      if (url.endsWith('/api/admin/programs/program-1')) {
        return Promise.resolve(jsonResponse<'AdminProgramDetailDto'>(programDetail({ state: 'Archived' })));
      }
      return Promise.resolve(problemResponse('NOT_FOUND', 404));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();
    renderPage();

    await user.click(await screen.findByRole('button', { name: 'Arxivdan tiklash' }));
    await user.click(await screen.findByText('Tiklash'));

    expect(
      await screen.findByText("Dastur 'Active' holatidan 'Paused' ga o'ta olmaydi."),
    ).toBeInTheDocument();
    expect(screen.getByText('Arxiv')).toBeInTheDocument();
  });

  it("qoralama dasturda holat tugmalari yo'q, faqat nashr va arxiv taklif qilinadi", async () => {
    mockFetch(programDetail({ state: 'Draft' }));
    renderPage();

    expect(await screen.findByText('Qoralama')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Faollashtirish' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "To'xtatish" })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Nashr qilish' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Arxivlash' })).toBeInTheDocument();
  });

  it("faol dasturda \"To'xtatish\", to'xtatilganida \"Faollashtirish\" ko'rsatiladi", async () => {
    mockFetch(programDetail({ state: 'Active' }));
    const { unmount } = renderPage();

    expect(await screen.findByRole('button', { name: "To'xtatish" })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Nashr qilish' })).not.toBeInTheDocument();
    unmount();

    mockFetch(programDetail({ state: 'Paused' }));
    renderPage();

    expect(await screen.findByRole('button', { name: 'Faollashtirish' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "To'xtatish" })).not.toBeInTheDocument();
  });
});
