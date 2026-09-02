using System.Globalization;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.Ai;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Ai;

/// <inheritdoc cref="IAiCostCalculator"/>
internal sealed class AiCostCalculator : IAiCostCalculator
{
    private readonly IConfiguration _configuration;

    public AiCostCalculator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public decimal? EstimateCostUsd(AiProvider provider, string model, int? inputTokens, int? outputTokens)
    {
        if (inputTokens is null || outputTokens is null || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var section = _configuration.GetSection($"Ai:Pricing:{provider}:{model}");
        var inputPer1MRaw = section["inputPer1M"];
        var outputPer1MRaw = section["outputPer1M"];

        if (!TryParseUsd(inputPer1MRaw, out var inputPer1M) || !TryParseUsd(outputPer1MRaw, out var outputPer1M))
        {
            // Narx konfiguratsiyada topilmadi — taxminiy xarajatni hisoblab bo'lmaydi, `0` emas
            // `null` qaytariladi (`docs/07` "null qoidasi", `IAiCostCalculator` izohi).
            return null;
        }

        var cost = (inputTokens.Value / 1_000_000m * inputPer1M) + (outputTokens.Value / 1_000_000m * outputPer1M);
        return Math.Round(cost, 6);
    }

    private static bool TryParseUsd(string? raw, out decimal value) =>
        decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
