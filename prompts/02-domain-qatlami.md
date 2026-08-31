# P02 — Domain qatlami

## Kontekst
Solution skeleti tayyor (P01). Endi biznes yadrosi — entity'lar, value object'lar, enum'lar,
holat mashinasi va domen hodisalari.

## O'qish shart
- `docs/04-domain-model.md` (to'liq)
- `docs/05-database-schema.md` (3-bo'lim — enum raqamlari)

## Vazifa
`src/StudentRoadMap.Domain/` ichida:

1. **Common:** `Entity`, `AggregateRoot` (domen hodisalari ro'yxati bilan), `IDomainEvent`,
   `ValueObject`, `Result` va `Result<T>`, `DomainException`.
2. **Schools:** `School` (+ `RegenerateAccessToken()`, `Deactivate()`, `Activate()`), `SchoolSlug` VO
   (o'zbek harflari translit: `o'`→`o`, `g'`→`g`, `sh`→`sh`, bo'sh joy→`-`).
3. **Students:** `Student` (+ snapshot maydonlari va `UpdateSnapshot(...)` metodi), `Gender`,
   `PhoneNumber` VO (`+998XXXXXXXXX` normalizatsiya), `NameNormalizer` (statik yordamchi).
4. **Assessments:** `Assessment` (holat mashinasi metodlari: `StartTest`, `CompleteTest`,
   `Complete`, `MarkAnalyzing`, `MarkAnalyzed`, `MarkAnalysisFailed`, `MarkAbandoned` —
   noto'g'ri o'tishda `DomainException`), `AssessmentTest`, `Answer` (+ `UpdateValue` →
   `RevisionCount++`), `TestResult`, `AssessmentStatus`, `TestStatus`, `ReliabilityFlag`.
5. **Catalog:** `TestDefinition`, `Question`, `AnswerOption`, `QuestionType`, `TypeCatalogEntry`,
   `CareerMapEntry`.
6. **Ai:** `AiAnalysis`, `AiProvider`, `AiAnalysisStatus`, `AiProviderConfig`, `PromptTemplate`.
7. **Identity:** `AdminUser` (+ `RegisterFailedLogin()`, `ResetFailedLogins()`, `IsLocked(now)`),
   `RefreshToken`, `AdminRole`.
8. **Events:** `AssessmentStartedEvent`, `TestCompletedEvent`, `AssessmentCompletedEvent`,
   `AiAnalysisSucceededEvent`, `AiAnalysisFailedEvent`, `SchoolLinkRegeneratedEvent`.

Barcha enum qiymatlari `docs/05` dagi raqamlarga **aniq mos** bo'lsin (`[Description]` emas,
aniq `= 1` ko'rinishida).

## Cheklovlar
- EF, HTTP, `DateTime.Now`, fayl IO — **yo'q**. Vaqt parametr sifatida uzatiladi (`DateTimeOffset now`).
- Setterlar `private set`; obyekt faqat konstruktor yoki fabrika metodlari orqali yaratiladi.
- Kolleksiyalar tashqariga `IReadOnlyCollection<T>` sifatida chiqadi.

## DoD
- [ ] `dotnet build` toza
- [ ] `Domain.Tests` da yozilgan testlar: holat mashinasi (kamida 6 ta noto'g'ri o'tish),
      `PhoneNumber` (5 xil format), `SchoolSlug` (translit), `NameNormalizer` (dublikat)
- [ ] `Domain` loyihasida NuGet paket yo'q

## Tekshiruv
```bash
dotnet build && dotnet test tests/StudentRoadMap.Domain.Tests
grep -r "PackageReference" src/StudentRoadMap.Domain/*.csproj   # bo'sh bo'lishi kerak
```
