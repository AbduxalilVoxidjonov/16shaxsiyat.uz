/**
 * E2E to'plami ATAYLAB kechadigan topilmalar. Ikki xil bo'ladi:
 *
 *   (a) HAQIQIY, lekin shu vazifa chegarasidan tashqarida qolgan kamchiliklar;
 *   (b) tekshiruv vositasining YOLG'ON IJOBIY natijalari — WCAG bo'yicha buzilish yo'q,
 *       lekin `axe` uni farqlay olmaydi. Har bir bunday yozuvda WCAG bandiga havola
 *       bilan sabab yozilishi SHART, aks holda yozuv haqiqiy nuqsonni yashiradi.
 *
 * Ro'yxat ikki vazifani bajaradi:
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

// P30-4 (`E-3 Test sahifasi` — savol raqami `text-neutral-400`) P45 reskinida TUZATILDI:
// raqam endi `bg-firuza-50` medalyoni ichida `text-firuza-700` (#0a6767 / #eaf7f7 ≈ 6.1:1,
// `LikertQuestion.tsx` `legend` bloki). Tekshirildi — `axe` shu ekranda bitta ham
// `serious`/`critical` topilma bermaydi, shu sabab yozuv o'chirildi.
export const KNOWN_A11Y_ISSUES: readonly KnownA11yIssue[] = [
  {
    id: 'P45-1',
    screens: ['M-1 Bosh sahifa', 'M-1.1 Mobil menyu'],
    rule: 'color-contrast',
    note:
      "YOLG'ON IJOBIY. Bosh sahifadagi \"Qanday ishlaydi\" kartalarining burchagidagi " +
      "ulkan tartib raqami (`HowItWorksSection.tsx`, 110px, `text-line/70` ≈ 1.19:1) — " +
      "sof BEZAK: u `aria-hidden=\"true\"`, ma'noni tashimaydi va o'sha raqam yonidagi " +
      '"Qadam 01" chipida to\'liq kontrast bilan takrorlanadi. WCAG 1.4.3 sof bezak ' +
      "matnni kontrast talabidan ochiq istisno qiladi; `axe` esa faqat vizual ko'rinishga " +
      "qaraydi va `aria-hidden` ni hisobga olmaydi. Rangni 3:1 gacha quyuqlashtirish " +
      "bezakni asosiy matndan kuchliroq qilib yuborardi — ya'ni o'qishni YOMONLASHTIRARDI.",
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
