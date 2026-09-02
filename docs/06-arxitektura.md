# 06 — Arxitektura

## 1. Umumiy ko'rinish

```
┌──────────────────────────┐        ┌──────────────────────────┐
│  Ommaviy SPA (o'quvchi)  │        │  Admin SPA (superadmin)  │
│  React 19 + TS + Vite    │        │  React 19 + TS + Vite    │
│  /t/:slug                │        │  /admin/*                │
└─────────────┬────────────┘        └────────────┬─────────────┘
              │  HTTPS / JSON                    │  HTTPS / JSON + JWT
              └───────────────┬──────────────────┘
                              ▼
              ┌───────────────────────────────┐
              │  StudentRoadMap.Api           │
              │  ASP.NET Core 10, Controllers │
              │  JWT + SessionToken auth      │
              │  Rate limiting, ProblemDetails│
              └───────────────┬───────────────┘
                              ▼
              ┌───────────────────────────────┐
              │  Application (CQRS/MediatR)   │
              │  Commands, Queries, Validators│
              │  IScoringEngine, IAiAnalyzer  │
              └───────────────┬───────────────┘
                     ┌────────┴────────┐
                     ▼                 ▼
       ┌───────────────────┐   ┌────────────────────────┐
       │  Domain           │   │  Infrastructure        │
       │  Entity, VO,      │   │  EF Core + Npgsql      │
       │  Events, Rules,   │   │  AI providerlar        │
       │  Scoring strategy │   │  Hangfire, Excel, PDF  │
       └───────────────────┘   └───────────┬────────────┘
                                           ▼
                        ┌──────────────┬──────────────────┐
                        │ PostgreSQL16 │ Gemini/OpenAI/   │
                        │              │ Anthropic API    │
                        └──────────────┴──────────────────┘
```

---

## 2. Yechim strukturasi

```
StudentRoadMap.sln
├── src/
│   ├── StudentRoadMap.Domain/
│   │   ├── Common/            AggregateRoot, Entity, IDomainEvent, Result<T>
│   │   ├── Schools/           School.cs, SchoolSlug.cs
│   │   ├── Students/          Student.cs, PhoneNumber.cs, Gender.cs
│   │   ├── Assessments/       Assessment.cs, AssessmentTest.cs, Answer.cs, TestResult.cs, Statuses
│   │   ├── Catalog/           TestDefinition.cs, Question.cs, AnswerOption.cs, TypeCatalog.cs, CareerMap.cs
│   │   ├── Scoring/           IScoringStrategy.cs, ScoringInput/Result, Mbti16Strategy.cs,
│   │   │                      BigFiveStrategy.cs, RiasecStrategy.cs, ActivityStrategy.cs,
│   │   │                      SumStrategy.cs (Custom anketalar), CompositeScorer.cs,
│   │   │                      ReliabilityCalculator.cs, ScoringConstants.cs
│   │   ├── Ai/                AiAnalysis.cs, AiProvider.cs, AiAnalysisStatus.cs
│   │   ├── Identity/          AdminUser.cs, RefreshToken.cs, AdminRole.cs
│   │   └── Events/            AssessmentCompletedEvent.cs, ...
│   │
│   ├── StudentRoadMap.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/     ValidationBehavior, LoggingBehavior, TransactionBehavior
│   │   │   ├── Interfaces/    IAppDbContext, ICurrentUser, IDateTime, ITokenGenerator,
│   │   │   │                  IAiAnalysisProvider, IAiProviderResolver, IEncryptionService,
│   │   │   │                  IExcelExporter, IPdfExporter, IBackgroundJobQueue
│   │   │   ├── Models/        PagedResult<T>, Result, ProblemCodes
│   │   │   └── Mappings/      (Mapster / qo'lda mapping)
│   │   ├── Public/            (o'quvchi oqimi)
│   │   │   ├── StartSession/  StartSessionCommand + Handler + Validator
│   │   │   ├── GetSession/    GetSessionStateQuery
│   │   │   ├── GetQuestions/  GetTestQuestionsQuery
│   │   │   ├── SaveAnswers/   SaveAnswersCommand
│   │   │   ├── CompleteTest/  CompleteTestCommand  → scoring
│   │   │   └── CompleteSession/ CompleteSessionCommand → reliability + AI navbat
│   │   ├── Schools/           Create/Update/Delete/List/Get/RegenerateLink
│   │   ├── Students/          List/Get/Delete/Export
│   │   ├── Assessments/       List/GetDetail/RerunAnalysis
│   │   ├── Catalog/           ListTests/UpdateQuestion/ImportQuestions
│   │   ├── Ai/                GetConfigs/UpsertConfig/TestConnection/AnalyzeAssessment
│   │   ├── Dashboard/         GetStatsQuery
│   │   └── Identity/          Login/Refresh/Logout/ChangePassword/Me
│   │
│   ├── StudentRoadMap.Infrastructure/
│   │   ├── Persistence/       AppDbContext, Configurations/*, Migrations/, DbSeeder,
│   │   │                      SeedData/ (mbti16.json, big5.json, riasec.json, activity.json,
│   │   │                                 type-catalog.json, career-map.json)
│   │   ├── Ai/                GeminiProvider.cs, OpenAiProvider.cs, AnthropicProvider.cs,
│   │   │                      AiProviderResolver.cs, PromptBuilder.cs, AnalysisJsonSchema.cs,
│   │   │                      AiResponseValidator.cs
│   │   ├── Identity/          JwtTokenService, PasswordHasher, TotpService
│   │   ├── Security/          AesEncryptionService, IpHasher
│   │   ├── Jobs/              AnalysisJobQueue, AnalysisWorker (Hangfire yoki HostedService)
│   │   ├── Export/            ClosedXmlExporter, QuestPdfExporter
│   │   └── DependencyInjection.cs
│   │
│   └── StudentRoadMap.Api/
│       ├── Controllers/       PublicSessionController, SchoolsController, StudentsController,
│       │                      AssessmentsController, CatalogController, AiConfigController,
│       │                      DashboardController, AuthController, AuditController, ExportController
│       ├── Middleware/        ExceptionHandlingMiddleware, RequestLoggingMiddleware
│       ├── Auth/              SessionTokenHandler (ommaviy oqim uchun scheme)
│       ├── Extensions/        SwaggerSetup, CorsSetup, RateLimitSetup
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── StudentRoadMap.Domain.Tests/        (scoring golden testlar)
│   ├── StudentRoadMap.Application.Tests/   (handler testlar)
│   └── StudentRoadMap.Api.IntegrationTests/(Testcontainers + WebApplicationFactory)
│
├── frontend/
│   ├── public-app/     (o'quvchi SPA)  — yoki bitta app ichida route bo'linishi
│   └── admin-app/
│
├── docs/               (shu hujjatlar)
├── prompts/            (ishlab chiqish promptlari)
├── docker/             Dockerfile.api, Dockerfile.web, nginx.conf
├── docker-compose.yml
└── CLAUDE.md
```

