import type { components } from '@/shared/api/schema';
import type { Gender } from '@/shared/api/types';
import type { AiAttentionFlag } from '@/widgets/AiReportView';
import type { ActivityLevel, AssessmentStatus, ReliabilityFlag } from './enums';

/**
 * `GET /api/admin/students/{id}` javob DTO'lari — `docs/07-api-shartnoma.md` 3.2-bo'lim.
 *
 * `docs/10` §6 qoidasi: backend DTO'lari qo'lda yozilmaydi — hammasi
 * `shared/api/schema.d.ts` dan re-export. Bu faylda BITTA ham qo'lda yozilgan DTO qolmadi;
 * ikki joyda faqat `Omit<…> & { … }` bilan TORAYTIRISH qilingan (`docs/10` §6.2, 2-naqsh) —
 * maydon NOMI va soni baribir sxemadan tekshiriladi, faqat TIPI toraytiriladi:
 *
 * 1. **Kalitlari yopiq lug'atlar** (`Mbti16Result.axes`, `BigFiveResult.factors`,
 *    `RiasecResult.types`, `ActivityResult.scales`) — sxemada `Record<string, …>`, bu esa
 *    `noUncheckedIndexedAccess` ostida har o'qishni `| undefined` qiladi va aynan
 *    diagrammalarni himoya qiladigan ANIQ kalitlar ro'yxatini (`R I A S E C` va h.k.)
 *    yo'qotadi — ya'ni diagrammani buzgan `A` ↔ `ART` xatosidan HIMOYA QILMAYDI. Kalitlar
 *    shartnomaning bir qismi (`docs/07` 3.2), shu sabab ular bu yerda ochiq yozilgan;
 *    qiymat tipi va qolgan barcha maydon esa sxemadan keladi.
 * 2. **Enum toraytirish** — backend enum'ni `ToString()` bilan qaytaradi, sxemada `string`;
 *    `Omit` + qayta e'lon bilan `enums.ts` dagi union'ga toraytiriladi.
 */

/** `Domain/Ai/AiProvider.cs` — sxemada haqiqiy `enum` sifatida bor. */
export type AiProvider = components['schemas']['AiProvider'];

/** `docs/05-database-schema.md`: "AiAnalysisStatus | 0 Pending, 1 Running, 2 Succeeded, 3 Failed". */
export const AI_ANALYSIS_STATUS_VALUES = ['Pending', 'Running', 'Succeeded', 'Failed'] as const;
export type AiAnalysisStatus = (typeof AI_ANALYSIS_STATUS_VALUES)[number];

/**
 * `docs/07` 3.2 `student` bloki — backend `AdminStudentDetailDto`.
 * `gender` sxemada `string`, bu yerda ommaviy oqim bilan bir xil `Gender` union'iga toraytirilgan.
 */
export type StudentDetailDto = Omit<
  components['schemas']['AdminStudentDetailDto'],
  'gender'
> & { gender: Gender };

/** `docs/07` 3.2 `assessments[]` — backend `AdminAssessmentSummaryDto`. */
export type AssessmentSummaryDto = Omit<
  components['schemas']['AdminAssessmentSummaryDto'],
  'status' | 'reliabilityFlag'
> & {
  status: AssessmentStatus;
  reliabilityFlag?: ReliabilityFlag | null;
};

/** 16 tipli model o'qi — backend `AdminAxisDto` (`docs/03` 2.3-bo'lim "Natija obyekti"). */
export type Mbti16AxisResult = components['schemas']['AdminAxisDto'];

/**
 * 16 tipli model natijasi — backend `AdminMbti16Dto`. `axes` kalitlari — 4 dixotomiya
 * (`docs/03` 2-bo'lim); sxemada ochiq lug'at, bu yerda YOPIQ (yuqoridagi 1-band).
 * Qolgan maydonlar (`resultCode`, `typeName`, `borderlineAxes`) sxemadan.
 */
export type Mbti16Result = Omit<components['schemas']['AdminMbti16Dto'], 'axes'> & {
  axes: {
    EI: Mbti16AxisResult;
    SN: Mbti16AxisResult;
    TF: Mbti16AxisResult;
    JP: Mbti16AxisResult;
  };
};

/** Big Five omili — backend `AdminFactorDto` (`docs/03` 3.4-bo'lim). */
export type BigFiveFactorResult = components['schemas']['AdminFactorDto'];

