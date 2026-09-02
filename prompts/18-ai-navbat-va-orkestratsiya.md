# P18 — Fon navbati, orkestratsiya va fallback

## O'qish shart
- `docs/09-ai-analiz-moduli.md` (7, 8, 11-bo'limlar)
- `docs/04-domain-model.md` (2.8, 3-bo'limlar)

## Vazifa
1. `IBackgroundJobQueue` implementatsiyasi: Hangfire (Postgres storage) **yoki**
   `analysis_jobs` jadvali + `BackgroundService` (tanlovni `docs/06` qarorlar jurnaliga yoz).
   - parallellik: 4 ta job
   - retry: 2 s, 6 s, 15 s
2. `AnalysisOrchestrator`:
   - `PromptBuilder` → `IAiProviderResolver` → provider → `AiResponseValidator`
   - har urinish uchun alohida `AiAnalysis` yozuvi (`AttemptNumber`)
   - xato turiga qarab: `Auth` → retry yo'q, darhol keyingi provider; boshqalar → retry
   - muvaffaqiyat: `Status = Succeeded`, `IsCurrent = true` (eskisi `false`),
     `Assessment.Status = Analyzed`, `StudentSnapshot` yangilanadi,
     `AiAnalysisSucceededEvent`
   - to'liq muvaffaqiyatsizlik: `Assessment.Status = AnalysisFailed`, `AiAnalysisFailedEvent`,
     `Error` log
   - **idempotent**: job boshida holat tekshiriladi
3. `AssessmentCompletedEvent` handler → job navbatga qo'yiladi (P12 dagi `NoOpJobQueue` almashtiriladi).
4. `RerunAnalysisCommand` → `POST /api/admin/assessments/{id}/rerun-analysis`
   - `{ provider?, promptVersion? }`; yangi job, `202 Accepted`
   - eski `IsCurrent` faqat yangisi muvaffaqiyatli bo'lgach o'zgaradi
5. **Zaxira hisobot** (`docs/09` 11-bo'lim): barcha urinishlar muvaffaqiyatsiz bo'lsa,
   `TypeCatalog` + `CareerMap` + daraja matnlaridan shablon hisobot yig'iladi va
   `IsFallbackReport = true` belgisi bilan qaytariladi.
6. Xarajat va tokenlar har yozuvda saqlanadi.

## Cheklovlar
- Job ichida `HttpContext` ishlatilmaydi (scope alohida yaratiladi).
- Bir sessiya uchun bir vaqtda bitta job (dublikat navbatga qo'yish bloklanadi).
- AI xatosi butun oqimni buzmaydi — ballar baribir ko'rinadi.

## DoD
- [ ] `MockAiProvider` bilan: sessiya yakunlangach 5 soniya ichida `Analyzed` bo'ladi
- [ ] Test: birinchi provider `Timeout`, ikkinchisi muvaffaqiyat → 2 ta `AiAnalysis`, holat `Analyzed`
- [ ] Test: hammasi muvaffaqiyatsiz → `AnalysisFailed` + zaxira hisobot ko'rinadi
- [ ] Test: `rerun` yangi yozuv yaratadi, eskisi tarixda qoladi
- [ ] Job ikki marta ishga tushirilsa dublikat tahlil yaratmaydi

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "Analysis"
```

## ⚠️ P18-R1 (MAJBURIY) — navbatga qo'yish tranzaksiya commit'idan KEYIN

`CompleteSessionCommandHandler` hozir `IBackgroundJobQueue.EnqueueAiAnalysisAsync` ni
**handler ichida**, ya'ni `TransactionBehavior` ochgan tranzaksiya commit bo'lishidan **oldin**
chaqiradi. Bugun bu zararsiz (`NoOpJobQueue`), lekin haqiqiy navbat ulanganda klassik poyga
holatiga aylanadi:

- fon ishchisi vazifani darhol olib, hali **commit qilinmagan** `Assessment` ni o'qishga uradi
  → "topilmadi" xatosi yoki eski holat;
- tranzaksiya rollback bo'lsa, navbatda **mavjud bo'lmagan sessiya uchun** vazifa qolib ketadi.

Talab: navbatga qo'yish tranzaksiya **muvaffaqiyatli commit bo'lgandan keyin** bajarilsin.
Yechim variantlari (qaysi biri toza chiqsa — o'shani tanla va sababini yoz):
- `AssessmentCompletedEvent` ni commit'dan keyin publish qilib, hodisa ishlovchisida navbatga qo'yish;
- yoki `TransactionBehavior` ga "commit'dan keyin bajariladigan amallar" ro'yxatini qo'shish;
- yoki outbox jadvali (eng ishonchli, lekin qimmatroq).

**P18-R2 (majburiy test):** tranzaksiya rollback bo'lganda navbatga **hech narsa qo'yilmasligi**
tasdiqlansin; muvaffaqiyatli holatda esa qo'yilgan vazifa commit qilingan ma'lumotni ko'ra olsin.
