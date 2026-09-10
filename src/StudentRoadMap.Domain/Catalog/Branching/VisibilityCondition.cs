namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// Bitta ko'rsatish sharti: `QuestionCode` (B-5 — savol ID emas, kod — jsonb import/eksport
/// aylanmasi buzilmasligi uchun) savolining javobi `Operator` bo'yicha `Values` bilan
/// solishtiriladi (`docs/18` §2.4).
/// </summary>
public sealed record VisibilityCondition(
    string QuestionCode,
    VisibilityOperator Operator,
    IReadOnlyList<int> Values)
{
    /// <summary>
    /// Qiymat cheklovlarini tekshiradi (`docs/18` §2.4): `Answered`/`NotAnswered` da `Values`
    /// bo'sh bo'lishi shart, qolgan operatorlarda kamida bitta, ko'pi bilan 50 ta qiymat.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(QuestionCode))
        {
            throw new ArgumentException("Shart bog'langan savol kodi bo'sh bo'lishi mumkin emas.", nameof(QuestionCode));
        }

        var values = Values ?? throw new ArgumentNullException(nameof(Values));

        var requiresEmptyValues = Operator is VisibilityOperator.Answered or VisibilityOperator.NotAnswered;

        if (requiresEmptyValues && values.Count > 0)
        {
            throw new ArgumentException(
                "'Answered'/'NotAnswered' operatorlarida qiymatlar ro'yxati bo'sh bo'lishi shart.",
                nameof(Values));
        }

        if (!requiresEmptyValues && values.Count == 0)
        {
            throw new ArgumentException(
                "Bu operator uchun kamida bitta qiymat ko'rsatilishi kerak.",
                nameof(Values));
        }

        if (values.Count > 50)
        {
            throw new ArgumentException("Qiymatlar soni 50 tadan oshmasligi kerak.", nameof(Values));
        }
    }
}
