# 12 — Testlash strategiyasi

## 1. Piramida

```
        ▲  E2E (Playwright)          ~10 stsenariy
       ███  Integration (API + DB)   ~40 test
     ██████  Unit (Domain + App)     ~200 test
```

| Daraja | Vosita | Nima qamraladi |
|--------|--------|----------------|
| Unit | xUnit + FluentAssertions | Scoring strategiyalari, reliability, value object, domen qoidalari |
| Unit (front) | Vitest + Testing Library | Komponentlar, store, autosave navbati |
| Integration | xUnit + Testcontainers (Postgres) + `WebApplicationFactory` | Endpointlar, EF konfiguratsiya, migratsiya, transaksiya |
| Contract | Schema snapshot testlari | API DTO va AI JSON schema o'zgarmasligi |
| E2E | Playwright | To'liq o'quvchi oqimi va admin asosiy yo'llari |

**Minimal qoplama:** `Domain/Scoring` — 100% (majburiy), `Application` — ≥ 70%, umumiy ≥ 60%.

---

## 2. Scoring "oltin testlari" (eng muhim)

Har metodika uchun `tests/StudentRoadMap.Domain.Tests/Scoring/GoldenCases/` da JSON fayllar:

```json
{
  "name": "MBTI16 — aniq INTJ",
  "testCode": "MBTI16",
  "answers": { "MB-Q01": 2, "MB-Q02": 4, "…": 0 },
  "expected": {
    "resultCode": "INTJ",
    "axes": { "EI": 28.3, "SN": 71.6, "TF": 33.3, "JP": 64.1 },
    "borderlineAxes": []
  },
  "tolerance": 0.1
}
```

Majburiy holatlar:

| Test | Holat |
|------|-------|
| MBTI16 | Aniq INTJ · Aniq ESFP · Chegaraviy (`EI = 50`) → tie-break `I` · Barcha javob 3 → hamma o'q 50 |
| BIG5 | Barcha maksimal → hamma omil 100 · Barcha minimal → 0 · Teskari savollar to'g'ri hisoblanishi · `MaturityIndex` qo'lda hisoblangan qiymatga teng |
| RIASEC | Aniq `IRA` · Teng ballar → alifbo tie-break · Past differensiatsiya (`< 20`) bayrog'i |
| ACTIVITY | `ActivityIndex` chegaralari: 30/31, 50/51, 70/71, 85/86 — daraja to'g'ri o'zgarishi |
| Reliability | Barcha javob bir xil → jarima 50 · 20 ta ketma-ket bir xil → straight-line jarima · Tez javoblar → jarima · Toza sessiya → 100 |

**Qoida:** scoring formulasi o'zgarsa — oltin testlar **avval** yangilanadi (kutilgan qiymat
qo'lda hisoblanadi), keyin kod.

---

## 3. Unit testlar (domen)

- `School.RegenerateAccessToken()` — yangi token eskisidan farq qiladi, uzunligi to'g'ri.
- `Assessment` holat mashinasi — noto'g'ri o'tishlar istisno beradi
  (`Draft → Analyzed` mumkin emas).
- `Assessment.Complete()` — barcha testlar tugamagan bo'lsa xato.
- `PhoneNumber` — `998901234567`, `+998 90 123 45 67`, `901234567` → bir xil normal shakl;
  noto'g'ri format → xato.
- `SchoolSlug` — bo'sh joy `-` ga, katta harf kichrayadi, o'zbek harflari translit (`o'` → `o`).
- `Student` — dublikat normalizatsiya (`  Aliyev   Sardor ` → `ALIYEV SARDOR`).

---

## 4. Application testlari

Handlerlar in-memory yoki mock `IAppDbContext` bilan:

- `StartSessionHandler`: yangi o'quvchi → yaratiladi; dublikat + tugallanmagan sessiya →
  o'sha sessiya (`resumed=true`); dublikat + 90 kun ichida yakunlangan → `DUPLICATE_ASSESSMENT`.
- `SaveAnswersHandler`: bir savolga ikki marta javob → bitta yozuv, `RevisionCount=1`.
- `CompleteTestHandler`: to'ldirilmagan majburiy savol → xato; to'liq → `TestResult` yaratiladi.
- `CompleteSessionHandler`: reliability hisoblanadi, AI job navbatga qo'yiladi (mock queue tekshiradi).
- `RerunAnalysisHandler`: yangi `AiAnalysis` yaratiladi, eski `IsCurrent` faqat muvaffaqiyatdan keyin o'zgaradi.
- Validatorlar: har `Command` uchun chegaraviy qiymatlar (sinf 0/1/11/12, yosh, telefon).

