import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, listResponse, problemResponse, typedResponse } from '@/test/apiMock';
import type { PublishIssue } from '../model/publishIssues';
import type {
  CatalogQuestionItem,
  CatalogScaleItem,
  CatalogSection,
  CatalogTestDetail,
} from '../model/types';
import CatalogTestDetailPage from './CatalogTestDetailPage';

/**
 * Katalog DTO'lari endi `schema.d.ts` da BOR (`AssessmentCatalogController`, P37), shu sabab
 * barcha javoblar `jsonResponse<'…'>` / `listResponse<'…'>` bilan — mock tanasi sxemadan
 * tekshiriladi (`docs/10` §6.4).
 */

function testDetail(overrides: Partial<CatalogTestDetail> = {}): CatalogTestDetail {
  return {
    id: 'test-1',
    code: 'MBTI16',
    nameUz: 'Shaxsiyat testi',
    kind: 'System',
    isSystem: true,
    status: 'Published',
    isActive: true,
    scoringMode: 'Scored',
    questionCount: 2,
    scaleCount: 0,
    estimatedMinutes: 12,
    version: 1,
    usedInProgramCount: 1,
    descriptionUz: 'Shaxsiyat tipini aniqlaydi',
    pageSize: 10,
    shuffleQuestions: false,
    displayOrder: 4,
    ...overrides,
  };
}

function questionRow(overrides: Partial<CatalogQuestionItem> = {}): CatalogQuestionItem {
  return {
    id: 'q-1',
    code: 'MB-Q01',
    order: 1,
    textUz: 'Yangi odamlar bilan tanishish menga oson.',
    textRu: null,
    textEn: null,
    type: 'Likert5',
    scale: 'EI',
    direction: 1,
    weight: 1,
    isRequired: true,
    isActive: true,
    isSystem: true,
    // Shkala nomi/tavsifi BACKEND'dan keladi (`docs/07` §3.4) — frontendda kod → nom
    // jadvali yo'q, shu sabab mock ham aynan javob shaklini takrorlaydi.
    scaleNameUz: 'Ekstraversiya/Introversiya',
    scaleDescriptionUz: null,
    sectionId: null,
    visibility: null,
    placeholder: null,
    inputPattern: null,
    maxLength: null,
    minSelections: null,
    maxSelections: null,
    options: null,
    hasAnswers: false,
    ...overrides,
  };
}

function scaleRow(overrides: Partial<CatalogScaleItem> = {}): CatalogScaleItem {
  return {
    id: 'scale-1',
    testDefinitionId: 'test-1',
    code: 'STRESS',
    nameUz: 'Stressga munosabat',
    descriptionUz: null,
    displayOrder: 0,
    interpretationBands: [],
    questionCount: 4,
    ...overrides,
  };
}

interface MockOptions {
  detail?: CatalogTestDetail;
  questions?: CatalogQuestionItem[];
  scales?: CatalogScaleItem[];
  sections?: CatalogSection[];
  /** `POST .../publish` javobi — berilmasa nashr muvaffaqiyatli hisoblanadi. */
  publishResponse?: () => Response;
}

