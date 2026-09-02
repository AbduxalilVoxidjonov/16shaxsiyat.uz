# P37 — Test katalogi CRUD (backend)

## Kontekst
P35 (admin UI) qurilgan va sinalgan, lekin **ulanadigan backend yo'q**: `docs/07` §3.4 dagi
katalog endpointlari hech qachon yozilmagan. Natijada "Savollar" bo'limi va "Test yuklash"
ishlamaydi, hamda dastur tahrirlashda "test tanlash" ro'yxati bo'sh qoladi.

Bu `prompts/33` (anketa konstruktori) ning **backend qismi** — konstruktor UI'si P33 da qoladi.

## O'qish shart
- `docs/07-api-shartnoma.md` (**3.4 — to'liq katalog API**)
- `docs/02-biznes-talablar.md` (FR-6.5…FR-6.9, **BR-8** — tizim metodikalari qulfi)
- `docs/03-psixologik-metodikalar.md` (6-bo'lim — `SUM` va nashr validatsiyasi)
- `docs/04-domain-model.md` (2.7 `TestDefinition`, `TestScale`)
- `docs/05-database-schema.md`, `docs/06-arxitektura.md` (8-bo'lim, 2026-09-02 qarorlari)
- `prompts/04-katalog-va-seed-infratuzilma.md` (**JSON sxemasi — import shu shaklda**)
- `prompts/34` (dastur modeli, `ScoringMode`)

## Vazifa

### A. Domain
1. `TestScale` entity + `InterpretationBand` value object (`docs/04` 2.7) — `SUM` strategiyasi
   uchun. `Domain/Scoring/SumStrategy.cs` allaqachon `InterpretationBand` bilan ishlaydi
   (`ScoringInput.ScaleBands`) — shu shaklga mos bo'lsin.
2. `TestDefinition` ga: `AddScale`, `RemoveScale`, `Publish`, `Archive`, `Duplicate`,
   `BumpVersion`. `IsSystem = true` da tarkib o'zgartiruvchi amallar `DomainException`
   (`SYSTEM_TEST_LOCKED`) beradi — **BR-8**.

### B. Application va API — `docs/07` §3.4 dagi **barcha** endpointlar
3. Test CRUD: ro'yxat (filtr: `kind`, `status`, `scoringMode`), detal, yaratish, yangilash.
4. `publish` — `docs/03` 6.3 validatsiyasi bilan; xatolar **ro'yxat** sifatida qaytadi
   (`400 TEST_NOT_PUBLISHABLE`, har xatoda `code`, `scale`/`questionCode`, `message`).
5. `duplicate`, `archive`, `toggle-active`, `preview`.
6. Shkala CRUD; savol CRUD va `reorder`.
7. **`POST .../questions/import`** — `prompts/04` dagi seed JSON sxemasi bilan **bir xil** shakl.
   Import natijasi: yaratilgan test **`Draft`** holatida. Dublikat `code` → `409`.
8. Ishlatilgan testni o'chirish → `409 TEST_IN_USE` (dasturda yoki sessiyada bo'lsa).
9. Nashr etilgan testga savol qo'shilsa/olib tashlansa `Version++` (BR-9) —
   **eski natijalar qayta hisoblanmaydi**, faqat `TestResult.TestVersion` bilan belgilanadi.
10. Katalog keshini (`PublicCatalogCache`, P11) nashr/o'zgarishda **invalidatsiya qil** —
    aks holda o'quvchi eski savollarni ko'radi va buni hech kim payqamaydi.
11. Audit: `Catalog.TestCreated/Updated/Published/Archived/Deleted`, `Catalog.ScaleChanged`,
    `Catalog.QuestionAdded/Removed`, `Catalog.VersionBumped`.

## Cheklovlar
- **Tizim metodikalarining savol soni, `Scale`, `Direction`, `Weight` hech qanday yo'l bilan
  o'zgartirilmasin** — domain, API, uchala darajada (`CLAUDE.md` 9a).
- `Draft` test hech qachon o'quvchi sessiyasiga tushmasin.
- `MaturityIndex`/`ActivityIndex` formulalariga custom testlar ta'sir qilmasin.
- Xotirada saralash/agregatsiya taqiqlanadi; `pageSize` ≤ 100; sort oq ro'yxatda.
- `[Authorize(Policy = SuperAdminPolicy)]` + `[EnableRateLimiting(RateLimitSetup.AdminApi)]`.
- `Application` da EF paketi yo'q; handler'lar `internal sealed`; validator'lar `public sealed`.

## DoD
- [ ] P35 dagi UI ishlaydi: katalog ro'yxati, test detali, JSON import
- [ ] Dastur tahrirlashda "test tanlash" ro'yxati to'ladi
- [ ] Tizim metodikasiga savol qo'shishga urinish → `409 SYSTEM_TEST_LOCKED` (test bilan)
- [ ] Nashr validatsiyasi barcha xato turlarini ro'yxat bilan qaytaradi
- [ ] Nashrdan keyin katalog keshi invalidatsiya bo'ladi (test bilan)
- [ ] Import: to'g'ri JSON → `Draft` test; dublikat kod → `409`; buzilgan JSON → aniq xato
- [ ] `dotnet test` yashil, `has-pending-model-changes` bo'sh, 0 ogohlantirish

## Tekshiruv
```bash
dotnet build && dotnet test
ConnectionStrings__Postgres="..." dotnet ef migrations has-pending-model-changes -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api
```
