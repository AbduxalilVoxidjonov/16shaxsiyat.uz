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

    public AppSettingsProvider(IConfiguration configuration)
    {
        // `ConfigurationBinder.GetValue&lt;T&gt;` uchun alohida paket kerak bo'lmasligi uchun
        // (`Microsoft.Extensions.Configuration.Binder`) qo'lda parse qilinadi.
        var raw = configuration["App:SessionLifetimeDays"];
        SessionLifetimeDays = int.TryParse(raw, out var value) ? value : DefaultSessionLifetimeDays;
    }

    public int SessionLifetimeDays { get; }
}
