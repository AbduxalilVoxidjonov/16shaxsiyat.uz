import type { InterpretationBand } from './types';

/**
 * Talqin oraliqlari qoidasi — `docs/03` §6.3 ("Talqin oraliqlari — butun son qoidasi",
 * 2026-09-02 da qat'iylashtirilgan). Bu modul backend'dagi
 * `CatalogPublishValidator.ValidateBandCoverage` ning AYNAN nusxasi: xato kodlari, tekshirish
 * TARTIBI va erta to'xtash nuqtalari bir xil. Yangi qoida bu yerda O'YLAB TOPILMAYDI — backend
 * o'zgarsa, avval `docs/03` §6.3, keyin ikkala tomon birga yangilanadi.
 *
 * **Nima uchun UI ham tekshiradi.** Superadmin oraliqni qo'lda kiritadi. Kasrli chegara
 * ruxsat etilsa (`0–33.3` / `33.4–66.6`), `scalePct = 33.35` bo'lgan o'quvchi hech qaysi
 * oraliqqa tushmaydi va `SUM` strategiyasi ishlash vaqtida
 * `SUM_INTERPRETATION_BAND_NOT_FOUND` bilan yiqiladi. Nashr validatsiyasi buni ushlaydi,
 * lekin faqat "Nashr qilish" bosilganda — UI esa kiritish paytida, xato qilingan qatorning
 * yonida aytadi.
 */

/** Oraliqlar qoplashi shart bo'lgan diapazon (`ScorePercent.MinValue`/`MaxValue`). */
export const INTERPRETATION_BAND_MIN = 0;
export const INTERPRETATION_BAND_MAX = 100;

/** "Teng bo'lish" qulayligi uchun ruxsat etilgan oraliqlar soni. */
export const MIN_EQUAL_SPLIT_COUNT = 2;
export const MAX_EQUAL_SPLIT_COUNT = 10;

/** Backend `PublishIssueDto.Code` qiymatlari (`CatalogPublishValidator`) — boshqa kod YO'Q. */
export const INTERPRETATION_BAND_ISSUE_CODES = [
  'SCALE_BANDS_MISSING',
  'SCALE_BAND_NOT_INTEGER',
  'SCALE_BAND_INVALID',
  'SCALE_BAND_INCOMPLETE',
  'SCALE_BAND_GAP',
  'SCALE_BAND_OVERLAP',
] as const;

export type InterpretationBandIssueCode = (typeof INTERPRETATION_BAND_ISSUE_CODES)[number];

/** `value` butun sonmi — backend `IsWholeNumber` bilan bir xil (`NaN` ham butun emas). */
function isWholeNumber(value: number): boolean {
  return Number.isFinite(value) && Math.abs(value - Math.round(value)) < 1e-9;
}

/**
 * Bitta shkalaning talqin oraliqlarini tekshiradi va backend qaytaradigan xato kodlarini
 * O'SHA TARTIBDA qaytaradi (takrorlanishi mumkin — har bir buzilgan qo'shni juftlik uchun
 * alohida `SCALE_BAND_GAP`/`SCALE_BAND_OVERLAP`, backend'da ham shunday). Bo'sh ro'yxat —
 * oraliqlar to'g'ri.
 *
 * Erta to'xtashlar backend bilan bir xil:
 * 1. oraliq umuman yo'q → faqat `SCALE_BANDS_MISSING`;
 * 2. birorta chegara butun son emas → faqat `SCALE_BAND_NOT_INTEGER` (qolgan tekshiruvlar
 *    kasrli chegara ustida ma'nosiz);
 * 3. birorta oraliq teskari (`from > to`) → faqat `SCALE_BAND_INVALID` (har bir teskari
 *    oraliq uchun bittadan).
 */
export function validateInterpretationBands(
  bands: readonly InterpretationBand[],
): InterpretationBandIssueCode[] {
  if (bands.length === 0) {
    return ['SCALE_BANDS_MISSING'];
  }

  if (bands.some((band) => !isWholeNumber(band.from) || !isWholeNumber(band.to))) {
    return ['SCALE_BAND_NOT_INTEGER'];
  }

  const ordered = [...bands].sort((a, b) => a.from - b.from);

  const invalid = ordered.filter((band) => band.from > band.to);
  if (invalid.length > 0) {
    return invalid.map(() => 'SCALE_BAND_INVALID' as const);
  }

  const issues: InterpretationBandIssueCode[] = [];

  const first = ordered[0];
  const last = ordered[ordered.length - 1];
  if (!first || !last) {
    return issues;
  }

  if (first.from !== INTERPRETATION_BAND_MIN || last.to !== INTERPRETATION_BAND_MAX) {
    issues.push('SCALE_BAND_INCOMPLETE');
  }

  for (let i = 1; i < ordered.length; i += 1) {
    const previous = ordered[i - 1];
    const current = ordered[i];
    if (!previous || !current) {
      continue;
    }
    // `to` INKLYUZIV, shuning uchun to'g'ri qo'shnilikda farq aynan 1 (`next.from == prev.to + 1`).
    const gap = current.from - previous.to;
    if (gap > 1) {
      issues.push('SCALE_BAND_GAP');
    } else if (gap < 1) {
      issues.push('SCALE_BAND_OVERLAP');
    }
  }

  return issues;
}

/**
 * `0–100` ni `count` ta teng (imkon qadar) oraliqqa bo'ladi — `docs/03` §6.3 misolidagi
 * natijani beradi: `3` → `0–33` / `34–66` / `67–100`. Chegaralar har doim butun son va
 * `next.from === prev.to + 1`, ya'ni natija hech qachon validatsiyadan yiqilmaydi.
 *
 * `labelAt` — yorliqni tashqaridan (i18n) oladi, funksiya sof qoladi.
 */
export function splitIntoEqualBands(
  count: number,
  labelAt: (index: number) => string,
): InterpretationBand[] {
  if (!Number.isInteger(count) || count < 1 || count > MAX_EQUAL_SPLIT_COUNT) {
    return [];
  }

  const bands: InterpretationBand[] = [];
  let from = INTERPRETATION_BAND_MIN;

  for (let i = 1; i <= count; i += 1) {
    const to =
      i === count ? INTERPRETATION_BAND_MAX : Math.floor((i * INTERPRETATION_BAND_MAX) / count);
    bands.push({ from, to, label: labelAt(i - 1) });
    from = to + 1;
  }

  return bands;
}

/**
 * Oraliqlar SAQLASHGA tayyormi (`ScaleDialog` saqlash tugmasi shu funksiyaga tayanadi).
 *
 * Bo'sh ro'yxat RUXSAT etiladi — shkala hozircha oraliqsiz saqlanishi mumkin,
 * `SCALE_BANDS_MISSING` faqat NASHR to'sig'i (backend ham `PUT` da buni tekshirmaydi).
 * Qolgan har qanday buzilish saqlashni to'xtatadi: backend `PUT` ni qabul qilar edi va xato
 * faqat "Nashr qilish" bosilganda chiqar edi.
 *
 * Yorliq (`label`) bo'sh bo'lmasligi — `docs/03` §6.3 oraliq qoidasi EMAS, backend
 * `UpdateTestScaleCommandValidator` ning alohida `Label.NotEmpty()` qoidasi; shu sabab
 * `validateInterpretationBands` ichida emas, shu yerda tekshiriladi.
 */
export function areBandsSavable(bands: readonly InterpretationBand[]): boolean {
  if (bands.length === 0) {
    return true;
  }
  if (bands.some((band) => band.label.trim().length === 0)) {
    return false;
  }
  return validateInterpretationBands(bands).length === 0;
}
