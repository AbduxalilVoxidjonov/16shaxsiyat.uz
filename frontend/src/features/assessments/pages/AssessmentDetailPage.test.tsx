import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import axe from 'axe-core';
import { ToastProvider } from '@/shared/ui/Toast';
import { jsonResponse, problemResponse, typedResponse } from '@/test/apiMock';
import AssessmentDetailPage from './AssessmentDetailPage';
import type {
  AssessmentTestItemDto,
  AssessmentAiAnalysisDto,
  AssessmentDetailDto,
  AssessmentListItemDto,
} from '../model/types';
import type { AssessmentAnswersDto } from '@/shared/api/assessmentAnswersTypes';


/**
 * `AssessmentTestItemDto` — backend 7 ta MAJBURIY maydon yuboradi
 * (`AdminAssessmentTestItemDto`). Test faqat `testCode`/`nameUz`/`scoringMode` ni
 * tekshiradi, qolganlari shartnomani to'liq saqlash uchun realistik qiymat bilan
 * to'ldiriladi — mock backenddan uzilib qolmasin (`docs/12` §7.1).
 * `questionCount`/`answeredCount` ataylab NOLDAN farqli: UI ularni hali ko'rsatmaydi,
 * lekin "`0` ko'rinmasin" assert'lari bilan tasodifan chalkashmasin.
 */
function testItem(overrides: Partial<AssessmentTestItemDto> = {}): AssessmentTestItemDto {
  return {
    testDefinitionId: '11111111-1111-1111-1111-111111111111',
    testCode: 'MBTI16',
    nameUz: 'Shaxsiyat tipi',
    scoringMode: 'Scored',
    status: 'Completed',
    questionCount: 20,
    answeredCount: 20,
    ...overrides,
  };
}

/** `widgets/a11y.test.tsx`dagi bilan bir xil sabab — jsdom'da ma'nosiz qoidalar o'chirilgan. */
async function expectNoAxeViolations(container: Element): Promise<void> {
  const results = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
  expect(results.violations).toEqual([]);
}

const ASSESSMENT_ID = 'assessment-1';

/** Sessiyalar ro'yxatidan (`AdminAssessmentListItemDto`) uzatiladigan qator. */
const LIST_ROW = {
  id: ASSESSMENT_ID,
  studentId: 'student-1',
  studentName: 'Aliyev Sardor Bekzodovich',
  schoolId: 'school-1',
  schoolName: "12-son maktab, Qo'qon",
  status: 'Analyzed',
  startedAt: '2026-08-30T09:00:00Z',
  completedAt: '2026-08-30T09:29:00Z',
  durationMinutes: 29,
  reliabilityScore: 82.5,
  reliabilityFlag: 'Reliable',
  programId: 'program-1',
  programName: null,
} satisfies AssessmentListItemDto;

/**
 * Sessiya AI tahlili — backend `AdminAiAnalysisDto`. Sxema tipi ISHLATILMAYDI: `schema.d.ts`
 * dagi `AdminAiAnalysisDto` eskirgan (`isFallbackReport`/`isModerated`/`learningStyle`/
 * `motivationProfile`/`activityAssessment`/`reliabilityNote`/`disclaimer` yo'q; `strengths`/
 * `growthAreas`/`attentionFlags` — `string[]`, backendda tuzilmali obyektlar).
 */
const AI_ANALYSIS = {
  id: 'ai-1',
  status: 'Succeeded',
  provider: 'Gemini',
  model: 'gemini-2.5-flash',
  promptVersion: 'v1.0',
  createdAt: '2026-08-31T10:00:00Z',
  isFallbackReport: false,
  isModerated: false,
  errorMessage: null,
  summary: 'Bu — sessiyaning namunaviy xulosasi.',
  personalityPortrait: 'Tahliliy fikrlaydi.',
  strengths: [{ title: 'Tahliliy fikrlash', description: null, evidence: null }],
  growthAreas: [],
  learningStyle: null,
  motivationProfile: null,
  activityAssessment: null,
  careerSuggestions: [],
  studentRecommendations: [],
  teacherNotes: [],
  parentNotes: [],
  attentionFlags: [],
  reliabilityNote: null,
  disclaimer: 'Bu tahlil tashxis emas.',
} satisfies AssessmentAiAnalysisDto;

