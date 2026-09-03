using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using StudentRoadMap.Application.Admin.Students;

namespace StudentRoadMap.Application.Tests.Admin.Students;

/// <summary>
/// `latestAssessment.results` SHARTNOMA KALITLARI — `docs/07-api-shartnoma.md` 3.2-bo'lim.
/// <para>
/// Muammo (2026-09-03 tekshiruvi): backend `AdminTestResultsDto` xususiyatlari standart
/// camelCase siyosati bilan `mbti16`/`big5`/`riasec`/`activity` bo'lib chiqardi, mijoz esa
/// shartnomadagi `MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY` ni o'qirdi — natijada shaxsiyat va kasb
/// qiziqishlari bo'limlari JIMGINA bo'sh chiqardi (hech qanday xato ko'rsatilmasdan).
/// Shunga o'xshash kalit farqi faqat HARFMA-HARF tekshiradigan test bilan ushlanadi, chunki
/// `ReadFromJsonAsync&lt;AdminTestResultsDto&gt;` bilan qilingan integratsiya testi kalitni
/// o'zi qayta bog'lab yuboradi va farqni KO'RMAYDI.
/// </para>
/// </summary>
public sealed class AdminTestResultsJsonKeysTests
{
    /// <summary>
    /// `Program.cs`dagi `AddControllers().AddJsonOptions(...)` bilan bir xil: web sukut
    /// qiymatlari (camelCase) + string enum. Kalit nomlari aynan shu siyosatda hosil bo'ladi.
    /// </summary>
    private static readonly JsonSerializerOptions ApiOptions = CreateApiOptions();

    private static JsonSerializerOptions CreateApiOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static AdminTestResultsDto SampleResults() => new(
        new AdminMbti16Dto(
            "INTJ",
            "Loyihachi",
            new Dictionary<string, AdminAxisDto>(StringComparer.Ordinal)
            {
                ["EI"] = new(28.3, "I", false),
                ["SN"] = new(71.6, "N", false),
                ["TF"] = new(33.3, "T", false),
                ["JP"] = new(64.1, "J", false),
            },
            []),
        new AdminBig5Dto(
            new Dictionary<string, AdminFactorDto>(StringComparer.Ordinal)
            {
                ["O"] = new(38, 70, "Yuqori"),
                ["C"] = new(41, 77.5, "Yuqori"),
                ["E"] = new(24, 35, "Past"),
                ["A"] = new(35, 62.5, "Yuqori"),
                ["N"] = new(22, 30, "Past"),
            },
            70,
            68.4,
            "Yaxshi"),
        new AdminRiasecDto(
            "IRA",
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["R"] = 62,
                ["I"] = 88,
                ["A"] = 71,
                ["S"] = 40,
                ["E"] = 35,
                ["C"] = 48,
            },
            53,
            "High",
            []),
        new AdminActivityDto(
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["MOT"] = 74,
                ["SELF"] = 68,
                ["SOCA"] = 52,
                ["ENG"] = 60,
            },
            65.2,
            "Moderate",
            false));

    [Fact]
    public void Results_KalitlariTestDefinitionCodeBilanBirXil()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(SampleResults(), ApiOptions));

        var keys = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();

        keys.Should().BeEquivalentTo(
            ["MBTI16", "BIG5", "RIASEC", "ACTIVITY"],
            "`docs/07` 3.2: `results` kalitlari `TestDefinition.Code` bilan harfma-harf bir xil");
    }

    [Theory]
    [InlineData("MBTI16")]
    [InlineData("BIG5")]
    [InlineData("RIASEC")]
    [InlineData("ACTIVITY")]
    public void Results_CamelCaseKalitYozilmaydi(string contractKey)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(SampleResults(), ApiOptions));

        document.RootElement.TryGetProperty(contractKey, out _)
            .Should().BeTrue($"shartnoma kaliti `{contractKey}` bo'lishi kerak");

#pragma warning disable CA1308 // Shartnoma tekshiruvi: aynan camelCase VARIANTI yo'qligini isbotlaydi.
        var camelCaseKey = JsonNamingPolicy.CamelCase.ConvertName(contractKey);
#pragma warning restore CA1308
        document.RootElement.TryGetProperty(camelCaseKey, out _)
            .Should().BeFalse($"`{camelCaseKey}` — sukut siyosatidan kelib chiqqan NOTO'G'RI kalit");
    }

    [Fact]
    public void Riasec_TypesKalitlariBittaHarfliHollandMnemonikasi()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(SampleResults(), ApiOptions));

        var types = document.RootElement.GetProperty("RIASEC").GetProperty("types");

        types.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["R", "I", "A", "S", "E", "C"],
            "`docs/03` §4.1 va `docs/07` 3.2: `types` kalitlari Holland harflari, bazadagi `Scale` kodlari (`ART`/`SOC`/`ENT`/`CONV`) EMAS");
    }

    [Fact]
    public void Mbti16Big5Activity_IchkiLugatKalitlariShkalaKodlari()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(SampleResults(), ApiOptions));

        document.RootElement.GetProperty("MBTI16").GetProperty("axes")
            .EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["EI", "SN", "TF", "JP"]);

        document.RootElement.GetProperty("BIG5").GetProperty("factors")
            .EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["O", "C", "E", "A", "N"]);

        document.RootElement.GetProperty("ACTIVITY").GetProperty("scales")
            .EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["MOT", "SELF", "SOCA", "ENG"]);
    }
}
