import { useMutation } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { ChangePasswordRequest } from '../model/types';

/** `POST /api/auth/change-password` — docs/07, 2-bo'lim. */
export function useChangePassword() {
  return useMutation({
    mutationFn: (payload: ChangePasswordRequest) =>
      adminRequest<void>('/api/auth/change-password', { method: 'POST', body: payload }),
  });
}
