# P12 — Testni va sessiyani yakunlash, scoring'ni ulash

## O'qish shart
- `docs/07-api-shartnoma.md` (1.7, 1.8, 1.9-bo'limlar)
- `docs/03-psixologik-metodikalar.md` (6 va 7-bo'limlar)

## Vazifa
1. `CompleteTestCommand` → `POST .../tests/{testCode}/complete`
   - barcha majburiy savollar javoblangani tekshiriladi; aks holda `400` + `unansweredCount`
   - `ScoringEngine` chaqiriladi → `TestResult` yoziladi (`ScoringVersion` bilan)
   - `AssessmentTest.Status = Completed`
   - `TestCompletedEvent` chiqadi
   - javobda `nextTestCode` va `allTestsCompleted`
2. `CompleteSessionCommand` → `POST /api/public/sessions/complete`
   - barcha testlar tugagani tekshiriladi
   - `CompositeScorer` → `MaturityIndex` hisoblanadi va `BIG5` natijasiga yoziladi
   - `ReliabilityCalculator` → `Assessment.ReliabilityScore` va bayroq
   - `TotalDurationSeconds` hisoblanadi
   - `Assessment.Status = Completed` → `AssessmentCompletedEvent`
   - `Status = Analyzing` (AI navbati P18 da ulanadi; hozircha `IBackgroundJobQueue` interfeysi
     chaqiriladi, implementatsiyasi vaqtincha bo'sh — `NoOpJobQueue`)
   - `StudentSnapshot` yangilanadi (tip, indekslar, `NeedsAttention`)
3. `GetStudentResultQuery` → `GET /api/public/sessions/result`
   - faqat `showResultToStudent` sozlamasi yoqilgan bo'lsa (hozircha `appsettings` dan)
   - **qisqartirilgan** javob (`docs/07` 1.9) — aktivlik, bayroq, xom ball **yo'q**
   - tahlil tayyor bo'lmasa `202`

## Cheklovlar
- Scoring **sinxron** (tez, IO yo'q); AI — asinxron.
- `CompleteSession` idempotent: ikkinchi chaqiruv xato bermaydi, joriy holatni qaytaradi.
- O'quvchi javobida `MaturityIndex`, `ActivityIndex`, `ReliabilityScore` bo'lmasin.

## DoD
- [ ] To'liq oqim integration testi: sessiya → 4 testni to'ldirish → yakunlash →
      4 ta `TestResult` mavjud, `MaturityIndex` hisoblangan, `ReliabilityScore` yozilgan
- [ ] To'ldirilmagan savol bilan yakunlashga urinish → 400
- [ ] `CompleteSession` ikki marta chaqirilsa xato yo'q
- [ ] O'quvchi natijasida maxfiy maydonlar yo'qligi test bilan tekshirilgan

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "FullFlow"
```
