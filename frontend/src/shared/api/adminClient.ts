import { apiRequest, type RequestOptions } from './client';
import { AppError } from './AppError';

/**
 * Superadmin access tokeni — **faqat xotirada** (modul o'zgaruvchisi), `localStorage`ga
 * yozilmaydi (docs/10-frontend-arxitektura.md, 5.1-bo'lim; CLAUDE.md). React holati sifatida
 * `features/auth/store/authStore.ts` (Zustand, persist YO'Q) shu setter'larni chaqirib
 * o'zini sinxron tutadi.
 */
let accessToken: string | null = null;

export function setAdminAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAdminAccessToken(): string | null {
  return accessToken;
}

/** Refresh ham 401 bersa yoki qayta urinish muvaffaqiyatsiz bo'lsa chaqiriladi (logout uchun). */
let onSessionExpired: (() => void) | null = null;

export function setOnAdminSessionExpired(handler: (() => void) | null): void {
  onSessionExpired = handler;
}

/**
 * Fon refresh'i yangi access token olganda chaqiriladi. Usiz `authStore.accessToken` eski
 * qiymatda qolib ketardi — HTTP qatlami bilan React qatlami bir-biriga zid bo'lib,
 * `ProtectedRoute` ko'radigan token bilan haqiqatda yuborilayotgan token farq qilardi.
 */
let onTokenRefreshed: ((token: string) => void) | null = null;

export function setOnAdminTokenRefreshed(handler: ((token: string) => void) | null): void {
  onTokenRefreshed = handler;
}

interface RefreshResponse {
  accessToken: string;
  refreshToken?: string;
  expiresIn?: number;
}

/**
 * Parallel so'rovlarning barchasi bitta refresh chaqiruvini ulashishi uchun mutex.
 * Birinchi 401 refresh boshlaydi; shu payt kelgan boshqa 401'lar xuddi shu promise'ni kutadi.
 */
let refreshPromise: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  refreshPromise ??= performRefresh().finally(() => {
    refreshPromise = null;
  });
  return refreshPromise;
}

async function performRefresh(): Promise<boolean> {
  try {
    // Refresh token `httpOnly` cookie'da saqlanadi (docs/08-auth-va-xavfsizlik.md) —
    // brauzer uni `credentials: 'include'` bilan avtomatik yuboradi, JS'dan o'qilmaydi.
    const result = await apiRequest<RefreshResponse>('/api/auth/refresh', {
      method: 'POST',
      credentials: 'include',
    });
    setAdminAccessToken(result.accessToken);
    onTokenRefreshed?.(result.accessToken);
    return true;
  } catch {
    setAdminAccessToken(null);
    return false;
  }
}

/**
 * `/api/admin/*` va `/api/auth/*` uchun mijoz — `Authorization: Bearer` qo'shadi,
 * `401` kelganda **bitta** refresh urinib ko'radi (mutex), muvaffaqiyatli bo'lsa so'rovni
 * bir marta qayta yuboradi; refresh ham muvaffaqiyatsiz bo'lsa `onSessionExpired` chaqiriladi.
 */
export async function adminRequest<T>(
  path: string,
  options: RequestOptions = {},
  isRetry = false,
): Promise<T> {
  const headers: Record<string, string> = { ...options.headers };
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  try {
    return await apiRequest<T>(path, {
      ...options,
      headers,
      credentials: options.credentials ?? 'include',
    });
  } catch (error) {
    if (error instanceof AppError && error.status === 401 && !isRetry) {
      const refreshed = await refreshAccessToken();
      if (refreshed) {
        return adminRequest<T>(path, options, true);
      }
      onSessionExpired?.();
    }
    throw error;
  }
}
