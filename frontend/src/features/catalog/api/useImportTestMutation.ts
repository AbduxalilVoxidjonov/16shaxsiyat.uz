import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { TestImportFile } from '../model/importSchema';
import type { CatalogTestDetail } from '../model/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

interface CreateCatalogTestRequestBody {
  code: string;
  nameUz: string;
  descriptionUz?: string;
  estimatedMinutes: number;
  pageSize?: number;
  shuffleQuestions?: boolean;
  displayOrder?: number;
}

interface ImportQuestionsRequestBody {
  questions: TestImportFile['questions'];
}

/**
 * "Test yuklash" (JSON import) — `prompts/35` B3–B5-band. Ikki bosqichli chaqiruv:
 * 1) `POST /api/admin/catalog/tests` — metadata bilan `Draft` test yaratadi;
 * 2) `POST /api/admin/catalog/tests/{id}/questions/import` — savollarni yuklaydi
 *    (`docs/07` 3.4-bo'lim: "`Custom` — to'liq").
 *
 * Muvaffaqiyat — test doim `Draft` holatida qoladi (`prompts/35` B5-band: "hech qachon
 * to'g'ridan-to'g'ri o'quvchiga chiqmaydi"), chunki `POST /tests` shu holatda yaratadi va
 * bu mutatsiya `publish` chaqirmaydi. Dublikat `code` bo'lsa backend `409` beradi
 * (`prompts/35` B6-band) — chaqiruvchi (`TestImportDialog`) buni alohida ushlaydi.
 *
 * Backend endpoint hali yo'q (`model/types.ts` boshidagi izohga qarang) — bu mutatsiya
 * real so'rov yuboradi, hozircha xato (404) kutiladi.
 */
export function useImportTestMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (file: TestImportFile) => {
      const created = await adminRequest<CatalogTestDetail>('/api/admin/catalog/tests', {
        method: 'POST',
        body: {
          code: file.code,
          nameUz: file.nameUz,
          descriptionUz: file.descriptionUz,
          estimatedMinutes: file.estimatedMinutes,
          pageSize: file.pageSize,
          shuffleQuestions: file.shuffleQuestions,
          displayOrder: file.displayOrder,
        } satisfies CreateCatalogTestRequestBody,
      });

      await adminRequest<CatalogTestDetail>(
        `/api/admin/catalog/tests/${created.id}/questions/import`,
        {
          method: 'POST',
          body: { questions: file.questions } satisfies ImportQuestionsRequestBody,
        },
      );

      return created;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
    },
  });
}
