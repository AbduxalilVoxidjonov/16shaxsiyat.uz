using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>
/// `App:*`/`Jwt:*`/`Ai:*` sir bo'lmagan sozlamalarni `IConfiguration`dan o'qiydi
/// (`docs/06-arxitektura.md` 7-bo'lim). Standart qiymatlar hujjatdagi qiymatlarga mos
/// (`SessionLifetimeDays: 7`, `AutoAnalyzeOnCompletion: false`).
/// </summary>
internal sealed class AppSettingsProvider : IAppSettings
{
    private const int DefaultSessionLifetimeDays = 7;
    private const int DefaultRefreshTokenDays = 14;
    private const string DefaultPublicWebBaseUrl = "https://16shaxsiyat.uz";

    /// <summary>
    /// P47: standart `true`. Ilgari `false` edi va u YAGONA qaror nuqtasi edi; endi haqiqiy
    /// qaror MAKON darajasida (`School.ShowResultToStudent` — maktab uchun `false`, ommaviy
    /// makon uchun `true`), global bayroq esa faqat avariya rubilnigi (`ShowResultPolicy`).
    /// Sozlama BERILMAGAN muhitda rubilnik YOPIQ bo'lib qolsa, ommaviy mahsulot (shaxsiy
    /// kabinet) qutidan chiqishi bilan ishlamas edi; maktab oqimi esa makon bayrog'i tufayli
    /// avvalgidek yopiq qolaveradi.
    /// </summary>
    private const bool DefaultShowResultToStudent = true;

    public AppSettingsProvider(IConfiguration configuration)
    {
        // `ConfigurationBinder.GetValue&lt;T&gt;` uchun alohida paket kerak bo'lmasligi uchun
        // (`Microsoft.Extensions.Configuration.Binder`) qo'lda parse qilinadi.
        var raw = configuration["App:SessionLifetimeDays"];
        SessionLifetimeDays = int.TryParse(raw, out var value) ? value : DefaultSessionLifetimeDays;

        // Sozlama berilmagan/noto'g'ri bo'lsa — `DefaultShowResultToStudent` (`true`, P47).
        // `bool.TryParse` `false` qaytarsa qiymat yo'q degani, shu sabab standartga tushamiz.
        ShowResultToStudent = bool.TryParse(configuration["App:ShowResultToStudent"], out var showResultToStudent)
            ? showResultToStudent
            : DefaultShowResultToStudent;

        // `bool.TryParse` bo'sh/noto'g'ri qiymatda `false` qaytaradi — bu AYNAN xohlangan
        // standart (`docs/06` 8-bo'lim, 2026-09-03 egasi qarori: AI xarajati nazorati).
        // Sozlama BERILMAGAN muhitda avtomatik tahlil O'CHIQ bo'ladi.
        _ = bool.TryParse(configuration["Ai:AutoAnalyzeOnCompletion"], out var autoAnalyzeOnCompletion);
        AutoAnalyzeOnCompletion = autoAnalyzeOnCompletion;

        var refreshDaysRaw = configuration["Jwt:RefreshTokenDays"];
        RefreshTokenDays = int.TryParse(refreshDaysRaw, out var refreshDays) ? refreshDays : DefaultRefreshTokenDays;

        var publicWebBaseUrl = configuration["App:FrontendUrl"];
        PublicWebBaseUrl = string.IsNullOrWhiteSpace(publicWebBaseUrl) ? DefaultPublicWebBaseUrl : publicWebBaseUrl.TrimEnd('/');
    }

    public int SessionLifetimeDays { get; }

    public bool ShowResultToStudent { get; }

    public bool AutoAnalyzeOnCompletion { get; }

    public int RefreshTokenDays { get; }

    public string PublicWebBaseUrl { get; }
}
