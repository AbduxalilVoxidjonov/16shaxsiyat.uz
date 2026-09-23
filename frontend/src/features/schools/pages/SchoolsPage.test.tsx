import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useSearchParams } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import {
  emptyResponse,
  jsonResponse,
  pagedResponse,
  problemResponse,
  type Schemas,
} from '@/test/apiMock';
import SchoolsPage from './SchoolsPage';

const SCHOOL_1 = {
  id: 'school-1',
  name: '12-son maktab',
  region: "Farg'ona",
  district: "Qo'qon",
  slug: '12-maktab-qokon',
  publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon?k=abc123token',
  isActive: true,
  studentCount: 120,
  completedCount: 80,
  lastActivityAt: '2026-08-30T10:00:00Z',
  // `docs/07` 3.1 (2026-09-03): havola sog'ligi — bu fikstura "sog'lom" maktab.
  linkHealth: { status: 'Ok', availableProgramCount: 1, usableProgramCount: 1 },
  // `docs/07` 3.1 (2026-09-07): maktab kodi — backend FORMATLANGAN holda (`ABCD-2345`) qaytaradi.
  entryCode: 'PGMY-95MP',
} satisfies Schemas['AdminSchoolListItemDto'];

/**
 * `GET/PUT/POST /api/admin/schools[/{id}]` javobi — backend `AdminSchoolDetailDto`.
 *
 * Fixture sxemadan tekshiriladi (`satisfies Schemas['AdminSchoolDetailDto']`) — maydon
 * nomi/tipi backenddan uzilsa `tsc` qizaradi (`docs/10` §6.4).
 */
const SCHOOL_DETAIL = {
  id: 'school-1',
  name: SCHOOL_1.name,
  region: SCHOOL_1.region,
  district: SCHOOL_1.district,
  schoolNumber: '12',
  contactPerson: 'Aliyev Vali',
  contactPhone: '+998901234567',
  dailyRegistrationLimit: 500,
  accessCode: null,
  entryCode: 'ABCD-2345',
  notes: null,
  slug: SCHOOL_1.slug,
  publicUrl: SCHOOL_1.publicUrl,
  qrCodeBase64: 'aGVsbG8=',
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-02T00:00:00Z',
  // `docs/07` 3.1: statistika `stats` obyektida keladi (ro'yxat qatoridagi tekis
  // `studentCount`/`completedCount` bilan chalkashtirilmasin).
  stats: {
    studentCount: SCHOOL_1.studentCount,
    completedCount: SCHOOL_1.completedCount,
    inProgressCount: 5,
    completionRate: 0.667,
    lastActivityAt: SCHOOL_1.lastActivityAt,
  },
  linkHealth: SCHOOL_1.linkHealth,
  // `docs/07` 3.1 (2026-09-23) — maktabga aniq biriktirilgan testlar.
  testIds: [],
} satisfies Schemas['AdminSchoolDetailDto'];

interface FetchMockOptions {
  deleteResponse?: () => Response | Promise<Response>;
  /** Ro'yxat javobidagi maktab qatorini almashtiradi (havola sog'ligi testlari uchun). */
  listItem?: Schemas['AdminSchoolListItemDto'];
}

/**
 * NOTE: jsdom `HTMLDialogElement.showModal()`ni amalga oshirmaydi (`Dialog.tsx`
 * dagi himoyalangan chaqiruvga qarang), shu sabab dialog/drawer elementi `open` atributisiz
 * qoladi va `@testing-library`ning `getByRole` so'rovi (yashirin elementlarni chiqarib
 * tashlaydi) uni topa olmaydi — xuddi `SettingsPage.test.tsx`dagi TOTP dialog testidagi kabi.
 * Shu sabab dialog/drawer ICHIDAGI narsalar matn asosidagi so'rovlar bilan (`getByText`,
 * `getByLabelText`) topiladi; sahifadagi (dialogdan tashqari) tugmalar uchun `getByRole` bemalol
 * ishlatiladi.
 */
