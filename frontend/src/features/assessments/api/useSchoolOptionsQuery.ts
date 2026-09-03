import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import { ASSESSMENTS_QUERY_KEYS } from './assessmentsKeys';

/**
 * Maktab filtri uchun yengil DTO. `features/*` bir-birini import qilmaydi (`docs/10`
 * 2-bo'lim), shu sabab `features/students` va `features/programs` dagi kabi shu endpointga
 * mustaqil, minimal shaklda murojaat qilinadi.
 */
export interface SchoolOption {
  id: string;
  name: string;
}

/** Filtr ro'yxati — birinchi 100 maktab, nom bo'yicha (`pageSize` chegarasi `docs/07` §4). */
const SCHOOL_OPTIONS_PAGE_SIZE = 100;

export function useSchoolOptionsQuery() {
  return useQuery({
    queryKey: ASSESSMENTS_QUERY_KEYS.schoolOptions(),
    queryFn: ({ signal }) =>
      adminRequest<PagedResult<SchoolOption>>(
        `/api/admin/schools?page=1&pageSize=${String(SCHOOL_OPTIONS_PAGE_SIZE)}&sort=name`,
        { signal },
      ),
    staleTime: 5 * 60_000,
  });
}
