using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudentRoadMap.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>
/// Butun sinov to'plami uchun BITTA `postgres:16-alpine` konteyneri ko'taradi va oxirida
/// to'liq o'chiradi. Har test ICHIDA o'z BAZASINI yaratadi (`srm_mig_*`) — testlar bir-birini
/// ifloslantirmaydi.
///
/// ⚠️ JONLI BAZAGA ULANMASLIK KAFOLATI (`docs/12` §5.1):
///   1. Ulanish satri FAQAT konteynerdan olinadi (`GetConnectionString()`) — `.env`,
///      `appsettings*.json`, `ConnectionStrings__Postgres` muhit o'zgaruvchisi HECH QAYERDA
///      o'qilmaydi.
///   2. Host porti TASODIFIY — Testcontainers konteyner 5432 ni bo'sh efemer portga
///      map qiladi (`WithPortBinding(5432, 5432)` ATAYLAB ishlatilmaydi).
///   3. Foydalanuvchi/parol har yurishda tasodifiy generatsiya qilinadi.
///   4. `ConnectionStringFor` har chaqiruvda baza nomini va host/portni QAYTA TEKSHIRADI
///      (<see cref="GuardAgainstNonContainerTarget"/>) — nomi `srm_mig_` bilan boshlanmasa
///      yoki port konteyner porti bo'lmasa, istisno tashlanadi.
/// </summary>
public sealed class MigrationsPostgresFixture : IAsyncLifetime
{
    /// <summary>Sinov bazalari uchun majburiy prefiks — jonli `studentroadmap` bazasidan farqlash uchun.</summary>
    public const string DatabaseNamePrefix = "srm_mig_";

    private const string Image = "postgres:16-alpine";
    private const string MaintenanceDatabase = DatabaseNamePrefix + "maintenance";

    private PostgreSqlContainer? _container;

    /// <summary>Sinov ma'lumotlari uchun barqaror vaqt (determinizm).</summary>
    public static DateTimeOffset Now { get; } = new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        DockerEnvironment.EnsureAvailable();

        _container = new PostgreSqlBuilder(Image)
            .WithDatabase(MaintenanceDatabase)
            .WithUsername("srm_migrations_test")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Konteyner ichida yangi, bo'm-bo'sh baza yaratadi va uning ulanish satrini qaytaradi.
    /// Har test o'z bazasini oladi (`srm_mig_<label>_<tasodifiy>`).
    /// </summary>
    public async Task<string> CreateEmptyDatabaseAsync(string label, CancellationToken cancellationToken = default)
    {
        // Postgres identifikatori 63 belgidan oshmasligi kerak: 8 (prefiks) + <=20 + 1 + 12 = 41.
        var safeLabel = new string(label.ToLowerInvariant().Where(char.IsLetterOrDigit).Take(20).ToArray());
        var databaseName = $"{DatabaseNamePrefix}{safeLabel}_{Guid.NewGuid():N}"[..(DatabaseNamePrefix.Length + safeLabel.Length + 1 + 12)];

        await using (var connection = new NpgsqlConnection(MaintenanceConnectionString()))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            // Baza nomi generatsiya qilingan (foydalanuvchi kiritmagan) va faqat
            // [a-z0-9_] belgilardan iborat — qo'shtirnoq bilan qo'shimcha himoyalangan.
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return ConnectionStringFor(databaseName);
    }

    /// <summary>`AppDbContext` — production bilan bir xil provayder sozlamalari (Npgsql + snake_case).</summary>
    public static AppDbContext CreateContext(string connectionString)
    {
        GuardAgainstNonContainerDatabaseName(connectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, new FixedDateTimeProvider(Now), new NoOpPublisher());
    }

    /// <summary>Xom SQL uchun ochiq ulanish (migratsiyadan oldingi sxemaga ma'lumot yozish uchun).</summary>
    public static async Task<NpgsqlConnection> OpenRawConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        GuardAgainstNonContainerDatabaseName(connectionString);

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private string MaintenanceConnectionString() => ConnectionStringFor(MaintenanceDatabase);

    private string ConnectionStringFor(string databaseName)
    {
        var container = _container
            ?? throw new InvalidOperationException("Konteyner hali ko'tarilmagan — `InitializeAsync` chaqirilmagan.");

        var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = databaseName,
            IncludeErrorDetail = true,
        };

        GuardAgainstNonContainerTarget(builder, container.GetMappedPublicPort(5432));
        return builder.ConnectionString;
    }

    private static void GuardAgainstNonContainerTarget(NpgsqlConnectionStringBuilder builder, int mappedPort)
    {
        if (builder.Port != mappedPort)
        {
            throw new InvalidOperationException(
                $"Xavfsizlik to'sig'i: ulanish porti ({builder.Port}) konteynerning map qilingan porti ({mappedPort}) emas.");
        }

        GuardAgainstNonContainerDatabaseName(builder.ConnectionString);
    }

    private static void GuardAgainstNonContainerDatabaseName(string connectionString)
    {
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database ?? string.Empty;

        if (!database.StartsWith(DatabaseNamePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Xavfsizlik to'sig'i: migratsiya sinovlari faqat '{DatabaseNamePrefix}*' bazalarida ishlaydi, "
                + $"lekin '{database}' so'raldi. Jonli (`studentroadmap` / `16shaxsiyat`) bazaga ULANISH TAQIQLANGAN.");
        }
    }
}

/// <summary>
/// Bitta konteyner butun sinov to'plamiga yetadi — xUnit kolleksiyasi testlarni ketma-ket
/// bajaradi va fixture'ni qayta ishlatadi (konteyner bir marta ko'tariladi).
/// </summary>
[CollectionDefinition(Name)]
public sealed class MigrationsPostgresCollection : ICollectionFixture<MigrationsPostgresFixture>
{
    public const string Name = "postgres-migrations";
}
