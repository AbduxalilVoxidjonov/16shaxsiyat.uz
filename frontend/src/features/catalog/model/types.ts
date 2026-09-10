import type { components } from '@/shared/api/schema';
import type { BadgeVariant } from '@/shared/ui/Badge';
import type {
  AdminQuestionBranchingFields,
  AdminQuestionOption,
  AdminSection,
} from '@/shared/api/branchingTypes';

/**
 * Test katalogi va anketa konstruktori DTO'lari — `docs/07-api-shartnoma.md` 3.4-bo'lim,
 * backend `AssessmentCatalogController` (P37) + `AdminCatalogDtos.cs`.
 *
 * Barcha DTO `shared/api/schema.d.ts` dan **re-export** (`docs/10` §6). Enum maydonlari
 * (`kind`, `status`, `scoringMode`, `direction`) sxemada `string`/`number` — backend ularni
 * `ToString()`/`int` bilan yuboradi, shu sabab `Omit<…> & { … }` bilan TORAYTIRILADI
 * (`docs/10` §6.2, 2-naqsh): maydon NOMI baribir sxemadan tekshiriladi.
 */

export const TEST_SCORING_MODE_VALUES = ['Scored', 'Survey'] as const;
export type TestScoringMode = (typeof TEST_SCORING_MODE_VALUES)[number];

export const TEST_STATUS_VALUES = ['Draft', 'Published', 'Archived'] as const;
export type TestDefinitionStatus = (typeof TEST_STATUS_VALUES)[number];

export const TEST_STATUS_BADGE_VARIANT: Record<TestDefinitionStatus, BadgeVariant> = {
  Draft: 'neutral',
  Published: 'success',
  Archived: 'danger',
};

/**
 * Sxemada `kind`/`status`/`scoringMode` — oddiy `string` (backend `ToString()`).
 * Katalog jadvali va dialoglari aynan shu uch union'ga tayanadi (nishon/tugma holati),
 * shu sabab ular toraytiriladi.
 */
type CatalogTestEnums = {
  kind: 'System' | 'Custom';
  status: TestDefinitionStatus;
  scoringMode: TestScoringMode;
};

/** `GET /api/admin/catalog/tests` — ro'yxat elementi (`docs/07` 3.4: "Barchasi"). */
export type CatalogTestListItem = Omit<
  components['schemas']['CatalogTestListItemDto'],
  keyof CatalogTestEnums
> &
  CatalogTestEnums;

/**
 * `GET /api/admin/catalog/tests/{id}` — batafsil (`CatalogTestDetailDto`).
 *
 * `displayOrder` — katalogdagi tartib raqami. `PUT /tests/{id}` uni MAJBURIY talab qiladi,
 * shu sabab detal javobida ham bor — aks holda meta oynasi joriy tartibni bilmasdan saqlar
 * va uni tasodifiy qiymatga o'zgartirar edi.
 */
export type CatalogTestDetail = Omit<
  components['schemas']['CatalogTestDetailDto'],
  keyof CatalogTestEnums
> &
  CatalogTestEnums;

/**
 * `QuestionType` (backend `Domain/Catalog/QuestionType.cs`) — savol turi faqat YARATISHDA
 * tanlanadi, `PUT questions/{id}` uni umuman qabul qilmaydi.
 *
 * `ShortText`/`LongText`/`MultiChoice`/`Phone` — `docs/18` §2.1 kengaytmasi: FAQAT
 * `ScoringMode = Survey` anketalarda tanlanadi (B-1) — `QuestionEditorDialog` shu turlarni
 * `Scored` anketada butunlay yashiradi.
 */
export const QUESTION_TYPE_VALUES = [
  'Likert5',
  'Likert7',
  'Binary',
  'SingleChoice',
  'ForcedChoice',
  'ShortText',
  'LongText',
  'MultiChoice',
  'Phone',
] as const;
export type QuestionType = (typeof QUESTION_TYPE_VALUES)[number];

/** `docs/18` §2.1, B-1 — faqat `Survey` anketalarda ruxsat etilgan turlar (ballash yo'q). */
export const SURVEY_ONLY_QUESTION_TYPES = ['ShortText', 'LongText', 'MultiChoice', 'Phone'] as const;

/** `docs/18` §2.1 — matn javobli turlar (`text`, `RawValue = null`). */
export const TEXT_QUESTION_TYPES = ['ShortText', 'LongText', 'Phone'] as const;

/** Variantli turlar — `AnswerOption` ro'yxati talab qilinadi (`docs/18` §2.3). */
export const CHOICE_QUESTION_TYPES = ['SingleChoice', 'ForcedChoice', 'MultiChoice'] as const;

export function isSurveyOnlyQuestionType(type: QuestionType): boolean {
  return (SURVEY_ONLY_QUESTION_TYPES as readonly string[]).includes(type);
}

export function isTextQuestionType(type: QuestionType): boolean {
  return (TEXT_QUESTION_TYPES as readonly string[]).includes(type);
}

export function isChoiceQuestionType(type: QuestionType): boolean {
  return (CHOICE_QUESTION_TYPES as readonly string[]).includes(type);
}

/** `docs/18` §5 — savol varianti (`SingleChoice`/`ForcedChoice`/`MultiChoice`). */
export type CatalogQuestionOption = AdminQuestionOption;

/**
 * `GET /api/admin/catalog/tests/{id}/questions` — bitta savol qatori
 * (`CatalogQuestionItemDto`). `direction` sxemada oddiy `int`, domenda esa faqat `+1`/`-1`
 * (`docs/03` 1.2 "teskari savol") — toraytirildi. `isSystem` bo'lsa
 * `scale`/`direction`/`weight` qulflangan (BR-8, `CLAUDE.md` 9a).
 *
 * `docs/18` §2.3 kengaytmasi (`AdminQuestionBranchingFields`) HALI sxemada yo'q — vaqtinchalik
 * qo'lda yozilgan (`shared/api/branchingTypes.ts`dagi "MUVAQQAT" izohiga qarang).
 */
export type CatalogQuestionItem = Omit<
  components['schemas']['CatalogQuestionItemDto'],
  'direction'
> & { direction: 1 | -1 } & AdminQuestionBranchingFields;

/**
 * `docs/18` §2.2, §5 — bo'lim qatori. HALI sxemada yo'q, `AdminSection`dan re-export
 * (`shared/api/branchingTypes.ts`).
 */
export type CatalogSection = AdminSection;

/** `docs/03` 6.1-bo'lim saqlash shakli: `{ "from": 0, "to": 33, "label": "Past" }`. */
export type InterpretationBand = components['schemas']['InterpretationBandDto'];

/** `GET /api/admin/catalog/tests/{id}/scales` — faqat `Custom` testlarda tahrirlanadi. */
export type CatalogScaleItem = components['schemas']['CatalogScaleItemDto'];
