import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';

/**
 * MUVAQQAT QO'LDA YOZILGAN TIP — `docs/07-api-shartnoma.md` 3.4-bo'lim ("Testlar" jadvali:
 * `GET /api/admin/catalog/tests`).
 *
 * Dasturga test qo'shish paneli mavjud testlar katalogidan tanlashni talab qiladi
 * (`prompts/35` C8-band: "testlarni tanlash"), lekin bu endpoint backend'da hali YO'Q —
 * loyihada faqat `AssessmentProgramsController` (P34) bor, `docs/07` 3.4dagi katalog CRUD
 * (`AssessmentCatalogController` yoki shunga o'xshash) hali yozilmagan (tekshirildi:
 * `src/StudentRoadMap.Api/Controllers/Admin/` da bunday controller yo'q, `IAppDbContext.
 * TestDefinitions` mavjud lekin uni admin uchun ochuvchi Query/handler yo'q).
 *
 * Bu — `features/students/model/enums.ts`dagi izohda ham qayd etilgan **oldindan ma'lum**
 * bo'shliq ("frontendda hali ularni olib keladigan ommaviy katalog endpointi ulanmagan").
 * `features/schools/model/types.ts` va `shared/api/types.ts`dagi "MUVAQQAT QO'LDA YOZILGAN
 * TIPLAR" naqshiga ergashib bu tip **oldindan** shartnoma (`docs/07` 3.4) bo'yicha yoziladi:
 * backend tayyor bo'lgach `npm run generate:api` ishga tushiriladi va shu tip hamda
 * `queryFn` pastdagi TODO bo'yicha `schema.d.ts`dan re-export bilan almashtiriladi.
 *
 * **Natija:** panel hozircha `ErrorState` (404) ko'rsatadi — bu XATO EMAS, real holat:
 * tanlanadigan test yo'q, chunki ularni ro'yxatlaydigan backend endpoint mavjud emas.
 * PM/backend ushbu endpointni qo'shgach panel avtomatik ishga tushadi (kod tayyor).
 */
export interface CatalogTestOption {
  id: string;
  code: string;
  nameUz: string;
  kind: 'System' | 'Custom';
  isSystem: boolean;
  status: 'Draft' | 'Published' | 'Archived';
  questionCount: number;
  scaleCount: number;
  estimatedMinutes: number;
}

/**
 * `GET /api/admin/catalog/tests` — faqat `Published` va `isActive` testlarni dasturga
 * qo'shish mumkin bo'lishi kerak (backend tomonidan filtrlanadi deb kutiladi); hozircha
 * so'rov chindan yuboriladi (kod real), lekin 404 qaytaradi (yuqoridagi izohga qarang).
 */
export function useCatalogTestOptionsQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['programs', 'catalogTestOptions'],
    queryFn: ({ signal }) =>
      adminRequest<CatalogTestOption[]>('/api/admin/catalog/tests?status=Published&isActive=true', {
        signal,
      }),
    enabled,
    staleTime: 60_000,
    retry: false,
  });
}
