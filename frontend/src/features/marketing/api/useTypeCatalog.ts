import { useQuery } from '@tanstack/react-query';
import { apiRequest } from '@/shared/api/client';
import { QUERY_KEYS } from '@/shared/config/queryKeys';
import type { PublicTypeCatalog, PublicTypeCatalogItem } from '@/shared/api/types';

/** `docs/07` 1.10-bo'lim: bo'sh javob ham qonuniy (seed qilinmagan baza) — mijoz "bo'sh holat" ko'rsatadi. */
const EMPTY_TYPES: PublicTypeCatalogItem[] = [];

/**
 * `GET /api/public/type-catalog` — docs/07-api-shartnoma.md, 1.10-bo'lim.
 *
 * `publicRequest` EMAS, balki bevosita `apiRequest`: bu endpoint sessiyaga umuman bog'liq
 * emas va `X-Session-Token` talab qilmaydi. `publicRequest` mavjud o'quvchi sessiyasi bo'lsa
 * tokenni qo'shib yuborardi — ochiq marketing sahifasidan keraksiz token yuborilmasligi uchun
 * ataylab quruq mijoz ishlatiladi.
 *
 * Kontent deyarli o'zgarmaydi (faqat seed bilan), backend uni bir soatga keshlaydi — shu
 * sabab mijozda ham uzun `staleTime`: `/metodika` ↔ `/metodika/:kod` orasida yurganda
 * takroriy so'rov yubormaydi.
 */
export function useTypeCatalog() {
  return useQuery({
    queryKey: QUERY_KEYS.publicTypeCatalog(),
    queryFn: ({ signal }) => apiRequest<PublicTypeCatalog>('/api/public/type-catalog', { signal }),
    staleTime: 60 * 60 * 1000,
  });
}

/**
 * Katalogdan bitta tipni kod bo'yicha topadi (`/metodika/:kod`). Kod registrga sezgir emas —
 * URL da kichik harf (`/metodika/intj`), katalogda esa katta harf (`INTJ`).
 *
 * `types` — barcha tiplar (kod bo'yicha barqaror tartibda, backend shunday qaytaradi):
 * "oldingi/keyingi tip" navigatsiyasi shu ro'yxatga tayanadi.
 */
export function useTypeCatalogEntry(code: string | undefined) {
  const query = useTypeCatalog();
  const types = query.data?.types ?? EMPTY_TYPES;
  const normalized = (code ?? '').trim().toUpperCase();
  const index = types.findIndex((type) => type.code === normalized);

  return {
    ...query,
    types,
    type: index >= 0 ? types[index] : undefined,
    previous: index > 0 ? types[index - 1] : undefined,
    next: index >= 0 && index < types.length - 1 ? types[index + 1] : undefined,
    /** Ma'lumot kelgan, lekin bunday kod yo'q — 404 ("topilmadi") holati. */
    notFound: query.isSuccess && index < 0,
  };
}