/**
 * Big Five natijasi. `factors` kalitlari — O C E A N (`docs/03` 3-bo'lim).
 *
 * `maturityIndex`/`maturityLevel` — backend `double? MaturityIndex` / `string? MaturityLevel`
 * (`AdminBig5Dto`): `MaturityIndex` FAQAT BIG5 **va** ACTIVITY birga topshirilganda
 * hisoblanadi (`docs/06` §8, 2026-09-02 qarori), aks holda `null` — `0` EMAS. Ilgari bu tip
 * ularni majburiy `number`/`string` deb e'lon qilardi va `StudentSummaryCards` to'g'ridan-
 * to'g'ri `maturityIndex.toFixed(1)` chaqirardi: faqat BIG5 topshirgan o'quvchining profili
 * `TypeError` bilan yiqilar edi. `features/assessments/model/types.ts` da shakl allaqachon
 * to'g'ri edi — ikki nusxa bir-biridan uzilib qolgani shu bilan tuzatildi.
 */
export type BigFiveResult = Omit<components['schemas']['AdminBig5Dto'], 'factors'> & {
  factors: {
    O: BigFiveFactorResult;
    C: BigFiveFactorResult;
    E: BigFiveFactorResult;
    A: BigFiveFactorResult;
    N: BigFiveFactorResult;
  };
};

/** RIASEC kasb sohasi — backend `AdminCareerFieldDto` (`docs/03` 4-bo'lim). */
export type RiasecCareerField = components['schemas']['AdminCareerFieldDto'];

/** RIASEC natijasi — `docs/03` 4-bo'lim. */
export type RiasecResult = Omit<
  components['schemas']['AdminRiasecDto'],
  'types' | 'consistency'
> & {
  /**
   * Kalitlar — **Holland harflari** `R I A S E C` (`docs/07` 3.2 `RIASEC.types`; `docs/03`
   * 4.1: "matnda qisqalik uchun R-I-A-S-E-C harflari"). Bazadagi `scale` kodlari (`ART`,
   * `SOC`, `ENT`, `CONV`) API'ga HECH QACHON chiqmaydi (`CLAUDE.md` 9-band), shuning uchun
   * bu yerda ham ishlatilmaydi. `resultCode` ham aynan shu harflardan iborat.
   *
   * Sxemadagi ochiq `Record<string, number>` bu kafolatni bermaydi — aynan shu sabab kalitlar
   * bu yerda ochiq yozilgan (diagramma `types.A` ni topa olmay yiqilgan xato takrorlanmasin).
   */
  types: { R: number; I: number; A: number; S: number; E: number; C: number };
  /** Backend `ToString()` bilan `string` yuboradi — `docs/03` 4.3 bo'yicha toraytirildi. */
  consistency: 'High' | 'Medium' | 'Low';
};

/**
 * Aktivlik natijasi — `docs/03` 5-bo'lim.
 *
 * `activityIndex`/`activityLevel` — backend `double? ActivityIndex` / `string? ActivityLevel`
 * (`AdminActivityDto`): ACTIVITY bloki tugallanmagan bo'lsa `null`. Ilgari bu yerda majburiy
 * deb e'lon qilingan edi (yuqoridagi `maturityIndex` bilan bir xil xato).
 */
export type ActivityResult = Omit<
  components['schemas']['AdminActivityDto'],
  'scales' | 'activityLevel'
> & {
  scales: { MOT: number; SELF: number; SOCA: number; ENG: number };
  activityLevel?: ActivityLevel | null;
};

/**
 * Test natijalari to'plami — `docs/07` 3.2 `latestAssessment.results`.
 *
 * Kalitlar sxemadan olinadi (`keyof AdminTestResultsDto` — `MBTI16 BIG5 RIASEC ACTIVITY`,
 * backend ularni `[property: JsonPropertyName("MBTI16")]` bilan kafolatlaydi), qiymat tipi
 * esa yuqoridagi TORAYTIRILGAN natija tiplariga bog'lanadi. Backend kalitni qayta nomlasa
 * (`MBTI16` → `mbti16`) mos kelmagan tarmoq `never` ga tushadi va sahifadagi `results.MBTI16`
 * o'qishi `tsc` da qizaradi — bo'limlar JIMGINA bo'sh qolmaydi (2026-09-03 hodisasi).
 *
 * Har biri ixtiyoriy: sessiya hali barcha bloklarni tugatmagan yoki `recalculate-scores`
 * hali ishlamagan bo'lishi mumkin.
 */
