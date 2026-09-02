import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { SchoolDetailDto } from '../model/types';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

/**
 * `GET /api/admin/schools/{id}` — docs/07, 3.1-bo'lim. Tahrirlash drawer'i ochilganda va
 * QR modal (havolani qayta yaratmasdan) ochilganda ishlatiladi.
 */
export function useSchoolDetailQuery(id: string | null) {
  return useQuery({
    queryKey: SCHOOLS_QUERY_KEYS.detail(id ?? ''),
    queryFn: ({ signal }) => adminRequest<SchoolDetailDto>(`/api/admin/schools/${id ?? ''}`, { signal }),
    enabled: id !== null,
  });
}
