import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, listResponse, problemResponse } from '@/test/apiMock';
import type { TestImportFile } from '../model/importSchema';
import CatalogPage from './CatalogPage';

/** Katalog ro'yxatining minimal, SXEMADAN tiplangan javobi (mock backenddan uzilsa `tsc` qizaradi). */
const SYSTEM_TEST = {
  id: '11111111-1111-1111-1111-111111111111',
  code: 'MBTI16',
  nameUz: '16 tipli shaxsiyat modeli',
  kind: 'System',
  isSystem: true,
  status: 'Published',
  isActive: true,
  scoringMode: 'Scored',
  questionCount: 60,
  scaleCount: 4,
  estimatedMinutes: 12,
  version: 1,
  usedInProgramCount: 1,
} satisfies Parameters<typeof listResponse<'CatalogTestListItemDto'>>[0][number];

const CREATED_TEST = {
  id: '22222222-2222-2222-2222-222222222222',
  code: 'STRESS-1',
  nameUz: 'Stressga chidamlilik',
  kind: 'Custom',
  isSystem: false,
  status: 'Draft',
  isActive: true,
  scoringMode: 'Scored',
  questionCount: 0,
  scaleCount: 0,
  estimatedMinutes: 6,
  version: 1,
  usedInProgramCount: 0,
  descriptionUz: null,
  pageSize: 10,
  shuffleQuestions: false,
  displayOrder: 0,
} satisfies Parameters<typeof jsonResponse<'CatalogTestDetailDto'>>[0];

/**
 * Katalog ro'yxatini muvaffaqiyatli qaytaradigan, qolgan chaqiruvlarni yo'l bo'yicha
 * ajratadigan mock. `POST /tests` — "Yangi anketa" oqimi.
 */
