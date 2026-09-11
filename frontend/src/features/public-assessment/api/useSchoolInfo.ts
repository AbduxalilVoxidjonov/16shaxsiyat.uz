import { useQuery } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { PublicSchoolInfoWithRegistration } from '@/shared/api/registrationModeTypes';

/**
 * `GET /api/public/schools/{slug}?k={accessToken}` — docs/07-api-shartnoma.md, 1.1-bo'lim.
 * `404`/`410` domen javoblari — qayta urinish foydasiz, shu sabab `retry: false`.
 *
 * Javob tipi `PublicSchoolInfoWithRegistration` (P52, 2026-09-11) — `programs[].registrationMode`
 * va `programs[].tests` bilan kengaytirilgan, `schema.d.ts` hali eskirgan
 * (`shared/api/registrationModeTypes.ts`dagi izohga qarang).
 */
export function useSchoolInfo(slug: string, accessToken: string) {
  return useQuery({
    queryKey: QUERY_KEYS.publicSchoolInfo(slug, accessToken),
    queryFn: ({ signal }) =>
      publicRequest<PublicSchoolInfoWithRegistration>(
        `/api/public/schools/${encodeURIComponent(slug)}?k=${encodeURIComponent(accessToken)}`,
        { signal },
      ),
    enabled: slug.length > 0,
    retry: false,
  });
}
