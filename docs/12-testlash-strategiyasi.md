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
7. **Migratsiya:** bo'sh DB'da barcha migratsiya xatosiz o'tadi; `dotnet ef migrations has-pending` bo'sh.
8. **Maxfiylik testi (majburiy):** `PromptBuilder` chiqishida `fullName`, `phone`, `email`,
   `birthDate` yo'qligi tekshiriladi.

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

Test ma'lumoti: har run oldidan `seed-e2e` skripti bilan tayyorlanadi (izolyatsiya uchun
alohida maktab slug'i).

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
2. dotnet test --collect:"XPlat Code Coverage"     → qoplama chegarasi tekshiriladi
3. dotnet ef migrations has-pending-model-changes  → bo'sh bo'lishi shart
4. npm ci && npm run lint && npm run typecheck && npm run test
5. npm run generate:api → git diff bo'sh bo'lishi shart (kontrakt sinxron)
6. npm run build
7. Playwright (faqat main branch va release PR)
```

Yashil quvursiz merge yo'q.
