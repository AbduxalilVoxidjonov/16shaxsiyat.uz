import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { ROUTE_PATTERNS } from '@/shared/config/routes';
import { jsonResponse, listResponse, problemResponse } from '@/test/apiMock';
import SchoolDetailPage from './SchoolDetailPage';
import type { SchoolDetailDto, SchoolStatsDto } from '../model/types';

const BASE_DETAIL: SchoolDetailDto = {
  id: 'school-1',
  name: '12-son maktab',
  region: "Farg'ona",
  district: "Qo'qon",
  schoolNumber: '12',
  contactPerson: 'Aliyev Vali',
  contactPhone: '+998901234567',
  dailyRegistrationLimit: 500,
  accessCode: null,
  entryCode: 'ABCD-2345',
  notes: null,
  slug: '12-maktab-qokon',
  publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon?k=abc123token',
  qrCodeBase64: 'aGVsbG8=',
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-02T00:00:00Z',
  stats: {
    studentCount: 120,
    completedCount: 80,
    inProgressCount: 7,
    completionRate: 0.667,
    lastActivityAt: '2026-08-30T10:00:00Z',
  },
  // `docs/07` 3.1 (2026-09-03) — asosiy fikstura "sog'lom" havola; buzuq holat alohida testda.
  linkHealth: { status: 'Ok', availableProgramCount: 1, usableProgramCount: 1 },
  // `docs/07` 3.1 (2026-09-23) — maktabga aniq biriktirilgan testlar.
  testIds: [],
};

/**
 * `jsonResponse<'AdminSchoolDetailDto'>` — mock tanasi sxemadan tekshiriladi (`docs/10` §6.4).
 */
