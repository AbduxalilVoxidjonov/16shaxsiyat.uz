import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import {
  jsonResponse,
  listResponse,
  pagedResponse,
  problemResponse,
  type Schemas,
} from '@/test/apiMock';
import PublicSpacePage from './PublicSpacePage';

/**
 * `GET /api/admin/public-space` javobi — backend `AdminPublicSpaceDto`.
 *
 * Fikstura EGASINING JONLI HOLATIDAN olingan (2026-09-05): yagona `PERSONALITY_PROFILE`
 * dasturi nashr qilingan, lekin TO'XTATILGAN (`state: 'Paused'`) — ya'ni hozir hech kim
 * test boshlay olmaydi. Aynan shu holat panelda ANIQ ko'rinishi kerak (2026-09-03
 * hodisasining takrori bo'lmasin).
 */
const PAUSED_PROGRAM = {
  id: 'program-1',
  code: 'PERSONALITY_PROFILE',
  nameUz: 'Shaxsiyat profili',
  state: 'Paused',
  visibility: 'Assigned',
  testCount: 1,
  hasUsableTest: true,
  // 2026-09-23 (`docs/07` §3.7): test dasturi — biriktirish/olib tashlash TEST orqali.
  testDefinitionId: 'test-1',
} satisfies Schemas['AdminPublicSpaceProgramDto'];

/** "Dasturlar" bo'limidan qolgan ESKI (ko'p testli) dastur — `testDefinitionId == null`. */
const LEGACY_PROGRAM = {
  id: 'legacy-program-1',
  code: 'LEGACY_PACK',
  nameUz: 'Eski to’plam',
  state: 'Active',
  visibility: 'Assigned',
  testCount: 4,
  hasUsableTest: true,
  testDefinitionId: null,
} satisfies Schemas['AdminPublicSpaceProgramDto'];

const BLOCKED_SPACE = {
  id: '00000000-0000-0000-0000-000000000002',
  name: 'Ommaviy makon',
  slug: 'ommaviy',
  isActive: true,
  showResultToStudent: true,
  dailyRegistrationLimit: 100000,
  publicUrl: 'https://16shaxsiyat.uz/kirish',
  availability: {
    status: 'ProgramsDeactivated',
    availableProgramCount: 0,
    usableProgramCount: 0,
  },
  programs: [PAUSED_PROGRAM],
  stats: {
    userCount: 128,
    deletedUserCount: 3,
    totalAssessments: 96,
    inProgressCount: 7,
    completedCount: 74,
    analyzedCount: 61,
    lastActivityAt: '2026-09-04T12:00:00Z',
  },
} satisfies Schemas['AdminPublicSpaceDto'];

/** Hammasi joyida bo'lgan holat — ogohlantirish CHIQMASLIGI kerak. */
const HEALTHY_SPACE = {
  ...BLOCKED_SPACE,
  availability: { status: 'Ok', availableProgramCount: 1, usableProgramCount: 1 },
  programs: [{ ...PAUSED_PROGRAM, state: 'Active' }],
} satisfies Schemas['AdminPublicSpaceDto'];

/** Dastursiz holat — biriktirish oqimini sinash uchun. */
const EMPTY_SPACE = {
  ...BLOCKED_SPACE,
  availability: {
    status: 'NoProgramAssigned',
    availableProgramCount: 0,
    usableProgramCount: 0,
  },
  programs: [],
} satisfies Schemas['AdminPublicSpaceDto'];

/** `GET /api/admin/catalog/tests` — ommaviy makonga biriktirish tanlovi (2026-09-23). */
function catalogTest(
  overrides: Partial<Schemas['CatalogTestListItemDto']>,
): Schemas['CatalogTestListItemDto'] {
  return {
    id: 'test-1',
    code: 'PERSONALITY_PROFILE',
    nameUz: 'Shaxsiyat profili',
    kind: 'Custom',
    isSystem: false,
    status: 'Published',
    isActive: true,
    scoringMode: 'Scored',
    questionCount: 40,
    scaleCount: 4,
    estimatedMinutes: 10,
    version: 1,
    usedInProgramCount: 1,
    ...overrides,
  };
}

