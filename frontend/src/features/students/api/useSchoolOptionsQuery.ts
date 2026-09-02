import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import { STUDENTS_QUERY_KEYS } from './studentsKeys';

/**
 * Maktab tanlash (searchable select) uchun yengil DTO — `GET /api/admin/schools`ning
 * to'liq `SchoolListItemDto`si (`features/schools/model/types.ts`) shart emas, faqat
 * nomi kerak. `features/*` bir-birini import qilmaydi (docs/10, 2-bo'lim) — shu sabab
 * `features/schools` tipi qayta ishlatilmaydi, xuddi shu endpointga alohida, minimal
 * shaklda murojaat qilinadi (CLAUDE.md topshirig'i: "P23 dagi maktablar ro'yxati
 * API'sidan foydalan" — API bir xil, feature-kod emas).
 */
export interface SchoolOption {
  id: string;
  name: string;
  region: string;
  district: string;
}

const SCHOOL_OPTIONS_PAGE_SIZE = 20;
/** Qidiruv debounce'i — CLAUDE.md "MAXSUS DIQQAT" 3 (400ms, P23 dagidek). */
export const SCHOOL_SEARCH_DEBOUNCE_MS = 400;

/** Kombobox ochilganda / qidiruv matni bo'yicha mos maktablar ro'yxati. */
export function useSchoolOptionsQuery(search: string, enabled: boolean) {
  return useQuery({
    queryKey: STUDENTS_QUERY_KEYS.schoolOptions(search),
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
 * URL'dagi `schoolId`ga mos maktab nomini oladi (chuqur havoladan ochilganda kombobox
 * bo'sh emas, "Filtr chipi" ham nom ko'rsatishi uchun — CLAUDE.md "MAXSUS DIQQAT" 2/5).
 */
export function useSchoolNameQuery(schoolId: string | null) {
  return useQuery({
    queryKey: STUDENTS_QUERY_KEYS.schoolName(schoolId ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<{ id: string; name: string }>(`/api/admin/schools/${schoolId!}`, { signal }),
    enabled: Boolean(schoolId),
    staleTime: 60_000,
  });
}
