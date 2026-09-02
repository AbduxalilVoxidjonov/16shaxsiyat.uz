import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { CatalogQuestionItem } from '../model/types';
import type { CreateQuestionPayload, UpdateQuestionPayload } from '../model/questionPayload';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/** Savol o'zgargach: ro'yxat (savol soni), detal va savollar keshi yangilanadi. */
function useQuestionInvalidation(testId: string) {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.questions(testId) });
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.detail(testId) });
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
    void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.scales(testId) });
  };
}

/**
 * `PUT /api/admin/catalog/questions/{id}` — tana `buildQuestionUpdatePayload` bilan quriladi.
 * Tizim savolida `scale`/`direction`/`weight` tanaga UMUMAN tushmaydi (aks holda backend
 * `409 SYSTEM_TEST_LOCKED` beradi) — `model/questionPayload.ts` izohiga qarang.
 */
export function useUpdateCatalogQuestion(testId: string) {
  const invalidate = useQuestionInvalidation(testId);
  return useMutation({
    mutationFn: ({ questionId, payload }: { questionId: string; payload: UpdateQuestionPayload }) =>
      adminRequest<CatalogQuestionItem>(`/api/admin/catalog/questions/${questionId}`, {
        method: 'PUT',
        body: payload,
      }),
    onSuccess: invalidate,
  });
}

/** `POST /api/admin/catalog/tests/{id}/questions` — faqat `Custom` (tizimda `409`). */
export function useCreateCatalogQuestion(testId: string) {
  const invalidate = useQuestionInvalidation(testId);
  return useMutation({
    mutationFn: (payload: CreateQuestionPayload) =>
      adminRequest<CatalogQuestionItem>(`/api/admin/catalog/tests/${testId}/questions`, {
        method: 'POST',
        body: payload,
      }),
    onSuccess: invalidate,
  });
}

/** `DELETE /api/admin/catalog/questions/{id}` — faqat `Custom` (tizimda `409`). */
export function useDeleteCatalogQuestion(testId: string) {
  const invalidate = useQuestionInvalidation(testId);
  return useMutation({
    mutationFn: (questionId: string) =>
      adminRequest<void>(`/api/admin/catalog/questions/${questionId}`, { method: 'DELETE' }),
    onSuccess: invalidate,
  });
}

/**
 * `POST /api/admin/catalog/tests/{id}/questions/reorder` — `[{ id, displayOrder }]`.
 * Tizim testida ham ochiq (BR-8 tartibni qulflamaydi).
 */
export function useReorderCatalogQuestions(testId: string) {
  const invalidate = useQuestionInvalidation(testId);
  return useMutation({
    mutationFn: (items: { id: string; displayOrder: number }[]) =>
      adminRequest<CatalogQuestionItem[]>(`/api/admin/catalog/tests/${testId}/questions/reorder`, {
        method: 'POST',
        body: { items },
      }),
    onSuccess: invalidate,
  });
}
