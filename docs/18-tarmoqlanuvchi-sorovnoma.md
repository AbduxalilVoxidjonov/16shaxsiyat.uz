# 18 — Tarmoqlanuvchi so'rovnoma (branching survey)

> **Holat:** P52 shartnomasi. Bu hujjat — barcha qatlamlar (domen, DB, API, frontend) uchun
> **yagona haqiqat manbai**. `docs/04`, `docs/05`, `docs/06`, `docs/07`, `docs/10`, `docs/11`
> shu hujjatga havola qiladi va faqat qisqa qism saqlaydi.

---

## 0. Muammo va maqsad

Superadmin kod yozmasdan **shartli (tarmoqlanuvchi)** so'rovnoma yarata olishi kerak:
o'quvchi bitta variantni belgilasa — bir guruh savol paydo bo'ladi, boshqasini belgilasa —
butunlay boshqasi, uchinchisida esa hech narsa qo'shilmaydi.

Namunaviy oqim (egasining "Maktab o'quvchilari uchun so'rovnoma" hujjati):

```
1-BO'LIM (hamma)
  1.1 F.I.Sh.            ShortText
  1.2 Maktab va sinf     ShortText
  1.3 Telefon            Phone
  1.4 Ota-ona telefoni   Phone
  1.5 Smena              SingleChoice
  1.6 [FILTR] Qo'shimcha kursga qatnashasizmi?   SingleChoice (A=1 / B=2 / C=3)
        │
        ├── 1 ──▶ 2-A BO'LIM (ichki NPS)              6 savol
        ├── 2 ──▶ 2-B BO'LIM (raqobatchilar tahlili)  5 savol
        └── 3 ──▶ 2-C BO'LIM (potensial lidlar)       4 savol
                        │
3-BO'LIM (hamma)  ◀─────┘
  3.1 Bonus tanlash      SingleChoice (majburiy)
  3.2 Do'st ma'lumoti    ShortText (ixtiyoriy)
```

Shu bilan birga **bitta savol darajasida** ham shart kerak: "Boshqa (kiriting)" varianti
tanlansa — ostida matn maydoni paydo bo'lsin.

---

## 1. Qamrov va qat'iy chegaralar

| Qoida | Sabab |
|-------|-------|
| **B-1.** `ShortText`, `LongText`, `Phone`, `MultiChoice` turlari FAQAT `ScoringMode = Survey` anketalarda. Aks holda `QUESTION_TYPE_NOT_SCORABLE` | Matn va ko'p tanlovni deterministik ballash formulasi yo'q (`CLAUDE.md` 3-qoida) |
| **B-2.** Ko'rsatish sharti (`visibility`) FAQAT `ScoringMode = Survey` anketalarda. Aks holda `BRANCHING_NOT_ALLOWED_IN_SCORED` | Yashirilgan savol ballash maxrasini o'zgartirib, natijani nodeterministik qilardi |
| **B-3.** Tizim metodikasida (`IsSystem = true`) bo'lim ham, shart ham qo'shilmaydi — `SYSTEM_TEST_LOCKED` | BR-8 |
| **B-4.** Shart faqat **oldinroqdagi** savolga (`DisplayOrder` kichikroq) havola qila oladi | Sikl va oldinga havola bo'lmasligi kafolati; bir marta o'tishda hisoblanadi |
| **B-5.** Shartda savol **kodi** (`questionCode`) ishlatiladi, ID emas | jsonb o'qiladigan bo'ladi, seed/import/eksport aylanmasi buzilmaydi |
| **B-6.** "Boshqa (kiriting)" alohida `AnswerOption` bayrog'i EMAS — bu **keyingi `ShortText` savol + shart** bilan modellanadi | Bitta mexanizm, javob saqlashda ikkinchi shakl paydo bo'lmaydi |
| **B-7.** `scale`/`scaleDirection` hech qachon ommaviy API'ga chiqmaydi (shart ichida ham) | `CLAUDE.md` 9-qoida |

**Hech narsa buzilmaydi:** bo'limsiz va shartsiz anketalar (4 ta tizim metodikasi) uchun
barcha mavjud xatti-harakat — sahifalash, `scaleLabels`, scoring, ishonchlilik — **aynan
o'zgarishsiz** qoladi.

---

## 2. Domen modeli

### 2.1 `QuestionType` — yangi qiymatlar

```csharp
public enum QuestionType
{
    Likert5 = 1,
    Likert7 = 2,
    Binary = 3,
    SingleChoice = 4,
    ForcedChoice = 5,
    ShortText = 6,    // bir qatorli matn
    LongText = 7,     // ko'p qatorli matn (textarea)
    MultiChoice = 8,  // checkbox — bir nechta variant
    Phone = 9,        // O'zbekiston raqami; standart shablon quyida
}
```

