namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// `PublicApiTestFactory` + `Ai:AutoAnalyzeOnCompletion=true` — sessiya yakunlangach AI
/// tahlili AVTOMATIK navbatga qo'yiladigan (2026-09-03 gacha yagona bo'lgan) oqimni sinash
/// uchun. Standart `PublicApiTestFactory`da bayroq berilmaydi, ya'ni `false` — o'chirilgan
/// holat aynan o'sha (standart) factory bilan sinaladi. `ShowResultApiTestFactory` naqshi.
/// </summary>
public sealed class AutoAnalyzeApiTestFactory : PublicApiTestFactory
{
    protected override IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; } =
        new Dictionary<string, string?>
        {
            ["Ai:AutoAnalyzeOnCompletion"] = "true",
        };
}
