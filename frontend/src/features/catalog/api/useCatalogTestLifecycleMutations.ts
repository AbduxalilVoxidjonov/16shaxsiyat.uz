import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { CatalogTestDetail } from '../model/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/** `docs/07` 3.4-bo'lim: `publish`/`toggle-active`/`archive` — hammasi `{ id } -> CatalogTestDetail`. */
function useCatalogTestAction(action: 'publish' | 'toggle-active' | 'archive') {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      adminRequest<CatalogTestDetail>(`/api/admin/catalog/tests/${id}/${action}`, {
        method: 'POST',
      }),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.detail(id) });
    },
  });
}

/** `POST /api/admin/catalog/tests/{id}/publish` — validatsiya yiqilsa `400` (`issues[]`). */
export function usePublishCatalogTest() {
  return useCatalogTestAction('publish');
}

/** `POST /api/admin/catalog/tests/{id}/toggle-active` — yangi sessiyalarga qo'shilish (BR-10). */
export function useToggleCatalogTestActive() {
  return useCatalogTestAction('toggle-active');
}

/** `POST /api/admin/catalog/tests/{id}/archive` — `Custom` va ishlatilgan bo'lsa (BR-11). */
export function useArchiveCatalogTest() {
  return useCatalogTestAction('archive');
}
