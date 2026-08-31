using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// `TestDefinition.ScoringStrategyCode` bo'yicha mos <see cref="IScoringStrategy"/>ni topadi
/// va ishga tushiradi (`prompts/09-scoring-engine.md`). Strategiyalar DI orqali
/// (`IEnumerable&lt;IScoringStrategy&gt;`) uzatiladi — bu sinf o'zi hech qanday strategiya
/// yaratmaydi, faqat kod bo'yicha yo'naltiradi.
/// </summary>
public sealed class ScoringEngine
{
    private readonly IReadOnlyDictionary<string, IScoringStrategy> _strategiesByCode;

    public ScoringEngine(IEnumerable<IScoringStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);

        var byCode = new Dictionary<string, IScoringStrategy>(StringComparer.Ordinal);
        foreach (var strategy in strategies)
        {
            if (!byCode.TryAdd(strategy.StrategyCode, strategy))
            {
                // DI konfiguratsiyasida bitta kod ikki marta ro'yxatdan o'tishi — sukut bo'yicha
                // birini "yutqazish" o'rniga darhol aniq xato bilan to'xtatiladi.
                throw new DomainException(
                    "SCORING_STRATEGY_DUPLICATE",
                    $"'{strategy.StrategyCode}' kodi bilan bir nechta scoring strategiyasi ro'yxatdan o'tgan.");
            }
        }

        _strategiesByCode = byCode;
    }

    /// <summary>Berilgan strategiya kodi bilan hisoblaydi. Strategiya topilmasa aniq xato qaytaradi.</summary>
    public Result<ScoringResult> Score(string strategyCode, ScoringInput input)
    {
        if (string.IsNullOrWhiteSpace(strategyCode))
        {
            return Result.Failure<ScoringResult>(new Error("SCORING_STRATEGY_CODE_EMPTY", "Scoring strategiyasi kodi bo'sh bo'lishi mumkin emas."));
        }

        if (!_strategiesByCode.TryGetValue(strategyCode, out var strategy))
        {
            return Result.Failure<ScoringResult>(new Error("SCORING_STRATEGY_NOT_FOUND", $"'{strategyCode}' uchun scoring strategiyasi ro'yxatdan o'tmagan."));
        }

        return Result.Success(strategy.Score(input));
    }
}
