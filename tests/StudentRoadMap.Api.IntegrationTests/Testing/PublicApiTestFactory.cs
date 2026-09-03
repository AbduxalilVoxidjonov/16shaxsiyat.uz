using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// Ommaviy sessiya API (`prompts/10`) integratsiya testlari uchun host. Docker/PostgreSQL
/// bu muhitda yo'q — `AppDbContext`ning Npgsql ro'yxatidan o'tkazilishi olib tashlanib,
/// **SQLite in-memory** bilan almashtiriladi. Baza NOMLANGAN va `Cache=Shared` rejimida:
/// har bir DI scope O'Z `SqliteConnection`ini ochadi, lekin hammasi BITTA in-memory bazaga
/// tegishli bo'ladi. Baza faqat unga ochiq ulanish borgacha yashaydi — shu sabab factory
/// umri davomida bitta "tirik ushlab turuvchi" (`_keepAliveConnection`) ulanish ochiq turadi.
///
/// <para>
/// ⚠️ **Nega bitta umumiy `SqliteConnection` EMAS** (2026-09-03, beqaror test tuzatildi):
/// ilgari barcha scope'lar BITTA `SqliteConnection` obyektini baham ko'rardi. Har yangi
/// `AppDbContext` uchun EF Core `SqliteRelationalConnection` yaratadi, u esa konstruktorida
/// `SqliteConnection.CreateFunction(...)` chaqirib ulanishning ICHKI (concurrent bo'lmagan)
/// `Dictionary`siga yozadi. Bu xostda haqiqiy `AnalysisWorkerBackgroundService` ishlaydi va
/// har 2 soniyada o'z scope'ini ochadi — test oqimi bilan AYNI PAYTDA. Ikki oqim bitta
/// `SqliteConnection`ni o'zgartirganda `ObjectDisposedException`,
/// "non-concurrent collections must have exclusive access" yoki
/// "SQLite Error 5: unable to delete/modify user-function due to active statements"
/// tasodifiy chiqardi (`AiAnalysisBackgroundPipelineTests` va vaqti-vaqti bilan admin
/// endpoint testlari). `Microsoft.Data.Sqlite` bitta ulanish uchun THREAD-SAFE EMAS —
/// yechim: har scope o'z ulanishiga ega bo'lsin.
/// </para>
///
/// `sealed` EMAS — `ForwardedHeadersTests` `App:KnownProxies`ni boshqacha qiymat bilan sinash
/// uchun `AdditionalConfiguration`ni override qiladi (pastga qarang).
/// </summary>
public class PublicApiTestFactory : WebApplicationFactory<Program>, Xunit.IAsyncLifetime
{
    /// <summary>Faqat in-memory bazani "tirik" ushlab turish uchun — hech qanday so'rov shu ulanishda bajarilmaydi.</summary>
    private SqliteConnection? _keepAliveConnection;

    /// <summary>Subklasslar bazaviy konfiguratsiya ustiga qo'shimcha/bekor qiluvchi qiymat berishi mumkin.</summary>
    protected virtual IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; } =
        new Dictionary<string, string?>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // `AddInfrastructure` bu qiymatni LAZY o'qiydi (DbContext birinchi
                // materiallashtirilganda) — biz DbContextOptions'ni pastda SQLite bilan
                // almashtiramiz, shu sabab bu qiymat amalda ishlatilmaydi, faqat xavfsizlik uchun.
                ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Port=5432;Database=unused;Username=x;Password=x;Timeout=1",
                ["Security:IpHashSalt"] = "integration-test-salt",
                ["App:SessionLifetimeDays"] = "7",
                // `Program.cs` `IJwtTokenService`ni START-UPda MAJBURIY resolve qiladi
                // (fail-fast, `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 2-band) — bu BARCHA
                // test host'lari (hatto auth bilan ishlamaydiganlari) uchun ham amal qiladi,
                // shu sabab bu ikkalasi shu yerda, BAZAVIY konfiguratsiyada beriladi
                // (`Security:IpHashSalt`dagi bilan bir xil sabab: `IIpHasher` ham har doim
                // resolve qilinishi mumkin). Qiymatlar faqat sinov uchun — ishlab chiqarishda
                // ishlatilmaydi.
                ["Jwt:Key"] = "integration-test-jwt-signing-key-at-least-32-bytes-long-0000",
                ["Jwt:Issuer"] = "studentroadmap-test",
                ["Jwt:Audience"] = "studentroadmap-admin-test",
                ["Security:EncryptionKey"] = Convert.ToBase64String(new byte[32]),
            });

            // Bazaviy qiymatlardan KEYIN qo'shiladi — bir xil kalit bo'lsa ustidan yozadi
            // (`IConfiguration` keyingi manba ustunlik qiladi).
            config.AddInMemoryCollection(AdditionalConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            // `AddInfrastructure` allaqachon `AddDbContext&lt;AppDbContext&gt;`ni Npgsql bilan
            // ro'yxatdan o'tkazgan. Faqat `DbContextOptions&lt;AppDbContext&gt;`ni olib tashlash
            // yetarli emas — EF Core 8+ `IDbContextOptionsConfiguration&lt;AppDbContext&gt;`ni
            // (Enumerable, qo'shiladigan) ham ro'yxatga oladi va ikkalasi ("Npgsql" + "Sqlite")
            // birga qolib ketsa "faqat bitta provayder" xatosini beradi — shu sabab
            // `AppDbContext` bilan bog'liq BARCHA generic ro'yxat yozuvlari olib tashlanadi.
            var efDescriptors = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext)))
                .ToList();
            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<AppDbContext>();

            // Har factory uchun ALOHIDA nomlangan in-memory baza (testlar parallel ishlaydi).
            // `Default Timeout` — bir vaqtda yozayotgan fon ishchisi va test oqimi
            // to'qnashganda `SQLITE_BUSY/LOCKED`da darhol yiqilmasdan qayta urinish uchun.
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"srm-tests-{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                DefaultTimeout = 30,
            }.ToString();

            _keepAliveConnection = new SqliteConnection(connectionString);
            _keepAliveConnection.Open();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString).UseSnakeCaseNamingConvention());
        });
    }

    public async Task InitializeAsync()
    {
        // `Server`ga murojaat host qurilishini majburlaydi — shu bilan `ConfigureWebHost`
        // chaqiriladi va `_connection` yaratiladi (aks holda quyidagi qator `null` bo'ladi).
        _ = Server;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// AVVAL xost to'xtatiladi (`base.DisposeAsync` — `IHost.StopAsync` fon xizmatlarining
    /// joriy siklini kutadi), KEYIN baza yopiladi. Teskari tartibda ishlayotgan
    /// `AnalysisWorkerBackgroundService` yopilgan bazaga so'rov yuborib qolishi mumkin edi.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        if (_keepAliveConnection is not null)
        {
            await _keepAliveConnection.DisposeAsync();
            _keepAliveConnection = null;
        }
    }
}
