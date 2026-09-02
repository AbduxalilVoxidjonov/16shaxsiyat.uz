using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>`TestScale` — `docs/03` §6.1, P37 (`prompts/37-katalog-crud-backend.md`).</summary>
public sealed class TestScaleTests
{
    [Fact]
    public void Create_WithEmptyCode_ThrowsArgumentException()
    {
        var act = () => TestScale.Create(Guid.NewGuid(), Guid.NewGuid(), " ", "Nomi", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => TestScale.Create(Guid.NewGuid(), Guid.NewGuid(), "STRESS", " ", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Default_HasNoInterpretationBands()
    {
        var scale = TestScale.Create(Guid.NewGuid(), Guid.NewGuid(), "STRESS", "Stress", 1);

        scale.InterpretationBands.Should().BeEmpty();
    }

    [Fact]
    public void UpdateMetadata_ChangesNameDescriptionAndOrder()
    {
        var scale = TestScale.Create(Guid.NewGuid(), Guid.NewGuid(), "STRESS", "Stress", 1);

        scale.UpdateMetadata("Yangi nom", "Tavsif", 2);

        scale.NameUz.Should().Be("Yangi nom");
        scale.DescriptionUz.Should().Be("Tavsif");
        scale.DisplayOrder.Should().Be(2);
    }

    [Fact]
    public void UpdateInterpretationBands_ReplacesBands()
    {
        var scale = TestScale.Create(Guid.NewGuid(), Guid.NewGuid(), "STRESS", "Stress", 1);
        var bands = new[]
        {
            new InterpretationBand(0, 33, "Past"),
            new InterpretationBand(34, 66, "O'rtacha"),
            new InterpretationBand(67, 100, "Yuqori"),
        };

        scale.UpdateInterpretationBands(bands);

        scale.InterpretationBands.Should().BeEquivalentTo(bands);
    }
}
