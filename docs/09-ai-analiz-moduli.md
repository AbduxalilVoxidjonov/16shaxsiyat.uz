# 09 — AI tahlil moduli

## 1. Prinsip

Modul **provider-agnostik**. Superadmin panelda qaysi provayderning API kaliti kiritilgan bo'lsa,
tizim faqat o'shalarni taklif qiladi; tahlil boshlanishida qaysi biri ishlatilishi so'raladi
(yoki `IsDefault` avtomatik olinadi).

```
AnalyzeAssessmentCommand
      │
      ▼
AnalysisOrchestrator
      │  1. Ma'lumot yig'ish (TestResult'lar + kontekst)   → AnalysisInput
      │  2. PromptBuilder  → (systemText, userText, jsonSchema)
      │  3. IAiProviderResolver → tanlangan yoki default provider
      ▼
IAiAnalysisProvider  ──►  GeminiProvider | OpenAiProvider | AnthropicProvider
      │  4. Structured output (JSON schema bilan)
      ▼
AiResponseValidator   (schema + taqiqlangan atamalar + uzunlik)
      │
      ▼
AiAnalysis (Succeeded) → AssessmentStatus = Analyzed → StudentSnapshot yangilanadi
```

⚠️ Bu zanjir **avtomatik emas**: tahlil superadmin admin panelidagi "AI tahlil qilish"
tugmasini bosganda boshlanadi (8.0-bo'lim). Sessiya yakunlanishi bilan avtomatik navbatga
qo'yish `Ai:AutoAnalyzeOnCompletion` bayrog'i ostida va standart holda **o'chiq**.

---

## 2. Abstraksiya

```csharp
public interface IAiAnalysisProvider
{
    AiProvider Kind { get; }                       // Gemini | OpenAi | Anthropic
    Task<AiCompletionResult> CompleteJsonAsync(
        AiCompletionRequest request, CancellationToken ct);
    Task<AiHealthResult> CheckHealthAsync(CancellationToken ct);
}

public sealed record AiCompletionRequest(
    string SystemText,
    string UserText,
    JsonDocument JsonSchema,      // structured output sxemasi
    string Model,
    int MaxOutputTokens,
    double Temperature);

public sealed record AiCompletionResult(
    bool Success,
    string? RawJson,
    int? InputTokens,
    int? OutputTokens,
    int DurationMs,
    string? ErrorMessage,
    AiErrorKind ErrorKind);       // None, Auth, RateLimit, Timeout, Schema, Server, Unknown

public interface IAiProviderResolver
{
    Task<IReadOnlyList<AiProviderInfo>> GetAvailableAsync(CancellationToken ct);  // kaliti bor va faol
    Task<IAiAnalysisProvider> ResolveAsync(AiProvider? requested, CancellationToken ct);
    Task<IReadOnlyList<IAiAnalysisProvider>> GetFallbackChainAsync(AiProvider primary, CancellationToken ct);
}
```

### Provider xususiyatlari

| Provider | Structured output usuli | Eslatma |
|----------|-------------------------|---------|
| **Gemini** | `generationConfig.responseMimeType = "application/json"` + `responseSchema` | Arzon, uzun kontekst |
| **OpenAI** | `response_format: { type: "json_schema", json_schema: { strict: true } }` | Eng qat'iy schema qo'llab-quvvatlash |
| **Anthropic** | Tool-use ("`emit_analysis`" tool, `input_schema`) yoki prefill `{` | Nozik matn sifati yuqori |

Model nomlari **konfiguratsiyadan** olinadi (`ai_provider_configs.model`) — kodda qattiq yozilmaydi,
chunki modellar tez yangilanadi.

---

## 3. AI'ga yuboriladigan kirish (`AnalysisInput`)

> Shaxsiy ma'lumot **yo'q**: ism, telefon, email, maktab nomi, aniq tug'ilgan sana yuborilmaydi.

```json
{
  "context": { "age": 16, "grade": 9, "gender": "male", "language": "uz" },
  "reliability": { "score": 82.5, "flag": "Reliable", "notes": [] },
  "personality16": {
    "type": "INTJ", "typeNameUz": "Loyihachi",
    "axes": { "EI": { "pct": 28.3, "letter": "I" }, "SN": { "pct": 71.6, "letter": "N" },
              "TF": { "pct": 33.3, "letter": "T" }, "JP": { "pct": 64.1, "letter": "J" } },
    "borderlineAxes": []
  },
  "bigFive": {
    "O": { "pct": 70, "level": "Yuqori" }, "C": { "pct": 77.5, "level": "Yuqori" },
    "E": { "pct": 35, "level": "Past" },  "A": { "pct": 62.5, "level": "Yuqori" },
    "stability": { "pct": 70, "level": "Yuqori" },
    "maturityIndex": 68.4, "maturityLevel": "Yaxshi"
  },
  "interests": {
    "hollandCode": "IRA",
    "types": { "R": 62, "I": 88, "A": 71, "S": 40, "E": 35, "C": 48 },
    "differentiation": 53, "consistency": "High",
    "mappedFields": ["Muhandislik", "IT", "Ilmiy tadqiqot", "Dizayn"]
  },
  "activity": {
    "MOT": 74, "SELF": 68, "SOCA": 52, "ENG": 60,
    "activityIndex": 65.2, "activityLevel": "Moderate", "needsAttention": false
  },
  "customTests": [
    { "name": "Stressga chidamlilik anketasi",
      "scales": [ { "name": "Stressga munosabat", "pct": 58, "level": "O'rtacha" },
                  { "name": "Qo'llab-quvvatlanish hissi", "pct": 74, "level": "Yuqori" } ] }
  ]
}

> `customTests` — superadmin o'zi tuzgan anketalar (bo'lmasa bo'sh massiv). AI ularni umumiy
> portretga bog'laydi, lekin **tip va kasb xulosalari faqat ilmiy metodikalarga tayanadi** —
> bu promptda alohida aytiladi.
```

---

## 4. Prompt (v1.0)

### 4.1 System

```
Sen — o'smirlar bilan ishlaydigan tajribali ta'lim psixologi va kasb yo'naltirish
maslahatchisisan. Vazifang: o'quvchining psixologik test natijalari asosida
qo'llab-quvvatlovchi, aniq va amaliy tahlil yozish.

TIL: barcha matnni O'ZBEK tilida (lotin yozuvida), sodda va iliq uslubda yoz.
O'quvchi 12–18 yoshda — u tushunadigan tilda yoz, atamalarni izohlab ket.

QAT'IY TAQIQLAR:
- Hech qanday tibbiy yoki psixiatrik tashxis qo'yma. "Depressiya", "ADHD", "autizm",
  "buzilish", "kasallik", "patologiya", "norma emas" kabi so'zlarni ISHLATMA.
- Bolani yorliqlamang: "yomon", "qobiliyatsiz", "dangasa", "muvaffaqiyatsiz" so'zlari taqiqlanadi.
- Kelajakni qat'iy bashorat qilma ("sen albatta ... bo'lasan").
- Bir kasbni yagona to'g'ri yo'l sifatida ko'rsatma.
- Test natijasini o'zgarmas xususiyat sifatida taqdim etma — bu HOZIRGI holat surati.

USLUB:
- Kuchli tomonlardan boshla, o'sish zonalarini imkoniyat sifatida ko'rsat.
- Har bir xulosani aniq ballarga bog'la ("Vijdonlilik 77% — bu shuni ko'rsatadiki...").
- Umumiy gaplardan qoch, aniq va amaliy tavsiya ber.
- Har bir tavsiya bajarilishi mumkin bo'lgan qadam bo'lsin.

ISHONCHLILIK: agar reliability.flag "Questionable" yoki "Unreliable" bo'lsa, tahlilni
ehtiyotkor tilda yoz va buni "reliabilityNote" maydonida ochiq ayt.

CHIQISH: faqat berilgan JSON sxemasiga to'liq mos JSON qaytar. Qo'shimcha matn, izoh yoki
markdown belgilari qo'shma.
```

### 4.2 User

```
Quyida bitta o'quvchining test natijalari berilgan. Ularni birlashtirib to'liq tahlil yoz.

{ANALYSIS_INPUT_JSON}

Talablar:
1. summary — 2–3 jumlada umumiy portret.
2. personalityPortrait — 16 tipli model va Big Five natijalarini BIRLASHTIRIB tavsifla
   (faqat tip nomini takrorlama).
3. strengths — 4–6 ta kuchli tomon, har biri ball bilan asoslangan.
4. growthAreas — 3–5 ta o'sish zonasi, har birida aniq qadam.
5. learningStyle — bu o'quvchi qanday o'rganganda samarali bo'ladi.
6. motivationProfile — nima uni harakatga keltiradi, nima to'xtatadi.
7. activityAssessment — hozirgi faollik darajasi va nima qilish kerakligi.
8. careerSuggestions — 3–5 yo'nalish, har birida: nomi, nega mos, 2–3 keyingi qadam
   (to'garak, kurs, kitob, tajriba).
9. studentRecommendations — o'quvchining o'ziga 5–7 amaliy maslahat.
10. teacherNotes — sinf rahbari/o'qituvchiga 3–5 tavsiya.
11. parentNotes — ota-onaga 3–5 tavsiya.
12. attentionFlags — e'tibor talab qiladigan holatlar (masalan juda past motivatsiya).
    Hech narsa bo'lmasa bo'sh massiv.
13. disclaimer — natijaning cheklovlari haqida 1–2 jumla.

Agar `customTests` bo'sh bo'lmasa: ularning natijalarini `personalityPortrait` va
`growthAreas` da hisobga ol, lekin `careerSuggestions` va shaxsiyat tipi xulosalarini
faqat 16 tipli model, Big Five va RIASEC natijalariga asosla.
```

---

## 5. Javob JSON sxemasi

```json
{
  "type": "object",
  "required": ["summary","personalityPortrait","strengths","growthAreas","learningStyle",
               "motivationProfile","activityAssessment","careerSuggestions",
               "studentRecommendations","teacherNotes","parentNotes","attentionFlags","disclaimer"],
  "additionalProperties": false,
  "properties": {
    "summary":             { "type": "string", "minLength": 80,  "maxLength": 600 },
    "personalityPortrait": { "type": "string", "minLength": 300, "maxLength": 2500 },
    "strengths": {
      "type": "array", "minItems": 4, "maxItems": 6,
      "items": { "type": "object", "required": ["title","description","evidence"],
        "additionalProperties": false,
        "properties": { "title": {"type":"string","maxLength":80},
                        "description": {"type":"string","maxLength":400},
                        "evidence": {"type":"string","maxLength":200} } }
    },
    "growthAreas": {
      "type": "array", "minItems": 3, "maxItems": 5,
      "items": { "type": "object", "required": ["title","description","actionStep"],
        "additionalProperties": false,
        "properties": { "title": {"type":"string","maxLength":80},
                        "description": {"type":"string","maxLength":400},
                        "actionStep": {"type":"string","maxLength":250} } }
    },
    "learningStyle":       { "type": "string", "maxLength": 900 },
    "motivationProfile":   { "type": "string", "maxLength": 900 },
    "activityAssessment":  { "type": "string", "maxLength": 900 },
    "careerSuggestions": {
      "type": "array", "minItems": 3, "maxItems": 5,
      "items": { "type": "object", "required": ["field","why","nextSteps"],
        "additionalProperties": false,
        "properties": { "field": {"type":"string","maxLength":100},
                        "why": {"type":"string","maxLength":400},
                        "exampleProfessions": {"type":"array","items":{"type":"string"},"maxItems":5},
                        "nextSteps": {"type":"array","minItems":2,"maxItems":4,
                                      "items":{"type":"string","maxLength":200}} } }
    },
    "studentRecommendations": { "type":"array","minItems":5,"maxItems":7,
                                "items":{"type":"string","maxLength":250} },
    "teacherNotes":           { "type":"array","minItems":3,"maxItems":5,
                                "items":{"type":"string","maxLength":250} },
    "parentNotes":            { "type":"array","minItems":3,"maxItems":5,
                                "items":{"type":"string","maxLength":250} },
    "attentionFlags": {
      "type":"array","maxItems":5,
      "items": { "type":"object","required":["code","message","severity"],
        "additionalProperties": false,
        "properties": { "code": {"type":"string","maxLength":40},
                        "message": {"type":"string","maxLength":300},
                        "severity": {"type":"string","enum":["info","attention","high"]} } }
    },
    "reliabilityNote": { "type": ["string","null"], "maxLength": 400 },
    "disclaimer":      { "type": "string", "maxLength": 400 }
  }
}
```

---

> **Javob qayerda saqlanadi.** Yuqoridagi sxema bo'yicha kelgan XOM javob to'liq holda
> `AiAnalysis.ResponseJson`da saqlanadi va admin hisoboti (`GET /api/admin/students/{id}`
> → `latestAssessment.aiAnalysis`, `docs/07` 3.2) AYNAN shundan yig'iladi. `ai_analyses`
> jadvalidagi alohida ustunlar (`strengths_json`, `teacher_notes`, …) — tez o'qish uchun
> YASSILANGAN ikkilamchi nusxa; ular sxemadagi tuzilmani va beshta maydonni
> (`learningStyle`, `motivationProfile`, `activityAssessment`, `disclaimer`,
> `reliabilityNote`) saqlamaydi, shuning uchun hisobot manbai sifatida ishlatilmaydi.

---

## 6. Validatsiya va post-filtr

`AiResponseValidator` ketma-ket tekshiradi:

1. **JSON parse** — xato bo'lsa `ErrorKind.Schema`, retry.
2. **Schema validatsiya** (`JsonSchema.Net`) — mos kelmasa retry (2-urinishda promptga
   "oldingi javob sxemaga mos emas edi" eslatmasi qo'shiladi).
3. **Taqiqlangan atamalar** (case-insensitive, o'zak bo'yicha):
   `depressiya, shizofren, autiz, ADHD, SDVG, buzilish, kasallik, patologi, aqli zaif,
    dangasa, qobiliyatsiz, norma emas, tashxis, diagnoz, davolash, dori`
   — topilsa: 1 marta qayta so'raladi ("quyidagi so'zlarni ishlatma"); yana chiqsa
   o'sha maydon "moderatsiya qilindi" belgisi bilan saqlanadi va `AttentionFlags` ga
   `MODERATION_REQUIRED` qo'shiladi (superadmin ko'radi).
   > Bu belgi admin API'sida `aiAnalysis.isModerated = true` va `attentionFlags` ichidagi
   > `MODERATION_REQUIRED` (`severity: "high"`) sifatida ochiq qaytariladi (`docs/07` 3.2) —
   > post-filtrdan toza o'tmagan matn ekranda belgisiz qolmaydi.
4. **Uzunlik va tillar** — matn asosan lotin o'zbek alifbosida ekanini yengil tekshirish
   (kiril ulushi > 30% bo'lsa retry).
5. **Nomlar sizmasligi** — javobda o'quvchi ismi bo'lmasligi kerak (promptga yuborilmagan,
   lekin baribir tekshiriladi).

---

## 7. Retry va fallback

```
urinish 1: default provider (yoki so'ralgan)
   ├─ Timeout / RateLimit / Server → 2 s, 6 s, 15 s kutib 3 martagacha qayta urinish
   ├─ Schema xatosi                → tuzatuvchi eslatma bilan 2 martagacha
   └─ Auth xatosi                  → retry YO'Q, darhol keyingi providerga
urinish 2: fallback zanjiridagi keyingi provider (fallback_order bo'yicha)
urinish 3: zanjirdagi uchinchi provider
hammasi muvaffaqiyatsiz → AiAnalysis.Status = Failed,
                          Assessment.Status = AnalysisFailed,
                          log: Error + superadmin dashboardida "Tahlil kutilmoqda" hisoblagichi
```

Har urinish alohida `AiAnalysis` yozuvi sifatida saqlanadi (`AttemptNumber` bilan) —
qaysi provider nima xato berganini keyin ko'rish mumkin.

**Timeout:** so'rovga 90 soniya (`Ai:TimeoutSeconds`). Polly bilan retry siyosati.

---

## 8. Fon jarayoni

### 8.0 Tahlil QANDAY boshlanadi (2026-09-03 dan)

⚠️ **Tahlil QO'LDA ishga tushiriladi.** Sessiya yakunlanishi tahlilni AVTOMATIK boshlamaydi:

| Boshlanish yo'li | Holat |
|------------------|-------|
| Admin panel → "AI tahlil qilish" tugmasi (`POST /api/admin/assessments/{id}/rerun-analysis`) | **Asosiy yo'l.** Har doim ishlaydi, sozlamadan MUSTAQIL |
| O'quvchi sessiyani yakunlashi (`POST /api/public/sessions/complete`) | Faqat `Ai:AutoAnalyzeOnCompletion=true` bo'lsa. Standart — **`false`**, ya'ni O'CHIQ |

Sabab (`docs/06` 8-bo'lim, 2026-09-03 loyiha EGASI qarori): har bir tahlil AI provayderiga
to'lanadigan xarajat, shuning uchun qaysi o'quvchi tahlil qilinishini egasi o'zi tanlaydi.

Bayroq **o'chiq** bo'lganda `CompleteSessionCommandHandler` avvalgidek ishlaydi — ballar,
`TestResult`, `MaturityIndex`, ishonchlilik va `StudentSnapshot` YAKUNLASH paytida
hisoblanadi — faqat `MarkAnalyzing` chaqirilmaydi va navbatga hech narsa qo'yilmaydi:
sessiya `Completed` holatida qoladi. Tugma bosilganda `Completed` → `Analyzing` o'tishi
`RerunAnalysisCommandHandler` orqali bo'ladi (holat mashinasi `Completed`dan
`MarkAnalyzing`ga ruxsat beradi, `docs/04` 2.3).

Bayroq **yoqilganda** navbatga qo'yish baribir `IPostCommitActions` orqali — tranzaksiya
COMMIT bo'lgandan keyin (P18-R1), rollback bo'lsa hech qachon (P18-R2).

Sozlama: `Ai:AutoAnalyzeOnCompletion` (env: `Ai__AutoAnalyzeOnCompletion`), `docs/13` 6-bo'lim.

### 8.1 Navbat

- Navbatga qo'yish (yuqoridagi ikki yo'ldan biri) → `IBackgroundJobQueue.EnqueueAiAnalysisAsync(assessmentId)`.
- Ishlov: Hangfire (Postgres storage) yoki `BackgroundService` + `analysis_jobs` jadvali.
- Parallellik: bir vaqtda 4 ta job (provider rate limitlarini urmaslik uchun).
- Idempotentlik: job boshlanishida `Assessment.Status` tekshiriladi; allaqachon `Analyzed` bo'lsa chiqib ketadi.
- Qayta ishga tushirish (`rerun-analysis`) — yangi job, eski `IsCurrent = false` qilinadi
  faqat yangisi muvaffaqiyatli bo'lgandan **keyin**.

---

## 9. Xarajat hisobi

`ai_analyses` da `input_tokens`, `output_tokens`, `estimated_cost_usd` saqlanadi.
Narx jadvali konfiguratsiyada (`Ai:Pricing:{Provider}:{Model}: { inputPer1M, outputPer1M }`) —
model narxi o'zgarsa faqat konfig yangilanadi.

```
cost = inputTokens/1e6 × inputPer1M + outputTokens/1e6 × outputPer1M
```

`GET /api/admin/ai/usage` — davr bo'yicha: chaqiruvlar soni, tokenlar, jami xarajat, provider kesimi.

**Taxminiy hajm:** bitta tahlil ≈ 1.5–2.5K input + 2–3K output token.
1000 o'quvchi ≈ 5M token — arzon modelda bir necha dollar darajasida.

---

## 10. Sifatni nazorat qilish

1. **Oltin namunalar:** 10 ta tayyor `AnalysisInput` (turli tip/daraja kombinatsiyalari) —
   prompt o'zgarganda shular bo'yicha qayta ishga tushirib, natija sifatini ko'zdan kechirish.
2. **Prompt versiyalash:** `prompt_templates` jadvalida `key + version`; faol versiya bittasi.
   Har `AiAnalysis` qaysi versiya bilan yaratilganini saqlaydi.
3. **A/B:** yangi versiya avval 20 sessiyada sinaladi (`PromptVersion=v1.1`), taqqoslanadi, keyin faollashadi.
4. **Superadmin baholashi (v2):** har hisobotga 👍/👎 — past baholi promptlar ko'rib chiqiladi.
5. **Determinizm:** `temperature = 0.4` (juda past qilinsa matn quruq, yuqori qilinsa beqaror).

---

## 11. Zaxira (fallback) hisobot

Barcha AI urinishlari muvaffaqiyatsiz bo'lsa, tizim **shablon asosidagi** hisobot ko'rsatadi:
`TypeCatalog` (tip tavsifi, kuchli tomonlar) + `CareerMap` (Holland kodi bo'yicha yo'nalishlar) +
ball darajalari matnlari. Bu AI emas — shuning uchun UI'da "Avtomatik shablon hisobot,
to'liq AI tahlil hali tayyor emas" deb belgilanadi va "Qayta urinish" tugmasi chiqadi.
