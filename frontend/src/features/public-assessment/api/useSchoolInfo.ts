import { useQuery } from '@tanstack/react-query';
import { publicRequest } from '@/shared/api/publicClient';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { PublicSchoolInfo } from '@/shared/api/types';

/**
 * `GET /api/public/schools/{slug}?k={accessToken}` — docs/07-api-shartnoma.md, 1.1-bo'lim.
 * `404`/`410` domen javoblari — qayta urinish foydasiz, shu sabab `retry: false`.
 */
export function useSchoolInfo(slug: string, accessToken: string) {
  return useQuery({
    queryKey: QUERY_KEYS.publicSchoolInfo(slug, accessToken),
    queryFn: ({ signal }) =>
      publicRequest<PublicSchoolInfo>(
        `/api/public/schools/${encodeURIComponent(slug)}?k=${encodeURIComponent(accessToken)}`,
        { signal },
      ),
    enabled: slug.length > 0,
    retry: false,
  });
}