function mockFetch({
  detail = testDetail(),
  questions = [questionRow()],
  scales = [],
  sections = [],
  publishResponse,
}: MockOptions = {}) {
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('/publish')) {
      return Promise.resolve(
        publishResponse ? publishResponse() : jsonResponse<'CatalogTestDetailDto'>(detail),
      );
    }
    if (url.endsWith('/questions')) {
      return Promise.resolve(listResponse<'CatalogQuestionItemDto'>(questions));
    }
    if (url.endsWith('/sections')) {
      return Promise.resolve(typedResponse<CatalogSection[]>(sections));
    }
    if (url.endsWith('/scales'))
      return Promise.resolve(listResponse<'CatalogScaleItemDto'>(scales));
    if (url.includes('/api/admin/catalog/scales/')) {
      return Promise.resolve(jsonResponse<'CatalogScaleItemDto'>(scales[0] ?? scaleRow()));
    }
    if (url.includes('/api/admin/catalog/questions/')) {
      return Promise.resolve(jsonResponse<'CatalogQuestionItemDto'>(questions[0] ?? questionRow()));
    }
    if (url.includes('/api/admin/catalog/tests/test-1')) {
      return Promise.resolve(jsonResponse<'CatalogTestDetailDto'>(detail));
    }
    return Promise.resolve(problemResponse('NOT_FOUND', 404));
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={['/admin/catalog/tests/test-1']}>
          <Routes>
            <Route path="/admin/catalog/tests/:id" element={<CatalogTestDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('CatalogTestDetailPage — tizim metodikasi', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("tahrirlash ochiq, lekin o'chirish/savol qo'shish tugmalari YO'Q", async () => {
    mockFetch();
    renderPage();

    expect(await screen.findByText('Shaxsiyat testi')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Tahrirlash/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Nusxa olish/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: "O'chirish" })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Savol qo'shish/ })).not.toBeInTheDocument();
    // Shkala CRUD bo'limi tizim testida umuman ko'rsatilmaydi.
    expect(screen.queryByRole('button', { name: /Shkala qo'shish/ })).not.toBeInTheDocument();
  });

  it("savol oynasida shkala/yo'nalish/og'irlik disabled va sababi tushuntirilgan", async () => {
    mockFetch();
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /MB-Q01 savolini tahrirlash/ }));

    const scaleInput = await screen.findByLabelText('Shkala');
    expect(scaleInput).toBeDisabled();
    expect(screen.getByLabelText("Yo'nalish")).toBeDisabled();
    expect(screen.getByLabelText("Og'irlik")).toBeDisabled();

    const describedBy = scaleInput.getAttribute('aria-describedby');
    expect(describedBy).toBeTruthy();
    expect(document.getElementById(describedBy ?? '')?.textContent).toContain(
      "shkala, yo'nalish va og'irlik o'zgartirilmaydi",
    );

    // Matn maydonlari esa ochiq.
    expect(screen.getByLabelText("Matni (o'zbekcha)")).toBeEnabled();
  });

  it("nashr etilgan testda ballar o'zgarmasligi haqida ogohlantirish ko'rsatiladi", async () => {
    mockFetch();
    renderPage();

    expect(
      await screen.findByText(/Ballar o'zgarmaydi — hisoblash shkala va og'irlikka tayanadi/),
    ).toBeInTheDocument();
  });

  it('savol matni saqlanganda PUT /questions/{id} ga `scale`/`direction`/`weight`siz tana ketadi', async () => {
    const fetchMock = mockFetch();
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /MB-Q01 savolini tahrirlash/ }));

    const textarea = await screen.findByLabelText("Matni (o'zbekcha)");
    await user.clear(textarea);
    await user.type(textarea, 'Yangilangan savol matni');
    await user.click(screen.getByText('Saqlash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) =>
          String(url).includes('/api/admin/catalog/questions/q-1'),
        ),
      ).toBe(true);
    });

    const call = fetchMock.mock.calls.find(([url]) =>
      String(url).includes('/api/admin/catalog/questions/q-1'),
    );
    const init = call?.[1] as RequestInit | undefined;
    expect(init?.method).toBe('PUT');

    const body: unknown = JSON.parse(String(init?.body));
    expect(body).toEqual({
      textUz: 'Yangilangan savol matni',
      textRu: null,
      textEn: null,
      isActive: true,
      order: 1,
      isRequired: true,
    });
  });
});