`Phone` — `ShortText`ning maxsus holati: mijozda `inputmode="tel"`, standart
`InputPattern` `^\+?998[0-9]{9}$`, standart `Placeholder` `+998 90 123 45 67`.
Superadmin `InputPattern`ni o'zgartira oladi.

Yangi turlar uchun **javob qiymati (`RawValue`) yo'q** (`null`):

| Tur | Javob maydoni | `RawValue` |
|-----|---------------|-----------|
| `Likert5`/`Likert7`/`Binary`/`SingleChoice`/`ForcedChoice` | `value` | to'ldiriladi |
| `ShortText`/`LongText`/`Phone` | `text` | `null` |
| `MultiChoice` | `selectedValues[]` | `null` |

### 2.2 `QuestionSection` — yangi entity

`TestDefinition` agregati ichidagi bola (`Domain/Catalog/QuestionSection.cs`):

| Maydon | Tur | Izoh |
|--------|-----|------|
| `Id` | `Guid` | |
| `TestDefinitionId` | `Guid` | |
| `Code` | `string` (≤20) | Anketa ichida unikal. `^[A-Za-z0-9_-]+$` |
| `TitleUz` | `string` (≤200) | Ekranda sarlavha |
| `DescriptionUz` | `string?` (≤1000) | Ixtiyoriy kirish matni |
| `DisplayOrder` | `int` | |
| `VisibilityRule` | `VisibilityRule?` | Bo'lim ko'rinishi sharti |

