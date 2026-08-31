# P11 — Savollarni berish va javoblarni saqlash

## O'qish shart
- `docs/07-api-shartnoma.md` (1.4, 1.5, 1.6-bo'limlar)
- `docs/04-domain-model.md` (2.4, 2.5-bo'limlar)

## Vazifa
1. `StartTestCommand` → `POST /api/public/sessions/tests/{testCode}/start`
   - oldingi test tugamagan bo'lsa `409 TEST_NOT_UNLOCKED`
   - `ShuffleQuestions` yoqilgan bo'lsa tartib bir marta generatsiya qilinib
     `QuestionOrderJson` ga yoziladi (qayta kirganda **o'sha** tartib)
   - `Assessment.Status`: `Draft → InProgress`
2. `GetTestQuestionsQuery` → `GET .../questions?page=N`
   - sahifalash `TestDefinition.PageSize` bo'yicha
   - javob berilgan savollarga `currentValue` qo'shiladi
   - `scaleLabels` tildan olinadi
3. `SaveAnswersCommand` → `POST .../answers`
   - paketli upsert: `(assessmentTestId, questionId)` bo'yicha
   - mavjud javob yangilansa `RevisionCount++`
   - `RawValue` savol turiga mos ekani tekshiriladi (Likert5 → 1..5)
   - `AnsweredCount` yangilanadi
   - yakunlangan/muddati o'tgan sessiyaga yozish taqiqlanadi
   - bir so'rovda maksimum 50 javob

## Cheklovlar
- **`scale` va `scaleDirection` javobga hech qachon qo'shilmaydi** — buni integration test tekshiradi.
- Boshqa sessiyaning `assessmentTestId` siga yozish imkoni bo'lmasin (token orqali tekshiruv).
- Savollar har so'rovda DB'dan olinadi, lekin `TestDefinition` va `Question` ro'yxati
  `IMemoryCache` da 10 daqiqa keshlanadi (o'zgarsa invalidatsiya).

## DoD
- [ ] 3 endpoint ishlaydi
- [ ] Integration test: bir savolga ikki marta javob → bitta qator, `RevisionCount=1`
- [ ] Test: boshqa sessiya tokeni bilan yozishga urinish → 403/404
- [ ] Test: javobda `scale`/`direction` yo'qligi
- [ ] Test: aralashtirilgan tartib qayta so'rovda bir xil

## Tekshiruv
```bash
dotnet test tests/StudentRoadMap.Api.IntegrationTests --filter "Answers|Questions"
```
