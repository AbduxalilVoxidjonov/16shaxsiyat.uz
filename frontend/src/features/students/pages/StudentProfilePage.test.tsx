import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import axe from 'axe-core';
import { ToastProvider } from '@/shared/ui/Toast';
import { problemResponse, typedResponse } from '@/test/apiMock';
import StudentProfilePage from './StudentProfilePage';
import type {
  ActivityResult,
  AiAnalysisDto,
  AssessmentSummaryDto,
  BigFiveResult,
  LatestAssessmentDto,
  Mbti16Result,
  RiasecResult,
  StudentDetailDto,
  StudentProfileResponse,
} from '../model/profileTypes';

/** `widgets/a11y.test.tsx`dagi bilan bir xil sabab — jsdom'da ma'nosiz qoidalar o'chirilgan. */
async function expectNoAxeViolations(container: Element): Promise<void> {
  const results = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
  expect(results.violations).toEqual([]);
}

const STUDENT = {
  id: 'student-1',
  fullName: 'Aliyev Sardor Bekzodovich',
  birthDate: '2010-04-17',
  age: 16,
  gender: 'Male',
  grade: 9,
  classLetter: 'B',
  phone: '+998901234567',
  parentPhone: '+998911112233',
  email: null,
  school: { id: 'school-1', name: "12-son maktab, Qo'qon" },
  consentGivenAt: '2026-08-01T10:00:00Z',
  createdAt: '2026-08-01T10:00:00Z',
} satisfies StudentDetailDto;

const ASSESSMENT_SUMMARY = {
  id: 'assessment-1',
  status: 'Analyzed',
  startedAt: '2026-08-30T09:00:00Z',
  completedAt: '2026-08-30T09:29:00Z',
  durationMinutes: 29,
  reliabilityScore: 82.5,
  reliabilityFlag: 'Reliable',
  isLatest: true,
} satisfies AssessmentSummaryDto;

const MBTI16_RESULT = {
  resultCode: 'INTJ',
  typeName: 'Loyihachi',
  axes: {
    EI: { pct: 28.3, letter: 'I', borderline: false },
    SN: { pct: 71.6, letter: 'N', borderline: false },
    TF: { pct: 33.3, letter: 'T', borderline: false },
    JP: { pct: 64.1, letter: 'J', borderline: false },
  },
  borderlineAxes: [],
} satisfies Mbti16Result;

const BIG5_RESULT = {
  factors: {
    O: { raw: 38, pct: 70, level: 'Yuqori' },
    C: { raw: 41, pct: 77.5, level: 'Yuqori' },
    E: { raw: 24, pct: 35, level: 'Past' },
    A: { raw: 35, pct: 62.5, level: 'Yuqori' },
    N: { raw: 22, pct: 30, level: 'Past' },
  },
  stabilityPct: 70,
  maturityIndex: 68.4,
  maturityLevel: 'Yaxshi',
} satisfies BigFiveResult;

const RIASEC_RESULT = {
  resultCode: 'IRA',
  types: { R: 62, I: 88, A: 71, S: 40, E: 35, C: 48 },
  differentiation: 53,
  consistency: 'High',
  careerFields: [{ name: 'Muhandislik', professions: ['Dasturchi'] }],
} satisfies RiasecResult;

const ACTIVITY_RESULT = {
  scales: { MOT: 74, SELF: 68, SOCA: 52, ENG: 60 },
  activityIndex: 65.2,
  activityLevel: 'Moderate',
  needsAttention: false,
} satisfies ActivityResult;

