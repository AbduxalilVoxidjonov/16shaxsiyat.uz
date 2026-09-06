import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useSearchParams } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { pagedResponse, problemResponse, typedResponse, type Schemas } from '@/test/apiMock';
import StudentsPage from './StudentsPage';

const STUDENT_1 = {
  id: 'student-1',
  fullName: 'Aliyev Sardor',
  schoolName: '12-son maktab',
  grade: 9,
  classLetter: 'B',
  phone: '+998901234567',
  lastAssessmentStatus: 'Analyzed',
  personalityType: 'INTJ',
  maturityIndex: 68.4,
  activityLevel: 'Moderate',
  needsAttention: true,
  reliabilityFlag: 'Reliable',
  lastAssessmentAt: '2026-08-30T10:00:00Z',
  source: 'School',
} satisfies Schemas['AdminStudentListItemDto'];

const STUDENT_2 = {
  id: 'student-2',
  fullName: 'Karimova Nilufar',
  schoolName: '1-son ixtisoslashtirilgan maktab',
  grade: 10,
  classLetter: null,
  phone: '+998901112233',
  lastAssessmentStatus: null,
  personalityType: null,
  maturityIndex: null,
  activityLevel: null,
  needsAttention: false,
  reliabilityFlag: null,
  lastAssessmentAt: null,
  source: 'Public',
} satisfies Schemas['AdminStudentListItemDto'];

/**
 * `GET /api/admin/schools` — maktab kombobox'i uchun. Kombobox faqat `name`/`region`/`district`
 * ni o'qiydi (`SchoolOption`), lekin mock backendning TO'LIQ `AdminSchoolListItemDto` qatorini
 * qaytaradi: mock backend shartnomasidan uzilmasligi uchun tor proyeksiya emas, haqiqiy javob
 * shakli ishlatiladi.
 */
const SCHOOL_OPTION = {
  id: 'school-1',
  name: '12-son maktab',
  region: "Farg'ona",
  district: "Qo'qon",
  slug: '12-maktab-qokon',
  publicUrl: 'https://16shaxsiyat.uz/t/12-maktab-qokon?k=abc123token',
  isActive: true,
  studentCount: 42,
  completedCount: 17,
  lastActivityAt: '2026-08-30T10:00:00Z',
  // `docs/07` 3.1 (2026-09-03): havola sog'ligi — bu fikstura "sog'lom" maktab.
  linkHealth: { status: 'Ok', availableProgramCount: 1, usableProgramCount: 1 },
} satisfies Schemas['AdminSchoolListItemDto'];

interface FetchMockOptions {
  students?: Schemas['AdminStudentListItemDto'][];
  studentsErrorStatus?: number;
  exportResponse?: () => Response | Promise<Response>;
}

/**
 * NOTE: `SchoolCombobox` va `Dialog` bilan bir xil sabab — jsdom o'zi
 * `HTMLDialogElement`ni amalga oshirmaydi, lekin bu sahifada dialog yo'q; shu sabab bu
 * yerda faqat `SchoolsPage.test.tsx` (P23) bilan bir xil `mockFetch` naqshi ishlatiladi.
 */
