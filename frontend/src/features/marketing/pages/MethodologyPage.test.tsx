import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router';
import { jsonResponse } from '@/test/apiMock';
import { TYPE_CATALOG_BODY } from '../test/typeCatalogFixture';
import MethodologyPage from './MethodologyPage';

/**
 * Sahifada endi ma'lumot oluvchi bo'lim ham bor (16 tip — `TypeCatalogSection`), shu sabab
 * `QueryClientProvider` va `fetch` mock'i kerak. Bo'limning O'Z xatti-harakati
 * (`sections/TypeCatalogSection.test.tsx`) da alohida sinaladi — bu yerda faqat u sahifaga
 * ULANGANI tekshiriladi.
 */
function renderPage() {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(jsonResponse<'GetTypeCatalogResult'>(TYPE_CATALOG_BODY)),
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <MethodologyPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('MethodologyPage (`/metodika`)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sarlavha va to'rtta blok ko'rsatiladi", () => {
    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      /Platforma nimani va qanday o'lchaydi/,
    );

    for (const title of [
      'Shaxsiyat uslubi',
      'Besh omil',
      'Kasb qiziqishlari',
      "O'quv faolligi va motivatsiya",
    ]) {
      expect(screen.getByRole('heading', { level: 2, name: title })).toBeInTheDocument();
    }
  });

  it('har bir blok uchun "nimani o\'lchaydi" va "qanday ballanadi" bloklari bor', () => {
    renderPage();

    expect(screen.getAllByRole('heading', { name: "Nimani o'lchaydi" })).toHaveLength(4);
    expect(screen.getAllByRole('heading', { name: 'Qanday ballanadi' })).toHaveLength(4);
  });

  it('ishonchlilikning uchta belgisi tavsiflanadi', () => {
    renderPage();

    const section = screen.getByRole('region', { name: /Har bir sessiya tekshiriladi/ });
    expect(within(section).getByText('Ishonchli')).toBeInTheDocument();
    expect(within(section).getByText('Shubhali')).toBeInTheDocument();
    expect(within(section).getByText('Ishonchsiz')).toBeInTheDocument();
  });

  it("AI ball hisoblamasligi va tashxis qo'ymasligi ochiq yozilgan (CLAUDE.md 5 va 6-qoida)", () => {
    renderPage();

    expect(screen.getByRole('heading', { name: 'Ball hisoblamaydi' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: "Tashxis qo'ymaydi" })).toBeInTheDocument();
    expect(screen.getByText(/tibbiy yoki psixiatrik tashxis o'rnini bosmaydi/)).toBeInTheDocument();
  });

  it("16 tip bo'limi sahifaga ulangan va har bir tip kartasi chiqadi", async () => {
    renderPage();

    // Sarlavha darhol chiziladi (yuklanish holatida ham), shu sabab kutish AYNAN karta
    // havolasiga bog'lanadi — aks holda test skeleton holatida tekshirib qolardi.
    expect(await screen.findByRole('link', { name: /INTJ/ })).toBeInTheDocument();
    expect(
      screen.getByRole('heading', { level: 2, name: '16 ta shaxsiyat tipi' }),
    ).toBeInTheDocument();

    const section = screen.getByRole('region', { name: '16 ta shaxsiyat tipi' });
    expect(within(section).getAllByRole('link')).toHaveLength(16);
    expect(within(section).getByRole('link', { name: /INTJ/ })).toHaveAttribute(
      'href',
      '/metodika/intj',
    );
  });

  it('aloqa sahifasiga yakuniy havola beradi', () => {
    renderPage();

    const hrefs = screen.getAllByRole('link').map((link) => link.getAttribute('href'));
    expect(hrefs).toContain('/aloqa');
  });
});
