namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// `VisibilityRule.Conditions` orasidagi mantiqiy bog'lovchi (`docs/18` §2.4): `All` — barcha
/// shart bajarilishi kerak, `Any` — kamida bittasi yetarli.
/// </summary>
public enum VisibilityMatch
{
    All = 1,
    Any = 2,
}
