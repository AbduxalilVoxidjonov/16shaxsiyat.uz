using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai;

namespace StudentRoadMap.Infrastructure.Tests.Ai;

/// <summary>
/// `MockAiProvider` — DoD talabi: "`MockAiProvider` bilan to'liq tahlil oqimi ishlaydi", ya'ni
/// uning javobi `AiResponseValidator`ning barcha 5 bosqichidan `Ok` bilan o'tishi kerak.
/// </summary>
public sealed class MockAiProviderTests
{
    [Fact]
    public async Task CompleteJsonAsync_ReturnsSuccessWithNonEmptyJson()
    {
        var provider = new MockAiProvider();
        using var schemaDocument = JsonDocument.Parse(AnalysisJsonSchema.RawJson);
        var request = new AiCompletionRequest("system", "user", schemaDocument, "mock-model", 4096, 0.4);

        var result = await provider.CompleteJsonAsync(request, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorKind.Should().Be(AiErrorKind.None);
        result.RawJson.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy()
    {
        var provider = new MockAiProvider();

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task FullFlow_MockProviderResponse_PassesAllFiveValidationStages()
    {
        var provider = new MockAiProvider();
        using var schemaDocument = JsonDocument.Parse(AnalysisJsonSchema.RawJson);
        var request = new AiCompletionRequest("system", "user", schemaDocument, "mock-model", 4096, 0.4);

        var completion = await provider.CompleteJsonAsync(request, CancellationToken.None);

        var validator = new AiResponseValidator(new ConfigurationBuilder().Build());

        // O'quvchining (soxta) shaxsiy ma'lumot izlari — `MockAiProvider` namunasida bular
        // hech qachon bo'lmasligi kerak.
        var piiTokens = new[] { "Aliyev", "Sardorbek", "+998901234567" };

        var validation = validator.Validate(completion.RawJson, piiTokens, attemptNumber: 1);

        validation.Outcome.Should().Be(ValidationOutcome.Ok, validation.Message);
        validation.ParsedJson.Should().NotBeNull();
    }
}
