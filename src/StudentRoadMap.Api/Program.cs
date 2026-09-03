using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Api.Middleware;
using StudentRoadMap.Api.Swagger;
using StudentRoadMap.Application;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.ChangePassword;
using StudentRoadMap.Application.Identity.DisableTotp;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---------------------------------------------------------------
// `Destructure.ByTransforming` — parol/token tashuvchi buyruqlar tasodifan `{@request}` kabi
// to'liq obyekt sifatida log qilinsa ham (hozircha `LoggingBehavior` faqat request NOMINI
// log qiladi, lekin bu himoya ehtiyot chorasi sifatida qo'shildi) — sir maydonlar hech qachon
// log oqimiga tushmaydi (`CLAUDE.md` 4-qoida, `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 6-band).
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Destructure.ByTransforming<LoginCommand>(c => new { c.Username, HasTotpCode = c.TotpCode is not null })
        .Destructure.ByTransforming<ChangePasswordCommand>(c => new { c.AdminUserId })
        .Destructure.ByTransforming<DisableTotpCommand>(c => new { c.AdminUserId }));

// --- Servislar ---------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // `docs/07-api-shartnoma.md` 4-bo'lim: "Enum'lar JSON'da string ko'rinishida" (`"Male"`, `"Analyzed"`...).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Non-nullable C# xususiyatlari OpenAPI `required` ro'yxatiga tushishi, `T?` bo'lganlari esa
    // tushmasdan `nullable: true` bo'lib qolishi uchun — `Directory.Build.props`dagi
    // `Nullable=enable` kompilyator chiqargan NRT metadatasidan foydalanadi. Aks holda barcha
    // maydonlar ixtiyoriy ko'rinadi va `npm run generate:api` chiqargan TS tiplari haqiqiy
    // shartnomani aks ettirmaydi (frontend agenti xabari, 2026-09-02).
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<RequiredNonNullablePropertiesSchemaFilter>();
});

// RFC 9457 `ProblemDetails` + `code` maydoni (docs/06-arxitektura.md, 6-bo'lim). Konkret `code`
// qiymatlarini `ExceptionHandlingMiddleware`/`ControllerResultExtensions`/auth handlerlari
// beradi; `ProblemDetailsSetup` faqat ular yetib bormagan TRANSPORT xatolariga (404/405/415…)
// zaxira kod qo'yadi.
builder.Services.AddConfiguredProblemDetails();

// `[ApiController]`ning standart `ValidationProblemDetails` javobini (unda `code` YO'Q) loyihaning
// yagona shakli bilan almashtiradi — buzuq JSON, noto'g'ri tur va yo'q maydon holatlari uchun.
builder.Services.AddModelStateProblemDetails();

builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();

// `docs/08-auth-va-xavfsizlik.md` 7-bo'lim: HSTS 1 yil + subdomenlar. `UseHsts()` standart
// qiymati atigi 30 kun, shu sabab aniq berilyapti. Faqat Production'da qo'llanadi (pastda).
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddRateLimitPolicies();

// --- Autentifikatsiya: SessionToken (o'quvchi, `docs/08` 4-bo'lim, DEFAULT sxema — o'zgarmaydi)
// + JWT Bearer (superadmin, `docs/08` 2-bo'lim, QO'SHIMCHA sxema, `[Authorize(Policy =
// JwtAuthenticationSetup.SuperAdminPolicy)]` orqali aniq tanlanadi). ---------------------------
builder.Services
    .AddAuthentication(SessionTokenAuthenticationHandler.SchemeName)
    .AddScheme<SessionTokenAuthenticationSchemeOptions, SessionTokenAuthenticationHandler>(
        SessionTokenAuthenticationHandler.SchemeName, _ => { })
    .AddAdminJwtBearer(builder.Configuration);
builder.Services.AddAdminAuthorizationPolicy();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, StudentRoadMap.Api.Auth.CurrentUser>();

