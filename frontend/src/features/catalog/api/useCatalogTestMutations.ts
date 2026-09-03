import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { CatalogTestDetail, TestScoringMode } from '../model/types';
import { CATALOG_QUERY_KEYS } from './catalogKeys';

/** `POST /api/admin/catalog/tests` so'rov tanasi (`CreateCatalogTestRequest`). */
export interface CreateCatalogTestPayload {
  code: string;
  nameUz: string;
  descriptionUz: string | null;
  estimatedMinutes: number;
  pageSize: number;
  scoringMode: TestScoringMode;
}

/**
 * `POST /api/admin/catalog/tests` — `docs/07` §3.4: "Yangi `Custom` anketa — `Draft`
 * holatida yaratiladi". Yuklashsiz, katalog sahifasining o'zida yaratish yo'li: backendda
 * to'liq CRUD (P37) va tahrirlash sahifasi (P38) allaqachon bor edi, faqat tugmasi yo'q edi.
 * Band kod uchun backend `409` beradi (`ux_test_definitions_code`).
 */
export function useCreateCatalogTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateCatalogTestPayload) =>
      adminRequest<CatalogTestDetail>('/api/admin/catalog/tests', {
        method: 'POST',
        body: payload,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
    },
  });
}

/** `PUT /api/admin/catalog/tests/{id}` so'rov tanasi (`UpdateCatalogTestRequest`). */
export interface UpdateCatalogTestPayload {
  nameUz: string;
  descriptionUz: string | null;
  displayOrder: number;
  estimatedMinutes: number;
  shuffleQuestions: boolean;
  pageSize: number;
}

/**
 * `PUT /api/admin/catalog/tests/{id}` — `docs/07` 3.4-bo'lim: "Nom, tavsif, tartib,
 * `pageSize`, `shuffleQuestions`". Backend bu yerda `IsSystem` ni TEKSHIRMAYDI — tizim
 * metodikasining meta ma'lumotlari ham tahrirlanadi (qulflangani faqat savol/shkala tarkibi).
 */
export function useUpdateCatalogTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateCatalogTestPayload }) =>
      adminRequest<CatalogTestDetail>(`/api/admin/catalog/tests/${id}`, {
        method: 'PUT',
        body: payload,
      }),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.detail(variables.id) });
    },
  });
}

/**
 * `POST /api/admin/catalog/tests/{id}/duplicate` — nusxa doim `Draft`/`Custom`. Tizim
 * metodikasini "tahrirlashning" xavfsiz yo'li: nusxa olinadi va nusxada hammasi ochiq.
 */
export function useDuplicateCatalogTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, newCode }: { id: string; newCode: string }) =>
      adminRequest<CatalogTestDetail>(`/api/admin/catalog/tests/${id}/duplicate`, {
        method: 'POST',
        body: { newCode },
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
    },
  });
}

/** `DELETE /api/admin/catalog/tests/{id}` — faqat `Custom` + hech qaysi sessiyada ishlatilmagan. */
export function useDeleteCatalogTest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      adminRequest<void>(`/api/admin/catalog/tests/${id}`, { method: 'DELETE' }),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: CATALOG_QUERY_KEYS.list() });
      void queryClient.removeQueries({ queryKey: CATALOG_QUERY_KEYS.detail(id) });
    },
  });
}
