import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { AppError, UNKNOWN_ERROR_CODE } from '@/shared/api/AppError';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

/**
 * Maktabga biriktiriladigan test (2026-09-23 egasi qarori: "Dasturlar" bo'limi olib
 * tashlandi — maktab formasida endi TESTLAR tanlanadi, `docs/07` §3.1 `testIds`).
 * `features/catalog` tipi import qilinmaydi (`docs/10` §2) — minimal shakl.
 */
export interface SchoolTestOption {
  id: string;
  code: string;
  nameUz: string;
  /** `Draft` · `Published` · `Archived`. */
  status: string;
  isActive: boolean;
}

/** `GET /api/admin/catalog/tests` — sahifalanmagan massiv (katalog kichik). */
export function useTestOptionsQuery(enabled = true) {
  return useQuery({
    queryKey: SCHOOLS_QUERY_KEYS.testOptions(),
    queryFn: async ({ signal }) => {
      const data = await adminRequest<SchoolTestOption[]>('/api/admin/catalog/tests', { signal });
      // Kutilmagan shakl jimgina "test yo'q" bo'lib ko'rinmasin ("ma'lumot yo'q ≠ nol").
      if (!Array.isArray(data)) {
        throw new AppError({ code: UNKNOWN_ERROR_CODE, message: UNKNOWN_ERROR_CODE, status: 200 });
      }
      return data;
    },
    enabled,
    staleTime: 30_000,
  });
}

/**
 * Formada TAKLIF qilinadigan testlar: faqat nashr qilingan va faol (arxivlangan/qoralama/
 * nofaol ko'rsatilmaydi). Istisno — maktabga ALLAQACHON biriktirilganlar: ular holatidan
 * qat'i nazar ro'yxatda qoladi, aks holda to'plam to'liq almashtirilganda (`testIds`)
 * ko'rinmay turib jimgina o'chib ketardi.
 */
export function selectOfferedTests(
  options: readonly SchoolTestOption[],
  selectedIds: readonly string[],
): SchoolTestOption[] {
  const selected = new Set(selectedIds);
  return options.filter(
    (option) => selected.has(option.id) || (option.status === 'Published' && option.isActive),
  );
}
