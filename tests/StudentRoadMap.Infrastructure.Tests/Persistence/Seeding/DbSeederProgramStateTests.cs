using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Persistence.Seeding;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence.Seeding;

/// <summary>
/// `DbSeeder.ReconcileProgramStatesAsync` — arxivlangan, lekin `is_active = true` bo'lib
/// qolgan qatorlarni tuzatadigan IDEMPOTENT qadam (2026-09-06).
///
/// Bunday qator egasining jonli bazasida haqiqatda bor edi (`PERSONALITY_PROFILE`,
/// `status = 3 AND is_active = true`) — qo'riqchisiz `Activate()` davridan qolgan. Domen
/// endi uni hosil qila olmaydi, lekin ESKI ma'lumot o'z-o'zidan tuzalmaydi. Migratsiya
/// emas, seed qadami: migratsiya bir marta bajariladi, seed esa har muhitda qayta ishga
/// tushiriladi va zararsiz.
/// </summary>
public sealed class DbSeederProgramStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 0, 0, 0, TimeSpan.Zero);

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_USERNAME"] = "superadmin",
                ["ADMIN_PASSWORD"] = "Sup3rSecret!Pass",
            })
            .Build();

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private static DbSeeder NewSeeder(AppDbContext context, IConfiguration configuration) =>
        new(context, new FixedDateTimeProvider(Now), configuration, new Pbkdf2PasswordHasher(), NullLogger<DbSeeder>.Instance);

    [Fact]
    public async Task SeedAsync_ArxivlanganLekinFaolQatorniTuzatadi_VaIkkinchiMartaHechNarsaOzgartirmaydi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        await using (var context = NewContext(connection))
        {
            await context.Database.EnsureCreatedAsync();
        }

        Guid brokenId;
        Guid healthyId;

        await using (var context = NewContext(connection))
        {
            // Qatorlar ATAYLAB xom SQL bilan shakllantiriladi: ziddiyatli juftlikka
            // (`Archived + IsActive`) domen orqali endi kirib bo'lmaydi, bu esa aynan
            // bazadagi ESKI qatorni taqlid qilish kerak bo'lgan yagona holat.
            var broken = AssessmentProgram.Create(Guid.NewGuid(), "BROKEN-ARCHIVED", "Buzilgan qator", Now);
            var healthy = AssessmentProgram.Create(Guid.NewGuid(), "HEALTHY-ACTIVE", "Sog'lom qator", Now);
            context.AssessmentPrograms.AddRange(broken, healthy);
            await context.SaveChangesAsync();

            brokenId = broken.Id;
            healthyId = healthy.Id;

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE assessment_programs SET status = 3, is_active = 1 WHERE id = {brokenId}");
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE assessment_programs SET status = 2, is_active = 1 WHERE id = {healthyId}");
        }

        var configuration = BuildConfiguration();

        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, configuration).SeedAsync();
        }

        await using (var verify = NewContext(connection))
        {
            var broken = await verify.AssessmentPrograms.AsNoTracking().SingleAsync(p => p.Id == brokenId);
            broken.IsActive.Should().BeFalse("arxivlangan dastur DOIM nofaol bo'lishi shart");
            broken.State.Should().Be(ProgramState.Archived);

            var healthy = await verify.AssessmentPrograms.AsNoTracking().SingleAsync(p => p.Id == healthyId);
            healthy.IsActive.Should().BeTrue("tuzatuvchi qadam faqat arxivlangan qatorlarga tegadi");
            healthy.State.Should().Be(ProgramState.Active);
        }

        // --- 2-marta (idempotentlik): ziddiyatli qator qaytib kelmaydi ---
        await using (var context = NewContext(connection))
        {
            await NewSeeder(context, configuration).SeedAsync();
        }

        await using var verifyAgain = NewContext(connection);
        var conflicting = await verifyAgain.AssessmentPrograms.AsNoTracking()
            .Where(p => p.Status == ProgramStatus.Archived && p.IsActive)
            .ToListAsync();

        conflicting.Should().BeEmpty();
    }
}
