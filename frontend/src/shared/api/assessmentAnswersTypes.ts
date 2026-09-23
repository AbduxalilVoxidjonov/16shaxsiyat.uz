/**
 * `GET /api/admin/assessments/{id}/answers` javob DTO'lari — `docs/07-api-shartnoma.md`
 * 3.3-bo'lim ("Savolma-savol javoblar va tahlili").
 *
 * Bu fayl ikki sababdan `features/students/model/profileTypes.ts`dan bu yerga (P52-A,
 * 2026-09-12) ko'chirildi:
 *
 * 1. **Widget ikki feature'da ishlatiladi.** `widgets/AnswersSection.tsx` endi
 *    `features/students` (o'quvchi profili) VA `features/assessments` (bitta sessiya
 *    detali) ikkalasida ham ochiladi (egasining talabi — eski sessiyalarning javoblarini
 *    ham ko'rish). `docs/10` §2 qoidasi: "`features/*` bir-birini import qilmaydi — umumiy
 *    narsa `shared/`ga chiqadi". Tip shu widget bilan birga ko'chdi.
 * 2. **Sxema eskirgan (P52-B, commit `0a9d582`).** Backend `AdminAssessmentAnswerDto`ga
 *    `SelectedValues`/`SelectedOptionTexts`/`ScoringMode`/`TextValue` qo'shdi va
 *    `RawValue`/`EffectiveValue`/`IsFastAnswer`ni `int?`/`bool?` qildi (P52 tarmoqlanuvchi
 *    so'rovnoma — `ShortText`/`LongText`/`MultiChoice`/`Phone` javoblari endi shu jadvalda
 *    ham ko'rinadi), lekin `npm run generate:api` bu sessiyada ISHGA TUSHIRILMAGAN — shu
 *    sabab `schema.d.ts` hali eski holatda (`rawValue`/`effectiveValue`: majburiy `number`,
 *    `isFastAnswer`: majburiy `boolean`, yangi maydonlar umuman yo'q). Naqsh
 *    `shared/api/registrationModeTypes.ts`/`branchingTypes.ts`dagi bilan AYNAN bir xil:
 *    backend chiqib generatsiya ishga tushgach bu fayldagi `Omit<…> & {…}` kengaytmalari
 *    olib tashlanadi, `RawAnswerDto` yana to'g'ridan-to'g'ri re-export bo'ladi.
 */
import type { components } from './schema';

/**
 * `docs/06` 8-bo'lim / `AdminAssessmentTestItemDto.ScoringMode` bilan bir xil satr —
 * `"Scored"` (ballanadigan, Likert) yoki `"Survey"` (ballanmaydigan so'rovnoma). Backend
 * buni endi javobning O'ZIGA (`AdminAssessmentAnswerDto.ScoringMode`) ham qo'shdi — mijoz
 * qaysi test blokidan ekanini `tests[]` bilan solishtirib aniqlamasin, degan maqsadda
 * (backend DTO izohi, `AdminAssessmentDtos.cs`).
 */
export const ANSWER_SCORING_MODE_VALUES = ['Scored', 'Survey'] as const;
export type AnswerScoringMode = (typeof ANSWER_SCORING_MODE_VALUES)[number];

/**
 * Bitta javob qatori — backend `AdminAssessmentAnswerDto` (P52-B kengaytmasi bilan).
 *
 * `rawValue`/`effectiveValue`/`isFastAnswer` — sxemada hali majburiy (`number`/`boolean`),
 * backendda esa `Scored` qatorlarda ma'noli, `Survey` qatorlarda `null` ("qo'llanilmaydi",
 * `0`/`false` EMAS — egasi topgan kamchilik, 2026-09-12). `Omit` bilan TORAYTIRILMAYDI,
 * balki KENGAYTIRILADI (`| null` qo'shiladi) — `docs/10` §6.2 "sxema `null`ni ifodalay
 * olmaydigan joy" naqshiga o'xshash, faqat sabab bu yerda YANGI maydon emas, ESKIRGAN sxema.
 *
 * `selectedValues`/`selectedOptionTexts`/`textValue`/`scoringMode` — sxemada UMUMAN yo'q,
 * yuqoridagi fayl izohiga qarang.
 */
export type RawAnswerDto = Omit<
  components['schemas']['AdminAssessmentAnswerDto'],
  'rawValue' | 'effectiveValue' | 'isFastAnswer'
> & {
  rawValue: number | null;
  effectiveValue: number | null;
  isFastAnswer: boolean | null;
  /** `MultiChoice` javobida tanlangan variantlarning RAQAM qiymatlari, `selectedOptionTexts` bilan bir xil tartibda. */
  selectedValues?: number[] | null;
  /** `MultiChoice` javobida tanlangan variantlarning MATNI — jadvalda ko'rsatiladigan asosiy shakl (`selectedValues` raqamlari EMAS). */
  selectedOptionTexts?: string[] | null;
  /** `ShortText`/`LongText`/`Phone` javobining xom matni. */
  textValue?: string | null;
  scoringMode: AnswerScoringMode;
  /** Katalogdagi test nomi (backend `AdminAssessmentAnswerDto.TestNameUz`, 2026-09-23). */
  testNameUz?: string | null;
};

/** Sessiya darajasidagi ishonchlilik signallari — backend `AdminAnswerSessionSignalsDto` (`docs/03` §7). */
export type AnswerSessionSignalsDto = components['schemas']['AdminAnswerSessionSignalsDto'];

/** Shkala darajasidagi teskari savol ziddiyati — backend `AdminAnswerScaleSignalDto` (`docs/03` §7.1 band 4). */
export type AnswerScaleSignalDto = components['schemas']['AdminAnswerScaleSignalDto'];

/**
 * `ScoringConstants` chegaralari — backend `AdminAnswerThresholdsDto`. Frontendda
 * QO'LDA TAKRORLANMAYDI: `900 ms` / `12` / `6 daqiqa` faqat shu javobdan o'qiladi, shunda
 * domendagi konstanta o'zgarsa UI avtomatik ergashadi.
 */
export type AnswerThresholdsDto = components['schemas']['AdminAnswerThresholdsDto'];

/**
 * `GET /api/admin/assessments/{id}/answers` to'liq javobi — backend `AdminAssessmentAnswersDto`.
 * `answers` yuqoridagi kengaytirilgan `RawAnswerDto[]`ga bog'lanadi, qolgani sxemadan.
 */
export type AssessmentAnswersDto = Omit<
  components['schemas']['AdminAssessmentAnswersDto'],
  'answers'
> & {
  answers: RawAnswerDto[];
};
