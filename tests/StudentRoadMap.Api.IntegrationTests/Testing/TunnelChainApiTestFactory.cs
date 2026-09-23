using StudentRoadMap.Api.Extensions;

namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// Production topologiyasi: `App:KnownProxies` = docker compose subneti (`172.26.0.0/16`,
/// `docker-compose.yml`). Zanjir: Cloudflare edge → `tunnel` (cloudflared) → `app` (nginx) →
/// `api` — ya'ni `api`ga ikki ishonchli hop'li `X-Forwarded-For` keladi (`ForwardedHeadersSetup`).
/// </summary>
public sealed class TunnelChainApiTestFactory : PublicApiTestFactory
{
    public const string DockerSubnet = "172.26.0.0/16";

    /// <summary>cloudflared konteynerining docker IP'si (nginx uni XFF oxiriga qo'shadi).</summary>
    public const string CloudflaredIp = "172.26.0.9";

    protected override IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; } =
        new Dictionary<string, string?>
        {
            [ForwardedHeadersSetup.KnownProxiesConfigKey] = DockerSubnet,
        };
}
