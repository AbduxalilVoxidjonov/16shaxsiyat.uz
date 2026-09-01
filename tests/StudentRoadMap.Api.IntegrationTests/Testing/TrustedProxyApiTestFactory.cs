using StudentRoadMap.Api.Extensions;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// `PublicApiTestFactory` + `App:KnownProxies`da bitta ishonchli proksi manzili — `X-Forwarded-For`
/// ishonch bilan qabul qilinadigan holatni sinash uchun (`ForwardedHeadersSetup`).
/// </summary>
public sealed class TrustedProxyApiTestFactory : PublicApiTestFactory
{
    /// <summary>Testlarda ishlatiladigan "ishonchli proksi" manzili — `docs`dagi haqiqiy IP emas, ixtiyoriy sinov qiymati.</summary>
    public const string TrustedProxyIp = "203.0.113.10";

    protected override IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; } =
        new Dictionary<string, string?>
        {
            [ForwardedHeadersSetup.KnownProxiesConfigKey] = TrustedProxyIp,
        };
}
