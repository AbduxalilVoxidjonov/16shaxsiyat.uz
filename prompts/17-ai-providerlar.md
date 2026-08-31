# P17 — Gemini, OpenAI va Anthropic provayderlari

## Kontekst
Abstraksiya tayyor (P16). Endi uchta real provayder.

## O'qish shart
- `docs/09-ai-analiz-moduli.md` (2-bo'lim — provider xususiyatlari)
- `docs/07-api-shartnoma.md` (3.5-bo'lim — sozlamalar API)

## Vazifa
1. Uchta implementatsiya — har biri `IAiAnalysisProvider`:
   - `GeminiProvider` — `responseMimeType: application/json` + `responseSchema`
   - `OpenAiProvider` — `response_format: { type: "json_schema", json_schema: { strict: true } }`
   - `AnthropicProvider` — tool-use (`emit_analysis` tool, `input_schema`)
   Har biri: `HttpClient` (named, `IHttpClientFactory`), timeout `Ai:TimeoutSeconds`,
   token sarfini javob metadata'sidan o'qish, xatoni `AiErrorKind` ga xaritalash
   (401/403 → `Auth`, 429 → `RateLimit`, 5xx → `Server`, timeout → `Timeout`).
2. `AiProviderResolver`:
   - `GetAvailableAsync` — faqat kaliti bor va `IsActive` bo'lganlar
   - `ResolveAsync(requested)` — so'ralgan bo'lsa o'sha, aks holda `IsDefault`
   - `GetFallbackChainAsync` — `fallback_order` bo'yicha qolganlar
3. `AiProviderConfig` boshqaruvi (`AiConfigController`):
   - `GET /api/admin/ai/providers` — kalit **maskalangan** (`AIza••••7f2b`)
   - `PUT .../{provider}` — kalit AES-256-GCM bilan shifrlanadi
   - `POST .../{provider}/test` — `CheckHealthAsync`, natija `last_check_*` ga yoziladi
   - `POST .../{provider}/set-default`
   - Audit: `AiConfig.Updated`, `AiConfig.KeyChanged` (**kalit qiymati yozilmaydi**)
4. Narx hisobi: `Ai:Pricing:{Provider}:{Model}` konfiguratsiyasidan `EstimatedCostUsd`.
5. `GET /api/admin/ai/usage` — davr bo'yicha token va xarajat statistikasi.

## Cheklovlar
- SDK paketlari o'rniga to'g'ridan-to'g'ri HTTP + `System.Text.Json` (bog'liqlik kam, nazorat ko'p).
- Model nomlari kodda qattiq yozilmaydi — konfiguratsiyadan.
- API kalitlari log'ga, xato xabariga yoki javobga **hech qachon** tushmasin.

## DoD
- [ ] Har provider uchun so'rov tanasi snapshot testi (tarmoqsiz)
- [ ] Xato xaritalash testlari (401 → Auth, 429 → RateLimit, timeout)
- [ ] Sozlamalar API ishlaydi, kalit maskalangan qaytadi
- [ ] `test` endpointi haqiqiy kalit bilan yashil natija beradi (qo'lda tekshiriladi)
- [ ] Kalit shifrlangan holda saqlanadi (DB'da ochiq matn yo'qligi tekshirilgan)

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Application.Tests --filter "Provider"
psql ... -c "select provider, left(api_key_encrypted, 20) from ai_provider_configs;"
```
