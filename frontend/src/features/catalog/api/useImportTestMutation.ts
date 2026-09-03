import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { TestImportFile } from '../model/importSchema';
import type { CatalogScaleItem, CatalogTestDetail } from '../model/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

interface CreateCatalogTestRequestBody {
  code: string;
  nameUz: string;
  descriptionUz?: string;
  estimatedMinutes: number;
  pageSize?: number;
  shuffleQuestions?: boolean;
  displayOrder?: number;
  scoringMode?: string;
}

interface ImportQuestionsRequestBody {
  questions: TestImportFile['questions'];
}

/**
 * "Anketa yuklash" — `prompts/35` B3–B5-band. Uch bosqichli chaqiruv:
 * 1) `POST /api/admin/catalog/tests` — metadata bilan `Draft` test yaratadi;
 * 2) `POST /api/admin/catalog/tests/{id}/scales` — faylda shkala bo'lsa, har biri
 *    talqin oraliqlari bilan (`docs/07` §3.4);
 * 3) `POST /api/admin/catalog/tests/{id}/questions/import` — savollarni yuklaydi.
 *
 * Shkalalar savollardan OLDIN yaratiladi: savol shkalaga KOD orqali bog'lanadi va nashr
 * validatsiyasi (`docs/03` §6.3) shkala shu testga tegishli ekanini talab qiladi — shkalasiz
 * import qilingan anketani nashr qilib bo'lmasdi. JSON fayllarda (seed sxemasi) shkala yo'q,
 * shu sabab bu bosqich shunchaki o'tkazib yuboriladi.
 *
 * Muvaffaqiyat — test doim `Draft` holatida qoladi (`prompts/35` B5-band), chunki
 * `POST /tests` shu holatda yaratadi va bu mutatsiya `publish` chaqirmaydi. Dublikat `code`
 * bo'lsa backend `409` beradi — chaqiruvchi (`TestImportDialog`) buni alohida ushlaydi.
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
          descriptionUz: file.descriptionUz ?? undefined,
          estimatedMinutes: file.estimatedMinutes,
          pageSize: file.pageSize,
          shuffleQuestions: file.shuffleQuestions,
          displayOrder: file.displayOrder,
          scoringMode: file.scoringMode,
        } satisfies CreateCatalogTestRequestBody,
      });

      for (const [index, scale] of (file.scales ?? []).entries()) {
        await adminRequest<CatalogScaleItem>(
          `/api/admin/catalog/tests/${created.id}/scales`,
          {
            method: 'POST',
            body: {
              code: scale.code,
              nameUz: scale.nameUz,
              descriptionUz: scale.descriptionUz ?? null,
              displayOrder: index,
              interpretationBands: scale.interpretationBands,
            },
          },
        );
      }

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
