using FluentAssertions;
using StudentRoadMap.Migrations.Tests.Infrastructure;

namespace StudentRoadMap.Migrations.Tests;

/// <summary>
/// Migratsiya sinovlari JONLI bazaga hech qachon ulanmasligi kerak (`docs/12` §5.1).
/// Bu to'siqning o'zi ham sinaladi (`CLAUDE.md` 10-qoida: test yozilmagan mantiq tugallanmagan).
/// Docker TALAB QILMAYDI — sof mantiq.
/// </summary>
[Trait("Category", "Migrations")]
public sealed class LiveDatabaseGuardTests
{
    [Theory]
    [InlineData("studentroadmap")]     // jonli baza nomi (`docker-compose.yml`)
    [InlineData("16shaxsiyat")]
    [InlineData("postgres")]
    [InlineData("")]
    public void KonteynerdanTashqariBazaNomi_RadEtiladi(string database)
    {
        var connectionString = $"Host=127.0.0.1;Port=5432;Database={database};Username=srm;Password=x";

        var act = () => MigrationsPostgresFixture.CreateContext(connectionString);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Xavfsizlik to'sig'i*")
            .WithMessage($"*{MigrationsPostgresFixture.DatabaseNamePrefix}*");
    }

    [Fact]
    public void KonteynerBazasiNomi_QabulQilinadi()
    {
        var connectionString =
            $"Host=127.0.0.1;Port=54321;Database={MigrationsPostgresFixture.DatabaseNamePrefix}sample_0123456789ab;Username=srm;Password=x";

        var act = () => MigrationsPostgresFixture.CreateContext(connectionString);

        act.Should().NotThrow("to'siq faqat prefiksni tekshiradi — ulanish bu bosqichda ochilmaydi");
    }
}
