# 04 — Domen modeli

## 1. Kontekst xaritasi

```
┌─────────────────────┐   ┌──────────────────────┐   ┌────────────────────┐
│  School Management  │   │  Assessment          │   │  Analysis          │
│  ─────────────────  │   │  ──────────────────  │   │  ────────────────  │
│  School             │──▶│  Student             │──▶│  AiAnalysis        │
│  SchoolAccessLink   │   │  Assessment          │   │  AiProviderConfig  │
│                     │   │  AssessmentTest      │   │  PromptTemplate    │
└─────────────────────┘   │  Answer              │   └────────────────────┘
                          │  TestResult          │
┌─────────────────────┐   └──────────────────────┘   ┌────────────────────┐
│  Test Catalog       │                              │  Identity & Audit  │
│  ─────────────────  │                              │  ────────────────  │
│  TestDefinition     │                              │  AdminUser         │
│  Question           │                              │  RefreshToken      │
│  AnswerOption       │                              │  AuditLog          │
│  TypeCatalog        │                              └────────────────────┘
│  CareerMap          │
└─────────────────────┘
```

---

## 2. Agregatlar va entity'lar

### 2.1 `School` (agregat ildizi)

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | PK |
| `Name` | `string(200)` | "12-son umumiy o'rta ta'lim maktabi" |
| `Region` | `string(100)` | Viloyat |
| `District` | `string(100)` | Tuman/shahar |
| `SchoolNumber` | `string(20)?` | |
| `ContactPerson` | `string(150)?` | Mas'ul shaxs FISH |
| `ContactPhone` | `string(20)?` | |
| `Slug` | `string(80)` | **Unikal**, URL uchun: `12-maktab-kokand` |
| `AccessToken` | `string(64)` | Kriptografik random, havolada `?k=` |
| `AccessCode` | `string(6)?` | Ixtiyoriy qo'shimcha kod |
| `DailyRegistrationLimit` | `int` | Default 500 |
| `IsActive` | `bool` | |
| `Notes` | `string(1000)?` | |
| `CreatedAt` / `UpdatedAt` | `DateTimeOffset` | |

**Invariantlar**
- `Slug` global unikal, faqat `[a-z0-9-]`.
- `AccessToken` qayta generatsiya qilinsa eskisi darhol yaroqsiz (jadvalda yagona ustun, tarixi `AuditLog` da).
- `IsActive = false` → ommaviy API `410 Gone`.

**Metodlar:** `RegenerateAccessToken()`, `Deactivate()`, `Activate()`.

---

### 2.2 `Student` (agregat ildizi)

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | |
| `SchoolId` | `Guid` | FK → School |
| `FullName` | `string(200)` | FISH, kiritilgani bo'yicha |
| `NormalizedName` | `string(200)` | Katta harf, ortiqcha probel olib tashlangan — dublikat uchun |
| `BirthDate` | `DateOnly` | |
| `Gender` | `enum` | `Male`, `Female`, `Unspecified` |
| `Grade` | `int` | 1..11 |
| `ClassLetter` | `string(2)?` | "A", "B" |
| `Phone` | `string(20)` | |
| `ParentPhone` | `string(20)?` | |
| `Email` | `string(150)?` | |
| `ConsentGivenAt` | `DateTimeOffset` | Rozilik vaqti |
| `CreatedAt` / `UpdatedAt` | | |

