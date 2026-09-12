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
| `Kind` | `enum` | **P47** — `School = 1` (maktab havolasi oqimi), `PublicSpace = 2` (ommaviy makon). Bazada AYNAN BITTA `PublicSpace` |
| `AccessToken` | `string(64)` | Kriptografik random, havolada `?k=` |
| `AccessCode` | `string(6)?` | Ixtiyoriy qo'shimcha kod |
| `EntryCode` | `string(8)?` | **Maktab kodi** (2026-09-07) — `/kirish` → "Maktab uchun" yo'li. `Kind = School` uchun HAR DOIM to'ldirilgan (yaratishda avtomatik), `PublicSpace` uchun `null`. Alifbo `SchoolEntryCode.Alphabet` (31 belgi, `0 O 1 I L` yo'q), saqlashda defissiz, ko'rsatishda `XXXX-XXXX`. `AccessCode` bilan ALOQASIZ |
| `DailyRegistrationLimit` | `int` | Default 500 |
| `IsActive` | `bool` | |
| `ShowResultToStudent` | `bool` | **P47** — natija o'quvchiga ko'rsatiladimi. Ilgari GLOBAL `App:ShowResultToStudent` edi; maktablarda default `false`, ommaviy makonda `true` |
| `Notes` | `string(1000)?` | |
| `CreatedAt` / `UpdatedAt` | `DateTimeOffset` | |

**Invariantlar**
- `Slug` global unikal, faqat `[a-z0-9-]`.
- `AccessToken` qayta generatsiya qilinsa eskisi darhol yaroqsiz (jadvalda yagona ustun, tarixi `AuditLog` da).
- `IsActive = false` → ommaviy API `410 Gone`.
- **P47:** `Kind = PublicSpace` bo'lgan yozuv o'chirilmaydi va faolsizlantirilmaydi
  (`DomainException("SCHOOL_PUBLIC_SPACE_PROTECTED")`); bunday yozuv bazada faqat BITTA
  bo'ladi (`ux_schools_public_space` qisman unikal indeksi — poyga holatiga qarshi yagona
  haqiqiy himoya; domen tekshiruvi niyatni ifodalaydi, DB esa uni kafolatlaydi).

- **2026-09-07:** `EntryCode` maktabni ANIQLAYDI (`ux_schools_entry_code` qisman unikal indeks,
  `entry_code IS NOT NULL`) va havola bilan BIR XIL huquq beradi — `resolve-code` uni
  `{ slug, accessToken }` ga aylantiradi. Qayta generatsiya qilinsa eskisi darhol yaroqsiz.
  Tasodifiylik domen tashqarisida (`Infrastructure.Security.EntryCodeGenerator`), domen
  (`SchoolEntryCode`) faqat formatni tekshiradi/normalizatsiya qiladi.

**Metodlar:** `RegenerateAccessToken()`, `RegenerateEntryCode()`, `Deactivate()`, `Activate()`, `SetShowResultToStudent()`.

**Fabrikalar:** `Create()` — har doim `Kind = School`; `CreatePublicSpace()` — yagona ommaviy
makon (faqat `DbSeeder.SeedPublicSpaceAsync` chaqiradi, slug `ommaviy`, `ShowResultToStudent = true`).

---

