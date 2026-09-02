import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { CatalogTestListItem } from '../model/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/**
 * `GET /api/admin/catalog/tests` — `docs/07` 3.4-bo'lim. Backend hali yo'q (`model/types.ts`
 * boshidagi izohga qarang); so'rov real, hozircha 404/tarmoq xatosi kutiladi.
 */
export function useCatalogTestsQuery() {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.list(),
    queryFn: ({ signal }) =>
      adminRequest<CatalogTestListItem[]>('/api/admin/catalog/tests', { signal }),
    staleTime: 30_000,
    retry: false,
  });
}
