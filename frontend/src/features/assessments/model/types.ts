import type { components } from '@/shared/api/schema';
import type { AiAttentionFlag } from '@/widgets/AiReportView';

/**
 * `features/assessments` DTO va enum tiplari — `docs/07-api-shartnoma.md` 3.3-bo'lim.
 *
 * `docs/10` §6 qoidasi bo'yicha BARCHA backend DTO'si `shared/api/schema.d.ts` dan
 * **re-export** qilinadi (`AdminAssessmentDetailDto`, `AdminAssessmentListItemDto`,
 * `AdminAssessmentTestItemDto`, `AdminAiAnalysisDto`, ref bloklari…). Enum maydonlari
 * `Omit<…> & { … }` bilan toraytiriladi (`docs/10` §6.2, 2-naqsh) — maydon NOMI baribir
 * sxemadan tekshiriladi. Qo'lda yozilgan yagona shakl — `AssessmentTestResultsRaw`
 * (transport EMAS, mijoz tomonidagi moslashuv shakli; pastdagi izohga qarang).
 *
 * Enum'lar `features/students/model/enums.ts` va `widgets/ReliabilityBadge.tsx` dagi bilan
 * bir xil — `docs/10` 2-bo'lim qoidasi bo'yicha feature'lar bir-biridan import qilmaydi,
 * shu sabab ataylab takrorlangan (string literal union — struktura jihatdan mos).
 */

/** `docs/05`: "AssessmentStatus | 0 Draft, 1 InProgress, 2 Completed, 3 Analyzing, 4 Analyzed, 5 AnalysisFailed, 6 Abandoned". */
export const ASSESSMENT_STATUS_VALUES = [
  'Draft',
  'InProgress',
  'Completed',
  'Analyzing',
  'Analyzed',
  'AnalysisFailed',
  'Abandoned',
] as const;
export type AssessmentStatus = (typeof ASSESSMENT_STATUS_VALUES)[number];

/** `docs/05`: "ReliabilityFlag | 1 Reliable, 2 Questionable, 3 Unreliable". */
export const RELIABILITY_FLAG_VALUES = ['Reliable', 'Questionable', 'Unreliable'] as const;
export type ReliabilityFlag = (typeof RELIABILITY_FLAG_VALUES)[number];

/** `docs/05`: "AiAnalysisStatus | 0 Pending, 1 Running, 2 Succeeded, 3 Failed". */
export type AiAnalysisStatus = 'Pending' | 'Running' | 'Succeeded' | 'Failed';

/** `Domain/Ai/AiProvider.cs` — sxemada haqiqiy `enum` sifatida bor. */
export type AiProvider = components['schemas']['AiProvider'];

/*
 * Ruxsat etilgan holatlar ro'yxati (`Completed`/`Analyzed`/`AnalysisFailed`) endi bu yerda
 * EMAS: u o'quvchi profili bilan umumiy — `shared/lib/aiAnalysisState.ts`
 * (`AI_RUNNABLE_STATUSES`). Ikki nusxa ikki ekranning bir-biridan uzilib qolishiga olib
 * kelardi.
 */

/**
 * Natija bloklari — hammasi `schema.d.ts` dan re-export (backend
 * `Application/Admin/Students/AdminStudentDtos.cs`). Bu yerda `axes`/`factors`/`types`/
 * `scales` ATAYLAB ochiq lug'at (`Record<string, …>`) bo'lib qoladi: sessiya detali ularni
 * faqat jadvalga aylantiradi, `features/students` dagi diagrammalardan farqli o'laroq aniq
 * kalit bo'yicha o'qimaydi.
 */
export type AssessmentAxisResult = components['schemas']['AdminAxisDto'];

export type Mbti16Result = components['schemas']['AdminMbti16Dto'];

export type BigFiveFactorResult = components['schemas']['AdminFactorDto'];

/** `maturityIndex` — `TestResult.CompositeIndex`: BIG5 + ACTIVITY birga bo'lmasa `null` (nol EMAS). */
export type BigFiveResult = components['schemas']['AdminBig5Dto'];

