import { useMutation } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import type { StartTestResponse } from '@/shared/api/types';

/**
 * `POST /sessions/tests/{testCode}/start` — docs/07-api-shartnoma.md, 1.4-bo'lim.
 * Aralashtirish tartibini qat'iylashtiradi. Shartnomada idempotentlik aniq yozilmagan, lekin
 * boshqa ommaviy `POST`lar ataylab idempotent qilingan (`docs/07` 4-bo'lim: `/sessions`,
 * `/answers`) — shu naqshga tayanib `TestPage` bu mutatsiyani sahifa har mount bo'lganda
 * (masalan yangilanganda) qayta chaqiradi. P12 tayyor bo'lgach tasdiqlanishi kerak — hisobotga
 * qarang.
 */
export function useStartTest() {
  return useMutation({
    mutationFn: (testCode: string) =>
      publicRequest<StartTestResponse>(
        `/api/public/sessions/tests/${encodeURIComponent(testCode)}/start`,
        { method: 'POST' },
      ),
  });
}
