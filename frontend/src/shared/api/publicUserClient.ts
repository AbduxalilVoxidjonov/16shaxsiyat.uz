import { STORAGE_KEYS } from '@/shared/config/storageKeys';
import { apiRequest, type RequestOptions } from './client';
import { AppError } from './AppError';
import type { components } from './schema';

/**
 * Ommaviy (Telegram orqali kirgan) foydalanuvchi uchun HTTP mijozi — `docs/07` §2a va §5.
 *
 * **Superadmin mijozidan (`adminClient.ts`) ATAYLAB alohida.** Ikki auditoriya ikki xil JWT
 * `aud` bilan ajratilgan (`docs/08` §2a) va ikki xil refresh yo'liga ega:
 * `/api/auth/refresh` (superadmin) ↔ `/api/auth/telegram/refresh` (ommaviy). Bitta mijozga
 * birlashtirish "qaysi token qaysi so'rovga qo'shiladi" degan savolni ish vaqtiga surib
 * qo'yardi — bir brauzerda ikkala sessiya bir vaqtda ochiq bo'lishi mumkin (superadmin o'z
 * kabinetini ham ko'rishi mumkin), va noto'g'ri token yuborilishi `401` bilan tugardi.
 *
 * ## Access token QAYERDA saqlanadi va NEGA
 *
 * **Faqat xotirada** (shu modulning o'zgaruvchisi + `publicUserStore` Zustand nusxasi).
 * `localStorage`/`sessionStorage`ga YOZILMAYDI:
 *
 * - `localStorage` har qanday JS uchun ochiq — bitta XSS (yoki buzilgan npm paketi) tokenni
 *   o'g'irlash uchun yetarli bo'lardi, token esa `/api/me/*` ga to'liq huquq beradi;
 * - Bizga u yerda saqlash KERAK ham emas: refresh token `httpOnly; Secure; SameSite=Strict`
 *   cookie'da (`srm_public_refresh_token`, `Path=/api/auth/telegram` — `docs/07` §2a.1),
 *   ya'ni JS'dan umuman o'qib bo'lmaydi, lekin brauzer uni refresh so'roviga o'zi qo'shadi.
 *   Sahifa yangilanganda sessiya `POST /api/auth/telegram/refresh` bilan tiklanadi — bu
 *   `localStorage`dagi nusxadan XAVFSIZ va, muhimi, TO'G'RIROQ: token muddati o'tgan bo'lsa
 *   server buni aytadi, `localStorage` esa eskirgan tokenni "tirik" deb ko'rsatib turardi.
 *
 * Bu qaror superadmin oqimidagi bilan bir xil (`docs/10` §5.1) — ikkala auditoriya uchun
 * bitta xavfsizlik modeli.
 */
let accessToken: string | null = null;

export function setPublicAccessToken(token: string | null): void {
  accessToken = token;
}

export function getPublicAccessToken(): string | null {
  return accessToken;
}

/**
 * "Bu brauzerda kirilgan edi" belgisi ({@link STORAGE_KEYS.publicSessionHint}) — token EMAS.
 * Batafsil sabab `storageKeys.ts` izohida.
 */
export function setPublicSessionHint(value: boolean): void {
  try {
    if (value) {
      localStorage.setItem(STORAGE_KEYS.publicSessionHint, '1');
    } else {
      localStorage.removeItem(STORAGE_KEYS.publicSessionHint);
    }
  } catch {
    // Private rejim yoki to'lgan xotira — belgi shunchaki saqlanmaydi. Sessiya baribir
    // ishlayveradi, faqat keyingi ochilishda avtomatik tiklanmaydi.
  }
}

export function hasPublicSessionHint(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEYS.publicSessionHint) === '1';
  } catch {
    return false;
  }
}

/** Refresh ham `401` bersa chaqiriladi — React qatlami sessiyani tozalaydi. */
let onSessionExpired: (() => void) | null = null;

export function setOnPublicSessionExpired(handler: (() => void) | null): void {
  onSessionExpired = handler;
}

/** Fon refresh'i yangi token olganda chaqiriladi — store HTTP qatlamidan orqada qolmasin. */
let onTokenRefreshed: ((token: string) => void) | null = null;

export function setOnPublicTokenRefreshed(handler: ((token: string) => void) | null): void {
  onTokenRefreshed = handler;
}

/** `POST /api/auth/telegram/refresh` javobi — sxemadan re-export (qo'lda DTO YO'Q). */
type PublicRefreshResult = components['schemas']['PublicRefreshResult'];

/**
 * Parallel so'rovlar bitta refresh chaqiruvini ulashishi uchun mutex — `adminClient` dagi
 * bilan bir xil naqsh: birinchi `401` refresh boshlaydi, qolganlari shu promise'ni kutadi.
 */
let refreshPromise: Promise<boolean> | null = null;

/**
 * Refresh cookie orqali yangi access token oladi. `true` — muvaffaqiyat.
 * Cookie `Path=/api/auth/telegram` bilan cheklangani uchun `credentials: 'include'` SHART.
 */
export async function refreshPublicAccessToken(): Promise<boolean> {
  refreshPromise ??= performRefresh().finally(() => {
    refreshPromise = null;
  });
  return refreshPromise;
}

async function performRefresh(): Promise<boolean> {
  try {
    const result = await apiRequest<PublicRefreshResult>('/api/auth/telegram/refresh', {
      method: 'POST',
      credentials: 'include',
    });
    setPublicAccessToken(result.accessToken);
    onTokenRefreshed?.(result.accessToken);
    return true;
  } catch {
    setPublicAccessToken(null);
    return false;
  }
}

/**
 * `/api/me/*` (va Telegram auth yo'llari) uchun mijoz — `Authorization: Bearer` qo'shadi,
 * `401` kelganda **bitta marta** refresh qilib so'rovni qayta yuboradi; refresh ham
 * muvaffaqiyatsiz bo'lsa `onSessionExpired` chaqiriladi va xato yuqoriga uzatiladi.
 */
export async function publicUserRequest<T>(
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
      const refreshed = await refreshPublicAccessToken();
      if (refreshed) {
        return publicUserRequest<T>(path, options, true);
      }
      onSessionExpired?.();
    }
    throw error;
  }
}
