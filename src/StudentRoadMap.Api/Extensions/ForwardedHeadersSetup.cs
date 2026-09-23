using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace StudentRoadMap.Api.Extensions;

/// <summary>
/// Production'da API Caddy reverse-proksi ortida turadi (`docs/13-deploy-va-infratuzilma.md`
/// 4-bo'lim: `api.16shaxsiyat.uz` → `api` konteyner). Proksi orqasida `HttpContext.Connection
/// .RemoteIpAddress` doim proksining o'zi bo'ladi — shu sabab `X-Forwarded-For`'ni o'qib haqiqiy
/// mijoz IP'siga almashtirish shart, aks holda IP-asosli tezlik cheklovi (`RateLimitSetup`) va
/// IP xeshlash (`IIpHasher`) jimgina ishlamay qoladi (hamma foydalanuvchi bitta "IP" ostida
/// yig'iladi).
///
/// **Muhim xavfsizlik sharti (spoofing himoyasi):** `ForwardedHeadersOptions.KnownProxies`/
/// `KnownIPNetworks`ni ODDIY `Clear()` qilib bo'sh qoldirish YETARLI EMAS — bu ASP.NET Core'ning
/// hujjatlanmagan "footgun"i: `ForwardedHeadersMiddleware` ikkala ro'yxat ham BO'SH bo'lganda
/// tekshiruvni O'TKAZIB YUBORADI va HAR QANDAY manbadan kelgan `X-Forwarded-For`'ga ishonadi
/// (amalda tekshirilgan: `KnownProxies.Clear(); KnownIPNetworks.Clear();` — istalgan uzoq IP
/// header orqali qabul qilinaveradi, garchi ro'yxat "bo'sh" bo'lsa ham). Shu sabab bo'sh
/// ro'yxatda middleware UMUMAN RO'YXATDAN O'TKAZILMAYDI — `UseForwardedHeaders` chaqirilmasa,
/// `X-Forwarded-For` header'i butunlay e'tiborsiz qoldiriladi va `RemoteIpAddress` haqiqiy TCP
/// ulanish manzili bo'lib qoladi (lokal/test muhitida xavfsiz standart holat).
/// </summary>
public static class ForwardedHeadersSetup
{
    /// <summary>Vergul bilan ajratilgan ishonchli proksi ro'yxati (IP yoki `IP/prefix` CIDR).</summary>
    public const string KnownProxiesConfigKey = "App:KnownProxies";

    public static WebApplication UseConfiguredForwardedHeaders(this WebApplication app)
    {
        var entries = ParseEntries(app.Configuration[KnownProxiesConfigKey]);

        if (entries.Count == 0)
        {
            // Konfiguratsiyada ishonchli proksi yo'q — header'ga ISHONILMAYDI (yuqoridagi izoh).
            return app;
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // Standart `ForwardLimit = 1` — faqat ENG O'NG (oxirgi) yozuv o'qiladi. Production
            // zanjirida IKKI ishonchli hop bor: Cloudflare edge → `tunnel` (cloudflared) →
            // `app` (nginx) → `api`. nginx `$proxy_add_x_forwarded_for` bilan cloudflared'ning
            // docker IP'sini qo'shadi, ya'ni `api` ko'radigan header: `<mijoz>, <cloudflared>`.
            // Limit 1 bo'lsa `RemoteIpAddress` = cloudflared konteyneri — BARCHA foydalanuvchi
            // bitta "IP" ostida yig'ilib, IP rate-limit butun sayt uchun umumiy bo'lib qolardi
            // (2026-09-23 da nginx logidan tasdiqlangan: `remote=docker`, XFF = tashqi IP).
            //
            // `null` — cheklovsiz, LEKIN xavfsiz: middleware o'ngdan chapga faqat joriy manzil
            // `KnownProxies`/`KnownIPNetworks` ichida bo'lgan paytgacha yuradi va birinchi
            // ishonchsiz manzilda to'xtaydi. Mijoz o'zi yuborgan soxta yozuvlar (Cloudflare
            // ularni haqiqiy IP'dan CHAPGA qo'yadi) shu sabab hech qachon o'qilmaydi.
            ForwardLimit = null,
        };

        // Standart ro'yxatlar (loopback) tozalanadi — faqat konfiguratsiyadagi manzillarga ishoniladi.
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var entry in entries)
        {
            if (IPNetwork.TryParse(entry, out var network))
            {
                options.KnownIPNetworks.Add(network);
            }
            else if (IPAddress.TryParse(entry, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }

        app.UseForwardedHeaders(options);
        return app;
    }

    internal static IReadOnlyList<string> ParseEntries(string? configuredValue) =>
        string.IsNullOrWhiteSpace(configuredValue)
            ? []
            : configuredValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
