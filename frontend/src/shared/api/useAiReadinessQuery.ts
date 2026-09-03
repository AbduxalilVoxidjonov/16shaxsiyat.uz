import { useQuery } from '@tanstack/react-query';
import { adminRequest } from '@/shared/api/adminClient';
import type { components } from '@/shared/api/schema';

/**
 * "AI provayder umuman sozlanganmi" tekshiruvi — tahlil tugmasi yonida OLDINDAN
 * ogohlantirish uchun.
 *
 * **Nega kerak.** Kalit kiritilmagan bo'lsa tahlil barcha provayderda yiqiladi va
 * `AnalysisOrchestrator` SHABLON hisobot yozadi (`isFallbackReport`, `docs/09` 11-bo'lim).
 * Admin tugmani bosib, bir daqiqa kutib, sababsiz shablon matn olishi — yomon UX. Sabab
 * bosishdan OLDIN aytiladi; bosilgandan keyin esa mavjud `fallbackBadge` banneri baribir
 * tushuntiradi (u olib tashlanmagan).
 *
 * **Nega `shared/api` da.** Ikkala ekran ham (`features/students`, `features/assessments`)
 * shu ma'lumotni so'raydi, `features/*` esa bir-birini import qilmaydi (`docs/10` 2-bo'lim).
 *
 * **Kalit `features/ai-settings/api/aiSettingsKeys.ts` dagi bilan ATAYLAB bir xil** —
 * `/admin/ai` sahifasidan kelingan bo'lsa javob keshdan olinadi va qo'shimcha so'rov
 * ketmaydi. Bog'liqlik yumshoq: ai-settings kalitini o'zgartirsa eng yomon oqibat — bitta
 * ortiqcha `GET`, xato emas.
 */
const AI_PROVIDERS_QUERY_KEY = ['ai-settings', 'providers'] as const;

/** Sozlamalar tez-tez o'zgarmaydi — `docs/10` 5.2: ro'yxatlar uchun 30 s. */
const READINESS_STALE_TIME_MS = 30_000;

type AiProviderDto = components['schemas']['AdminAiProviderDto'];

/**
 * `GET /api/admin/ai/providers` (`docs/07` 3.5) FAQAT DB'da mavjud (kamida bir marta
 * saqlangan) provayderlarni qaytaradi. "Tayyor" deb faol VA kaliti kiritilgan provayder
 * hisoblanadi: `maskedApiKey` kalit yo'q bo'lsa `null` keladi (`ApiKeyMasker`, to'liq kalit
 * hech qachon yuborilmaydi — `docs/08` 3-bo'lim).
 */
function hasReadyProvider(providers: AiProviderDto[]): boolean {
  return providers.some(
    (provider) => provider.isActive && (provider.maskedApiKey ?? '').length > 0,
  );
}

export interface AiReadiness {
  /**
   * `true` — kamida bitta faol, kaliti bor provayder mavjud;
   * `false` — birortasi ham yo'q (tahlil o'rniga shablon hisobot chiqadi);
   * `null` — NOMA'LUM (so'rov hali tugamagan yoki xato bilan tugadi). Noma'lumda hech narsa
   * da'vo qilinmaydi — ogohlantirish ham, "hammasi joyida" degan xabar ham ko'rsatilmaydi.
   */
  hasConfiguredProvider: boolean | null;
}

/**
 * @param enabled Faqat tahlil tugmasi ko'rinadigan ekranlarda so'raladi — ortiqcha so'rov yo'q.
 */
export function useAiReadinessQuery(enabled: boolean): AiReadiness {
  const query = useQuery({
    queryKey: AI_PROVIDERS_QUERY_KEY,
    queryFn: ({ signal }) => adminRequest<AiProviderDto[]>('/api/admin/ai/providers', { signal }),
    enabled,
    staleTime: READINESS_STALE_TIME_MS,
    // Bu — qulaylik uchun qo'shimcha ma'lumot, sahifaning asosiy mazmuni emas: xato bo'lsa
    // qayta urinilmaydi va ekranda hech qanday xato ko'rsatilmaydi.
    retry: false,
  });

  return {
    hasConfiguredProvider: query.data === undefined ? null : hasReadyProvider(query.data),
  };
}
