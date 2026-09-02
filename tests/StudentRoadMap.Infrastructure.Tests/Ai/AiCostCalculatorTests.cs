using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai;

namespace StudentRoadMap.Infrastructure.Tests.Ai;

/// <summary>
/// `AiCostCalculator` — `docs/09-ai-analiz-moduli.md` 9-bo'lim: `cost = inputTokens/1e6 ×
/// inputPer1M + outputTokens/1e6 × outputPer1M`, narx `Ai:Pricing:{Provider}:{Model}`dan
/// (`prompts/17` vazifa #4).
/// </summary>
public sealed class AiCostCalculatorTests
{
    private static AiCostCalculator BuildCalculator(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();
        return new AiCostCalculator(configuration);
    }

    [Fact]
    public void EstimateCostUsd_PricingConfigured_ComputesExpectedCost()
    {
        var calculator = BuildCalculator(
            ("Ai:Pricing:Gemini:gemini-2.0-flash:inputPer1M", "0.10"),
            ("Ai:Pricing:Gemini:gemini-2.0-flash:outputPer1M", "0.40"));

        var cost = calculator.EstimateCostUsd(AiProvider.Gemini, "gemini-2.0-flash", inputTokens: 2_000_000, outputTokens: 3_000_000);

        // 2 × 0.10 + 3 × 0.40 = 0.20 + 1.20 = 1.40
        cost.Should().Be(1.40m);
    }

    [Fact]
    public void EstimateCostUsd_PricingMissing_ReturnsNull_NotZero()
    {
        var calculator = BuildCalculator();

        var cost = calculator.EstimateCostUsd(AiProvider.OpenAi, "gpt-unknown", inputTokens: 1000, outputTokens: 1000);

        cost.Should().BeNull();
    }

    [Fact]
    public void EstimateCostUsd_NullTokens_ReturnsNull()
    {
        var calculator = BuildCalculator(
            ("Ai:Pricing:Anthropic:claude-sonnet-5:inputPer1M", "3.00"),
            ("Ai:Pricing:Anthropic:claude-sonnet-5:outputPer1M", "15.00"));

        calculator.EstimateCostUsd(AiProvider.Anthropic, "claude-sonnet-5", inputTokens: null, outputTokens: 500).Should().BeNull();
        calculator.EstimateCostUsd(AiProvider.Anthropic, "claude-sonnet-5", inputTokens: 500, outputTokens: null).Should().BeNull();
    }

    [Fact]
    public void EstimateCostUsd_DifferentModelSameProvider_UsesModelSpecificPricing()
    {
        var calculator = BuildCalculator(
            ("Ai:Pricing:OpenAi:gpt-4.1-mini:inputPer1M", "0.40"),
            ("Ai:Pricing:OpenAi:gpt-4.1-mini:outputPer1M", "1.60"),
            ("Ai:Pricing:OpenAi:gpt-4.1:inputPer1M", "2.00"),
            ("Ai:Pricing:OpenAi:gpt-4.1:outputPer1M", "8.00"));

        var miniCost = calculator.EstimateCostUsd(AiProvider.OpenAi, "gpt-4.1-mini", 1_000_000, 1_000_000);
        var fullCost = calculator.EstimateCostUsd(AiProvider.OpenAi, "gpt-4.1", 1_000_000, 1_000_000);

        miniCost.Should().Be(2.00m);
        fullCost.Should().Be(10.00m);
    }
}