> **Eslatma:** hozirgi papkadagi `StudentRoadMap/` (MVC shablon) `src/StudentRoadMap.Api/` ga
> aylantiriladi: `Views/`, `wwwroot/lib/`, `Models/ErrorViewModel.cs` o'chiriladi,
> `Controllers/HomeController.cs` o'rniga API kontrollerlar keladi. Buni **Prompt 01** bajaradi.

---

## 3. Qatlam qoidalari

| Qatlam | Bog'liqligi | Nima qiladi | Nima QILMAYDI |
|--------|-------------|-------------|---------------|
| **Domain** | Hech nimaga (faqat BCL) | Entity, VO, biznes qoidalar, scoring formulalar, hodisalar | EF, HTTP, DateTime.Now, IO |
| **Application** | Domain | Use-case (Command/Query), validatsiya, orkestrovka, interfeyslar | Konkret DB/HTTP kutubxona |
| **Infrastructure** | Domain + Application | EF Core, AI SDK, fayl, navbat, shifrlash | Biznes qoidalar |
| **Api** | Application + Infrastructure (DI uchun) | HTTP, auth, model bog'lash, xato → ProblemDetails | Biznes mantiq |

**Qat'iy:** `Domain` va `Application` da `Microsoft.EntityFrameworkCore` paketi bo'lmaydi
(faqat `IAppDbContext` abstraksiyasi orqali; u `Application` da e'lon qilinadi va
`DbSet<T>` o'rniga `IQueryable<T>` qaytaradi yoki EF.Core.Abstractions ishlatiladi).

---

## 4. CQRS konvensiyasi

```csharp
// Command
public sealed record RegenerateSchoolLinkCommand(Guid SchoolId) : IRequest<Result<SchoolLinkDto>>;

// Handler — bitta use-case, bitta fayl
internal sealed class RegenerateSchoolLinkHandler
    : IRequestHandler<RegenerateSchoolLinkCommand, Result<SchoolLinkDto>> { ... }

// Validator — FluentValidation, pipeline behavior orqali avtomatik ishlaydi
public sealed class RegenerateSchoolLinkValidator
    : AbstractValidator<RegenerateSchoolLinkCommand> { ... }
```

Qoidalar:
- Bitta papka = bitta use-case (Command + Handler + Validator + DTO).
- Query'lar **read-only**, `AsNoTracking()`, to'g'ridan-to'g'ri DTO'ga proyeksiya (`Select`).
- Command'lar `TransactionBehavior` ichida bajariladi.
- Handler'lar `internal sealed`.
- **Validator'lar `public sealed`** — `FluentValidation.DependencyInjectionExtensions`
  `AssemblyScanner` faqat public tiplarni topadi; `internal` bo'lsa validatsiya jimgina
  ishlamay qoladi (P10 da integratsiya sinovi bilan tasdiqlandi).
- Natija: `Result<T>` — istisno biznes oqimi uchun ishlatilmaydi.

---

## 5. Texnologik qarorlar (ADR qisqacha)

| № | Qaror | Sabab | Muqobil (rad etilgan) |
|---|-------|-------|------------------------|
| ADR-1 | Clean Architecture + CQRS | Loyiha o'sadi (v2: rollar, mobil ilova); IntellectCRM bilan bir xil yondashuv | Oddiy MVC — UI cheklangan, testlash qiyin |
| ADR-2 | PostgreSQL | `jsonb` ball/AI javob uchun ideal, bepul, Docker'da yengil | SQL Server — litsenziya, Linux'da og'irroq |
| ADR-3 | Ballar `jsonb` da, tez filtrlanadigan qiymatlar alohida ustunda | Sxema moslashuvchan, lekin ro'yxat tez | Har omilga ustun — yangi metodikada migratsiya |
| ADR-4 | AI provider abstraksiyasi (`IAiAnalysisProvider`) | Superadmin qaysi kalitni qo'ysa o'sha ishlaydi; fallback zanjiri | Bitta providerga qattiq bog'lanish |
| ADR-5 | AI structured output (JSON schema) | Parse ishonchli, UI barqaror | Erkin matn — parse xatolari |
| ADR-6 | Scoring backendda, deterministik | Bir xil natija, audit qilinadi, frontend soxtalashtira olmaydi | Frontendda hisoblash |
| ADR-7 | Ommaviy oqimda `SessionToken` (login yo'q) | O'quvchiga hisob yaratish keraksiz to'siq | Login/parol — drop-off oshadi |
| ADR-8 | AI fon jarayonida (navbat) | 30–60 s kutish HTTP so'rovni bloklamaydi | Sinxron chaqiruv — timeout riski |
| ADR-9 | Bitta React app, ikkita route guruhi (`/t/*`, `/admin/*`) | Bitta build, bitta deploy, umumiy komponentlar | Ikkita alohida SPA — ortiqcha murakkablik |
| ADR-10 | Hangfire (yoki `BackgroundService` + DB navbat) | Retry, dashboard, kechiktirilgan ish | Redis/RabbitMQ — MVP uchun ortiqcha |
| ADR-11 | Snapshot ustunlar `students` da | Admin ro'yxati JOIN'siz, tez | Har so'rovda agregat — sekin |
| ADR-12 | `ProblemDetails` (RFC 9457) barcha xatolarda | Frontendda yagona xato ishlash | Har xil format |
| ADR-13 | Superadmin anketalari uchun universal `SUM` strategiyasi | Kod yozmasdan yangi test qo'shish mumkin; ilmiy metodikalar himoyalangan qoladi | Har anketaga alohida kod — superadmin qila olmaydi |
| ADR-14 | Tizim testlari `IsSystem` bayrog'i bilan qulflangan | Savol soni normalizatsiyaga kiradi; o'zgarsa eski natijalar taqqoslanmay qoladi | Hamma narsani tahrirlashga ruxsat — yaxlitlik buziladi |
| ADR-15 | `Draft → Published → Archived` holat oqimi | Yarim tayyor anketa o'quvchiga chiqib ketmaydi | Darhol faol — xato anketa sessiyalarga tushadi |

---

## 6. Xato ishlash

Barcha xatolar `application/problem+json`:

```json
{
  "type": "https://studentroadmap/errors/session-expired",
  "title": "Sessiya muddati tugagan",
  "status": 410,
  "detail": "Ushbu test sessiyasining amal qilish muddati tugadi.",
  "code": "SESSION_EXPIRED",
  "traceId": "00-9f2c...-01",
  "errors": { "phone": ["Telefon raqami noto'g'ri formatda"] }
}
```

| `code` | HTTP | Qachon |
|--------|------|--------|
| `VALIDATION_ERROR` | 400 | FluentValidation |
| `UNAUTHORIZED` | 401 | JWT yo'q/eskirgan |
| `FORBIDDEN` | 403 | Rol yetarli emas |
| `NOT_FOUND` | 404 | Resurs yo'q |
| `SCHOOL_INACTIVE` | 410 | Maktab o'chirilgan |
| `SESSION_EXPIRED` | 410 | Sessiya muddati o'tgan |
| `DUPLICATE_ASSESSMENT` | 409 | BR-1 buzildi |
| `ACCESS_CODE_INVALID` | 400 | Maktab kirish kodi noto'g'ri (P10) |
| `ACCOUNT_LOCKED` | 423 | 5 xato urinishdan keyin 15 daqiqalik blokirovka (P13) |
| `SCHOOL_HAS_STUDENTS` | 409 | O'quvchisi bor maktabni o'chirib bo'lmaydi (P14) |
| `UNIQUE_CONSTRAINT_CONFLICT` | 409 | Unique indeks buzildi (masalan slug poyga holatida) (P14) |
| `TOTP_REQUIRED` | 401 | Foydalanuvchida 2FA yoqilgan, `totpCode` kerak (P13) |
| `TOTP_ALREADY_ENABLED` | 409 | 2FA allaqachon yoqilgan (P13) |
| `TOTP_NOT_ENABLED` | 409 | 2FA yoqilmagan, o'chirib bo'lmaydi (P13) |

> **`ProblemDetails` qo'shimcha maydonlari.** `Error` tipida ixtiyoriy `Extensions` lug'ati bor;
> u `ProblemDetails.Extensions` ga ko'chiriladi. Shu orqali xato bilan birga kontekst yuboriladi —
> masalan `VALIDATION_ERROR` bilan `unansweredCount` (P12, testni yakunlashda nechta savol
> javobsiz qolgani). Frontend shu maydonga tayanib aniqroq xabar ko'rsatadi.
| `TEST_NOT_UNLOCKED` | 409 | Oldingi test tugamagan |
| `SYSTEM_TEST_LOCKED` | 409 | Tizim metodikasini o'zgartirishga urinish |
| `TEST_NOT_PUBLISHABLE` | 400 | Anketa nashr validatsiyasidan o'tmadi |
| `TEST_IN_USE` | 409 | Ishlatilgan testni o'chirishga urinish |
| `RATE_LIMITED` | 429 | Limit oshdi |
| `AI_PROVIDER_ERROR` | 502 | Provider javob bermadi |
| `INTERNAL_ERROR` | 500 | Kutilmagan |

---

## 7. Konfiguratsiya (`appsettings.json` + env)

```json
{
  "ConnectionStrings": { "Postgres": "Host=db;Database=studentroadmap;Username=srm;Password=***" },
  "Jwt": { "Issuer": "studentroadmap", "Audience": "studentroadmap-admin",
           "Key": "***", "AccessTokenMinutes": 30, "RefreshTokenDays": 14 },
  "App": { "FrontendUrl": "https://16shaxsiyat.uz", "SessionLifetimeDays": 7,
           "SeedOnStartup": false },
  "Security": { "EncryptionKey": "***base64-32byte***", "IpHashSalt": "***" },
  "Ai": { "DefaultProvider": "Gemini", "TimeoutSeconds": 90, "MaxRetries": 3,
          "EnableFallbackChain": true, "PromptVersion": "v1.0" },
  "RateLimit": { "PublicRegisterPerHourPerIp": 10, "AnswerSavePerMinutePerSession": 120 },
  "Serilog": { "MinimumLevel": "Information" }
}
```

**Sirlar** hech qachon repo'ga tushmaydi: `dotnet user-secrets` (dev), env o'zgaruvchi (prod).

---

## 8. Qarorlar jurnali (yangi qarorlar shu yerga qo'shiladi)

| Sana | Qaror | Kim | Sabab |
|------|-------|-----|-------|
| 2026-08-31 | Boshlang'ich arxitektura tanlandi (ADR-1..12) | — | MVP asos |
| 2026-08-31 | Superadmin uchun anketa konstruktori qo'shildi (ADR-13..15) | Loyiha egasi | Superadmin kod yozmasdan o'z testini kirita olishi kerak |
| 2026-08-31 | EF Core paketlari `9.x` da qulflandi (net10.0 loyihada) | PM (P01) | Npgsql'ning EF Core 10 uchun barqaror provayderi hali chiqmagan; TFM oldinga moslik bilan ishlaydi. P03 da runtime'da tekshiriladi, provayder chiqqach yangilanadi |
| 2026-08-31 | Loyiha uchun lokal git repozitoriysi ochildi (`main` + `feat/*` branch'lar) | PM (P01) | PM.md 6-bo'limidagi git intizomi uchun shart edi; masofaviy repo hali yo'q |
| 2026-08-31 | `docs/05` da ikki marta ishlatilgan `TestStatus` enum nomi ajratildi: `test_definitions.status` endi **`TestDefinitionStatus`** | PM (P02) | Bitta nom ikki xil enum uchun ishlatilgan edi — kodda to'qnashardi. Qiymat raqamlari o'zgarmadi |
| 2026-08-31 | Soft delete maydonlari **DDL bo'yicha** (School/Student: `IsDeleted`+`DeletedAt`; Assessment: faqat `IsDeleted`); `DeletedByAdminUserId` yo'q | PM (P02) | `docs/04` §5 matni DDL bilan mos emas edi — DDL asos qilib olindi, kim o'chirgani `AuditLog` da qoladi |
| 2026-08-31 | `Assessment.MarkAnalyzing` `Completed`, `Analyzed` va `AnalysisFailed` holatlaridan ruxsat etiladi | PM (P02) | `docs/06` Application/Assessments/`RerunAnalysis` use-case MVP qamrovida — qayta tahlil kerak |
| 2026-08-31 | Admin lockout: 5 noto'g'ri urinish → 15 daqiqa (`docs/08`) domenga kiritildi | PM (P02) | `docs/04`/`docs/05` da son yo'q edi; taxmin o'rniga mavjud hujjat qiymati olindi |
| 2026-08-31 | Barcha savol banklarida shkalalar navbatlashadi va teskari savollar aralash joylashadi (ketma-ket ≤ 2 bir xil `direction`, ≤ 2 bir xil `scale`) | PM (P05–P08) | Bir shkalaning savollari blok bo'lib kelsa o'quvchi mavzuni payqab bir xil javob bosadi (javob to'plami effekti); teskari savollar aynan buni ushlash uchun kerak. `prompts/05` da MBTI16 uchun talab qilingan — 4 metodika ham bir xil printsipda |
| 2026-08-31 | `IAppDbContext` `DbSet<T>` emas, `IQueryable<T>` + `Add`/`Remove`/`SaveChangesAsync` beradi; async materializatsiya `IAsyncQueryExecutor` orqali | PM (P03) | `prompts/03` "DbSet<T>" degan, lekin `CLAUDE.md` 2-qoidasi va shu hujjatning 3-bo'limi `Application` da EF Core paketini qat'iy taqiqlaydi. Hujjat promptdan ustun. Natija: `Application.csproj` da EF paketi yo'q |
| 2026-08-31 | `admin_users` da `lower(...)` ifoda indeksi o'rniga `username_lower`/`email_lower` STORED generated ustunlari + unique indeks | PM (P03) | EF Core fluent API ifoda indeksini xom SQL'siz yarata olmaydi; funksional natija bir xil (registrga befarq unikallik) |
| 2026-08-31 | `ix_students_last_at` uchun migratsiyada xom SQL ishlatishga ruxsat (`DESC NULLS LAST`) | PM (P03) | EF fluent API'da NULLS tartibi sozlanmaydi, `docs/05` §5 esa test topshirmagan o'quvchilar ro'yxat oxirida turishini talab qiladi. "Qo'lda SQL yozilmaydi" qoidasidan yagona, indeks nuansiga cheklangan istisno |
| 2026-08-31 | `test_definitions.question_count` DB ustuni sifatida saqlanmaydi (`Ignore()`) | PM (P03) | Domain'da hisoblanadigan get-only xususiyat. **Ogohlantirish:** LINQ `Select()` proyeksiyasida ishlatib bo'lmaydi — katalog ro'yxati so'rovida `Questions.Count()` subquery kerak bo'ladi (P13/P29) |
| 2026-08-31 | `test_scales`, `audit_logs`, `registration_counters` jadvallari `InitialCreate` da yo'q | PM (P03) | Domain'da mos entity hali yo'q: `TestScale` → P33, `AuditLog` → P13–P15, `RegistrationCounter` → P10 (BR-1 kunlik limit). Qo'shimcha jadval keyingi migratsiyada to'qnashuvsiz qo'shiladi |
| 2026-08-31 | Parol xeshi: `docs/08` dagi BCrypt/Argon2id o'rniga **PBKDF2-HMACSHA256 210k** | PM (P04) | OWASP 2023 tavsiyasiga mos, .NET ichida (uchinchi tomon paketi kerak emas), bitta superadmin hisobi uchun yetarli (lockout 5/15 daq bilan birga). `docs/08` yangilandi. Ko'p adminli v2 da Argon2id qayta ko'riladi |
| 2026-08-31 | `docs/03` §7 ga 5 ta aniqlashtirish qo'shildi (§7.1): `AllSame`↔`StraightLining` istisnosi, blok sanash, sessiya bo'ylab tartiblash, teskari ziddiyat `d` formulasi, tez javob maxraji | PM (P09) | Jadval qisqa edi va ikki xil talqinga yo'l qo'yardi; QA ikkita bloklovchi xato aynan shu noaniqlikdan kelib chiqqanini ko'rsatdi |
| 2026-08-31 | `TestDefinition.Duplicate()` dagi `Guid.NewGuid()` — texnik qarz sifatida qoldirildi | PM (P02) | Domain ichida ID generatsiyasi loyiha konvensiyasini buzadi (boshqa hamma fabrika ID ni parametr oladi), lekin `Duplicate` hozir hech qayerda ishlatilmaydi. P33 (anketa konstruktori) da tuzatiladi |
| 2026-08-31 | Refresh token **faqat `httpOnly` cookie** orqali yuradi; `docs/07` jadvali shunga moslandi | PM (P19) | `docs/07` tanada `{refreshToken}` deb ko'rsatgan, `docs/08` esa cookie-only degan edi — ziddiyat. Xavfsizlik hujjati ustun: XSS holatida refresh token o'g'irlanmasligi kerak |
| 2026-08-31 | Admin access token `adminClient.ts` ichida modul o'zgaruvchisi (HTTP qatlami manbai) + `authStore` (Zustand, `persist`siz) React reaktivligi uchun | PM (P19) | `docs/10` "faqat xotirada" deydi; `localStorage` ishlatilmaydi. Ikki joy sinxron: klient `setOnAdminSessionExpired` orqali store'ni tozalaydi |
| 2026-08-31 | `publicClient` sessiya tokenini `features/` dan import qilmaydi — umumiy `STORAGE_KEYS.session` kaliti shartnomasi orqali o'qiydi | PM (P19) | `docs/10` qoidasi: `shared/` `features/` ga bog'lanmaydi. Kalit `shared/config/storageKeys.ts` da yagona manba |
| 2026-08-31 | Validator'lar `public sealed` (hujjatdagi `internal` namuna tuzatildi) | PM (P10) | `AssemblyScanner` faqat public validatorlarni ko'radi — `internal` bo'lsa validatsiya jimgina ishlamaydi. Integratsiya sinovi bilan tasdiqlandi |
| 2026-08-31 | `IAppDbContext` ga `AsNoTracking<T>()`, `BeginTransactionAsync`, `IncrementRegistrationCounterAsync`, `RegistrationCounters` qo'shildi | PM (P10) | "Query'lar `AsNoTracking`" va "Command'lar tranzaksiyada" talablarini `Application` da EF paketisiz bajarish uchun zarur |
| 2026-08-31 | `POST /api/public/sessions`: yangi sessiya **201**, `resumed: true` → **200** | PM (P10) | `docs/07` faqat 201 ni ko'rsatgan edi; REST semantikasi bo'yicha mavjud resursga qaytish 200 |
| 2026-08-31 | `registration_counters` faqat **yangi** `Assessment` yaratilganda oshadi (resume/dublikatda emas) | PM (P10) | BR-1 maktabning kunlik **ro'yxatdan o'tish** sonini o'lchaydi. Har so'rovni sanash bitta o'quvchi sahifani yangilaganda maktab limitini yeb qo'yardi. Suiiste'mol — rate limiting vazifasi |
| 2026-09-01 | 16 tipning barcha nomlari qayta yozildi (Tayanch, G'amxo'r, Teran, Loyihachi, Chevar, Sezgir, Orzumand, Bilimdon, Sinovchi, Quvnoq, Otashqalb, Yangilikchi, Tuzuvchi, Jonkuyar, Murabbiy, Bunyodkor) | Loyiha egasi (`CLAUDE.md` 6a, `docs/17` §8) | Eski nomlarning **hammasi** 16Personalities nomlarining o'zbekcha tarjimasi edi (Strateg=Architect, Vositachi=Mediator, Konsul=Consul…). Keirsey nomlari ham taqiqlandi. Har yangi nom 4 harfli kodning ma'nosidan kelib chiqadi va teng qadrli (hech biri "pastroq" eshitilmaydi) |
| 2026-09-02 | Barcha ommaviy endpointlar `ActionResult<T>` + `[ProducesResponseType]` bilan; `SupportNonNullableReferenceTypes()` va `RequiredNonNullablePropertiesSchemaFilter` yoqildi | PM (P11) | Ilgari hech bir endpoint javob sxemasini generatsiya qilmasdi — `npm run generate:api` bo'sh tip berardi va PM.md §9 darvozasi #4 ma'nosiz edi. `SupportNonNullableReferenceTypes()` yolg'iz o'zi `required` ro'yxatini to'ldirmasligi empirik aniqlandi, shuning uchun alohida sxema filtri yozildi |
| 2026-09-02 | `ProblemDetails.errors` kalitlari camelCase (`fullName`, `answers[0].questionId`) | PM (P11) | `System.Text.Json` nom siyosati `Dictionary<string,T>` kalitlariga qo'llanmaydi — FluentValidation `PropertyName` i PascalCase bo'lib sizib chiqardi. Frontend buni qo'lda xarita bilan aylanib o'tayotgan edi; xarita olib tashlandi |
| 2026-09-02 | Savol matni va `scaleLabels` `Assessment.LanguageCode` bo'yicha tanlanadi, mos matn yo'q bo'lsa `uz` ga qaytadi; **kesh kaliti tilni o'z ichiga oladi** | PM (P11) | `prompts/11` talabi bajarilmagan edi. Kesh tilni ajratmasa, `ru` matn qo'shilgan kuni kesh birinchi so'ragan tilni hamma uchun qaytarardi — jimgina, tushunish qiyin xato. Hozircha faqat `uz` kontenti seed qilingan, mexanizm tayyor |
| 2026-09-02 | TanStack Table ishlatilmaydi — `DataTable` qo'lda yozildi va bog'liqlik olib tashlandi | PM (P22) | Bizga faqat server tomonda hisoblangan sahifa/saralashni ko'rsatish kerak; kutubxonaning qiymati mijoz-tomon saralash/filtrlash/guruhlashda. `docs/10` va `prompts/19` yangilandi |
| 2026-09-02 | `admin_totp_backup_codes` jadvali va `admin_users.totp_last_used_step` ustuni qo'shildi | PM (P13) | Zaxira kodlarni xom saqlash mumkin emas — har biri alohida xeshlangan qator; `totp_last_used_step` bir TOTP kodini ikki marta ishlatishni bloklaydi. `docs/05` yangilandi |
| 2026-09-02 | `POST /api/auth/totp/disable` `{currentPassword}` talab qiladi | PM (P13) | `docs/07` da tana ko'rsatilmagan edi. Parolsiz o'chirish o'g'irlangan sessiyaga 2FA ni yechib tashlash imkonini berardi — ya'ni 2FA ning ma'nosi yo'qolardi |
| 2026-09-02 | Admin login rate limit: 10 urinish / 5 daqiqa / IP | PM (P13) | `AdminUser` blokirovkasi (5/15daq) hisob bo'yicha ishlaydi; rate limit esa IP bo'yicha — birgalikda parol terish va hisob sanash hujumlarini qoplaydi |
| 2026-09-02 | Admin ro'yxatlarida saralash **doim DB darajasida**; SQLite tarjima qila olmaydigan `DateTimeOffset` testlari `Skip` bilan o'tkazib yuboriladi | PM (P14) | Dastlab saralash xotiraga ko'chirilgan edi (SQLite cheklovi uchun) — bu filtrga mos **barcha** o'quvchini yuklardi, `ix_students_last_at` indeksini foydasiz qilardi va "1000 o'quvchida <300ms" talabini buzardi. Sinov muhiti qulayligi uchun ishlab chiqarish kodi pasaytirilmaydi |
| 2026-09-02 | `GET /api/admin/schools/{id}` javobida ham `qrCodeBase64` qaytadi | PM (P14/P23) | Aks holda admin QR ko'rish uchun `regenerate-link` chaqirishga majbur bo'lardi — bu eski havolani darhol o'ldiradi va maktab o'quvchilarini yarim yo'lda qoldiradi |
| 2026-09-02 | Admin API umumiy rate limit: 300/daqiqa/IP | PM (P14) | `docs/07` §4 da talab qilingan, lekin P13 dan beri hech bir admin endpointda yo'q edi |
| 2026-09-02 | Sinov muhitida (SQLite) `DateTimeOffset` ustunlar `long` (`UtcTicks`) ga konverter bilan o'giriladi; Postgres'da `timestamptz` o'zgarmaydi | PM (P15) | SQLite `DateTimeOffset` bo'yicha na `ORDER BY`, na `WHERE` ni tarjima qila olmaydi — shu sabab sana filtrlari va saralash **hech qachon integratsiya sinovidan o'tmagan** edi va `Skip` soni o'sib borardi. Konverter faqat `ProviderName == Sqlite` bo'lganda qo'llanadi. **Diqqat:** xom SQL konverterni chetlab o'tadi — `TryMarkTotpBackupCodeUsedAsync` da qo'lda o'girish kerak bo'ldi |
| 2026-09-02 | Dashboard kesh kaliti so'rovning **xom** `from`/`to` qiymatidan quriladi (`null` ham kalit qismi) | PM (P15) | Avval hisoblangan qiymatdan qurilardi va `to = UtcNow` tik aniqligida bo'lgani uchun parametrsiz chaqiruvda (dashboard'ning odatiy yuklanishi) kesh **hech qachon urmasdi**. Mavjud test buni sezmasdi, chunki ikkala chaqiruvda `from`/`to` ni qotirib berardi |
| 2026-09-02 | Yangi admin DTO shakllari (`AdminAssessmentListItemDto`, `AdminAuditLogItemDto`, `AdminRecalculateScoresResultDto`) va limitlar (`recentAssessments` 10, `hollandTop` 10) | PM (P15) | `docs/07` da faqat yo'l jadvali bor edi, javob shakli yo'q. `GET /assessments/{id}` uchun mavjud `AdminLatestAssessmentDto` qayta ishlatildi — yangi tur yaratilmadi |
| 2026-09-02 | **Dastur (`AssessmentProgram`) tushunchasi kiritildi** — nomlangan, tartiblangan test to'plami. Sessiya endi dasturga bog'lanadi (`Assessment.ProgramId`) | Loyiha egasi | Ilgari nashr qilingan **har** test **har** sessiyaga avtomatik qo'shilardi (`prompts/33` 12-band): 5 anketa nashr qilinsa o'quvchi 190 + N savolga majbur bo'lardi. Dastur 4 talik ilmiy batareyani ham **yaxlit** ushlab turadi — `MaturityIndex` faqat BIG5 + ACTIVITY birga bo'lganda ma'noga ega |
| 2026-09-02 | Dastur ko'rinishi: `Public` (barcha maktabda) yoki `Assigned` (`school_programs` orqali biriktirilgan maktablarda) | Loyiha egasi | "maktabga biriktirish yoki ommaviy qilish" talabi |
| 2026-09-02 | **Batareya majburiy emas** — admin qaysi dasturlar maktabga biriktirilishini hal qiladi; o'quvchi kirishda mavjudlaridan **bittasini tanlaydi** (bitta bo'lsa avtomatik) | Loyiha egasi (2026-09-02 savoliga javob: "admin hal qiladi biriktirishni olishni") | **Oqibati katta:** sessiyada shaxsiyat ma'lumoti bo'lmasligi mumkin. `CompositeScorer`, `StudentSnapshot`, dashboard taqsimotlari, o'quvchilar ro'yxati ustunlari, profil sahifasi va AI prompti "ma'lumot yo'q" holatini **jimgina emas, aniq** ishlashi shart |
| 2026-09-02 | `TestDefinition.ScoringMode`: `Scored` (ballanadi) yoki `Survey` (ballanmaydi) | Loyiha egasi ("Ikkalasi ham kerak") | "Forms" ikki xil: ballanadigan anketa (SUM) va oddiy so'rovnoma. `Survey` javoblari saqlanadi, `TestResult` ball yozilmaydi, scoring va AI xulosasiga kirmaydi |
| 2026-09-02 | Migratsiyada mavjud 4 metodika bitta tizim dasturiga (`PERSONALITY_PROFILE` — "Shaxsiyat profili", `Public`, `IsSystem`) birlashtiriladi; mavjud sessiyalar o'shanga bog'lanadi | PM | Orqaga moslik: bugungi ma'lumot va oqim buzilmaydi |
| 2026-09-02 | Tizim dasturi **deterministik `Guid`** bilan yaratiladi (`00000000-…-0001`) va backfill **migratsiya ichida**, `SET NOT NULL` dan oldin bajariladi | PM (P34 QA) | Backfill seeder'ga qoldirilgan edi, lekin `--migrate` barcha migratsiyalarni bitta chaqiruvda bajaradi va `--seed` faqat undan keyin ishlaydi — mavjud sessiyalar nol-GUID ga bog'lanib FK cheklovini buzardi va deploy to'xtardi. Testlar buni ushlay olmasdi: ular `EnsureCreated()` ishlatadi va migratsiyalarni umuman bajarmaydi |
| 2026-09-02 | `GET /api/public/schools/{slug}` (Query) havola ochilishi hisoblagichini oshiradi — `docs/06` §4 "Query'lar read-only" qoidasidan **ataylab qilingan istisno** | PM | Bu domen holati emas, telemetriya. Alohida endpoint yomonroq bo'lardi: qo'shimcha so'rov, mijoz uni o'tkazib yuborishi mumkin, poyga holati. **Shart:** hisoblagich xatosi `try/catch` bilan yutiladi — telemetriya nosozligi o'quvchini landing sahifasiga kirita olmay qo'ymasligi kerak |
| 2026-09-02 | Dashboard voronkasi: `linkViews → registered → started → completed → analyzed`, `[from,to]` oynasida; `schoolBreakdown` maksimum 20 qator | PM (egasi talabi) | "maktablar qancha, so'rov qancha, qanchasi register qilmoqda, qanchasi so'rovnomadan o'tmoqda". Havola ochilishi ilgari umuman kuzatilmasdi — usiz konversiyani nisbat bilan o'lchab bo'lmasdi |
| 2026-09-02 | `null` qoidasi dashboard'ga ham kengaytirildi: `avgDurationMinutes`, `avgReliability`, `dropOffRate`, `completionRate` — ma'lumot yo'q bo'lsa `null` | PM | `avgReliability = 0` dashboard'da "ma'lumot sifati falokat" deb o'qilardi, aslida hali sessiya yakunlanmagan edi. `completionRate = 0` esa "0% yakunladi" (yomon natija) va "hali hech kim ro'yxatdan o'tmagan" ni bir xil ko'rsatardi |
