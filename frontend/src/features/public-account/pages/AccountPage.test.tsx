import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { emptyResponse, jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { setPublicAccessToken } from '@/shared/api/publicUserClient';
import AccountPage from './AccountPage';
import { usePublicUserStore } from '../store/publicUserStore';

const USER = {
  id: 'user-1',
  username: 'alivali',
  firstName: 'Ali',
  lastName: 'Valiyev',
  photoUrl: null,
  createdAt: '2026-09-01T10:12:00Z',
  lastLoginAt: '2026-09-05T10:12:00Z',
} satisfies Schemas['PublicUserDto'];

const ASSESSMENTS = {
  items: [
    {
      id: 'assessment-1',
      status: 'Analyzed',
      startedAt: '2026-09-01T09:00:00Z',
      completedAt: '2026-09-01T09:48:00Z',
      programCode: 'PERSONALITY_PROFILE',
      programName: 'Shaxsiyat profili',
      resultAvailable: true,
    },
    {
      id: 'assessment-2',
      status: 'Analyzing',
      startedAt: '2026-09-04T09:00:00Z',
      completedAt: null,
      programCode: 'PERSONALITY_PROFILE',
      programName: 'Shaxsiyat profili',
      resultAvailable: false,
    },
  ],
} satisfies Schemas['ListMyAssessmentsResult'];

const PROFILE = {
  hasProfile: true,
  fullName: 'Karimov Sardor Alisherovich',
  birthDate: '1995-04-12',
  gender: 'Male',
  phone: '+998901234567',
  grade: null,
  email: null,
  consentVersion: '1.0',
  consentCurrent: true,
  parentalConsent: false,
  isMinor: false,
  suggestedFullName: 'Valiyev Ali',
} satisfies Schemas['MyStudentProfileDto'];

/** Tugallanmagan sessiya bilan tarix — kabinet "Davom ettirish" holatiga o'tadi. */
const IN_PROGRESS_ASSESSMENT = {
  id: 'assessment-3',
  status: 'InProgress',
  startedAt: '2026-09-05T09:00:00Z',
  completedAt: null,
  programCode: 'PERSONALITY_PROFILE',
  programName: 'Shaxsiyat profili',
  resultAvailable: false,
} satisfies Schemas['MyAssessmentDto'];

const ASSESSMENTS_WITH_UNFINISHED = {
  items: [IN_PROGRESS_ASSESSMENT, ...ASSESSMENTS.items],
} satisfies Schemas['ListMyAssessmentsResult'];

/** `POST /api/me/sessions` `{}` javobi: mavjud sessiya qaytarildi, 1-blok tugagan, 2-blok yarim. */
const RESUMED_SESSION = {
  sessionToken: 'session-token-resumed',
  assessmentId: 'assessment-3',
  status: 'InProgress',
  expiresAt: '2026-09-12T10:12:00Z',
  resumed: true,
  tests: [
    { code: 'BIG5', name: 'Xarakter', status: 'InProgress', answered: 17, total: 44, order: 2, estimatedMinutes: 10 },
    { code: 'MBTI16', name: 'Shaxsiyat', status: 'Completed', answered: 60, total: 60, order: 1, estimatedMinutes: 9 },
  ],
} satisfies Schemas['StartSessionResult'];

/** `fetch` ni URL bo'yicha yo'naltiradi: profil → `profile`, qolgani → tarix. */
function mockApi(profile: Schemas['MyStudentProfileDto'], assessments = ASSESSMENTS) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    if (String(input).includes('/api/me/profile')) {
      return Promise.resolve(jsonResponse<'MyStudentProfileDto'>(profile));
    }
    return Promise.resolve(jsonResponse<'ListMyAssessmentsResult'>(assessments));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

/**
 * `fetch`: sessiya so'rovi (`POST /api/me/sessions`) → `sessionResponse`, qolgani → tarix.
 * `sessionCall()` — yuborilgan so'rov (URL, init) yoki `undefined`.
 */
function mockResumeApi(sessionResponse: () => Response, assessments = ASSESSMENTS_WITH_UNFINISHED) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    if (String(input).includes('/api/me/sessions')) {
      return Promise.resolve(sessionResponse());
    }
    if (String(input).includes('/api/me/profile')) {
      return Promise.resolve(jsonResponse<'MyStudentProfileDto'>(PROFILE));
    }
    return Promise.resolve(jsonResponse<'ListMyAssessmentsResult'>(assessments));
  });
  vi.stubGlobal('fetch', fetchMock);
  return {
    sessionCall: () =>
      fetchMock.mock.calls.find(([url]) => String(url).includes('/api/me/sessions')) as
        | [string, RequestInit]
        | undefined,
  };
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/kabinet']}>
          <Routes>
            <Route path="/kabinet" element={<AccountPage />} />
            <Route path="/kabinet/test" element={<p>ANKETA_STUB</p>} />
            <Route path="/t/:slug/test/:testCode" element={<p>TEST_STUB</p>} />
            <Route path="/" element={<p>BOSH_SAHIFA_STUB</p>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function signIn() {
  usePublicUserStore.getState().setSession('access-1', USER);
}

/**
 * jsdom `HTMLDialogElement.showModal()` ni amalga oshirmaydi (`Dialog.tsx` dagi himoyalangan
 * chaqiruvga qarang), shu sabab dialog `open` atributisiz qoladi va `getByRole` (yashirin
 * elementlarni chiqarib tashlaydi) uning ichidagini topa olmaydi — `SchoolsPage.test.tsx`
 * dagi bilan bir xil holat. Dialog ichidagi tugma shu sabab matn bo'yicha olinadi.
 */
function dialogButton(label: string): HTMLElement {
  const node = screen.getByText(label).closest('button');
  if (!node) throw new Error(`Dialogda "${label}" tugmasi topilmadi`);
  return node;
}

describe('AccountPage', () => {
  beforeEach(() => {
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    setPublicAccessToken(null);
    usePublicUserStore.getState().clear();
  });

  it("profilni va test tarixini ko'rsatadi, natijaga havola beradi", async () => {
    signIn();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'ListMyAssessmentsResult'>(ASSESSMENTS)),
    );

    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Ali Valiyev');
    expect(screen.getByText(/01\.09\.2026/)).toBeInTheDocument();

    const [ready, pending] = await screen.findAllByRole('listitem');
    expect(pending).toBeDefined();

    // Natijasi tayyor sessiyada havola bor, tayyor bo'lmaganida — yo'q (bayroq backenddan).
    expect(
      within(ready as HTMLElement).getByRole('link', { name: /Natijani ko'rish/ }),
    ).toHaveAttribute('href', '/kabinet/natijalar/assessment-1');
    expect(within(pending as HTMLElement).queryByRole('link')).not.toBeInTheDocument();
    expect(within(pending as HTMLElement).getByText('Natija hali ochilmagan')).toBeInTheDocument();
    // Yakunlangan sessiyalarda "Davom ettirish" YO'Q, tepadagi karta ham yo'q.
    expect(screen.queryByRole('button', { name: /Davom ettirish/ })).not.toBeInTheDocument();
    expect(screen.queryByText('Sizda tugallanmagan test bor')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Yangi test boshlash/ })).toHaveAttribute(
      'href',
      '/kabinet/test',
    );
  });

  describe('tugallanmagan sessiya', () => {
    it("`InProgress` qatorida \"Davom ettirish\" bor, natija havolasi yo'q; tepada karta, \"Yangi test\" o'rnida \"Davom ettirish\"", async () => {
      signIn();
      mockResumeApi(() => jsonResponse<'StartSessionResult'>(RESUMED_SESSION));

      renderPage();

      const [inProgress, analyzed] = await screen.findAllByRole('listitem');
      expect(
        within(inProgress as HTMLElement).getByRole('button', { name: /Davom ettirish/ }),
      ).toBeInTheDocument();
      expect(within(inProgress as HTMLElement).queryByRole('link')).not.toBeInTheDocument();
      expect(within(inProgress as HTMLElement).getByText('Davom etmoqda')).toBeInTheDocument();
      // Yakunlangan qator avvalgidek — natija havolasi, "Davom ettirish" yo'q.
      expect(within(analyzed as HTMLElement).getByRole('link', { name: /Natijani ko'rish/ })).toHaveAttribute(
        'href',
        '/kabinet/natijalar/assessment-1',
      );
      expect(within(analyzed as HTMLElement).queryByRole('button')).not.toBeInTheDocument();

      // Tepadagi karta: dastur nomi va boshlangan sana bilan.
      expect(screen.getByRole('heading', { name: 'Sizda tugallanmagan test bor' })).toBeInTheDocument();
      expect(screen.getByText(/Shaxsiyat profili · boshlangan: 05\.09\.2026/)).toBeInTheDocument();
      // Server ikkinchi sessiya ochmaydi — "Yangi test boshlash" havolasi ko'rsatilmaydi.
      expect(screen.queryByRole('link', { name: /Yangi test boshlash/ })).not.toBeInTheDocument();
    });

    it('"Davom ettirish" → `POST /api/me/sessions` `{}`, token `sessionStore` ga, birinchi tugallanmagan blokga', async () => {
      const user = userEvent.setup();
      signIn();
      const api = mockResumeApi(() => jsonResponse<'StartSessionResult'>(RESUMED_SESSION));

      renderPage();
      const [inProgress] = await screen.findAllByRole('listitem');
      await user.click(within(inProgress as HTMLElement).getByRole('button', { name: /Davom ettirish/ }));

      // `order` bo'yicha birinchi tugallanmagan blok — BIG5 (MBTI16 tugagan).
      expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();

      const [url, init] = api.sessionCall()!;
      expect(String(url)).toContain('/api/me/sessions');
      expect(init.method).toBe('POST');
      expect((init.headers as Record<string, string>).Authorization).toBe('Bearer access-1');
      // Shaxsiy ma'lumot yuborilmaydi — server mavjud sessiyani o'zi topadi.
      expect(JSON.parse(String(init.body))).toEqual({});

      // Yangi token `sessionStore` persist kalitiga tushdi — test oqimi `X-Session-Token` sifatida yuboradi.
      const persisted = JSON.parse(localStorage.getItem(STORAGE_KEYS.session) ?? '{}') as {
        state?: { sessionToken?: string; slug?: string; assessmentId?: string };
      };
      expect(persisted.state?.sessionToken).toBe('session-token-resumed');
      expect(persisted.state?.slug).toBe('ommaviy');
      expect(persisted.state?.assessmentId).toBe('assessment-3');
    });

    it("tepadagi kartadan ham davom ettiriladi va \"davom ettirildi\" bildirishnomasi chiqadi", async () => {
      const user = userEvent.setup();
      signIn();
      mockResumeApi(() => jsonResponse<'StartSessionResult'>(RESUMED_SESSION));

      renderPage();
      const card = (await screen.findByRole('heading', { name: 'Sizda tugallanmagan test bor' })).closest(
        'section',
      );
      await user.click(within(card as HTMLElement).getByRole('button', { name: /Davom ettirish/ }));

      expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
      expect(screen.getByText('Tugallanmagan sessiyangiz davom ettirildi.')).toBeInTheDocument();
    });

    it("`400 VALIDATION_ERROR` (rozilik eskirgan) → anketa sahifasiga (`/kabinet/test`)", async () => {
      const user = userEvent.setup();
      signIn();
      mockResumeApi(() =>
        problemResponse('VALIDATION_ERROR', 400, 'Xato', {
          errors: { consentAccepted: ['Rozilik talab qilinadi.'] },
        }),
      );

      renderPage();
      const [inProgress] = await screen.findAllByRole('listitem');
      await user.click(within(inProgress as HTMLElement).getByRole('button', { name: /Davom ettirish/ }));

      expect(await screen.findByText('ANKETA_STUB')).toBeInTheDocument();
    });

    it("`409 NO_PROGRAM_AVAILABLE` uchun xarita matni ko'rsatiladi, sahifa o'zgarmaydi", async () => {
      const user = userEvent.setup();
      signIn();
      mockResumeApi(() => problemResponse('NO_PROGRAM_AVAILABLE', 409));

      renderPage();
      const [inProgress] = await screen.findAllByRole('listitem');
      await user.click(within(inProgress as HTMLElement).getByRole('button', { name: /Davom ettirish/ }));

      expect(await screen.findByRole('alert')).toHaveTextContent(/ochiq test dasturi yo'q/);
      expect(screen.queryByText('TEST_STUB')).not.toBeInTheDocument();
      expect(localStorage.getItem(STORAGE_KEYS.session)).toBeNull();
    });
  });

  it("saqlangan anketa (F.I.Sh., sana, telefon) va \"O'zgartirish\" havolasi ko'rsatiladi", async () => {
    signIn();
    mockApi(PROFILE);

    renderPage();

    expect(
      await screen.findByRole('heading', { name: "Sizning ma'lumotlaringiz" }),
    ).toBeInTheDocument();
    expect(screen.getByText('Karimov Sardor Alisherovich')).toBeInTheDocument();
    expect(screen.getByText('12.04.1995')).toBeInTheDocument();
    expect(screen.getByText('+998 (90) 123-45-67')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /O'zgartirish/ })).toHaveAttribute(
      'href',
      '/kabinet/test?edit=1',
    );
  });

  it("anketa hali to'ldirilmagan bo'lsa karta ko'rsatilmaydi", async () => {
    signIn();
    mockApi({ ...PROFILE, hasProfile: false, fullName: null, birthDate: null, phone: null });

    renderPage();
    await screen.findAllByRole('listitem');

    expect(
      screen.queryByRole('heading', { name: "Sizning ma'lumotlaringiz" }),
    ).not.toBeInTheDocument();
  });

  it("bo'sh tarixda tushunarli holat va test boshlash taklifi chiqadi", async () => {
    signIn();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'ListMyAssessmentsResult'>({ items: [] })),
    );

    renderPage();

    expect(await screen.findByText('Hali test topshirmagansiz')).toBeInTheDocument();
    const startLinks = screen.getAllByRole('link', { name: /Yangi test boshlash/ });
    expect(startLinks[0]).toHaveAttribute('href', '/kabinet/test');
  });

  it("tarix so'rovi xato bersa qayta urinish tugmasi ko'rsatiladi", async () => {
    signIn();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('SERVER_ERROR', 500)));

    renderPage();

    expect(await screen.findByRole('alert')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it("akkauntni o'chirish tasdiqsiz bajarilmaydi va oqibatlari yozilgan", async () => {
    const user = userEvent.setup();
    signIn();
    const fetchMock = vi
      .fn()
      .mockResolvedValue(jsonResponse<'ListMyAssessmentsResult'>({ items: [] }));
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await screen.findByText('Hali test topshirmagansiz');

    await user.click(screen.getByRole('button', { name: "Akkauntni o'chirish" }));

    expect(screen.getByText("Akkauntni o'chirasizmi?")).toBeInTheDocument();
    expect(screen.getByText(/YANGI akkaunt ochiladi/)).toBeInTheDocument();
    // Dialog ochilishining O'ZI hech narsani o'chirmaydi.
    expect(
      fetchMock.mock.calls.some((call: unknown[]) => (call[1] as RequestInit).method === 'DELETE'),
    ).toBe(false);
  });

  it('tasdiqlangach `DELETE /api/me` yuboriladi va sessiya tozalanadi', async () => {
    const user = userEvent.setup();
    signIn();
    const fetchMock = vi
      .fn()
      .mockImplementation((_input: RequestInfo | URL, init?: RequestInit) => {
        if (init?.method === 'DELETE') {
          return Promise.resolve(emptyResponse(204));
        }
        return Promise.resolve(jsonResponse<'ListMyAssessmentsResult'>({ items: [] }));
      });
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await screen.findByText('Hali test topshirmagansiz');

    await user.click(screen.getByRole('button', { name: "Akkauntni o'chirish" }));
    await user.click(dialogButton("Ha, o'chirilsin"));

    expect(await screen.findByText('BOSH_SAHIFA_STUB')).toBeInTheDocument();
    expect(usePublicUserStore.getState().accessToken).toBeNull();
    expect(usePublicUserStore.getState().status).toBe('anonymous');
  });

  it("chiqish `logout` so'rovini yuboradi va bosh sahifaga qaytaradi", async () => {
    const user = userEvent.setup();
    signIn();
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      if (String(input).includes('/logout')) {
        return Promise.resolve(emptyResponse(204));
      }
      return Promise.resolve(jsonResponse<'ListMyAssessmentsResult'>({ items: [] }));
    });
    vi.stubGlobal('fetch', fetchMock);

    renderPage();
    await screen.findByText('Hali test topshirmagansiz');

    await user.click(screen.getByRole('button', { name: 'Chiqish' }));

    expect(await screen.findByText('BOSH_SAHIFA_STUB')).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.some((call: unknown[]) =>
        String(call[0]).includes('/api/auth/telegram/logout'),
      ),
    ).toBe(true);
    expect(usePublicUserStore.getState().status).toBe('anonymous');
  });
});
