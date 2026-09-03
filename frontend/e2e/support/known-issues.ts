/**
 * P30 da HAQIQIY topilgan, LEKIN shu vazifada TUZATILMAGAN kamchiliklar
 * (P30 chegarasi: `frontend/src/**` va `src/**` ga tegilmaydi — u fayllarda boshqa
 * agentlar ishlayapti). Ro'yxat ikki vazifani bajaradi:
 *
 *   1. to'plam yashil qoladi — mavjud kamchiliklar butun E2E'ni to'sib qo'ymaydi;
 *   2. YANGI buzilish baribir testni yiqitadi — regressiya himoyasi saqlanadi.
 *
 * Kamchilik tuzatilgach, tegishli yozuv shu yerdan O'CHIRILADI (aks holda test uni
 * qayta paydo bo'lganda ham kechiradi).
 */

export interface KnownA11yIssue {
  id: string;
  /** `checkScreen` ga berilgan ekran nomi. */
  screens: readonly string[];
  /** `axe` qoidasi identifikatori. */
  rule: string;
  note: string;
}

export const KNOWN_A11Y_ISSUES: readonly KnownA11yIssue[] = [
  {
    id: 'P30-4',
    screens: ['E-3 Test sahifasi'],
    rule: 'color-contrast',
    note:
      "Savol raqami prefiksi (`legend` ichidagi `text-neutral-400` — \"1.\", \"2.\" …) " +
      'kontrast chegarasidan past — sahifada 11 ta element.',
  },
];

export interface KnownLayoutIssue {
  id: string;
  screens: readonly string[];
  /** Qaysi viewport kengliklarida uchraydi. */
  widths: readonly number[];
  note: string;
}

// P30-5 (`table.sr-only` — `shared/ui/VisuallyHidden.tsx`) va P30-6 (`DataTable` —
// `shared/ui/Table.tsx` dagi `relative`) tuzatildi, shu sabab yozuvlar o'chirildi.
export const KNOWN_LAYOUT_ISSUES: readonly KnownLayoutIssue[] = [];

/**
 * Konsol xatolari uchun ma'lum naqshlar. Hozircha bitta: admin panelda sahifa TO'LIQ
 * qayta yuklanganda (masalan to'g'ridan-to'g'ri havola) birinchi so'rov `401` bilan
 * qaytadi va faqat shundan keyin `refresh` ishlaydi — brauzer konsolida xato ko'rinadi
 * (P30-7). Foydalanuvchi oqimi buzilmaydi, lekin konsol "shovqin"i haqiqiy xatolarni
 * yashiradi.
 */
export const KNOWN_CONSOLE_PATTERNS: readonly RegExp[] = [
  /Failed to load resource: the server responded with a status of 401 \(Unauthorized\)/,
];

export function isKnownA11yIssue(screen: string, rule: string): boolean {
  return KNOWN_A11Y_ISSUES.some(
    (issue) => issue.rule === rule && issue.screens.includes(screen),
  );
}

export function isKnownLayoutIssue(screen: string, width: number): boolean {
  return KNOWN_LAYOUT_ISSUES.some(
    (issue) => issue.screens.includes(screen) && issue.widths.includes(width),
  );
}

export function isKnownConsoleError(message: string): boolean {
  return KNOWN_CONSOLE_PATTERNS.some((pattern) => pattern.test(message));
}
