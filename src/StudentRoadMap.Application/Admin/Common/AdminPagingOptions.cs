namespace StudentRoadMap.Application.Admin.Common;

/// <summary>
/// Admin ro'yxat so'rovlari uchun sahifalash normalizatsiyasi — `docs/07-api-shartnoma.md`
/// 4-bo'lim: `pageSize` maksimum 100. `prompts/14` MAXSUS DIQQAT #1: katta so'ralsa **jimgina**
/// 100ga tushiriladi, xato QAYTARILMAYDI.
/// </summary>
internal static class AdminPagingOptions
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;
    public const int DefaultPage = 1;

    /// <summary>`page &lt; 1` → 1ga; `pageSize &lt;= 0` → standart; `pageSize &gt; 100` → 100ga (jimgina).</summary>
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? DefaultPage : page;
        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (normalizedPage, normalizedPageSize);
    }
}
