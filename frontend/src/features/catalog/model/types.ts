import type { BadgeVariant } from '@/shared/ui/Badge';

/**
 * MUVAQQAT QO'LDA YOZILGAN TIPLAR — `docs/07-api-shartnoma.md` 3.4-bo'lim ("Test katalogi
 * va anketa konstruktori"). Naqsh `shared/api/types.ts`dagi "MUVAQQAT QO'LDA YOZILGAN
 * TIPLAR" bo'limi va `features/students/model/enums.ts`dagi izoh bilan bir xil.
 *
 * **Tasdiqlangan bo'shliq:** bu endpointlar backend'da HALI YO'Q. Tekshirildi —
 * `src/StudentRoadMap.Api/Controllers/Admin/` ichida faqat `SchoolsController`,
 * `StudentsController`, `AssessmentsController`, `AssessmentProgramsController` (P34),
 * `DashboardController`, `AuditController` bor; katalog/test CRUD controller yo'q.
 * `IAppDbContext.TestDefinitions` mavjud, lekin uni ochuvchi admin Query/Command'lar yo'q.
 * Bu holat `features/students/model/enums.ts`da ham oldindan qayd etilgan
 * ("frontendda hali ularni olib keladigan ommaviy katalog endpointi ulanmagan... hozircha
 * boshqa promptda").
 *
 * **Natija:** bu feature'dagi so'rovlar (`api/*.ts`) real, docs/07 shartnomasi bo'yicha
 * yozilgan, lekin backend tayyor bo'lguncha `404`/tarmoq xatosi qaytaradi — sahifalar buni
 * `ErrorState` + "Qayta urinish" bilan to'g'ri ko'rsatadi (soxta muvaffaqiyat YO'Q).
 * Backend qo'shilgach: `npm run generate:api`, bu tiplar `schema.d.ts`dan re-export bilan
 * almashtiriladi.
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

/** `GET /api/admin/catalog/tests` — ro'yxat elementi (`docs/07` 3.4: "Barchasi"). */
export interface CatalogTestListItem {
  id: string;
  code: string;
  nameUz: string;
  kind: 'System' | 'Custom';
  isSystem: boolean;
  status: TestDefinitionStatus;
  isActive: boolean;
  scoringMode: TestScoringMode;
  questionCount: number;
  scaleCount: number;
  estimatedMinutes: number;
  version: number;
  usedInProgramCount: number;
}

/** `GET /api/admin/catalog/tests/{id}` — batafsil. */
export interface CatalogTestDetail extends CatalogTestListItem {
  descriptionUz: string | null;
  pageSize: number;
  shuffleQuestions: boolean;
}

/** `GET /api/admin/catalog/tests/{id}/questions` — bitta savol qatori. */
export interface CatalogQuestionItem {
  id: string;
  code: string;
  order: number;
  textUz: string;
  type: string;
  scale: string;
  direction: 1 | -1;
  weight: number;
  isRequired: boolean;
  isActive: boolean;
}
