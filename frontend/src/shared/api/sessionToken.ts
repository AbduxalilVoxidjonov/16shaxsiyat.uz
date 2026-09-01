import { STORAGE_KEYS } from '@/shared/config/storageKeys';

interface PersistedSessionState {
  state?: { sessionToken?: string | null };
}

/**
 * `publicClient` uchun joriy o'quvchi sessiya tokenini o'qiydi.
 *
 * Eslatma (arxitektura): `shared/` `features/*` ni import qilmaydi, shuning uchun bu funksiya
 * `sessionStore` (Zustand + persist, `features/public-assessment/store/sessionStore.ts`) yozgan
 * xuddi shu `localStorage` kalitini bevosita o'qiydi — ikkalasi shu kalit nomi ({@link STORAGE_KEYS.session})
 * orqali "shartnoma" qiladi, JS import orqali emas.
 */
export function getSessionToken(): string | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEYS.session);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as PersistedSessionState;
    return parsed.state?.sessionToken ?? null;
  } catch {
    return null;
  }
}
