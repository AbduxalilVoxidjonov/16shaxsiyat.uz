import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { CatalogQuestionItem, CatalogTestDetail } from '../model/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/** `GET /api/admin/catalog/tests/{id}` — batafsil (`docs/07` 3.4-bo'lim). */
export function useCatalogTestDetailQuery(id: string | null) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.detail(id ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<CatalogTestDetail>(`/api/admin/catalog/tests/${id!}`, { signal }),
    enabled: Boolean(id),
    retry: false,
  });
}

/** `GET /api/admin/catalog/tests/{id}/questions` — savollar ro'yxati (`docs/07` 3.4-bo'lim). */
export function useCatalogQuestionsQuery(id: string | null) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.questions(id ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<CatalogQuestionItem[]>(`/api/admin/catalog/tests/${id!}/questions`, { signal }),
    enabled: Boolean(id),
    retry: false,
  });
}