describe('CatalogTestDetailPage — `Custom` test', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("savol qo'shish, o'chirish, shkala boshqaruvi va testni o'chirish ochiq", async () => {
    mockFetch({
      detail: testDetail({
        id: 'test-1',
        code: 'STRESS',
        nameUz: 'Stress anketasi',
        kind: 'Custom',
        isSystem: false,
        status: 'Draft',
      }),
      questions: [questionRow({ isSystem: false, scale: 'STRESS' })],
    });
    renderPage();

    expect(await screen.findByText('Stress anketasi')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Savol qo'shish/ })).toBeInTheDocument();
    expect(
      await screen.findByRole('button', { name: /MB-Q01 savolini o'chirish/ }),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Shkala qo'shish/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: "O'chirish" })).toBeInTheDocument();
    // `Draft` bo'lgani uchun "Nashr qilish" ko'rinadi.
    expect(screen.getByRole('button', { name: 'Nashr qilish' })).toBeInTheDocument();
  });

  it("nashr rad etilsa `issues[]` bandma-band ko'rsatiladi", async () => {
    // Backend `CatalogPublishValidator` barcha muammoni bir yo'la yig'adi va
    // `ProblemDetails.issues` kengaytmasida qaytaradi (`docs/07` 3.4-bo'lim).
    mockFetch({
      detail: testDetail({ kind: 'Custom', isSystem: false, status: 'Draft' }),
      questions: [questionRow({ isSystem: false, scale: 'STRESS' })],
      // `issues[]` — `ProblemDetails` kengaytmasi, shu sabab `problemResponse` ning
      // `extensions` argumentida; `title` ham shu yerda backendning haqiqiy matni bilan
      // qayta yoziladi (`problemResponse` sukut bo'yicha umumiy "Xato" beradi).
      publishResponse: () =>
        problemResponse('TEST_NOT_PUBLISHABLE', 400, undefined, {
          title: 'Anketa nashr qilishga tayyor emas.',
          issues: [
            {
              code: 'SCALE_TOO_FEW_QUESTIONS',
              scale: 'SUPPORT',
              questionCode: null,
              message: 'Kamida 4 savol kerak, hozir 2',
            },
            {
              code: 'SCALE_BAND_GAP',
              scale: 'STRESS',
              questionCode: null,
              message: "Talqin oraliqlarida bo'shliq bor.",
            },
          ] satisfies PublishIssue[],
        }),
    });
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(screen.getByRole('button', { name: 'Nashr qilish' }));

    // `<dialog>` jsdom'da `showModal` siz ochilmaydi (display:none) — shu sabab oyna ichidagi
    // elementlar `getByRole` uchun ko'rinmaydi va matn bo'yicha olinadi (mavjud testlar naqshi).
    const publishButtons = await screen.findAllByText('Nashr qilish');
    await user.click(publishButtons[publishButtons.length - 1]!);

    // Har bir muammo alohida qator: kontekst (shkala kodi) + o'zbekcha sabab.
    expect(await screen.findByText(/Kamida 4 savol kerak, hozir 2/)).toBeInTheDocument();
    expect(screen.getByText(/SUPPORT shkalasi/)).toBeInTheDocument();
    expect(screen.getByText(/Oraliqlar orasida bo'shliq bor/)).toBeInTheDocument();
    expect(screen.getByText(/2 ta muammo/)).toBeInTheDocument();
  });

  it("meta oynasi joriy tartib raqamini API'dan ko'rsatadi", async () => {
    mockFetch({
      detail: testDetail({ kind: 'Custom', isSystem: false, status: 'Draft', displayOrder: 7 }),
      questions: [questionRow({ isSystem: false })],
    });
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(screen.getByRole('button', { name: /Tahrirlash/ }));

    expect(await screen.findByLabelText(/Katalogdagi tartib raqami/)).toHaveValue(7);
  });

  it("shkala oynasida talqin oraliqlari tahrirlanadi va teng bo'linadi", async () => {
    const fetchMock = mockFetch({
      detail: testDetail({ kind: 'Custom', isSystem: false, status: 'Draft' }),
      questions: [questionRow({ isSystem: false, scale: 'STRESS' })],
      scales: [scaleRow()],
    });
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /STRESS shkalasini tahrirlash/ }));

    await user.click(await screen.findByText("Teng bo'lish"));

    expect(await screen.findByLabelText('1-oraliq: dan')).toHaveValue(0);
    expect(screen.getByLabelText('1-oraliq: gacha')).toHaveValue(33);
    expect(screen.getByLabelText('2-oraliq: dan')).toHaveValue(34);
    expect(screen.getByLabelText('2-oraliq: gacha')).toHaveValue(66);
    expect(screen.getByLabelText('3-oraliq: dan')).toHaveValue(67);
    expect(screen.getByLabelText('3-oraliq: gacha')).toHaveValue(100);

    await user.click(screen.getByText('Saqlash'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([url]) =>
          String(url).includes('/api/admin/catalog/scales/scale-1'),
        ),
      ).toBe(true);
    });

    const call = fetchMock.mock.calls.find(([url]) =>
      String(url).includes('/api/admin/catalog/scales/scale-1'),
    );
    const body: unknown = JSON.parse(String((call?.[1] as RequestInit | undefined)?.body));
    expect(body).toMatchObject({
      interpretationBands: [
        { from: 0, to: 33, label: 'Past' },
        { from: 34, to: 66, label: "O'rtacha" },
        { from: 67, to: 100, label: 'Yuqori' },
      ],
    });
  });

  it("kasrli chegara kiritilsa saqlash bloklanadi va sabab ko'rsatiladi", async () => {
    mockFetch({
      detail: testDetail({ kind: 'Custom', isSystem: false, status: 'Draft' }),
      questions: [questionRow({ isSystem: false, scale: 'STRESS' })],
      scales: [
        scaleRow({
          interpretationBands: [
            { from: 0, to: 33, label: 'Past' },
            { from: 34, to: 100, label: 'Yuqori' },
          ],
        }),
      ],
    });
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /STRESS shkalasini tahrirlash/ }));

    const firstTo = await screen.findByLabelText('1-oraliq: gacha');
    await user.clear(firstTo);
    await user.type(firstTo, '33.3');

    expect(
      await screen.findByText(/Oraliq chegaralari butun son bo'lishi shart/),
    ).toBeInTheDocument();
    expect(screen.getByText('Saqlash')).toBeDisabled();
  });

  it('`Custom` savolda shkala maydoni tahrirlanadi', async () => {
    mockFetch({
      detail: testDetail({ kind: 'Custom', isSystem: false, status: 'Draft' }),
      questions: [questionRow({ isSystem: false, scale: 'STRESS' })],
    });
    const user = userEvent.setup();
    renderPage();

    await screen.findByText('Shaxsiyat testi');
    await user.click(await screen.findByRole('button', { name: /MB-Q01 savolini tahrirlash/ }));

    expect(await screen.findByLabelText('Shkala')).toBeEnabled();
    expect(screen.getByLabelText("Og'irlik")).toBeEnabled();
  });
});

