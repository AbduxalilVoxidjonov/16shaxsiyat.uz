import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { jsonResponse, problemResponse, typedResponse, type Schemas } from '@/test/apiMock';
import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { PublicSchoolInfoWithRegistration } from '@/shared/api/registrationModeTypes';
import LandingPage from './LandingPage';
import { useSessionStore } from '../store/sessionStore';
import { readAnswerStore, upsertAnswer, writeAnswerStore } from '../lib/answerQueue';

/** `programs[0].tests` — 1.1-bo'lim, `PERSONALITY_PROFILE` dasturining 4 bloki. */
const PERSONALITY_PROFILE_TESTS = [
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
];

const SCHOOL_INFO_BODY = {
  schoolId: 'school-1',
  name: "12-son umumiy o'rta ta'lim maktabi",
  region: "Farg'ona",
  district: "Qo'qon",
  requiresAccessCode: false,
  tests: PERSONALITY_PROFILE_TESTS,
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
      // P52 (2026-09-11) — `docs/07` §1.1: `Full` bo'lgani sabab `RegistrationPage` avvalgidek
      // ishlaydi (regressiya himoyasi), `tests` esa AYNAN shu dasturning bloklari.
      registrationMode: 'Full',
      tests: PERSONALITY_PROFILE_TESTS,
    },
  ],
} satisfies PublicSchoolInfoWithRegistration;

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
      registrationMode: 'Full',
      tests: PERSONALITY_PROFILE_TESTS,
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
      registrationMode: 'Full',
      tests: [
        {
          code: 'CAREER_SURVEY_Q',
          name: "Kasb so'rovnomasi savollari",
          questionCount: 20,
          estimatedMinutes: 4,
          order: 1,
        },
      ],
    },
  ],
} satisfies PublicSchoolInfoWithRegistration;