export type RiasecCareerField = components['schemas']['AdminCareerFieldDto'];

export type RiasecResult = components['schemas']['AdminRiasecDto'];

export type ActivityResult = components['schemas']['AdminActivityDto'];

/**
 * Natija to'plami kalitlari — `docs/07` 3.2-bo'lim bo'yicha `TestDefinition.Code` bilan
 * harfma-harf bir xil (`MBTI16`, `BIG5`, `RIASEC`, `ACTIVITY`); backend buni aniq
 * `[property: JsonPropertyName("MBTI16")]` bilan kafolatlaydi (`AdminTestResultsDto`,
 * 2026-09-03 tuzatishi — undan oldin standart camelCase siyosati `mbti16` yuborardi).
 * Yangi `schema.d.ts` ham aynan KATTA HARFLI kalitlarni tasdiqlaydi.
 *
 * Bu tip — backend DTO'si EMAS, mijoz tomonidagi **moslashuv shakli**: eski (camelCase)
 * yozuv ham qabul qilinadi va `normalizeTestResults` bitta shaklga keltiradi, shu sabab
 * kalit yozilishi yana o'zgarsa ham sahifa JIMGINA bo'sh qolmaydi. Aynan shu sabab u
 * sxemadan re-export QILINMAYDI.
 */
export interface AssessmentTestResultsRaw {
  mbti16?: Mbti16Result | null;
  big5?: BigFiveResult | null;
  riasec?: RiasecResult | null;
  activity?: ActivityResult | null;
  MBTI16?: Mbti16Result | null;
  BIG5?: BigFiveResult | null;
  RIASEC?: RiasecResult | null;
  ACTIVITY?: ActivityResult | null;
}

/** Bitta shaklga keltirilgan natijalar — har biri yo'q bo'lsa `null` (bo'sh obyekt emas). */
export interface AssessmentTestResults {
  mbti16: Mbti16Result | null;
  big5: BigFiveResult | null;
  riasec: RiasecResult | null;
  activity: ActivityResult | null;
}

/**
 * `AdminAiAnalysisDto` dan re-export — mazmun bo'limlari (`summary`, `learningStyle`,
 * `careerSuggestions` …) `widgets/AiReportView` ning `AiReportSections` shakli bilan
 * struktura jihatdan mos, shu sabab to'g'ridan-to'g'ri widget'ga uzatiladi.
 *
 * `status` va `attentionFlags[].severity` sxemada `string` (backend `ToString()`) —
 * toraytirildi. `provider` ATAYLAB `string` bo'lib qoladi: sessiya detali uni faqat
 * ko'rsatadi, `AiProvider` union'iga tayanmaydi.
 */
export type AssessmentAiAnalysisDto = Omit<
  components['schemas']['AdminAiAnalysisDto'],
  'status' | 'attentionFlags'
> & {
  status: AiAnalysisStatus;
  attentionFlags: AiAttentionFlag[];
};

/** `aiHistory[]` — backend `AdminAiHistoryItemDto`. */
export type AssessmentAiHistoryItemDto = Omit<
  components['schemas']['AdminAiHistoryItemDto'],
  'status'
> & { status: AiAnalysisStatus };

/**
 * Sessiyadagi bitta test bloki — backend `AdminAssessmentTestItemDto` dan re-export.
 *
 * `questionCount`/`answeredCount` — SESSIYA snapshoti (`AssessmentTest.TotalCount` /
 * `AnsweredCount`), katalogdagi joriy savol soni EMAS.
 *
 * `scoringMode` sxemada `string` (`ToString()`), bu yerda ataylab TORAYTIRILGAN:
 * `buildTestSummaryRows` aynan `scoringMode === 'Survey'` ni tekshiradi (`Survey` anketa
 * BALLANMAYDI — `0` ball EMAS, `docs/06` qarorlar jurnali 2026-09-02). `status` esa
 * toraytirilmaydi — u `AssessmentTestStatus` (sessiya blok holati), `AssessmentStatus` EMAS.
 */
