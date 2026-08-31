namespace StudentRoadMap.Domain.Ai;

/// <summary>AI tahlil holati — qiymatlar `docs/05-database-schema.md` 3-bo'limiga mos.</summary>
public enum AiAnalysisStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
}