---

## 5. Integration testlar (Testcontainers)

```csharp
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();
    // Migratsiya + seed har test klassi uchun bir marta, testlar orasida Respawn bilan tozalash
}
```

Qamrov:
1. **To'liq ommaviy oqim:** maktab yaratish → `GET /schools/{slug}` → sessiya ochish →
   4 testni to'ldirish → yakunlash → `TestResult` lar to'g'ri → AI mock `Analyzed` qiladi.
2. **Auth:** login → access token bilan admin endpoint 200; tokensiz 401; noto'g'ri rol 403.
3. **Refresh rotatsiyasi:** eski refresh token qayta ishlatilsa — barcha tokenlar bekor.
4. **Rate limit:** 11-chi ro'yxatdan o'tish so'rovi 429.
5. **IDOR:** A sessiya tokeni bilan B sessiyasining ma'lumotiga kirish imkoni yo'q.
6. **Soft delete:** o'chirilgan maktab ro'yxatda chiqmaydi, havolasi 410.
7. **Migratsiya:** alohida loyihada — §5.1 ga qara.
8. **Maxfiylik testi (majburiy):** `PromptBuilder` chiqishida `fullName`, `phone`, `email`,
   `birthDate` yo'qligi tekshiriladi.

> ⚠️ **Haqiqiy holat (2026-09-03):** `tests/StudentRoadMap.Api.IntegrationTests` hozircha
> `PublicApiTestFactory` orqali SQLite + `EnsureCreated()` ustida ishlaydi — ya'ni u
> **migratsiyalarni bajarmaydi**. Shu sabab migratsiya yo'li alohida loyihada, haqiqiy
> PostgreSQL konteynerida sinaladi (§5.1).

---

## 5.1. Migratsiya testlari — `tests/StudentRoadMap.Migrations.Tests` (MAXSUS DIQQAT)

**Nima uchun alohida qatlam.** Barcha boshqa sinov loyihalari sxemani **modeldan**
(`EnsureCreated()` yoki SQLite) quradi — migratsiyalar umuman bajarilmaydi. Oqibati P34 da
amalda ko'rindi (`docs/13` §8): ikki bosqichli migratsiyada mavjud `assessments` qatorlari
`program_id`siz qolib FK/`NOT NULL` cheklovini buzardi va **deploy to'xtadi** — hech bir
test buni ushlay olmasdi. Bu loyiha aynan shu bo'shliqni yopadi.

**Nima sinaladi (11 test):**

| Sinf | Nima tekshiriladi |
|------|-------------------|
| `MigrateFromScratchTests` | Bo'sh bazada `Database.Migrate()` xatosiz o'tadi; `HasPendingModelChanges()` bo'sh; **migratsiya bilan qurilgan HAQIQIY sxema modeldan qurilgan sxema bilan bir xil** (ustunlar, PK/FK/UNIQUE/CHECK, indekslar, kengaytmalar) |
| `BackfillMigrationTests` | **Eng muhim:** migratsiyalar oraliq nuqtagacha qo'llanadi (`AddAuditLogsAndTotpSupport` — `program_id` ustuni hali yo'q; `AddAssessmentPrograms` — ustun bor, qiymatlar `NULL`), o'sha sxemaga mos ma'lumot yoziladi, keyin qolgan migratsiyalar qo'llanadi va backfill natijasi tasdiqlanadi (yetim/nol-GUID qator yo'q, FK kuchda, `SET NOT NULL` buzilmaydi). Shuningdek `migrate → seed` topshirig'i |
| `MigrationIdempotencyTests` | `Migrate()` ikki marta — xato yo'q, sxema va qator sonlari o'zgarmaydi |
| `SeedIdempotencyTests` | `DbSeeder` ikki marta — dublikat yo'q (tizim metodikalari, tizim dasturi, superadmin) |

**Ishga tushirish.**