/**
 * `latestAssessment.aiAnalysis` — backend `AdminAiAnalysisDto`
 * (`Application/Admin/Students/AdminStudentDtos.cs`).
 *
 * Tip sxemadan EMAS, `../model/profileTypes` dan: `schema.d.ts` dagi `AdminAiAnalysisDto`
 * eskirgan — unda `isFallbackReport`/`isModerated`/`errorMessage`/`learningStyle`/
 * `motivationProfile`/`activityAssessment`/`reliabilityNote`/`disclaimer` YO'Q, `strengths`/
 * `growthAreas`/`attentionFlags` esa `string[]` (backendda tuzilmali obyektlar),
 * `teacherNotes`/`parentNotes` — `string` (backendda `string[]`).
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
  summary: "Bu — o'quvchining namunaviy portreti.",
  personalityPortrait: 'Tahliliy fikrlaydi.',
  strengths: [{ title: 'Tahliliy fikrlash', description: 'Tavsif', evidence: 'Asos' }],
  growthAreas: [{ title: "O'sish zonasi", description: 'Tavsif', actionStep: 'Qadam' }],
  learningStyle: "Mustaqil o'qish",
  motivationProfile: 'Aniq maqsad',
  activityAssessment: "O'rtacha faol",
  // `exampleProfessions` — backend `AdminCareerSuggestionDto` da MAJBURIY; mock uni
  // tushirib qoldirgan edi (sxemadan tiplashda topildi).
  careerSuggestions: [
    { field: 'IT', why: 'Sabab', exampleProfessions: ['Dasturchi'], nextSteps: ['Kurs'] },
  ],
  studentRecommendations: ['Tavsiya'],
  teacherNotes: ['Eslatma'],
  parentNotes: ['Eslatma'],
  attentionFlags: [],
  reliabilityNote: 'Javoblar tez berilgan, natijani ehtiyot bilan talqin qiling.',
  disclaimer: 'Bu tahlil tashxis emas.',
} satisfies AiAnalysisDto;

/**
 * `GET /api/admin/students/{id}` javobi. `results` kalitlari — `TestDefinition.Code` bilan
 * HARFMA-HARF bir xil (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`); sxemada ham aynan shunday.
 * Qaytish tipi `StudentProfileResponse` — u SXEMADAN olingan (`AdminStudentProfileDto` +
 * enum toraytirish), shu sabab fixture baribir shartnomaga qarab tekshiriladi.
 *
 * `jsonResponse<'AdminStudentProfileDto'>` EMAS, chunki sxemada `latestAssessment`/
 * `aiAnalysis`/`results.*` faqat IXTIYORIY (`?`), `| null` emas: Swashbuckle OpenAPI 3.0 da
 * `$ref` yonida `nullable: true` chiqara olmaydi. Backend esa `DefaultIgnoreCondition`
 * sozlamagani uchun (`Program.cs`) bu maydonlarni ANIQ `null` bilan yuboradi — mock aynan
 * shu haqiqiy javobni taqlid qiladi.
 */
function buildProfileResponse(
  overrides: {
    assessments?: AssessmentSummaryDto[];
    latestAssessment?: LatestAssessmentDto | null;
  } = {},
): StudentProfileResponse {
  return {
    student: STUDENT,
    assessments: overrides.assessments ?? [ASSESSMENT_SUMMARY],
    latestAssessment:
      overrides.latestAssessment === undefined
        ? {
            id: 'assessment-1',
            results: {
              MBTI16: MBTI16_RESULT,
              BIG5: BIG5_RESULT,
              RIASEC: RIASEC_RESULT,
              ACTIVITY: ACTIVITY_RESULT,
            },
            aiAnalysis: AI_ANALYSIS,
            aiHistory: [],
          }
        : overrides.latestAssessment,
  };
}

/** Xato javobi — `ProblemDetails` (`docs/06` 6-bo'lim). */
interface ProblemEnvelope {
  status: number;
  code: string;
}

type ProfileFetchResult = StudentProfileResponse | ProblemEnvelope;

function isProblemEnvelope(value: ProfileFetchResult | undefined): value is ProblemEnvelope {
  return value !== undefined && 'code' in value;
}

