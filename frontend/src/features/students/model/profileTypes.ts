import type { Gender } from '@/shared/api/types';
import type { AiReportSections } from '@/widgets/AiReportView';
import type { ActivityLevel, AssessmentStatus, ReliabilityFlag } from './enums';

/**
 * `GET /api/admin/students/{id}` javob DTO'lari — docs/07-api-shartnoma.md, 3.2-bo'lim.
 *
 * MUVAQQAT QO'LDA YOZILGAN TIPLAR — xuddi `shared/api/types.ts` dagi 1.7–1.9-bo'lim TODO'si
 * bilan bir xil sabab: bu endpoint P14'da backend'da tayyor, lekin `npm run generate:api`
 * shu ish doirasida ATAYLAB ishga tushirilmagan (topshiriqda "boshqa agent hozir backend'da
 * ishlayapti" — parallel P16–P18 AI moduli qo'shilishi kutilmoqda, hozir ishga tushirish
 * chala/beqaror sxema olib kelishi mumkin). Backend tayyor bo'lgach: `npm run generate:api`,
 * so'ng bu fayldagi tiplar `components['schemas'][...]`dan re-export bilan almashtiriladi va
 * ishlatuvchi kod (`useStudentProfileQuery` va komponentlar) maydon nomi/nullability farqiga
 * qarab moslashtiriladi.
 */

export type AiProvider = 'Gemini' | 'OpenAi' | 'Anthropic';

/** `docs/05-database-schema.md`: "AiAnalysisStatus | 0 Pending, 1 Running, 2 Succeeded, 3 Failed". */
export const AI_ANALYSIS_STATUS_VALUES = ['Pending', 'Running', 'Succeeded', 'Failed'] as const;
export type AiAnalysisStatus = (typeof AI_ANALYSIS_STATUS_VALUES)[number];

export interface StudentDetailDto {
  id: string;
  fullName: string;
  birthDate: string;
  age: number;
  gender: Gender;
  grade: number;
  classLetter: string | null;
  phone: string;
  parentPhone: string | null;
  email: string | null;
  school: { id: string; name: string };
  consentGivenAt: string | null;
  createdAt: string;
}

export interface AssessmentSummaryDto {
  id: string;
  status: AssessmentStatus;
  startedAt: string;
  completedAt: string | null;
  durationMinutes: number | null;
  reliabilityScore: number | null;
  reliabilityFlag: ReliabilityFlag | null;
  isLatest: boolean;
}

/** 16 tipli model — docs/03, 2.3-bo'lim "Natija obyekti". */
export interface Mbti16AxisResult {
  pct: number;
  letter: string;
  borderline: boolean;
}

export interface Mbti16Result {
  resultCode: string;
  typeName: string;
  axes: {
    EI: Mbti16AxisResult;
    SN: Mbti16AxisResult;
    TF: Mbti16AxisResult;
    JP: Mbti16AxisResult;
  };
  borderlineAxes: string[];
}

/** Big Five — docs/03, 3.4-bo'lim "Natija obyekti". */
export interface BigFiveFactorResult {
  raw: number;
  pct: number;
  level: string;
}

export interface BigFiveResult {
  factors: {
    O: BigFiveFactorResult;
    C: BigFiveFactorResult;
    E: BigFiveFactorResult;
    A: BigFiveFactorResult;
    N: BigFiveFactorResult;
  };
  stabilityPct: number;
  maturityIndex: number;
  maturityLevel: string;
}

/** RIASEC — docs/03, 4-bo'lim. */
export interface RiasecCareerField {
  name: string;
  professions: string[];
}

export interface RiasecResult {
  resultCode: string;
  types: { R: number; I: number; ART: number; SOC: number; ENT: number; CONV: number };
  differentiation: number;
  consistency: 'High' | 'Medium' | 'Low';
  careerFields: RiasecCareerField[];
}

