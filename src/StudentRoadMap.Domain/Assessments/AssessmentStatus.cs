namespace StudentRoadMap.Domain.Assessments;

/// <summary>`Assessment` holat mashinasi qiymatlari — `docs/05-database-schema.md` 3-bo'limiga mos.</summary>
public enum AssessmentStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2,
    Analyzing = 3,
    Analyzed = 4,
    AnalysisFailed = 5,
    Abandoned = 6,
}
