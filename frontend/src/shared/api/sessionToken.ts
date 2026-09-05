import { STORAGE_KEYS } from '@/shared/config/storageKeys';

interface PersistedSessionState {
  version?: number;
  state?: {
    sessionToken?: string | null;
    slug?: string | null;
    assessmentId?: string | null;
  };
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

/** Yangi ochilgan sessiya haqidagi minimal ma'lumot (`StartSessionResult` dan). */
export interface SessionSnapshot {
  sessionToken: string;
  slug: string;
  assessmentId: string;
}

/**
 * Sessiyani "qabul qiluvchi" — `sessionStore` (Zustand) modul yuklanganda o'zini shu yerga
 * ro'yxatdan o'tkazadi.
 *
 * Nega kerak: maktabsiz sessiyani `features/public-account` ochadi
 * (`POST /api/me/sessions`), lekin testni `features/public-assessment` olib boradi va
 * sessiya o'sha feature'ning store'ida yashaydi. `features/*` bir-birini import
 * QILMAYDI (`docs/10` §2), shu sabab uzatish shu neytral nuqta orqali ketadi.
 */
let sessionAdopter: ((snapshot: SessionSnapshot) => void) | null = null;

export function setSessionAdopter(adopter: ((snapshot: SessionSnapshot) => void) | null): void {
  sessionAdopter = adopter;
}

/**
 * Yangi ochilgan sessiyani o'quvchi oqimiga uzatadi.
 *
 * Ikki yo'l bor va ikkalasi ham to'g'ri natija beradi:
 * 1. `sessionStore` moduli ALLAQACHON yuklangan (foydalanuvchi shu tabda test oqimida
 *    bo'lgan) — adapter chaqiriladi, store darhol yangilanadi;
 * 2. Hali yuklanmagan (odatiy holat: kabinet va test oqimi alohida `lazy()` chunk'lar) —
 *    `localStorage` ga o'sha persist formatida yoziladi; store keyin yuklanganda Zustand
 *    persist uni AVTOMATIK o'qiydi (hydration) va bir xil holatga keladi.
 *
 * Yozuvda mavjud maydonlar saqlanadi (`selectedProgramCode` va h.k.) — faqat sessiya
 * bilan bog'liq uchta maydon almashtiriladi.
 */
export function adoptSession(snapshot: SessionSnapshot): void {
  if (sessionAdopter) {
    sessionAdopter(snapshot);
    return;
  }

  try {
    const raw = localStorage.getItem(STORAGE_KEYS.session);
    const parsed = raw ? (JSON.parse(raw) as PersistedSessionState) : {};
    const next: PersistedSessionState = {
      ...parsed,
      version: parsed.version ?? 0,
      state: { ...parsed.state, ...snapshot },
    };
    localStorage.setItem(STORAGE_KEYS.session, JSON.stringify(next));
  } catch {
    // Xotira to'lgan yoki private rejim — sessiya baribir joriy sahifada ishlaydi,
    // faqat qayta yuklashda tiklanmaydi.
  }
}
