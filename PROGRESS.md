# PROGRESS — loyiha holati jurnali

> Bu fayl **PM agentning xotirasi**. Sessiya uzilsa ham ish shu yerdan davom etadi.
> PM har vazifa boshlanganda va tugaganda **darhol** yangilaydi.

**Loyiha:** Shaxsiyat (`16shaxsiyat.uz`) · ichki nom `StudentRoadMap`
**Oxirgi yangilanish:** 2026-09-11 · **Joriy bosqich:** B8 (Anketa konstruktori — tarmoqlanish) · **Joriy vazifa:** P52b (namunaviy so'rovnomani seed qilish) yakunlandi; navbatdagi ish kutilmoqda

---

## Holat belgilari

`⬜ Kutmoqda` · `🟡 Ish jarayonida` · `🔵 Review'da (PR ochilgan)` · `✅ Merge qilingan` · `⛔ Bloklangan`

---

## Vazifalar jadvali

| № | Vazifa | Agent | Holat | PR | Izoh |
|---|--------|-------|-------|----|------|
| P01 | Solution'ni Clean Architecture ga o'tkazish | backend-dotnet | ✅ | — | QA: PASS · PR oqimi yo'q (egasi push qilmaslikni tanladi), ish `main` da |
| P02 | Domain qatlami | backend-dotnet | ✅ | `84ebaed` | 43 fayl · 193 test · QA testlari alohida agentda |
| P03 | EF Core, DbContext, migratsiya | backend-dotnet | ✅ | `9bb42d2` | QA: PASS (3 topilma tuzatildi) · jonli DB tekshiruvi qoldi |
| P04 | Katalog va seed infratuzilmasi | backend-dotnet | ✅ | — | QA: FAIL→PASS · SQLite in-memory bilan 18 test |
| P05 | 16 tip savol banki (60) | scoring-psychometrics | ✅ | — | QA: PASS · JSON tayyor; seed integratsiyasi P04 da |
| P06 | Big Five savol banki (50) | scoring-psychometrics | ✅ | — | QA: PASS · JSON tayyor; naqsh 2 marta qaytarildi |
| P07 | RIASEC savol banki (48) | scoring-psychometrics | ✅ | — | QA: PASS · JSON tayyor |
| P08 | Aktivlik anketasi (32) | scoring-psychometrics | ✅ | — | QA: PASS · JSON tayyor; shkalalar aralashtirildi |
| P09 | Scoring engine + oltin testlar | scoring-psychometrics | ✅ | — | **Kritik** · QA: FAIL→PASS · 2 bloklovchi tuzatildi · 127 scoring testi |
| P10 | Application skeleti + sessiya API | backend-dotnet | ✅ | — | QA: PASS · 3 endpoint · ForwardedHeaders tuzatildi |
| P11 | Savol va javob API | backend-dotnet | ✅ | — | QA: PASS · kesh va IDOR toza · til qo'llab-quvvatlash qo'shildi |
| P12 | Yakunlash va scoring ulash | backend-dotnet | ✅ | — | QA: PASS · P12-R1/R2 mutatsiya sinovidan o'tdi |
| P13 | Auth va JWT | backend-dotnet | ✅ | — | QA: PASS · TOTP poyga holati tuzatildi |
| P14 | Maktab va o'quvchi admin API | backend-dotnet | ✅ | — | QA: PASS · xotirada saralash tuzatildi |
| P15 | Sessiya va dashboard API | backend-dotnet | ✅ | — | QA: FAIL→PASS · kesh bloklovchisi tuzatildi |
| P16 | AI abstraksiya va prompt | ai-integration | ✅ | — | maxfiylik testi majburiy · validator 5 bosqich |
| P17 | 3 provider (Gemini/OpenAI/Anthropic) | ai-integration | ✅ | — | 709 test · **jonli kalit bilan sinalmagan** |
| P18 | Fon navbati, fallback, AI config API | ai-integration | ✅ | — | `analysis_jobs` + `BackgroundService` · navbat commit'dan keyin (P18-R1) |
| P19 | Frontend skeleti | frontend-react | ✅ | — | QA: PASS · 18 test · Vite qoldiqlari tozalandi |
| P20 | Ommaviy UI: landing va anketa | frontend-react | ✅ | — | QA: PASS · 63 test · 390px vizual tekshiruv qoldi |
| P21 | Ommaviy UI: test oqimi, autosave | frontend-react | ✅ | — | QA: PASS · 127 test · javob yo'qolmasligi qulflangan |
| P22 | Admin skelet va login | frontend-react | ✅ | — | QA: PASS · 171 test · DataTable qayta ishlatiladi |
| P23 | Admin: maktablar va havolalar | frontend-react | ✅ | — | QA: PASS · 183 test · QR, havola, drawer |
| P24 | Admin: o'quvchilar ro'yxati | frontend-react | ✅ | — | QA: PASS · 200 test |
| P25 | Individual profil sahifasi | frontend-react | ✅ | — | **Asosiy ekran** · axe toza |
| P26 | Diagramma widgetlari | frontend-react | ✅ | — | 7 widget · neytral palitra |
| P27 | Excel va PDF eksport | backend-dotnet | ✅ | — | Filtr `AdminStudentFilterBuilder` bilan ro'yxat bilan birlashtirildi · 500 qatorli oqim |
| P28 | AI sozlamalari UI | frontend-react | ✅ | — | DTO shakllari P18 bilan solishtirilishi kerak |
| P29 | Katalog va audit UI | frontend-react | ✅ | — | frontend 313 test |
| P30 | E2E testlar | qa-reviewer | ✅ | — | Playwright 18/18 · 390px+1440px · axe · 9 ta haqiqiy xato topdi |
| P31 | Xavfsizlik va mustahkamlash | qa-reviewer | ✅ | — | `ProblemDetails` yagona shakl · `Retry-After` · CSP jonli tekshirilgan · IDOR qo'riqchisi |
| P32 | Docker, CI/CD, yakuniy hujjat | backend-dotnet | ✅ | — | CI 5 job · backup/restore sinaldi · CD ochiq (remote yo'q) |
| P34 | Dastur modeli va biriktirish | backend-dotnet | ✅ | — | QA: FAIL→PASS · migratsiya backfill tuzatildi |
| P35 | Admin: savollar, dasturlar, biriktirish | frontend-react | ✅ | — | Dasturlar ishlaydi; katalog P37 ni kutadi |
| P36 | Ommaviy: dastur tanlash, so'rovnoma | frontend-react | ✅ | — | Bitta dasturda oqim o'zgarmadi (regressiya testi) |
| P37 | Katalog CRUD (backend) | backend-dotnet | ✅ | — | backend **752 test** · BR-8 uch darajada · talqin oraliqlari butun son qoidasi |
| P39 | Login sessiyasi barqarorligi | frontend-react | ✅ | — | **Egasi topgan bloklovchi** · vaqtinchalik xato sessiyani o'chirmaydi |
| P33 | Anketa konstruktori | frontend + backend | ✅ | — | Talqin oraliqlari muharriri · `issues[]` ro'yxati · `displayOrder` |
| P40 | AI UI ↔ API shartnomasi | frontend + backend | ✅ | — | `baseUrl` jimgina o'chishi tuzatildi · shablon hisobot belgisi |
| P41 | Havola sog'ligi va ogohlantirishlar | backend + frontend | ✅ | — | Dastursiz maktab paneldа belgilanadi · `409 NO_PROGRAM_AVAILABLE` |
| P42 | Excel import, shablon, anketa yaratish | backend + frontend | ✅ | — | Eksport↔import aylanma testi · zip bomba himoyasi |
| P43 | Savolma-savol javoblar va tahlili | backend + frontend | ✅ | — | Teskari tuzatilgan qiymat · ishonchlilik signallari |
| P44 | AI tahlil tugmasi, avtomatik o'chirildi | backend + frontend | ✅ | — | Egasining qarori · `Ai__AutoAnalyzeOnCompletion=false` |
| P38 | Katalog: tizim testini tahrirlash UI | frontend-react | ✅ | — | **Egasi so'radi** · ko'rish+tahrirlash, o'chirish yo'q |
| P45 | Ommaviy dizayn ko'chirish (16shaxsiyat statik sayt → frontend) | frontend-react | ✅ | — | Marketing qatlami yangi (`/`, `/metodika`, `/biz-haqimizda`, `/aloqa`) · dizayn tizimi FAQAT qo'shimcha (eski tokenlar tegilmagan) · E2E 22/22 · commit `15baf48` |
| P46 | 2FA QR kod bilan va tasdiqlashdan keyin yoqiladi | backend + frontend | ✅ | — | `AdminUser.EnableTotp` OLIB TASHLANDI (tasdiqlashsiz yoqish hisobni bloklab qo'ygan edi) · kutish holati 10 daq · zaxira kodlar `confirm` da · migratsiya `AddPendingTotpEnrollment` · commit `d4328e5` |
| P47 | 16 tip metodika sahifasida (ommaviy katalog endpointi) | backend + frontend | ✅ | — | Kontent YOZILMADI — `SeedData/type-catalog.json` dan · `GET /api/public/type-catalog`, kesh 1 soat · `/metodika/:kod` · 6a: guruhlarga bo'linmadi · commit `1204a36` |
| P48 | Ommaviy makon va Telegram foydalanuvchisi (domen) | backend-dotnet | ✅ | — | `SchoolId` nullable QILINMADI (42 fayl) — o'rniga `School.Kind` + bitta "Ommaviy makon" yozuvi · `public_users`, `public_refresh_tokens` · sessiya tokeni SHA-256 ga o'tdi (mavjud sessiyalar buzilmadi) · yosh 6–99 · 1095 test · commit `8b035b6` |
| P49 | Telegram kirish, kabinet va maktabsiz sessiya (API) | backend-dotnet | ✅ | — | `POST /api/auth/telegram` (HMAC-SHA256, doimiy vaqtda solishtirish, `auth_date` 24s) · `/api/me/*` · `StartSession` kengaytirilmadi, alohida command · ikki auditoriya qat'iy ajratilgan (test bilan qulflangan) · begona sessiya → `404` · 1179 test · commit `70b5d59` |
| P50 | Telegram kirish va kabinet UI | frontend-react | ✅ | — | `/kirish`, `/kabinet`, `/kabinet/test`, `/kabinet/natijalar/:id` · access token FAQAT xotirada (XSS) · Telegram obyekti qayta yig'ilmaydi (imzo) · maktab oqimi tegilmagan · 608 test · commit `c155c59` |
| P51 | Docker: seed `init` profiliga, obraz bir marta quriladi | backend-dotnet | ✅ | — | Egasining talabi · `up --build` 4 emas 2 obraz · `migrate` ATAYLAB avtomatik qoldi (API sxema eskirganini tekshirmaydi) · commit `b7d69c5` |
| P52 | Tarmoqlanuvchi so'rovnoma (bo'limlar, ko'rsatish sharti, matn/ko'p tanlovli savollar) | backend + frontend | ✅ | — | Egasining talabi · shartnoma `docs/18` · oltin fikstura `tests/fixtures/visibility-golden.json` **ikkala tomon o'qiydi** va 3 ta haqiqiy ajralishni ushladi · backend **1554 test**, frontend **920 test**, E2E 390px+1440px real Docker stekda PASS · namuna `docs/examples/sorovnoma-intellect.json` (5 bo'lim, 25 savol) import→nashr→ommaviy API testi bilan qulflangan · QA: PASS (1 bloklovchi + 2 muhim tuzatildi) |
| P52b | Namunaviy so'rovnomani SEED qilish (egasining talabi: "bir marta qo'shib qo'y") | backend-dotnet | ✅ | — | Fayl ko'chirildi: `docs/examples/sorovnoma-intellect.json` → `src/.../Infrastructure/Persistence/SeedData/surveys/intellect-survey.json` (yagona manba, eski nusxa o'chirildi) · `DbSeeder.SeedSurveysAsync` + `SeedDataLoader.ParseSurvey`/`ToDomainCustomDraftSurvey` qo'shildi · idempotentlik TIZIM METODIKALARIDAN FARQLI: `Code` bazada bo'lsa BUTUNLAY o'tkazib yuboriladi (superadmin tahriri himoyalangan) · natija har doim `Kind=Custom/IsSystem=false/ScoringMode=Survey/Status=Draft` (2-B bo'lim markaz nomlari haqiqiy emas — superadmin to'ldirib nashr qiladi, `docs/18` §7) · `AdminCatalogBranchingImportEndpointTests` HTTP-import zanjiridan seed-yo'liga moslashtirildi (A5 bilan takrorlanmaslik uchun) · yangi testlar: `SeedDataLoaderSurveyRealFixtureTests` (8), `DbSeederSurveyTests` (3, jumladan "qo'lda tahrirlangan anketa qayta seeddan keyin o'zgarmaydi") · backend **1566 test** (jumladan `Migrations.Tests` real Postgres/Docker ustida — `SeedIdempotencyTests` PASS), frontend **921 test** — barchasi yashil · `dotnet build` 0 xato/0 ogohlantirish |

---

## Qabul qilingan qarorlar (sessiyalar davomida)

| Sana | Qaror | Sabab | Qayerga yozildi |
|------|-------|-------|-----------------|
| 2026-08-31 | Brend "Shaxsiyat", domen `16shaxsiyat.uz`, API `api.16shaxsiyat.uz` | Domen olindi | `docs/01` 2a-bo'lim |
| 2026-08-31 | Superadmin uchun anketa konstruktori (`SUM` strategiyasi) qo'shildi | Superadmin o'z testini kirita olishi kerak | `docs/06` ADR-13..15 |
| 2026-08-31 | Lokal git repozitoriysi ochildi (`main` + `feat/*`) | PM.md 6-bo'limidagi git intizomi shuni talab qiladi | `docs/06` 8-bo'lim |
| 2026-08-31 | EF Core paketlari `9.x` da qulflandi (net10.0 da) | Npgsql'ning EF Core 10 provayderi hali yo'q | `docs/06` 8-bo'lim |
| 2026-08-31 | **Push qilinmaydi** — faqat lokal commit va branch | Loyiha egasining ko'rsatmasi; repo `AbduxalilVoxidjonov/shaxsiyat` tayyor, keyinroq push qilinadi | shu jurnal |
| 2026-09-08 | Ommaviy foydalanuvchilar ro'yxatida telefon raqami ko'rsatiladi (manba — anketa, `students.phone`) | Egasining talabi. Telegram Login Widget telefon BERMAYDI, shu sabab `public_users`ga ustun qo'shilmadi — yagona mavjud raqam anketadagi | `docs/07` §3.7 |
| 2026-09-08 | Akkauntni o'chirishda sabab so'raladi; o'chirilgan akkaunt admin ro'yxatidan YO'QOLMAYDI — "O'chirilgan" holatda sababi bilan turadi | Egasining talabi: "nega ketdi" ko'rinib tursin. Anonimlashtirish qoidasi saqlandi (Telegram ID/ism/username tozalanadi), faqat sabab va izoh qo'shildi | `docs/04` §2.11, `docs/05`, `docs/07` §3.7 va §5.5, `docs/08` |

---

## Insondan kutilayotgan javoblar

| № | Savol | Kerak bo'ladigan vazifa | Holat |
|---|-------|-------------------------|-------|
| 1 | O'quvchiga natija ko'rsatiladimi (qisqa versiya)? | P12, P21 | ⏳ |
| 2 | Maktab bilan rozilik matni (`consentText`) | P10, P20 | ⏳ |
| 3 | Qaysi sinflar qamraladi (hozir 5–11 mo'ljallangan)? | P05–P08 savol tili | ⏳ |
| 4 | Qayta topshirishga necha oydan keyin ruxsat (hozir 90 kun)? | P10 (BR-1) | ⏳ |
| 5 | Birinchi AI provider va byudjet | P17 | ⏳ |
| 6 | `.env` sirlari: DB paroli, `Jwt:Key`, `EncryptionKey` | P03 | ⏳ |

> PM: bu javoblarsiz ham ishni boshlash mumkin — hujjatlardagi default qiymatlar bilan ket,
> javob kelganda o'zgartir. Faqat 6-band (`P03` uchun lokal `.env`) haqiqatan to'sqinlik qiladi.

---

## Kundalik jurnal

### 2026-08-31
- Hujjatlar (`docs/00–15`) va promptlar (`prompts/00–33`) tayyorlandi.
- Brend va domen qat'iylashtirildi: **Shaxsiyat**, `16shaxsiyat.uz`.
- Anketa konstruktori qamrovga qo'shildi (P33).
- PM agent (`PM.md`), 5 mutaxassis agent (`.claude/agents/`) va shu jurnal yaratildi.
- **P01 bajarildi** — MVC shabloni `src/StudentRoadMap.Api/` ga aylantirildi; 4 `src/` + 3 `tests/`
  loyihasi, `Directory.Build.props` (`TreatWarningsAsErrors=true`), `.editorconfig`, `README.md`.
  `Program.cs`: Serilog, Swagger (faqat Dev), CORS `App:FrontendUrl` dan, `/health`, `/health/ready`,
  ProblemDetails. Razor/MVC qoldiqlari yo'q, Domain paketsiz, sirlar yo'q.
  Tekshiruv (PM o'zi): `dotnet build` 0 xato 0 ogohlantirish · `dotnet test` 4/4 yashil ·
  `/health` 200 `Healthy` · `/health/ready` 200 · `/swagger` 200. **QA verdikti: PASS.**
- **P02 boshlandi** (`feat/P02-domain-qatlami`).
- **Ish tartibi o'zgardi (egasining ko'rsatmasi: "tezroq agentlar asosida ishla"):**
  P05–P08 savol banklari (190 savol) endi P04 ni kutmasdan, P02/P03 bilan **parallel** yozilmoqda.
  Ular faqat `SeedData/test-definitions/*.json` kontentini yozadi — seeder integratsiyasi
  va "DB'da N qator" DoD bandi P04 da tekshiriladi. JSON sxemasi P04 promptidan olindi.
- **P05–P08 savol banklari tayyor:** MBTI16 60, BIG5 50, RIASEC 48, ACTIVITY 32 = **190 savol**.
  Ikkita sifat nuqsoni topilib qaytarildi: BIG5 da teskari savollar birinchi/ikkinchi yarimga
  bo'linib qolgan edi, ACTIVITY da har shkala bitta blokda turardi — ikkalasi ham javob to'plami
  effektiga olib keladi. Endi hamma bankda `direction` ketma-ketligi ≤ 2, `scale` ketma-ketligi = 1.
  BIG5 tartibi `random.seed(20260831)` bilan deterministik qayta hisoblangan.
- **Muhit muammosi:** bu mashinada Docker ishlamayapti, PostgreSQL yo'q. P03 migratsiyani
  yozadi, lekin `docker compose up -d db` / `dotnet ef database update` / `psql` tekshiruvlari
  bajarilmaydi. Egasiga aytildi; bandlar P04 da qaytib tekshiriladi.
- **P02 commit qilindi** (`84ebaed`): Domain 43 fayl + 193 test (PM o'zi ishga tushirdi, 193/193 yashil).
  Testlarni alohida agent yozdi — TOPILGAN XATO: yo'q.
- **P05–P08 commit qilindi** (`a2f7e6d`) va **QA verdikti: PASS** (4 bank ham).
  QA barcha 190 savolning yo'nalish mantiqini `docs/03` konvensiyasi bilan solishtirdi —
  scoring'ni buzadigan xato yo'q. Til, brend nomlari, taqiqlangan mavzular, gender stereotip,
  litsenziya riski — hammasi toza. 5 ta zaif savol texnik qarz sifatida yozildi.
- **P03 commit qilindi** (`9bb42d2`). QA: PASS. Uchta topilma tuzatildi:
  `question_order_json` nullable qilindi, `ix_students_last_at` ga `NULLS LAST` qo'shildi
  (xom SQL bilan — EF fluent API'da sozlanmaydi), `docker-compose.yml` dagi parol zaxira qiymati
  olib tashlandi. Oltita arxitektura qarori `docs/06` §8 ga yozildi.
- **⚠️ Ma'lumot yo'qolishi:** P03 agenti ish oxirida `.srm_bundle.json` (294 KB) faylini
  o'z-o'zidan o'chirib yubordi. Fayl `.gitignore` da edi → git'da yo'q → **tiklanmadi**.
  Repoda unga havola yo'q. Egasiga aytildi. Chora: barcha keyingi topshiriqlarda agentlarga
  fayl o'chirish va `rm` qat'iy taqiqlandi.
- **P04 ‖ P09 parallel boshlandi.** P09 ning birinchi agenti hech narsa yozmasdan qotib qoldi
  (ish daraxti toza qoldi), qayta ishga tushirildi.
- **P04 tayyor** (commit kutmoqda): `SeedDataLoader` (DB'siz, sinaladigan), `DbSeeder` (idempotent
  upsert, BR-8 himoyasi — `scale`/`direction` farq qilsa xato bilan to'xtaydi), `type-catalog.json`
  (16 tip), `career-map.json` (18 juftlik), PBKDF2 parol xeshi, `--seed` va `App:SeedOnStartup`.
  38 ta yangi test. Qamrovdan chiqish: Domain `Catalog/TestDefinition.cs` va `Question.cs` ga
  metod qo'shildi (`CreateSystemPublished`, `UpdateMetadata`, `UpdateOrder`) — seed uchun zarur edi,
  QA ko'rigida tekshiriladi.
- **Domain tozaligi testi ish berdi:** P09 agentining `ReliabilityCalculator.cs` fayli tizim
  soatidan foydalangani uchun `DomainSourceFiles_DoNotUseSystemClockDirectly` qizil bo'ldi.
  Agentga qaytarildi — scoring deterministik bo'lishi shart.
- **P04 va P09 tugadi. QA: FAIL → tuzatildi → PASS.** Bu ko'rik o'zini oqladi:
  - **Bloklovchi 1:** MBTI borderline bayrog'i `pct = 55` da qo'yilmasdi (IEEE-754: `55.00000000000001`).
    Tuzatishda **xuddi shu xato yana 4 joyda** topildi — BIG5 (20/40/60/80), ACTIVITY (30/50/70/85/31),
    CompositeScorer (35/55/75), SUM. Endi hamma chegara `ScorePercent.FromClamped` bilan
    yaxlitlangan qiymatdan hisoblanadi.
  - **Bloklovchi 2:** `ReliabilityCalculator` javoblarni faqat `DisplayOrder` bo'yicha tartiblardi,
    4 test esa `1..N` dan boshlanadi → bloklar aralashib, bitta testni to'liq bir xil javob bilan
    to'ldirgan o'quvchi `Reliable` chiqardi. Endi kalkulyator tartibni buzmaydi;
    javobgarlik chaqiruvchida va `prompts/12` ga **P12-R1/P12-R2** majburiy talab qilib yozildi.
  - M1–M13: javob diapazoni validatsiyasi, `Weight != 1.0` xatosi, RIASEC `direction` tekshiruvi,
    `SUM` min 4 savol, tez javob maxraji, `ScoringVersion` — hammasi tuzatildi.
  - P04: `DbSeeder` uchun SQLite in-memory testlari (idempotentlik, BR-8, tranzaksiya rollback),
    yangi Domain metodlariga testlar, `Pbkdf2PasswordHasher` mustahkamlandi (iteratsiya chegarasi).
- **Hujjatlar yangilandi:** `docs/03` §7.1 (ishonchlilik qoidalarining 5 ta aniqlashtirilishi),
  `docs/08` (PBKDF2-HMACSHA256 210k), `docs/06` §8 (3 yangi qaror).
- **P10 va P19 tugadi, ikkalasi ham QA: PASS.** Jami **419 test yashil**.
  - P10 QA topilmasi (jiddiy): `UseForwardedHeaders()` yo'q edi. Production'da API Caddy ortida
    turadi, ya'ni `RemoteIpAddress` har doim proksining manzili — **rate limiting ham, IP audit
    ham jimgina ishlamay qolardi**. Tuzatildi.
  - Tuzatish jarayonida ASP.NET Core'ning tuzog'i aniqlandi: `KnownProxies` ni `Clear()` qilib
    bo'sh qoldirish cheklov qo'ymaydi, **aksincha har qanday manbadan `X-Forwarded-For` ga
    ishonadi**. Shuning uchun ro'yxat bo'sh bo'lganda `UseForwardedHeaders` **umuman
    chaqirilmaydi**. Agent buni amaliy probe bilan tasdiqladi va regressiya testi yozdi
    (middleware'ni o'chirib ko'rib, test qizarishini tekshirgan).
  - P19 QA: `adminClient` refresh mutex testi haqiqiy, `shared/` `features/` ga bog'lanmaydi,
    `dangerouslySetInnerHTML` ESLint qoidasi chindan `error` (QA qasddan buzib sinadi).

### 2026-09-01
- **Loyiha egasi `docs/17-16personalities-tahlili.md` va `CLAUDE.md` 6a-qoidasini qo'shdi:**
  raqobatchi kontenti (16Personalities/NERIS, MBTI, Keirsey) ishlatilmaydi.
- **Natijada 16 tip nomi to'liq qayta yozildi.** Eski nomlarning hammasi 16Personalities
  nomlarining o'zbekcha tarjimasi ekan (Strateg=Architect, Vositachi=Mediator, Konsul=Consul,
  Qo'mondon=Commander va h.k.). Yangi nomlar: Tayanch, G'amxo'r, Teran, Loyihachi, Chevar,
  Sezgir, Orzumand, Bilimdon, Sinovchi, Quvnoq, Otashqalb, Yangilikchi, Tuzuvchi, Jonkuyar,
  Murabbiy, Bunyodkor. Tavsiflardagi eski nom izlari ham tozalandi;
  `docs/03`, `docs/04`, `docs/07`, `docs/09`, `docs/10`, `docs/11`, `docs/17` va
  `TypeCatalogEntry.cs` izohi yangi nomlarga moslandi.
- **P11 va P20 tugadi, ikkalasi ham QA: PASS.** Backend **445 test**, frontend **63 test**.
  Bu bosqichda topilgan uchta backend kontrakt nuqsoni — uchalasi ham frontend ishi paytida
  aniqlandi (kontraktni haqiqatan iste'mol qilmaguncha ko'rinmaydi):
  1. Birorta endpoint javob sxemasini generatsiya qilmasdi → `generate:api` bo'sh tip berardi;
  2. `errors` kalitlari PascalCase edi → frontend qo'lda xarita bilan aylanib o'tayotgandi;
  3. Barcha maydonlar ixtiyoriy chiqardi → frontend hamma joyda `?? ''` yozgandi.
  Hammasi tuzatildi; frontend vaqtinchalik yechimlari olib tashlandi.
- **QA topgan bajarilmagan talab:** `Assessment.LanguageCode` butunlay e'tiborsiz qolgan edi
  (`prompts/11`: "`scaleLabels` tildan olinadi"). Endi matn va yorliqlar til bo'yicha tanlanadi,
  `uz` ga fallback qiladi va **kesh kaliti tilni o'z ichiga oladi** — busiz `ru` matn qo'shilgan
  kuni kesh birinchi so'ragan tilni hamma uchun qaytarardi.
- **P12 va P21 tugadi, ikkalasi ham QA: PASS.** Backend **457 test**, frontend **127 test**.
  - P12-R1/R2 o'zini oqladi: QA `ReliabilityInputBuilder` ni buzib ikkala testni qizartirdi,
    keyin qaytardi — regressiya himoyasi ishlashi isbotlandi. `65.19` ishonchlilik balli
    QA tomonidan qo'lda mustaqil qayta hisoblandi.
  - QA topgan spetsifikatsiya buzilishi: `IsRequired = false` savollar majburiy deb
    hisoblanardi — tuzatildi.
  - P21 da `sendBeacon` → `fetch(keepalive: true)` ga almashtirildi: `sendBeacon` maxsus
    header qo'ya olmaydi, ya'ni `beforeunload` yo'li ishlayotgandek ko'rinib 401 qaytarardi.
    `visibilitychange` ham qo'shildi (mobilda `beforeunload` ishonchsiz).
  - `durationMs` ga 10 daqiqalik chegara qo'yildi — o'quvchi tanaffus qilsa qiymat
    cheksiz o'sib ketardi.
- **Agentlar sessiya limitiga urildi** (03:50 da tiklanadi). Qolgan uchta band PM tomonidan
  yakunlandi: `PublicTestSummaryDto` ga `name`/`estimatedMinutes`, `/start` idempotentligi
  va sessiya holati shartnomasi uchun regressiya testlari.
- **P13 va P22 tugadi, ikkalasi ham QA: PASS.** Backend **511 test**, frontend **171 test**.
  - P13 QA alohida puxta bo'ldi: timing farqi **o'lchandi** (mavjud bo'lmagan foydalanuvchi
    21.91ms ↔ noto'g'ri parol 21.99ms), `[JsonIgnore]` atributga ishonmasdan **haqiqiy
    serializatsiya** bilan tekshirildi, TOTP ni sinash uchun RFC 6238 **mustaqil qayta yozildi**
    (aylanma tekshiruv bo'lmasligi uchun).
  - QA topgan MAJOR: TOTP va zaxira kod qayta ishlatishga qarshi himoyaning **o'zi poyga
    holatiga chidamsiz** edi — "o'qi, keyin yoz" naqshi. Tuzatildi: `AdminUser` ga optimistik
    konkurentlik tokeni (`ConcurrencyStamp`, `xmin` emas — SQLite sinov muhiti uchun portativ),
    zaxira kod uchun atomik `UPDATE ... WHERE used_at IS NULL` (P10 dagi `ON CONFLICT` namunasi).
  - P22 da ishlatilmaydigan `@tanstack/react-table` bog'liqligi olib tashlandi — `DataTable`
    qo'lda yozilgan va QA buni to'g'ri deb topdi (bizga faqat server-tomon sahifa/saralash kerak).
    `docs/10`, `prompts/19`, `docs/06` §8 sababi bilan yangilandi.
  - Saralanadigan ustunlarga `aria-sort` qo'shildi.
- **P14 va P23 tugadi, ikkalasi ham QA: PASS.** Backend **540 test** (537 yashil, 3 Skip),
  frontend **183 test**.
  - **PM topgan bloklovchi:** `ListStudentsQueryHandler` filtrga mos **barcha** o'quvchini
    xotiraga yuklab, keyin saralab, keyin sahifalar edi — va `lastAssessmentAt` standart
    saralash, ya'ni bu default yo'l edi. 10 000 o'quvchida ro'yxatni ochish 10 000 qatorni
    API xotirasiga yuklardi va P03 da maxsus xom SQL bilan to'g'ri qilingan `ix_students_last_at`
    indeksini foydasiz qilardi. Sabab: SQLite sinov muhiti `DateTimeOffset` bo'yicha
    `ORDER BY` ni tarjima qila olmaydi. **Qaror: sinov muhiti qulayligi uchun ishlab
    chiqarish kodi pasaytirilmaydi** — saralash DB'da qoldi, 3 ta test `Skip` bilan belgilandi.
  - P23 agenti frontend'dagi `PagedResult<T>` tipi `docs/07` §4 shaklidan farq qilishini
    topdi (u hech qayerda ishlatilmagani uchun sezilmagan edi) va tuzatdi.
  - `GET /schools/{id}` javobiga `qrCodeBase64` qo'shildi — aks holda admin QR ko'rish uchun
    havolani yangilashga majbur bo'lardi va maktab o'quvchilarini yarim yo'lda qoldirardi.
  - Admin API rate limiti (300/daq) qo'shildi — `docs/07` §4 da talab qilingan, lekin
    P13 dan beri hech bir admin endpointda yo'q edi.
  - QA topgan ikki test bo'shlig'i yopildi: hard delete allaqachon soft-o'chirilgan sessiyani
    ham tozalashi, va IDOR regressiyasi (o'quvchi tokeni admin endpointida ishlamasligi).
- **P15 va P24 tugadi.** Backend **575 test, 0 Skip**, frontend **200 test**.
  - **QA FAIL bergan bloklovchi:** dashboard keshi parametrsiz chaqiruvda (ya'ni odatiy
    yuklanishda) **hech qachon ishlamasdi** — `to = UtcNow` tik aniqligida kalitga kirardi.
    Mavjud kesh testi buni sezmasdi, chunki ikkala chaqiruvda `from`/`to` ni qotirib berardi.
    QA buni empirik isbotladi. Tuzatildi; yangi test eski kodda **qizarishi** tekshirilgan.
  - **Sinov muhiti cheklovi butunlay yopildi:** SQLite `DateTimeOffset` bo'yicha na `ORDER BY`,
    na `WHERE` ni tarjima qila olmasligi aniqlandi — ya'ni P14 dagi sana filtrlari hech qachon
    sinalmagan ekan. Value converter qo'shildi (faqat SQLite'da), **`Skip` soni 8 dan 0 ga tushdi**.
    Yo'l-yo'lakay haqiqiy xato topildi: xom SQL konverterni chetlab o'tadi
    (`TryMarkTotpBackupCodeUsedAsync` — P13 dagi TOTP himoyasi).
  - `Changed` bayrog'i endi to'liq payload bo'yicha solishtiriladi; yangi test eski kodda
    qizarishi tasdiqlangan.
- **P34 (dastur modeli) va dashboard voronkasi tugadi.** Backend **643 test**, frontend **263 test**.
  - **QA FAIL bergan bloklovchi:** ikki bosqichli migratsiya faqat qog'ozda ikki bosqichli edi.
    `--migrate` barcha migratsiyalarni bitta chaqiruvda bajaradi, seeder esa faqat undan keyin —
    ya'ni mavjud sessiyalar nol-GUID ga bog'lanib FK cheklovini buzardi va deploy to'xtardi.
    Bizning bazamiz **tasodifan** omon qolgan (migratsiyalar `assessments` bo'sh paytda qo'llangan).
    Testlar ushlay olmasdi — ular `EnsureCreated()` ishlatadi. Tuzatildi: tizim dasturi
    deterministik ID bilan **migratsiya ichida** yaratiladi va backfill `SET NOT NULL` dan
    oldin bajariladi. Agent buni haqiqiy Postgres'da `program_id IS NULL` qatorlar bilan sinadi.
  - **PM topgan xato:** `completionRate` ulush (0..1) qaytaradi, frontend esa uni to'g'ridan-to'g'ri
    foiz deb ko'rsatardi — 50% yakunlagan maktab jadvalda **`1%`** bo'lib ko'rinardi.
    Tuzatildi, regressiya testi qo'shildi, birlik `docs/07` da aniq yozildi.
  - Havola ochilishi hisoblagichi qo'shildi (voronkaning yuqori bo'g'ini ilgari umuman
    kuzatilmasdi) va u **fail-open**: telemetriya xatosi landing sahifasini buza olmaydi.
- **Keyingi:** P35 (savollar bo'limi, dasturlar, biriktirish UI) ‖ P36 (o'quvchi dastur tanlovi).

---

### 2026-09-02

- **Ishga tushirish va deploy.** Butun stek `docker compose up -d --build` bilan ko'tariladi
  (`db → migrate → seed → api → app → tunnel`), host portlari ochiq emas — kirish faqat
  Cloudflare tunnel orqali `16shaxsiyat.uz` da. Docker loyihasi nomi `16shaxsiyat`.
  - **Egasi ko'rgan "Kirishda xatolik" ning sababi topildi:** `env.ts` bo'sh
    `VITE_API_BASE_URL` ni `http://localhost:5000` ga qaytarardi, HTTPS sahifadan bu
    *mixed content* sifatida bloklanardi. Standart qiymat same-origin qilindi.
- **P35/P36 (dastur modeli UI) tugadi**, lekin P35 katalog bo'limi ulanadigan backend
  yo'qligini aniqladi → **P37** yozildi va bajarildi.
- **P37 (katalog CRUD backend) tugadi** — backend **752 test**.
  - BR-8 uch darajada qulflangan (domen → handler → integratsiya testi).
  - Kesh invalidatsiyasi taxmin emas, test bilan isbotlangan: kesh to'ldiriladi, `PUT`
    dan keyin kalit yo'qligi tekshiriladi.
  - **Uchinchi marta chiqqan EF Core tuzog'i:** allaqachon kuzatilayotgan ota-obyektning
    kolleksiyasiga ID'si oldindan belgilangan yangi bola qo'shilsa, EF uni `Added` emas
    `Modified` deb biladi va har savol/shkala yaratishda `409 CONCURRENCY_CONFLICT`
    beradi. Yechim — aniq `_context.Add(...)`.
  - **Talqin oraliqlari qoidasi qat'iylashtirildi** (PM qarori): chegaralar butun son,
    `to` inklyuziv, `next.from == prev.to + 1`, 0 dan 100 gacha. Tolerantlik heuristikasi
    olib tashlandi — `docs/03` §6.3 ga yozildi. Bu `SUM_INTERPRETATION_BAND_NOT_FOUND`
    ni ishlash vaqtidan nashr vaqtiga ko'chiradi.
- **P27 (eksport) tugadi.** Filtr mantiqi `AdminStudentFilterBuilder` ga ajratildi — ro'yxat
  va eksport endi bir xil kodni ishlatadi va ajralib keta olmaydi. Audit'ga qidiruv **matni**
  emas, `hasSearch` bayrog'i yoziladi (qidiruv so'zi ism bo'lishi mumkin — aks holda audit
  jurnali o'zi shaxsiy ma'lumot omboriga aylanardi).
- **P28/P29 (AI sozlamalari, katalog va audit UI) tugadi** — frontend **313 test**.
- **Egasi so'ragan UI ishlari:** navigatsiya `sticky` qilindi (sahifa bilan siljimaydi),
  "Tez orada" yorliqlari olib tashlandi, `Drawer` o'chirilib hamma oyna markazda ochiladigan
  `Dialog` ga o'tkazildi (`dialog:modal { margin: auto }`), brend hamma joyda
  **Shaxsiyat** ga, sarlavha va boshqaruv panelidagi chaqmoq belgisi olib tashlandi.
- **Egasining yangi talabi (P38):** tizim metodikalari (shu jumladan shaxsiyat testi)
  panelda **ko'rilishi va tahrirlanishi** kerak, faqat o'chirilmasin. Backend buni
  allaqachon qo'llab-quvvatlaydi (`Question.UpdateText`/`UpdateOrder`/`UpdateRequired` va
  `TestDefinition.UpdateMetadata` da `IsSystem` tekshiruvi yo'q); yetishmayotgani —
  frontend. `CatalogTestDetailPage` tahrirlanadigan qilinmoqda.
  - **Diqqat:** `UpdateTestQuestionCommandHandler` `scale`/`direction`/`weight` maydonlari
    **berilgan bo'lsa** `SYSTEM_TEST_LOCKED` otadi. Demak tizim savolini saqlaganda ular
    umuman yuborilmasligi kerak — aks holda tahrirlash 409 bilan ishlamaydi.
- **Egasi topgan bloklovchi: "login qilsam yana login sahifasiga otib qolyapti".**
  Sabab `ProtectedRoute` da edi — u `GET /api/auth/me` ning **har qanday** xatosida
  `clear()` chaqirardi. API bir soniya javob bermasa (konteyner qayta ishga tushishi,
  tunnel uzilishi, `502`) haqiqiy sessiyasi bor superadmin login sahifasiga uloqtirilardi;
  sahifani yangilagach esa refresh cookie orqali yana kirardi — egasi tasvirlagan
  "urlda login o'chirsam admin panelga o'tib ketyapti" aynan shu.
  Endi sessiya **faqat serverning aniq `401`ida** tugatiladi; boshqa xatoda qayta urinish
  ekrani ko'rsatiladi va sessiya saqlanadi. Ikkita regressiya testi eski kodda qizarishi
  tasdiqlangan (`isSessionRejected` ni `true` ga qaytarib sinaldi).
  - Yo'l-yo'lakay: `Button` da `disabled={disabled ?? isLoading}` → `||`. Audit jurnalida
    176 ms farq bilan ikkita `Auth.LoginSucceeded` bor edi — yuklanayotgan tugma ochiq
    qolgani uchun ikki marta bosish ikkita login yuborardi.
  - Yo'l-yo'lakay: fon refresh'i endi `authStore` ni ham yangilaydi (ilgari faqat
    `adminClient` ichidagi modul o'zgaruvchisi yangilanardi — ikki manba zid bo'lish xavfi).
  - Operatsion: mashina uyquga ketganda Docker hamma konteynerni to'xtatadi va
    `restart: unless-stopped` ularni qaytarmaydi (ular "stopped" deb belgilanadi) —
    `docker compose up -d` qo'lda kerak.
- **P18 (AI navbat, fallback zanjiri, AI config API) tugadi** — backend **816 test**, Skip 0.
- **P38 (tizim testini tahrirlash UI) tugadi** — frontend **327 test**.
- **Keyingi:** P18 (AI navbati) tugashini kutish → P33 (anketa konstruktori) →
  P30 (E2E) → P31 (xavfsizlik). P30/P31 oxirida.

---

### 2026-09-05

- **P45 (ommaviy dizayn ko'chirish) tugadi** — frontend **527 test**, `typecheck`/`lint`/
  `build` yashil. `16shaxsiyat.uz` (Next.js 15 + Tailwind 3 statik sayt) loyihasining vizual
  dizayni shu frontendga (React 19 + Vite + Tailwind 4) ko'chirildi:
  - **Dizayn tizimi** `frontend/src/index.css`ga FAQAT QO'SHIMCHA tarzda kiritildi — eski
    `primary/success/warning/danger/neutral` tokenlariga TEGILMADI (admin panel va
    `shared/ui` hamon ularga tayanadi). Yangi: ranglar (`paper`/`ink`/`line` oilalari,
    `firuza` brend aksenti, `binafsha`/`zumrad`/`lojuvard`/`zarhal`/`terakota` dekorativ
    palitralar), shriftlar (`font-sans` Inter Variable, `font-display` Plus Jakarta Sans
    Variable — `@fontsource-variable/*`), `rounded-4xl/5xl`, `shadow-soft/lift/glow`,
    `max-w-content/prose`, animatsiyalar (`animate-fade-up/fade-in/float/spin-slow/grow`),
    girih naqsh fonlari (`bg-girih`/`bg-girih-light`) va komponent klasslari
    (`.wrap`/`.card`/`.btn`/`.chip`/`.eyebrow`/`.lead`/`.prose-uz`). Ikkita ataylab kontrast
    og'ishi WCAG AA sababli: `.btn-primary` `firuza-600` (`firuza-500`+oq = 3.28:1, AA dan
    past), `.eyebrow` `ink-soft` (`ink-muted` = 4.49:1, chegaradan past).
  - **Yangi brend komponentlari** — `shared/ui/brand/`: `GirihStar`, `Logo`, `Divider`/
    `Blob`/`ArchTop` (`Ornament.tsx`), `PatternBackdrop`.
  - **Yangi marketing qatlami** — ilgari `/` `404` qaytarardi. `MarketingLayout.tsx` (sticky
    shaffof header + girih naqshli katta footer) + `features/marketing/` (`HomePage`/
    `MethodologyPage`/`AboutPage`/`ContactPage` + umumiy `sections/`), route'lar `/`,
    `/metodika`, `/biz-haqimizda`, `/aloqa`. Kontent 100% yangi yozilgan — CLAUDE.md
    6a-qoidasi bo'yicha 16shaxsiyat'ning tur kodlari, guruh nomlari, "MBTI" so'zi
    ko'chirilmagan; `HomePage.test.tsx`da buni qo'riqlaydigan regressiya testi bor. Hech
    qayerda "Testni boshlash" CTA yo'q — test faqat maktab havolasi orqali ochiladi;
    `/aloqa`da forma YO'Q (backend endpointi mavjud emas, ishlamaydigan forma aldardi).
  - **Ommaviy TEST oqimi qayta ko'rindi** (`PublicLayout.tsx`, `public-assessment/**`) —
    MANTIQ o'zgarmadi, faqat JSX/`className`. `LikertQuestion` moslashuvchan bo'ldi:
    `Likert7`/`Likert5` (doiralar qatori, chetdan markazga kichrayadi), `Binary` (yirik
    markazlashgan doiralar), `SingleChoice`/`ForcedChoice` (variant kartalari); klaviatura
    endi `1..9` (avval `1..5`).
  - **Natija sahifasida** (`StudentResultPage`, E-6) diagramma/foiz ATAYLAB qo'shilmadi —
    `GET /sessions/result` ball yoki o'lcham qaytarmaydi. Uch holat: tayyor / tayyorlanmoqda
    (`202`) / ko'rsatilmaydi (`403`, `App:ShowResultToStudent=false` — standart sozlama,
    xato EMAS).
  - `shared/ui`ning 19 komponenti + `AdminLayout` yangi palitraga o'tkazildi (`primary-*`→
    `firuza-*`, `neutral-*`→`paper`/`ink`/`line`, `danger`→`terakota`, `success`→`zumrad`,
    `warning`→`zarhal`). **Props API o'zgarmadi.**
  - **Ochiq qolgan:** `ru` lokali bo'sh (til almashtirgich yo'q, `fallbackLng: 'uz'`); eski
    semantik tokenlar `index.css`da hali turibdi va `features/**`/`widgets/**`da **69 dan
    ortiq faylda** to'g'ridan-to'g'ri ishlatiladi — demak hozircha IKKITA rang tizimi
    yonma-yon (birlashtirish keyingi bosqich); E2E (Playwright) bu o'zgarishlar bilan hali
    ishga tushirilmagan (boshqa agent parallel 2FA/QR to'lqinini tekshirmoqda, shu bilan
    birga bu branch ham qayta sinaladi).
  - **Branch:** `feat/P45-ommaviy-dizayn`, hali commit qilinmagan (loyiha egasining "push
    qilinmaydi" siyosati — 2026-08-31 qarori — davom etmoqda; commit navbatda).
- **Hujjatlar P45ga moslashtirildi** (shu vazifa, faqat hujjat fayllari):
  - `docs/10-frontend-arxitektura.md`: §1 stek jadvalidagi noto'g'ri `shadcn/ui`(Radix)
    yozuvi tuzatildi (`package.json`da bunday paket hech qachon bo'lmagan) va `ru` "tayyor"
    degan noto'g'ri yozuv ham; §2 papka tuzilmasiga `marketing/`, `MarketingLayout.tsx`,
    `shared/ui/brand/` qo'shildi; §3 route jadvaliga marketing qatorlari qo'shildi va
    P45'dan oldin ham eskirgan bir necha admin sub-route (`schools/:id`,
    `catalog/tests/:id`, `programs(/:id)`) tiklandi; §4.3 Savol UI moslashuvchan
    komponentga mos yangilandi; yangi **§9** qo'shildi (dizayn tizimi, brend komponentlari,
    marketing qatlami, ikki qatlam-komponent farqi, ochiq savollar).
  - `docs/11-ux-va-ekranlar.md`: §1 rang jadvaliga ikki tizim tushuntirilishi qo'shildi
    (admin — eski tokenlar, ommaviy — yangi); E-3/E-6 tavsiflari yangi ko'rinishga
    moslashtirildi; yangi **§2a** qo'shildi (M-1…M-4 marketing ekranlari); §0ga til holati
    (faqat `uz`) qo'shildi.
  - `frontend/README.md`: papka tuzilmasi, route xaritasi va dizayn tizimi bo'limlari
    qo'shildi (ilgari umuman yo'q edi — faqat run/lint/generate:api ko'rsatmalari bor edi).
  - `CLAUDE.md`: "Joriy holat" ro'yxatiga P45 belgilandi.
  - **Tegilmagan (topshiriq bo'yicha qat'iy taqiqlangan):** `docs/04–08` va barcha kod
    fayllari — ular ustida boshqa agent (2FA/QR) parallel ishlamoqda.

---

## Ma'lum risklar va texnik qarzlar

| Risk / qarz | Ta'sir | Reja |
|-------------|--------|------|
| P52: Excel eksport/import bo'lim va `visibility` maydonlarini bilmaydi — tarmoqlanuvchi anketani Excel'ga chiqarsa bu ma'lumot JIMGINA yo'qoladi | Superadmin Excel aylanmasidan keyin tarmoqlanishni yo'qotib qo'yishi mumkin | `docs/07` §3.4 da ogohlantirish yozilgan (rasmiy yo'l — JSON). Kerak bo'lsa eksportga bloklovchi/ogohlantiruvchi javob qo'shiladi |
| P52: E2E `409 UNIQUE_CONSTRAINT_CONFLICT` — admin katalog `questions/import` da. **Aniqlashtirildi (PM, 2026-09-11):** parallel workerlar sababi EMAS — `--workers=1` bilan ham, IKKI proyekt (`desktop-1440` + `mobile-390`) BITTA chaqiriqda ketma-ket yurganda chiqadi; har bir proyekt ALOHIDA yurganda 100% barqaror | CI'da tasodifiy qizil; import yo'lida haqiqiy nuqson bo'lishi mumkin (test izolyatsiyasi emas) | Backend agenti tekshirsin: `ImportTestQuestionsCommandHandler` da qaysi unikal cheklov buzilyapti va nega ikkinchi seansda. Repro: `npx playwright test <fayl> --project=desktop-1440 --project=mobile-390 --workers=1` |
| P52: 390px/1440px piksel darajasida KO'Z bilan ko'rilmagan — faqat `axe` va gorizontal-skroll avtomatik tekshirildi | Vizual nomuvofiqlik sezilmay qolishi mumkin | E2E aynan shu yo'l bilan bitta BLOKLOVCHI ni ushladi (footer "Keyingi" ni to'sardi), ya'ni qamrov yomon emas. Egasi ko'rsa yaxshi |
| P50 dan beri `PUBLIC_SPACE_SLUG = 'ommaviy'` frontendda backend `DbSeeder.PublicSpaceSlug` konstantasining NUSXASI — backend uni javobda qaytarmaydi | Backend slug'ni o'zgartirsa frontend jimgina buziladi (test ushlamaydi, chunki ikkalasi ham qattiq yozilgan) | Backend `GET /api/public/...` javobida ommaviy makon slug'ini qaytarsin yoki shartnoma testi qo'shilsin |
| P49/P50: ommaviy oqim uchun E2E (Playwright) hali yozilmagan — Telegram kirishini e2e'da taqlid qilish kerak | Kabinet va maktabsiz test oqimida regressiya vitest bilan ushlanmasligi mumkin (brauzer xatti-harakati, cookie, refresh) | Telegram javobini soxtalashtiruvchi test yordamchisi (bot tokeni test muhitida ma'lum) + `screens.e2e.ts` ga `/kirish`, `/kabinet` qo'shish |
| P49: `DELETE /api/me` `public_users` ni anonimlashtiradi, lekin ommaviy makondagi `Student` yozuvidagi F.I.Sh./telefon qoladi (BR-1 takrorlanish mantig'iga kiradi) | "O'chirish huquqi" to'liq bajarilmagan — shaxsiy ma'lumot bazada qolaveradi | Egasining qarori kerak: `Student` PII ham anonimlashtirilsinmi va takrorlanishni aniqlash nima bilan almashtirilsin |
| P49: ommaviy makonda 90 kunlik `DUPLICATE_ASSESSMENT` cheklovi maktabdagidek qoldirildi | B2C mahsulot uchun 90 kun uzun bo'lishi mumkin — foydalanuvchi qayta test topshira olmaydi | Egasining qarori: ommaviy makon uchun alohida (qisqaroq) oyna |
| P45 (2026-09-05) dan beri IKKITA rang tizimi yonma-yon: eski `primary/success/warning/danger/neutral` (admin, `shared/ui`) va yangi `paper/ink/line/firuza/...` (ommaviy sahifalar) | Ikki manba — kelajakda admin va ommaviy ekranlar orasida vizual nomuvofiqlik yoki noto'g'ri tokenni ko'chirib qo'yish xavfi. `features/**`/`widgets/**`da eski tokenning 69+ ishlatilishi bor | Egasi so'raganda admin panelni ham yangi palitraga to'liq o'tkazish va eski tokenlarni `index.css`dan olib tashlash (`docs/10` §9.9) |
| E2E (Playwright) P45 dizayn o'zgarishlari bilan hali ishga tushirilmagan | Yangi marketing ekranlari va o'zgargan `LikertQuestion` uchun regressiya qamrovi yo'q | Boshqa agentning parallel (2FA/QR) to'lqini tugagach, `feat/P45-ommaviy-dizayn` branch'i uchun alohida E2E/vizual tekshiruv |
| Savol banklari (190 savol) sifati — real o'quvchida sinalmagan | Natija ishonchliligi | Pilotdan keyin matnlarni tuzatish (`docs/14` 5-bo'lim) |
| AI prompt sifati faqat mock bilan sinaladi | Hisobot sifati | P17 dan keyin 10 ta oltin namuna bilan qo'lda baholash |
| `answers` jadvali tez o'sadi (190 qator/sessiya) | Ishlash | 100k sessiyadan keyin partitsiya (v2) |
| `type-catalog.json` dagi 16 tip nomi (Strateg, Mantiqchi, Munozarachi, Konsul, Qo'mondon…) 16Personalities (NERIS) rol nomlarining o'zbekcha muqobili | `docs/03` §0 aynan litsenziyalangan materialdan qochishni talab qiladi — nomlar to'plami himoyalangan bo'lishi mumkin | `docs/03` ning o'zi 2 ta misolni (Strateg, Ilhomlantiruvchi) shu uslubda bergan, shuning uchun bloklanmadi. **Egasidan huquqiy tasdiq kerak** yoki nomlar mustaqil qayta o'ylanadi (P25/P29 kontent ko'rigida) |
| `type-catalog.json` (16 tip tavsifi) va `career-map.json` (18 Holland juftligi, kasb nomlari) agent tavsiyasi | O'quvchi ko'radigan asosiy kontent — sifati tekshirilmagan | Kasb yo'riqchisi/egasi ko'rib chiqishi kerak (P25 individual profil sahifasidan oldin) |
| `ReliabilityInput.Questions` tartibi shartnoma bilan himoyalangan, kod bilan emas | Noto'g'ri tartibda berilsa straight-lining signali **jimgina o'chadi**, hech narsa ushlamaydi | `prompts/12` ga **P12-R1** (aniq LINQ) va **P12-R2** (majburiy regressiya testi) yozildi. Yagona himoya — o'sha test |
| `ReliabilityCalculator` da dublikat `QuestionId` tekshirilmaydi | Soxta straight-lining hosil qilish mumkin | Kichik; P10–P12 da kirish validatsiyasi bilan birga yopiladi |
| ~~`SUM` talqin oraliqlari 3+ kasrli belgilansa `pct` bo'shliqqa tushishi mumkin~~ — **yopildi (P37, 2026-09-02)**: chegaralar butun son bo'lishi shart, nashr validatsiyasi `SCALE_BAND_NOT_INTEGER` bilan rad etadi (`docs/03` §6.3) | — | — |
| `type-catalog` va `career-map` admin CRUD endpointlari (`docs/07` §3.4) yo'q | 16 tip tavsifi va kasb xaritasi faqat seed orqali o'zgaradi — egasi matnni panelda tahrirlay olmaydi | `TypeCatalogEntry` — o'zgarmas `ValueObject`, alohida domen ishi kerak. Alohida promptga qoldirildi; hozir hech kim so'ramagan |
| Noto'g'ri (malformed) JSON so'rov tanasi ASP.NET Core'ning standart `ValidationProblemDetails` iga tushadi — `code` maydonisiz | `CLAUDE.md` 11-qoidasi buziladi; klient xatoni kod bo'yicha ajrata olmaydi | Butun kodbaza bo'ylab mavjud bo'shliq (`InvalidModelStateResponseFactory` hech qayerda sozlanmagan). **P31 qamrovida** |
| `docs/17` va `CLAUDE.md` 6a — raqobatchi kontenti taqiqi kech kiritildi | `type-catalog.json` dagi 16 tip nomi allaqachon 16Personalities tarjimasi bo'lib yozilgan edi | **Bajarildi (2026-09-01):** hamma nom qayta yozildi. Kelgusida yangi kontent yozilganda 6a-qoida oldindan tekshiriladi |
| `lastAssessmentStatus`/`reliabilityFlag` snapshot ustun emas — sahifa uchun alohida batch so'rov | Bitta qo'shimcha so'rov (N+1 emas), lekin ADR-11 ruhiga to'liq mos emas | P15 (dashboard) da o'lchov asosida snapshot ustunga ko'chirish qaroriga qaytamiz |
| ~~3 ta test SQLite `DateTimeOffset` cheklovi sababli `Skip`~~ — **yopildi (P15)**: sinov muhitiga value converter qo'shildi, `Skip` soni **0** | Saralash faqat Postgres'da sinaladi | P30 (E2E) da Testcontainers/Postgres bilan yopiladi |
| `frontend` da `sessionStore.testCatalog` hamon bor (backend endi `name`/`estimatedMinutes` beradi) | Ikki manba — kelajakda nomuvofiqlik | Kichik tozalash: `testCatalog` olib tashlanib, nom `GET /sessions/me` dan olinsin. P22 bilan birga |
| 390px/1440px real brauzer vizual tekshiruvi hech bir ekranda qilinmagan | Gorizontal scroll yoki konsol xatosi sezilmay qolishi mumkin | Chrome MCP kengaytmasi ulanmagan; subagent uni ocholmaydi. Egasi yoqsa P21 dan boshlab tekshiriladi, aks holda P30 (E2E) da Playwright bilan |
| ~~"Dasturda shaxsiyat batareyasi bormi" frontend'da `"MBTI16"` kodini qidirish bilan aniqlanadi~~ — **backend qismi yopildi (P52, 2026-09-11)**: `GetSessionStateResult`/`PublicProgramSummaryDto` (ommaviy) va endi `AdminProgramListItemDto`/`AdminProgramDetailDto` (admin, batch so'rov — N+1 yo'q) ham `hasPersonalityBattery` bayrog'ini `Domain.Catalog.PersonalityBattery` qoidasidan beradi | Admin frontend (`programComputations.ts`) hamon `PERSONALITY_BATTERY_TEST_CODES` qattiq ro'yxatiga tayanadi — bu QISMI OCHIQ qoldi | Keyingi frontend to'lqinida `programComputations.ts` ni yangi `hasPersonalityBattery` maydoniga o'tkazish (qattiq kod ro'yxati olib tashlanadi) |
| `shared/api/types.ts` da `CompleteTestResponse`/`CompleteSessionResponse`/`StudentResultResponse` hamon qo'lda yozilgan ("MUVAQQAT") | Ikki manba — shartnoma o'zgarsa sezilmaydi | P35 tugagach umumiy `generate:api` o'tkazish va ularni re-export'ga almashtirish |
| Havola ochilishi hisoblagichi botlar va takroriy ochish bilan shishadi (per-IP/qurilma dedup yo'q) | Voronkaning yuqori bo'g'ini haqiqiydan kattaroq ko'rinadi | MVP uchun qabul qilindi. Kerak bo'lsa `docs/13` da CDN/Cloudflare analitikasi bilan solishtirish yoki sessiya cookie bilan dedup |
| `PublicApiTestFactory` `EnsureCreated()` ishlatadi — integratsiya testlari **migratsiyalarni umuman bajarmaydi** | Migratsiya xatolari testlarda ko'rinmaydi (P34 QA aynan shu sababli bloklovchini topdi) | P30 (E2E) da Testcontainers bilan haqiqiy `Database.Migrate()` yo'lini sinash |
| ~~`env.ts` bo'sh `VITE_API_BASE_URL` ni `localhost:5000` ga qaytaradi~~ — **tuzatildi (2026-09-02)**: standart qiymat endi same-origin (bo'sh satr), `/api/...` nisbiy yo'li ishlatiladi |
| `429` javobida `retry-after` yo'q | Foydalanuvchi qancha kutishni bilmaydi | P31 (mustahkamlash) da `ProblemDetails` ga qo'shiladi |
| QA topgan 5 ta zaif savol (cross-loading): `MB-Q58` (SN↔JP), `B5-Q36` (O↔E), `B5-Q49` (A↔ish uslubi), `AC-Q15` (SOCA↔SELF), va `MB-Q01`≈`AC-Q27` deyarli bir xil misol | Omillar orasida ortiqcha korrelyatsiya; ball biroz aniqroq bo'lishi mumkin edi | Bloklovchi emas (professional testlarda ham uchraydi). Pilotdan keyin real ma'lumot bilan qayta ko'riladi — `docs/14` 5-bo'lim |
| EF Core 9.x paketlari net10.0 loyihada (Npgsql EF10 provayderi yo'q) | P03 da runtime muammosi bo'lishi mumkin | P03 boshida DbContext bilan haqiqiy so'rov sinab ko'riladi; provayder chiqqach yangilanadi |
| `/health` va `/health/ready` hozir bir xil (bog'liqlik tekshiruvi yo'q) | Orkestrator noto'g'ri "ready" deb o'ylashi mumkin | P03 da DB health check qo'shilib, `tag` bo'yicha ajratiladi |
| `frontend/src/shared/api/schema.d.ts` eskirgan — `generate:api` P13 dan beri ishga tushirilmagan | Qo'lda yozilgan tiplar bilan haqiqiy shartnoma ajralib ketishi mumkin (P40 aynan 5 ta nomuvofiqlikni topdi) | API ko'tarilgan holda `npm run generate:api` va qo'lda yozilgan DTO'larni re-export'ga almashtirish. **P30 dan oldin** |
| `AssessmentDetailPage` hali `PlaceholderPage` | Sessiya darajasidagi AI belgisi va tafsilotlari ko'rinmaydi (faqat o'quvchi profilida) | Alohida kichik vazifa |
| `npx prettier --check` 79 ta eski faylda ogohlantiradi; `npm run lint` formatni tekshirmaydi | Formatlash bir xil emas, diff shovqini | CI ga `prettier --check` qo'shilsa, avval butun repo bir marta formatlanadi |
| `locales/ru/common.json` bo'sh | Ruscha tarjima yo'q (MVP da o'zbekcha yetarli) | Egasi so'raganda |
| Push qilinmaydi (egasining qarori) — PR oqimi ishlamaydi | Ko'rikni faqat `qa-reviewer` beradi, tashqi review yo'q | Repo: `AbduxalilVoxidjonov/shaxsiyat`. Egasi aytganda `origin` qayta qo'shilib, barcha branch birdan push qilinadi |