```bash
# Odatiy yurishga kiradi (Docker bo'lsa) — hozircha ~3 s, ya'ni ajratish shart emas
dotnet test

# Faqat migratsiya testlari
dotnet test --filter "Category=Migrations"

# Migratsiya testlarisiz (CI `backend` job shu rejimda ishlaydi — Docker kerak emas)
dotnet test --filter "Category!=Migrations"
```

**Docker yo'q bo'lsa nima bo'ladi.** Testlar **jimgina o'tib ketmaydi** — bu eng yomon
variant bo'lardi (bo'shliq yopilgandek ko'rinadi, aslida yopilmagan). Ikki xatti-harakat:

- odatiy holatda — test **aniq sabab bilan `Skip`** bo'ladi (natijalar jadvalida ko'rinadi);
- `SRM_REQUIRE_DOCKER=1` bo'lsa — `Skip` umuman qo'yilmaydi, test tushunarli xato bilan
  **yiqiladi**. CI (`.github/workflows/ci.yml` → `migrations` job) shu rejimda ishlaydi.

**Xavfsizlik.** Testlar jonli bazaga hech qachon ulanmaydi: ulanish satri faqat konteynerdan
olinadi (`.env`/`appsettings`/`ConnectionStrings__Postgres` o'qilmaydi), host porti tasodifiy,
foydalanuvchi/parol har yurishda generatsiya qilinadi, va har baza nomi `srm_mig_` prefiksi
bilan tekshiriladi (`MigrationsPostgresFixture` dagi qat'iy to'siq).

**Ma'lum sxema farqlari (`SchemaDriftBaseline`).** Sxema taqqoslash testi bugungi ikki farqni
hujjatlashtirilgan "baseline" sifatida qabul qiladi — **yangi** farq esa darhol qizil beradi:

1. `admin_users.concurrency_stamp` — migratsiya qilingan bazada `DEFAULT '000…0'::uuid`
   qoladi, modelda yo'q (`AddAuditLogsAndTotpSupport` da EF `defaultValue` yozgan). **Ochiq
   qarz:** alohida migratsiya bilan `DROP DEFAULT` qilinishi kerak.
2. `ix_students_last_at` — `NULLS LAST` ataylab xom SQL bilan qo'yilgan (`docs/05` §2/§5),
   EF fluent API'da ifodalanmaydi. Tuzatish shart emas, qayd etilgan.

---

## 6. AI modul testlari

- `AiResponseValidator`: to'g'ri JSON → o'tadi; maydon yetishmasa → `Schema` xatosi;
  taqiqlangan so'z (`"depressiya"`) → moderatsiya oqimi.
- `AiProviderResolver`: kaliti yo'q providerlar ro'yxatga tushmaydi; default to'g'ri tanlanadi;
  fallback zanjiri `fallback_order` bo'yicha.
- Retry: mock provider ketma-ket `Timeout, Timeout, Success` → uchinchi urinishda muvaffaqiyat,
  3 ta `AiAnalysis` yozuvi.
- `Auth` xatosi → retry qilinmaydi, darhol keyingi provider.
- Har provider uchun **so'rov tanasi snapshot testi** (JSON schema to'g'ri joyga qo'yilganini
  tekshirish) — real tarmoq chaqiruvisiz.
- Real provider chaqiruvlari faqat qo'lda ishga tushiriladigan `[Trait("Category","Live")]`
  testlarda, CI'dan chiqarilgan.

---

## 7. Frontend testlari

- `LikertQuestion` — tanlov, klaviatura (1..5), `durationMs` hisoblash.
- `useAutosave` — debounce, offline navbat, online bo'lganda yuborish, xatoda qayta urinish.
- `sessionStore` — persist, `410` da tozalash.
- `AiReportView` — barcha bo'limlar render bo'ladi, bo'sh massivlarda yiqilmaydi.
- Jadval filtrlari — URL query bilan sinxron.
- a11y: `jest-axe` (yoki `axe-playwright`) bilan asosiy sahifalarda buzilish yo'qligi.

### 7.1 API mock'lari SXEMADAN tiplanadi (majburiy)

Barcha API mock'lari `src/test/apiMock.ts` yordamchilaridan foydalanadi va tanasi
`satisfies components['schemas'][...]` bilan tekshiriladi (`docs/10` §6.4).

