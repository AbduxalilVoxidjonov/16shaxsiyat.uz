using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Application.Ai;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;
using StudentRoadMap.Infrastructure.Common;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Jobs;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Security;

namespace StudentRoadMap.Infrastructure;

/// <summary>
/// `Infrastructure` qatlami servislarini DI konteyneriga ro'yxatdan o'tkazadi
/// (`docs/06-arxitektura.md` 2-bo'limi).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Ulanish satri LAZY o'qiladi (options lambdasi ichida, `AddInfrastructure` chaqirilganda
        // emas) — `IConfiguration` (`ConfigurationManager`) jonli obyekt, keyinroq qo'shiladigan
        // manbalar (masalan, `WebApplicationFactory.ConfigureAppConfiguration` — sinov host'i)
        // shu tufayli ko'rinadi. Eager o'qish bu integratsiya sinovlarini buzadi.
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException(
                    "'ConnectionStrings:Postgres' sozlamasi topilmadi. Env o'zgaruvchi yoki user-secrets orqali bering.");

            options
                .UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(30))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAsyncQueryExecutor, EfAsyncQueryExecutor>();
        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddScoped<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<IEntryCodeGenerator, EntryCodeGenerator>();
        services.AddScoped<IIpHasher, IpHasher>();
        // Singleton — ikkalasi ham faqat `IConfiguration`ga tayanadi (holatsiz). `Program.cs`
        // `IJwtTokenService`ni ilova ishga tushganda MAJBURIY resolve qiladi — `Jwt:Key`
        // uzunligi xato bo'lsa fail-fast (`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 2-band).
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITotpService, TotpService>();
        // P47: Telegram Login Widget imzosini tekshiradi. Holatsiz (`Telegram:BotToken`dan
        // hisoblangan `secret_key` konstruktorda bir marta) — `Singleton`. `JwtTokenService`dan
        // farqli, sozlama yo'qligida fail-fast QILMAYDI (`TelegramLoginVerifier` izohi).
        services.AddSingleton<ITelegramLoginVerifier, TelegramLoginVerifier>();
        services.AddSingleton<IAppSettings, AppSettingsProvider>();
        // `prompts/14`: maktab havolasi QR kodi — holatsiz (sof funksiya), `Singleton`.
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        // P18 (`prompts/18`): haqiqiy fon navbati — `analysis_jobs` jadvali + `BackgroundService`
        // (`docs/06` qarorlar jurnaliga qo'shilishi kerak bo'lgan qaror, PM'ga hisobotda qayd
        // etilgan — Hangfire emas, chunki sinov muhiti SQLite `EnsureCreated()` bilan ishlaydi,
        // Postgres-only Hangfire storage'i bunga mos kelmasdi). `NoOpJobQueue` endi ishlatilmaydi.
        services.AddScoped<IBackgroundJobQueue, AnalysisJobQueue>();
        services.AddScoped<IPostCommitActions, PostCommitActions>();
        services.AddScoped<IAnalysisOrchestrator, AnalysisOrchestrator>();
        services.AddHostedService<AnalysisWorkerBackgroundService>();
        services.AddScoped<DbSeeder>();

        // `prompts/16`: prompt qurish (`prompt_templates`ga bog'liq — Scoped) va AI javob
        // validatsiyasi (holatsiz — Singleton). `MockAiProvider` BU YERDA ro'yxatdan
        // o'tkazilmaydi — faqat Development/Test muhitida, composition root'da (`Api/Program.cs`).
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<IAiResponseValidator, AiResponseValidator>();

        // `prompts/17`: uchta real provider — nomlangan `HttpClient`lar (`IHttpClientFactory`),
        // taymeri `Ai:TimeoutSeconds`dan (standart 90s, `docs/09` 7-bo'lim). `AiProviderResolver`
        // qaysi providerni ishlatishni `AiProviderConfig`dan (DB) tanlaydi — kod ichida
        // qattiq yozilmagan (`CLAUDE.md` provider abstraksiyasi qoidasi).
        var timeoutSecondsRaw = configuration["Ai:TimeoutSeconds"];
        var timeout = TimeSpan.FromSeconds(int.TryParse(timeoutSecondsRaw, out var timeoutSeconds) && timeoutSeconds > 0 ? timeoutSeconds : 90);
        services.AddHttpClient(GeminiProvider.HttpClientName, client => client.Timeout = timeout);
        services.AddHttpClient(OpenAiProvider.HttpClientName, client => client.Timeout = timeout);
        services.AddHttpClient(AnthropicProvider.HttpClientName, client => client.Timeout = timeout);
        services.AddScoped<IAiProviderResolver, AiProviderResolver>();
        services.AddSingleton<IAiCostCalculator, AiCostCalculator>();

        // `prompts/11`: ommaviy katalog keshi (10 daqiqa) va savollarni aralashtirish abstraksiyasi.
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IQuestionShuffler, RandomQuestionShuffler>();

        // `prompts/27`: eksport (Excel/PDF) — ikkalasi ham holatsiz (sof funksiya), `Singleton`.
        // `PdfExporter`ning statik konstruktorida `QuestPDF.Settings.License` va shrift
        // registratsiyasi bir marta bajariladi (P27 MAXSUS DIQQAT #7).
        services.AddSingleton<Application.Common.Interfaces.IExcelExporter, StudentRoadMap.Infrastructure.Export.ExcelExporter>();
        services.AddSingleton<Application.Common.Interfaces.IPdfExporter, StudentRoadMap.Infrastructure.Export.PdfExporter>();

        // P39: anketa Excel shabloni/eksporti va yuklangan `.xlsx` ni o'qish — holatsiz,
        // `ExcelExporter` bilan bir xil paket (`ClosedXML`), yangi bog'liqlik yo'q.
        services.AddSingleton<Application.Admin.Catalog.Excel.ICatalogExcelWorkbook, StudentRoadMap.Infrastructure.Excel.CatalogExcelWorkbook>();

        // `/health/ready` DB ulanishini tekshiradi — `ready` tag bilan ajratilib,
        // Api/Program.cs da alohida endpoint sifatida ochiladi (`/health` esa DB'siz jonlik).
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "postgres", tags: ["ready"]);

        return services;
    }
}
