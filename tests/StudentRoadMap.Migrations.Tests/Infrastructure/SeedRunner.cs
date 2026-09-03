using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;

namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>
/// Haqiqiy `DbSeeder`ni sinov uchun quradi — `Api/Program.cs` dagi `--seed` bosqichi bilan
/// bir xil (`GetRequiredService&lt;DbSeeder&gt;().SeedAsync()`), faqat DI o'rniga qo'lda.
/// Seed fayllari `AppContext.BaseDirectory/SeedData` dan o'qiladi (csproj `Content` orqali
/// haqiqiy `src/.../SeedData` fayllari ko'chiriladi — dublikat qilinmaydi).
/// </summary>
internal static class SeedRunner
{
    public const string AdminUsername = "srm_migrations_admin";
    public const string AdminPassword = "MigrationsTest!Pass1";

    public static DbSeeder Create(AppDbContext context) =>
        new(
            context,
            new FixedDateTimeProvider(MigrationsPostgresFixture.Now),
            Configuration(),
            new FakePasswordHasher(),
            NullLogger<DbSeeder>.Instance);

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_USERNAME"] = AdminUsername,
                ["ADMIN_PASSWORD"] = AdminPassword,
                ["ADMIN_EMAIL"] = "migrations-test@16shaxsiyat.uz",
            })
            .Build();
}
