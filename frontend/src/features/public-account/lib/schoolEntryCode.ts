/** Maktab kodi uzunligi (defissiz) — backend `SchoolEntryCode.Length` bilan bir xil. */
export const SCHOOL_ENTRY_CODE_LENGTH = 8;

/**
 * Foydalanuvchi kiritgan matnni yuborish shakliga keltiradi: katta harf, defis/bo'shliq/
 * en-dash olib tashlanadi, faqat `[A-Z0-9]` qoladi, 8 belgidan ortig'i kesiladi.
 * Server o'zi ham normalizatsiya qiladi (`SchoolEntryCode.Normalize`) — bu yerdagi
 * qoida uning kichik nusxasi; alifbo tekshiruvi (masalan `0`/`O`) serverga qoldirilgan.
 */
export function normalizeSchoolEntryCode(input: string): string {
  return input
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, '')
    .slice(0, SCHOOL_ENTRY_CODE_LENGTH);
}

/** `ABCD2345` → `ABCD-2345` (ko'rsatish uchun; qisqa qiymat defissiz qoladi). */
export function formatSchoolEntryCode(code: string): string {
  return code.length > 4 ? `${code.slice(0, 4)}-${code.slice(4)}` : code;
}

/** 8 belgi to'liq kiritilganmi — faqat shunda so'rov yuboriladi. */
export function isCompleteSchoolEntryCode(code: string): boolean {
  return code.length === SCHOOL_ENTRY_CODE_LENGTH;
}