**Agregat metodlari** (`TestDefinition` da): `AddSection`, `RemoveSection`
(savollari bo'lsa `SECTION_IN_USE`), `MoveQuestionToSection`, `ReorderSections`.
`IsSystem` bo'lsa hammasi `SYSTEM_TEST_LOCKED`. Kod takrorlansa `SECTION_CODE_DUPLICATE`.

### 2.3 `Question` — yangi maydonlar

| Maydon | Tur | Qaysi turlarda | Izoh |
|--------|-----|----------------|------|
| `SectionId` | `Guid?` | hammasi | `null` — bo'limga tegishli emas |
| `VisibilityRule` | `VisibilityRule?` | hammasi (B-2) | Savol darajasidagi shart |
| `Placeholder` | `string?` (≤200) | matn turlari | |
| `InputPattern` | `string?` (≤200) | `ShortText`/`Phone` | .NET va JS ikkalasida ham ishlaydigan regex |
| `MaxLength` | `int?` | matn turlari | Standart: `ShortText`/`Phone` 200, `LongText` 2000. Chegara 4000 |
| `MinSelections` | `int?` | `MultiChoice` | Standart 1 (majburiy bo'lsa) |
| `MaxSelections` | `int?` | `MultiChoice` | `null` — cheklovsiz |

`Scale`/`ScaleDirection`/`Weight` — `Survey` anketada ham talab qilinadi (mavjud shakl
buzilmasin), lekin ishlatilmaydi. Yangi turlar uchun standart: `Scale = "SURVEY"`,
`Direction = 1`, `Weight = 1`.

`MultiChoice` `AddOption` uchun ruxsat etilgan turlar ro'yxatiga qo'shiladi
(`SingleChoice`, `ForcedChoice`, `MultiChoice`).

### 2.4 `VisibilityRule` — qiymat obyekti

`Domain/Catalog/Branching/`:

```csharp
public enum VisibilityMatch { All = 1, Any = 2 }

public enum VisibilityOperator
{
    Equals = 1,        // javob qiymati == values[0]
    NotEquals = 2,
    AnyOf = 3,         // javob qiymati values ichida
    NoneOf = 4,
    ContainsAny = 5,   // MultiChoice: tanlanganlar values bilan kesishadi
    ContainsAll = 6,   // MultiChoice: values ning hammasi tanlangan
    Answered = 7,      // javob bor (matn bo'sh emas / qiymat bor) — `values` bo'sh
    NotAnswered = 8,
}

public sealed record VisibilityCondition(
    string QuestionCode,
    VisibilityOperator Operator,
    IReadOnlyList<int> Values);

public sealed record VisibilityRule(
    VisibilityMatch Match,
    IReadOnlyList<VisibilityCondition> Conditions);
```

**jsonb saqlash shakli** (camelCase, enum — satr):

```json
{
  "match": "All",
  "conditions": [
    { "questionCode": "Q1_6", "operator": "Equals", "values": [1] }
  ]
}
```

Cheklovlar: `conditions` 1..10 ta; `values` 0..50 ta; `Answered`/`NotAnswered` da `values`
bo'sh bo'lishi shart; qolgan operatorlarda kamida bitta qiymat.

### 2.5 Hisoblash — `VisibilityEvaluator`

**Sof, deterministik, qatlamsiz** (`Domain/Catalog/Branching/VisibilityEvaluator.cs`):

```csharp
public sealed record AnswerSnapshot(int? RawValue, string? TextValue, IReadOnlyList<int> SelectedValues)
{
    public bool IsAnswered => RawValue is not null
        || !string.IsNullOrWhiteSpace(TextValue)
        || SelectedValues.Count > 0;
}

public static class VisibilityEvaluator
{
    /// <summary>Bitta qoidani baholaydi. `rule is null` → har doim `true`.</summary>
    public static bool Evaluate(VisibilityRule? rule, IReadOnlyDictionary<string, AnswerSnapshot> answers);
}
```

Bitta shartni baholash qoidalari:

| Operator | Javob yo'q bo'lsa | Bor bo'lsa |
|----------|-------------------|-----------|
| `Equals`, `AnyOf`, `ContainsAny`, `ContainsAll` | `false` | qiymat solishtiriladi |
| `NotEquals`, `NoneOf` | **`false`** | qiymat solishtiriladi |
| `Answered` | `false` | `true` |
| `NotAnswered` | `true` | `false` |

> `NotEquals`/`NoneOf` javobsizda `false` — "hali javob bermagan" holat "boshqa qiymat"
> deb hisoblanmasin: aks holda sahifa ochilishi bilan barcha "boshqacha bo'lsa" tarmoqlari
> birdan ko'rinib ketardi.

`Equals`/`NotEquals`/`AnyOf`/`NoneOf` — `RawValue` bo'yicha. Manba savol `MultiChoice`
bo'lsa, ular `SelectedValues` bo'yicha ishlaydi (`Equals` → yagona tanlov shu qiymat).

### 2.6 Kaskad — `VisibleQuestionResolver`

Butun anketa uchun ko'rinadigan savollar to'plamini **bir marta o'tishda** (`DisplayOrder`
bo'yicha) hisoblaydi:

```csharp
public static class VisibleQuestionResolver
{
    public static VisibilityMap Resolve(
        IReadOnlyList<SectionSnapshot> sections,    // DisplayOrder bo'yicha
        IReadOnlyList<QuestionSnapshot> questions,  // DisplayOrder bo'yicha
        IReadOnlyDictionary<string, AnswerSnapshot> answersByQuestionCode);
}
```

Savol **ko'rinadi** ⟺ hamma shart bajarilsa:

1. `IsActive = true`;
2. bo'limi bor bo'lsa — **bo'lim ko'rinadi** (bo'lim sharti `true` VA bo'lim sharti
   havola qilgan savollar ko'rinadi);
3. o'z sharti `true`;
4. **kaskad:** shartda havola qilingan har bir savol ham ko'rinadi. Manba savol
   yashirilgan bo'lsa — bog'liq savol ham yashiriladi, javobi eski bo'lsa ham.

Kaskad uchun baholashda **yashirilgan savolning javobi hisobga olinmaydi**: rezolver
ketma-ket yurar ekan, ko'rinmagan savol uchun `answers` xaritasidan `AnswerSnapshot`
o'chiriladi (bo'sh javob sifatida qaraladi). B-4 (faqat orqaga havola) shu bir martalik
o'tishni to'g'ri qiladi.

`VisibilityMap` qaytaradi: `IReadOnlySet<Guid> VisibleQuestionIds`,
`IReadOnlySet<Guid> VisibleSectionIds`.

### 2.7 `Answer` — o'zgarishlar

| Maydon | Edi | Bo'ladi |
|--------|-----|---------|
| `RawValue` | `int` | **`int?`** |
| `TextValue` | — | `string?` (≤4000) |
| `SelectedValues` | — | `IReadOnlyList<int>` (jsonb, bo'sh ro'yxat = yo'q) |

`Answer.Create`/`UpdateValue` uchta shaklni ham qabul qiladi. Invariant: uchtadan
**aynan bittasi** to'ldirilgan bo'lishi kerak (`ANSWER_SHAPE_INVALID` — `DomainException`).

`AssessmentTest`:
- `UpsertAnswer(...)` yangi imzo bilan (`int? rawValue, string? textValue, IReadOnlyList<int> selectedValues`);
- yangi `RemoveAnswers(IEnumerable<Guid> questionIds)` — yashirilgan savollarning javoblarini
  tozalaydi va `AnsweredCount` ni mos kamaytiradi.

### 2.8 Scoring va ishonchlilik — himoya

`ScoringEngine` va `ReliabilityCalculator` kirishiga **faqat `ScoringMode = Scored`** testlar
tushadi (mavjud xatti-harakat). Qo'shimcha himoya sifatida ikkalasi ham `RawValue is null`
javobni **istisno bilan** rad etadi (`ArgumentException`) — jimgina 0 deb hisoblamaydi.
Bu B-1/B-2 chegarasi buzilib ketsa darhol ko'rinadi.

---

## 3. Ma'lumotlar bazasi (`docs/05` ga qo'shiladi)

### 3.1 Yangi jadval `question_sections`

| Ustun | Tur | Izoh |
|-------|-----|------|
| `id` | `uuid` PK | `gen_random_uuid()` |
| `test_definition_id` | `uuid` FK → `test_definitions` | `ON DELETE CASCADE` |
| `code` | `varchar(20)` | |
| `title_uz` | `varchar(200)` | |
| `description_uz` | `varchar(1000)` NULL | |
| `display_order` | `int` | |
| `visibility_rule` | `jsonb` NULL | |

Indekslar: `ux_question_sections_test_code` UNIQUE (`test_definition_id`, `code`),
`ix_question_sections_test_order` (`test_definition_id`, `display_order`).

### 3.2 `questions` — yangi ustunlar

| Ustun | Tur | Standart |
|-------|-----|----------|
| `section_id` | `uuid` NULL FK → `question_sections` | `ON DELETE SET NULL` |
| `visibility_rule` | `jsonb` NULL | |
| `placeholder` | `varchar(200)` NULL | |
| `input_pattern` | `varchar(200)` NULL | |
| `max_length` | `int` NULL | |
| `min_selections` | `int` NULL | |
| `max_selections` | `int` NULL | |

Indeks: `ix_questions_section` (`section_id`, `display_order`).

### 3.3 `answers` — o'zgarishlar

| Ustun | O'zgarish |
|-------|-----------|
| `raw_value` | `int NOT NULL` → **`int NULL`** (kengaytirish — destruktiv emas) |
| `text_value` | yangi `text` NULL |
| `selected_values` | yangi `jsonb` NULL (`int[]`) |

`CHECK` cheklovi `ck_answers_shape`:
```sql
(raw_value IS NOT NULL)::int
+ (text_value IS NOT NULL)::int
+ (selected_values IS NOT NULL)::int = 1
```

### 3.4 Migratsiya

Bitta EF Core migratsiyasi `AddBranchingSurvey`. Faqat qo'shish va kengaytirish —
ikki bosqichli destruktiv qadam kerak emas (`CLAUDE.md` 7-qoida).

---

## 4. Ommaviy API (`docs/07` §1.5–1.7 kengaytmasi)

### 4.1 `GET /api/public/sessions/tests/{testCode}/questions`

**Yangi qoida:** anketada **kamida bitta bo'lim** bo'lsa — sahifalash o'chadi:
javob `page = 1`, `totalPages = 1` va **barcha faol savollar** bilan qaytadi; tarmoqlanishni
mijoz o'zi bo'lim-bo'lim boshqaradi. Bo'limsiz anketalarda mavjud `pageSize` sahifalash
aynan saqlanadi.

> Sabab: 2-B bo'limi yashirilganda server sahifa raqamlarini surib turishi kerak bo'lardi,
> mijoz esa qaysi sahifaga o'tishini bilmasdi. So'rovnoma qisqa (≤60 savol) — bitta yuk
> bilan berish soddaroq va tezroq.

```json
{
  "testCode": "INTELLECT-SURVEY",
  "page": 1, "pageSize": 60, "totalPages": 1, "totalQuestions": 21,
  "scaleLabels": null,
  "sections": [
    { "id": "…", "code": "S1", "title": "Asosiy ma'lumotlar",
      "description": "Bu bo'limni hamma to'ldiradi.", "order": 1, "visibility": null },
    { "id": "…", "code": "S2A", "title": "Intellect o'quvchilari uchun",
      "description": null, "order": 2,
      "visibility": { "match": "All",
        "conditions": [ { "questionCode": "Q1_6", "operator": "Equals", "values": [1] } ] } }
  ],
  "questions": [
    { "id": "…", "code": "Q1_1", "order": 1, "sectionId": "…",
      "text": "F.I.Sh.", "type": "ShortText", "isRequired": true,
      "placeholder": "Masalan: Karimov Alisher", "inputPattern": null, "maxLength": 200,
      "minSelections": null, "maxSelections": null,
      "options": null, "visibility": null,
      "currentValue": null, "currentText": null, "currentValues": null }
  ]
}
```

`PublicQuestionDto` yangi maydonlari: `sectionId`, `placeholder`, `inputPattern`,
`maxLength`, `minSelections`, `maxSelections`, `visibility`, `currentText`, `currentValues`.
`scale`/`scaleDirection` — **oldingidek yo'q** (B-7).

`sections` — bo'limsiz anketada `null`.

### 4.2 `POST /api/public/sessions/tests/{testCode}/answers`

So'rov elementi:

```json
{ "questionId": "…", "value": 3, "text": null, "selectedValues": null, "durationMs": 4210 }
```

`value`/`text`/`selectedValues` — **aynan bittasi** to'ldirilishi shart, savol turiga mos
(`ANSWER_SHAPE_INVALID` → 400 `VALIDATION_ERROR`).

Validatsiya (mavjud ikki bosqichli "avval hammasi tekshiriladi, keyin hammasi yoziladi"
naqshi saqlanadi):

| Tur | Tekshiruv | Xato |
|-----|-----------|------|
| `ShortText`/`Phone` | `text` bo'sh emas, `≤ MaxLength`, `InputPattern` mos | `VALIDATION_ERROR` |
| `LongText` | `text` bo'sh emas, `≤ MaxLength` | `VALIDATION_ERROR` |
| `MultiChoice` | `selectedValues` bo'sh emas, takrorlanmaydi, hammasi variant qiymatlaridan, `MinSelections ≤ n ≤ MaxSelections` | `VALIDATION_ERROR` |
| qolgani | mavjud qoidalar | |

**Yangi qo'riqchi:** so'rovdagi savol joriy javoblar (bazadagi + shu so'rovdagi) bo'yicha
**ko'rinmasa** — `400 QUESTION_NOT_VISIBLE`. Bu yashirilgan tarmoqqa javob yozilishini
to'sadi (mijoz xatosi yoki qo'lda so'rov).

`inputPattern` server tomonda **`RegexOptions.NonBacktracking` va 100 ms timeout** bilan
qo'llaniladi (ReDoS himoyasi). Regex kompilyatsiya qilinmasa — savol saqlanishida
(`admin` tomonda) rad etiladi, ommaviy oqimda esa shablon e'tiborsiz qoldiriladi.

### 4.3 `POST /api/public/sessions/tests/{testCode}/complete`

1. `VisibleQuestionResolver` joriy javoblar bilan chaqiriladi.
2. **Majburiy savol** tekshiruvi faqat **ko'rinadigan** savollar bo'yicha
   (`unansweredCount` shu to'plamdan).
3. **Yashirilgan savollarning javoblari o'chiriladi** (`AssessmentTest.RemoveAnswers`) —
   o'quvchi 1.6 ni A dan B ga o'zgartirsa, 2-A javoblari eksportga va AI'ga tushmasligi
   kerak. `AnsweredCount` mos ravishda kamayadi.
4. Qolgani — mavjud oqim (`Survey` bo'lsa `TestResult` yozilmaydi).

`Assessment.CompleteTest` ga uzatiladigan `requiredQuestionIds` ham ko'rinadiganlardan
iborat bo'ladi.

---

## 5. Admin API (`docs/07` §3.4 kengaytmasi)

**Bo'limlar** (faqat `Custom`; tizimda `409 SYSTEM_TEST_LOCKED`)

| Metod | Yo'l |
|-------|------|
| GET | `/api/admin/catalog/tests/{id}/sections` |
| POST | `/api/admin/catalog/tests/{id}/sections` — `{ code, titleUz, descriptionUz, displayOrder, visibility }` |
| PUT | `/api/admin/catalog/sections/{sectionId}` |
| DELETE | `/api/admin/catalog/sections/{sectionId}` — savollari bo'lsa `409 SECTION_IN_USE` |
| POST | `/api/admin/catalog/tests/{id}/sections/reorder` — `[{ id, displayOrder }]` |

**Savollar** — `POST .../questions` va `PUT .../questions/{id}` yangi maydonlarni qabul
qiladi: `sectionCode` (yoki `null`), `placeholder`, `inputPattern`, `maxLength`,
`minSelections`, `maxSelections`, `visibility`, `options[]`
(`{ textUz, value, displayOrder }` — `SingleChoice`/`ForcedChoice`/`MultiChoice` uchun).

Tizim metodikasida `PUT` avvalgidek faqat `textUz`/`textRu`/`isActive` ni o'zgartiradi.

**Nashr validatsiyasi** (`CatalogPublishValidator`) — yangi `issues[]` kodlari:

| Kod | Ma'no |
|-----|-------|
| `VISIBILITY_UNKNOWN_QUESTION` | Shart mavjud bo'lmagan savol kodiga havola qiladi |
| `VISIBILITY_FORWARD_REFERENCE` | Shart keyingi (yoki o'sha) tartibdagi savolga havola qiladi (B-4) |
| `VISIBILITY_OPERATOR_MISMATCH` | Operator manba savol turiga mos emas (masalan `ContainsAny` `Likert5` ga) |
| `VISIBILITY_VALUE_UNKNOWN` | `values` manba savolning variantlari/darajalari orasida yo'q |
| `BRANCHING_NOT_ALLOWED_IN_SCORED` | B-2 |
| `QUESTION_TYPE_NOT_SCORABLE` | B-1 |
| `QUESTION_OPTIONS_REQUIRED` | `SingleChoice`/`MultiChoice` da 2 tadan kam variant |
| `QUESTION_OPTION_VALUE_DUPLICATE` | Bitta savolda takroriy variant qiymati |
| `SECTION_EMPTY` | Bo'limda bitta ham faol savol yo'q |
| `INPUT_PATTERN_INVALID` | Regex kompilyatsiya qilinmadi |

Nashr qilinmagan (`Draft`) anketada bu tekshiruvlar **bloklamaydi**, faqat ro'yxatda
ko'rsatiladi — mavjud `publishIssues` naqshi.

**Import/eksport JSON sxemasi** — `sections[]`, savolda `sectionCode`, `visibility`,
`options[]` va yangi maydonlar bilan kengaytiriladi. Mavjud (bo'limsiz) fayllar
o'zgarishsiz o'qiladi.

---

## 6. Frontend

### 6.1 Umumiy hisoblagich — TS egizagi

`frontend/src/shared/lib/visibility.ts` — `VisibilityEvaluator` va
`VisibleQuestionResolver` ning **aynan bir xil xatti-harakatli** TS nusxasi.

**Ikki nusxa xavfi oltin fikstura bilan qulflanadi:**
`tests/fixtures/visibility-golden.json` — bitta fayl, `{ cases: [{ name, sections,
questions, answers, expectedVisibleQuestionCodes }] }`. Uni **ikkala** tomon o'qiydi:

- `tests/StudentRoadMap.Domain.Tests/Branching/VisibilityGoldenTests.cs`
- `frontend/src/shared/lib/visibility.golden.test.ts`

Fikstura kamida shu holatlarni qamraydi: shartsiz; `Equals` A/B/C tarmoqlanishi;
javobsiz `NotEquals`; kaskad (manba yashirilganda bog'liq ham yashirilishi);
`ContainsAny`/`ContainsAll` `MultiChoice` da; `Answered`/`NotAnswered`; `Any` vs `All`;
bo'lim sharti savol shartidan ustunligi.

### 6.2 Ommaviy oqim (`features/public-assessment`)

Yangi komponentlar (`components/`):

| Komponent | Tur |
|-----------|-----|
| `TextQuestion` | `ShortText`, `Phone` (`inputmode`, shablon, jonli xato) |
| `LongTextQuestion` | `LongText` (`textarea`, belgi hisoblagichi) |
| `MultiChoiceQuestion` | `MultiChoice` (checkbox kartalari, min/maks) |
| `QuestionRenderer` | Turga qarab yuqoridagilardan yoki `LikertQuestion` dan birini tanlaydi |
| `SectionIntro` | Bo'lim sarlavhasi va tavsifi |

`TestPage`:
- `sections` bo'lsa — **mahalliy bo'lim-qadam** rejimi: bir ekranda bitta ko'rinadigan
  bo'lim; "Keyingi" keyingi **ko'rinadigan** bo'limga o'tadi, yashirilganlar sakrab
  o'tiladi; progress `visibleQuestions` bo'yicha hisoblanadi;
- javob o'zgarganda `useMemo` bilan ko'rinish qayta hisoblanadi — yangi savollar darhol
  paydo bo'ladi/yo'qoladi (animatsiya `prefers-reduced-motion` ni hurmat qiladi);
- yashirilgan savolning mahalliy javobi **yuborilmaydi** (autosave navbatidan chiqariladi);
- "Keyingi"da to'ldirilmagan **ko'rinadigan majburiy** savollar belgilanadi.

Mavjud (bo'limsiz) oqim — sahifalash, autosave, `durationMs` o'lchovi — tegilmaydi.

### 6.3 Admin konstruktor (`features/catalog`)

| Fayl | Vazifa |
|------|--------|
| `components/SectionsSection.tsx` | Bo'limlar ro'yxati, qo'shish/tahrirlash/tartiblash/o'chirish |
| `components/SectionDialog.tsx` | Bo'lim formasi + ko'rsatish sharti |
| `components/VisibilityRuleEditor.tsx` | Shart muharriri: manba savol (faqat oldingilari), operator (turga mos), qiymatlar (variantdan tanlash) |
| `components/OptionsEditor.tsx` | Variantlar ro'yxati (matn, qiymat, tartib) |
| `components/QuestionEditorDialog.tsx` | Kengaytirish: tur bo'yicha maydonlar, bo'lim tanlash, variantlar, shart |
| `components/BranchingPreview.tsx` | "Oqim" ko'rinishi: qaysi javob qaysi bo'limga olib boradi (faqat o'qish) |

`VisibilityRuleEditor` **o'zbekcha jumla** ko'rinishida ko'rsatadi:
«**Q1_6** savoliga javob **"Ha, Intellect o'quv markazida"** bo'lsa ko'rsatilsin».

Import sxemasi (`model/importSchema.ts`) va nashr xatolari ro'yxati
(`model/publishIssues.ts`) yangi kodlar bilan to'ldiriladi.

---

## 7. Namunaviy so'rovnoma — SEED (egasining talabi)

`src/StudentRoadMap.Infrastructure/Persistence/SeedData/surveys/intellect-survey.json` —
egasining "Maktablar uchun Elektron So'rovnoma" hujjatidagi so'rovnoma to'liq (kod
`INTELLECT-SURVEY`, `scoringMode: "Survey"`, **5 bo'lim, 25 savol**, 1.6-savol filtri, ikkita
"Boshqa (kiriting)" tarmog'i va bitta `ContainsAny` sharti bilan).

**Bu fayl endi SEED** — `DbSeeder.SeedSurveysAsync` uni har `--seed` ishga tushishida o'qiydi
(`docs/13` §"seed" bo'limi). Manba **yagona joyda** saqlanadi: eski `docs/examples/` nusxasi
olib tashlangan (ikki joyda holat saqlash chalkashlikka olib keladi — `CLAUDE.md` "Joriy
holat" izohi).

**Idempotentlik — tizim metodikalaridan TUBDAN farqli:** `Code` (`INTELLECT-SURVEY`) bo'yicha
anketa bazada allaqachon bo'lsa, seed uni BUTUNLAY o'tkazib yuboradi — matn, variant, shart
QAYTA YOZILMAYDI. Sabab: bu anketa `Kind = Custom`/`IsSystem = false` — superadmin uni panelda
tahrirlashi (masalan, o'quv markaz nomlarini) ATAYLAB kutiladi, qayta seed uning ishini yo'q
qilib yubormasligi kerak (4 ta tizim metodikasidagi "matn/tartib qayta sinxronlanadi" mantig'i
bu yerga OLIB KELINMAGAN).

**Natija har doim `Status = Draft`** — avtomatik nashr qilinmaydi. Sabab: 2-B bo'limidagi
(`Q2B_1`) raqobatchi o'quv markazlari `"1-o'quv markaz (nomini tahrirlang)"` kabi
TO'LDIRILMAGAN namuna qiymatlar bilan keladi — ularning haqiqiy nomini faqat egasi biladi.
`Published` qilib seed qilinsa, o'quvchiga shu tahrirlanmagan matn ko'rinib qolardi.

**Superadmin nima qilishi kerak (qadam-baqadam):**
1. Katalog → `INTELLECT-SURVEY` (`Draft` sifatida allaqachon ko'rinadi, import shart emas).
2. `Q2B_1` savolining variantlarini (2-B bo'lim, "Boshqa markazda o'qiyotganlar uchun") o'z
   hududidagi haqiqiy o'quv markazlari nomlariga almashtiring.
3. "Nashr qilish" tugmasini bosing va dasturga (`AssessmentProgram`) biriktiring.

Superadmin xohlasa **Katalog → Import (JSON)** orqali BOSHQA (yangi) tarmoqlanuvchi
so'rovnoma ham yuklashi mumkin — bu yo'l o'zgarishsiz qoladi, faqat `INTELLECT-SURVEY` uchun
endi qo'lda import qilish shart emas.

---

## 8. Testlar (DoD)

| Qatlam | Talab |
|--------|-------|
| Domen | `VisibilityEvaluator` — har operator uchun; `VisibleQuestionResolver` — kaskad, bo'lim ustunligi; `Answer` shakl invarianti; `QuestionSection` agregat qoidalari; oltin fikstura testi |
| Application | `SaveAnswers` — 4 ta yangi tur validatsiyasi + `QUESTION_NOT_VISIBLE`; `CompleteTest` — yashirilgan majburiy savol bloklamasligi VA yashirilgan javoblar o'chishi; `GetTestQuestions` — bo'limli anketada bitta sahifa |
| Integratsiya | To'liq oqim: 1.6=A → 2-A ko'rinadi, complete o'tadi; 1.6 B ga o'zgaradi → complete'da 2-A javoblari o'chadi |
| Regressiya | 4 ta tizim metodikasi uchun `GetTestQuestions` javobi va scoring **bayt-bayt o'zgarmagan** |
| Frontend | Oltin fikstura testi; `TextQuestion`/`MultiChoiceQuestion` a11y va validatsiya; `TestPage` bo'lim-qadam va sakrash testi; `VisibilityRuleEditor` |
| E2E | `screens.e2e.ts` ga tarmoqlanuvchi so'rovnoma oqimi (390px va 1440px) |

---

## 9. Ro'yxatdan o'tishsiz dasturlar (`RegistrationMode`, 2026-09-11)

### 9.0 Muammo

Maktab kodini kiritgach o'quvchi **ro'yxatdan o'tish anketasini** to'ldiradi (F.I.Sh.,
tug'ilgan sana, jins, sinf, telefon), keyin so'rovnomaga kiradi va u yerda **yana o'sha
ma'lumotlar** so'raladi (masalan `INTELLECT-SURVEY` ning `Q1_1`–`Q1_4`: F.I.Sh., maktab/sinf,
telefon, ota-ona telefoni). Ikki marta so'ralar edi.

### 9.1 Qaror

Ro'yxatdan o'tish **dasturga** biriktiriladi (`AssessmentProgram.RegistrationMode`, `docs/04`
§2.13): dastur "ro'yxatdan o'tishsiz" (`None`) bo'lsa — registratsiya ekrani UMUMAN
ko'rsatilmaydi, o'quvchi yozuvi **anonim** yaratiladi (`Student.CreateAnonymous`, `docs/04`
§2.2), shaxs ma'lumoti (agar so'rovnomaning o'z savollarida bo'lsa) o'sha javoblarda qoladi
(eksportda ko'rinadi, `Student` yozuvida emas).

**Qabul qilingan kamchiliklar** (egasi bilib turib tanladi):
- Anonim sessiyada admin "O'quvchilar" ro'yxatida ism ko'rinmaydi (`"Anonim ishtirokchi #XXXXXX"`);
- BR-1 (90 kunlik takror topshirish) tekshiruvi ishlamaydi — bir xil brauzerdan bir necha
  marta kirish mumkin.

### 9.2 Qat'iy invariant

Dasturda ilmiy shaxsiyat batareyasi (`PersonalityBattery` — `Standard` + `Scored` metodika)
bo'lsa `RegistrationMode` **DOIM `Full`** bo'lishi SHART. Sabab: scoring, normalar va AI
tahlili yosh/sinf/jinsga tayanadi — ularsiz natija ma'nosiz bo'ladi. Buzilsa —
`DomainException("REGISTRATION_REQUIRED_FOR_BATTERY")` → `400` (`docs/06` §6). Tekshiruv IKKI
nazorat nuqtasida: `AssessmentProgram.SetRegistrationMode()` (rejim o'zgartirilganda) va
`Publish()` (nashr qilinganda).

### 9.3 Qamrov

Faqat maktab oqimi (`POST /api/public/sessions`, `StartSessionCommandHandler`) — ommaviy
makon/Telegram oqimi (`StartPublicSessionCommand`) TEGILMAGAN, u har doim to'liq profil talab
qiladi (`docs/06` §8, 2026-09-05 qarori: ikki oqim ataylab ajratilgan).

### 9.4 Shartnoma

- `GET /api/public/schools/{slug}` → `programs[].registrationMode` (`docs/07` §1.1).
- `POST /api/public/sessions` — `registrationMode = None` dasturda shaxs maydonlari talab
  qilinmaydi va berilsa ham e'tiborsiz qoldiriladi (`docs/07` §1.2 "Anonim oqim").
- Admin: `POST`/`PUT /api/admin/programs` `registrationMode` qabul qiladi/qaytaradi
  (`docs/07` §3.5).
- DDL: `assessment_programs.registration_mode`, `students.is_anonymous`,
  `students.birth_date`/`students.phone` → NULLABLE (`docs/05`, migratsiya
  `AddProgramRegistrationModeAndAnonymousStudents`).
