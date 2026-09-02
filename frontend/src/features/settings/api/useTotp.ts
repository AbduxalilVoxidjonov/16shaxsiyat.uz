import { useMutation } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { TotpDisableRequest, TotpEnableResponse } from '../model/types';

/** `POST /api/auth/totp/enable` — docs/07, 2-bo'lim. */
export function useEnableTotp() {
  return useMutation({
    mutationFn: () => adminRequest<TotpEnableResponse>('/api/auth/totp/enable', { method: 'POST' }),
  });
}

/** `POST /api/auth/totp/disable` — docs/07, 2-bo'lim. Parol bilan tasdiqlash talab qilinadi. */
export function useDisableTotp() {
  return useMutation({
    mutationFn: (payload: TotpDisableRequest) =>
      adminRequest<void>('/api/auth/totp/disable', { method: 'POST', body: payload }),
  });
}
