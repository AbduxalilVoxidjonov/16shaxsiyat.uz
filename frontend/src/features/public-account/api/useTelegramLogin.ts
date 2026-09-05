import { useMutation } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api/client';
import type { TelegramLoginRequestBody, TelegramLoginResponse } from '@/shared/api/types';

/**
 * `POST /api/auth/telegram` — `docs/07` §2a.1.
 *
 * So'rov tanasi Telegram Login Widget bergan obyektning **o'zi**: kalitlar `snake_case`
 * (`first_name`, `auth_date`, `photo_url`…), chunki `hash` imzosi aynan shu nomlardan
 * hisoblangan. Telegram bermagan maydon (masalan `username`) umuman YUBORILMAYDI —
 * bo'sh satr qo'shilsa imzo mos kelmay `401 TELEGRAM_AUTH_INVALID` qaytadi. Shu sabab
 * bu yerda obyekt qayta yig'ilmaydi va normalizatsiya qilinmaydi.
 *
 * `adminClient`/`publicUserClient` EMAS, to'g'ridan-to'g'ri `apiRequest`: bu so'rovda hali
 * hech qanday token yo'q. `credentials: 'include'` SHART — javob refresh tokenni
 * `Set-Cookie` bilan qaytaradi (`Path=/api/auth/telegram`).
 */
export function useTelegramLogin() {
  return useMutation({
    mutationFn: (payload: TelegramLoginRequestBody) =>
      apiRequest<TelegramLoginResponse>('/api/auth/telegram', {
        method: 'POST',
        body: payload,
        credentials: 'include',
      }),
  });
}
