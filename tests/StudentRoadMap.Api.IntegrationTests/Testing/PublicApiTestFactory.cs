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
/// **SQLite in-memory** bilan almashtiriladi (`SqliteAppDbContextFactory` — `Infrastructure.Tests`
/// — bilan bir xil naqsh). Ulanish butun factory umri davomida ochiq turadi (aks holda
/// in-memory baza yo'qoladi).
///
/// `sealed` EMAS — `ForwardedHeadersTests` `App:KnownProxies`ni boshqacha qiymat bilan sinash
/// uchun `AdditionalConfiguration`ni override qiladi (pastga qarang).
/// </summary>
public class PublicApiTestFactory : WebApplicationFactory<Program>, Xunit.IAsyncLifetime
{
    private SqliteConnection? _connection;

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

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection).UseSnakeCaseNamingConvention());
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

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
