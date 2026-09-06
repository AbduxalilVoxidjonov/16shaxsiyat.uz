import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import type { StudentListItemDto, StudentsListQuery } from '../model/types';
import { STUDENTS_QUERY_KEYS } from './studentsKeys';

/** Ro'yxatlar uchun `staleTime` — docs/10, 5.2-bo'lim: "ro'yxatlar 30s". */
const LIST_STALE_TIME_MS = 30_000;

/**
 * `GET /api/admin/students` uchun query-string quradi — docs/07, 3.2-bo'lim.
 * Eksport (`useExportStudentsMutation.ts`) ham joriy filtrni aynan shu funksiya bilan
 * kodlaydi, shu sabab bu yerda eksport qilingan (CLAUDE.md "MAXSUS DIQQAT" 3: server-side
 * filtr — bitta haqiqat manbai, ikkita joyda takrorlanmasin).
 */
export function buildStudentsQueryString(query: StudentsListQuery): string {
  const params = new URLSearchParams();
  // Manba filtri (P48) — eksport ham AYNAN shu funksiyadan o'tadi, shu sabab ro'yxatda
  // ko'rilgan kesim bilan eksport qilingan fayl hech qachon farq qilmaydi.
  if (query.source) params.set('source', query.source);
  if (query.schoolId) params.set('schoolId', query.schoolId);
  if (query.grade !== undefined) params.set('grade', String(query.grade));
  if (query.status) params.set('status', query.status);
  if (query.needsAttention) params.set('needsAttention', 'true');
  if (query.personalityType) params.set('personalityType', query.personalityType);
  if (query.activityLevel) params.set('activityLevel', query.activityLevel);
  if (query.from) params.set('from', query.from);
  if (query.to) params.set('to', query.to);
  if (query.search) params.set('search', query.search);
  params.set('page', String(query.page));
  params.set('pageSize', String(query.pageSize));
  if (query.sort) params.set('sort', query.sort);
  return params.toString();
}

/**
 * `GET /api/admin/students` — docs/07, 3.2-bo'lim. Server-side sahifalash/saralash/filtrlash
 * — CLAUDE.md "MAXSUS DIQQAT" 1: butun jadvalni bir marta yuklash TAQIQLANADI, shu sabab
 * `pageSize` chaqiruvchida (`StudentsPage.tsx`) ≤ 100 bilan cheklanadi.
 */
export function useStudentsQuery(query: StudentsListQuery) {
  return useQuery({
    queryKey: STUDENTS_QUERY_KEYS.list(query),
    queryFn: ({ signal }) =>
      adminRequest<PagedResult<StudentListItemDto>>(
        `/api/admin/students?${buildStudentsQueryString(query)}`,
        { signal },
      ),
    staleTime: LIST_STALE_TIME_MS,
    placeholderData: (previous) => previous,
  });
}