### 2.2 `Student` (agregat ildizi)

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | |
| `SchoolId` | `Guid` | FK → School (ommaviy oqimda — ommaviy makon `Id`si) |
| `PublicUserId` | `Guid?` | **P47** — FK → `PublicUser`. Maktab oqimida `null` |
| `IsAnonymous` | `bool` | **P52** — `AssessmentProgram.RegistrationMode.None` dasturi orqali yaratilgan (`Student.CreateAnonymous`). `true` bo'lsa `BirthDate`/`Phone` DOIM `null` |
| `FullName` | `string(200)` | FISH, kiritilgani bo'yicha. Anonim o'quvchida PII EMAS: `"Anonim ishtirokchi #XXXXXX"` (o'z `Id`sidan olingan 6 belgili qo'shimcha — ro'yxatda qatorlar ajralib tursin) |
| `NormalizedName` | `string(200)` | Katta harf, ortiqcha probel olib tashlangan — dublikat uchun |
| `BirthDate` | `DateOnly?` | **P52** — NULLABLE: anonim o'quvchida `null` |
| `Gender` | `enum` | `Male`, `Female`, `Unspecified` |
| `Grade` | `int` | 1..11, yoki `0` = `Student.NoGrade` (**P47** — maktabda o'qimaydigan ommaviy foydalanuvchi; **P52** — anonim o'quvchida ham shu sentinel) |
| `ClassLetter` | `string(2)?` | "A", "B" |
| `Phone` | `string(20)?` | **P52** — NULLABLE: anonim o'quvchida `null` |
| `ParentPhone` | `string(20)?` | |
| `Email` | `string(150)?` | |
| `ConsentGivenAt` | `DateTimeOffset` | Rozilik vaqti (topshiriqdagi `ConsentAcceptedAt` — AYNAN shu maydon, dublikat ustun qo'shilmadi) |
| `ConsentVersion` | `string(30)?` | **P47** — qabul qilingan rozilik matni versiyasi (`2026-09-v1`) |
| `ParentalConsent` | `bool` | **P47** — voyaga yetmagan uchun ota-ona/vasiy roziligi. Rozilik SESSIYAGA emas, SHAXSGA tegishli — shu sabab `Assessment` da emas |
| `ProfileExtra` | `jsonb?` (xom matn, Domain talqin qilmaydi) | **P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2, egasining qarori)** — GLOBAL ro'yxatdan o'tish formasidagi (`RegistrationFormSettings.Definition.CustomFields`) superadmin qo'shgan "o'z maydonlari" javoblari, kod → qiymat: `{"PARENT_JOB":"O'qituvchi","TRANSPORT":[1,3]}` (matn turlarida satr, `SingleChoice`da `RegistrationCustomFieldOption.Order` (butun son), `MultiChoice`da shunday sonlar massivi). Har doim BUTUNLIGICHA o'qiladi/yoziladi, hech qachon qidirilmaydi — shu sabab alohida jadval EMAS, oddiy `jsonb` ustun (`AiAnalysis.ResponseJson` bilan bir xil naqsh). `null` — javob yo'q/sozlamada `customFields` yo'q |
| `CreatedAt` / `UpdatedAt` | | |

**Hosila (denormalizatsiya, tez ro'yxat uchun) — `StudentSnapshot`:**
`LastPersonalityType`, `LastMaturityIndex`, `LastActivityIndex`, `LastActivityLevel`,
`NeedsAttention`, `LastAssessmentAt`, `CompletedAssessmentCount`.
Sessiya **`Completed`** bo'lganda (ya'ni `CompleteSession` da, scoring tugagach) yangilanadi — snapshot maydonlarining hammasi deterministik ballardan kelib chiqadi, AI natijasiga bog'liq emas. (P12 da qat'iylashtirildi; avval "`Analyzed` bo'lganda" deb yozilgan edi.)

**Invariantlar**
- `(SchoolId, NormalizedName, BirthDate)` — mantiqiy unikal (unique index), **P47 dan boshlab
  faqat maktab oqimida** (indeks filtri: `is_deleted = false AND public_user_id IS NULL`).
  Ommaviy makonda identifikator — Telegram akkaunti, ism emas.
- **P47:** `PublicUserId` bo'yicha bitta akkauntga BITTA o'quvchi profili
  (`ux_students_public_user`). 90 kunlik qayta-topshirish oynasi ommaviy oqimda shu profil
  bo'yicha ishlaydi — qoida o'zgarmaydi, faqat o'quvchini topish kaliti almashadi.
