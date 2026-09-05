import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { jsonResponse, problemResponse } from '@/test/apiMock';
import { TYPE_CATALOG_BODY, TYPE_CODES } from '../test/typeCatalogFixture';
import { TypeCatalogSection } from './TypeCatalogSection';

function renderSection() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <TypeCatalogSection />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("TypeCatalogSection (`/metodika` — 16 tip bo'limi)", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("16 tipning HAR BIRI uchun karta ko'rsatiladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetTypeCatalogResult'>(TYPE_CATALOG_BODY)),
    );

    renderSection();

    expect(await screen.findByText('INTJ')).toBeInTheDocument();

    // Kartalar — bo'lim ichidagi havolalar; 16 ta bo'lishi SHART (loyiha egasining talabi).
    const links = screen.getAllByRole('link');
    expect(links).toHaveLength(16);

    for (const code of TYPE_CODES) {
      expect(screen.getByText(code)).toBeInTheDocument();
      expect(screen.getByText(`${code} nomi`)).toBeInTheDocument();
      expect(screen.getByText(`${code} qisqa tavsifi`)).toBeInTheDocument();
    }
  });

  it('karta tegishli tur sahifasiga (kichik harfli kod bilan) havola qiladi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetTypeCatalogResult'>(TYPE_CATALOG_BODY)),
    );

    renderSection();
    await screen.findByText('INTJ');

    const link = screen.getByRole('link', { name: /INTJ/ });
    expect(link).toHaveAttribute('href', '/metodika/intj');
  });

  it("so'rov autentifikatsiyasiz ommaviy endpointga yuboriladi", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(jsonResponse<'GetTypeCatalogResult'>(TYPE_CATALOG_BODY));
    vi.stubGlobal('fetch', fetchMock);

    renderSection();
    await screen.findByText('INTJ');

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/public/type-catalog');
    // Ochiq kontent — sessiya tokeni yuborilmasligi kerak (`docs/07` 1.10-bo'lim).
    expect(Object.keys(init.headers as Record<string, string>)).not.toContain('X-Session-Token');
  });

  it("yuklanish holatida holat xabari ko'rsatiladi", () => {
    // Hech qachon yakunlanmaydigan so'rov — `isPending` holatida qoladi.
    vi.stubGlobal('fetch', vi.fn().mockReturnValue(new Promise(() => {})));

    renderSection();

    expect(screen.getByRole('status')).toHaveTextContent('Tiplar yuklanmoqda…');
  });

  it("xato bo'lsa xato holati va qayta urinish tugmasi ko'rsatiladi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('INTERNAL_ERROR', 500)));

    renderSection();

    expect(await screen.findByRole('alert')).toHaveTextContent("Tiplar ro'yxatini yuklab bo'lmadi");
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it("katalog bo'sh bo'lsa bo'sh holat ko'rsatiladi (sahifa yiqilmaydi)", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetTypeCatalogResult'>({ types: [] })),
    );

    renderSection();

    expect(await screen.findByText("Tiplar ro'yxati hozircha bo'sh")).toBeInTheDocument();
    expect(screen.queryAllByRole('link')).toHaveLength(0);
  });
});
