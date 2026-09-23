import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import { PUBLIC_SPACE_QUERY_KEYS } from './publicSpaceKeys';

/**
 * Test tanlash uchun yengil DTO (2026-09-23: "Dasturlar" bo'limi olib tashlandi, ommaviy
 * makonga endi TEST biriktiriladi — `docs/07` §3.7). `features/catalog` tipi qayta
 * ISHLATILMAYDI (`docs/10` §2: feature'lar bir-birini import qilmaydi) — xuddi shu
 * `GET /api/admin/catalog/tests` ga minimal shaklda murojaat.
 */
export interface TestOption {
  id: string;
  code: string;
  nameUz: string;
  /** `Draft` · `Published` · `Archived`. */
  status: string;
  isActive: boolean;
}

/** Katalogdagi barcha testlar (sahifalanmagan massiv; katalog kichik — o'nlab yozuv). */
export function useTestOptionsQuery() {
  return useQuery({
    queryKey: PUBLIC_SPACE_QUERY_KEYS.testOptions(),
    queryFn: ({ signal }) => adminRequest<TestOption[]>('/api/admin/catalog/tests', { signal }),
    staleTime: 30_000,
  });
}
