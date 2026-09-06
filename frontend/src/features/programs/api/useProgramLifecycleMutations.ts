import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { AdminProgramDetail } from '../model/types';
import { PROGRAMS_QUERY_KEYS } from './programsKeys';

/**
 * Bitta dastur ustidagi holat-o'tish (lifecycle) amallari — hammasi `{ id } -> AdminProgramDetail`
 * shaklida, shu sabab bitta umumiy fabrika bilan yaratiladi (`useToggleSchoolActive.ts`
 * naqshiga o'xshash, faqat 3 xil yo'l uchun takrorlanmasin).
 */
function useProgramAction(action: 'publish' | 'archive' | 'restore' | 'toggle-active') {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      adminRequest<AdminProgramDetail>(`/api/admin/programs/${id}/${action}`, { method: 'POST' }),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: ['programs', 'list'] });
      void queryClient.invalidateQueries({ queryKey: PROGRAMS_QUERY_KEYS.detail(id) });
    },
  });
}

/** `POST /api/admin/programs/{id}/publish` — kamida bitta test bo'lmasa `400 PROGRAM_NOT_PUBLISHABLE`. */
export function usePublishProgram() {
  return useProgramAction('publish');
}

/** `POST /api/admin/programs/{id}/archive`. */
export function useArchiveProgram() {
  return useProgramAction('archive');
}

/**
 * `POST /api/admin/programs/{id}/restore` — `Archived ──▶ Paused` (2026-09-06). Natija
 * `Paused`, `Active` EMAS: arxiv maktab biriktirishlarini saqlab qolgan, shu sabab tiklash
 * dasturni darhol jonli qilmaydi — keyin admin "Faollashtirish"ni alohida bosadi.
 */
export function useRestoreProgram() {
  return useProgramAction('restore');
}

/** `POST /api/admin/programs/{id}/toggle-active`. */
export function useToggleProgramActive() {
  return useProgramAction('toggle-active');
}
