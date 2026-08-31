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

## ⚠️ P12-R1 (MAJBURIY) — `ReliabilityInput.Questions` tartibi

`ReliabilityCalculator` ro'yxat tartibini **xronologiya** deb qabul qiladi va uni qayta
tartiblamaydi (`docs/03` §7.1, 3-band). Handler ro'yxatni **aynan shunday** tuzadi:

```csharp
assessment.Tests
    .OrderBy(t => t.DisplayOrder)                       // sessiyadagi blok tartibi
    .SelectMany(t => testDefinitions[t.TestDefinitionId].Questions
                         .OrderBy(q => q.DisplayOrder)) // test ICHIDAGI tartib
    .Select(QuestionMeta.From)
    .ToList();
```

**Butun ro'yxatga `.OrderBy(q => q.DisplayOrder)` qo'llash QAT'IY TAQIQLANADI.**
`DisplayOrder` har testda `1..N` dan boshlanadi (`1..60`, `1..50`, `1..48`, `1..32`) — butun
ro'yxatni shu bo'yicha saralash 4 test blokini aralashtirib yuboradi va straight-lining
signalini **jimgina o'chiradi**: bitta test blokini boshdan-oxir bir xil javob bilan to'ldirgan
o'quvchi `Reliable` deb baholanadi. Bu P09 QA ko'rigida topilgan bloklovchi xato edi;
Domain tomonida tuzatilgan, lekin noto'g'ri tartibni **hech narsa ushlamaydi** — na kompilyator,
na runtime tekshiruvi. Yagona himoya — P12-R2 testi.

## ⚠️ P12-R2 (MAJBURIY test)

190 savolli to'liq sessiyada **bitta test bloki** butunlay bir xil javob bilan to'ldirilsa,
handler natijasida `ReliabilityResult.Reasons` ichida `StraightLining` bo'lishi **shart**.
Namuna: `tests/StudentRoadMap.Domain.Tests/Scoring/RealQuestionBankTests.cs` dagi
`ReliabilityCalculator_RealSeedSession_StraightLinedTestBlockIsDetected` testining
Application/integration darajasidagi nusxasi. Handler tartibni buzsa aynan shu test qizaradi.

## DoD
- [ ] To'liq oqim integration testi: sessiya → 4 testni to'ldirish → yakunlash →
      4 ta `TestResult` mavjud, `MaturityIndex` hisoblangan, `ReliabilityScore` yozilgan
- [ ] To'ldirilmagan savol bilan yakunlashga urinish → 400
- [ ] `CompleteSession` ikki marta chaqirilsa xato yo'q
- [ ] O'quvchi natijasida maxfiy maydonlar yo'qligi test bilan tekshirilgan
- [ ] **P12-R1** bajarilgan: `ReliabilityInput.Questions` `(test, DisplayOrder)` tartibida quriladi
- [ ] **P12-R2** testi yozilgan va yashil

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "FullFlow"
```
