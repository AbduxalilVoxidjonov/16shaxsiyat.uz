import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { RegistrationFormDefinition } from '@/shared/api/registrationFormSettingsTypes';
import { SETTINGS_QUERY_KEYS } from './settingsKeys';

/**
 * `GET /api/admin/settings/registration-form` — `docs/07` §3.8. Sozlama hali yaratilmagan
 * bo'lsa ham backend `404` EMAS, STANDART qiymatni `200` bilan qaytaradi — shu sabab bu yerda
 * alohida "bo'sh holat" yo'q, faqat yuklanish/xato.
 */
export function useRegistrationFormSettingsQuery() {
  return useQuery({
    queryKey: SETTINGS_QUERY_KEYS.registrationForm(),
    queryFn: ({ signal }) =>
      adminRequest<RegistrationFormDefinition>('/api/admin/settings/registration-form', { signal }),
    staleTime: 0,
  });
}

/**
 * `PUT /api/admin/settings/registration-form` — TO'LIQ almashtirish (`docs/07` §3.8).
 * Muvaffaqiyatli bo'lsa keshni serverdan qaytgan qiymat bilan darhol yangilaydi (qayta
 * so'rov shart emas — javob shakli `GET` bilan bir xil).
 */
export function useUpdateRegistrationFormSettings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (definition: RegistrationFormDefinition) =>
      adminRequest<RegistrationFormDefinition>('/api/admin/settings/registration-form', {
        method: 'PUT',
        body: definition,
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(SETTINGS_QUERY_KEYS.registrationForm(), data);
    },
  });
}