function mockFetch(options: FetchMockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? 'GET';

    if (url.includes('/api/admin/schools/school-1/regenerate-link')) {
      return Promise.resolve(
        jsonResponse<'RegenerateSchoolLinkResult'>({
          publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon-2?k=newtoken',
          qrCodeBase64: 'bmV3',
        }),
      );
    }
    if (url.includes('/api/admin/schools/school-1/toggle-active')) {
      return Promise.resolve(
        jsonResponse<'AdminSchoolDetailDto'>({
          ...SCHOOL_DETAIL,
          isActive: !SCHOOL_DETAIL.isActive,
        }),
      );
    }
    if (url.includes('/api/admin/schools/school-1') && method === 'DELETE') {
      return Promise.resolve(
        options.deleteResponse ? options.deleteResponse() : emptyResponse(204),
      );
    }
    if (url.includes('/api/admin/schools/school-1') && method === 'PUT') {
      return Promise.resolve(jsonResponse<'AdminSchoolDetailDto'>(SCHOOL_DETAIL));
    }
    if (url.includes('/api/admin/schools/school-1') && method === 'GET') {
      return Promise.resolve(jsonResponse<'AdminSchoolDetailDto'>(SCHOOL_DETAIL));
    }
    if (url.includes('/api/admin/schools') && method === 'POST') {
      return Promise.resolve(
        jsonResponse<'AdminSchoolDetailDto'>({ ...SCHOOL_DETAIL, id: 'school-2' }, 201),
      );
    }
    if (url.includes('/api/admin/schools') && method === 'GET') {
      return Promise.resolve(
        pagedResponse<'AdminSchoolListItemDto'>([options.listItem ?? SCHOOL_1]),
      );
    }
    return Promise.reject(new Error(`unexpected fetch: ${method} ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function UrlProbe() {
  const [searchParams] = useSearchParams();
  return <div data-testid="url-probe">{searchParams.toString()}</div>;
}

function renderSchoolsPage(initialEntry = '/admin/schools') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialEntry]}>
          <UrlProbe />
          <SchoolsPage />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('SchoolsPage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("maktablar ro'yxatini jadval sifatida ko'rsatadi", async () => {
    mockFetch();
    renderSchoolsPage();

    expect(await screen.findByText('12-son maktab')).toBeInTheDocument();
    expect(screen.getByText("Farg'ona, Qo'qon")).toBeInTheDocument();
    expect(screen.getByText('120')).toBeInTheDocument();
    expect(screen.getByText('80')).toBeInTheDocument();
    // `within(table)` — "Faol" so'zi filtr paneldagi "Holat" select'ida ham bor
    // (`SchoolFormDialog` ham DOM'da doim mavjud, faqat `open`siz — pastdagi izohga qarang).
    expect(within(screen.getByRole('table')).getByText('Faol')).toBeInTheDocument();
  });

  it("qidiruv 400ms debounce bilan URL'ga yoziladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderSchoolsPage();

    await screen.findByText('12-son maktab');
    fetchMock.mockClear();

    await user.type(screen.getByLabelText('Qidiruv'), '12-son');

    // Debounce tugamaguncha URL'da hali eski holat (yangi so'rov ketmagan).
    expect(screen.getByTestId('url-probe').textContent).not.toContain('search=');

    await vi.advanceTimersByTimeAsync(450);

    await waitFor(() => {
      expect(screen.getByTestId('url-probe').textContent).toContain('search=12-son');
    });
    await waitFor(() => {
      expect(fetchMock.mock.calls.some(([input]) => String(input).includes('search=12-son'))).toBe(
        true,
      );
    });
  });

  it("viloyat va faollik filtrlari URL query da saqlanadi (chuqur havoladan ham to'g'ri o'qiladi)", async () => {
    const fetchMock = mockFetch();
    renderSchoolsPage("/admin/schools?region=Farg'ona&active=true");

    await screen.findByText('12-son maktab');

    // `within(filtrPanel)` — `SchoolFormDialog`da ham "Viloyat" nomli maydon bor (DOM'da doim
    // mavjud, `open` faqat `showModal()`ni boshqaradi — jsdom bunda `display:none` qo'ymaydi).
    const filtersBar = within(screen.getByTestId('schools-filters'));
    expect(filtersBar.getByLabelText('Viloyat')).toHaveValue("Farg'ona");
    expect(filtersBar.getByLabelText('Holat')).toHaveValue('true');
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input]) => String(input).includes('region=') && String(input).includes('isActive=true'),
        ),
      ).toBe(true);
    });

    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    await user.selectOptions(filtersBar.getByLabelText('Holat'), 'Nofaol');

    await waitFor(() => {
      expect(screen.getByTestId('url-probe').textContent).toContain('active=false');
    });
  });

  it("yaratish oynasi bo'sh formani yubormaydi — validatsiya xatolari ko'rsatiladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderSchoolsPage();

    await screen.findByText('12-son maktab');
    fetchMock.mockClear();

    await user.click(screen.getByRole('button', { name: 'Yangi maktab' }));
    await screen.findByLabelText('Nomi');
    await user.click(screen.getByText('Yaratish'));

    expect(await screen.findByText('Maktab nomini kiriting.')).toBeInTheDocument();
    expect(screen.getByText('Tumanni kiriting.')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.some(([input, init]) => {
        const req = init as RequestInit | undefined;
        return String(input).includes('/api/admin/schools') && req?.method === 'POST';
      }),
    ).toBe(false);
  });

  it('havolani yangilash tasdiq dialogisiz bajarilmaydi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderSchoolsPage();

    await screen.findByText('12-son maktab');
    fetchMock.mockClear();

    await user.click(screen.getByRole('button', { name: 'Havolani yangilash' }));

    await screen.findByText('Havolani yangilashni tasdiqlang');
    expect(
      screen.getByText(
        'Eski havola darhol ishlamay qoladi. Maktabga yangi havolani yuborishni unutmang.',
      ),
    ).toBeInTheDocument();

    // Dialog ochilgani bilan hali so'rov ketmagan — faqat tasdiqlangach ketadi.
    expect(fetchMock.mock.calls.some(([input]) => String(input).includes('regenerate-link'))).toBe(
      false,
    );

    await user.click(screen.getByText('Ha, yangilash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => String(input).includes('regenerate-link')),
      ).toBe(true);
    });
    expect(await screen.findByText('Havola yangilandi')).toBeInTheDocument();
  });

  it("maktabda o'quvchilar bo'lsa o'chirishda 409 tushunarli xabarga aylanadi", async () => {
    mockFetch({
      deleteResponse: () =>
        problemResponse('SCHOOL_HAS_STUDENTS', 409, "Bu maktabda o'quvchilar bor."),
    });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderSchoolsPage();

    await screen.findByText('12-son maktab');
    await user.click(screen.getByRole('button', { name: "O'chirish" }));

    await screen.findByText("Maktabni o'chirishni tasdiqlang");
    await user.click(screen.getByText("Ha, o'chirish"));

    expect(
      await screen.findByText(
        "Bu maktabda o'quvchilar bor. Avval ularni ko'chiring yoki maktabni nofaol qiling.",
      ),
    ).toBeInTheDocument();
  });

  /**
   * 2026-09-03 jonli hodisasi: yagona dastur o'chirilgan edi, barcha maktab havolasi jimgina
   * o'lik bo'lib qoldi, panelda esa HECH QANDAY belgi yo'q edi.
   */
  it('dastursiz maktabda "havola ishlamaydi" belgisi va SABAB ko\'rsatiladi', async () => {
    mockFetch({
      listItem: {
        ...SCHOOL_1,
        linkHealth: {
          status: 'NoProgramAssigned',
          availableProgramCount: 0,
          usableProgramCount: 0,
        },
      },
    });
    renderSchoolsPage();

    await screen.findByText('12-son maktab');

    expect(await screen.findByText('Havola ishlamaydi')).toBeInTheDocument();

    // Umumiy "xato" yetarli emas — admin NIMA QILISHNI bilishi kerak.
    expect(screen.getByText(/Maktabga test biriktirilmagan/)).toBeInTheDocument();

    // Havolani nusxalash/QR yonida ham ogohlantirish bo'lishi kerak.
    expect(screen.getByText(/Bu havolani hozir tarqatish foydasiz/)).toBeInTheDocument();
  });

  it("dasturi bor maktabda ogohlantirish CHIQMAYDI (yolg'on signal bermaslik)", async () => {
    mockFetch();
    renderSchoolsPage();

    await screen.findByText('12-son maktab');

    expect(screen.queryByText('Havola ishlamaydi')).not.toBeInTheDocument();
    expect(screen.queryByText(/Bu havolani hozir tarqatish foydasiz/)).not.toBeInTheDocument();
    expect(screen.getByText('Ishlaydi')).toBeInTheDocument();
  });

  it("dasturda test bo'lmasa alohida (sariq) sabab ko'rsatiladi", async () => {
    mockFetch({
      listItem: {
        ...SCHOOL_1,
        linkHealth: {
          status: 'ProgramsWithoutTests',
          availableProgramCount: 1,
          usableProgramCount: 0,
        },
      },
    });
    renderSchoolsPage();

    await screen.findByText('12-son maktab');

    expect(await screen.findByText("Test yo'q")).toBeInTheDocument();
    expect(screen.getByText(/Biriktirilgan testda savol yo'q yoki u nashr qilinmagan/)).toBeInTheDocument();
  });

  it('QR modal havola va maktab nomi bilan ochiladi', async () => {
    mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderSchoolsPage();

    await screen.findByText('12-son maktab');
    await user.click(screen.getByRole('button', { name: "QR kodni ko'rish" }));

    await screen.findByText('QR kod');
    expect(await screen.findByAltText('QR kod')).toHaveAttribute(
      'src',
      'data:image/png;base64,aGVsbG8=',
    );
    expect(screen.getByText('Chop etish')).toBeInTheDocument();
    expect(screen.getByText('PNG yuklab olish')).toBeInTheDocument();
  });

  it('ro\'yxatda "Maktab kodi" ustuni bor va kod formatlangan holda ko\'rinadi', async () => {
    mockFetch();
    renderSchoolsPage();

    expect(await screen.findByRole('columnheader', { name: 'Maktab kodi' })).toBeInTheDocument();
    expect(screen.getByText('PGMY-95MP')).toBeInTheDocument();
    // Ro'yxatda "qayta yaratish" YO'Q — bu amal faqat detal sahifasida (tasdiq bilan).
    expect(screen.queryByRole('button', { name: /qayta yaratish/i })).not.toBeInTheDocument();
  });
});
