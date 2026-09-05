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

**Metodlar:** `RegenerateAccessToken()`, `Deactivate()`, `Activate()`, `SetShowResultToStudent()`.

**Fabrikalar:** `Create()` — har doim `Kind = School`; `CreatePublicSpace()` — yagona ommaviy
makon (faqat `DbSeeder.SeedPublicSpaceAsync` chaqiradi, slug `ommaviy`, `ShowResultToStudent = true`).

---

### 2.2 `Student` (agregat ildizi)

| Maydon | Tip | Izoh |
|--------|-----|------|
| `Id` | `Guid` | |
| `SchoolId` | `Guid` | FK → School (ommaviy oqimda — ommaviy makon `Id`si) |
| `PublicUserId` | `Guid?` | **P47** — FK → `PublicUser`. Maktab oqimida `null` |
| `FullName` | `string(200)` | FISH, kiritilgani bo'yicha |
| `NormalizedName` | `string(200)` | Katta harf, ortiqcha probel olib tashlangan — dublikat uchun |
| `BirthDate` | `DateOnly` | |
| `Gender` | `enum` | `Male`, `Female`, `Unspecified` |
| `Grade` | `int` | 1..11, yoki `0` = `Student.NoGrade` (**P47** — maktabda o'qimaydigan ommaviy foydalanuvchi) |
| `ClassLetter` | `string(2)?` | "A", "B" |
| `Phone` | `string(20)` | |
| `ParentPhone` | `string(20)?` | |
| `Email` | `string(150)?` | |
| `ConsentGivenAt` | `DateTimeOffset` | Rozilik vaqti (topshiriqdagi `ConsentAcceptedAt` — AYNAN shu maydon, dublikat ustun qo'shilmadi) |
| `ConsentVersion` | `string(30)?` | **P47** — qabul qilingan rozilik matni versiyasi (`2026-09-v1`) |
| `ParentalConsent` | `bool` | **P47** — voyaga yetmagan uchun ota-ona/vasiy roziligi. Rozilik SESSIYAGA emas, SHAXSGA tegishli — shu sabab `Assessment` da emas |
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

**Metodlar:** `UpdateSnapshot()`, `MarkDeleted()`, `LinkToPublicUser()` (**P47** — eski yozuvni
Telegram akkauntga ulash; boshqa akkauntga bog'langan yozuv qayta bog'lanmaydi:
`DomainException("STUDENT_ALREADY_LINKED")`), `RecordConsent()`.

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

**Invariantlar**
- `TelegramId > 0` (yaratilishda).
- O'chirilgan akkauntda hech qanday harakat qilinmaydi (`DomainException("PUBLIC_USER_DELETED")`).

**Metodlar:** `Create()`, `RecordLogin()`, `MarkDeleted()`.

**Nima uchun yumshoq o'chirish + anonimlashtirish (qattiq o'chirish emas):** `students.public_user_id`
FK'si va test tarixi (`docs/08`: natijalar 5 yil saqlanadi) qattiq o'chirishda buzilardi.
`MarkDeleted` shaxsni aniqlovchi BARCHA maydonni (Telegram ID ham) tozalaydi — ya'ni GDPR
ma'nosidagi "o'chirish" bajariladi, statistika esa anonim qoladi. Telegram ID tozalangani uchun
bir xil foydalanuvchi qayta kirsa YANGI akkaunt oladi (kutilgan xatti-harakat).

---

### 2.12 `PublicRefreshToken` — P47

`Identity/RefreshToken` naqshini AYNAN takrorlaydi (SHA-256 xesh, rotatsiya,
qayta-ishlatishni aniqlash, muddat), yagona farq — FK `PublicUserId`.

`Id`, `PublicUserId`, `TokenHash` (unikal), `ExpiresAt`, `RevokedAt?`, `CreatedByIpHash?`, `CreatedAt`.
Ikki marta bekor qilishga urinish — `DomainException("PUBLIC_REFRESH_TOKEN_ALREADY_REVOKED")`.

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
