import { useMutation } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type {
  TotpConfirmRequest,
  TotpConfirmResponse,
  TotpDisableRequest,
  TotpEnableResponse,
} from '../model/types';

/**
 * `POST /api/auth/totp/enable` — docs/07, 2-bo'lim. O'rnatishning 1-bosqichi: sir KUTISH
 * holatida saqlanadi va QR kod qaytadi. **2FA hali yoqilmaydi** — buning uchun
 * `useConfirmTotp` chaqirilishi shart.
 */
export function useEnableTotp() {
  return useMutation({
    mutationFn: () => adminRequest<TotpEnableResponse>('/api/auth/totp/enable', { method: 'POST' }),
  });
}

/**
 * `POST /api/auth/totp/confirm` — docs/07, 2-bo'lim. O'rnatishning 2-bosqichi: ilovadagi
 * 6 xonali kod tekshiriladi, 2FA yoqiladi va zaxira kodlar bir marta qaytariladi.
 */
export function useConfirmTotp() {
  return useMutation({
    mutationFn: (payload: TotpConfirmRequest) =>
      adminRequest<TotpConfirmResponse>('/api/auth/totp/confirm', { method: 'POST', body: payload }),
  });
}

/** `POST /api/auth/totp/disable` — docs/07, 2-bo'lim. Parol bilan tasdiqlash talab qilinadi. */
export function useDisableTotp() {
  return useMutation({
    mutationFn: (payload: TotpDisableRequest) =>
      adminRequest<void>('/api/auth/totp/disable', { method: 'POST', body: payload }),
  });
}
