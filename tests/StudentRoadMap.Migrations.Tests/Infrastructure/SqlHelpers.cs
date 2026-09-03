
namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>Xom SQL yordamchilari — migratsiyadan OLDINGI sxemaga ma'lumot yozish va tekshirish uchun.</summary>
internal static class SqlHelpers
{
    public static async Task ExecuteAsync(string connectionString, string sql, CancellationToken ct = default)
    {
        await using var connection = await MigrationsPostgresFixture.OpenRawConnectionAsync(connectionString, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public static async Task<T> ScalarAsync<T>(string connectionString, string sql, CancellationToken ct = default)
    {
        await using var connection = await MigrationsPostgresFixture.OpenRawConnectionAsync(connectionString, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);

        if (value is null or DBNull)
        {
            throw new InvalidOperationException($"So'rov NULL qaytardi: {sql}");
        }

        // `Guid`/`bool` kabi turlar `IConvertible` emas — Npgsql allaqachon to'g'ri CLR turini
        // qaytaradi, shu sabab avval to'g'ridan-to'g'ri uzatish sinaladi.
        return value is T typed
            ? typed
            : (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public static async Task<long> CountAsync(string connectionString, string fromWhere, CancellationToken ct = default)
        => await ScalarAsync<long>(connectionString, $"SELECT count(*) FROM {fromWhere};", ct).ConfigureAwait(false);
}
