using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>
/// `OpenAiSchemaAdapter` — PM ko'rsatmasi (`prompts/17` javob xati, 2026-09-02) talab #1(a):
/// "yuborilgan sxemada barcha kalit `required` va ixtiyoriylari `null` qabul qiladi".
/// Kanonik `AnalysisJsonSchema` (P16) ustida ishlaydi — o'sha fayl bu testda o'zgartirilmaydi.
/// </summary>
public sealed class OpenAiSchemaAdapterTests
{
    private static JsonElement Canonical() => JsonDocument.Parse(AnalysisJsonSchema.RawJson).RootElement;

    [Fact]
    public void ToStrictSchema_EveryObjectLevel_AllPropertyKeysAppearInRequired()
    {
        var strict = OpenAiSchemaAdapter.ToStrictSchema(Canonical());
        using var strictDoc = JsonDocument.Parse(strict.ToJsonString());

        AssertAllPropertiesRequired(strictDoc.RootElement);
    }

    private static void AssertAllPropertiesRequired(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (schema.TryGetProperty("properties", out var properties))
        {
            var propertyKeys = properties.EnumerateObject().Select(p => p.Name).ToList();
            schema.TryGetProperty("required", out var required).Should().BeTrue("`strict` rejimi har bir obyektda `required` massivini talab qiladi");
            var requiredKeys = required.EnumerateArray().Select(r => r.GetString()).ToList();

            requiredKeys.Should().BeEquivalentTo(propertyKeys, "OpenAI `strict: true` — BARCHA `properties` kalitlari `required`da bo'lishi shart");

            foreach (var property in properties.EnumerateObject())
            {
                AssertAllPropertiesRequired(property.Value);
            }
        }

        if (schema.TryGetProperty("items", out var items))
        {
            AssertAllPropertiesRequired(items);
        }
    }

    [Fact]
    public void ToStrictSchema_OriginallyOptionalTopLevelField_StaysNullable()
    {
        var strict = OpenAiSchemaAdapter.ToStrictSchema(Canonical());
        using var strictDoc = JsonDocument.Parse(strict.ToJsonString());

        var reliabilityNoteType = strictDoc.RootElement.GetProperty("properties").GetProperty("reliabilityNote").GetProperty("type");
        reliabilityNoteType.EnumerateArray().Select(t => t.GetString()).Should().Contain("null");
    }

    [Fact]
    public void ToStrictSchema_OriginallyOptionalNestedField_BecomesNullable()
    {
        var strict = OpenAiSchemaAdapter.ToStrictSchema(Canonical());
        using var strictDoc = JsonDocument.Parse(strict.ToJsonString());

        // `careerSuggestions[].exampleProfessions` kanonik sxemada `required`da YO'Q edi.
        var exampleProfessionsType = strictDoc.RootElement
            .GetProperty("properties").GetProperty("careerSuggestions")
            .GetProperty("items").GetProperty("properties").GetProperty("exampleProfessions")
            .GetProperty("type");

        exampleProfessionsType.ValueKind.Should().Be(JsonValueKind.Array);
        exampleProfessionsType.EnumerateArray().Select(t => t.GetString()).Should().Contain(new[] { "array", "null" });
    }

    [Fact]
    public void ToStrictSchema_OriginallyRequiredField_TypeUnchanged_NoNullAdded()
    {
        var strict = OpenAiSchemaAdapter.ToStrictSchema(Canonical());
        using var strictDoc = JsonDocument.Parse(strict.ToJsonString());

        var summaryType = strictDoc.RootElement.GetProperty("properties").GetProperty("summary").GetProperty("type");
        summaryType.ValueKind.Should().Be(JsonValueKind.String);
        summaryType.GetString().Should().Be("string");
    }

    [Fact]
    public void ToStrictSchema_PreservesAdditionalPropertiesFalseAndSizeConstraints()
    {
        var strict = OpenAiSchemaAdapter.ToStrictSchema(Canonical());
        using var strictDoc = JsonDocument.Parse(strict.ToJsonString());

        strictDoc.RootElement.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        strictDoc.RootElement.GetProperty("properties").GetProperty("summary").GetProperty("minLength").GetInt32().Should().Be(80);
        strictDoc.RootElement.GetProperty("properties").GetProperty("strengths").GetProperty("minItems").GetInt32().Should().Be(4);

        var strengthsItem = strictDoc.RootElement.GetProperty("properties").GetProperty("strengths").GetProperty("items");
        strengthsItem.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
    }
}
