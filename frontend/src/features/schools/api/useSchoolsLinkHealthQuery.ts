import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { SchoolsLinkHealthDto } from '../model/types';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

/**
 * `GET /api/admin/schools/link-health` — `docs/07` 3.1 (2026-09-03).
 *
 * Butun tizim bo'yicha "nechta maktab havolasi ishlamaydi". Hisob BACKENDDA, ommaviy handler
 * bilan bitta mezondan — frontend uni qayta ixtiro qilmaydi va N+1 so'rov yubormaydi (ilgari
 * bu ma'lumot faqat har dastur uchun alohida so'rov bilan taxminan hisoblanardi).
 */
export function useSchoolsLinkHealthQuery() {
  return useQuery({
    queryKey: SCHOOLS_QUERY_KEYS.linkHealth(),
    queryFn: ({ signal }) =>
      adminRequest<SchoolsLinkHealthDto>('/api/admin/schools/link-health', { signal }),
    staleTime: 30_000,
  });
}