export type AssessmentTestItemDto = Omit<
  components['schemas']['AdminAssessmentTestItemDto'],
  'scoringMode'
> & {
  /** `Scored` — ballanadi; `Survey` — BALLANMAYDI (`0` ball EMAS). */
  scoringMode: 'Scored' | 'Survey';
};

/** `docs/07` 3.3 `student` bloki — backend `AdminAssessmentStudentRefDto`. */
export type AssessmentStudentRefDto = components['schemas']['AdminAssessmentStudentRefDto'];

/** `docs/07` 3.3 `school` bloki — backend `AdminAssessmentSchoolRefDto`. */
export type AssessmentSchoolRefDto = components['schemas']['AdminAssessmentSchoolRefDto'];

/** `docs/07` 3.3 `program` bloki — backend `AdminAssessmentProgramRefDto`. */
export type AssessmentProgramRefDto = components['schemas']['AdminAssessmentProgramRefDto'];

/**
 * `GET /api/admin/assessments/{id}` javobi — backend `AdminAssessmentDetailDto` dan
 * re-export (`GetAssessmentByIdQueryHandler` sarlavha maydonlarini uchta `LEFT JOIN` bilan
 * to'ldiradi).
 *
 * Enum'lar (`status`, `reliabilityFlag`) va AI/natija bloklari yuqoridagi toraytirilgan
 * tiplarga bog'lanadi.
 *
 * **Sxemadan ATAYLAB kengaytirilgan (toraytirilmagan) ikki guruh — yashirin emas, hujjatli:**
 *
 * 1. `| null` qo'shildi (`results`, `aiAnalysis`, `aiHistory`, `student`, `school`,
 *    `program`, `tests`): sxemada ular faqat IXTIYORIY (`?`), `| null` emas — Swashbuckle
 *    OpenAPI 3.0 da `$ref` yonida `nullable: true` chiqara olmaydi. Backend esa
 *    `DefaultIgnoreCondition` sozlanmagani uchun (`Api/Program.cs`) bu maydonlarni ANIQ
 *    `null` bilan yuboradi. Sxemaga so'zma-so'z ergashish bu yerda RUNTIME xatoga olib
 *    kelardi (`detail.tests === undefined` tekshiruvi `null` ni o'tkazib yuborardi).
 * 2. `?` qo'shildi (`status`, `startedAt`, `tests`): sxemada MAJBURIY, ya'ni backend ularni
 *    doim yuboradi. Sahifa shunga qaramay ularsiz ham ishlashi kerak — `AssessmentDetailPage`
 *    sessiyalar ro'yxatidan kelgan `Link state` ni ZAXIRA manba sifatida ishlatadi va
 *    ma'lumot yo'q bo'lsa "Ma'lumot yo'q" deb ANIQ ko'rsatadi, `0` yoki bo'sh satr EMAS
 *    (`docs/06` qarorlar jurnali, 2026-09-02). Bu — chidamlilik uchun ONGLI kengaytirish;
 *    maydon NOMI baribir `Omit` orqali sxemadan tekshiriladi.
 */
export type AssessmentDetailDto = Omit<
  components['schemas']['AdminAssessmentDetailDto'],
  | 'results'
  | 'aiAnalysis'
  | 'aiHistory'
  | 'status'
  | 'startedAt'
  | 'reliabilityFlag'
  | 'student'
  | 'school'
  | 'program'
  | 'tests'
