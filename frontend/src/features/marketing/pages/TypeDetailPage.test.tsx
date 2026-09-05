import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { jsonResponse, problemResponse } from '@/test/apiMock';
import { TYPE_CATALOG_BODY } from '../test/typeCatalogFixture';
import TypeDetailPage from './TypeDetailPage';

function renderPage(initialPath = '/metodika/intj') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/metodika/:kod" element={<TypeDetailPage />} />
          <Route path="/metodika" element={<div>METODIKA_STUB</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function stubCatalog() {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(jsonResponse<'GetTypeCatalogResult'>(TYPE_CATALOG_BODY)),
  );
}

describe('TypeDetailPage (`/metodika/:kod`)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("tur kontentini to'liq ko'rsatadi", async () => {
    stubCatalog();

    renderPage();

    expect(await screen.findByRole('heading', { level: 1, name: 'INTJ nomi' })).toBeInTheDocument();
    expect(screen.getByText('INTJ')).toBeInTheDocument();
    expect(screen.getByText('INTJ qisqa tavsifi')).toBeInTheDocument();
    expect(screen.getByText("INTJ to'liq tavsifi")).toBeInTheDocument();
    expect(screen.getByText('INTJ kuchli tomoni')).toBeInTheDocument();
    expect(screen.getByText("INTJ o'sish yo'nalishi")).toBeInTheDocument();
    expect(screen.getByText('INTJ kasb maslahati')).toBeInTheDocument();
  });

  it("uchta ro'yxat bo'limi sarlavhalari bilan chiqadi", async () => {
    stubCatalog();

    renderPage();
    await screen.findByRole('heading', { level: 1, name: 'INTJ nomi' });

    expect(screen.getByRole('heading', { name: 'Kuchli tomonlar' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: "O'sish yo'nalishlari" })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: "Kasb yo'nalishlari" })).toBeInTheDocument();
  });

  it("tashxis emasligi haqidagi eslatma ko'rsatiladi (CLAUDE.md 6-qoida)", async () => {
    stubCatalog();

    renderPage();
    await screen.findByRole('heading', { level: 1, name: 'INTJ nomi' });

    expect(screen.getByRole('heading', { name: 'Bu tavsif tashxis emas' })).toBeInTheDocument();
  });

  it('oldingi va keyingi tipga navigatsiya beradi', async () => {
    stubCatalog();

    // Alifbo tartibida: … INFP, INTJ, INTP … — `INTJ` ning qo'shnilari.
    renderPage();
    await screen.findByRole('heading', { level: 1, name: 'INTJ nomi' });

    const nav = screen.getByRole('navigation', { name: 'Boshqa tiplar' });
    const hrefs = [...nav.querySelectorAll('a')].map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(['/metodika/infp', '/metodika/intp']);
  });

  it("ro'yxatning birinchi tipida faqat 'keyingi' havolasi bo'ladi", async () => {
    stubCatalog();

    renderPage('/metodika/enfj');
    await screen.findByRole('heading', { level: 1, name: 'ENFJ nomi' });

    const nav = screen.getByRole('navigation', { name: 'Boshqa tiplar' });
    const hrefs = [...nav.querySelectorAll('a')].map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(['/metodika/enfp']);
  });

  it('kod registrga sezgir emas (katta harfli URL ham ishlaydi)', async () => {
    stubCatalog();

    renderPage('/metodika/INTJ');

    expect(await screen.findByRole('heading', { level: 1, name: 'INTJ nomi' })).toBeInTheDocument();
  });

  it("noma'lum kod uchun 'topilmadi' holati ko'rsatiladi", async () => {
    stubCatalog();

    renderPage('/metodika/xxxx');

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Bunday tip topilmadi' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: "Barcha tiplarni ko'rish" })).toHaveAttribute(
      'href',
      '/metodika',
    );
  });

  it("yuklanish holati ko'rsatiladi", () => {
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise(() => {})));

    renderPage();

    expect(screen.getByRole('status')).toHaveTextContent("Tip ma'lumoti yuklanmoqda…");
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1);
  });

  it("xato holati qayta urinish tugmasi bilan ko'rsatiladi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('INTERNAL_ERROR', 500)));

    renderPage();

    expect(
      await screen.findByRole('heading', { level: 1, name: "Tip ma'lumotini yuklab bo'lmadi" }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it('metodika sahifasiga qaytish havolasi bor', async () => {
    stubCatalog();

    renderPage();
    await screen.findByRole('heading', { level: 1, name: 'INTJ nomi' });

    const hrefs = screen.getAllByRole('link').map((link) => link.getAttribute('href'));
    expect(hrefs).toContain('/metodika');
  });
});
