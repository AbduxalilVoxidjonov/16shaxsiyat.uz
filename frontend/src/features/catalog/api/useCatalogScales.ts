import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { CatalogScaleItem, InterpretationBand } from '../model/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/** `GET /api/admin/catalog/tests/{id}/scales` — tizim testida odatda bo'sh massiv. */
export function useCatalogScalesQuery(id: string | null, enabled = true) {
  return useQuery({
    queryKey: CATALOG_QUERY_KEYS.scales(id ?? ''),
    queryFn: ({ signal }) =>
      adminRequest<CatalogScaleItem[]>(`/api/admin/catalog/tests/${id!}/scales`, { signal }),
    enabled: enabled && Boolean(id),
    retry: false,
  });
}

/** `POST`/`PUT` shkala so'rov tanasi (`CreateTestScaleRequest`/`UpdateTestScaleRequest`). */
export interface ScalePayload {
  code?: string;
  nameUz: string;
  descriptionUz: string | null;
  displayOrder: number;
  /**
   * Backend `UpdateTestScaleCommandHandler` bandlarni `request.InterpretationBands ?? []`
   * bilan TO'LIQ ALMASHTIRADI — yubormaslik ularni O'CHIRADI. Shu sabab `ScaleDialog` har
   * doim TO'LIQ ro'yxatni (muharrirdagi joriy holatni) yuboradi, "faqat o'zgarganini" emas.
   */
  interpretationBands: InterpretationBand[];
}

function useScaleInvalidation(testId: string) {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.scales(testId) });
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.detail(testId) });
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
  };
}

/** `POST /api/admin/catalog/tests/{id}/scales` — faqat `Custom` (tizimda `409`). */
export function useCreateCatalogScale(testId: string) {
  const invalidate = useScaleInvalidation(testId);
  return useMutation({
    mutationFn: (payload: ScalePayload) =>
      adminRequest<CatalogScaleItem>(`/api/admin/catalog/tests/${testId}/scales`, {
        method: 'POST',
        body: payload,
      }),
    onSuccess: invalidate,
  });
}

/** `PUT /api/admin/catalog/scales/{scaleId}`. */
export function useUpdateCatalogScale(testId: string) {
  const invalidate = useScaleInvalidation(testId);
  return useMutation({
    mutationFn: ({ scaleId, payload }: { scaleId: string; payload: ScalePayload }) =>
      adminRequest<CatalogScaleItem>(`/api/admin/catalog/scales/${scaleId}`, {
        method: 'PUT',
        body: payload,
      }),
    onSuccess: invalidate,
  });
}

/** `DELETE /api/admin/catalog/scales/{scaleId}` — savollari bo'lsa `409 SCALE_IN_USE`. */
export function useDeleteCatalogScale(testId: string) {
  const invalidate = useScaleInvalidation(testId);
  return useMutation({
    mutationFn: (scaleId: string) =>
      adminRequest<void>(`/api/admin/catalog/scales/${scaleId}`, { method: 'DELETE' }),
    onSuccess: invalidate,
  });
}
