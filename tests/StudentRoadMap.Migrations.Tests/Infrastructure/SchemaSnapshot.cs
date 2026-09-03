
namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>
/// Haqiqiy bazadagi sxemani (ustunlar, cheklovlar, indekslar, kengaytmalar) matn ro'yxati
/// sifatida o'qiydi — ikki bazani (migratsiya bilan qurilgan va modeldan `EnsureCreated()`
/// bilan qurilgan) taqqoslash uchun.
///
/// Nima uchun kerak: `dotnet ef migrations has-pending-model-changes` faqat KOD darajasida
/// (model ↔ snapshot) tekshiradi. Migratsiyaning xom SQL'i (`migrationBuilder.Sql`) yoki
/// qo'lda tahrirlangan qadam snapshot bilan mos, lekin HAQIQIY sxema bilan mos bo'lmasligi
/// mumkin — bu taqqoslash aynan shuni ushlaydi.
/// </summary>
internal static class SchemaSnapshot
{
    /// <summary>Migratsiya tarixi jadvali faqat migratsiya bilan qurilgan bazada bo'ladi — solishtiruvdan chiqariladi.</summary>
    private const string HistoryTable = "__EFMigrationsHistory";

    public static async Task<IReadOnlyList<string>> ColumnsAsync(string connectionString, CancellationToken ct = default)
        => await QueryAsync(
            connectionString,
            $"""
             SELECT table_name || '.' || column_name
                    || ' type=' || data_type
                    || COALESCE(' len=' || character_maximum_length, '')
                    || COALESCE(' prec=' || numeric_precision || ',' || numeric_scale, '')
                    || ' nullable=' || is_nullable
                    || ' default=' || COALESCE(column_default, '-')
             FROM information_schema.columns
             WHERE table_schema = 'public' AND table_name <> '{HistoryTable}'
             ORDER BY 1;
             """,
            ct).ConfigureAwait(false);

    public static async Task<IReadOnlyList<string>> ConstraintsAsync(string connectionString, CancellationToken ct = default)
        => await QueryAsync(
            connectionString,
            $"""
             SELECT c.conrelid::regclass::text || ' | ' || c.conname || ' | ' || pg_get_constraintdef(c.oid)
             FROM pg_constraint c
             JOIN pg_namespace n ON n.oid = c.connamespace
             WHERE n.nspname = 'public'
               AND c.conrelid::regclass::text NOT LIKE '%{HistoryTable}%'
             ORDER BY 1;
             """,
            ct).ConfigureAwait(false);

    public static async Task<IReadOnlyList<string>> IndexesAsync(string connectionString, CancellationToken ct = default)
        => await QueryAsync(
            connectionString,
            $"""
             SELECT indexdef
             FROM pg_indexes
             WHERE schemaname = 'public' AND tablename <> '{HistoryTable}'
             ORDER BY 1;
             """,
            ct).ConfigureAwait(false);

    public static async Task<IReadOnlyList<string>> ExtensionsAsync(string connectionString, CancellationToken ct = default)
        => await QueryAsync(connectionString, "SELECT extname FROM pg_extension ORDER BY 1;", ct).ConfigureAwait(false);

    private static async Task<IReadOnlyList<string>> QueryAsync(string connectionString, string sql, CancellationToken ct)
    {
        await using var connection = await MigrationsPostgresFixture.OpenRawConnectionAsync(connectionString, ct).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var rows = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            rows.Add(reader.GetString(0));
        }

        return rows;
    }
}