/** Aktivlik — docs/03, 5-bo'lim. */
export interface ActivityResult {
  scales: { MOT: number; SELF: number; SOCA: number; ENG: number };
  activityIndex: number;
  activityLevel: ActivityLevel;
  needsAttention: boolean;
}

/**
 * Test natijalari to'plami — docs/07, 3.2 `latestAssessment.results`. Har biri ixtiyoriy:
 * sessiya hali barcha bloklarni tugatmagan yoki `recalculate-scores` hali ishlamagan bo'lishi
 * mumkin — CLAUDE.md "MAXSUS DIQQAT — Bo'sh ma'lumotga chidamlilik" shu yerga ham tegishli.
 */
export interface TestResultsDto {
  MBTI16?: Mbti16Result | null;
  BIG5?: BigFiveResult | null;
  RIASEC?: RiasecResult | null;
  ACTIVITY?: ActivityResult | null;
}

export type {
  AiAttentionFlag,
  AiCareerSuggestion,
  AiGrowthArea,
  AiReportSections,
  AiStrength,
} from '@/widgets/AiReportView';

/**
 * AI tahlil yozuvi — docs/07, 3.2 `latestAssessment.aiAnalysis` + docs/04, 2.8-bo'lim
 * (`AiAnalysis` entity, `ErrorMessage`). Mazmun bo'limlari (`AiReportSections`, `widgets/
 * AiReportView.tsx`) bilan bir xil — widget o'z shaklini "egallaydi", bu DTO faqat transport
 * maydonlarini (`id`/`status`/`provider`/…) ustiga qo'shadi.
 *
 * Barcha mazmun maydonlari ixtiyoriy/`null`: `status !== 'Succeeded'` bo'lganda (`Pending`,
 * `Running`, `Failed`) backend faqat transport maydonlarini qaytarishi kutiladi (docs/09,
 * 7-bo'lim — muvaffaqiyatsiz urinish alohida yozuv). `isFallbackReport` docs/07 3.2 JSON
 * namunasida yo'q — docs/09, 11-bo'lim ("Zaxira hisobot") ga asoslanib qo'shilgan, backend
 * P16–P18 hali yozilmagani uchun aniq maydon nomi PM/backend bilan tasdiqlanishi kerak
 * (hisobotda savol sifatida qoldirilgan).
 */
export interface AiAnalysisDto extends AiReportSections {
  id: string;
  status: AiAnalysisStatus;
  provider: AiProvider;
  model: string;
  promptVersion: string;
  createdAt: string;
  errorMessage?: string | null;
  /** docs/09, 11-bo'lim: barcha AI urinishi muvaffaqiyatsiz bo'lganda ko'rsatiladigan shablon hisobot belgisi. */
  isFallbackReport?: boolean;
}

export interface AiAnalysisHistoryItemDto {
  id: string;
  provider: AiProvider;
  createdAt: string;
  status: AiAnalysisStatus;
  isCurrent: boolean;
}

export interface LatestAssessmentDto {
  id: string;
  results: TestResultsDto;
  aiAnalysis: AiAnalysisDto | null;
  aiHistory: AiAnalysisHistoryItemDto[];
}

export interface StudentProfileResponse {
  student: StudentDetailDto;
  assessments: AssessmentSummaryDto[];
  latestAssessment: LatestAssessmentDto | null;
}

/**
 * `GET /api/admin/assessments/{id}/answers?testCode=` — docs/07, 3.3-bo'lim ("Xom javoblar
 * (audit uchun)"). Aniq maydon nomlari docs'da batafsil emas — `docs/03`, 1-bo'lim (`Question`
 * maydonlari) va P25 vazifa matni ("savol matni, javob, `durationMs`, `revisionCount`") asosida
 * mantiqiy nomlangan; backend tayyor bo'lgach `generate:api` bilan tasdiqlanadi/tuzatiladi.
 */
export interface RawAnswerDto {
  questionCode: string;
  questionText: string;
  value: number;
  durationMs: number | null;
  revisionCount: number;
}
