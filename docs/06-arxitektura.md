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
internal sealed class RegenerateSchoolLinkValidator
    : AbstractValidator<RegenerateSchoolLinkCommand> { ... }
```

Qoidalar:
- Bitta papka = bitta use-case (Command + Handler + Validator + DTO).
- Query'lar **read-only**, `AsNoTracking()`, to'g'ridan-to'g'ri DTO'ga proyeksiya (`Select`).
- Command'lar `TransactionBehavior` ichida bajariladi.
- Handler'lar `internal sealed`.
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
  "App": { "FrontendUrl": "https://salohiyat.uz", "SessionLifetimeDays": 7,
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