> & {
  results: AssessmentTestResultsRaw | null;
  aiAnalysis: AssessmentAiAnalysisDto | null;
  aiHistory: AssessmentAiHistoryItemDto[] | null;
  status?: AssessmentStatus | null;
  startedAt?: string | null;
  reliabilityFlag?: ReliabilityFlag | null;
  student?: AssessmentStudentRefDto | null;
  school?: AssessmentSchoolRefDto | null;
  program?: AssessmentProgramRefDto | null;
  tests?: AssessmentTestItemDto[] | null;
  /**
   * MUVAQQAT (2026-09-12): backend `AdminAssessmentDetailDto` ga `hasPersonalityBattery`
   * qo'shildi, lekin `schema.d.ts` hali yangilanmagan (`generate:api` API ko'tarilgan holda
   * ishga tushirilishi kerak). O'shanda bu satr O'CHIRILADI va maydon `Omit` orqali
   * sxemadan keladi. Shakli: `bool` (ixtiyoriy — eski javoblarda yo'q, `undefined` bo'lsa
   * `AiAnalysisSection` standart `true` bilan ishlaydi, ya'ni xatti-harakat o'zgarmaydi).
   */
  hasPersonalityBattery?: boolean;
};

/**
 * Sessiyalar ro'yxatidan (`GET /api/admin/assessments`, `AdminAssessmentListItemDto`) detal
 * sahifasiga `Link state` orqali uzatiladigan qator. Detal endpointi endi sarlavha
 * maydonlarini o'zi ham qaytaradi (yuqoridagi izoh), shu sabab bu — ZAXIRA manba: detal
 * javobi eski/chala bo'lganda sahifa holat/vaqt/maktab/o'quvchini shundan oladi.
 */
export interface AssessmentDetailLocationState {
  assessment?: {
    id: string;
    studentId?: string | null;
    studentName?: string | null;
    schoolId?: string | null;
    schoolName?: string | null;
    status?: AssessmentStatus | null;
    startedAt?: string | null;
    completedAt?: string | null;
    durationMinutes?: number | null;
    reliabilityScore?: number | null;
    reliabilityFlag?: ReliabilityFlag | null;
  };
}

/** Sahifada ko'rsatiladigan yig'ma sarlavha ma'lumoti — noma'lum maydon har doim `null`. */
export interface AssessmentSessionMeta {
  status: AssessmentStatus | null;
  startedAt: string | null;
  completedAt: string | null;
  durationMinutes: number | null;
  reliabilityScore: number | null;
  reliabilityFlag: ReliabilityFlag | null;
  student: AssessmentStudentRefDto | null;
  school: AssessmentSchoolRefDto | null;
  program: AssessmentProgramRefDto | null;
}

/** `value` — haqiqiy `AssessmentStatus` qiymatimi (backend `string` yuboradi). */
export function isAssessmentStatus(value: string | null | undefined): value is AssessmentStatus {
  return (
    typeof value === 'string' && (ASSESSMENT_STATUS_VALUES as readonly string[]).includes(value)
  );
}

/** `value` — haqiqiy `ReliabilityFlag` qiymatimi. */
export function isReliabilityFlag(value: string | null | undefined): value is ReliabilityFlag {
  return (
    typeof value === 'string' && (RELIABILITY_FLAG_VALUES as readonly string[]).includes(value)
  );
}

/**
 * `GET /api/admin/assessments` ro'yxat elementi — backend `AdminAssessmentListItemDto`
 * (`Application/Admin/Assessments/AdminAssessmentDtos.cs`) dan to'liq re-export.
 * `programId`/`programName` endi sxemada BOR — qo'lda qo'shilgan `&` bloki olib tashlandi.
 *
 * Ro'yxat FAQAT maktab sessiyalarini qaytaradi (egasining qarori, 2026-09-07; ommaviy makon
 * sessiyalari `/admin/ommaviy` bo'limida), shu sabab 2026-09-06 dagi `source` ustuni yo'q.
 */
export type AssessmentListItemDto = components['schemas']['AdminAssessmentListItemDto'];

/** `GET /api/admin/assessments` query parametrlari — `ListAssessmentsQuery` bilan bir xil. */
export interface AssessmentsListQuery {
  schoolId?: string;
  status?: string;
  from?: string;
  to?: string;
  page: number;
  pageSize: number;
  sort?: string;
}