const TEST_OPTIONS = [
  catalogTest({}),
  catalogTest({ id: 'test-archived', code: 'OLD_TEST', nameUz: 'Arxiv test', status: 'Archived' }),
  catalogTest({ id: 'test-draft', code: 'DRAFT_TEST', nameUz: 'Qoralama test', status: 'Draft' }),
];

interface FetchMockOptions {
  space?: Schemas['AdminPublicSpaceDto'];
  /** Ketma-ket `GET` javoblari — mutatsiyadan keyin yangilangan holatni ko'rsatish uchun. */
  mutationResult?: Schemas['AdminPublicSpaceDto'];
  spaceErrorStatus?: number;
  spaceErrorCode?: string;
}

function mockFetch(options: FetchMockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';

    if (url.includes('/api/admin/catalog/tests')) {
      return Promise.resolve(listResponse<'CatalogTestListItemDto'>(TEST_OPTIONS));
    }

    // Foydalanuvchilar ro'yxati — `/api/admin/public-space` prefiksidan OLDIN ushlanadi.
    if (url.includes('/api/admin/public-space/users')) {
      return Promise.resolve(pagedResponse<'AdminPublicUserListItemDto'>([]));
    }

    if (url.includes('/api/admin/public-space')) {
      if (method !== 'GET') {
        return Promise.resolve(
          jsonResponse<'AdminPublicSpaceDto'>(
            options.mutationResult ?? options.space ?? BLOCKED_SPACE,
          ),
        );
      }
      if (options.spaceErrorStatus) {
        return Promise.resolve(
          problemResponse(options.spaceErrorCode ?? 'INTERNAL_ERROR', options.spaceErrorStatus),
        );
      }
      return Promise.resolve(jsonResponse<'AdminPublicSpaceDto'>(options.space ?? BLOCKED_SPACE));
    }

    return Promise.reject(new Error(`kutilmagan so'rov: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/ommaviy']}>
          <Routes>
            <Route path="/admin/ommaviy" element={<PublicSpacePage />} />
            <Route path="/admin/catalog/tests/:id" element={<div>TEST_DETAIL_STUB</div>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('PublicSpacePage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("makon holatini ko'rsatadi (nomi, faolligi, slug, kunlik limit)", async () => {
    mockFetch();
    renderPage();

    expect(screen.getByText('Ommaviy makon', { selector: 'h1' })).toBeInTheDocument();
    // Ma'lumot kelguncha skelet turadi — avval yuklangan mazmunni kutamiz.
    expect(await screen.findByText('ommaviy')).toBeInTheDocument();
    expect(screen.getByText('Faol')).toBeInTheDocument();
    // Kunlik limit — ming ajratkichi bilan.
    expect(screen.getByText(/100.?000/)).toBeInTheDocument();
  });

  it("statistika kartalari ko'rsatiladi", async () => {
    mockFetch();
    renderPage();

    const stats = within(await screen.findByRole('region', { name: 'Statistika' }));
    expect(stats.getByText('128')).toBeInTheDocument(); // ro'yxatdan o'tganlar
    expect(stats.getByText('96')).toBeInTheDocument(); // jami sessiyalar
    expect(stats.getByText('7')).toBeInTheDocument(); // jarayonda
    expect(stats.getByText('74')).toBeInTheDocument(); // tugallangan
    expect(stats.getByText('61')).toBeInTheDocument(); // tahlil qilingan
    expect(stats.getByText('04.09.2026')).toBeInTheDocument();
  });

  /**
   * Egasining jonli holati: dastur nashr qilingan, lekin TO'XTATILGAN — oqim jimgina
   * o'lik. 2026-09-03 da aynan shu holat panelda hech qanday belgi bermagan edi.
   */
  it("test to'xtatilgan bo'lsa aniq ogohlantirish va o'sha testga havola ko'rsatiladi", async () => {
    mockFetch();
    renderPage();

    const alert = await screen.findByTestId('public-space-availability-alert');
    expect(within(alert).getByText('Hozir hech kim test boshlay olmaydi')).toBeInTheDocument();
    expect(
      within(alert).getByText(/Biriktirilgan test o’chirilgan yoki arxivlangan/),
    ).toBeInTheDocument();
    expect(
      within(alert).getByText(/“Shaxsiyat profili” hozir faol emas/),
    ).toBeInTheDocument();

    // Havola aynan o'sha TESTning sahifasiga olib boradi (2026-09-23: "Dasturlar" bo'limi yo'q;
    // test bu yerdan YOQILMAYDI).
    const link = within(alert).getByRole('link', { name: 'Testni ochish' });
    expect(link).toHaveAttribute('href', '/admin/catalog/tests/test-1');
  });

  it("hammasi joyida bo'lsa ogohlantirish CHIQMAYDI", async () => {
    mockFetch({ space: HEALTHY_SPACE });
    renderPage();

    expect(await screen.findByTestId('public-space-availability-ok')).toBeInTheDocument();
    expect(screen.queryByTestId('public-space-availability-alert')).not.toBeInTheDocument();
  });

  it('test biriktiriladi (POST tests/{testId}) va yangilangan holat keshga yoziladi', async () => {
    const fetchMock = mockFetch({ space: EMPTY_SPACE, mutationResult: HEALTHY_SPACE });
    const user = userEvent.setup();
    renderPage();

    expect(await screen.findByText('Hali test biriktirilmagan')).toBeInTheDocument();
    // Katalog testlari alohida so'rov bilan keladi — variant paydo bo'lguncha kutamiz
    // (aks holda `<select>` hali `disabled` holatda bo'ladi).
    await screen.findByRole('option', { name: /PERSONALITY_PROFILE/ });
    // Arxivlangan test tanlovda UMUMAN yo'q; qoralama — belgi bilan.
    expect(screen.queryByRole('option', { name: /OLD_TEST/ })).not.toBeInTheDocument();
    expect(screen.getByRole('option', { name: /DRAFT_TEST .*\(qoralama\)/ })).toBeInTheDocument();

    await user.selectOptions(screen.getByLabelText('Test biriktirish'), 'test-1');
    await user.click(screen.getByRole('button', { name: 'Biriktirish' }));

    expect(await screen.findByText('Test biriktirildi')).toBeInTheDocument();
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input, init]) =>
            String(input).includes('/api/admin/public-space/tests/test-1') &&
            (init as RequestInit | undefined)?.method === 'POST',
        ),
      ).toBe(true);
    });
    // Javob keshga yozilgani uchun ro'yxat qo'shimcha `GET`siz yangilanadi.
    expect(await screen.findByText('Shaxsiyat profili')).toBeInTheDocument();
  });

  it('biriktirilgan test olib tashlanadi (DELETE tests/{testId})', async () => {
    const fetchMock = mockFetch({ mutationResult: EMPTY_SPACE });
    const user = userEvent.setup();
    renderPage();

    const programs = within(await screen.findByTestId('public-space-programs'));
    await user.click(
      programs.getByRole('button', { name: '“Shaxsiyat profili” ni olib tashlash' }),
    );

    expect(await screen.findByText('Test olib tashlandi')).toBeInTheDocument();
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input, init]) =>
            String(input).includes('/api/admin/public-space/tests/test-1') &&
            (init as RequestInit | undefined)?.method === 'DELETE',
        ),
      ).toBe(true);
    });
  });

  it("eski dastur (testDefinitionId == null) faqat olib tashlash bilan ko'rsatiladi va eski endpoint chaqiriladi", async () => {
    const fetchMock = mockFetch({
      space: { ...HEALTHY_SPACE, programs: [LEGACY_PROGRAM] },
      mutationResult: EMPTY_SPACE,
    });
    const user = userEvent.setup();
    renderPage();

    const programs = within(await screen.findByTestId('public-space-programs'));
    expect(programs.getByText('Eski dastur')).toBeInTheDocument();
    expect(programs.getByText('4 ta anketa')).toBeInTheDocument();
    await user.click(programs.getByRole('button', { name: '“Eski to’plam” ni olib tashlash' }));

    expect(await screen.findByText('Test olib tashlandi')).toBeInTheDocument();
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input, init]) =>
            String(input).includes('/api/admin/public-space/programs/legacy-program-1') &&
            (init as RequestInit | undefined)?.method === 'DELETE',
        ),
      ).toBe(true);
    });
    // Eski dasturni tanlovdan QAYTA qo'shib bo'lmaydi — tanlov faqat katalog testlaridan.
    expect(screen.queryByRole('option', { name: /LEGACY_PACK/ })).not.toBeInTheDocument();
  });

  it("natijani ko'rsatish sozlamasi PUT so'rovi bilan o'zgaradi", async () => {
    const fetchMock = mockFetch({
      mutationResult: { ...BLOCKED_SPACE, showResultToStudent: false },
    });
    const user = userEvent.setup();
    renderPage();

    const toggle = await screen.findByLabelText('Natija foydalanuvchiga ko’rsatilsin');
    expect(toggle).toBeChecked();

    await user.click(toggle);

    expect(await screen.findByText('Sozlama saqlandi')).toBeInTheDocument();
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input, init]) => {
          const request = init as RequestInit | undefined;
          return (
            String(input).includes('/api/admin/public-space/show-result') &&
            request?.method === 'PUT' &&
            String(request.body).includes('"enabled":false')
          );
        }),
      ).toBe(true);
    });
  });

  it("nofaollashtirish yoki o'chirish tugmasi UMUMAN yo'q (domen buni taqiqlaydi)", async () => {
    mockFetch();
    renderPage();

    await screen.findByText('ommaviy');
    expect(screen.queryByRole('button', { name: /Faolsizlantirish/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /O'chirish/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Nofaol/ })).not.toBeInTheDocument();
  });

  it("ommaviy havola nusxa olish tugmasi bilan ko'rsatiladi", async () => {
    mockFetch();
    renderPage();

    expect(await screen.findByText('https://16shaxsiyat.uz/kirish')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Havoladan nusxa olish' })).toBeInTheDocument();
  });

  it("bu bo'limda “maktab” so'zi ishlatilmaydi", async () => {
    mockFetch();
    const { container } = renderPage();

    await screen.findByText('ommaviy');
    expect(container.textContent ?? '').not.toMatch(/maktab/i);
  });

  it("makon sozlanmagan bo'lsa (409) tushunarli xato ko'rsatiladi", async () => {
    mockFetch({ spaceErrorStatus: 409, spaceErrorCode: 'PUBLIC_SPACE_NOT_CONFIGURED' });
    renderPage();

    expect(await screen.findByText('Ommaviy makon sozlanmagan')).toBeInTheDocument();
    // Qayta urinish bu holatda foyda bermaydi — tugma ko'rsatilmaydi.
    expect(screen.queryByRole('button', { name: 'Qayta urinish' })).not.toBeInTheDocument();
  });

  it("server xato bersa qayta urinish tugmasi ko'rsatiladi", async () => {
    const fetchMock = mockFetch({ spaceErrorStatus: 500 });
    const user = userEvent.setup();
    renderPage();

    const retry = await screen.findByRole('button', { name: 'Qayta urinish' });
    const before = fetchMock.mock.calls.length;
    await user.click(retry);

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(before);
    });
  });
});
