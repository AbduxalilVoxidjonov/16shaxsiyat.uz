import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AdminSection, AdminSectionPayload } from '@/shared/api/branchingTypes';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/**
 * Bo'limlar CRUD — `docs/18` §5 (Admin API). Faqat `Custom` testlarda ochiq; tizim
 * metodikasida backend `409 SYSTEM_TEST_LOCKED` qaytaradi (`docs/18` B-3).
 */
export function useCatalogSectionsQuery(testId: string | null) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.sections(testId ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<AdminSection[]>(`/api/admin/catalog/tests/${testId!}/sections`, { signal }),
    enabled: Boolean(testId),
    retry: false,
  });
}

/** Bo'lim o'zgargach: bo'limlar, savollar (`sectionId` ko'rsatish uchun) va detal keshi yangilanadi. */
function useSectionInvalidation(testId: string) {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.sections(testId) });
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.questions(testId) });
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.detail(testId) });
  };
}

/** `POST /api/admin/catalog/tests/{id}/sections`. */
export function useCreateCatalogSection(testId: string) {
  const invalidate = useSectionInvalidation(testId);
  return useMutation({
    mutationFn: (payload: AdminSectionPayload) =>
      adminRequest<AdminSection>(`/api/admin/catalog/tests/${testId}/sections`, {
        method: 'POST',
        body: payload,
      }),
    onSuccess: invalidate,
  });
}

/** `PUT /api/admin/catalog/sections/{sectionId}`. */
export function useUpdateCatalogSection(testId: string) {
  const invalidate = useSectionInvalidation(testId);
  return useMutation({
    mutationFn: ({ sectionId, payload }: { sectionId: string; payload: AdminSectionPayload }) =>
      adminRequest<AdminSection>(`/api/admin/catalog/sections/${sectionId}`, {
        method: 'PUT',
        body: payload,
      }),
    onSuccess: invalidate,
  });
}

/** `DELETE /api/admin/catalog/sections/{sectionId}` — savollari bo'lsa `409 SECTION_IN_USE`. */
export function useDeleteCatalogSection(testId: string) {
  const invalidate = useSectionInvalidation(testId);
  return useMutation({
    mutationFn: (sectionId: string) =>
      adminRequest<void>(`/api/admin/catalog/sections/${sectionId}`, { method: 'DELETE' }),
    onSuccess: invalidate,
  });
}

/** `POST /api/admin/catalog/tests/{id}/sections/reorder` — `[{ id, displayOrder }]`. */
export function useReorderCatalogSections(testId: string) {
  const invalidate = useSectionInvalidation(testId);
  return useMutation({
    mutationFn: (items: { id: string; displayOrder: number }[]) =>
      adminRequest<AdminSection[]>(`/api/admin/catalog/tests/${testId}/sections/reorder`, {
        method: 'POST',
        body: { items },
      }),
    onSuccess: invalidate,
  });
}
