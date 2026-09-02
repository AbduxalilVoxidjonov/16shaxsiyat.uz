namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// `PublicApiTestFactory` + `App:ShowResultToStudent=true` — `GET /api/public/sessions/result`
/// (`docs/07` 1.9-bo'lim, `prompts/12` cheklovi 8: standart qiymat `false`) yoqilgan holatni
/// sinash uchun. Standart `PublicApiTestFactory`dan alohida — sozlama o'chirilgan holatni
/// (`403 FORBIDDEN`) boshqa factory (standart) bilan sinaladi.
/// </summary>
public sealed class ShowResultApiTestFactory : PublicApiTestFactory
{
    protected override IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; } =
        new Dictionary<string, string?>
        {
            ["App:ShowResultToStudent"] = "true",
        };
}
