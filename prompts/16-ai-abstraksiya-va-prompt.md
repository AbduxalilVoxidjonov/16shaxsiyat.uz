# P16 — AI abstraksiyasi, prompt va javob validatsiyasi

## Kontekst
Backend API to'liq. Endi loyihaning "aqli" — AI tahlil moduli. Bu promptda **provayder
implementatsiyasi yozilmaydi**, faqat abstraksiya, prompt qurish va validatsiya.

## O'qish shart
- `docs/09-ai-analiz-moduli.md` (**to'liq**)
- `docs/08-auth-va-xavfsizlik.md` (5-bo'lim — AI'ga nima yuborilmaydi)

## Vazifa
1. `Application/Common/Interfaces/`: `IAiAnalysisProvider`, `IAiProviderResolver`,
   `AiCompletionRequest/Result`, `AiErrorKind`, `AiProviderInfo` — `docs/09` 2-bo'limidagi imzolar.
2. `Infrastructure/Ai/PromptBuilder`:
   - `Assessment` + `TestResult` lardan `AnalysisInput` JSON quradi (`docs/09` 3-bo'limi)
   - **shaxsiy ma'lumot qo'shilmaydi**: ism, telefon, email, aniq tug'ilgan sana, maktab nomi yo'q;
     faqat yosh (butun son), sinf, jins
   - system va user matnlarini `prompt_templates` jadvalidan oladi (faol versiya),
     jadval bo'sh bo'lsa embedded default (`v1.0`)
3. `AnalysisJsonSchema` — `docs/09` 5-bo'limidagi sxema (embedded resurs sifatida).
4. `AiResponseValidator` — `docs/09` 6-bo'limidagi 5 bosqich:
   parse → schema (`JsonSchema.Net`) → taqiqlangan atamalar → til/uzunlik → ism sizmasligi.
   Natija: `ValidationOutcome { Ok, Retry, Moderated }`.
5. `PromptTemplate` seed: `full_analysis` `v1.0` (system + user + schema) `--seed` bilan yuklanadi.
6. `MockAiProvider` (faqat Development/Test) — tayyor to'g'ri JSON qaytaradi; testlar va
   lokal ishlab chiqish uchun.

## Cheklovlar
- Bu promptda tarmoq chaqiruvi yo'q.
- Prompt matni kodda emas, seed'dagi shablonda (versiyalanadi).
- Taqiqlangan atamalar ro'yxati konfiguratsiyada (`Ai:BannedTerms`), kengaytirilishi oson.

## DoD
- [ ] `PromptBuilder` chiqishida shaxsiy ma'lumot yo'qligini tekshiruvchi test (**majburiy**)
- [ ] Validator testlari: to'g'ri JSON, maydon yetishmasligi, taqiqlangan so'z, kiril matn
- [ ] Prompt shabloni seed qilinadi va versiyasi `AiAnalysis` ga yoziladi
- [ ] `MockAiProvider` bilan to'liq tahlil oqimi ishlaydi

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Application.Tests --filter "Prompt|Validator"
```
