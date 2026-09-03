import type { components } from '@/shared/api/schema';
import type { BadgeVariant } from '@/shared/ui/Badge';

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
 */
export const QUESTION_TYPE_VALUES = [
  'Likert5',
  'Likert7',
  'Binary',
  'SingleChoice',
  'ForcedChoice',
] as const;
export type QuestionType = (typeof QUESTION_TYPE_VALUES)[number];

/**
 * `GET /api/admin/catalog/tests/{id}/questions` — bitta savol qatori
 * (`CatalogQuestionItemDto`). `direction` sxemada oddiy `int`, domenda esa faqat `+1`/`-1`
 * (`docs/03` 1.2 "teskari savol") — toraytirildi. `isSystem` bo'lsa
 * `scale`/`direction`/`weight` qulflangan (BR-8, `CLAUDE.md` 9a).
 */
export type CatalogQuestionItem = Omit<
  components['schemas']['CatalogQuestionItemDto'],
  'direction'
> & { direction: 1 | -1 };

/** `docs/03` 6.1-bo'lim saqlash shakli: `{ "from": 0, "to": 33, "label": "Past" }`. */
export type InterpretationBand = components['schemas']['InterpretationBandDto'];

/** `GET /api/admin/catalog/tests/{id}/scales` — faqat `Custom` testlarda tahrirlanadi. */
export type CatalogScaleItem = components['schemas']['CatalogScaleItemDto'];
