import type { components } from '@/shared/api/schema';

/**
 * AI sozlamalari DTO'lari — backend `Controllers/Admin/AiConfigController.cs` marshrutlari va
 * `Application/Admin/Ai/AdminAiDtos.cs` yozuvlari (P18).
 *
 * Hammasi endi `shared/api/schema.d.ts` da bor va bu yerda **re-export** qilinadi
 * (`docs/10` §6). Qo'lda yozilgan DTO bu faylda YO'Q.
 */

/**
 * `AiProvider` enum (`Domain/Ai/AiProvider.cs`) `JsonStringEnumConverter` bilan **nom**
 * sifatida serializatsiya qilinadi (`Program.cs`). Marshrutda ham shu nom ishlatiladi —
 * `PUT/POST .../providers/{provider}` `{id}` EMAS, `Gemini` / `OpenAi` / `Anthropic`
 * (`AiProviderConfig.Provider` ustida `ux_ai_provider_kind` unique indeksi bor).
 */
export type AiProviderKind = components['schemas']['AiProvider'];

export const AI_PROVIDER_KINDS: readonly AiProviderKind[] = ['Gemini', 'OpenAi', 'Anthropic'];

/**
 * `GET /api/admin/ai/providers` ro'yxat elementi — `AdminAiProviderDto`.
 *
 * `maskedApiKey` — backend maskalab qaytaradi (`AIza••••••cdef`, birinchi va oxirgi 4 belgi);
 * kalit umuman kiritilmagan bo'lsa `null`. TO'LIQ kalit qiymati HECH QACHON bu DTO'da
 * kelmaydi (`ApiKeyMasker`, `docs/08` 3-bo'lim).
 *
 * `lastCheckStatus` — oxirgi "ulanishni tekshirish" natijasi: `"ok"` yoki `AiErrorKind` nomi
 * (`"Auth"`, `"RateLimit"`, `"ModelNotFound"` …). Faqat DB'da MAVJUD (kamida bir marta `PUT`
 * qilingan) provayderlar ro'yxatga tushadi — sozlanmagan provayder umuman qaytmaydi.
 */
export type AiProviderConfigDto = components['schemas']['AdminAiProviderDto'];

/**
 * `PUT /api/admin/ai/providers/{provider}` so'rov tanasi — `UpdateAiProviderRequest`
 * (`Contracts/Admin/Ai/UpdateAiProviderRequest.cs`). To'liq PUT semantikasi: yuborilmagan
 * maydon backendda `null`/`default` bo'ladi.
 *
 * - `apiKey` — **istisno**: bo'sh/`undefined` bo'lsa backend mavjud kalitni SAQLAB QOLADI
 *   (`UpdateAiProviderCommandHandler`: `if (!string.IsNullOrWhiteSpace(request.ApiKey))`).
 * - `baseUrl` — istisno EMAS: `UpdateSettings(...)` uni **shartsiz** yozadi, ya'ni maydon
 *   yuborilmasa mavjud `baseUrl` O'CHADI. Shu sabab UI joriy qiymatni har doim qaytarib
 *   yuboradi (jonli `curl` bilan tasdiqlangan, 2026-09-02).
 */
export type UpdateAiProviderRequest = components['schemas']['UpdateAiProviderRequest'];

/**
 * `POST /api/admin/ai/providers/{provider}/test` javobi — `AdminAiProviderTestResultDto`.
 *
 * Muvaffaqiyatsiz tekshiruv HTTP xatosi EMAS: `200` bilan `ok:false` qaytadi. `message`
 * har doim to'ldirilgan (`string`, `null` emas) va **provayderning xom javobi emas** —
 * `AiErrorKind` bo'yicha oldindan yozilgan o'zbekcha matn ("API kaliti noto'g'ri…",
 * "Model topilmadi…", "kvota/limit tugagan…", "ulanib bo'lmadi…"). UI uni AYNAN ko'rsatadi.
 *
 * Kalit sozlanmagan provayderda esa `404 ProblemDetails` (`NOT_FOUND`) qaytadi — bu
 * `AppError` sifatida `catch` blokiga tushadi.
 */
export type TestAiConnectionResult = components['schemas']['AdminAiProviderTestResultDto'];

/**
 * `GET /api/admin/ai/prompts` elementi — `AdminPromptTemplateDto`.
 * `version` — **matn** (`"v1.0"`), raqam EMAS; `id` — `Guid`.
 */
export type AiPromptTemplateDto = components['schemas']['AdminPromptTemplateDto'];

/** `estimatedCostUsd` — `null` qoidasi (`docs/07` §8): narx konfiguratsiyasi yo'q bo'lsa `0` EMAS `null`. */
export type AiUsageBreakdownItem = components['schemas']['AdminAiUsageByProviderDto'];

/**
 * `GET /api/admin/ai/usage?from=&to=` javobi — `AdminAiUsageDto`.
 *
 * Maydon nomlari `inputTokens`/`outputTokens` (`total*` prefiksi YO'Q) va javobda `from`/`to`
 * **qaytmaydi** — davr faqat so'rov parametri. Zaxira shablon hisobotlar
 * (`IsFallbackReport = true`) statistikaga KIRMAYDI — ular haqiqiy AI chaqiruvi emas
 * (`GetAiUsageQueryHandler`).
 */
export type AiUsageStatsResponse = components['schemas']['AdminAiUsageDto'];
