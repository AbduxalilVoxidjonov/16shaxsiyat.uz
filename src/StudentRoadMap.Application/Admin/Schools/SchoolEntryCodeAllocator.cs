using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Application.Admin.Schools;

/// <summary>
/// Bo'sh (bazada yo'q) maktab kodini tanlaydi — `CreateSchoolCommandHandler.AllocateUniqueSlugAsync`
/// bilan bir xil "proaktiv tekshiruv + DB indeksi yakuniy himoya" naqshi. 31^8 fazoda
/// to'qnashish amalda uchramaydi, lekin `AccessToken` (32 bayt) dan farqli bu kod QISQA —
/// shu sabab bir necha urinish ATAYLAB bor. ChIN bir vaqtdagi poyga holatini
/// `ux_schools_entry_code` ushlaydi (`409 UNIQUE_CONSTRAINT_CONFLICT`).
///
/// O'chirilgan (`IsDeleted`) maktablar ham tekshiriladi (`IgnoreQueryFilters` — indeks
/// filtri faqat `entry_code IS NOT NULL`, `is_deleted` ga qaramaydi).
/// </summary>
internal static class SchoolEntryCodeAllocator
{
    private const int MaxAttempts = 10;

    public static async Task<string> AllocateUniqueAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IEntryCodeGenerator generator,
        CancellationToken cancellationToken)
    {
        string candidate = generator.Generate();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var exists = await executor.AnyAsync(
                context.IgnoreQueryFilters(context.Schools).Where(s => s.EntryCode == candidate),
                cancellationToken).ConfigureAwait(false);

            if (!exists)
            {
                return candidate;
            }

            candidate = generator.Generate();
        }

        // Nazariy holat: 10 ta ketma-ket to'qnashish. So'nggi nomzod qaytariladi — DB indeksi
        // baribir yakuniy hakam (`UniqueConstraintViolationException` → 409).
        return candidate;
    }
}