function stubCatalogFetch() {
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input.toString();
    const method = init?.method ?? 'GET';

    if (url.includes('/api/admin/catalog/tests') && method === 'POST') {
      return Promise.resolve(jsonResponse<'CatalogTestDetailDto'>(CREATED_TEST, 201));
    }
    if (url.includes('/api/admin/catalog/tests')) {
      return Promise.resolve(listResponse<'CatalogTestListItemDto'>([SYSTEM_TEST]));
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderWithRoutes() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/catalog']}>
          <Routes>
            <Route path="/admin/catalog" element={<CatalogPage />} />
            <Route
              path="/admin/catalog/tests/:id"
              element={<p>Tahrirlash sahifasi ochildi</p>}
            />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/catalog']}>
          <CatalogPage />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

/**
 * Katalog ro'yxati so'rovi xato bilan qaytganda (masalan `404`) sahifa soxta muvaffaqiyat
 * ko'rsatmaydi, aniq xato holatini (`ErrorState` + "Qayta urinish") beradi.
 *
 * Katalog marshrutlari `AssessmentCatalogController` (P37) da ochilgan va `schema.d.ts` da
 * bor — `model/types.ts` dagi tiplar sxemadan re-export qilinadi.
 */
describe('CatalogPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('katalog endpointi mavjud bo\'lmasa (404) xato holatini "Qayta urinish" bilan ko\'rsatadi', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('NOT_FOUND', 404)),
    );
    renderPage();

    expect(await screen.findByText("Katalogni yuklab bo'lmadi")).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it("\"Anketa yuklash\" dialogida noto'g'ri JSON tanlansa yuklash tugmasi o'chiq qoladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('NOT_FOUND', 404)),
    );
    const user = userEvent.setup();
    renderPage();

    await screen.findByText("Katalogni yuklab bo'lmadi");
    await user.click(screen.getByRole('button', { name: /Anketa yuklash/ }));

    const file = new File(['{ not json'], 'test.json', { type: 'application/json' });
    const input = document.getElementById('test-import-file-input') as HTMLInputElement;
    await user.upload(input, file);

    await waitFor(() => {
      expect(screen.getByText('Fayl yaroqli JSON emas.')).toBeInTheDocument();
    });
    expect(screen.getByText('Yuklash').closest('button')).toBeDisabled();
  });

  it("to'g'ri JSON tanlansa preview ko'rsatiladi va yuklash tugmasi yoqiladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(problemResponse('NOT_FOUND', 404)),
    );
    const user = userEvent.setup();
    renderPage();

    await screen.findByText("Katalogni yuklab bo'lmadi");
    await user.click(screen.getByRole('button', { name: /Anketa yuklash/ }));

    // Yuklanadigan fayl shakli — `testImportFileSchema` dan olingan `TestImportFile` bilan
    // tiplangan: fixture sxemadan uzilib qolsa `tsc` qizaradi, test jimgina yashil qolmaydi.
    const validFile = {
      code: 'STRESS',
      nameUz: 'Stress anketasi',
      estimatedMinutes: 5,
      questions: Array.from({ length: 4 }, (_, index) => ({
        code: `Q0${index + 1}`,
        order: index + 1,
        textUz: 'Savol',
        type: 'Likert5',
        scale: 'STRESS',
        direction: 1,
        weight: 1,
        isRequired: true,
      })),
    } satisfies TestImportFile;
    const file = new File([JSON.stringify(validFile)], 'stress.json', { type: 'application/json' });
    const input = document.getElementById('test-import-file-input') as HTMLInputElement;
    await user.upload(input, file);

    await waitFor(() => {
      expect(screen.getByText("Fayl to'g'ri — yuklashga tayyor")).toBeInTheDocument();
    });
    expect(screen.getByText('Yuklash').closest('button')).toBeEnabled();
  });

  /**
   * P39: dialog ilgari QAYSI format kerakligini umuman aytmasdi — egasi "word file"
   * yuklamoqchi bo'lgan. Endi format aniq yozilgan va IKKALA yuklab olish tugmasi
   * (bo'sh shablon + mavjud anketadan namuna) ko'rinadigan joyda.
   */
  it("yuklash dialogi formatni tushuntiradi va ikkala yuklab olish tugmasini ko'rsatadi", async () => {
    stubCatalogFetch();
    const user = userEvent.setup();
    renderWithRoutes();

    await screen.findByText('16 tipli shaxsiyat modeli');
    await user.click(screen.getByRole('button', { name: /Anketa yuklash/ }));

    expect(screen.getByText(/Excel \(\.xlsx\) fayl yuklang/)).toBeInTheDocument();
    expect(screen.getByText(/beshta varaq/)).toBeInTheDocument();
    // Oraliq qoidasi — usiz import muvaffaqiyatli bo'lib, NASHR bosqichida to'siqqa uriladi.
    expect(screen.getByText(/0 dan 100 gacha/)).toBeInTheDocument();

    // `getByRole` bu yerda ishlatilmaydi: dialog native `<dialog>` ustida, jsdom uni
    // `showModal()` bilan ochmaydi va mazmuni a11y daraxtida "yashirin" bo'lib qoladi —
    // fayldagi boshqa dialog testlari ham shu sabab `getByText(...).closest('button')` naqshini
    // ishlatadi.
    expect(screen.getByText("Bo'sh shablonni yuklab olish").closest('button')).toBeEnabled();
    expect(screen.getByText('Namunani yuklab olish').closest('button')).toBeEnabled();
    // Namuna sifatida mavjud anketa tanlanadi (tizim metodikasi ham — bu o'qish amali).
    expect(screen.getByText('16 tipli shaxsiyat modeli (MBTI16)')).toBeInTheDocument();
  });

  /**
   * P39: katalogda ilgari YAGONA tugma bor edi va u import dialogini ochardi — backendda
   * to'liq CRUD (P37) va tahrirlash sahifasi (P38) bo'lsa ham "noldan yaratish" yo'li yo'q edi.
   */
  it('"Yangi anketa" anketa yaratib tahrirlash sahifasiga o\'tadi', async () => {
    const fetchMock = stubCatalogFetch();
    const user = userEvent.setup();
    renderWithRoutes();

    await screen.findByText('16 tipli shaxsiyat modeli');
    await user.click(screen.getByRole('button', { name: /Yangi anketa/ }));

    await user.type(screen.getByLabelText('Anketa kodi'), 'STRESS-1');
    await user.type(screen.getByLabelText('Anketa nomi'), 'Stressga chidamlilik');
    const submit = screen.getByText('Yaratish').closest('button');
    expect(submit).not.toBeNull();
    await user.click(submit!);

    expect(await screen.findByText('Tahrirlash sahifasi ochildi')).toBeInTheDocument();

    const createCall = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit | undefined)?.method === 'POST',
    );
    expect(createCall).toBeDefined();
    expect(String(createCall?.[0])).toContain('/api/admin/catalog/tests');
  });
});
