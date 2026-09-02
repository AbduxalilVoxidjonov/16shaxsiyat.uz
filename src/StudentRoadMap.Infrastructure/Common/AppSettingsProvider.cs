using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>
/// `App:*` sir bo'lmagan sozlamalarni `IConfiguration`dan o'qiydi (`docs/06-arxitektura.md`
/// 7-bo'lim). Standart qiymatlar hujjatdagi qiymatlarga mos (`SessionLifetimeDays: 7`).
/// </summary>
internal sealed class AppSettingsProvider : IAppSettings
{
    private const int DefaultSessionLifetimeDays = 7;
    private const int DefaultRefreshTokenDays = 14;

    public AppSettingsProvider(IConfiguration configuration)
    {
        // `ConfigurationBinder.GetValue&lt;T&gt;` uchun alohida paket kerak bo'lmasligi uchun
        // (`Microsoft.Extensions.Configuration.Binder`) qo'lda parse qilinadi.
        var raw = configuration["App:SessionLifetimeDays"];
        SessionLifetimeDays = int.TryParse(raw, out var value) ? value : DefaultSessionLifetimeDays;

        // `bool.TryParse` topilmagan/bo'sh qiymatda `false` qaytaradi — bu aynan xohlangan
        // ehtiyotkor standart (`prompts/12`, `IAppSettings.ShowResultToStudent` izohi).
        _ = bool.TryParse(configuration["App:ShowResultToStudent"], out var showResultToStudent);
        ShowResultToStudent = showResultToStudent;

        var refreshDaysRaw = configuration["Jwt:RefreshTokenDays"];
        RefreshTokenDays = int.TryParse(refreshDaysRaw, out var refreshDays) ? refreshDays : DefaultRefreshTokenDays;
    }

    public int SessionLifetimeDays { get; }

    public bool ShowResultToStudent { get; }

    public int RefreshTokenDays { get; }
}