function renderPage(
  profileResponses: ProfileFetchResult[],
  initialEntry = '/admin/students/student-1',
) {
  const responses = [...profileResponses];
  let lastResponse: ProfileFetchResult | undefined = responses[0];
  const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes('/api/admin/students/student-1')) {
      const next = responses.length > 0 ? responses.shift() : lastResponse;
      lastResponse = next;
      if (isProblemEnvelope(next)) {
        return Promise.resolve(problemResponse(next.code, next.status));
      }
      return Promise.resolve(typedResponse<StudentProfileResponse>(next!));
    }
    return Promise.reject(new Error(`unexpected fetch: ${url}`));
  });
  vi.stubGlobal('fetch', fetchMock);

  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[initialEntry]}>
          <Routes>
            <Route path="/admin/students/:id" element={<StudentProfilePage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
  return Object.assign(fetchMock, { container: view.container });
}

describe('StudentProfilePage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("to'liq ma'lumot bilan sarlavha, kartalar, diagrammalar va AI hisobotni ko'rsatadi", async () => {
    renderPage([buildProfileResponse()]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getByText(/12-son maktab/)).toBeInTheDocument();
    expect(screen.getByText('INTJ')).toBeInTheDocument();
    expect(screen.getByText('68.4')).toBeInTheDocument();
    expect(screen.getByText('IRA')).toBeInTheDocument();
    expect(screen.getByText("Bu — o'quvchining namunaviy portreti.")).toBeInTheDocument();

    // 'N' o'rniga "Emotsional barqarorlik" ko'rsatiladi (CLAUDE.md MAXSUS DIQQAT).
    expect(screen.getAllByText('Emotsional barqarorlik').length).toBeGreaterThan(0);
    expect(screen.queryByText(/neyrotizm/i)).not.toBeInTheDocument();

    // Har diagramma uchun yashirin jadval alternativi mavjud.
    expect(screen.getAllByRole('table', { hidden: true }).length).toBeGreaterThan(0);
  });

  it('axe a11y tekshiruvi buzilishsiz o\'tadi', async () => {
    const fetchMock = renderPage([buildProfileResponse()]);
    await screen.findByText('Aliyev Sardor Bekzodovich');
    await expectNoAxeViolations(fetchMock.container);
  });

  it("bo'sh/null AI ma'lumotida sahifa yiqilmaydi", async () => {
    renderPage([
      buildProfileResponse({
        latestAssessment: {
          id: 'assessment-1',
          results: {},
          aiAnalysis: null,
          aiHistory: [],
        },
      }),
    ]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getAllByText('—').length).toBeGreaterThan(0);
    expect(screen.getByText('Bu sessiya uchun AI tahlil hali mavjud emas.')).toBeInTheDocument();
  });

  it("shablon (fallback) hisobotda aniq belgi ko'rsatiladi — haqiqiy AI tahlili deb o'qilmaydi", async () => {
    renderPage([
      buildProfileResponse({
        latestAssessment: {
          id: 'assessment-1',
          results: {},
          aiAnalysis: { ...AI_ANALYSIS, isFallbackReport: true, model: 'template' },
          aiHistory: [],
        },
      }),
    ]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getByText('Avtomatik shablon hisobot')).toBeInTheDocument();
    expect(screen.getByText(/Bu matnni AI yozmagan/)).toBeInTheDocument();
    // Ogohlantirish e'tiborni tortadigan `role="alert"` bo'lishi kerak, jimgina yorliq emas.
    expect(
      screen.getAllByRole('alert').some((el) => el.textContent?.includes('Avtomatik shablon hisobot')),
    ).toBe(true);
  });

  it("moderatsiya qilingan hisobotda ochiq ogohlantirish chiqadi (post-filtrdan toza o'tmagan matn)", async () => {
    renderPage([
      buildProfileResponse({
        latestAssessment: {
          id: 'assessment-1',
          results: {},
          aiAnalysis: {
            ...AI_ANALYSIS,
            isModerated: true,
            attentionFlags: [
              {
                code: 'MODERATION_REQUIRED',
                message: "Taqiqlangan atama ikkinchi urinishda ham topildi.",
                severity: 'high',
              },
            ],
          },
          aiHistory: [],
        },
      }),
    ]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getByText('Moderatsiya qilingan hisobot')).toBeInTheDocument();
    expect(
      screen.getAllByRole('alert').some((el) => el.textContent?.includes('Moderatsiya qilingan hisobot')),
    ).toBe(true);
    // Bayroqning o'zi ham hisobot ichida ko'rinadi — jimgina yo'qolib ketmaydi.
    expect(screen.getByText('Taqiqlangan atama ikkinchi urinishda ham topildi.')).toBeInTheDocument();
  });

  it("disclaimer va reliabilityNote hisobotda doim ko'rinadi", async () => {
    renderPage([buildProfileResponse()]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.getByText('Bu tahlil tashxis emas.')).toBeInTheDocument();
    expect(
      screen.getByText(/Javoblar tez berilgan, natijani ehtiyot bilan talqin qiling\./),
    ).toBeInTheDocument();
  });

  it("moderatsiya qilinmagan hisobotda ogohlantirish KO'RSATILMAYDI", async () => {
    renderPage([buildProfileResponse()]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.queryByText('Moderatsiya qilingan hisobot')).not.toBeInTheDocument();
  });

  it("haqiqiy AI tahlilida shablon belgisi KO'RSATILMAYDI", async () => {
    renderPage([buildProfileResponse()]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(screen.queryByText('Avtomatik shablon hisobot')).not.toBeInTheDocument();
  });

  it('Unreliable holatida sariq banner ko\'rinadi', async () => {
    renderPage([
      buildProfileResponse({
        assessments: [{ ...ASSESSMENT_SUMMARY, reliabilityFlag: 'Unreliable', reliabilityScore: 25 }],
      }),
    ]);

    expect(await screen.findByText('Aliyev Sardor Bekzodovich')).toBeInTheDocument();
    expect(
      screen.getByText(/Javoblar juda tez berilgan, natija ishonchsiz bo'lishi mumkin/),
    ).toBeInTheDocument();
  });

  it("Analyzing holatida refetchInterval yoqiladi va Analyzed'ga o'tgach o'chadi", async () => {
    const analyzingResponse = buildProfileResponse({
      assessments: [{ ...ASSESSMENT_SUMMARY, status: 'Analyzing', reliabilityFlag: null, reliabilityScore: null }],
      latestAssessment: { id: 'assessment-1', results: {}, aiAnalysis: null, aiHistory: [] },
    });
    const analyzedResponse = buildProfileResponse();

    const fetchMock = renderPage([analyzingResponse, analyzedResponse]);

    expect(await screen.findByText('Tahlil tayyorlanmoqda')).toBeInTheDocument();
    expect(
      screen.getByText('Odatda 1 daqiqa vaqt ketadi. Sahifa avtomatik yangilanadi.'),
    ).toBeInTheDocument();
    const callsAfterFirstLoad = fetchMock.mock.calls.length;

    await vi.advanceTimersByTimeAsync(5100);

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsAfterFirstLoad);
    });
    expect(await screen.findByText('INTJ')).toBeInTheDocument();

    const callsAfterAnalyzed = fetchMock.mock.calls.length;
    await vi.advanceTimersByTimeAsync(15000);
    // `Analyzed`ga o'tgach `refetchInterval` o'chadi — qo'shimcha so'rov ketmaydi.
    expect(fetchMock.mock.calls.length).toBe(callsAfterAnalyzed);
  });

  it('404 kelsa "topilmadi" holati ko\'rsatiladi', async () => {
    renderPage([{ status: 404, code: 'NOT_FOUND' }]);

    expect(await screen.findByText('O\'quvchi topilmadi')).toBeInTheDocument();
  });

  it('server xatosida qayta urinish tugmasi bilan xato holati ko\'rsatiladi', async () => {
    const fetchMock = renderPage([{ status: 500, code: 'INTERNAL_ERROR' }]);

    const retryButton = await screen.findByRole('button', { name: 'Qayta urinish' });
    const callsBeforeRetry = fetchMock.mock.calls.length;
    retryButton.click();

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsBeforeRetry);
    });
  });
});
