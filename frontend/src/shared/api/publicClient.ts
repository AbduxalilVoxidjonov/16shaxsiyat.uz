import { apiRequest, type RequestOptions } from './client';
import { getSessionToken } from './sessionToken';

const SESSION_TOKEN_HEADER = 'X-Session-Token';

/**
 * `/api/public/*` uchun mijoz — mavjud bo'lsa `X-Session-Token` header'ini qo'shadi
 * (docs/07-api-shartnoma.md, 0-bo'lim; CLAUDE.md 8-qoida — ommaviy API'da ID qabul qilinmaydi).
 */
export function publicRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getSessionToken();
  const headers: Record<string, string> = { ...options.headers };
  if (token) {
    headers[SESSION_TOKEN_HEADER] = token;
  }
  return apiRequest<T>(path, { ...options, headers });
}
