/**
 * `docs/07-api-shartnoma.md` 3.5-bo'lim shartnomasi bo'yicha QO'LDA yozilgan tiplar.
 *
 * AI sozlamalari backend'i (`AiConfigController`, P16-P18) shu vazifa bilan **parallel**
 * boshqa agent tomonidan yozilmoqda — hozircha `swagger.json`/`schema.d.ts`da yo'q
 * (`npm run generate:api` ISHGA TUSHIRILMAYDI, vazifa ko'rsatmasi). Bu xuddi
 * `shared/api/types.ts`dagi "MUVAQQAT QO'LDA YOZILGAN TIPLAR" bilan bir xil vaziyat/naqsh
 * (P12 izohiga qarang, o'sha faylda). Backend tayyor bo'lgach `npm run generate:api` ishga
 * tushiriladi va bu tiplar generatsiya qilingan `components['schemas'][...]`dan re-export
 * bilan almashtiriladi; maydon nomi/nullability'da farq chiqsa shu faylni ishlatuvchi kod
 * shunga qarab tuzatiladi.
 */

export type AiProviderKind = 'Gemini' | 'OpenAi' | 'Anthropic';

export const AI_PROVIDER_KINDS: readonly AiProviderKind[] = ['Gemini', 'OpenAi', 'Anthropic'];

/**
 * `GET /api/admin/ai/providers` ro'yxat elementi. `maskedApiKey` — backend maskalab
 * qaytaradi (`AIza••••7f2b`); kalit umuman kiritilmagan bo'lsa `null`. TO'LIQ kalit
 * qiymati HECH QACHON bu DTO'da kelmaydi (`docs/08` 3-bo'lim, CLAUDE.md MAXSUS DIQQAT #1).
 */
export interface AiProviderConfigDto {
  provider: AiProviderKind;
  displayName: string;
  maskedApiKey: string | null;
  model: string;
  baseUrl?: string | null;
  maxOutputTokens: number;
  temperature: number;
  isDefault: boolean;
  isActive: boolean;
  fallbackOrder: number;
  lastCheckedAt?: string | null;
  lastCheckStatus?: string | null;
}

/**
 * `PUT /api/admin/ai/providers/{provider}` so'rov tanasi — `docs/07` 3.5-bo'lim:
 * `{ apiKey?, model, maxOutputTokens, temperature, isActive, fallbackOrder }`.
 *
 * `apiKey` — **faqat** admin yangi kalit kiritganda yuboriladi. Maydon butunlay yo'q
 * bo'lsa (`undefined`, `client.ts` `JSON.stringify` bilan tashlab yuboradi) backend mavjud
 * kalitni SAQLAB QOLADI — bo'sh qoldirilgan maydon "kalitni o'chir" degani EMAS
 * (`prompts/28` MAXSUS DIQQAT #1).
 */
export interface UpdateAiProviderRequest {
  apiKey?: string;
  model: string;
  maxOutputTokens: number;
  temperature: number;
  isActive: boolean;
  fallbackOrder: number;
}

/** `POST /api/admin/ai/providers/{provider}/test` javobi — `docs/07` 3.5-bo'lim. */
export interface TestAiConnectionResult {
  ok: boolean;
  latencyMs: number | null;
  /**
   * `ok: false` bo'lganda xato TURINI aniq bildiradigan matn (masalan "API kaliti
   * noto'g'ri", "So'rov limiti tugagan", "Provider javob bermadi") — backend tomonidan
   * shakllantiriladi. UI bu matnni umumiy "Xatolik" bilan ALMASHTIRMAYDI, aynan ko'rsatadi
   * (`prompts/28` MAXSUS DIQQAT #3).
   */
  message: string | null;
}

/** `GET /api/admin/ai/prompts` ro'yxat elementi — `docs/07` 3.5-bo'lim. */
export interface AiPromptTemplateDto {
  key: string;
  version: number;
  isActive: boolean;
  createdAt: string;
  systemText: string;
  userText: string;
}

export interface AiUsageBreakdownItem {
  provider: AiProviderKind;
  calls: number;
  inputTokens: number;
  outputTokens: number;
  /** `null` qoidasi (`docs/07` §8) — narx konfiguratsiyasi yo'q bo'lsa `0` EMAS `null`. */
  estimatedCostUsd: number | null;
}

/** `GET /api/admin/ai/usage?from=&to=` javobi — `docs/07` 3.5-bo'lim. */
export interface AiUsageStatsResponse {
  from: string;
  to: string;
  totalCalls: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  estimatedCostUsd: number | null;
  byProvider: AiUsageBreakdownItem[];
}
