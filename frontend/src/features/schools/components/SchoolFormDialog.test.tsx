import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, listResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { SchoolFormDialog } from './SchoolFormDialog';

const SCHOOL_DETAIL = {
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
  publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon?k=abc',
  qrCodeBase64: 'aGVsbG8=',
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-02T00:00:00Z',
  stats: {
    studentCount: 120,
    completedCount: 80,
    inProgressCount: 5,
    completionRate: 0.667,
    lastActivityAt: '2026-08-30T10:00:00Z',
  },
  linkHealth: { status: 'Ok', availableProgramCount: 1, usableProgramCount: 1 },
  // `docs/07` 3.1 (2026-09-23) — maktabga aniq biriktirilgan testlar.
  testIds: [],
} satisfies Schemas['AdminSchoolDetailDto'];

/** `GET /api/admin/catalog/tests` elementi — maktab formasidagi "Testlar" tanlovi (2026-09-23). */
function catalogTest(
  overrides: Partial<Schemas['CatalogTestListItemDto']>,
): Schemas['CatalogTestListItemDto'] {
  return {
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
    ...overrides,
  };
}

const CATALOG_TESTS = [
  catalogTest({}),
  catalogTest({ id: 'test-2', code: 'BIG5', nameUz: 'Katta beshlik' }),
  catalogTest({ id: 'test-paused', code: 'PAUSED', nameUz: "To'xtatilgan test", isActive: false }),
  catalogTest({ id: 'test-draft', code: 'DRAFT', nameUz: 'Qoralama test', status: 'Draft' }),
  catalogTest({ id: 'test-archived', code: 'OLD', nameUz: 'Arxiv test', status: 'Archived' }),
];

/**
 * URL bo'yicha yo'naltiruvchi mock: katalog testlari — ro'yxat, qolgani — maktab javobi.
 * Har chaqiruvga YANGI `Response` (tana bir marta o'qiladi).
 */
function schoolFetchMock(
  respond: (init?: RequestInit) => Response = () =>
    jsonResponse<'AdminSchoolDetailDto'>(SCHOOL_DETAIL),
) {
  return vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    if (String(input).includes('/api/admin/catalog/tests')) {
      return Promise.resolve(listResponse<'CatalogTestListItemDto'>(CATALOG_TESTS));
    }
    return Promise.resolve(respond(init));
  });
}

/**
 * jsdom `showModal()`ni amalga oshirmaydi — oyna `open` atributisiz qoladi va `getByRole`
 * uni topa olmaydi (`SchoolsPage.test.tsx` dagi izoh). Shu sabab maydonlar `getByLabelText`
 * bilan qidiriladi.
 */
function renderDialog(props: { open: boolean; schoolId: string | null }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <SchoolFormDialog open={props.open} schoolId={props.schoolId} onClose={() => undefined} />
      </ToastProvider>
    </QueryClientProvider>,
  );
  return {
    ...view,
    setProps: (next: { open: boolean; schoolId: string | null }) =>
      view.rerender(
        <QueryClientProvider client={queryClient}>
          <ToastProvider>
            <SchoolFormDialog open={next.open} schoolId={next.schoolId} onClose={() => undefined} />
          </ToastProvider>
        </QueryClientProvider>,
      ),
  };
}

