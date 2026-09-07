import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { pagedResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { PublicSpaceUsersSection } from './PublicSpaceUsersSection';

/**
 * `GET /api/admin/public-space/users` fixturalari — backend `AdminPublicUserListItemDto`.
 * `satisfies` — sxema o'zgarsa `tsc` qizaradi, test jimgina yashil qolmaydi.
 */

/** 2-blokda 17/44 savolda to'xtagan foydalanuvchi (egasining misoli). */
const IN_PROGRESS_USER = {
  publicUserId: 'user-in-progress',
  telegram: { firstName: 'Ali', lastName: 'Valiyev', username: 'alivaliyev' },
  registeredAt: '2026-09-01T10:00:00Z',
  lastLoginAt: '2026-09-06T08:30:00Z',
  studentId: 'student-1',
  fullName: 'Valiyev Ali Akramovich',
  age: 17,
  grade: 9,
  assessments: { total: 3, completed: 2, inProgress: 1 },
  lastAssessment: {
    id: 'assessment-1',
    status: 'InProgress',
    startedAt: '2026-09-06T08:30:00Z',
    completedAt: null,
    progress: {
      testsTotal: 4,
      testsCompleted: 1,
      currentTestNumber: 2,
      currentTestCode: 'BIG5',
      currentTestName: 'Besh omil',
      answered: 17,
      questionsTotal: 44,
    },
  },
} satisfies Schemas['AdminPublicUserListItemDto'];

/** Oxirgi sessiyasi yakunlangan — `progress` YO'Q. */
const COMPLETED_USER = {
  publicUserId: 'user-completed',
  telegram: { firstName: 'Zulfiya', lastName: null, username: 'zulfiya_k' },
  registeredAt: '2026-08-20T10:00:00Z',
  lastLoginAt: '2026-08-25T10:00:00Z',
  studentId: 'student-2',
  fullName: 'Karimova Zulfiya',
  age: 24,
  grade: null,
  assessments: { total: 1, completed: 1, inProgress: 0 },
  lastAssessment: {
    id: 'assessment-2',
    status: 'Completed',
    startedAt: '2026-08-25T10:00:00Z',
    completedAt: '2026-08-25T10:40:00Z',
  },
} satisfies Schemas['AdminPublicUserListItemDto'];

/** Ro'yxatdan o'tgan, lekin anketa to'ldirmagan — `Student` yo'q. */
const NEVER_STARTED_USER = {
  publicUserId: 'user-never-started',
  telegram: { firstName: null, lastName: null, username: 'yangi_user' },
  registeredAt: '2026-09-05T10:00:00Z',
  lastLoginAt: '2026-09-05T10:00:00Z',
  studentId: null,
  fullName: null,
  age: null,
  grade: null,
  assessments: { total: 0, completed: 0, inProgress: 0 },
} satisfies Schemas['AdminPublicUserListItemDto'];

interface FetchMockOptions {
  users?: Schemas['AdminPublicUserListItemDto'][];
  errorStatus?: number;
}

function mockFetch(options: FetchMockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/public-space/users')) {
      if (options.errorStatus) {
        return Promise.resolve(problemResponse('INTERNAL_ERROR', options.errorStatus));
      }
      return Promise.resolve(
        pagedResponse<'AdminPublicUserListItemDto'>(
          options.users ?? [IN_PROGRESS_USER, COMPLETED_USER, NEVER_STARTED_USER],
        ),
      );
    }
    return Promise.reject(new Error(`kutilmagan so'rov: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderSection() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/admin/ommaviy']}>
        <Routes>
          <Route path="/admin/ommaviy" element={<PublicSpaceUsersSection />} />
          <Route path="/admin/students/:id" element={<div>STUDENT_PROFILE_STUB</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('PublicSpaceUsersSection', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("jadval qatorlari ko'rinadi: Telegram ism, @username, F.I.Sh. va testlar soni", async () => {
    mockFetch();
    renderSection();

    // Jadval yuklanish skeleti bilan ham render bo'ladi — avval ma'lumotni kutamiz.
    await screen.findByText('Ali Valiyev');
    const table = within(screen.getByRole('table', { name: 'Foydalanuvchilar jadvali' }));
    expect(table.getByText('Ali Valiyev')).toBeInTheDocument();
    expect(table.getByText('@alivaliyev')).toBeInTheDocument();
    expect(table.getByText('Valiyev Ali Akramovich')).toBeInTheDocument();
    expect(table.getByText('· 17 yosh · 9-sinf')).toBeInTheDocument();
    expect(table.getByText('2 / 3 tugallangan')).toBeInTheDocument();
    // Ro'yxatdan o'tgan / oxirgi kirish sanalari.
    expect(table.getByText('01.09.2026')).toBeInTheDocument();
    expect(table.getByText('06.09.2026')).toBeInTheDocument();
  });

  it("hech kim ro'yxatdan o'tmagan bo'lsa bo'sh holat ko'rsatiladi", async () => {
    mockFetch({ users: [] });
    renderSection();

    expect(await screen.findByText("Hali hech kim ro’yxatdan o’tmagan")).toBeInTheDocument();
    expect(
      screen.getByText("Foydalanuvchilar Telegram orqali kirgach shu yerda ko’rinadi."),
    ).toBeInTheDocument();
  });

  it("jarayondagi sessiyada qayerda to'xtagani va progress chizig'i ko'rsatiladi", async () => {
    mockFetch({ users: [IN_PROGRESS_USER] });
    renderSection();

    const row = (await screen.findByText('2-blok · 17/44 savol')).closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getByText('Besh omil')).toBeInTheDocument();
    // 17/44 = 38.6% → 39.
    expect(within(row!).getByRole('progressbar')).toHaveAttribute('aria-valuenow', '39');
    // "Jarayonda" filtr `<option>`ida ham bor — badge qator ichida qidiriladi.
    expect(within(row!).getByText('Jarayonda')).toBeInTheDocument();
  });

  it("tugallangan sessiyada progress o'rniga “—” va “Tugallangan” badge", async () => {
    mockFetch({ users: [COMPLETED_USER] });
    renderSection();

    const row = (await screen.findByText('Zulfiya')).closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getByText('Tugallangan')).toBeInTheDocument();
    expect(within(row!).getByText('—')).toBeInTheDocument();
    expect(within(row!).queryByRole('progressbar')).not.toBeInTheDocument();
    expect(within(row!).getByText('1 / 1 tugallangan')).toBeInTheDocument();
    // Sinf yo'q (`grade: null`) — faqat yosh.
    expect(within(row!).getByText('· 24 yosh')).toBeInTheDocument();
  });

  it('anketa to‘ldirmagan foydalanuvchi “Boshlamagan” badge bilan ko‘rinadi', async () => {
    mockFetch({ users: [NEVER_STARTED_USER] });
    renderSection();

    // Ism yo'q — `@username` asosiy nom sifatida ko'rinadi.
    const row = (await screen.findByText('@yangi_user')).closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getByText('Boshlamagan')).toBeInTheDocument();
    // Testlar ham, progress ham yo'q.
    expect(within(row!).getAllByText('—')).toHaveLength(2);
    expect(within(row!).queryByRole('link')).not.toBeInTheDocument();
  });

  it("profili bor foydalanuvchi qatori bosilsa `/admin/students/:id` ga o'tiladi", async () => {
    mockFetch({ users: [IN_PROGRESS_USER] });
    const user = userEvent.setup();
    renderSection();

    const row = (await screen.findByText('Ali Valiyev')).closest('tr');
    expect(row).not.toBeNull();
    // F.I.Sh. ham to'g'ridan-to'g'ri havola.
    expect(within(row!).getByRole('link', { name: 'Valiyev Ali Akramovich' })).toHaveAttribute(
      'href',
      '/admin/students/student-1',
    );

    await user.click(row!);

    expect(await screen.findByText('STUDENT_PROFILE_STUB')).toBeInTheDocument();
  });

  it("profili yo'q foydalanuvchi qatori bosilsa hech qayerga o'tilmaydi", async () => {
    mockFetch({ users: [NEVER_STARTED_USER] });
    const user = userEvent.setup();
    renderSection();

    const row = (await screen.findByText('@yangi_user')).closest('tr');
    await user.click(row!);

    expect(screen.queryByText('STUDENT_PROFILE_STUB')).not.toBeInTheDocument();
    expect(screen.getByText('@yangi_user')).toBeInTheDocument();
  });

  it("holat filtri tanlanganda so'rovga `status=in_progress` ketadi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderSection();

    await screen.findByText('Ali Valiyev');
    // Standart holatda `status` parametri yuborilmaydi (`all`).
    expect(String(fetchMock.mock.calls[0]![0])).not.toContain('status=');

    await user.selectOptions(screen.getByLabelText('Holat'), 'in_progress');

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => {
          const url = String(input);
          return url.includes('status=in_progress') && url.includes('page=1');
        }),
      ).toBe(true);
    });
  });

  it("qidiruv matni kechiktirilib (debounce) so'rovga `search=` sifatida ketadi", async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderSection();

    await screen.findByText('Ali Valiyev');
    await user.type(screen.getByLabelText('Qidiruv'), 'Zulfiya');

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([input]) => String(input).includes('search=Zulfiya')),
      ).toBe(true);
    });
  });

  it("filtr faol bo'lib hech kim topilmasa boshqa bo'sh matn ko'rsatiladi", async () => {
    mockFetch({ users: [] });
    const user = userEvent.setup();
    renderSection();

    await screen.findByText("Hali hech kim ro’yxatdan o’tmagan");
    await user.selectOptions(screen.getByLabelText('Holat'), 'completed');

    expect(await screen.findByText('Hech kim topilmadi')).toBeInTheDocument();
  });

  it("server xato bersa qayta urinish tugmasi ko'rsatiladi", async () => {
    const fetchMock = mockFetch({ errorStatus: 500 });
    const user = userEvent.setup();
    renderSection();

    const retry = await screen.findByRole('button', { name: 'Qayta urinish' });
    const before = fetchMock.mock.calls.length;
    await user.click(retry);

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(before);
    });
  });

  it("bu bo'limda “maktab” so'zi ishlatilmaydi", async () => {
    mockFetch();
    const { container } = renderSection();

    await screen.findByText('Ali Valiyev');
    expect(container.textContent ?? '').not.toMatch(/maktab/i);
  });
});
