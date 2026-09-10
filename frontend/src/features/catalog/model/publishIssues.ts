import { AppError } from '@/shared/api/AppError';

/**
 * `POST /api/admin/catalog/tests/{id}/publish` — `400 TEST_NOT_PUBLISHABLE` javobidagi bitta
 * muammo (`docs/07` 3.4-bo'lim, backend `PublishIssueDto`). Backend BARCHA muammoni bir yo'la
 * yig'adi (`CatalogPublishValidator`), shu sabab admin ro'yxatni to'liq ko'radi.
 */
export interface PublishIssue {
  code: string;
  scale: string | null;
  questionCode: string | null;
  message: string;
}

/** Backend `ProblemDetails` kengaytmasining kaliti. */
const ISSUES_EXTENSION_KEY = 'issues';

function asString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value : null;
}

/**
 * Xatoni `PublishIssue[]` ga o'giradi. Javob kutilgan shaklda bo'lmasa (kengaytma yo'q,
 * massiv emas, elementlar obyekt emas) — bo'sh ro'yxat, ya'ni chaqiruvchi umumiy xabarga
 * qaytadi. Ko'r-ko'rona `as` YO'Q: har bir maydon tekshiriladi.
 */
export function parsePublishIssues(error: unknown): PublishIssue[] {
  if (!(error instanceof AppError)) {
    return [];
  }

  const raw = error.extension(ISSUES_EXTENSION_KEY);
  if (!Array.isArray(raw)) {
    return [];
  }

  const issues: PublishIssue[] = [];
  for (const item of raw) {
    if (typeof item !== 'object' || item === null) {
      continue;
    }
    const record = item as Record<string, unknown>;
    const code = asString(record.code);
    const message = asString(record.message);
    if (!code && !message) {
      continue;
    }
    issues.push({
      code: code ?? '',
      scale: asString(record.scale),
      questionCode: asString(record.questionCode),
      message: message ?? '',
    });
  }

  return issues;
}

/**
 * Backend `PublishIssueDto.Code` → i18n kaliti (`docs/18` §5 jadvali + eski shkala/savol
 * kodlari). ATAYLAB TO'LIQ EMAS: `SCALE_TOO_FEW_QUESTIONS` va `TEST_NAME_DUPLICATE` backend
 * xabarlari ICHIDA aniq sonni/nomni olib keladi — ularni umumiy tarjima bilan almashtirish
 * ma'lumot yo'qotardi, shu sabab bu kodlarda backend `message` ko'rsatiladi
 * (`PublishTestDialog.tsx`dagi `PublishIssueText`ga qarang).
 */
export const PUBLISH_ISSUE_MESSAGE_KEYS: Record<string, string> = {
  TEST_HAS_NO_QUESTIONS: 'catalog.publish.issues.TEST_HAS_NO_QUESTIONS',
  TEST_HAS_NO_SCALES: 'catalog.publish.issues.TEST_HAS_NO_SCALES',
  QUESTION_WITHOUT_SCALE: 'catalog.publish.issues.QUESTION_WITHOUT_SCALE',
  SCALE_BANDS_MISSING: 'catalog.bands.issues.SCALE_BANDS_MISSING',
  SCALE_BAND_NOT_INTEGER: 'catalog.bands.issues.SCALE_BAND_NOT_INTEGER',
  SCALE_BAND_INVALID: 'catalog.bands.issues.SCALE_BAND_INVALID',
  SCALE_BAND_INCOMPLETE: 'catalog.bands.issues.SCALE_BAND_INCOMPLETE',
  SCALE_BAND_GAP: 'catalog.bands.issues.SCALE_BAND_GAP',
  SCALE_BAND_OVERLAP: 'catalog.bands.issues.SCALE_BAND_OVERLAP',
  // `docs/18` §5 — tarmoqlanuvchi so'rovnoma nashr validatsiyasi (10 ta yangi kod).
  VISIBILITY_UNKNOWN_QUESTION: 'catalog.publish.issues.VISIBILITY_UNKNOWN_QUESTION',
  VISIBILITY_FORWARD_REFERENCE: 'catalog.publish.issues.VISIBILITY_FORWARD_REFERENCE',
  VISIBILITY_OPERATOR_MISMATCH: 'catalog.publish.issues.VISIBILITY_OPERATOR_MISMATCH',
  VISIBILITY_VALUE_UNKNOWN: 'catalog.publish.issues.VISIBILITY_VALUE_UNKNOWN',
  BRANCHING_NOT_ALLOWED_IN_SCORED: 'catalog.publish.issues.BRANCHING_NOT_ALLOWED_IN_SCORED',
  QUESTION_TYPE_NOT_SCORABLE: 'catalog.publish.issues.QUESTION_TYPE_NOT_SCORABLE',
  QUESTION_OPTIONS_REQUIRED: 'catalog.publish.issues.QUESTION_OPTIONS_REQUIRED',
  QUESTION_OPTION_VALUE_DUPLICATE: 'catalog.publish.issues.QUESTION_OPTION_VALUE_DUPLICATE',
  SECTION_EMPTY: 'catalog.publish.issues.SECTION_EMPTY',
  INPUT_PATTERN_INVALID: 'catalog.publish.issues.INPUT_PATTERN_INVALID',
};