export type TestResultsDto = {
  [K in keyof components['schemas']['AdminTestResultsDto']]?: K extends 'MBTI16'
    ? Mbti16Result | null
    : K extends 'BIG5'
      ? BigFiveResult | null
      : K extends 'RIASEC'
        ? RiasecResult | null
        : K extends 'ACTIVITY'
          ? ActivityResult | null
          : never;
};

export type {
  AiAttentionFlagSeverity,
  AiAttentionFlag,
  AiCareerSuggestion,
  AiGrowthArea,
  AiReportSections,
  AiStrength,
} from '@/widgets/AiReportView';

/**
 * AI tahlil yozuvi — `docs/07` 3.2 `latestAssessment.aiAnalysis`, backend
 * `AdminAiAnalysisDto` dan re-export (`isFallbackReport`, `isModerated`, `errorMessage` va
 * 5 mazmun bo'limi — `learningStyle`, `motivationProfile`, `activityAssessment`,
 * `reliabilityNote`, `disclaimer` — endi sxemada BOR).
 *
 * Uch maydon TORAYTIRILGAN, chunki backend ularni `ToString()` bilan `string` yuboradi:
 * - `status` → `AiAnalysisStatus`, `provider` → `AiProvider`;
 * - `attentionFlags[].severity` → `AiAttentionFlagSeverity` (`AdminStudentDtos.cs`:
 *   "`Severity` har doim uchta qiymatdan biri: noma'lum qiymat `attention`ga keltiriladi").
 *
 * Mazmun bo'limlari `AiReportSections` (`widgets/AiReportView.tsx`) bilan struktura jihatdan
 * mos — `AiReportView` ga to'g'ridan-to'g'ri uzatiladi (mosligini `tsc` tekshiradi).
 */
export type AiAnalysisDto = Omit<
  components['schemas']['AdminAiAnalysisDto'],
  'status' | 'provider' | 'attentionFlags'
> & {
  status: AiAnalysisStatus;
  provider: AiProvider;
  attentionFlags: AiAttentionFlag[];
};

/** `docs/07` 3.2 `aiHistory[]` — backend `AdminAiHistoryItemDto`. */
export type AiAnalysisHistoryItemDto = Omit<
  components['schemas']['AdminAiHistoryItemDto'],
  'provider' | 'status'
> & {
  provider: AiProvider;
  status: AiAnalysisStatus;
};

/**
 * `docs/07` 3.2 `latestAssessment` — backend `AdminLatestAssessmentDto`.
 * `results`/`aiAnalysis`/`aiHistory` yuqoridagi toraytirilgan tiplarga bog'lanadi.
 */
export type LatestAssessmentDto = Omit<
  components['schemas']['AdminLatestAssessmentDto'],
  'results' | 'aiAnalysis' | 'aiHistory'
> & {
  results: TestResultsDto;
  aiAnalysis?: AiAnalysisDto | null;
  aiHistory: AiAnalysisHistoryItemDto[];
};

/**
 * `GET /api/admin/students/{id}` to'liq javobi — backend `AdminStudentProfileDto`.
 */
export type StudentProfileResponse = Omit<
  components['schemas']['AdminStudentProfileDto'],
  'student' | 'assessments' | 'latestAssessment'
> & {
  student: StudentDetailDto;
  assessments: AssessmentSummaryDto[];
  latestAssessment?: LatestAssessmentDto | null;
};

/**
 * `GET /api/admin/assessments/{id}/answers` javob tiplari (`RawAnswerDto`,
 * `AnswerSessionSignalsDto`, `AnswerScaleSignalDto`, `AnswerThresholdsDto`,
 * `AssessmentAnswersDto`) — P52-A (2026-09-12) da `shared/api/assessmentAnswersTypes.ts`ga
 * KO'CHIRILDI: bu javobni ko'rsatuvchi `widgets/AnswersSection.tsx` endi `features/students`
 * VA `features/assessments` ikkalasida ham ochiladi, `docs/10` §2 qoidasi esa
 * feature'lararo importni taqiqlaydi — umumiy tip `shared/`da turishi shart. Shu fayldan
 * import qiluvchi eski joylar yangi manzilga o'tkazildi (`AnswersSection.tsx`,
 * `useRawAnswersQuery.ts`, ularning testlari).
 */