function mockFetch(
  stats?: Partial<SchoolStatsDto>,
  status = 200,
  linkHealth?: SchoolDetailDto['linkHealth'],
  overrides?: Partial<Pick<SchoolDetailDto, 'accessCode' | 'testIds'>>,
) {
  const detail: SchoolDetailDto = {
    ...BASE_DETAIL,
    ...overrides,
    stats: { ...BASE_DETAIL.stats, ...stats },
    linkHealth: linkHealth ?? BASE_DETAIL.linkHealth,
  };
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    // 2026-09-23: "Biriktirilgan testlar" kartasi nomlarni katalogdan oladi.
    if (url.includes('/api/admin/catalog/tests')) {
      return Promise.resolve(
        listResponse<'CatalogTestListItemDto'>([
          {
            id: 'test-1',
            code: 'INTELLECT-SURVEY',
            nameUz: "Intellect so'rovnomasi",
            kind: 'Custom',
            isSystem: false,
            status: 'Published',
            isActive: true,
            scoringMode: 'Survey',
            questionCount: 25,
            scaleCount: 0,
            estimatedMinutes: 8,
            version: 1,
            usedInProgramCount: 1,
          },
        ]),
      );
    }
    if (url.includes('/api/admin/schools/school-1')) {
      return Promise.resolve(
        status === 200
          ? jsonResponse<'AdminSchoolDetailDto'>(detail)
          : problemResponse('NOT_FOUND', status),
      );
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderDetailPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/schools/school-1']}>
          <Routes>
            <Route path={ROUTE_PATTERNS.admin.schoolDetail} element={<SchoolDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

/**
 * "Maktab kodi" `InfoRow` ining qiymat qismi ("Havola va maktab kodi" kartasida). Kod QR
 * modalda ham (yopiq, lekin DOM'da — jsdom `showModal` yo'q) turadi, shu sabab sahifa bo'ylab
 * `getByText` ikkitasini topardi.
 */
function entryCodeRow(): HTMLElement {
  const label = screen.getByText('Maktab kodi', { selector: 'dt' });
  return label.parentElement!;
}

/** Statistika bo'limidagi bitta karta qiymatini o'qiydi (label bo'yicha). */
function statValue(label: string): string {
  const section = screen.getByRole('region', { name: 'Maktab ishtirok statistikasi' });
  const card = within(section).getByText(label).parentElement;
  expect(card).not.toBeNull();
  return card!.querySelectorAll('span')[1]?.textContent ?? '';
}

describe('SchoolDetailPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("maktab ma'lumotlari va ishtirok statistikasini ko'rsatadi", async () => {
    mockFetch();
    renderDetailPage();

    expect(await screen.findByRole('heading', { name: '12-son maktab' })).toBeInTheDocument();
    expect(screen.getByText("Farg'ona, Qo'qon")).toBeInTheDocument();
    expect(screen.getByText('Aliyev Vali')).toBeInTheDocument();
    expect(screen.getByText('+998901234567')).toBeInTheDocument();

    expect(statValue("Ro'yxatdan o'tgan")).toBe('120');
    expect(statValue('Testni yakunlagan')).toBe('80');
    expect(statValue('Jarayonda')).toBe('7');
    expect(statValue('Oxirgi topshirilgan')).toBe('30.08.2026');
  });

  it("yakunlash ulushi (0..1) foizga o'giriladi — 0.667 → 67%", async () => {
    mockFetch();
    renderDetailPage();

    await screen.findByRole('heading', { name: '12-son maktab' });

    expect(statValue('Yakunlash ulushi')).toBe('67%');
  });

  it("hech kim topshirmagan bo'lsa 0 ko'rsatiladi, ulush esa '—'", async () => {
    mockFetch({
      studentCount: 0,
      completedCount: 0,
      inProgressCount: 0,
      completionRate: null,
      lastActivityAt: null,
    });
    renderDetailPage();

    await screen.findByRole('heading', { name: '12-son maktab' });

    expect(statValue("Ro'yxatdan o'tgan")).toBe('0');
    expect(statValue('Testni yakunlagan')).toBe('0');
    expect(statValue('Jarayonda')).toBe('0');
    // Nol va "ma'lumot yo'q" ajratiladi: nisbat aniqlanmagan → `—`, `0%` EMAS.
    expect(statValue('Yakunlash ulushi')).toBe('—');
    expect(statValue('Oxirgi topshirilgan')).toBe('—');
  });

  it("shu maktab o'quvchilariga `schoolId` filtri bilan havola beradi", async () => {
    mockFetch();
    renderDetailPage();

    const link = await screen.findByRole('link', { name: "Shu maktab o'quvchilarini ko'rish" });
    expect(link).toHaveAttribute('href', '/admin/students?schoolId=school-1');
  });

  /** 2026-09-03: ichki sahifada ham belgi VA sabab bo'lishi kerak — ro'yxatdagidek. */
  it("dastursiz maktabda belgi va SABAB ichki sahifada ham ko'rsatiladi", async () => {
    mockFetch(undefined, 200, {
      status: 'ProgramsDeactivated',
      availableProgramCount: 0,
      usableProgramCount: 0,
    });
    renderDetailPage();

    expect(await screen.findByText('Havola ishlamaydi')).toBeInTheDocument();
    expect(screen.getByText(/Biriktirilgan test o'chirilgan yoki arxivlangan/)).toBeInTheDocument();
    // Havolani nusxalash/QR yonidagi ogohlantirish.
    expect(screen.getByText(/Bu havolani hozir tarqatish foydasiz/)).toBeInTheDocument();
  });

  it("dasturi bor maktabda ichki sahifada ogohlantirish CHIQMAYDI", async () => {
    mockFetch();
    renderDetailPage();

    // Sog'lom havola — "Ishlaydi" belgisi, ogohlantirish YO'Q (yolg'on signal bermaslik).
    expect(await screen.findByText('Ishlaydi')).toBeInTheDocument();
    expect(screen.queryByText('Havola ishlamaydi')).not.toBeInTheDocument();
    expect(screen.queryByText(/Bu havolani hozir tarqatish foydasiz/)).not.toBeInTheDocument();
  });

  it('maktab topilmasa xato holati va qayta urinish tugmasi ko\'rsatiladi', async () => {
    mockFetch(undefined, 404);
    renderDetailPage();

    expect(await screen.findByText('Maktab topilmadi')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  /** Maktab kodi (`entryCode`) — `/kirish` → "Maktab uchun" siri; havola/QR yonida turadi. */
  describe('maktab kodi', () => {
    it("'Maktab kodi' qatorida formatlangan kodni ko'rsatadi; `accessCode: null` da 'Eski sinf kodi' qatori umuman yo'q", async () => {
      mockFetch();
      renderDetailPage();

      await screen.findByRole('heading', { name: '12-son maktab' });

      const row = entryCodeRow();
      expect(within(row).getByText('ABCD-2345')).toBeInTheDocument();
      expect(within(row).getByRole('button', { name: 'Maktab kodini nusxalash' })).toBeInTheDocument();
      // Eski qo'lda kiritiladigan "Kirish kodi" admin UI'dan olib tashlangan (2026-09-07):
      // `null` bo'lsa qator ham, `—` ham chiqmaydi — ikkinchi kod adminni chalg'itmasin.
      expect(screen.queryByText('Kirish kodi')).toBeNull();
      expect(screen.queryByText('Eski sinf kodi')).toBeNull();
    });

    /**
     * Eski ma'lumot yo'qolib ko'rinmasin: maktabda hali `accessCode` qiymati BO'LSA, u
     * "Eski sinf kodi" sifatida, "faqat ko'rish uchun" izohi bilan ko'rsatiladi.
     */
    it("eski `accessCode` qiymati bo'lsa — 'Eski sinf kodi' qatori izoh bilan ko'rinadi", async () => {
      mockFetch(undefined, 200, undefined, { accessCode: '123456' });
      renderDetailPage();

      await screen.findByRole('heading', { name: '12-son maktab' });

      const label = screen.getByText('Eski sinf kodi', { selector: 'dt' });
      const row = within(label.parentElement!);
      expect(row.getByText('123456')).toBeInTheDocument();
      expect(row.getByText(/faqat ko'rish uchun/)).toBeInTheDocument();
      expect(row.getByText(/O'rnini «Maktab kodi» egalladi/)).toBeInTheDocument();
    });

    /**
     * Egasi: "Maktab kodini qayerda? Topa olmadim" — kod endi sarlavha ostidagi alohida
     * "Havola va maktab kodi" kartasida, havola bilan YONMA-YON va katta shriftda turadi
     * (ma'lumotlar kartasining pastida, kichik qator ichida EMAS).
     */
    it("kod havola bilan bir kartada, ko'zga tashlanadigan (katta) ko'rinishda turadi", async () => {
      mockFetch();
      renderDetailPage();

      await screen.findByRole('heading', { name: '12-son maktab' });

      const shareHeading = screen.getByRole('heading', { name: 'Havola va maktab kodi' });
      const shareCard = shareHeading.closest('div.rounded-2xl');
      expect(shareCard).not.toBeNull();
      const card = within(shareCard as HTMLElement);

      // Havola ham, kod ham shu kartada.
      expect(card.getByText('12-maktab-qokon', { exact: false })).toBeInTheDocument();
      expect(card.getByText('ABCD-2345')).toHaveClass('text-2xl');
      expect(card.getByText('Kodni qayta yaratish')).toBeInTheDocument();
      // Kod nimaga kerakligi yozilgan — egasi "bu nima?" deb qolmasin.
      expect(card.getByText(/«Maktab uchun» bo'limiga shu kodni kiritadi/)).toBeInTheDocument();

      // Karta statistikadan OLDIN keladi (sarlavha ostida).
      const statsSection = screen.getByRole('region', { name: 'Maktab ishtirok statistikasi' });
      expect(shareHeading.compareDocumentPosition(statsSection) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    });

    it("'Kodni qayta yaratish' → tasdiq → POST regenerate-entry-code → yangi kod DARHOL ko'rinadi", async () => {
      const detail: SchoolDetailDto = { ...BASE_DETAIL };
      const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes('/regenerate-entry-code') && init?.method === 'POST') {
          return Promise.resolve(
            jsonResponse<'RegenerateSchoolEntryCodeResult'>({ entryCode: 'WXYZ-6789' }),
          );
        }
        if (url.includes('/api/admin/schools/school-1')) {
          return Promise.resolve(jsonResponse<'AdminSchoolDetailDto'>(detail));
        }
        return Promise.reject(new Error(`unexpected fetch: ${url}`));
      });
      vi.stubGlobal('fetch', fetchMock);
      const user = userEvent.setup();
      renderDetailPage();

      await screen.findByRole('heading', { name: '12-son maktab' });
      expect(within(entryCodeRow()).getByText('ABCD-2345')).toBeInTheDocument();
      await user.click(screen.getByRole('button', { name: 'Kodni qayta yaratish' }));

      // Tasdiq dialogi (jsdom `showModal` yo'q — matn bo'yicha, `SchoolsPage.test.tsx` izohi).
      expect(screen.getByText(/Eski kod darhol ishlamay qoladi/)).toBeInTheDocument();
      await user.click(screen.getByText('Ha, qayta yaratish'));

      await waitFor(() =>
        expect(
          fetchMock.mock.calls.some(
            ([url, init]) =>
              String(url).endsWith('/api/admin/schools/school-1/regenerate-entry-code') &&
              (init as RequestInit | undefined)?.method === 'POST',
          ),
        ).toBe(true),
      );
      // Detail hali eski (`ABCD-2345`) qaytaradi — sahifa esa javobdagi yangi kodni ko'rsatadi.
      await waitFor(() => expect(within(entryCodeRow()).getByText('WXYZ-6789')).toBeInTheDocument());
      expect(within(entryCodeRow()).queryByText('ABCD-2345')).not.toBeInTheDocument();
    });
  });
  it("biriktirilgan testlar nomlari ko'rsatiladi (noma'lumi — \"Noma'lum test\")", async () => {
    mockFetch(undefined, 200, undefined, { testIds: ['test-1', 'test-gone'] });
    renderDetailPage();

    const card = within(await screen.findByTestId('school-tests-card'));
    const link = await card.findByRole('link', { name: "Intellect so'rovnomasi" });
    expect(link).toHaveAttribute('href', '/admin/catalog/tests/test-1');
    expect(card.getByText("Noma'lum test")).toBeInTheDocument();
  });

  it("test biriktirilmagan maktabda bo'sh holat matni", async () => {
    mockFetch();
    renderDetailPage();

    const card = within(await screen.findByTestId('school-tests-card'));
    expect(card.getByText("Bu maktabga alohida test biriktirilmagan.")).toBeInTheDocument();
  });
});