const NO_PROGRAMS_BODY = {
  ...SCHOOL_INFO_BODY,
  programs: [],
} satisfies PublicSchoolInfoWithRegistration;

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
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY)),
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
      .mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY));
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
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY)),
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
        return Promise.resolve(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY));
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
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY)),
    );

    renderLanding('/t/demo-school?k=tok123');
    await screen.findByText(SCHOOL_INFO_BODY.name);

    expect(readAnswerStore()).toEqual({});
    expect(localStorage.getItem(STORAGE_KEYS.pendingAnswers)).toBe('{}');
  });

  it('?k= bilan kelganda sessiya/savol/natija query keshi bekor qilinadi (removeQueries)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY)),
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
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY)),
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
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY)),
    );

    renderLanding();

    await screen.findByText(SCHOOL_INFO_BODY.name);
    expect(screen.queryByRole('radiogroup')).not.toBeInTheDocument();
    // Dastur nomi endi faqat SARLAVHADA (h1) chiqadi — tanlov kartasi (radio) sifatida EMAS.
    expect(screen.queryByRole('radio')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Boshlash' })).toBeEnabled();
  });

  // ---------------------------------------------------------------------------------------
  // Sarlavha — katalogdagi nom (egasining talabi, 2026-09-23). "Sen haqingdagi test" kabi
  // umumiy "sen" shaklidagi sarlavha endi YO'Q.
  // ---------------------------------------------------------------------------------------
  it("bitta dastur, bir nechta test: sarlavha va sahifa title'i dastur nomi bo'ladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(SCHOOL_INFO_BODY)),
    );

    renderLanding();

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Shaxsiyat profili' }),
    ).toBeInTheDocument();
    expect(document.title).toContain(`Shaxsiyat profili — ${SCHOOL_INFO_BODY.name}`);
    expect(screen.queryByText(/Sen haqingdagi/)).not.toBeInTheDocument();
  });

  it("bitta dastur, bitta test (yangi so'rovnoma): sarlavha test nomi, xom i18n kalit chiqmaydi", async () => {
    const survey = {
      code: 'INTELLECT-SURVEY',
      name: "Maktab o'quvchilari uchun so'rovnoma",
      description: null,
      questionCount: 12,
      estimatedMinutes: 6,
      order: 1,
    };
    const body = {
      ...SCHOOL_INFO_BODY,
      tests: [survey],
      programs: [
        {
          ...SCHOOL_INFO_BODY.programs[0]!,
          code: 'INTELLECT',
          nameUz: 'Intellekt dasturi',
          testCount: 1,
          questionCount: 12,
          estimatedMinutes: 6,
          hasPersonalityBattery: false,
          tests: [survey],
        },
      ],
    } satisfies PublicSchoolInfoWithRegistration;
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(body)),
    );

    const { container } = renderLanding();

    expect(await screen.findByRole('heading', { level: 1, name: survey.name })).toBeInTheDocument();
    expect(document.title).toContain(`${survey.name} — ${SCHOOL_INFO_BODY.name}`);
    expect(container.textContent).not.toContain('pages.landing');
    expect(container.textContent).not.toContain('testDescriptions');
  });

  it("bir nechta dastur bo'lganda sarlavha neytral umumiy matn ('Testlar')", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(TWO_PROGRAMS_BODY)),
    );

    renderLanding();

    expect(await screen.findByRole('heading', { level: 1, name: 'Testlar' })).toBeInTheDocument();
  });

  it("test kartasida katalog tavsifi i18n matnidan ustun, tavsif bo'lmasa i18n zaxirasi (siz shaklida)", async () => {
    const tests = [
      { ...PERSONALITY_PROFILE_TESTS[0]!, description: 'Katalogdagi MBTI tavsifi' },
      ...PERSONALITY_PROFILE_TESTS.slice(1),
    ];
    const body = {
      ...SCHOOL_INFO_BODY,
      tests,
      programs: [{ ...SCHOOL_INFO_BODY.programs[0]!, tests }],
    } satisfies PublicSchoolInfoWithRegistration;
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(body)),
    );

    renderLanding();

    expect(await screen.findByText('Katalogdagi MBTI tavsifi')).toBeInTheDocument();
    expect(screen.queryByText(/O'zingizga xos fikrlash/)).not.toBeInTheDocument();
    expect(screen.getByText("Xarakteringizning 5 asosiy jihatini o'lchaydi.")).toBeInTheDocument();
    expect(
      screen.getByText(
        "Bu yerda to'g'ri yoki noto'g'ri javob yo'q — faqat sizga xos javoblar bor.",
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText('Bu test baho emas, shuning uchun tashvishlanmang.'),
    ).toBeInTheDocument();
  });

  it("bir nechta dastur bo'lganda tanlov kartalarini ko'rsatadi, tanlanmaguncha Boshlash o'chiq bo'ladi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(TWO_PROGRAMS_BODY)),
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
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(TWO_PROGRAMS_BODY)),
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
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(NO_PROGRAMS_BODY)),
    );

    renderLanding();

    await screen.findByText(SCHOOL_INFO_BODY.name);
    expect(
      screen.getByText('Bu maktab uchun test hali tayyorlanmagan, maktabingizga murojaat qiling.'),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Boshlash' })).not.toBeInTheDocument();
  });

  // ---------------------------------------------------------------------------------------
  // Egasi topgan jonli xato (2026-09-11): dastur arxivlangandan keyin ham uning testlari
  // kirish ekranida ko'rinishda davom etardi, chunki `SingleProgramView` yuqori darajadagi
  // `tests[]`ga (BUTUN katalog) tayanardi. Endi `programs[0].tests` ishlatiladi — bitta
  // dasturli tarmoqda yuqori darajadagi `tests[]` boshqa (arxivlangan) dastur testini o'z
  // ichiga olsa ham u ko'rinmasligi kerak.
  // ---------------------------------------------------------------------------------------
  it("bitta dasturli tarmoqda faqat SHU dasturning testlari ko'rsatiladi (arxivlangan dastur testi sizib chiqmaydi)", async () => {
    const bodyWithStaleTopLevelTests = {
      ...SCHOOL_INFO_BODY,
      // Yuqori darajadagi `tests[]` — endi arxivlangan boshqa dastur testini ham o'z ichiga
      // olganini simulyatsiya qiladi (backend jonli hodisadan oldingi noto'g'ri holat).
      tests: [
        ...PERSONALITY_PROFILE_TESTS,
        {
          code: 'ARCHIVED_PROGRAM_TEST',
          name: 'Arxivlangan dastur testi',
          questionCount: 10,
          estimatedMinutes: 2,
          order: 5,
        },
      ],
    } satisfies PublicSchoolInfoWithRegistration;
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(
          typedResponse<PublicSchoolInfoWithRegistration>(bodyWithStaleTopLevelTests),
        ),
    );

    renderLanding();

    await screen.findByText('16 tipli shaxsiyat modeli');
    expect(screen.queryByText('Arxivlangan dastur testi')).not.toBeInTheDocument();
  });

  // ---------------------------------------------------------------------------------------
  // `registrationMode: "None"` (P52, 2026-09-11, `docs/18` §9) — ro'yxatdan o'tish o'tkazib
  // yuboriladi, rozilik shu ekranda, sessiya to'g'ridan-to'g'ri shu yerdan ochiladi.
  // ---------------------------------------------------------------------------------------
  const NONE_MODE_TESTS = [
    {
      code: 'CAREER_SURVEY_Q',
      name: "Kasb so'rovnomasi savollari",
      questionCount: 20,
      estimatedMinutes: 4,
      order: 1,
    },
  ];

  const NONE_MODE_BODY = {
    schoolId: 'school-1',
    name: "12-son umumiy o'rta ta'lim maktabi",
    region: "Farg'ona",
    district: "Qo'qon",
    requiresAccessCode: false,
    tests: NONE_MODE_TESTS,
    totalEstimatedMinutes: 4,
    consentText: "Farzandimning testdan o'tishiga roziman.",
    programs: [
      {
        code: 'CAREER_SURVEY',
        nameUz: "Kasb so'rovnomasi",
        descriptionUz: null,
        testCount: 1,
        questionCount: 20,
        estimatedMinutes: 4,
        hasPersonalityBattery: false,
        registrationMode: 'None',
        tests: NONE_MODE_TESTS,
      },
    ],
  } satisfies PublicSchoolInfoWithRegistration;

  const ANONYMOUS_START_SESSION_RESULT = {
    sessionToken: 'anon-token-1',
    assessmentId: 'assessment-anon-1',
    status: 'Draft',
    expiresAt: '2026-09-10T00:00:00Z',
    resumed: false,
    tests: [
      {
        code: 'CAREER_SURVEY_Q',
        name: "Kasb so'rovnomasi savollari",
        status: 'NotStarted',
        answered: 0,
        total: 20,
        order: 1,
        estimatedMinutes: 4,
      },
    ],
  } satisfies Schemas['StartSessionResult'];

  function mockNoneModeFetch(overridePostResponse?: () => Response) {
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes('/api/public/schools/')) {
        return Promise.resolve(typedResponse<PublicSchoolInfoWithRegistration>(NONE_MODE_BODY));
      }
      if (url.includes('/api/public/sessions') && init?.method === 'POST') {
        return Promise.resolve(
          overridePostResponse
            ? overridePostResponse()
            : jsonResponse<'StartSessionResult'>(ANONYMOUS_START_SESSION_RESULT),
        );
      }
      return Promise.reject(new Error(`unexpected fetch: ${url}`));
    });
    vi.stubGlobal('fetch', fetchMock);
    return fetchMock;
  }

  it("registrationMode 'None' dasturda ro'yxatdan o'tish o'tkazib yuboriladi: rozilik ekranida ko'rsatiladi, belgilanmaguncha Boshlash o'chiq", async () => {
    mockNoneModeFetch();

    renderLanding();

    await screen.findByText(NONE_MODE_BODY.name);
    expect(screen.getByText(NONE_MODE_BODY.consentText)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Boshlash' })).toBeDisabled();
  });

  it("registrationMode 'None' dasturda rozilik belgilab 'Boshlash' bosilganda RegistrationPage ochilmaydi — sessiya to'g'ridan-to'g'ri shu yerdan, shaxs maydonlarisiz ochiladi", async () => {
    const fetchMock = mockNoneModeFetch();
    const user = userEvent.setup();

    renderLanding();

    await screen.findByText(NONE_MODE_BODY.name);
    await user.click(
      screen.getByLabelText("Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman"),
    );
    await user.click(screen.getByRole('button', { name: 'Boshlash' }));

    // `RegistrationPage` UMUMAN ochilmagan — to'g'ridan-to'g'ri test blokiga o'tadi.
    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
    expect(screen.queryByText('REGISTER_STUB')).not.toBeInTheDocument();
    expect(useSessionStore.getState().sessionToken).toBe('anon-token-1');
    expect(useSessionStore.getState().assessmentId).toBe('assessment-anon-1');

    const sessionCall = fetchMock.mock.calls.find(
      ([input, init]) =>
        String(input).includes('/api/public/sessions') &&
        (init as RequestInit | undefined)?.method === 'POST',
    );
    const body = JSON.parse((sessionCall?.[1] as RequestInit).body as string) as Record<
      string,
      unknown
    >;
    // Faqat shaxs maydonlarisiz anonim shakl (`docs/07` §1.2 "Anonim oqim") — bitta dastur
    // bo'lgani uchun `programCode` ham yo'q.
    expect(body).toEqual({
      slug: 'demo-school',
      accessToken: 'tok123',
      consentAccepted: true,
      languageCode: 'uz',
    });
  });

  it("registrationMode 'None' dasturda rozilik belgilanmaguncha 'Boshlash' bosilsa ham so'rov yuborilmaydi", async () => {
    const fetchMock = mockNoneModeFetch();

    renderLanding();

    await screen.findByText(NONE_MODE_BODY.name);
    const startButton = screen.getByRole('button', { name: 'Boshlash' });
    expect(startButton).toBeDisabled();

    const sessionCallsBefore = fetchMock.mock.calls.filter(
      ([, init]) => (init as RequestInit | undefined)?.method === 'POST',
    );
    expect(sessionCallsBefore).toHaveLength(0);
  });

  it("registrationMode 'None' — server 429 RATE_LIMITED qaytarsa tushunarli xato ko'rsatadi, sessiya ochilmaydi", async () => {
    mockNoneModeFetch(() => problemResponse('RATE_LIMITED', 429));
    const user = userEvent.setup();

    renderLanding();

    await screen.findByText(NONE_MODE_BODY.name);
    await user.click(
      screen.getByLabelText("Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman"),
    );
    await user.click(screen.getByRole('button', { name: 'Boshlash' }));

    expect(
      await screen.findByText("Juda ko'p urinish bo'ldi. Birozdan keyin qayta urinib ko'ring."),
    ).toBeInTheDocument();
    expect(useSessionStore.getState().sessionToken).toBeNull();
  });

  it("bir nechta dastur orasida 'None' rejimlisi tanlansa rozilik ekranida ko'rsatiladi va programCode bilan yuboriladi", async () => {
    const mixedBody = {
      ...SCHOOL_INFO_BODY,
      programs: [
        SCHOOL_INFO_BODY.programs[0]!,
        {
          code: 'CAREER_SURVEY',
          nameUz: "Kasb so'rovnomasi",
          descriptionUz: "Qisqa so'rovnoma",
          testCount: 1,
          questionCount: 20,
          estimatedMinutes: 4,
          hasPersonalityBattery: false,
          registrationMode: 'None',
          tests: NONE_MODE_TESTS,
        },
      ],
    } satisfies PublicSchoolInfoWithRegistration;
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes('/api/public/schools/')) {
        return Promise.resolve(typedResponse<PublicSchoolInfoWithRegistration>(mixedBody));
      }
      if (url.includes('/api/public/sessions') && init?.method === 'POST') {
        return Promise.resolve(jsonResponse<'StartSessionResult'>(ANONYMOUS_START_SESSION_RESULT));
      }
      return Promise.reject(new Error(`unexpected fetch: ${url}`));
    });
    vi.stubGlobal('fetch', fetchMock);
    const user = userEvent.setup();

    renderLanding();

    await user.click(await screen.findByText("Kasb so'rovnomasi"));
    // Rozilik faqat TANLANGAN dastur `None` bo'lgach ko'rinadi.
    expect(await screen.findByText(mixedBody.consentText)).toBeInTheDocument();
    const startButton = screen.getByRole('button', { name: 'Boshlash' });
    expect(startButton).toBeDisabled();

    await user.click(
      screen.getByLabelText("Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman"),
    );
    await user.click(startButton);

    expect(await screen.findByText('TEST_STUB')).toBeInTheDocument();
    const sessionCall = fetchMock.mock.calls.find(
      ([input, init]) =>
        String(input).includes('/api/public/sessions') &&
        (init as RequestInit | undefined)?.method === 'POST',
    );
    const body = JSON.parse((sessionCall?.[1] as RequestInit).body as string) as Record<
      string,
      unknown
    >;
    expect(body.programCode).toBe('CAREER_SURVEY');
    expect(body.fullName).toBeUndefined();
  });

  it("bir nechta dastur orasida 'Full' rejimlisi tanlansa ESKI oqim ishlaydi — rozilik ekranida ko'rsatilmaydi, ro'yxatdan o'tishga o'tadi", async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(typedResponse<PublicSchoolInfoWithRegistration>(TWO_PROGRAMS_BODY)),
    );
    const user = userEvent.setup();

    renderLanding();

    await user.click(await screen.findByText('Shaxsiyat profili'));
    expect(screen.queryByText(TWO_PROGRAMS_BODY.consentText)).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Boshlash' }));

    expect(await screen.findByText('REGISTER_STUB')).toBeInTheDocument();
  });
});
