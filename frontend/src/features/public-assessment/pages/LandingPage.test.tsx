import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { jsonResponse, problemResponse, type Schemas } from '@/test/apiMock';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import LandingPage from './LandingPage';
import { useSessionStore } from '../store/sessionStore';
import { readAnswerStore, upsertAnswer, writeAnswerStore } from '../lib/answerQueue';

const SCHOOL_INFO_BODY = {
  schoolId: 'school-1',
  name: "12-son umumiy o'rta ta'lim maktabi",
  region: "Farg'ona",
  district: "Qo'qon",
  requiresAccessCode: false,
  tests: [
    {
      code: 'MBTI16',
      name: '16 tipli shaxsiyat modeli',
      questionCount: 60,
      estimatedMinutes: 9,
      order: 1,
    },
    {
      code: 'BIG5',
      name: 'Shaxsiyatning 5 omili',
      questionCount: 50,
      estimatedMinutes: 8,
      order: 2,
    },
    { code: 'RIASEC', name: 'Kasb qiziqishlari', questionCount: 48, estimatedMinutes: 7, order: 3 },
    {
      code: 'ACTIVITY',
      name: 'Aktivlik va motivatsiya',
      questionCount: 32,
      estimatedMinutes: 5,
      order: 4,
    },
  ],
  totalEstimatedMinutes: 31,
  consentText: "Farzandimning testdan o'tishiga roziman.",
  // Migratsiyadan keyingi haqiqiy holat — bitta tizim dasturi (`docs/06` 8-bo'lim, `prompts/34`
  // C7-band): `programs.length === 1` bo'lganda `LandingPage` ESKI (tanlovsiz) oqimni ishlatadi.
  programs: [
    {
      code: 'PERSONALITY_PROFILE',
      nameUz: 'Shaxsiyat profili',
      descriptionUz: null,
      testCount: 4,
      questionCount: 190,
      estimatedMinutes: 31,
      hasPersonalityBattery: true,
    },
  ],
} satisfies Schemas['GetSchoolInfoResult'];

const TWO_PROGRAMS_BODY = {
  ...SCHOOL_INFO_BODY,
  programs: [
    {
      code: 'PERSONALITY_PROFILE',
      nameUz: 'Shaxsiyat profili',
      descriptionUz: "4 blokli to'liq baholash",
      testCount: 4,
      questionCount: 190,
      estimatedMinutes: 31,
      hasPersonalityBattery: true,
    },
    {
      code: 'CAREER_SURVEY',
      nameUz: "Kasb so'rovnomasi",
      descriptionUz: "Qisqa so'rovnoma",
      testCount: 1,
      questionCount: 20,
      estimatedMinutes: 4,
      // Shaxsiyat batareyasisiz dastur (`Survey` blok) — `docs/06` 8-bo'lim.
      hasPersonalityBattery: false,
    },
  ],
} satisfies Schemas['GetSchoolInfoResult'];

const NO_PROGRAMS_BODY = {
  ...SCHOOL_INFO_BODY,
  programs: [],
} satisfies Schemas['GetSchoolInfoResult'];

