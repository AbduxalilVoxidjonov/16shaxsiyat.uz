import type { ProblemDetails } from './types';

/** Tarmoq xatosi kodi — javob umuman kelmadi (fetch reject qildi). */
export const NETWORK_ERROR_CODE = 'NETWORK_ERROR';
/** Backend `code` bermagan yoki javob JSON emas bo'lganda ishlatiladigan kod. */
export const UNKNOWN_ERROR_CODE = 'UNKNOWN_ERROR';

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

  constructor(params: {
    code: string;
    message: string;
    status: number;
    errors?: Record<string, string[]>;
    traceId?: string;
  }) {
    super(params.message);
    this.name = 'AppError';
    this.code = params.code;
    this.status = params.status;
    this.errors = params.errors;
    this.traceId = params.traceId;
  }

  /** `ProblemDetails` javobini `AppError` ga o'giradi. */
  static fromProblemDetails(problem: ProblemDetails, status: number): AppError {
    return new AppError({
      code: problem.code ?? UNKNOWN_ERROR_CODE,
      message: problem.detail ?? problem.title ?? UNKNOWN_ERROR_CODE,
      status: problem.status ?? status,
      errors: problem.errors,
      traceId: problem.traceId,
    });
  }

  /** Fetch bajarilmagan (tarmoq yo'q, CORS, timeout) holat uchun. */
  static networkError(cause: unknown): AppError {
    const message = cause instanceof Error ? cause.message : 'Tarmoq xatosi';
    return new AppError({ code: NETWORK_ERROR_CODE, message, status: 0 });
  }
}
