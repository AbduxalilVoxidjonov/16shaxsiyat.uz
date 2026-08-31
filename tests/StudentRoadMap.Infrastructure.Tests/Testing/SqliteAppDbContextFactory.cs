using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Infrastructure.Tests.Testing;

/// <summary>
/// `AppDbContext`ni **SQLite in-memory** ustida quradi — Docker/PostgreSQL bu muhitda yo'q
/// (`prompts/04`), InMemory provayder esa tranzaksiyani qo'llamaydi (`DbSeeder.SeedAsync`
/// S8: bitta tranzaksiya). SQLite relational, real tranzaksiya/rollback beradi — Npgsql'ga
/// eng yaqin xatti-harakat. Ulanish ochiq turishi shart — aks holda in-memory baza yo'qoladi.
/// </summary>
internal static class SqliteAppDbContextFactory
{
    public static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    public static AppDbContext CreateContext(SqliteConnection connection, FixedDateTimeProvider dateTime, NoOpPublisher publisher)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, dateTime, publisher);
    }
}
