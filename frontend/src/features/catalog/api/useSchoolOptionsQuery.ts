import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/**
 * Maktab tanlash (test "Biriktirish" kartasi) uchun yengil DTO — `features/*` bir-birini
 * import qilmaydi (`docs/10` §2), shu sabab `GET /api/admin/schools` ga mustaqil, minimal
 * shaklda murojaat qilinadi (`features/students/api/useSchoolOptionsQuery.ts` naqshi).
 * Ro'yxat faqat `Kind = School` makonlarni qaytaradi — ommaviy makon bu yerda chiqmaydi
 * (u kartada alohida belgi bilan boshqariladi).
 */
export interface SchoolOption {
  id: string;
  name: string;
  region: string;
  district: string;
}

const SCHOOL_OPTIONS_PAGE_SIZE = 20;
export const SCHOOL_SEARCH_DEBOUNCE_MS = 300;

/** Qidiruv matni bo'yicha maktablar (nom bo'yicha saralangan, birinchi 20 ta). */
export function useSchoolOptionsQuery(search: string, enabled: boolean) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.schoolOptions(search),
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

/**
 * Biriktirilgan maktablar nomlari (chiplar uchun) — `AdminTestAssignmentDto.schoolIds` faqat
 * ID beradi. Har ID uchun `GET /api/admin/schools/{id}`; topilmagani (o'chirilgan maktab)
 * ro'yxatdan tushib qoladi — chip "Noma'lum maktab" bo'lib chiziladi.
 */
export function useSchoolsByIdsQuery(ids: readonly string[]) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.schoolsByIds(ids),
    queryFn: async ({ signal }) => {
      const results = await Promise.allSettled(
        ids.map((id) => adminRequest<SchoolOption>(`/api/admin/schools/${id}`, { signal })),
      );
      return results.flatMap((result) => (result.status === 'fulfilled' ? [result.value] : []));
    },
    enabled: ids.length > 0,
    staleTime: 30_000,
  });
}
