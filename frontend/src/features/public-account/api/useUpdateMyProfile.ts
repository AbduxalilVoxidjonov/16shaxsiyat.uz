import { useMutation } from '@tanstack/react-query';
import { publicUserRequest } from '@/shared/api/publicUserClient';
import type { MyStudentProfile, UpdateStudentProfileRequestBody } from '../model/types';

/**
 * `PUT /api/me/profile` — anketani FAQAT saqlash (`docs/07` §5.1b). `POST /api/me/sessions`
 * dan farqi: sessiya OCHILMAYDI — kabinetdagi "O'zgartirish" shu yerga boradi.
 * Javob — yangilangan `MyStudentProfile` (`GET /api/me/profile` bilan bir shakl); chaqiruvchi
 * uni `MY_PROFILE_QUERY_KEY` keshiga to'g'ridan-to'g'ri yozadi (`setQueryData`).
 */
export function useUpdateMyProfile() {
  return useMutation({
    mutationFn: (payload: UpdateStudentProfileRequestBody) =>
      publicUserRequest<MyStudentProfile>('/api/me/profile', {
        method: 'PUT',
        body: payload,
      }),
  });
}
