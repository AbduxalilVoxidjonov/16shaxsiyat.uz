import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';

/**
 * Maktab tanlash (biriktirish paneli) uchun yengil DTO — xuddi
 * `features/students/api/useSchoolOptionsQuery.ts`dagi naqsh: `features/*` bir-birini
 * import qilmaydi (`docs/10`, 2-bo'lim), shu sabab bir xil `GET /api/admin/schools`
 * endpointiga mustaqil, minimal shaklda murojaat qilinadi.
 */
export interface SchoolOption {
  id: string;
  name: string;
  region: string;
  district: string;
}

const SCHOOL_OPTIONS_PAGE_SIZE = 20;
export const SCHOOL_SEARCH_DEBOUNCE_MS = 400;

/** Qidiruv matni bo'yicha mos maktablar — dasturga biriktirish panelidagi qidiruv uchun. */
export function useSchoolOptionsQuery(search: string, enabled: boolean) {
  return useQuery({
    queryKey: ['programs', 'schoolOptions', search],
    queryFn: ({ signal }) => {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      params.set('page', '1');
      params.set('pageSize', String(SCHOOL_OPTIONS_PAGE_SIZE));
      params.set('sort', 'name');
      return adminRequest<PagedResult<SchoolOption>>(`/api/admin/schools?${params.toString()}`, {
        signal,
      });
    },
    enabled,
    staleTime: 30_000,
  });
}

/** Berilgan id ro'yxati uchun maktab nomlarini oladi (biriktirilgan chiplarni ko'rsatish uchun). */
export function useSchoolsByIdsQuery(ids: string[]) {
  return useQuery({
    queryKey: ['programs', 'schoolsByIds', [...ids].sort()],
    queryFn: async ({ signal }) => {
      const results = await Promise.all(
        ids.map((id) => adminRequest<SchoolOption>(`/api/admin/schools/${id}`, { signal })),
      );
      return results;
    },
    enabled: ids.length > 0,
    staleTime: 30_000,
  });
}
