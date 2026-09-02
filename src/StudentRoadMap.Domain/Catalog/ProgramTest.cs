using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Dastur ichidagi bitta anketa biriktiruvi — `(ProgramId, TestDefinitionId, DisplayOrder)`
/// (`docs/06` 8-bo'lim, `prompts/34` A2-band). `AssessmentProgram` agregati tarkibida yashaydi.
/// </summary>
public sealed class ProgramTest : Entity
{
    public Guid ProgramId { get; private set; }

    public Guid TestDefinitionId { get; private set; }

    public int DisplayOrder { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private ProgramTest()
    {
    }

    private ProgramTest(Guid id, Guid programId, Guid testDefinitionId, int displayOrder)
        : base(id)
    {
        ProgramId = programId;
        TestDefinitionId = testDefinitionId;
        DisplayOrder = displayOrder;
    }

    public static ProgramTest Create(Guid id, Guid programId, Guid testDefinitionId, int displayOrder) =>
        new(id, programId, testDefinitionId, displayOrder);

    public void UpdateOrder(int displayOrder) => DisplayOrder = displayOrder;
}
