import { useMutation } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';

/**
 * `POST /api/auth/logout` — docs/07-api-shartnoma.md, 2-bo'lim. Refresh tokenni bekor qiladi
 * va cookie'ni tozalaydi. Chaqiruvchi (`AdminLayout`) muvaffaqiyat/xatodan qat'i nazar
 * mahalliy sessiyani (`authStore.clear()`) tozalaydi — server bilan aloqa uzilgan bo'lsa ham
 * foydalanuvchi brauzerda chiqib ketishi kerak.
 */
export function useLogout() {
  return useMutation({
    mutationFn: () => adminRequest<void>('/api/auth/logout', { method: 'POST' }),
  });
}