Test faylida o'z `function jsonResponse(body: unknown, …)` nusxasini yozish **taqiqlanadi**.
Sabab: `unknown` hech narsani tekshirmaydi, shu sabab mock backend shartnomasidan uzilib
qolsa ham test YASHIL qolardi — ya'ni test frontendning o'z taxminini tasdiqlardi, backendni
emas. 2026-09-02/03 dagi oltita nomuvofiqlikning hech biri testda ushlanmagani sababi shu
(§11).

Mock sxemaga mos kelmasa — bu **topilma**, chetlab o'tiladigan noqulaylik emas: yo mock
backenddan uzilgan (tuzatiladi), yo `schema.d.ts` eskirgan (`typedResponse<FeatureDto>` +
maydon darajasidagi izoh, va nom `eslint.config.js` istisno ro'yxatiga tushadi).

---

## 8. E2E stsenariylari (Playwright)

| № | Stsenariy |
|---|-----------|
| E2E-1 | O'quvchi havoladan kiradi → anketa → 4 testni to'liq yechadi → rahmat ekrani |
| E2E-2 | Test o'rtasida sahifa yangilanadi → o'sha savoldan davom etadi, javoblar joyida |
| E2E-3 | Offline rejim (network throttle) → banner chiqadi → online bo'lganda javoblar yuboriladi |
| E2E-4 | Nofaol maktab havolasi → tushunarli xato ekrani |
| E2E-5 | Dublikat o'quvchi → tegishli xabar |
| E2E-6 | Superadmin login → maktab yaratadi → havolani nusxalaydi → QR yuklaydi |
| E2E-7 | Superadmin o'quvchi profilini ochadi → barcha diagramma va AI hisobot ko'rinadi |
| E2E-8 | Qayta tahlil → holat `Analyzing` → `Analyzed` (mock provider) |
| E2E-9 | Excel eksport → fayl yuklanadi, qatorlar soni filtrga mos |
| E2E-10 | Mobil viewport (390×844) da to'liq oqim ishlaydi |

Test ma'lumoti: har test O'ZI maktab yaratadi (`school` fixture, unikal nom → unikal slug)
va oxirida o'quvchilari bilan birga o'chiradi — testlar bir-biriga bog'liq emas.

### 8.1 Amalga oshirish (P30)

| Nima | Qayerda |
|------|---------|
| Playwright sozlamasi | `frontend/playwright.config.ts` |
| Fixture'lar, yordamchilar | `frontend/e2e/support/` |
| Stsenariylar | `frontend/e2e/tests/*.e2e.ts` |
| Izolyatsiyalangan stek | `docker-compose.e2e.yml` |

**Muhit — jonli saytga qarshi ISHLAMAYDI.** `globalSetup` `docker-compose.e2e.yml` ni
**alohida compose loyihasi** (`shaxsiyat-e2e`) sifatida ko'taradi: alohida bo'sh baza
(`studentroadmap_e2e`, har run boshida `down -v`), har run uchun tasodifiy generatsiya
qilinadigan DB paroli/`Jwt__Key`/`EncryptionKey`, API faqat `127.0.0.1:5081` da, tunnel yo'q.
Frontend shu yerda `vite build` bilan quriladi (`VITE_API_BASE_URL=` — same-origin) va
`e2e/support/preview-server.mjs` orqali `127.0.0.1:5199` da uzatiladi; `/api` shu serverda
E2E backendiga proksilanadi — jonli `web` (nginx) sxemasining aynan o'zi. Ikki qatlamli
himoya: (1) preview server build ichida `16shaxsiyat.uz` topsa umuman ishga tushmaydi;
(2) har test jonli domenga so'rov ketmaganini tekshiradi.

**Fayl nomlash:** stsenariylar `*.e2e.ts` (`*.test.ts`/`*.spec.ts` EMAS) — shu tufayli
`npm run test` (vitest) ularni yig'ib olmaydi va E2E alohida `npm run test:e2e` bilan yuradi.

**Tezlik cheklovi:** `POST /api/public/sessions` IP bo'yicha soatiga 10 ta. Har test
o'zining "mijoz IP"sini cookie orqali e'lon qiladi, preview server uni `X-Forwarded-For`
ga aylantiradi (E2E stekida `App__KnownProxies=0.0.0.0/0`) — shu tufayli to'plam ketma-ket
bir necha marta ishlay oladi.

**Har asosiy ekranda uchta tekshiruv birga:** `axe` (jiddiy — `serious`/`critical`
buzilish bo'lsa test yiqiladi), gorizontal scroll YO'Qligi (390px va 1440px) va konsolda
xato YO'Qligi. Hozircha tuzatilmagan topilmalar `frontend/e2e/support/known-issues.ts`
da ro'yxatlangan — YANGI buzilish baribir testni yiqitadi. Kamchilik tuzatilgach yozuv
shu fayldan o'chiriladi.

**Auditda ko'rish:** `E2E_A11Y_REPORT=1 npm run test:e2e` — barcha topilmalar testni
yiqitmasdan ro'yxatlanadi.

---

## 9. Yuklama testi

`k6` yoki `NBomber`:
- 300 virtual foydalanuvchi, har biri 5 s da bir javob paketi yuboradi — 10 daqiqa.
- Maqsad: `POST /answers` p95 < 300 ms, xato < 0.5%.
- DB ulanish pooli yetarliligi va CPU tekshiriladi.

---

## 10. CI quvuri (har PR'da)

```
1. dotnet restore / build (warnings as errors)
2. dotnet test --filter "Category!=Migrations" --collect:"XPlat Code Coverage"
                                                   → qoplama chegarasi tekshiriladi
3. dotnet ef migrations has-pending-model-changes  → bo'sh bo'lishi shart
4. dotnet test --filter "Category=Migrations"      → ALOHIDA job, SRM_REQUIRE_DOCKER=1
                                                     (haqiqiy Postgres 16, §5.1)
5. npm ci && npm run lint && npm run typecheck && npm run test
6. npm run generate:api → git diff bo'sh bo'lishi shart (kontrakt sinxron)
7. npm run build
8. Playwright (faqat main branch va release PR)
```

Yashil quvursiz merge yo'q.

**6-darvoza (`api-contract-sync` job) qanday ishlaydi — tasdiqlangan:**
API `ASPNETCORE_ENVIRONMENT=Development` bilan ko'tariladi (Swagger faqat shu muhitda
yoqiladi, `Program.cs`), tayyorlik `/health` orqali kutiladi. `/health` da
`Predicate = _ => false` — hech qanday tekshiruv bajarilmaydi, shu sabab **DB kerak emas**
(`--migrate`/`--seed` chaqirilmaydi, DB ulanishi ochilmaydi). Keyin `npm run generate:api`
ishga tushadi, fayl haqiqatan qayta yozilgani tekshiriladi (bo'sh emas, `components` bloki
bor — aks holda `git diff` aldab bo'sh chiqardi) va `git diff --exit-code` bilan
solishtiriladi.

---

## 11. Shartnoma driftidan himoya — uch qatlam (2026-09-03)

2026-09-02/03 da frontend va backend orasida bir kunda **oltita** nomuvofiqlik topildi
(`results.mbti16` ↔ `MBTI16`; RIASEC `A` ↔ `ART`; `backupCodes` ↔ `recoveryCodes`;
`currentPassword` ↔ `password`; AI provayder `baseUrl`; AI hisobotining 5 bo'limi).
Ularning **hech biri** testda ushlanmagan. Ildiz sabab ikkita: DTO'lar qo'lda yozilardi va
mock'lar `unknown` edi.

| Qatlam | Nima ushlaydi | Qayerda |
|---|---|---|
| 1. Re-export | DTO shakli farqi (maydon nomi, tip, nullability) | `features/*/model/**` → `components['schemas'][…]`, `docs/10` §6.2 |
| 2. ESLint qo'riqchisi | **Yangi** qo'lda yozilgan DTO | `eslint.config.js`, `docs/10` §6.3 |
| 3. Tiplangan mock'lar | Mock ↔ shartnoma uzilishi | `src/test/apiMock.ts`, §7.1 |
| 4. CI drift | `schema.d.ts` ning o'zi eskirishi | `api-contract-sync` job, §10 |

**Qolgan bo'shliq (ochiq qayd).** `schema.d.ts` eskirgan bo'lsa 1-qatlam ishlamaydi — bunday
tiplar qo'lda qoladi va `eslint.config.js` dagi "VAQTINCHALIK ISTISNOLAR" ro'yxatiga
tushadi. Bu ro'yxat — texnik qarzning **ochiq reyestri**: har `npm run generate:api` dan
keyin u qisqarishi shart. Ro'yxat uzayib borsa — himoya zaiflashmoqda demakdir.
