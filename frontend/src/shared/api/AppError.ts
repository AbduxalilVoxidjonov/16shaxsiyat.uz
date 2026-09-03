import type { ProblemDetails } from './types';

/** Tarmoq xatosi kodi — javob umuman kelmadi (fetch reject qildi). */
export const NETWORK_ERROR_CODE = 'NETWORK_ERROR';
/** Backend `code` bermagan yoki javob JSON emas bo'lganda ishlatiladigan kod. */
export const UNKNOWN_ERROR_CODE = 'UNKNOWN_ERROR';

/**
 * RFC 9457 `ProblemDetails` ning STANDART maydonlari — qolgan hamma narsa "kengaytma"
 * (`extensions`). `code`/`traceId`/`errors` standart emas, lekin `AppError` da alohida
 * maydon sifatida bor (`docs/06` 6-bo'lim), shu sabab ular ham bu ro'yxatda — kengaytmalar
 * ichida ikkinchi marta takrorlanmaydi.
 */
const KNOWN_PROBLEM_KEYS = new Set([
  'type',
  'title',
  'status',
  'detail',
  'code',
  'traceId',
  'errors',
]);

/**
 * Ilova butun davomida ishlatadigan yagona xato shakli.
 * `client.ts` barcha muvaffaqiyatsiz javoblarni (`ProblemDetails` yoki tarmoq xatosi)
 * shu klassga o'giradi — UI faqat shu tipni biladi.
 */
export class AppError extends Error {
  readonly code: string;
  readonly status: number;
  readonly errors?: Record<string, string[]>;
  readonly traceId?: string;
  /**
   * `ProblemDetails` ning standart bo'lmagan qo'shimcha maydonlari (`code`/`traceId`/`errors`
   * dan tashqari) — masalan `POST /catalog/tests/{id}/publish` ning `issues[]` ro'yxati
   * (`docs/07` 3.4-bo'lim). Qiymatlar `unknown`: shakl har endpointda har xil, shu sabab
   * ishlatuvchi feature ularni o'zi tekshirib (parse qilib) oladi — bu yerda ko'r-ko'rona
   * `as` YO'Q.
   */
  readonly extensions?: Record<string, unknown>;

  constructor(params: {
    code: string;
    message: string;
    status: number;
    errors?: Record<string, string[]>;
    traceId?: string;
    extensions?: Record<string, unknown>;
  }) {
    super(params.message);
    this.name = 'AppError';
    this.code = params.code;
    this.status = params.status;
    this.errors = params.errors;
    this.traceId = params.traceId;
    this.extensions = params.extensions;
  }

  /** `ProblemDetails` javobini `AppError` ga o'giradi. */
  static fromProblemDetails(problem: ProblemDetails, status: number): AppError {
    return new AppError({
      code: problem.code ?? UNKNOWN_ERROR_CODE,
      message: problem.detail ?? problem.title ?? UNKNOWN_ERROR_CODE,
      status: problem.status ?? status,
      errors: problem.errors,
      traceId: problem.traceId,
      extensions: extractExtensions(problem),
    });
  }

  /** Fetch bajarilmagan (tarmoq yo'q, CORS, timeout) holat uchun. */
  static networkError(cause: unknown): AppError {
    const message = cause instanceof Error ? cause.message : 'Tarmoq xatosi';
    return new AppError({ code: NETWORK_ERROR_CODE, message, status: 0 });
  }

  /** Bitta kengaytma maydonini oladi — bo'lmasa `undefined` (chaqiruvchi shaklini o'zi tekshiradi). */
  extension(key: string): unknown {
    return this.extensions?.[key];
  }
}

/**
 * Standart bo'lmagan maydonlarni yig'adi. Hech qanday kengaytma bo'lmasa `undefined`
 * qaytadi — mavjud chaqiruvchilar uchun xatti-harakat o'zgarmaydi (bo'sh obyekt emas).
 */
function extractExtensions(problem: ProblemDetails): Record<string, unknown> | undefined {
  const entries = Object.entries(problem).filter(
    ([key, value]) => !KNOWN_PROBLEM_KEYS.has(key) && value !== undefined,
  );
  return entries.length > 0 ? Object.fromEntries(entries) : undefined;
}