/**
 * `GET /api/admin/assessments/{id}` javobi. Tip `AssessmentDetailDto` — u SXEMADAN olingan
 * (`AdminAssessmentDetailDto` + enum toraytirish), shu sabab fixture shartnomaga qarab
 * tekshiriladi.
 *
 * `jsonResponse<'AdminAssessmentDetailDto'>` EMAS: sxema `$ref` maydonlarida (`aiAnalysis`,
 * `student`, `program`…) `null` ni ifodalay olmaydi, bu testlar esa aynan backend yuboradigan
 * `null` javoblarni taqlid qiladi (`../model/types.ts` dagi izohga qarang).
 */
const FULL_DETAIL = {
  id: ASSESSMENT_ID,
  results: {
    MBTI16: { resultCode: 'INTJ', typeName: 'Loyihachi', axes: {}, borderlineAxes: [] },
    BIG5: { factors: {}, stabilityPct: 70, maturityIndex: 68.4, maturityLevel: 'Yaxshi' },
  },
  aiAnalysis: AI_ANALYSIS,
  aiHistory: [],
} satisfies AssessmentDetailDto;

const EMPTY_DETAIL = {
  id: ASSESSMENT_ID,
  results: {},
  aiAnalysis: null,
  aiHistory: [],
} satisfies AssessmentDetailDto;

/** Xato javobi — `ProblemDetails` (`docs/06` 6-bo'lim). */
interface ProblemEnvelope {
  status: number;
  code: string;
}

type DetailFetchResult = AssessmentDetailDto | ProblemEnvelope;

function isProblemEnvelope(value: DetailFetchResult | undefined): value is ProblemEnvelope {
  return value !== undefined && 'code' in value;
}

interface RenderOptions {
  /** `undefined` — navigatsiya holati umuman berilmaydi (to'g'ridan-to'g'ri havola bilan kirish). */
  state?: unknown;
  rerunResponse?: Response;
}

