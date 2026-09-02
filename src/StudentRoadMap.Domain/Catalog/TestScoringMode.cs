namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// `TestDefinition.ScoringMode` — `docs/06-arxitektura.md` 8-bo'lim (2026-09-02 qaror,
/// loyiha egasi: "Ikkalasi ham kerak"). `Scored` — ballanadigan anketa (`ScoringStrategyCode`
/// majburiy). `Survey` — oddiy so'rovnoma: javoblar saqlanadi, ammo `TestResult` ball
/// yozilmaydi, `ReliabilityCalculator` kirishiga kirmaydi, `CompositeScorer`/AI xulosasiga
/// ta'sir qilmaydi (`prompts/34` D-band).
/// </summary>
public enum TestScoringMode
{
    Scored = 1,
    Survey = 2,
}