- `Grade ∈ [0..11]` (`0` = sinf yo'q).
- Yosh `Student.MinAge`..`Student.MaxAge` = **6..99** (ilgari validator 6–20 talab qilardi —
  kattalar ro'yxatdan o'ta olmasdi). Domen `CalculateAge`/`IsAgeAllowed` yordamchilarini beradi.
- `ConsentGivenAt` bo'lmasa student yaratilmaydi.
- **P52 (2026-09-11) → P52 kengaytmasi (2026-09-11, `docs/18` §9.5) bilan YECHILDI:** ilgari
  `IsAnonymous == false` bo'lsa `BirthDate` va `Phone` IKKALASI HAM to'ldirilgan bo'lishi SHART
  edi (`DomainException("STUDENT_IDENTITY_REQUIRED")`). `RegistrationFields` kiritilgach bu
  invariant OLIB TASHLANDI: `birthDate`/`phone` HAR MAYDON kabi dasturga qarab
  `Optional`/`Hidden` bo'lishi mumkin, ya'ni anonim BO'LMAGAN (`FullName` bor) o'quvchida ham
  ular `null` bo'lishi LEGITIM. `Student.Create` parametrlari shu sabab `DateOnly?`/`PhoneNumber?`
  (avval `DateOnly`/`PhoneNumber`) — majburiylik endi Application qatlamida
  (`StartSessionCommandHandler.ValidateRequiredIdentityFields`, dastur `RegistrationFields`
  sozlamasiga qarab) tekshiriladi, domen bu yerda cheklamaydi.

**Metodlar:** `UpdateSnapshot()`, `MarkDeleted()`, `LinkToPublicUser()` (**P47** — eski yozuvni
Telegram akkauntga ulash; boshqa akkauntga bog'langan yozuv qayta bog'lanmaydi:
`DomainException("STUDENT_ALREADY_LINKED")`), `RecordConsent()`.

**Fabrikalar:** `Create()` — to'liq profil (`IsAnonymous = false`; `BirthDate`/`Phone` odatda
to'ldirilgan, lekin `RegistrationFields` `Optional`/`Hidden` qilgan bo'lsa `null` ham bo'lishi
mumkin, `docs/18` §9.5);
`CreateAnonymous()` (**P52**) — `RegistrationMode.None` dasturi uchun, shaxs maydonlarisiz
(`Grade = NoGrade`, `Gender = Unspecified`, `BirthDate`/`Phone = null`), faqat maktab oqimida
(`StartSessionCommandHandler`) chaqiriladi. BR-1 (90 kunlik takror topshirish) va sessiyani
identifikator bo'yicha davom ettirish anonim yozuvda ISHLAMAYDI (`NormalizedName`+`BirthDate`
yo'q) — qabul qilingan cheklov: har so'rov yangi anonim `Student` yaratadi.

---

### 2.3 `Assessment` (agregat ildizi — asosiy)

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | |
| `StudentId` / `SchoolId` | `Guid` | |
| `SessionToken` | `string(64)` | Unikal, o'quvchi brauzerida saqlanadi. **P47:** ochiq matnli ustun eskirgan — 2-bosqichda o'chiriladi |
| `SessionTokenHash` | `string(64)` | **P47** — tokenning SHA-256 xeshi (`Common/TokenHash`). Domen `Create`/`RotateSessionToken` da O'ZI hisoblaydi |
| `Status` | `enum` | pastda |
| `LanguageCode` | `string(5)` | `uz`, `ru` |
| `StartedAt` | `DateTimeOffset` | |
| `CompletedAt` | `DateTimeOffset?` | |
| `ExpiresAt` | `DateTimeOffset` | `StartedAt + 7 kun` |
| `ReliabilityScore` | `double?` | 0–100 |
| `ReliabilityFlag` | `enum?` | `Reliable`, `Questionable`, `Unreliable` |
| `IpHash` / `UserAgent` | `string?` | Suiiste'mol tahlili uchun (IP xeshlangan) |
| `TotalDurationSeconds` | `int?` | |

