namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>`VisibleQuestionResolver.Resolve` natijasi — `docs/18` §2.6.</summary>
public sealed class VisibilityMap
{
    public IReadOnlySet<Guid> VisibleQuestionIds { get; }

    public IReadOnlySet<Guid> VisibleSectionIds { get; }

    public VisibilityMap(IReadOnlySet<Guid> visibleQuestionIds, IReadOnlySet<Guid> visibleSectionIds)
    {
        VisibleQuestionIds = visibleQuestionIds;
        VisibleSectionIds = visibleSectionIds;
    }
}
