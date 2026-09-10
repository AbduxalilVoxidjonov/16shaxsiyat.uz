namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// Savol yoki bo'limning ko'rsatish sharti (`docs/18` §2.4). `null` — shartsiz, har doim
/// ko'rinadi. `jsonb` saqlash shakli — `VisibilityRuleJson`.
/// </summary>
public sealed record VisibilityRule(
    VisibilityMatch Match,
    IReadOnlyList<VisibilityCondition> Conditions)
{
    /// <summary>`Conditions` 1..10 ta bo'lishi va har biri o'z ichida to'g'ri bo'lishi shart (`docs/18` §2.4).</summary>
    public void Validate()
    {
        var conditions = Conditions ?? throw new ArgumentNullException(nameof(Conditions));

        if (conditions.Count is < 1 or > 10)
        {
            throw new ArgumentException("Shartlar soni 1 dan 10 gacha bo'lishi kerak.", nameof(Conditions));
        }

        foreach (var condition in conditions)
        {
            condition.Validate();
        }
    }
}