function renderPage(detailResponses: DetailFetchResult[], options: RenderOptions = {}) {
  const responses = [...detailResponses];
  let last: DetailFetchResult | undefined = responses[0];
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.includes(`/api/admin/assessments/${ASSESSMENT_ID}/rerun-analysis`)) {
      expect(init?.method).toBe('POST');
      return Promise.resolve(
        options.rerunResponse ??
          jsonResponse<'RerunAnalysisResultDto'>(
            { assessmentId: ASSESSMENT_ID, status: 'Analyzing' },
            202,
          ),
      );
    }
    if (url.includes(`/api/admin/assessments/${ASSESSMENT_ID}/answers`)) {
      const answersBody: AssessmentAnswersDto = {
        answers: [
          {
            questionId: 'q-1',
            questionCode: 'MBTI16-Q01',
            testCode: 'MBTI16',
            questionText: 'Men odamlar bilan bo\'lishni yaxshi ko\'raman',
            rawValue: 4,
            selectedOptionText: null,
            selectedOptionTexts: null,
            selectedValues: null,
            textValue: null,
            durationMs: 2500,
            revisionCount: 0,
            answeredAt: '2026-08-30T09:10:00Z',
            questionType: 'Likert5',
            scoringMode: 'Scored',
            scale: 'EI',
            scaleNameUz: 'Ekstraversiya',
            scaleDirection: 1,
            weight: 1,
            effectiveValue: 4,
            isFastAnswer: false,
            straightLiningBlockIndex: null,
          },
        ],
        session: {
          answeredCount: 1,
          fastAnswerCount: 0,
          straightLiningBlockCount: 0,
          allSameAnswer: false,
          shortSession: false,
          totalDurationSeconds: 1800,
          reliabilityScore: 82.5,
          reliabilityFlag: 'Reliable',
        },
        scales: [],
        thresholds: {
          fastAnswerDurationMs: 900,
          straightLiningMinRunLength: 12,
          shortSessionMinutes: 6,
        },
      };
      return Promise.resolve(typedResponse<AssessmentAnswersDto>(answersBody));
    }
    if (url.includes(`/api/admin/assessments/${ASSESSMENT_ID}`)) {
      const next = responses.length > 0 ? responses.shift() : last;
      last = next;
      return Promise.resolve(
        isProblemEnvelope(next)
          ? problemResponse(next.code, next.status)
          : typedResponse<AssessmentDetailDto>(next!),
      );
    }
    return Promise.reject(new Error(`kutilmagan so'rov: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter
          initialEntries={[
            { pathname: `/admin/assessments/${ASSESSMENT_ID}`, state: options.state ?? null },
          ]}
        >
          <Routes>
            <Route path="/admin/assessments/:id" element={<AssessmentDetailPage />} />
            <Route path="/admin/students/:id" element={<div>O'quvchi profili sahifasi</div>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
  return Object.assign(fetchMock, { container: view.container });
}

describe('AssessmentDetailPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sessiya holati, vaqtlari, maktabi, o'quvchisi va natijalarini ko'rsatadi", async () => {
    renderPage([FULL_DETAIL], { state: { assessment: LIST_ROW } });

    expect(await screen.findByText('Tahlil qilingan')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Aliyev Sardor Bekzodovich' })).toHaveAttribute(
      'href',
      '/admin/students/student-1',
    );
    expect(screen.getByText("12-son maktab, Qo'qon")).toBeInTheDocument();
    expect(screen.getByText('30.08.2026 09:00')).toBeInTheDocument();
    expect(screen.getByText('30.08.2026 09:29')).toBeInTheDocument();
    expect(screen.getByText('29 daqiqa')).toBeInTheDocument();

    // Ishonchlilik indeksi va uning izohi.
    expect(screen.getByText('82.5')).toBeInTheDocument();
    expect(screen.getByText('Ishonchli')).toBeInTheDocument();
    expect(screen.getByText(/Past ball natija yomon degani EMAS/)).toBeInTheDocument();

    // Testlar jadvali.
    // 2026-09-03 dan tip NOMI asosiy, kod ikkinchi darajali (egasining talabi:
    // "qisqartirib yozilgan 16 ta shaxsiyatni to'liq nomi bilan chiqar"). Ilgari
    // ikkalasi bitta matnda birlashtirilgan edi: `"INTJ · Loyihachi"`.
    expect(screen.getByText('Loyihachi')).toBeInTheDocument();
    expect(screen.getByText('INTJ')).toBeInTheDocument();
    expect(screen.getByText('Yetuklik indeksi: 68.4')).toBeInTheDocument();

    // AI tahlili.
    expect(screen.getByText('Bu — sessiyaning namunaviy xulosasi.')).toBeInTheDocument();
    expect(screen.getByText('Bu tahlil tashxis emas.')).toBeInTheDocument();
  });

  /**
   * P52-A (egasining talabi, 2026-09-12): "testning ichiga kirib qaysi savolga qaysi javob
   * berganini ko'rish" — bu ekranda ham (`AssessmentHistoryTable`dan kelib) xuddi profildagi
   * kabi `AnswersSection` widgeti ochilishi kerak, nusxa emas — bitta widget.
   */
  it("savolma-savol javoblar bo'limi shu sahifada ham ochiladi (widgets/AnswersSection qayta ishlatiladi)", async () => {
    const user = userEvent.setup();
    renderPage([FULL_DETAIL], { state: { assessment: LIST_ROW } });

    await screen.findByText('Tahlil qilingan');
    await user.click(screen.getByRole('button', { name: /Savolma-savol javoblar/i }));
    await user.click(await screen.findByRole('button', { name: /MBTI16/ }));

    expect(await screen.findByText("Men odamlar bilan bo'lishni yaxshi ko'raman")).toBeInTheDocument();
  });

  it("axe a11y tekshiruvi buzilishsiz o'tadi", async () => {
    const fetchMock = renderPage([FULL_DETAIL], { state: { assessment: LIST_ROW } });
    await screen.findByText('Tahlil qilingan');
    await expectNoAxeViolations(fetchMock.container);
  });

  it("ma'lumot yo'q bo'lganda `0` emas, aniq holat ko'rsatiladi", async () => {
    renderPage([EMPTY_DETAIL]);

    expect(await screen.findByText(/faqat sessiyalar ro'yxatidan ochilganda/)).toBeInTheDocument();
    expect(screen.getAllByText("Ma'lumot yo'q").length).toBeGreaterThan(0);
    expect(screen.getByText('Ishonchlilik hali hisoblanmagan')).toBeInTheDocument();
    expect(screen.getByText('Bu sessiya uchun AI tahlil hali mavjud emas.')).toBeInTheDocument();
    // Yechilmagan testlar `0` ball bilan KO'RSATILMAYDI.
    expect(screen.queryByText('0')).not.toBeInTheDocument();
    expect(screen.getAllByText("Natija yo'q").length).toBe(4);
  });

  it('`Survey` rejimidagi anketa "ballanmaydi" deb ko\'rsatiladi', async () => {
    renderPage([
      {
        ...EMPTY_DETAIL,
        tests: [
          testItem({ testCode: 'MBTI16', scoringMode: 'Scored' }),
          testItem({ testCode: 'STRESS', nameUz: 'Stressga chidamlilik', scoringMode: 'Survey' }),
        ],
      },
    ]);

    expect(await screen.findByText('Stressga chidamlilik')).toBeInTheDocument();
    expect(screen.getByText("Ballanmaydi (so'rovnoma)")).toBeInTheDocument();
    expect(screen.getByText('Ballanmaydi')).toBeInTheDocument();
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });

  it('avtomatik shablon hisobot aniq belgi bilan ajratiladi', async () => {
    renderPage([
      { ...FULL_DETAIL, aiAnalysis: { ...AI_ANALYSIS, isFallbackReport: true, model: 'template' } },
    ]);

    expect(await screen.findByText('Avtomatik shablon hisobot')).toBeInTheDocument();
    expect(
      screen.getAllByRole('alert').some((el) => el.textContent?.includes('Bu matnni AI yozmagan')),
    ).toBe(true);
  });

  it("haqiqiy AI tahlilida shablon belgisi ko'rsatilmaydi", async () => {
    renderPage([FULL_DETAIL]);

    expect(await screen.findByText('Bu — sessiyaning namunaviy xulosasi.')).toBeInTheDocument();
    expect(screen.queryByText('Avtomatik shablon hisobot')).not.toBeInTheDocument();
  });

  it('qayta tahlil faqat tasdiq oynasidan keyin yuboriladi', async () => {
    const user = userEvent.setup();
    const fetchMock = renderPage([FULL_DETAIL], { state: { assessment: LIST_ROW } });

    await user.click(await screen.findByRole('button', { name: /Tahlilni qayta ishga tushirish/ }));

    // jsdom `showModal()`ni amalga oshirmaydi, shu sabab oynaning OCHIQligini tekshirib
    // bo'lmaydi (`Dialog.tsx` izohi) — muhimi shu: tasdiqlashdan OLDIN so'rov ketmaydi.
    expect(screen.getByText(/Ballar va javoblar o'zgarmaydi/)).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.filter((call) => String(call[0]).includes('rerun-analysis')),
    ).toHaveLength(0);

    await user.click(screen.getByText('Ha, qayta ishga tushirilsin'));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.filter((call) => String(call[0]).includes('rerun-analysis')),
      ).toHaveLength(1);
    });
    // Provayder tanlanmagan — tana bo'sh, backend o'z zanjirini ishlatadi.
    const rerunCall = fetchMock.mock.calls.find((call) =>
      String(call[0]).includes('rerun-analysis'),
    );
    expect(String((rerunCall?.[1] as RequestInit | undefined)?.body)).toBe('{}');
    expect(await screen.findByText(/Tahlil navbatga qo'yildi\./)).toBeInTheDocument();
  });

  it("ruxsat etilmagan holatda (409) sabab ko'rsatiladi", async () => {
    const user = userEvent.setup();
    renderPage([FULL_DETAIL], {
      state: { assessment: LIST_ROW },
      rerunResponse: problemResponse('ASSESSMENT_INVALID_TRANSITION', 409),
    });

    await user.click(await screen.findByRole('button', { name: /Tahlilni qayta ishga tushirish/ }));
    await user.click(screen.getByText('Ha, qayta ishga tushirilsin'));

    expect(
      await screen.findByText(/Bu holatdagi sessiyani qayta tahlil qilib bo'lmaydi/),
    ).toBeInTheDocument();
  });

  it("yakunlanmagan sessiyada tahlil tugmasi KO'RSATILMAYDI, o'rniga sabab aytiladi", async () => {
    renderPage([EMPTY_DETAIL], {
      state: { assessment: { ...LIST_ROW, status: 'InProgress', completedAt: null } },
    });

    // Tugmani ko'rsatib turib `409` ga urib yuborish yomon UX — tugma umuman chiqmaydi.
    expect(await screen.findByText(/faqat yakunlangan sessiyada ishlaydi/)).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /Tahlilni qayta ishga tushirish/ }),
    ).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /AI tahlil qilish/ })).not.toBeInTheDocument();
    expect(screen.getByText('Yakunlanmagan')).toBeInTheDocument();
  });

  it('404 kelsa "topilmadi" holati ko\'rsatiladi', async () => {
    renderPage([{ status: 404, code: 'NOT_FOUND' }]);

    expect(await screen.findByText('Sessiya topilmadi')).toBeInTheDocument();
  });

  it("server xatosida qayta urinish tugmasi ko'rsatiladi", async () => {
    const fetchMock = renderPage([{ status: 500, code: 'INTERNAL_ERROR' }]);

    const retryButton = await screen.findByRole('button', { name: 'Qayta urinish' });
    const callsBefore = fetchMock.mock.calls.length;
    retryButton.click();

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsBefore);
    });
  });
});