var frontendUrl = builder.Configuration["App:FrontendUrl"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            policy.WithOrigins(frontendUrl)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// `MockAiProvider` — tarmoqqa chiqmaydi, doim to'g'ri JSON qaytaradi. FAQAT Development/Test
// muhitida ro'yxatga olinadi (`prompts/16` MAXSUS DIQQAT #5) — bu qaror ATAYLAB shu yerda,
// composition root'da (`Infrastructure`ning o'zi muhitni bilmaydi): production'da bu satr
// hech qachon ishlamaydi, shu bilan haqiqiy AI tahlil o'rniga soxta javob ketib qolishining
// oldi olinadi. `WebApplicationFactory` standart holatda "Development" muhitini ishlatadi —
// integratsiya testlari ham shu orqali `MockAiProvider`ni ko'radi.
if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Test")
{
    builder.Services.AddSingleton<StudentRoadMap.Application.Common.Interfaces.IAiAnalysisProvider, StudentRoadMap.Infrastructure.Ai.MockAiProvider>();
}

var app = builder.Build();

// --- Fail-fast: `Jwt:Key` 32 baytdan qisqa/yo'q bo'lsa ilova SHU YERDA (start-upda) xato bilan
// to'xtaydi — birinchi login so'roviga qadar kutilmaydi (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT
// 2-band). `JwtTokenService` konstruktorida tekshiradi (`AesEncryptionService` bilan bir xil
// naqsh); Singleton bo'lgani uchun bu yerda MAJBURIY resolve qilish uni darhol ishga tushiradi.
// Test host'ining qo'shimcha konfiguratsiyasi (`ConfigureAppConfiguration`) `Build()` ichida
// allaqachon qo'llanilgan bo'ladi, shu sabab bu yerda o'qish xavfsiz (`AddInfrastructure`dagi
// "lazy o'qish" izohi bilan bir xil asos).
using (var jwtValidationScope = app.Services.CreateScope())
{
    _ = jwtValidationScope.ServiceProvider.GetRequiredService<StudentRoadMap.Application.Common.Interfaces.IJwtTokenService>();
}

// --- `--migrate`: migratsiyani bajarib ilovani to'xtatadi (docs/05 4-bo'lim: production'da
// `Database.Migrate()` avtomatik emas — alohida step/konteyner) ---------------------------
if (args.Contains("--migrate"))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

// --- `--seed`: test bankini/tip katalogini/kasb xaritasini/superadminni yuklab ilovani
// to'xtatadi (`docs/05` 4-bo'lim: seed migratsiyaga qo'yilmaydi, idempotent `DbSeeder`)
if (args.Contains("--seed"))
{
    using var seedScope = app.Services.CreateScope();
    var seeder = seedScope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
    return;
}

// `App:SeedOnStartup=true` bo'lsa har ishga tushishda idempotent seed bajariladi
// (masalan, docker-compose'da birinchi marta ko'tarilganda).
if (app.Configuration.GetValue<bool>("App:SeedOnStartup"))
{
    using var startupSeedScope = app.Services.CreateScope();
    var startupSeeder = startupSeedScope.ServiceProvider.GetRequiredService<DbSeeder>();
    await startupSeeder.SeedAsync();
}

// --- HTTP quvuri ---------------------------------------------------------------
// `UseConfiguredForwardedHeaders` pipeline'ning ENG BOSHIDA turishi shart — boshqa barcha
// middleware (shu jumladan HTTPS redirect, rate limiter, autentifikatsiya) `RemoteIpAddress`ga
// tayanadi (`ForwardedHeadersSetup` izohiga qarang, `docs/13` 4-bo'lim).
app.UseConfiguredForwardedHeaders();

// Xavfsizlik sarlavhalari — quvurning eng boshida, shunda undan keyingi HAR QANDAY javob
// (xato, 404, rate limit, auth rad javobi) ularni oladi (`docs/08` 7-bo'lim).
app.UseSecurityHeaders();

// Swagger FAQAT Development'da (`docs/08` 9-bo'lim ro'yxati: "Swagger production'da
// o'chirilgan"). `docker-compose.yml` standart holatda `ASPNETCORE_ENVIRONMENT=Production`
// beradi — ya'ni ishlab chiqarishda `/swagger` va `/swagger/v1/swagger.json` umuman
// ro'yxatga olinmaydi (404). Sxema hujjati ichki API yuzasini (barcha admin endpointlari,
// maydon nomlari, enum qiymatlari) ochib beradi va autentifikatsiyasiz o'qiladi — bu
// razvedka uchun tayyor xarita. Frontend'ning `npm run generate:api` buyrug'i lokal
// (Development) API'ga qarshi ishlaydi, shu sabab bu qaror ish oqimini buzmaydi.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Konteyner/orkestrator uchun jonlik va tayyorlik tekshiruvlari — ikkisi `tag` bilan ajratilgan:
// `/health` — DB'siz jonlik (hech qanday tekshiruv bajarilmaydi), `/health/ready` — faqat
// `ready` tegli tekshiruvlar (hozircha DB ulanishi, `AddDbContextCheck` orqali).
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

// Integratsiya testlari (WebApplicationFactory) uchun ochiq qilinadi.
public partial class Program;
