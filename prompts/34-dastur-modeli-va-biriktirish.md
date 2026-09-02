# P34 — Dastur modeli, maktabga biriktirish va shartli scoring

## Kontekst
Loyiha egasi 2026-09-02 da mahsulot oqimini o'zgartirdi. Qarorlar `docs/06` 8-bo'lim
(qarorlar jurnali) da yozilgan — **ular shu promptning haqiqat manbai**.

Ilgari: nashr qilingan **har** test **har** sessiyaga avtomatik qo'shilardi.
Endi: testlar **dastur** (`AssessmentProgram`) ichida guruhlanadi; dastur maktabga
biriktiriladi yoki ommaviy bo'ladi; o'quvchi kirishda mavjud dasturlardan bittasini tanlaydi.

## O'qish shart
- `docs/06-arxitektura.md` 8-bo'lim — 2026-09-02 sanali 5 ta qaror (**majburiy**)
- `docs/04-domain-model.md` (2.3 `Assessment`, 2.7 `TestDefinition`)
- `docs/05-database-schema.md` (jadval ta'riflari, indeks konvensiyalari)
- `docs/03-psixologik-metodikalar.md` (3.3 `MaturityIndex`, 5.2 `ActivityIndex`)
- `docs/07-api-shartnoma.md` (1.1, 1.2 — ommaviy oqim)

## Vazifa

### A. Domain
1. `AssessmentProgram` agregati: `Code` (unikal), `NameUz`, `DescriptionUz`,
   `Kind` (`System`|`Custom`), `Visibility` (`Public`|`Assigned`),
   `Status` (`Draft`|`Published`|`Archived`), `IsActive`, `DisplayOrder`,
   `IsSystem`, `CreatedByAdminUserId?`, `CreatedAt`/`UpdatedAt`.
   Metodlar: `AddTest`, `RemoveTest`, `ReorderTests`, `Publish`, `Archive`,
   `SetVisibility`, `Activate`/`Deactivate`. `IsSystem = true` da tarkib
   o'zgartirilmaydi (`DomainException`, BR-8 ruhida).
2. `ProgramTest` — `(ProgramId, TestDefinitionId, DisplayOrder)`, tartiblangan to'plam.
3. `SchoolProgram` — `(SchoolId, ProgramId)`, faqat `Visibility = Assigned` uchun.
4. `TestDefinition.ScoringMode`: `Scored = 1` | `Survey = 2`.
   `Survey` da `ScoringStrategyCode` ishlatilmaydi.
5. `Assessment.ProgramId` (majburiy). `Assessment.Create` dasturni talab qiladi.

### B. DB va migratsiya
6. `assessment_programs`, `program_tests`, `school_programs` jadvallari;
   `test_definitions.scoring_mode`, `assessments.program_id`.
   Nom/indeks konvensiyalari `docs/05` bo'yicha (snake_case, `ux_`/`ix_` prefikslari).
7. **Ma'lumot migratsiyasi (idempotent, seeder orqali):** mavjud 4 metodika
   `PERSONALITY_PROFILE` ("Shaxsiyat profili") tizim dasturiga birlashtiriladi —
   `Kind = System`, `Visibility = Public`, `IsSystem = true`, `Status = Published`,
   tartib `DisplayOrder` bo'yicha. Mavjud barcha `assessments` shu dasturga bog'lanadi.
   **Destruktiv o'zgarish ikki bosqichda** (`CLAUDE.md` 7-qoida): avval ustun
   `nullable` qo'shiladi va to'ldiriladi, keyingi migratsiyada `NOT NULL` qilinadi.

### C. Sessiya oqimi
8. `GET /api/public/schools/{slug}?k=` javobi endi `programs[]` qaytaradi:
   `code`, `nameUz`, `descriptionUz`, `testCount`, `questionCount`,
   `estimatedMinutes`. Maktab uchun mavjud dasturlar = `Visibility = Public`
   **yoki** `school_programs` da biriktirilganlar; faqat `Status = Published && IsActive`.
9. `StartSessionCommand` ga `programCode` qo'shiladi.
   - Bo'sh bo'lsa va maktabda **aynan bitta** dastur bo'lsa — o'sha tanlanadi;
   - bir nechta bo'lsa va `programCode` berilmasa → `400 PROGRAM_REQUIRED`;
   - berilgan dastur o'sha maktabda mavjud bo'lmasa → `404` (mavjudligini oshkor qilma).
10. Sessiyaga **faqat o'sha dasturning** testlari qo'shiladi, dasturdagi tartibda.
11. Boshlangan sessiyalar dastur o'zgarsa ham o'zgarmaydi (BR-10 ruhida).

### D. ⚠️ Shartli scoring — bu promptning eng nozik qismi
12. `CompleteSessionCommandHandler`:
    - `MaturityIndex` **faqat** sessiyada BIG5 **va** ACTIVITY natijalari bo'lsa hisoblanadi;
    - `ActivityIndex` faqat ACTIVITY bo'lsa;
    - yo'q bo'lsa maydonlar `null` qoladi — **0 yozilmaydi** (0 "past ball" degan
      ma'noni beradi va butun mahsulotni aldaydi);
    - `ReliabilityScore` avvalgidek hisoblanadi (u har qanday javob to'plamiga tegishli),
      **P12-R1 qoidasi kuchda**: `ReliabilityInputBuilder` ishlatiladi, o'zingdan tartiblash yozma.
13. `Survey` (`ScoringMode = Survey`) testlar:
    - javoblari saqlanadi, `TestResult` ball yozilmaydi;
    - `ReliabilityCalculator` kirishiga **kirmaydi** (ballanmagan javob ishonchlilikni
      buzadi — teskari savol tushunchasi yo'q);
    - `CompositeScorer` va AI xulosasiga ta'sir qilmaydi.
14. `StudentSnapshot` maydonlari (`LastPersonalityType`, `LastMaturityIndex`,
    `LastActivityIndex`, `LastActivityLevel`, `NeedsAttention`) **nullable** bo'ladi va
    faqat tegishli ma'lumot mavjud bo'lganda yangilanadi. Mavjud qiymat
    ma'lumotsiz sessiya tufayli **o'chirilmaydi**.

### E. Admin API (minimal — to'liq UI P35 da)
15. `AssessmentPrograms` CRUD, `publish`/`archive`, `toggle-active`,
    testlarni qo'shish/olib tashlash/tartiblash, maktablarga biriktirish/olib tashlash.
    `[Authorize(Policy = SuperAdminPolicy)]` + `[EnableRateLimiting(RateLimitSetup.AdminApi)]`.
16. Audit: `Program.Created/Updated/Published/Archived/Assigned/Unassigned`.

## Cheklovlar
- Ilmiy metodikalar himoyalangan qoladi: `IsSystem` test va `IsSystem` dastur tarkibi
  o'zgartirilmaydi (`409 SYSTEM_TEST_LOCKED` / `SYSTEM_PROGRAM_LOCKED`).
- `Draft` yoki nofaol dastur hech qachon o'quvchiga ko'rinmaydi.
- `Application` da EF paketi yo'q; handler'lar `internal sealed`; validator'lar `public sealed`.
- Xotirada saralash/agregatsiya taqiqlanadi (`docs/06` §8, P14 qarori).
- `scale`/`scaleDirection` o'quvchi API'siga chiqmaydi (CLAUDE.md 9-qoida).

## DoD
- [ ] Migratsiyadan keyin mavjud 4 metodika bitta tizim dasturida, eski sessiyalar unga bog'langan
- [ ] Maktabga faqat bitta dastur biriktirilgan bo'lsa, o'quvchi oqimi **avvalgidek** ishlaydi
      (tanlov ekranisiz) — regressiya testi majburiy
- [ ] Ikki dastur biriktirilgan bo'lsa `programCode`siz `400 PROGRAM_REQUIRED`
- [ ] Boshqa maktabga biriktirilgan dastur bilan sessiya ochib bo'lmaydi (`404`)
- [ ] **Faqat `Survey` dasturi bilan sessiya:** yakunlanadi, `MaturityIndex`/`ActivityIndex`
      `null` qoladi (0 emas), `StudentSnapshot` eski qiymatlarini o'chirmaydi, xato bermaydi
- [ ] Faqat BIG5 bo'lgan dasturda `MaturityIndex` `null` (ACTIVITY yo'q)
- [ ] `Survey` javoblari `ReliabilityCalculator` kirishiga kirmaydi
- [ ] `dotnet test` yashil, `has-pending-model-changes` bo'sh, 0 ogohlantirish

## Tekshiruv
```bash
dotnet build && dotnet test
ConnectionStrings__Postgres="..." dotnet ef migrations has-pending-model-changes -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api
```