describe('CatalogTestDetailPage — savollar jadvali va tizim shkalalari', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shkala ustunida NOM asosiy, kod esa ikkinchi darajali bo'lib qoladi", async () => {
    mockFetch();
    renderPage();

    // Tekshiruv AYNAN savollar jadvali ichida — sahifada shkalalar ma'lumot bloki ham bor.
    const table = within(await screen.findByRole('table'));

    // Nom ko'rinadi...
    expect(table.getByText('Ekstraversiya/Introversiya')).toBeInTheDocument();
    // ...kod esa OLIB TASHLANMAYDI: import formati va savol tahrirlash kod bo'yicha ishlaydi.
    const code = table.getByText('EI');
    expect(code).toBeInTheDocument();
    expect(code.className).toContain('text-xs');
  });

  it("`scaleNameUz` null bo'lsa faqat kod ko'rsatiladi, qo'shnisi esa nomini yo'qotmaydi", async () => {
    // Ikkita qator ATAYLAB: bittasi noma'lum kod (`null` nom), ikkinchisi nomli. Shu bilan
    // test ham degradatsiyani (kod qoladi), ham asosiy yo'lni (nom ko'rinadi) bir vaqtda
    // tekshiradi — faqat `null` qatorli test ESKI kodda ham yashil bo'lardi.
    mockFetch({
      questions: [
        questionRow({ id: 'q-1', code: 'MB-Q01', order: 1, scale: 'XYZ', scaleNameUz: null }),
        questionRow({ id: 'q-2', code: 'MB-Q02', order: 2, scale: 'EI' }),
      ],
    });
    renderPage();

    const table = within(await screen.findByRole('table'));

    expect(table.getByText('XYZ')).toBeInTheDocument();
    expect(table.getByText('Ekstraversiya/Introversiya')).toBeInTheDocument();
    // Noma'lum kod uchun nom O'YLAB TOPILMAYDI — faqat bitta nomli qator bor.
    expect(table.getAllByText('Ekstraversiya/Introversiya')).toHaveLength(1);
  });

  it("savol turi ustuni o'zbekcha nom bilan chiqadi", async () => {
    mockFetch({
      questions: [
        questionRow({ id: 'q-1', code: 'MB-Q01', order: 1, type: 'Likert5' }),
        questionRow({ id: 'q-2', code: 'MB-Q02', order: 2, type: 'ForcedChoice' }),
      ],
    });
    renderPage();

    expect(await screen.findByRole('columnheader', { name: 'Savol turi' })).toBeInTheDocument();
    const table = within(screen.getByRole('table'));
    expect(table.getByText('Likert (5 ball)')).toBeInTheDocument();
    expect(table.getByText('Majburiy tanlov')).toBeInTheDocument();
  });

  it("tizim metodikasida shkalalar FAQAT KO'RISH bloki chiqadi (tugmalarsiz)", async () => {
    mockFetch({
      questions: [
        questionRow({ id: 'q-1', code: 'MB-Q01', order: 1, scale: 'EI' }),
        questionRow({ id: 'q-2', code: 'MB-Q02', order: 2, scale: 'EI' }),
        questionRow({
          id: 'q-3',
          code: 'MB-Q11',
          order: 3,
          scale: 'SN',
          scaleNameUz: 'Sezish/Intuitsiya',
          scaleDescriptionUz: 'Ma`lumotni qanday qabul qiladi',
        }),
      ],
    });
    renderPage();

    expect(await screen.findByText("Shkalalar (faqat ko'rish uchun)")).toBeInTheDocument();
    // Nom + kod + nechta savol.
    expect(await screen.findByText('(EI)')).toBeInTheDocument();
    expect(screen.getByText('(SN)')).toBeInTheDocument();
    expect(screen.getByText('2 ta savol')).toBeInTheDocument();
    // Tavsif bo'lsa ko'rsatiladi.
    expect(screen.getByText('Ma`lumotni qanday qabul qiladi')).toBeInTheDocument();
    // Tahrirlash yo'llari YO'Q — bu CRUD bo'limi emas.
    expect(screen.queryByRole('button', { name: /Shkala qo'shish/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /shkalasini tahrirlash/ })).not.toBeInTheDocument();
    // Talqin oraliqlari tizim metodikasida YO'Q (`SUM` uchun) — soxta blok chizilmaydi.
    expect(screen.queryByText(/Talqin oraliqlari belgilanmagan/)).not.toBeInTheDocument();
  });

  it("`Custom` testda ma'lumot bloki emas, tahrirlanadigan shkalalar bo'limi chiqadi", async () => {
    mockFetch({
      detail: testDetail({ kind: 'Custom', isSystem: false, status: 'Draft' }),
      questions: [
        questionRow({ isSystem: false, scale: 'STRESS', scaleNameUz: 'Stressga munosabat' }),
      ],
      scales: [scaleRow()],
    });
    renderPage();

    expect(await screen.findByRole('button', { name: /Shkala qo'shish/ })).toBeInTheDocument();
    expect(screen.queryByText("Shkalalar (faqat ko'rish uchun)")).not.toBeInTheDocument();
  });
});
