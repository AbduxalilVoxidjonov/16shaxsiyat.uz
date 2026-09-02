using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>
/// `OpenAiResponseNormalizer` — PM ko'rsatmasi (`prompts/17` javob xati, 2026-09-02) talab
/// #1(b): "null qaytgan ixtiyoriy maydon normallashtirilgach kanonik validatordan o'tadi".
/// Oxirgi ikki test HAQIQIY `AiResponseValidator` + `AnalysisJsonSchema.Default` (P16, bu
/// yerda o'zgartirilmagan) orqali to'liq zanjirni sinaydi.
/// </summary>
public sealed class OpenAiResponseNormalizerTests
{
    private static JsonElement Canonical() => JsonDocument.Parse(AnalysisJsonSchema.RawJson).RootElement;

    [Fact]
    public void Normalize_TopLevelNullOptionalField_IsRemoved()
    {
        var response = """{"summary":"ok","reliabilityNote":null}""";

        var normalized = OpenAiResponseNormalizer.Normalize(response, Canonical());

        using var doc = JsonDocument.Parse(normalized);
        doc.RootElement.TryGetProperty("reliabilityNote", out _).Should().BeFalse();
        doc.RootElement.GetProperty("summary").GetString().Should().Be("ok");
    }

    [Fact]
    public void Normalize_NestedNullOptionalField_IsRemoved()
    {
        var response = """{"careerSuggestions":[{"field":"IT","why":"...","exampleProfessions":null,"nextSteps":["a","b"]}]}""";

        var normalized = OpenAiResponseNormalizer.Normalize(response, Canonical());

        using var doc = JsonDocument.Parse(normalized);
        var item = doc.RootElement.GetProperty("careerSuggestions")[0];
        item.TryGetProperty("exampleProfessions", out _).Should().BeFalse();
        item.GetProperty("field").GetString().Should().Be("IT");
    }

    [Fact]
    public void Normalize_NonNullOptionalField_IsKept()
    {
        var response = """{"reliabilityNote":"Natija ehtiyotkorlik bilan o'qilsin."}""";

        var normalized = OpenAiResponseNormalizer.Normalize(response, Canonical());

        using var doc = JsonDocument.Parse(normalized);
        doc.RootElement.GetProperty("reliabilityNote").GetString().Should().Be("Natija ehtiyotkorlik bilan o'qilsin.");
    }

    [Fact]
    public void Normalize_NullRequiredField_IsNotRemoved()
    {
        // `summary` KANONIK sxemada `required` — model `strict` shartni buzib `null` qaytarsa,
        // bu QASDDAN olib tashlanmaydi: validator buni HAQIQIY xato sifatida ko'rishi kerak.
        var response = """{"summary":null}""";

        var normalized = OpenAiResponseNormalizer.Normalize(response, Canonical());

        using var doc = JsonDocument.Parse(normalized);
        doc.RootElement.GetProperty("summary").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static string Filler(int minLength)
    {
        const string chunk = "Bu namuna matni faqat sxema uzunlik talabini qondirish uchun yozilgan, mazmuni ahamiyatsiz. ";
        var builder = new StringBuilder();
        while (builder.Length < minLength)
        {
            builder.Append(chunk);
        }

        return builder.ToString();
    }

    private static JsonObject StrengthItem(int i) => new()
    {
        ["title"] = $"Kuchli tomon {i}",
        ["description"] = "Tavsif matni.",
        ["evidence"] = "Dalil matni.",
    };

    private static JsonObject GrowthItem(int i) => new()
    {
        ["title"] = $"O'sish zonasi {i}",
        ["description"] = "Tavsif matni.",
        ["actionStep"] = "Amaliy qadam.",
    };

    private static JsonObject CareerItem(int i, bool withExampleProfessions) => new()
    {
        ["field"] = $"Yo'nalish {i}",
        ["why"] = "Sabab matni.",
        ["exampleProfessions"] = withExampleProfessions ? new JsonArray("Kasb A", "Kasb B") : null,
        ["nextSteps"] = new JsonArray("Qadam 1", "Qadam 2"),
    };

    /// <summary>To'liq, kanonik sxemaga struktura jihatidan mos, LEKIN ikkita ixtiyoriy
    /// maydoni (`reliabilityNote`, birinchi `careerSuggestions[].exampleProfessions`) `null`
    /// bo'lgan (OpenAI `strict` javobiga o'xshash) namuna.</summary>
    private static JsonObject BuildFullAnalysisWithNullableOptionals()
    {
        return new JsonObject
        {
            ["summary"] = Filler(80),
            ["personalityPortrait"] = Filler(300),
            ["strengths"] = new JsonArray(StrengthItem(1), StrengthItem(2), StrengthItem(3), StrengthItem(4)),
            ["growthAreas"] = new JsonArray(GrowthItem(1), GrowthItem(2), GrowthItem(3)),
            ["learningStyle"] = "O'quv uslubi haqida matn.",
            ["motivationProfile"] = "Motivatsiya haqida matn.",
            ["activityAssessment"] = "Faollik haqida matn.",
            ["careerSuggestions"] = new JsonArray(
                CareerItem(1, withExampleProfessions: false),
                CareerItem(2, withExampleProfessions: true),
                CareerItem(3, withExampleProfessions: true)),
            ["studentRecommendations"] = new JsonArray("Tavsiya 1", "Tavsiya 2", "Tavsiya 3", "Tavsiya 4", "Tavsiya 5"),
            ["teacherNotes"] = new JsonArray("Eslatma 1", "Eslatma 2", "Eslatma 3"),
            ["parentNotes"] = new JsonArray("Eslatma 1", "Eslatma 2", "Eslatma 3"),
            ["attentionFlags"] = new JsonArray(),
            ["reliabilityNote"] = null,
            ["disclaimer"] = "Cheklov haqida qisqa matn.",
        };
    }

    [Fact]
    public void EndToEnd_RawStrictResponseWithNullOptionals_FailsCanonicalValidatorBeforeNormalization()
    {
        var raw = BuildFullAnalysisWithNullableOptionals().ToJsonString();
        var validator = new AiResponseValidator(new ConfigurationBuilder().Build());

        // Normallashtirmasdan validatorga uzatilsa — `exampleProfessions: null` kanonik
        // sxemaning `"type":"array"` (nullable emas) talabini buzadi va Schema bosqichida
        // Retry beradi (`docs/09` 6-bo'lim, 2-bosqich).
        var result = validator.Validate(raw, piiTokens: [], attemptNumber: 1);

        result.Outcome.Should().Be(ValidationOutcome.Retry);
        result.FailedStage.Should().Be(AiValidationStage.Schema);
    }

    [Fact]
    public void EndToEnd_NormalizedStrictResponse_PassesCanonicalValidator()
    {
        var raw = BuildFullAnalysisWithNullableOptionals().ToJsonString();
        var normalized = OpenAiResponseNormalizer.Normalize(raw, Canonical());
        var validator = new AiResponseValidator(new ConfigurationBuilder().Build());

        var result = validator.Validate(normalized, piiTokens: [], attemptNumber: 1);

        result.Outcome.Should().Be(ValidationOutcome.Ok, result.Message);
        result.ParsedJson.Should().NotBeNull();
    }
}
