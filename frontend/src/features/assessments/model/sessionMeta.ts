import type { BadgeVariant } from '@/shared/ui/Badge';
import { ASSESSMENT_STATUS_VALUES, RELIABILITY_FLAG_VALUES } from './types';
import type {
  AssessmentDetailDto,
  AssessmentDetailLocationState,
  AssessmentSessionMeta,
  AssessmentStatus,
  ReliabilityFlag,
} from './types';

/** Holat chipi rangi — `features/students/model/enums.ts` dagi bilan bir xil semantika. */
export const ASSESSMENT_STATUS_BADGE_VARIANT: Record<AssessmentStatus, BadgeVariant> = {
  Draft: 'neutral',
  InProgress: 'primary',
  Completed: 'primary',
  Analyzing: 'warning',
  Analyzed: 'success',
  AnalysisFailed: 'danger',
  Abandoned: 'neutral',
};

function pad2(value: number): string {
  return String(value).padStart(2, '0');
}

/**
 * `ISO-8601` → `KK.OO.YYYY HH:MM` (UTC). `shared/lib/formatDate.ts` bilan bir xil qaror:
 * `Intl.DateTimeFormat` ISHLATILMAYDI (muhitga qarab ajratkich o'zgaradi), backend sanalari
 * UTC (`docs/07` 4-bo'lim). Sessiyada aynan VAQT ham muhim (davomiylik, "tez javob" signali),
 * shu sabab faqat sanadan iborat `formatDate` yetmaydi.
 *
 * Qiymat yo'q yoki noto'g'ri bo'lsa `null` qaytadi — chaqiruvchi "ma'lumot yo'q" matnini
 * i18n orqali o'zi qo'yadi (bu yerda `'—'` qotirilmaydi).
 */
export function formatDateTime(iso: string | null | undefined): string | null {
  if (!iso) return null;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return null;
  return (
    `${pad2(date.getUTCDate())}.${pad2(date.getUTCMonth() + 1)}.${String(date.getUTCFullYear())}` +
    ` ${pad2(date.getUTCHours())}:${pad2(date.getUTCMinutes())}`
  );
}

/**
 * Davomiylik (daqiqa): backend bergan `durationMinutes` ustunlik qiladi; u yo'q bo'lsa
 * boshlangan/yakunlangan vaqtdan hisoblanadi. Sessiya yakunlanmagan bo'lsa `null` —
 * "0 daqiqa" DEB KO'RSATILMAYDI (`docs/06` qarorlar jurnali, 2026-09-02).
 */
export function resolveDurationMinutes(
  durationMinutes: number | null | undefined,
  startedAt: string | null | undefined,
  completedAt: string | null | undefined,
): number | null {
  if (typeof durationMinutes === 'number' && Number.isFinite(durationMinutes)) {
    return durationMinutes;
  }
  if (!startedAt || !completedAt) return null;
  const start = new Date(startedAt).getTime();
  const end = new Date(completedAt).getTime();
  if (Number.isNaN(start) || Number.isNaN(end) || end < start) return null;
  return Math.round((end - start) / 60000);
}

function firstDefined<T>(...values: (T | null | undefined)[]): T | null {
  for (const value of values) {
    if (value !== null && value !== undefined) return value;
  }
  return null;
}

/**
 * Sarlavha ma'lumotini ikki manbadan yig'adi: (1) detal javobining o'zi — backend bu
 * maydonlarni qo'shgan kuni avtomatik ishlaydi; (2) sessiyalar ro'yxatidan kelgan
 * navigatsiya holati (`AdminAssessmentListItemDto` qatori). Ikkalasida ham yo'q maydon
 * `null` bo'lib qoladi va UI uni "ma'lumot yo'q" deb ANIQ ko'rsatadi.
 */
export function resolveSessionMeta(
  detail: AssessmentDetailDto | undefined,
  state: AssessmentDetailLocationState | null | undefined,
): AssessmentSessionMeta {
  const row = state?.assessment;
  const fromRow = row && (!detail || row.id === detail.id) ? row : undefined;

  const startedAt = firstDefined(detail?.startedAt, fromRow?.startedAt);
  const completedAt = firstDefined(detail?.completedAt, fromRow?.completedAt);

  const studentId = fromRow?.studentId;
  const studentName = fromRow?.studentName;
  const schoolId = fromRow?.schoolId;
  const schoolName = fromRow?.schoolName;

  return {
    status: firstDefined(detail?.status, fromRow?.status),
    startedAt,
    completedAt,
    durationMinutes: resolveDurationMinutes(
      firstDefined(detail?.durationMinutes, fromRow?.durationMinutes),
      startedAt,
      completedAt,
    ),
    reliabilityScore: firstDefined(detail?.reliabilityScore, fromRow?.reliabilityScore),
    reliabilityFlag: firstDefined(detail?.reliabilityFlag, fromRow?.reliabilityFlag),
    student:
      detail?.student ??
      (studentId && studentName ? { id: studentId, fullName: studentName } : null),
    school: detail?.school ?? (schoolId && schoolName ? { id: schoolId, name: schoolName } : null),
    program: detail?.program ?? null,
  };
}

/** Hech bir sarlavha maydoni ma'lum emasmi — shunda sahifa sababini tushuntiruvchi izoh chiqaradi. */
export function isSessionMetaEmpty(meta: AssessmentSessionMeta): boolean {
  return (
    meta.status === null &&
    meta.startedAt === null &&
    meta.completedAt === null &&
    meta.durationMinutes === null &&
    meta.reliabilityScore === null &&
    meta.reliabilityFlag === null &&
    meta.student === null &&
    meta.school === null &&
    meta.program === null
  );
}

function asString(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null;
}

function asNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

function asStatus(value: unknown): AssessmentStatus | null {
  return typeof value === 'string' &&
    (ASSESSMENT_STATUS_VALUES as readonly string[]).includes(value)
    ? (value as AssessmentStatus)
    : null;
}

function asFlag(value: unknown): ReliabilityFlag | null {
  return typeof value === 'string' && (RELIABILITY_FLAG_VALUES as readonly string[]).includes(value)
    ? (value as ReliabilityFlag)
    : null;
}

/**
 * `location.state` — mijoz tomonidan (brauzer tarixi orqali) o'zgartirilishi mumkin bo'lgan
 * ISHONCHSIZ ma'lumot: `react-router` uni `unknown`/`any` sifatida beradi. Shu sabab ko'r-ko'rona
 * `as` YO'Q — har maydon tekshirib olinadi, noto'g'ri qiymat jimgina tashlanadi.
 */
export function parseAssessmentLocationState(state: unknown): AssessmentDetailLocationState | null {
  if (typeof state !== 'object' || state === null || !('assessment' in state)) return null;
  const raw = (state as { assessment: unknown }).assessment;
  if (typeof raw !== 'object' || raw === null) return null;

  const row = raw as Record<string, unknown>;
  const id = asString(row.id);
  if (!id) return null;

  return {
    assessment: {
      id,
      studentId: asString(row.studentId),
      studentName: asString(row.studentName),
      schoolId: asString(row.schoolId),
      schoolName: asString(row.schoolName),
      status: asStatus(row.status),
      startedAt: asString(row.startedAt),
      completedAt: asString(row.completedAt),
      durationMinutes: asNumber(row.durationMinutes),
      reliabilityScore: asNumber(row.reliabilityScore),
      reliabilityFlag: asFlag(row.reliabilityFlag),
    },
  };
}
