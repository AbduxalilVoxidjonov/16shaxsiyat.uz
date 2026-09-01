import { env } from '@/shared/config/env';
import { AppError, UNKNOWN_ERROR_CODE } from './AppError';
import type { ProblemDetails } from './types';

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
  credentials?: RequestCredentials;
}

const JSON_CONTENT_TYPE = 'application/json';

/**
 * Baza fetch o'rami — docs/10-frontend-arxitektura.md, 2 va 6-bo'limlar.
 *
 * - `VITE_API_BASE_URL` ga nisbatan yo'l quradi (agar `path` to'liq URL bo'lmasa).
 * - So'rov tanasini JSON qilib serializatsiya qiladi.
 * - `application/problem+json` (RFC 9457) javobini {@link AppError} ga o'giradi.
 * - `204 No Content` javobini xatosiz `undefined` sifatida qaytaradi.
 * - Tarmoq xatosini (fetch reject) {@link AppError} ga o'grab qayta uloqtiradi.
 *
 * `publicClient` va `adminClient` shu funksiya ustiga header va 401-refresh mantiqini qo'shadi.
 */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const url = path.startsWith('http') ? path : `${env.apiBaseUrl}${path}`;
  const headers: Record<string, string> = {
    Accept: JSON_CONTENT_TYPE,
    ...options.headers,
  };

  let body: BodyInit | undefined;
  if (options.body !== undefined) {
    headers['Content-Type'] = JSON_CONTENT_TYPE;
    body = JSON.stringify(options.body);
  }

  let response: Response;
  try {
    response = await fetch(url, {
      method: options.method ?? 'GET',
      headers,
      body,
      signal: options.signal,
      credentials: options.credentials,
    });
  } catch (cause) {
    throw AppError.networkError(cause);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') ?? '';
  const isJson = contentType.includes('json');

  if (!response.ok) {
    if (isJson) {
      let problem: ProblemDetails = {};
      try {
        problem = (await response.json()) as ProblemDetails;
      } catch {
        // Javob tanasi JSON sifatida o'qilmadi — pastdagi umumiy AppError bilan tugaymiz.
      }
      throw AppError.fromProblemDetails(problem, response.status);
    }
    throw new AppError({
      code: UNKNOWN_ERROR_CODE,
      message: response.statusText || `So'rov muvaffaqiyatsiz (${String(response.status)})`,
      status: response.status,
    });
  }

  if (!isJson) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