**P47 — sessiya tokenini xeshlash:** ilgari token bazada OCHIQ saqlanardi (refresh tokenlardan
farqli — `docs/08` 2-bo'lim), ya'ni DB nusxasi sizib chiqsa barcha faol sessiyalar bevosita
ochilardi. Endi domen xeshni avtomatik hisoblaydi. Xeshdan xom token qayta tiklanmagani uchun
"mavjud sessiyani davom ettirish" (`resumed`) oqimi eski tokenni QAYTARA OLMAYDI —
`RotateSessionToken()` bilan yangi token beriladi (bu ayni paytda xavfsizroq ham).

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
| `RawValue` | `int?` (Likert/tanlov). Matn va ko'p tanlovli javobda `null` — P52 |
| `TextValue` | `string?` (≤4000) — `ShortText`/`LongText`/`Phone` javobi (P52) |
| `SelectedValues` | `IReadOnlyList<int>` (`jsonb`) — `MultiChoice` javobi (P52) |
| `SelectedOptionId` | `Guid?` (SingleChoice uchun) |
| `DurationMs` | `int` |
| `AnsweredAt` | `DateTimeOffset` |
| `RevisionCount` | `int` — necha marta o'zgartirilgan |

**Invariant 1:** `(AssessmentTestId, QuestionId)` unikal — upsert.

**Invariant 2 (P52):** `RawValue` / `TextValue` / `SelectedValues` dan **aynan bittasi**
to'ldirilgan bo'lishi shart — aks holda `DomainException("ANSWER_SHAPE_INVALID")`. DB
darajasida ham `ck_answers_shape` CHECK cheklovi bilan qulflangan (`docs/05`).

`AssessmentTest.RemoveAnswers(questionIds)` — yakunlashda **yashirilgan** savollarning
javoblarini o'chiradi va `AnsweredCount` ni mos kamaytiradi (`docs/18` §4.3).

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
**`IsSystem`** (seed'dan kelgan savol — o'chirilmaydi, `Scale`/`Direction` o'zgarmaydi),
va P52 da qo'shilganlar: **`SectionId`** (`Guid?`), **`VisibilityRule`** (`VisibilityRule?`),
**`Placeholder`**, **`InputPattern`**, **`MaxLength`**, **`MinSelections`**, **`MaxSelections`**.

**AnswerOption** (`SingleChoice`/`ForcedChoice`/**`MultiChoice`** — oxirgisi P52 da qo'shildi):
`Id`, `QuestionId`, `TextUz`, `Value`, `Scale?`, `DisplayOrder`.

**QuestionSection** (P52, `TestDefinition` agregati ichidagi bola): `Id`, `TestDefinitionId`,
`Code` (anketa ichida unikal), `TitleUz`, `DescriptionUz?`, `DisplayOrder`,
`VisibilityRule?`. Agregat metodlari: `AddSection()`, `RemoveSection()` (savollari bo'lsa
`SECTION_IN_USE`), `MoveQuestionToSection()`, `ReorderSections()` — hammasi tizim
metodikasida `SYSTEM_TEST_LOCKED` (BR-8).

> Bo'limlar, ko'rsatish sharti va yangi savol turlarining **to'liq** shartnomasi —
> `docs/18-tarmoqlanuvchi-sorovnoma.md`. Qat'iy chegara: matn/ko'p tanlovli turlar va
> `VisibilityRule` faqat `ScoringMode = Survey` anketalarda (B-1/B-2).

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
`FullName`, `IsActive`, `TotpSecretEncrypted?`, `TotpEnabled`, `LastLoginAt`, `FailedLoginCount`, `LockedUntil?`,
`PendingTotpSecretEncrypted?`, `PendingTotpCreatedAt?` (P46 — tasdiqlanmagan 2FA o'rnatishi: sir
`BeginTotpEnrollment` bilan kutish holatiga yoziladi va faqat `ConfirmTotpEnrollment` da
asosiy maydonga ko'chib, `TotpEnabled` `true` bo'ladi; muddati 10 daqiqa).

**RefreshToken:** `Id`, `AdminUserId`, `TokenHash`, `ExpiresAt`, `RevokedAt?`, `CreatedByIpHash`.

**AuditLog:** `Id`, `AdminUserId?`, `Action` (`School.Created`, `Link.Regenerated`, `Student.Deleted`,
`Analysis.Rerun`, `AiConfig.Updated`), `EntityType`, `EntityId`, `BeforeJson?`, `AfterJson?`,
`IpHash`, `UserAgent`, `CreatedAt`.

---

### 2.11 `PublicUser` (agregat ildizi) — P47

Telegram orqali kiradigan TASHQI foydalanuvchi. Superadmin (`AdminUser`) bilan aralashtirilmaydi:
alohida jadval, alohida refresh token jadvali, alohida JWT rol/policy.

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | PK |
| `TelegramId` | `long?` | **Unikal** (qisman indeks: `telegram_id IS NOT NULL`). Yaratishda majburiy; o'chirishda tozalanadi |
| `Username` | `string(64)?` | Telegram bermasligi mumkin |
| `FirstName` / `LastName` | `string(100)?` | |
| `PhotoUrl` | `string(500)?` | |
| `CreatedAt` / `UpdatedAt` | `DateTimeOffset` | |
| `LastLoginAt` | `DateTimeOffset` | Yaratilishda ham to'ldiriladi (kirish = ro'yxatdan o'tish) |
| `DeletedAt` | `DateTimeOffset?` | Yumshoq o'chirish; `IsDeleted` — hisoblanadigan xususiyat |
| `DeletionReason` | `PublicUserDeletionReason?` | O'chirish sababi (2026-09-08, egasining qarori) — anonimlashtirish bilan TOZALANMAYDI |
| `DeletionComment` | `string(500)?` | Ixtiyoriy erkin matnli izoh; `Other` sababida MAJBURIY (validatorda) |

**`PublicUserDeletionReason` enum** (`smallint`, raqamlar `docs/05` 3-bo'limi bilan bir xil):
`NoLongerNeeded` (1, "Endi kerak emas"), `NotUseful` (2, "Natijalar foydali bo'lmadi"),
`PrivacyConcern` (3, "Ma'lumotlarim saqlanishini xohlamayman"), `CreatedByMistake`
(4, "Xato bilan ro'yxatdan o'tganman"), `Other` (5, "Boshqa sabab" — izoh MAJBURIY).

**Invariantlar**
- `TelegramId > 0` (yaratilishda).
- O'chirilgan akkauntda hech qanday harakat qilinmaydi (`DomainException("PUBLIC_USER_DELETED")`).

**Metodlar:** `Create()`, `RecordLogin()`, `MarkDeleted(now, reason, comment?)`.

**Nima uchun yumshoq o'chirish + anonimlashtirish (qattiq o'chirish emas):** `students.public_user_id`
FK'si va test tarixi (`docs/08`: natijalar 5 yil saqlanadi) qattiq o'chirishda buzilardi.
`MarkDeleted` shaxsni aniqlovchi BARCHA maydonni (Telegram ID ham) tozalaydi — ya'ni GDPR
ma'nosidagi "o'chirish" bajariladi, statistika esa anonim qoladi. Telegram ID tozalangani uchun
bir xil foydalanuvchi qayta kirsa YANGI akkaunt oladi (kutilgan xatti-harakat).

**Sabab/izoh anonimlashtirilmaydi (2026-09-08):** egasining qarori bo'yicha o'chirish
SO'RALGANDA sabab olinadi va u superadmin ro'yxatida ("nega o'chirilgan") ko'rinishi kerak —
shu sabab `DeletionReason`/`DeletionComment` boshqa profil maydonlaridan farqli o'laroq
`MarkDeleted`da TOZALANMAYDI. Idempotent: takroriy chaqiruvda sabab/izoh QAYTA YOZILMAYDI
(birinchi o'chirishdagi qiymat — haqiqat manbai).

---

### 2.12 `PublicRefreshToken` — P47

`Identity/RefreshToken` naqshini AYNAN takrorlaydi (SHA-256 xesh, rotatsiya,
qayta-ishlatishni aniqlash, muddat), yagona farq — FK `PublicUserId`.

`Id`, `PublicUserId`, `TokenHash` (unikal), `ExpiresAt`, `RevokedAt?`, `CreatedByIpHash?`, `CreatedAt`.
Ikki marta bekor qilishga urinish — `DomainException("PUBLIC_REFRESH_TOKEN_ALREADY_REVOKED")`.

---

### 2.13 `AssessmentProgram` (agregat ildizi) — P34, holat 2026-09-06 da soddalashtirilgan, arxivdan tiklash qo'shilgan

Dastur — nomlangan, tartiblangan test to'plami. O'quvchi kirishda dasturni tanlaydi va
sessiyaga faqat shu dasturning testlari qo'shiladi (`Assessment.ProgramId`).

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | PK |
| `Code` | `string(50)` | **Unikal**, yaratilgandan keyin o'zgarmaydi |
| `NameUz` / `DescriptionUz` | `string` | |
| `Kind` | `ProgramKind` | `System = 1` · `Custom = 2` |
| `Visibility` | `ProgramVisibility` | `Public = 1` (barcha maktabda) · `Assigned = 2` (faqat biriktirilganda) |
| `RegistrationMode` | `RegistrationMode` | **P52** — `Full = 1` (standart: o'quvchi ro'yxatdan o'tish anketasini to'ldiradi) · `None = 2` (registratsiya ekrani ko'rsatilmaydi, `Student` ANONIM yaratiladi — faqat batareyasiz dasturda ruxsat, pastga qarang) |
| `RegistrationFields` | `RegistrationFields?` | ⚠️ **O'LIK (2026-09-12 dan) — GLOBAL sozlama bilan almashtirildi.** P52 kengaytmasida (`docs/18` §9.5) har DASTURDA alohida sozlanadigan qilib qo'shilgan edi, lekin egasi bir kundan keyin buni GLOBAL sozlamaga (`RegistrationFormSettings`, §2.14) o'tkazishga qaror qildi. **2-to'lqin (joriy, P52 2-to'lqin):** `StartSessionCommandHandler`/`GetSchoolInfoQueryHandler` ENDI BU MAYDONNI O'QIMAYDI (`RegistrationFormResolver` — GLOBAL sozlama, dastur ustunligi bilan). Admin `CreateProgram`/`UpdateProgram` hamon YOZADI/O'QIYDI (xossa/ustun `docs/06` 7-qoidasiga ko'ra hozircha saqlanadi, destruktiv o'zgarish ikki bosqichda) — lekin yozilgan qiymat endi HECH QANDAY sessiya oqimiga ta'sir qilmaydi. Ustunni o'chirish alohida keyingi migratsiyaga qoldirilgan (`PROGRESS.md` risklar jadvali) |
| `Status` | `ProgramStatus` | `Draft = 1` · `Published = 2` · `Archived = 3` — **saqlash maydoni** |
| `IsActive` | `bool` | **saqlash maydoni** |
| `IsSystem` | `bool` | Seed'dan kelgan tizim dasturi; tarkibi qulflangan (`SYSTEM_PROGRAM_LOCKED`) |
| `DisplayOrder` | `int` | |

#### Dastur holati — TASHQARIGA BITTA qiymat (`ProgramState`)

`Status` va `IsActive` bazada QOLADI (ommaviy oqim va havola sog'ligi mezoni ularga tayanadi:
`ProgramAvailability`, `SchoolLinkHealthEvaluator`), lekin **admin API va UI faqat bitta hosila
holatni ko'radi** — `AssessmentProgram.State` (`ProgramStateRules.Resolve`):

| `ProgramState` | Shart | UI matni |
|----------------|-------|----------|
| `Draft` | `Status == Draft` | Qoralama |
| `Active` | `Status == Published && IsActive` | Faol |
| `Paused` | `Status == Published && !IsActive` | To'xtatilgan |
| `Archived` | `Status == Archived` (`IsActive` e'tiborga olinmaydi) | Arxiv |

> **Nima uchun:** 2026-09-06 gacha admin ro'yxatida ikkita mustaqil ustun bor edi va bitta
> dastur bir vaqtda "Arxiv" ham, "Faol" ham bo'lib ko'rinardi. Sabab — qo'riqchisiz
> `Activate()`: arxivlangan dasturni faollashtirib, `status = 3 AND is_active = true`
> qatorini hosil qilish mumkin edi (egasining bazasida shunday qator bor edi).

**Holat mashinasi** (boshqa o'tish YO'Q — `DomainException("PROGRAM_INVALID_TRANSITION")`):

```
Draft ──Publish()──▶ Active ──Deactivate()──▶ Paused ──Activate()──▶ Active
  │                    │                        ▲  │
  │                    │           Restore()    │  │
  └──Archive()─────────┴────────────────────────┼──┴──────▶ Archived
                                                └──────────────┘
```

**Invariantlar**
- `Publish()` faqat `Draft` dan; kamida bitta test biriktirilgan bo'lishi shart
  (`PROGRAM_NOT_PUBLISHABLE`). Natija DOIM `Active`: `Publish()` `IsActive`ni ANIQ `true`
  qiladi — "nashr qilish" adminning dasturni o'quvchilarga ochish qarori.
- `Activate()` / `Deactivate()` faqat `Status == Published` da. Qoralamani ham, arxivlangan
  dasturni ham "to'xtatib"/"faollashtirib" bo'lmaydi.
- `Archive()` `Draft` va `Published` dan; `IsActive`ni `false` qiladi. Tarkib (`Tests`) va
  maktab biriktirishlari (`school_programs`) **saqlanib qoladi** — arxiv "o'quvchiga
  ko'rinmaydi" degani, "aloqalar uzildi" degani emas.
- `Restore()` (2026-09-06) faqat `Archived` dan va **faqat `Paused` ga** (`Published +
  IsActive = false`). `Active` ga EMAS: biriktirishlar saqlangani uchun bir bosishda `Active`
  ga tiklash dasturni o'sha maktablar uchun darhol jonli qilib qo'yardi. Tiklash va
  faollashtirish — ikki alohida admin qarori: `Restore()` → tarkib/biriktirishlarni ko'rib
  chiqish → `Activate()`. `PROGRAM_NOT_PUBLISHABLE` sharti tiklashda qayta tekshirilmaydi
  (bo'sh dastur `Paused` bo'lib qoladi — ommaviy oqim uni `ProgramsWithoutTests` deb ko'rsatadi).
- Tizim dasturida (`IsSystem`) tarkib o'zgartirilmaydi: `AddTest`/`RemoveTest`/`ReorderTests`
  — `SYSTEM_PROGRAM_LOCKED` (BR-8 ruhida).
- **P52 (2026-09-11, egasining qarori):** dasturda ilmiy shaxsiyat batareyasi (`Standard` +
  `Scored` metodika, `Domain.Catalog.PersonalityBattery.ContainedIn`) bo'lsa `RegistrationMode`
  DOIM `Full` bo'lishi SHART — scoring, normalar va AI tahlili yosh/sinf/jinsga tayanadi,
  ularsiz natija ma'nosiz bo'ladi. Buzilsa `DomainException("REGISTRATION_REQUIRED_FOR_BATTERY")`.
  Tekshiruv IKKI nazorat nuqtasida: `SetRegistrationMode()` (rejim o'zgartirilganda) va
  `Publish()` (nashr qilinganda) — ikkalasi ham `hasPersonalityBattery` bayrog'ini PARAMETR
  sifatida qabul qiladi, chunki domen agregatining o'zi `TestDefinition`larga to'g'ridan-to'g'ri
  murojaat qila olmaydi (faqat `ProgramTest.TestDefinitionId` saqlaydi) — chaqiruvchi
  (`UpdateProgramCommandHandler`/`PublishProgramCommandHandler`) tarkibni yuklab hisoblaydi.
- **P52 kengaytmasi (2026-09-11, `docs/18` §9.5):** shu batareyali dasturda `RegistrationFields.BirthDate`/
  `Grade` ham DOIM `Required` bo'lishi SHART (`RegistrationFields.SatisfiesPersonalityBatteryInvariant()`).
  Buzilsa `DomainException("REGISTRATION_FIELD_REQUIRED_FOR_BATTERY")`. Tekshiruv XUDDI SHU
  IKKI nazorat nuqtasida: `SetRegistrationFields()` va `Publish()`. `Gender` bu invariantga
  KIRMAYDI — erkin sozlanadi (batareyali dasturda ham).

**Eski ma'lumot:** `Archived + IsActive` juftligi bazada qolgan bo'lsa, `State` uni baribir
`Archived` deb ko'rsatadi (`Status` ustuvor), ustunning o'zi esa seed bosqichida idempotent
tarzda tuzatiladi (`DbSeeder.ReconcileProgramStatesAsync` — migratsiya emas).

---

### 2.14 `RegistrationFormSettings` (agregat ildizi, SINGLETON) — 2026-09-11/12, egasining talabi

Ro'yxatdan o'tish formasining GLOBAL sozlamasi — `docs/18-tarmoqlanuvchi-sorovnoma.md` §9.6.
`AssessmentProgram.RegistrationFields`ning O'RNIGA keladi (yuqoridagi jadval izohiga qarang):
Superadmin "Sozlamalar" sahifasidan bitta joyda boshqaradi — barcha dasturlar uchun umumiy.

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | Har doim `RegistrationFormSettings.SingletonId` (qattiq kodlangan `Guid`) — jadvalda bitta qatordan ortiq bo'lmaydi |
| `Definition` | `RegistrationFormDefinition` | Formaning to'liq shakli (pastga qarang), `jsonb` |
| `UpdatedAt` | `DateTimeOffset` | |
| `UpdatedByAdminUserId` | `Guid?` | |

DB'da yozuv UMUMAN bo'lmasligi mumkin (hali hech kim `PUT` qilmagan) — bu holda
`RegistrationFormDefinition.Default` ishlatiladi ("`NULL` = standart" naqshi,
`RegistrationFields.Default` bilan bir xil uslub).

**`RegistrationFormDefinition`** — qiymat obyekti:

- `CoreFields` (`RegistrationCoreFields`) — sakkizta QATTIQ KODLANGAN maydon: `fullName`,
  `birthDate`, `gender`, `grade`, `classLetter`, `phone`, `parentPhone`, `email`. Har biri
  `RegistrationCoreField { Requirement, LabelUz, PlaceholderUz?, Order }`.
  **`fullName.Requirement` HAR DOIM `Required`** — o'zgartirib bo'lmaydi (ism kerak bo'lmasa
  `AssessmentProgram.RegistrationMode = None` bor). Yorlig'i/placeholder'i esa tahrirlanadi.
- `CustomFields` (`IReadOnlyList<RegistrationCustomField>`) — superadmin qo'shgan o'z
  maydonlari (masalan "Ota-onangiz kasbi"). Har biri: `Code` (`^[A-Za-z0-9_-]{1,20}$`, unikal,
  core maydon nomlari bilan to'qnashmaydi), `Type` (`QuestionType` dan FAQAT `ShortText`/
  `LongText`/`Phone`/`SingleChoice`/`MultiChoice` — yangi atama o'ylab topilmadi), `LabelUz`,
  `PlaceholderUz?`, `Requirement`, `MaxLength?`, `InputPattern?`, `Options?`
  (`SingleChoice`/`MultiChoice` uchun, kamida 2 ta, qiymatlari unikal), `Order`.

**Invariantlar (`RegistrationFormDefinition.Create`, buzilsa `DomainException`, `docs/06` §6):**

| Qoida | Xato kodi |
|-------|-----------|
| `fullName.requirement != Required` | `REGISTRATION_FORM_FULL_NAME_LOCKED` |
| `customFields[].code` noto'g'ri shaklda | `REGISTRATION_FORM_FIELD_CODE_INVALID` |
| `code` takrorlangan YOKI core maydon nomi bilan to'qnashgan | `REGISTRATION_FORM_FIELD_CODE_DUPLICATE` |
| `SingleChoice`/`MultiChoice`da 2 tadan kam variant | `REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT` |
| Bitta maydon ichida takroriy `options[].value` | `REGISTRATION_FORM_OPTION_VALUE_DUPLICATE` |
| `inputPattern` kompilyatsiya qilinmaydi | `INPUT_PATTERN_INVALID` (`CachedInputPatternMatcher` bilan bir xil qoida, handler darajasida) |

**Diqqat — 1-to'lqin qamrovi (joriy):** bu sozlama HALI sessiya oqimiga (`StartSessionCommandHandler`,
ro'yxatdan o'tish formasining haqiqiy validatsiyasi) ULANMAGAN — faqat domen + saqlash + admin
`GET`/`PUT` API tayyor. Ulash va frontend keyingi to'lqinda (`PROGRESS.md`).

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
| `SchoolEntryCodeRegeneratedEvent` | Maktab kodi yangilandi (2026-09-07) | Audit |
| `TestPublishedEvent` | Superadmin anketani nashr qildi | Katalog keshini tozalash, audit |
| `TestVersionBumpedEvent` | Nashr qilingan testga savol qo'shildi/olib tashlandi | Audit |

---

## 4. Value object'lar

- `SchoolSlug` — validatsiya + normalizatsiya.
- `PhoneNumber` — O'zbekiston formati `+998XXXXXXXXX`, normalizatsiya.
- `PersonalityType` — 4 harf, validatsiya, `TypeCatalog` bilan bog'lanish.
- `HollandCode` — 3 harf, `RIASEC` alifbosidan.
- `ScorePercent` — 0..100 oralig'i kafolatlangan.
- `RegistrationFormDefinition`, `RegistrationCoreFields`, `RegistrationCoreField`,
  `RegistrationCustomField`, `RegistrationCustomFieldOption` — §2.14 (`RegistrationFormSettings`).

**Domen yordamchilari (value object emas):**
- `Common/TokenHash` — SHA-256 hex (64 belgi). `Assessment.SessionTokenHash` va
  `PublicRefreshToken.TokenHash` uchun yagona algoritm; `Application/Identity/Common/RefreshTokenHash`
  bilan bayt-ma-bayt bir xil. Postgres ekvivalenti: `encode(sha256(convert_to(t,'UTF8')),'hex')`.

---

## 5. Yumshoq o'chirish (soft delete)

- `School`, `Student`, `Assessment` — `IsDeleted`, `DeletedAt`, `DeletedByAdminUserId`.
- `PublicUser` (**P47**) — faqat `DeletedAt` (`is_deleted` ustuni yo'q): "o'chirilgan" holat
  anonimlashtirish bilan birga keladi, ikkita ustunni sinxron ushlash shart emas.
  Query filter: `HasQueryFilter(u => u.DeletedAt == null)`.
- Global query filter EF Core'da: `HasQueryFilter(e => !e.IsDeleted)`.
- **Istisno:** o'quvchi "ma'lumotimni o'chiring" desa — **hard delete** + `AuditLog` ga faqat
  `StudentId` va vaqt yoziladi (shaxsiy ma'lumotsiz).
