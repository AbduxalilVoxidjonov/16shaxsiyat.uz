import { useMutation } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api/client';
import type { LoginRequest, LoginResponse } from '../model/types';

/**
 * `POST /api/auth/login` — docs/07-api-shartnoma.md, 2-bo'lim.
 *
 * Ataylab `adminClient.adminRequest` EMAS, bazaviy `apiRequest` orqali chaqiriladi:
 * noto'g'ri login/parolda backend `401` qaytaradi, `adminRequest` esa har qanday `401`ni
 * "token eskirgan" deb talqin qilib avtomatik `refresh` chaqirar edi — bu login xatosi uchun
 * noto'g'ri va ortiqcha so'rov. `credentials: 'include'` shart — muvaffaqiyatli javobda
 * backend refresh tokenni `httpOnly` cookie sifatida qaytaradi (docs/08, 2-bo'lim).
 */
export function useLogin() {
  return useMutation({
    mutationFn: (payload: LoginRequest) =>
      apiRequest<LoginResponse>('/api/auth/login', {
        method: 'POST',
        body: payload,
        credentials: 'include',
      }),
  });
}
