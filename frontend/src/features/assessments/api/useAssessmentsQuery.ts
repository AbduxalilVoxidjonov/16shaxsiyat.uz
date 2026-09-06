import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { PagedResult } from '@/shared/api/types';
import type { AssessmentListItemDto, AssessmentsListQuery } from '../model/types';
import { ASSESSMENTS_QUERY_KEYS } from './assessmentsKeys';

/** Ro'yxatlar uchun `staleTime` — `docs/10`, 5.2-bo'lim: "ro'yxatlar 30s". */
const LIST_STALE_TIME_MS = 30_000;

/**
 * `from`/`to` — backendda `Assessment.StartedAt` bo'yicha `>=` va `<=` (`ListAssessmentsQueryHandler`).
 * Foydalanuvchi SANA tanlaydi, shu sabab `to` KUN OXIRIGACHA kengaytiriladi: aks holda
 * "30.08 gacha" filtri 30-avgust kuni boshlangan sessiyalarni butunlay tashlab yuborardi
 * (`2026-08-30` → `2026-08-30T00:00:00Z`). Vaqt mintaqasi — UTC, chunki sahifadagi barcha
 * vaqtlar ham UTC ko'rsatiladi (`formatDateTime`, `docs/07` 4-bo'lim).
 */
function toRangeStart(date: string): string {
  return `${date}T00:00:00.000Z`;
}

function toRangeEnd(date: string): string {
  return `${date}T23:59:59.999Z`;
}

export function buildAssessmentsQueryString(query: AssessmentsListQuery): string {
  const params = new URLSearchParams();
  // Manba filtri (P48) — maktab va ommaviy sessiyalarni ajratish uchun.
  if (query.source) params.set('source', query.source);
  if (query.schoolId) params.set('schoolId', query.schoolId);
  if (query.status) params.set('status', query.status);
  if (query.from) params.set('from', toRangeStart(query.from));
  if (query.to) params.set('to', toRangeEnd(query.to));
  params.set('page', String(query.page));
  params.set('pageSize', String(query.pageSize));
  if (query.sort) params.set('sort', query.sort);
  return params.toString();
}

/**
 * `GET /api/admin/assessments` — `docs/07` 3.3-bo'lim. Sahifalash/saralash/filtrlash
 * SERVER tomonda (`CLAUDE.md` "MAXSUS DIQQAT" 1: butun jadvalni bir marta yuklash taqiqlanadi).
 * `placeholderData` — sahifa almashganda jadval "sakramaydi".
 */
export function useAssessmentsQuery(query: AssessmentsListQuery) {
  return useQuery({
    queryKey: ASSESSMENTS_QUERY_KEYS.list(query),
    queryFn: ({ signal }) =>
      adminRequest<PagedResult<AssessmentListItemDto>>(
        `/api/admin/assessments?${buildAssessmentsQueryString(query)}`,
        { signal },
      ),
    staleTime: LIST_STALE_TIME_MS,
    placeholderData: (previous) => previous,
  });
}