**Hosila (denormalizatsiya, tez ro'yxat uchun) — `StudentSnapshot`:**
`LastPersonalityType`, `LastMaturityIndex`, `LastActivityIndex`, `LastActivityLevel`,
`NeedsAttention`, `LastAssessmentAt`, `CompletedAssessmentCount`.
Sessiya `Analyzed` bo'lganda yangilanadi.

**Invariantlar**
- `(SchoolId, NormalizedName, BirthDate)` — mantiqiy unikal (unique index).
- `Grade ∈ [1..11]`.
- `ConsentGivenAt` bo'lmasa student yaratilmaydi.

---

### 2.3 `Assessment` (agregat ildizi — asosiy)

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | |
| `StudentId` / `SchoolId` | `Guid` | |
| `SessionToken` | `string(64)` | Unikal, o'quvchi brauzerida saqlanadi |
| `Status` | `enum` | pastda |
| `LanguageCode` | `string(5)` | `uz`, `ru` |
| `StartedAt` | `DateTimeOffset` | |
| `CompletedAt` | `DateTimeOffset?` | |
| `ExpiresAt` | `DateTimeOffset` | `StartedAt + 7 kun` |
| `ReliabilityScore` | `double?` | 0–100 |
| `ReliabilityFlag` | `enum?` | `Reliable`, `Questionable`, `Unreliable` |
| `IpHash` / `UserAgent` | `string?` | Suiiste'mol tahlili uchun (IP xeshlangan) |
| `TotalDurationSeconds` | `int?` | |

**Holat mashinasi**

```
Draft ──startFirstTest──▶ InProgress ──allTestsDone──▶ Completed
                              │                            │
                              │                            ├─enqueue──▶ Analyzing
                              │                            │              │
                              ▼                            │              ├─ok──▶ Analyzed
                          Abandoned ◀──expired─────────────┘              └─fail─▶ AnalysisFailed
```

| Holat | Ma'nosi |
|-------|---------|
| `Draft` | O'quvchi ro'yxatdan o'tdi, hali test boshlamadi |
| `InProgress` | Kamida bitta test boshlangan |
| `Completed` | Barcha testlar yakunlandi, ballar hisoblandi |
| `Analyzing` | AI navbatda / ishlayapti |
| `Analyzed` | AI hisobot tayyor |
| `AnalysisFailed` | AI 3 urinishdan keyin ham bermadi (ballar bor, hisobot yo'q) |
| `Abandoned` | `ExpiresAt` o'tdi, yakunlanmadi |

**Invariantlar**
- `Completed` ga faqat barcha faol `AssessmentTest` lar `Completed` bo'lsa o'tadi.
- `Analyzed` holatida kamida bitta `AiAnalysis` `Succeeded` bo'lishi shart.
- Muddati o'tgan sessiyaga javob yozib bo'lmaydi.

---

### 2.4 `AssessmentTest`

| Maydon | Tip |
|--------|-----|
| `Id` | `Guid` |
| `AssessmentId` | `Guid` |
| `TestDefinitionId` | `Guid` |
| `Status` | `NotStarted`, `InProgress`, `Completed` |
| `DisplayOrder` | `int` |
| `StartedAt` / `CompletedAt` | `DateTimeOffset?` |
| `AnsweredCount` / `TotalCount` | `int` |
| `QuestionOrderJson` | `jsonb` — aralashtirilgan tartib (qayta kirganda bir xil bo'lishi uchun) |

---

### 2.5 `Answer`

| Maydon | Tip |
|--------|-----|
| `Id` | `Guid` |
| `AssessmentTestId` | `Guid` |
| `QuestionId` | `Guid` |
| `RawValue` | `int` (Likert) |
| `SelectedOptionId` | `Guid?` (SingleChoice uchun) |
| `DurationMs` | `int` |
| `AnsweredAt` | `DateTimeOffset` |
| `RevisionCount` | `int` — necha marta o'zgartirilgan |

**Invariant:** `(AssessmentTestId, QuestionId)` unikal — upsert.

---

### 2.6 `TestResult`

| Maydon | Tip |
|--------|-----|
| `Id` | `Guid` |
| `AssessmentTestId` | `Guid` (unikal) |
| `AssessmentId` | `Guid` (tez so'rov uchun) |
| `TestCode` | `string(20)` |
| `ResultCode` | `string(20)?` — `INTJ`, `IRA` |
| `RawScoresJson` | `jsonb` |
| `NormalizedScoresJson` | `jsonb` |
| `LevelsJson` | `jsonb` |
| `CompositeIndex` | `double?` |
| `FlagsJson` | `jsonb` |
| `ScoringVersion` | `int` |
| `TestVersion` | `int` — natija hisoblangan paytdagi `TestDefinition.Version` (BR-9) |
| `ComputedAt` | `DateTimeOffset` |

---

### 2.7 `TestDefinition`, `Question`, `AnswerOption`

**TestDefinition:** `Id`, `Code` (unikal), `Name`, `Description`, `Version`, `DisplayOrder`,
`QuestionCount`, `EstimatedMinutes`, `ShuffleQuestions`, `PageSize`, `IsActive`,
**`Kind`** (`Standard` | `Custom`), **`IsSystem`** (seed'dan kelgan, o'chirilmaydi),
**`ScoringStrategyCode`** (`MBTI16`|`BIG5`|`RIASEC`|`ACTIVITY`|`SUM`),
**`Status`** (`Draft` | `Published` | `Archived`), `CreatedByAdminUserId`, `PublishedAt`.

**Metodlar:** `AddQuestion()`, `RemoveQuestion()`, `Publish()` (validatsiya bilan),
`Archive()`, `Duplicate()`. Tizim testida (`IsSystem = true`) `AddQuestion`/`RemoveQuestion`
va shkala o'zgartirish `DomainException` beradi (BR-8).

**TestScale** (faqat `Custom` testlar uchun): `Id`, `TestDefinitionId`, `Code`, `NameUz`,
`DescriptionUz`, `DisplayOrder`, `InterpretationBandsJson`
(`[{ "from":0, "to":33, "label":"Past" }, …]`).

**Question:** `Id`, `TestDefinitionId`, `Code`, `DisplayOrder`, `TextUz`, `TextRu?`, `TextEn?`,
`QuestionType`, `Scale`, `ScaleDirection` (+1/−1), `Weight`, `IsRequired`, `IsActive`,
**`IsSystem`** (seed'dan kelgan savol — o'chirilmaydi, `Scale`/`Direction` o'zgarmaydi).

**AnswerOption** (faqat `SingleChoice`/`ForcedChoice`): `Id`, `QuestionId`, `TextUz`, `Value`, `Scale?`, `DisplayOrder`.

**TypeCatalog:** `Code` (`INTJ`), `NameUz` ("Loyihachi"), `ShortDescriptionUz`, `LongDescriptionUz`,
`StrengthsJson`, `GrowthAreasJson`, `CareerHintsJson`.

**CareerMap:** `HollandCode` (2 harf), `FieldNameUz`, `DescriptionUz`, `ExampleProfessionsJson`, `RelevanceOrder`.

---

### 2.8 `AiAnalysis`

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | |
| `AssessmentId` | `Guid` | |
| `Provider` | `enum` | `Gemini`, `OpenAi`, `Anthropic` |
| `Model` | `string(80)` | |
| `PromptVersion` | `string(20)` | `v1.0` |
| `Status` | `enum` | `Pending`, `Running`, `Succeeded`, `Failed` |
| `RequestPayloadJson` | `jsonb` | Yuborilgan strukturalangan ma'lumot (prompt matni emas) |
| `ResponseJson` | `jsonb` | Schema bo'yicha to'liq javob |
| `Summary` | `text` | Tez ko'rsatish uchun ajratilgan |
| `PersonalityPortrait` | `text` | |
| `StrengthsJson` / `GrowthAreasJson` / `RecommendationsJson` | `jsonb` | |
| `CareerSuggestionsJson` | `jsonb` | |
| `TeacherNotes` / `ParentNotes` | `text` | |
| `AttentionFlagsJson` | `jsonb` | |
| `InputTokens` / `OutputTokens` | `int?` | |
| `EstimatedCostUsd` | `decimal(10,6)?` | |
| `DurationMs` | `int?` | |
| `ErrorMessage` | `string(2000)?` | |
| `AttemptNumber` | `int` | |
| `IsCurrent` | `bool` | Oxirgi muvaffaqiyatli tahlil |
| `CreatedAt` | `DateTimeOffset` | |

**Invariant:** bitta `Assessment` uchun `IsCurrent = true` bo'lgan yozuv faqat bitta.

---

### 2.9 `AiProviderConfig`

`Id`, `Provider`, `DisplayName`, `ApiKeyEncrypted`, `Model`, `BaseUrl?`, `MaxOutputTokens`,
`Temperature`, `IsDefault`, `IsActive`, `FallbackOrder` (int), `LastCheckedAt`, `LastCheckStatus`.

**Invariant:** `IsDefault = true` faqat bitta faol yozuvda.

---

### 2.10 `AdminUser`, `RefreshToken`, `AuditLog`

**AdminUser:** `Id`, `Username` (unikal), `Email`, `PasswordHash`, `Role` (`SuperAdmin`),
`FullName`, `IsActive`, `TotpSecretEncrypted?`, `TotpEnabled`, `LastLoginAt`, `FailedLoginCount`, `LockedUntil?`.

**RefreshToken:** `Id`, `AdminUserId`, `TokenHash`, `ExpiresAt`, `RevokedAt?`, `CreatedByIpHash`.

**AuditLog:** `Id`, `AdminUserId?`, `Action` (`School.Created`, `Link.Regenerated`, `Student.Deleted`,
`Analysis.Rerun`, `AiConfig.Updated`), `EntityType`, `EntityId`, `BeforeJson?`, `AfterJson?`,
`IpHash`, `UserAgent`, `CreatedAt`.

---

## 3. Domen hodisalari

| Hodisa | Qachon | Kim tinglaydi |
|--------|--------|---------------|
| `AssessmentStartedEvent` | Sessiya `Draft` yaratildi | Statistika |
| `TestCompletedEvent` | Bitta test yakunlandi | Scoring handler → `TestResult` |
| `AssessmentCompletedEvent` | Barcha testlar tugadi | Reliability hisoblash → AI navbatga qo'yish |
| `AiAnalysisSucceededEvent` | AI javob berdi | `StudentSnapshot` yangilash, `Status = Analyzed` |
| `AiAnalysisFailedEvent` | 3 urinish ham muvaffaqiyatsiz | `Status = AnalysisFailed`, alert log |
| `SchoolLinkRegeneratedEvent` | Havola yangilandi | Audit |
| `TestPublishedEvent` | Superadmin anketani nashr qildi | Katalog keshini tozalash, audit |
| `TestVersionBumpedEvent` | Nashr qilingan testga savol qo'shildi/olib tashlandi | Audit |

---

## 4. Value object'lar

- `SchoolSlug` — validatsiya + normalizatsiya.
- `PhoneNumber` — O'zbekiston formati `+998XXXXXXXXX`, normalizatsiya.
- `PersonalityType` — 4 harf, validatsiya, `TypeCatalog` bilan bog'lanish.
- `HollandCode` — 3 harf, `RIASEC` alifbosidan.
- `ScorePercent` — 0..100 oralig'i kafolatlangan.

---

## 5. Yumshoq o'chirish (soft delete)

- `School`, `Student`, `Assessment` — `IsDeleted`, `DeletedAt`, `DeletedByAdminUserId`.
- Global query filter EF Core'da: `HasQueryFilter(e => !e.IsDeleted)`.
- **Istisno:** o'quvchi "ma'lumotimni o'chiring" desa — **hard delete** + `AuditLog` ga faqat
  `StudentId` va vaqt yoziladi (shaxsiy ma'lumotsiz).
