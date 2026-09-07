import { useQuery } from '@tanstack/react-query';
import { publicUserRequest } from '@/shared/api/publicUserClient';
import type { MyStudentProfile } from '../model/types';

/**
 * `['public-user', ...]` prefiksi — `usePublicLogout` chiqishda shu prefiks bo'yicha barcha
 * kabinet so'rovlarini tozalaydi; profil ham shaxsiy ma'lumot, u ham tozalanishi shart.
 * `shared/config/queryKeys.ts` ga qo'shilmadi — kalit faqat shu feature ichida ishlatiladi
 * (`features/public-space/api/publicSpaceKeys.ts` bilan bir xil yondashuv).
 */
export const MY_PROFILE_QUERY_KEY = ['public-user', 'profile'] as const;

/**
 * `GET /api/me/profile` — saqlangan anketa (`docs/07` §5.1a). Profil yo'q bo'lsa ham `200`
 * (`hasProfile: false` + Telegram ismidan F.I.Sh. taklifi), shu sabab `404` ishlovi yo'q.
 * Sessiya ochilgach (`POST /api/me/sessions`) profil yaratilgan/tahrirlangan bo'lishi
 * mumkin — chaqiruvchi `MY_PROFILE_QUERY_KEY` ni invalidatsiya qiladi.
 */
export function useMyProfile(enabled = true) {
  return useQuery({
    queryKey: MY_PROFILE_QUERY_KEY,
    queryFn: ({ signal }) => publicUserRequest<MyStudentProfile>('/api/me/profile', { signal }),
    enabled,
  });
}