function renderLanding(
  initialPath = '/t/demo-school?k=tok123',
  queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } }),
) {
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/t/:slug" element={<LandingPage />} />
          <Route path="/t/:slug/register" element={<div>REGISTER_STUB</div>} />
          <Route path="/t/:slug/test/:testCode" element={<div>TEST_STUB</div>} />
          <Route path="/t/:slug/finish" element={<div>FINISH_STUB</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('LandingPage', () => {
  beforeEach(() => {
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
    useSessionStore.getState().clear();
  });

  it("maktab ma'lumotini yuklab, sarlavha, 4 ta test kartasi va Boshlash tugmasini ko'rsatadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY)),
    );

    renderLanding();

    expect(await screen.findByText(SCHOOL_INFO_BODY.name)).toBeInTheDocument();
    expect(screen.getByText('16 tipli shaxsiyat modeli')).toBeInTheDocument();
    expect(screen.getByText('Shaxsiyatning 5 omili')).toBeInTheDocument();
    expect(screen.getByText('Kasb qiziqishlari')).toBeInTheDocument();
    expect(screen.getByText('Aktivlik va motivatsiya')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Boshlash' })).toBeInTheDocument();
  });

  it("so'rov to'g'ri manzil va query bilan yuboriladi (k parametri)", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY));
    vi.stubGlobal('fetch', fetchMock);

    renderLanding('/t/demo-school?k=tok123');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    const [url] = fetchMock.mock.calls[0] as [string];
    expect(url).toContain('/api/public/schools/demo-school');
    expect(url).toContain('k=tok123');
  });

  it("404 (havola noto'g'ri) bo'lsa tushunarli xabar ko'rsatadi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('NOT_FOUND', 404)));

    renderLanding();

    expect(await screen.findByText('Havola ishlamayapti')).toBeInTheDocument();
    expect(
      screen.getByText("Havola ishlamayapti, maktabingizdan yangisini so'rang."),
    ).toBeInTheDocument();
  });

  it("410 (maktab nofaol) bo'lsa tegishli xabar ko'rsatadi", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('SCHOOL_INACTIVE', 410)));

    renderLanding();

    expect(await screen.findByText('Test vaqtincha yopilgan')).toBeInTheDocument();
    expect(screen.getByText('Bu maktab uchun test vaqtincha yopilgan.')).toBeInTheDocument();
  });

  // 2026-09-03: maktab va havola TO'G'RI, faqat hozircha mavjud dastur yo'q. Ilgari bu
  // umumiy "Nimadir noto'g'ri ketdi" + "Qayta urinish" ekraniga tushardi — o'quvchi
  // havolani buzuq deb o'ylab maktabga behuda murojaat qilardi, qayta urinish esa hech
  // narsani o'zgartirmasdi.
  it("409 (dastur mavjud emas) bo'lsa tushunarli holat chiqadi, 'Qayta urinish' emas", async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('NO_PROGRAM_AVAILABLE', 409)));

    renderLanding();

    expect(await screen.findByText('Test hali tayyor emas')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Qayta urinish' })).not.toBeInTheDocument();
  });

  it('kutilmagan (masalan 500) xatoda umumiy xato holati va qayta urinish tugmasi chiqadi', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(problemResponse('INTERNAL_ERROR', 500)));

    renderLanding();

    expect(await screen.findByRole('button', { name: 'Qayta urinish' })).toBeInTheDocument();
  });

  it("'Boshlash' bosilganda k parametri saqlangan holda ro'yxatdan o'tish sahifasiga o'tadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY)),
    );
    const user = userEvent.setup();

    renderLanding();
    await user.click(await screen.findByRole('button', { name: 'Boshlash' }));

    expect(await screen.findByText('REGISTER_STUB')).toBeInTheDocument();
  });

  /**
   * `sessions/me` va maktab ma'lumotini birga mock qiladi; `sessions/me` uchun yuborilgan
   * `X-Session-Token` header'larini yig'adi (taklif tokeni aniq uzatilishini tekshirish uchun).
   */
  function mockSchoolAndSessionState(sessionResponse: () => Response) {
    const sessionTokens: Array<string | undefined> = [];
    vi.stubGlobal(
      'fetch',
      vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.includes('/api/public/sessions/me')) {
          sessionTokens.push(
            (init?.headers as Record<string, string> | undefined)?.['X-Session-Token'],
          );
          return Promise.resolve(sessionResponse());
        }
        return Promise.resolve(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY));
      }),
    );
    return { sessionTokens };
  }

  const ACTIVE_SESSION_STATE = {
    assessmentId: 'assessment-1',
    status: 'InProgress',
    student: { firstNameShort: 'Sardor', grade: 9 },
    expiresAt: '2026-09-10T00:00:00Z',
    currentTestCode: 'BIG5',
    tests: [],
    progressPercent: 25,
    hasPersonalityBattery: true,
  } satisfies Schemas['GetSessionStateResult'];

  // ---------------------------------------------------------------------------------------
  // Havola/kod bilan yangi kirish (`?k=`) = toza boshlanish — bir qurilma, bir necha o'quvchi
  // (2026-09-07, egasining qarori). Oldingi o'quvchining sessiyasi, javob navbati va sessiya
  // keshi tozalanadi; "Davom ettirish" faqat xuddi shu maktab + xuddi shu `k` uchun ixtiyoriy
  // taklif. `?k=`siz kelish (test ichidan "orqaga") hech narsani tozalamaydi.
  // ---------------------------------------------------------------------------------------

  it('?k= bilan kelganda BOSHQA maktab sessiyasi tozalanadi, yangi k saqlanadi, taklif chiqmaydi', async () => {
    useSessionStore.getState().setSession('old-token', 'other-school', 'assessment-old', 'k-old');
    const { sessionTokens } = mockSchoolAndSessionState(() =>
      jsonResponse<'GetSessionStateResult'>(ACTIVE_SESSION_STATE),
    );

    renderLanding('/t/demo-school?k=tok123');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    const state = useSessionStore.getState();
    expect(state.sessionToken).toBeNull();
    expect(state.slug).toBeNull();
    expect(state.assessmentId).toBeNull();
    expect(state.accessToken).toBe('tok123');
    expect(state.resumable).toBeNull();
    expect(screen.queryByRole('button', { name: 'Davom ettirish' })).not.toBeInTheDocument();
    // Eski token bilan `sessions/me` umuman so'ralmaydi.
    expect(sessionTokens).toEqual([]);
    expect(screen.getByRole('button', { name: 'Boshlash' })).toBeInTheDocument();
  });

  it("?k= bilan kelganda Telegram ('ommaviy') sessiyasi ham tozalanadi", async () => {
    useSessionStore.getState().setSession('tg-token', 'ommaviy', 'assessment-tg');
    mockSchoolAndSessionState(() => jsonResponse<'GetSessionStateResult'>(ACTIVE_SESSION_STATE));

    renderLanding('/t/demo-school?k=tok123');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(useSessionStore.getState().accessToken).toBe('tok123');
  });

  it('?k= bilan kelganda XUDDI SHU maktab, lekin boshqa k — tozalanadi, taklif chiqmaydi', async () => {
    useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1', 'k-old');
    const { sessionTokens } = mockSchoolAndSessionState(() =>
      jsonResponse<'GetSessionStateResult'>(ACTIVE_SESSION_STATE),
    );

    renderLanding('/t/demo-school?k=tok123');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(useSessionStore.getState().resumable).toBeNull();
    expect(screen.queryByRole('button', { name: 'Davom ettirish' })).not.toBeInTheDocument();
    expect(sessionTokens).toEqual([]);
  });

  it("?k= bilan kelganda xuddi shu maktab + xuddi shu k: faol sessiya tozalanadi, 'Davom ettirish' IXTIYORIY taklif bo'lib qoladi", async () => {
    useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1', 'tok123');
    const { sessionTokens } = mockSchoolAndSessionState(() =>
      jsonResponse<'GetSessionStateResult'>(ACTIVE_SESSION_STATE),
    );
    const user = userEvent.setup();

    renderLanding('/t/demo-school?k=tok123');

    const resumeButton = await screen.findByRole('button', { name: 'Davom ettirish' });
    // Faol sessiya BO'SH — standart yo'l "Boshlash" toza anketaga olib boradi...
    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(screen.getByRole('button', { name: 'Boshlash' })).toBeInTheDocument();
    // ...taklif tokeni `sessions/me`ga aniq uzatilgan (store bo'sh bo'lsa ham tekshiriladi).
    expect(sessionTokens).toContain('sess-token-1');

    // O'quvchi ATAYLAB davom ettirsa — sessiya qaytariladi va joriy blokga o'tadi.
    await user.click(resumeButton);

    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
    expect(useSessionStore.getState().sessionToken).toBe('sess-token-1');
    expect(useSessionStore.getState().assessmentId).toBe('assessment-1');
    expect(useSessionStore.getState().resumable).toBeNull();
  });

  it("?k= BO'LMASA (test ichidan 'orqaga') hech narsa tozalanmaydi — eski xatti-harakat: 'Davom ettirish' ko'rsatiladi", async () => {
    useSessionStore.getState().setSession('sess-token-1', 'demo-school', 'assessment-1', 'tok123');
    writeAnswerStore(
      upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 100 }),
    );
    mockSchoolAndSessionState(() => jsonResponse<'GetSessionStateResult'>(ACTIVE_SESSION_STATE));
    const user = userEvent.setup();

    renderLanding('/t/demo-school');

    const resumeButton = await screen.findByRole('button', { name: 'Davom ettirish' });
    expect(useSessionStore.getState().sessionToken).toBe('sess-token-1');
    expect(useSessionStore.getState().accessToken).toBe('tok123');
    expect(readAnswerStore()).toHaveProperty('q1');

    await user.click(resumeButton);
    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
  });

  it("?k= bilan kelganda oldingi o'quvchining javob navbati (answerQueue) tozalanadi", async () => {
    useSessionStore.getState().setSession('old-token', 'demo-school', 'assessment-old', 'k-old');
    writeAnswerStore(
      upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 100 }),
    );
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY)),
    );

    renderLanding('/t/demo-school?k=tok123');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    expect(readAnswerStore()).toEqual({});
    expect(localStorage.getItem(STORAGE_KEYS.pendingAnswers)).toBe('{}');
  });

  it('?k= bilan kelganda sessiya/savol/natija query keshi bekor qilinadi (removeQueries)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY)),
    );
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    queryClient.setQueryData(QUERY_KEYS.publicSessionMe(), ACTIVE_SESSION_STATE);
    queryClient.setQueryData(QUERY_KEYS.publicTestQuestions('BIG5', 1), { questions: [] });
    queryClient.setQueryData(QUERY_KEYS.publicStudentResult(), {});
    const removeSpy = vi.spyOn(queryClient, 'removeQueries');

    renderLanding('/t/demo-school?k=tok123', queryClient);
    await screen.findByText(SCHOOL_INFO_BODY.name);

    expect(removeSpy).toHaveBeenCalledWith({ queryKey: QUERY_KEYS.publicSessionMe() });
    expect(removeSpy).toHaveBeenCalledWith({ queryKey: ['public', 'test-questions'] });
    expect(removeSpy).toHaveBeenCalledWith({ queryKey: QUERY_KEYS.publicStudentResult() });
    expect(queryClient.getQueryData(QUERY_KEYS.publicSessionMe())).toBeUndefined();
    expect(queryClient.getQueryData(QUERY_KEYS.publicTestQuestions('BIG5', 1))).toBeUndefined();
    expect(queryClient.getQueryData(QUERY_KEYS.publicStudentResult())).toBeUndefined();
    // Maktab ma'lumoti sessiyaga bog'liq emas — tegilmaydi.
    expect(
      queryClient.getQueryData(QUERY_KEYS.publicSchoolInfo('demo-school', 'tok123')),
    ).toBeDefined();
  });

  it("?k= BO'LMASA javob navbati ham, query keshi ham tozalanmaydi", async () => {
    writeAnswerStore(
      upsertAnswer({}, { testCode: 'BIG5', questionId: 'q1', value: 4, durationMs: 100 }),
    );
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY)),
    );
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const removeSpy = vi.spyOn(queryClient, 'removeQueries');

    renderLanding('/t/demo-school', queryClient);
    await screen.findByText(SCHOOL_INFO_BODY.name);

    expect(readAnswerStore()).toHaveProperty('q1');
    expect(removeSpy).not.toHaveBeenCalled();
  });

  it("taklif qilingan sessiya muddati tugagan bo'lsa (410) sessionStore va taklif tozalanadi", async () => {
    useSessionStore.getState().setSession('expired-token', 'demo-school', 'assessment-1', 'tok123');
    mockSchoolAndSessionState(() => problemResponse('SESSION_EXPIRED', 410));

    renderLanding('/t/demo-school?k=tok123');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    await waitFor(() => {
      expect(useSessionStore.getState().resumable).toBeNull();
    });
    expect(useSessionStore.getState().sessionToken).toBeNull();
    expect(screen.queryByRole('button', { name: 'Davom ettirish' })).not.toBeInTheDocument();
  });

  it("?k=siz kelganda saqlangan sessiya muddati tugagan bo'lsa (410) sessionStore tozalanadi", async () => {
    useSessionStore.getState().setSession('expired-token', 'demo-school', 'assessment-1', 'tok123');
    mockSchoolAndSessionState(() => problemResponse('SESSION_EXPIRED', 410));

    renderLanding('/t/demo-school');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    await waitFor(() => {
      expect(useSessionStore.getState().sessionToken).toBeNull();
    });
    expect(screen.queryByRole('button', { name: 'Davom ettirish' })).not.toBeInTheDocument();
  });

  // `prompts/36` — dastur tanlovi.
  it("bitta dastur bo'lganda tanlov ekrani ko'rsatilmaydi (regressiya)", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(SCHOOL_INFO_BODY)),
    );

    renderLanding();

    await screen.findByText(SCHOOL_INFO_BODY.name);
    expect(screen.queryByRole('radiogroup')).not.toBeInTheDocument();
    expect(screen.queryByText('Shaxsiyat profili')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Boshlash' })).toBeEnabled();
  });

  it("bir nechta dastur bo'lganda tanlov kartalarini ko'rsatadi, tanlanmaguncha Boshlash o'chiq bo'ladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(TWO_PROGRAMS_BODY)),
    );
    const user = userEvent.setup();

    renderLanding();

    await screen.findByText(SCHOOL_INFO_BODY.name);
    expect(screen.getByText('Shaxsiyat profili')).toBeInTheDocument();
    expect(screen.getByText("Kasb so'rovnomasi")).toBeInTheDocument();
    const startButton = screen.getByRole('button', { name: 'Boshlash' });
    expect(startButton).toBeDisabled();

    await user.click(screen.getByText('Shaxsiyat profili'));
    expect(startButton).toBeEnabled();
  });

  it("dastur tanlab 'Boshlash' bosilganda tanlov sessionStore'da saqlanadi va ro'yxatdan o'tishga o'tadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(TWO_PROGRAMS_BODY)),
    );
    const user = userEvent.setup();

    renderLanding();

    await user.click(await screen.findByText("Kasb so'rovnomasi"));
    await user.click(screen.getByRole('button', { name: 'Boshlash' }));

    expect(await screen.findByText('REGISTER_STUB')).toBeInTheDocument();
    expect(useSessionStore.getState().selectedProgramCode).toBe('CAREER_SURVEY');
    expect(useSessionStore.getState().selectedProgramSlug).toBe('demo-school');
  });

  it("maktabda dastur yo'q bo'lsa tushunarli xabar ko'rsatadi (Boshlash tugmasisiz)", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse<'GetSchoolInfoResult'>(NO_PROGRAMS_BODY)),
    );

    renderLanding();

    await screen.findByText(SCHOOL_INFO_BODY.name);
    expect(
      screen.getByText('Bu maktab uchun test hali tayyorlanmagan, maktabingizga murojaat qiling.'),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Boshlash' })).not.toBeInTheDocument();
  });
});