function mockFetch(options: FetchMockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);

    if (url.includes('/api/admin/students/export')) {
      if (options.exportResponse) return Promise.resolve(options.exportResponse());
      // Node/undici'ning `Response` konstruktori jsdom global `Blob`ini tanimasligi mumkin
      // (realm nomuvofiqligi — "object.stream is not a function" xatosi topilgan edi), shu
      // sabab tana sifatida oddiy matn ishlatiladi; `response.blob()` baribir Blob qaytaradi.
      return Promise.resolve(
        new Response('xlsx-bytes', {
          status: 200,
          headers: {
            'content-type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          },
        }),
      );
    }
    if (url.includes('/api/admin/students')) {
      if (options.studentsErrorStatus) {
        return Promise.resolve(problemResponse('INTERNAL_ERROR', options.studentsErrorStatus));
      }
      return Promise.resolve(
        pagedResponse<'AdminStudentListItemDto'>(options.students ?? [STUDENT_1, STUDENT_2]),
      );
    }
    if (url.includes('/api/admin/schools/school-1')) {
      // `useSchoolNameQuery` ataylab TOR proyeksiya o'qiydi (`{id, name}`) — backend bu yerda
      // to'liq `AdminSchoolDetailDto` qaytaradi, lekin chuqur havoladagi kombobox uchun faqat
      // nom kerak. Shu sabab sxema tipi emas, mijoz kutgan aniq shakl ko'rsatiladi.
      return Promise.resolve(
        typedResponse<{ id: string; name: string }>({
          id: 'school-1',
          name: SCHOOL_OPTION.name,
        }),
      );
    }
    if (url.includes('/api/admin/schools')) {
      return Promise.resolve(pagedResponse<'AdminSchoolListItemDto'>([SCHOOL_OPTION]));
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function UrlProbe() {
  const [searchParams] = useSearchParams();
  return <div data-testid="url-probe">{searchParams.toString()}</div>;
}

function renderStudentsPage(initialEntry = '/admin/students') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialEntry]}>
          <UrlProbe />
          <StudentsPage />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('StudentsPage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("o'quvchilar ro'yxatini jadval sifatida ko'rsatadi", async () => {
    mockFetch();
    renderStudentsPage();

    expect(await screen.findByText('Aliyev Sardor')).toBeInTheDocument();
    const table = within(screen.getByRole('table'));
    expect(screen.getByText('12-son maktab')).toBeInTheDocument();
    expect(table.getByText('9-B')).toBeInTheDocument();
    // "INTJ" filtr paneldagi "Shaxsiyat tipi" select'ida ham `<option>` sifatida bor —
    // shu sabab `within(table)` bilan qamrab olinadi.
    expect(table.getByText('INTJ')).toBeInTheDocument();
    expect(table.getByText('68.4')).toBeInTheDocument();
    expect(table.getByText("O'rtacha faol")).toBeInTheDocument();
    expect(table.getByText('Tahlil qilingan')).toBeInTheDocument();
    expect(table.getByText('Ishonchli')).toBeInTheDocument();
    expect(table.getByText('30.08.2026')).toBeInTheDocument();

    // Analiz qilinmagan o'quvchida bo'sh maydonlar tire bilan ko'rsatiladi.
    expect(screen.getByText('Karimova Nilufar')).toBeInTheDocument();
    expect(screen.getByText('10')).toBeInTheDocument();
  });

  it("qidiruv 400ms debounce bilan URL'ga yoziladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage();

    await screen.findByText('Aliyev Sardor');
    fetchMock.mockClear();

    await user.type(screen.getByLabelText('Qidiruv'), 'Sardor');

    expect(screen.getByTestId('url-probe').textContent).not.toContain('search=');

    await vi.advanceTimersByTimeAsync(450);

    await waitFor(() => {
      expect(screen.getByTestId('url-probe').textContent).toContain('search=Sardor');
    });
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input]) =>
            String(input).includes('/api/admin/students?') &&
            String(input).includes('search=Sardor'),
        ),
      ).toBe(true);
    });
  });

  it("sinf, holat, tip, aktivlik va 'e'tibor talab qiladi' filtrlari URL query'ga yoziladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage();

    await screen.findByText('Aliyev Sardor');

    await user.selectOptions(screen.getByLabelText('Sinf'), '9-sinf');
    await user.selectOptions(screen.getByLabelText('Holat'), 'Tahlil qilingan');
    await user.selectOptions(screen.getByLabelText('Shaxsiyat tipi'), 'INTJ');
    await user.selectOptions(screen.getByLabelText('Aktivlik darajasi'), "O'rtacha faol");
    await user.click(screen.getByLabelText("Faqat e'tibor talab qiladiganlar"));

    await waitFor(() => {
      const url = screen.getByTestId('url-probe').textContent ?? '';
      expect(url).toContain('grade=9');
      expect(url).toContain('status=Analyzed');
      expect(url).toContain('personalityType=INTJ');
      expect(url).toContain('activityLevel=Moderate');
      expect(url).toContain('needsAttention=true');
    });

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input]) =>
            String(input).includes('/api/admin/students?') &&
            String(input).includes('needsAttention=true'),
        ),
      ).toBe(true);
    });
  });

  it('"Sinf" ustuni sarlavhasiga bosilganda saralash URL\'ga yoziladi (grade)', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage();

    await screen.findByText('Aliyev Sardor');
    fetchMock.mockClear();

    const gradeHeader = within(screen.getByRole('table')).getByRole('columnheader', {
      name: 'Sinf',
    });
    await user.click(within(gradeHeader).getByRole('button'));

    await waitFor(() => {
      const url = screen.getByTestId('url-probe').textContent ?? '';
      expect(url).toContain('sort=grade');
      expect(url).toContain('dir=asc');
    });
    expect(gradeHeader).toHaveAttribute('aria-sort', 'ascending');
    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input]) =>
            String(input).includes('/api/admin/students?') && String(input).includes('sort=grade'),
        ),
      ).toBe(true);
    });

    // Qayta bosilsa yo'nalish `desc`ga almashadi (`DataTable`ning umumiy naqshi).
    await user.click(within(gradeHeader).getByRole('button'));
    await waitFor(() => {
      const url = screen.getByTestId('url-probe').textContent ?? '';
      expect(url).toContain('sort=grade');
      expect(url).toContain('dir=desc');
    });
    expect(gradeHeader).toHaveAttribute('aria-sort', 'descending');
  });

  it("chuqur havoladan (deep-link) boshlang'ich filtr holati to'g'ri o'qiladi", async () => {
    const fetchMock = mockFetch();
    renderStudentsPage(
      '/admin/students?schoolId=school-1&grade=9&status=Analyzed&needsAttention=true&from=2026-08-01&to=2026-08-31',
    );

    await screen.findByText('Aliyev Sardor');

    const filtersBar = within(screen.getByTestId('students-filters'));
    expect(filtersBar.getByLabelText('Sinf')).toHaveValue('9');
    expect(filtersBar.getByLabelText('Holat')).toHaveValue('Analyzed');
    expect(filtersBar.getByLabelText("Faqat e'tibor talab qiladiganlar")).toBeChecked();
    expect(filtersBar.getByLabelText('Sanadan')).toHaveValue('2026-08-01');
    expect(filtersBar.getByLabelText('Sanagacha')).toHaveValue('2026-08-31');

    // Maktab kombobox'i `schoolId`ga mos nomni backenddan olib ko'rsatadi.
    await waitFor(() => {
      expect(filtersBar.getByLabelText('Maktab')).toHaveValue('12-son maktab');
    });

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => {
          const u = String(input);
          return (
            u.includes('/api/admin/students?') &&
            u.includes('schoolId=school-1') &&
            u.includes('grade=9') &&
            u.includes('status=Analyzed') &&
            u.includes('needsAttention=true') &&
            u.includes('from=2026-08-01') &&
            u.includes('to=2026-08-31')
          );
        }),
      ).toBe(true);
    });
  });

  it("faol filtr chipini olib tashlash URL'ni va so'rovni yangilaydi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage('/admin/students?grade=9&status=Analyzed');

    await screen.findByText('Aliyev Sardor');
    expect(screen.getByText('Sinf: 9')).toBeInTheDocument();
    expect(screen.getByText('Holat: Tahlil qilingan')).toBeInTheDocument();

    fetchMock.mockClear();
    await user.click(screen.getByLabelText('Sinf: 9 filtrini olib tashlash'));

    await waitFor(() => {
      const url = screen.getByTestId('url-probe').textContent ?? '';
      expect(url).not.toContain('grade=');
      expect(url).toContain('status=Analyzed');
    });
    expect(screen.queryByText('Sinf: 9')).not.toBeInTheDocument();
    expect(screen.getByText('Holat: Tahlil qilingan')).toBeInTheDocument();

    await user.click(screen.getByText('Hammasini tozalash'));
    await waitFor(() => {
      expect(screen.queryByText('Holat: Tahlil qilingan')).not.toBeInTheDocument();
    });
    expect(screen.getByTestId('url-probe').textContent).not.toContain('status=');
  });

  it("needsAttention qatorida chap chekkada belgi bo'ladi, butun qator bo'yalmaydi", async () => {
    mockFetch();
    renderStudentsPage();

    await screen.findByText('Aliyev Sardor');

    const markedRow = screen.getByText('Aliyev Sardor').closest('tr');
    const unmarkedRow = screen.getByText('Karimova Nilufar').closest('tr');
    expect(markedRow).not.toBeNull();
    expect(unmarkedRow).not.toBeNull();

    const marker = within(markedRow!).getByTestId('needs-attention-marker');
    expect(marker).toBeInTheDocument();
    expect(within(unmarkedRow!).queryByTestId('needs-attention-marker')).not.toBeInTheDocument();

    // Butun qator emas, faqat chap chekkadagi ingichka chiziq bo'yaladi — `tr`ning o'zida
    // fon rangi klassi yo'q.
    expect(markedRow?.className ?? '').not.toMatch(/bg-(warning|danger)/);
  });

  it("eksport tugmasi muvaffaqiyatli yuklaydi va tasdiq ko'rsatadi", async () => {
    mockFetch();
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    // `{...URL}` bilan spread qilib bo'lmaydi — `createObjectURL`/`revokeObjectURL` klass
    // ustida `static` metod sifatida enumerable EMAS, spread ularni tashlab ketadi va
    // `triggerBlobDownload` "is not a function" bilan yiqiladi (birinchi urinishda topilgan
    // xato — pastdagi to'g'ridan-to'g'ri xususiyat belgilash shu sabab ishlatiladi).
    const originalCreateObjectURL: unknown = URL.createObjectURL;
    const originalRevokeObjectURL: unknown = URL.revokeObjectURL;
    const createObjectURLSpy = vi.fn(() => 'blob:mock-url');
    const revokeObjectURLSpy = vi.fn();
    URL.createObjectURL = createObjectURLSpy;
    URL.revokeObjectURL = revokeObjectURLSpy;
    try {
      const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
      renderStudentsPage();

      await screen.findByText('Aliyev Sardor');
      await user.click(screen.getByRole('button', { name: "Excel'ga eksport" }));

      expect(await screen.findByText('Fayl yuklab olindi')).toBeInTheDocument();
      expect(clickSpy).toHaveBeenCalled();
      expect(createObjectURLSpy).toHaveBeenCalled();
      expect(revokeObjectURLSpy).toHaveBeenCalledWith('blob:mock-url');
    } finally {
      clickSpy.mockRestore();
      URL.createObjectURL = originalCreateObjectURL as typeof URL.createObjectURL;
      URL.revokeObjectURL = originalRevokeObjectURL as typeof URL.revokeObjectURL;
    }
  });

  it("eksport endpointi hali yo'q bo'lsa (404) tushunarli xabar ko'rsatadi, ilova yiqilmaydi", async () => {
    mockFetch({ exportResponse: () => new Response(null, { status: 404 }) });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage();

    await screen.findByText('Aliyev Sardor');
    await user.click(screen.getByRole('button', { name: "Excel'ga eksport" }));

    expect(await screen.findByText('Eksport hali mavjud emas')).toBeInTheDocument();
    // Ilova yiqilmagan — sahifa hali ishlaydi.
    expect(screen.getByText('Aliyev Sardor')).toBeInTheDocument();
  });

  it("bo'sh ro'yxatda tushunarli bo'sh holat ko'rsatiladi", async () => {
    mockFetch({ students: [] });
    renderStudentsPage();

    expect(await screen.findByText("Hali o'quvchi yo'q")).toBeInTheDocument();
    expect(
      screen.getByText("Hali o'quvchi yo'q — maktab havolasini ulashing."),
    ).toBeInTheDocument();
  });

  /**
   * P48 — manba bo'yicha ajratish. Ommaviy makon endi maktablar ro'yxatida YO'Q, shu sabab
   * ommaviy foydalanuvchilarga tushishning yagona yo'li shu filtr; ustun esa filtr
   * tanlanmagan bo'lsa ham har qatorning manbasini oshkor qiladi.
   */
  it("har qatorda manba ustuni ko'rsatiladi (maktab / ommaviy)", async () => {
    mockFetch();
    renderStudentsPage();

    await screen.findByText('Aliyev Sardor');
    const table = within(screen.getByRole('table'));

    const schoolRow = within(table.getByText('Aliyev Sardor').closest('tr')!);
    expect(schoolRow.getByText('Maktab')).toBeInTheDocument();

    const publicRow = within(table.getByText('Karimova Nilufar').closest('tr')!);
    expect(publicRow.getByText('Ommaviy makon')).toBeInTheDocument();
  });

  it("manba filtri URL'ga yoziladi va so'rovga `source` parametri qo'shiladi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage();

    await screen.findByText('Aliyev Sardor');
    fetchMock.mockClear();

    await user.selectOptions(screen.getByLabelText('Manba'), 'public');

    await waitFor(() => {
      expect(screen.getByTestId('url-probe').textContent).toContain('source=public');
    });
    await waitFor(() => {
      expect(fetchMock.mock.calls.some(([input]) => String(input).includes('source=public'))).toBe(
        true,
      );
    });
  });

  it("chuqur havoladan `source` o'qiladi va faol filtr chipi ko'rsatiladi", async () => {
    const fetchMock = mockFetch();
    renderStudentsPage('/admin/students?source=public');

    await screen.findByText('Aliyev Sardor');

    expect(screen.getByLabelText('Manba')).toHaveValue('public');
    expect(screen.getByText('Manba: Ommaviy makon')).toBeInTheDocument();
    await waitFor(() => {
      expect(fetchMock.mock.calls.some(([input]) => String(input).includes('source=public'))).toBe(
        true,
      );
    });
  });

  it('eksport ham joriy manba filtri bilan yuboriladi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage('/admin/students?source=public');

    await screen.findByText('Aliyev Sardor');
    await user.click(screen.getByRole('button', { name: "Excel'ga eksport" }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([input]) =>
            String(input).includes('/api/admin/students/export') &&
            String(input).includes('source=public'),
        ),
      ).toBe(true);
    });
  });

  it("server xato bersa qayta urinish tugmasi bilan xato holati ko'rsatiladi", async () => {
    const fetchMock = mockFetch({ studentsErrorStatus: 500 });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderStudentsPage();

    const retryButton = await screen.findByRole('button', { name: 'Qayta urinish' });
    const callsBeforeRetry = fetchMock.mock.calls.length;

    await user.click(retryButton);

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsBeforeRetry);
    });
  });
});
