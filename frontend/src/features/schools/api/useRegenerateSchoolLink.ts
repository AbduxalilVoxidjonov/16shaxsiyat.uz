import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { RegenerateLinkResponse } from '../model/types';
import { SCHOOLS_QUERY_KEYS } from './schoolsKeys';

/**
 * `POST /api/admin/schools/{id}/regenerate-link` — docs/07, 3.1-bo'lim. **Xavfli amal**:
 * eski havola darhol ishlamay qoladi — chaqiruvchi (`RegenerateLinkDialog`) doim tasdiq
 * dialogisiz chaqirmasligi shart.
 */
export function useRegenerateSchoolLink() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      adminRequest<RegenerateLinkResponse>(`/api/admin/schools/${id}/regenerate-link`, {
        method: 'POST',
      }),
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: ['schools', 'list'] });
      void queryClient.invalidateQueries({ queryKey: SCHOOLS_QUERY_KEYS.detail(id) });
    },
  });
}