describe('SchoolFormDialog', () => {
  /**
   * P30-9 regressiya himoyasi: forma faqat oyna OCHIQ bo'lganda mount qilinadi va
   * boshlang'ich qiymatlarni mount paytida oladi. Agar forma yana doim mount holatda
   * turib, qiymatlar `useEffect(… reset …)` bilan to'ldiriladigan bo'lsa, shu test
   * yiqiladi — o'sha naqsh foydalanuvchi yozganini jimgina o'chirib tashlagan edi.
   */
  it("oyna yopiq bo'lganda forma render qilinmaydi", () => {
    const { setProps } = renderDialog({ open: false, schoolId: null });
    expect(screen.queryByLabelText('Nomi')).toBeNull();

    setProps({ open: true, schoolId: null });
    expect(screen.getByLabelText('Nomi')).toHaveValue('');
  });

  it("ochilgandan keyin yozilgan qiymat jimgina yo'qolmaydi", async () => {
    const user = userEvent.setup();
    renderDialog({ open: true, schoolId: null });

    const nameInput = screen.getByLabelText('Nomi');
    await user.type(nameInput, 'Yangi maktab nomi');

    // Barcha effekt/mikrovazifalar oqib bo'lgach ham qiymat joyida turishi kerak.
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(nameInput).toHaveValue('Yangi maktab nomi');
    expect(screen.getByLabelText('Tuman')).toHaveValue('');
  });

  /**
   * Ikki ustunli tartibga o'tishda regressiya himoyasi: barcha 8 maydon hamon render bo'ladi
   * va to'ldirilgan forma `POST /api/admin/schools` ga to'g'ri tana bilan ketadi. Eski "Kirish
   * kodi" (`accessCode`) maydoni admin UI'dan olib tashlangan (2026-09-07) — u YO'Q va tanada
   * ham yuborilmaydi (backend `null` deb qabul qiladi).
   */
  it("yaratish: barcha maydonlar bor, to'ldirilgach POST ketadi va oyna yopiladi", async () => {
    const fetchMock = schoolFetchMock(() =>
      jsonResponse<'AdminSchoolDetailDto'>({ ...SCHOOL_DETAIL, id: 'school-2' }, 201),
    );
    vi.stubGlobal('fetch', fetchMock);
    const onClose = vi.fn();
    const user = userEvent.setup();
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <SchoolFormDialog open schoolId={null} onClose={onClose} />
        </ToastProvider>
      </QueryClientProvider>,
    );

    for (const label of [
      'Nomi',
      'Viloyat',
      'Tuman',
      'Maktab raqami',
      "Kunlik ro'yxatdan o'tish limiti",
      "Mas'ul shaxs (F.I.Sh.)",
      'Telefon raqami',
      'Izoh',
    ]) {
      expect(screen.getByLabelText(label)).toBeInTheDocument();
    }
    expect(screen.queryByLabelText('Kirish kodi')).toBeNull();

    await user.type(screen.getByLabelText('Nomi'), '12-son maktab');
    await user.selectOptions(screen.getByLabelText('Viloyat'), "Farg'ona");
    await user.type(screen.getByLabelText('Tuman'), "Qo'qon");
    await user.type(screen.getByLabelText('Maktab raqami'), '12');
    await user.click(screen.getByText('Yaratish'));

    await waitFor(() => expect(onClose).toHaveBeenCalledTimes(1));
    const postCall = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit | undefined)?.method === 'POST',
    );
    expect(postCall).toBeDefined();
    expect(String(postCall![0])).toContain('/api/admin/schools');
    const body = JSON.parse(String((postCall![1] as RequestInit).body)) as Record<string, unknown>;
    expect(body).toMatchObject({
      name: '12-son maktab',
      region: "Farg'ona",
      district: "Qo'qon",
      schoolNumber: '12',
      dailyRegistrationLimit: 500,
    });
    // Bo'sh ixtiyoriy maydonlar `""` sifatida YUBORILMAYDI (`emptyToUndefined`).
    expect(body).not.toHaveProperty('contactPerson');
    expect(body).not.toHaveProperty('notes');
    // Eskirgan `accessCode` tanada UMUMAN yo'q — `""` ham, `null` ham emas.
    expect(body).not.toHaveProperty('accessCode');
    expect(await screen.findByText('Maktab yaratildi')).toBeInTheDocument();
  });

  it("tahrirlashda maydonlar ma'lumot kelgach to'ldirilgan holda mount bo'ladi", async () => {
    vi.stubGlobal(
      'fetch',
      schoolFetchMock(),
    );

    renderDialog({ open: true, schoolId: 'school-1' });

    // Yuklanayotganda forma yo'q — skeleton ko'rsatiladi.
    expect(screen.queryByLabelText('Nomi')).toBeNull();

    await waitFor(() => {
      expect(screen.getByLabelText('Nomi')).toHaveValue('12-son maktab');
    });
    expect(screen.getByLabelText('Tuman')).toHaveValue("Qo'qon");
  });

  it("tahrirlashda eski `accessCode` qiymati bo'lsa ham maydon ko'rinmaydi va PUT tanasida yuborilmaydi", async () => {
    const fetchMock = schoolFetchMock((init) =>
      jsonResponse<'AdminSchoolDetailDto'>({
        ...SCHOOL_DETAIL,
        accessCode: '123456',
        ...(init?.method === 'PUT' ? { updatedAt: '2026-09-07T00:00:00Z' } : {}),
      }),
    );
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderDialog({ open: true, schoolId: 'school-1' });
    await waitFor(() => {
      expect(screen.getByLabelText('Nomi')).toHaveValue('12-son maktab');
    });
    expect(screen.queryByLabelText('Kirish kodi')).toBeNull();
    expect(screen.queryByDisplayValue('123456')).toBeNull();

    await user.click(screen.getByText('Saqlash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([, init]) => (init as RequestInit | undefined)?.method === 'PUT'),
      ).toBe(true);
    });
    const putCall = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit | undefined)?.method === 'PUT',
    );
    const body = JSON.parse(String((putCall![1] as RequestInit).body)) as Record<string, unknown>;
    expect(body).toMatchObject({ name: '12-son maktab', district: "Qo'qon" });
    expect(body).not.toHaveProperty('accessCode');
  });
  /**
   * 2026-09-23 egasi qarori: "Dasturlar" bo'limi olib tashlandi — maktab formasida TESTLAR
   * tanlanadi (`testIds`, `docs/07` §3.1). Faqat nashr qilingan va faol testlar taklif qilinadi.
   * (`getByLabelText` — jsdom `showModal()`siz oyna ichini "yashirin" deb hisoblaydi.)
   */
  it("yaratish: faqat nashr qilingan faol testlar taklif qilinadi, tanlangani POST `testIds`da ketadi", async () => {
    const fetchMock = schoolFetchMock(() =>
      jsonResponse<'AdminSchoolDetailDto'>({ ...SCHOOL_DETAIL, id: 'school-2', testIds: ['test-2'] }, 201),
    );
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();
    renderDialog({ open: true, schoolId: null });

    const testsField = within(await screen.findByTestId('school-tests-field'));
    expect(await testsField.findByLabelText(/Katta beshlik/)).not.toBeChecked();
    expect(testsField.getByLabelText(/Intellect so'rovnomasi/)).toBeInTheDocument();
    // Arxivlangan, qoralama va nofaol testlar taklif QILINMAYDI.
    expect(testsField.queryByText('Arxiv test')).toBeNull();
    expect(testsField.queryByText('Qoralama test')).toBeNull();
    expect(testsField.queryByText("To'xtatilgan test")).toBeNull();

    await user.click(testsField.getByLabelText(/Katta beshlik/));
    await user.type(screen.getByLabelText('Nomi'), '12-son maktab');
    await user.selectOptions(screen.getByLabelText('Viloyat'), "Farg'ona");
    await user.type(screen.getByLabelText('Tuman'), "Qo'qon");
    await user.click(screen.getByText('Yaratish'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([, init]) => (init as RequestInit | undefined)?.method === 'POST'),
      ).toBe(true);
    });
    const postCall = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit | undefined)?.method === 'POST',
    );
    const body = JSON.parse(String((postCall![1] as RequestInit).body)) as Record<string, unknown>;
    expect(body.testIds).toEqual(['test-2']);
  });

  it("tahrirlash: biriktirilgan testlar belgilangan (nofaol bo'lsa ham ko'rinadi), olib tashlangani PUT `testIds`dan chiqadi", async () => {
    const fetchMock = schoolFetchMock(() =>
      jsonResponse<'AdminSchoolDetailDto'>({ ...SCHOOL_DETAIL, testIds: ['test-1', 'test-paused'] }),
    );
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();
    renderDialog({ open: true, schoolId: 'school-1' });

    const testsField = within(await screen.findByTestId('school-tests-field'));
    const paused = await testsField.findByLabelText(/To'xtatilgan test.*nofaol/);
    expect(paused).toBeChecked();
    expect(testsField.getByLabelText(/Intellect so'rovnomasi/)).toBeChecked();
    expect(testsField.getByLabelText(/Katta beshlik/)).not.toBeChecked();

    await user.click(testsField.getByLabelText(/Intellect so'rovnomasi/));
    await user.click(screen.getByText('Saqlash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([, init]) => (init as RequestInit | undefined)?.method === 'PUT'),
      ).toBe(true);
    });
    const putCall = fetchMock.mock.calls.find(
      ([, init]) => (init as RequestInit | undefined)?.method === 'PUT',
    );
    const body = JSON.parse(String((putCall![1] as RequestInit).body)) as Record<string, unknown>;
    expect(body.testIds).toEqual(['test-paused']);
  });

  it("409 TEST_ARCHIVED bo'lsa tushunarli xabar chiqadi va oyna yopilmaydi", async () => {
    const onClose = vi.fn();
    vi.stubGlobal(
      'fetch',
      schoolFetchMock((init) =>
        init?.method === 'PUT'
          ? problemResponse('TEST_ARCHIVED', 409)
          : jsonResponse<'AdminSchoolDetailDto'>({ ...SCHOOL_DETAIL, testIds: ['test-1'] }),
      ),
    );
    const user = userEvent.setup();
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <SchoolFormDialog open schoolId="school-1" onClose={onClose} />
        </ToastProvider>
      </QueryClientProvider>,
    );
    await waitFor(() => {
      expect(screen.getByLabelText('Nomi')).toHaveValue('12-son maktab');
    });
    await user.click(screen.getByText('Saqlash'));

    expect(
      await screen.findByText(/Tanlangan testlardan biri arxivlangan/),
    ).toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
