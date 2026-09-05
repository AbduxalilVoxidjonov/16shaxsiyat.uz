import { useMutation } from '@tanstack/react-query';
import { publicUserRequest } from '@/shared/api/publicUserClient';

/**
 * `DELETE /api/me` — `docs/07` §5.5. Har doim `204`, idempotent.
 *
 * Akkaunt **anonimlashtiriladi** (yozuvning o'zi arxiv va FK butunligi uchun qoladi), barcha
 * refresh tokenlar bekor qilinadi. Shu Telegram akkaunti bilan qayta kirilsa **YANGI**
 * akkaunt ochiladi — foydalanuvchi bu haqda tasdiq oynasida ogohlantiriladi.
 */
export function useDeleteMyAccount() {
  return useMutation({
    mutationFn: () => publicUserRequest<void>('/api/me', { method: 'DELETE' }),
  });
}
