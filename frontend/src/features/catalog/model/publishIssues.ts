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
