using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>
/// `AssessmentProgram` — dastur tarkibi, nashr oqimi va tizim dasturi qulfi
/// (`docs/06` 8-bo'lim, `prompts/34` A1-band).
/// </summary>
public sealed class AssessmentProgramTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

    private static AssessmentProgram CreateCustomProgram() =>
        AssessmentProgram.Create(Guid.NewGuid(), "CUSTOM-PROG", "Maxsus dastur", Now);

    [Fact]
    public void AddTest_ToCustomProgram_Succeeds()
    {
        var program = CreateCustomProgram();
        var testDefinitionId = Guid.NewGuid();

        program.AddTest(testDefinitionId, 1, Now);

        program.Tests.Should().ContainSingle(t => t.TestDefinitionId == testDefinitionId && t.DisplayOrder == 1);
    }

    [Fact]
    public void AddTest_DuplicateTestDefinition_ThrowsDomainException()
    {
        var program = CreateCustomProgram();
        var testDefinitionId = Guid.NewGuid();
        program.AddTest(testDefinitionId, 1, Now);

        var act = () => program.AddTest(testDefinitionId, 2, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("PROGRAM_TEST_DUPLICATE");
    }

    [Fact]
    public void RemoveTest_FromCustomProgram_Succeeds()
    {
        var program = CreateCustomProgram();
        var testDefinitionId = Guid.NewGuid();
        program.AddTest(testDefinitionId, 1, Now);

        program.RemoveTest(testDefinitionId, Now);

        program.Tests.Should().BeEmpty();
    }

    [Fact]
    public void Publish_WithNoTests_ThrowsDomainException()
    {
        var program = CreateCustomProgram();

        var act = () => program.Publish(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("PROGRAM_NOT_PUBLISHABLE");
    }

    [Fact]
    public void Publish_WithAtLeastOneTest_Succeeds()
    {
        var program = CreateCustomProgram();
        program.AddTest(Guid.NewGuid(), 1, Now);

        program.Publish(Now);

        program.Status.Should().Be(ProgramStatus.Published);
    }

    [Fact]
    public void Publish_AlreadyPublished_ThrowsDomainException()
    {
        var program = CreateCustomProgram();
        program.AddTest(Guid.NewGuid(), 1, Now);
        program.Publish(Now);

        var act = () => program.Publish(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
    }

    [Fact]
    public void Archive_DraftProgram_SetsArchivedAndInactive()
    {
        var program = CreateCustomProgram();

        program.Archive(Now);

        program.Status.Should().Be(ProgramStatus.Archived);
        program.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ReorderTests_WithMatchingSet_UpdatesDisplayOrder()
    {
        var program = CreateCustomProgram();
        var testId1 = Guid.NewGuid();
        var testId2 = Guid.NewGuid();
        program.AddTest(testId1, 1, Now);
        program.AddTest(testId2, 2, Now);

        program.ReorderTests([testId2, testId1], Now);

        program.Tests.Single(t => t.TestDefinitionId == testId2).DisplayOrder.Should().Be(1);
        program.Tests.Single(t => t.TestDefinitionId == testId1).DisplayOrder.Should().Be(2);
    }

    [Fact]
    public void ReorderTests_WithDifferentCount_ThrowsDomainException()
    {
        var program = CreateCustomProgram();
        program.AddTest(Guid.NewGuid(), 1, Now);

        var act = () => program.ReorderTests([Guid.NewGuid(), Guid.NewGuid()], Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("PROGRAM_TEST_REORDER_MISMATCH");
    }

    [Fact]
    public void ReorderTests_WithUnknownTestDefinitionId_ThrowsDomainException()
    {
        var program = CreateCustomProgram();
        program.AddTest(Guid.NewGuid(), 1, Now);

        var act = () => program.ReorderTests([Guid.NewGuid()], Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("PROGRAM_TEST_NOT_FOUND");
    }

    [Fact]
    public void CreateSystemPublished_ProducesLockedPublishedProgram()
    {
        var testDefinitionId1 = Guid.NewGuid();
        var testDefinitionId2 = Guid.NewGuid();

        var program = AssessmentProgram.CreateSystemPublished(
            Guid.NewGuid(),
            "PERSONALITY_PROFILE",
            "Shaxsiyat profili",
            descriptionUz: null,
            displayOrder: 1,
            tests: [(testDefinitionId1, 1), (testDefinitionId2, 2)],
            now: Now);

        program.IsSystem.Should().BeTrue();
        program.Kind.Should().Be(ProgramKind.System);
        program.Visibility.Should().Be(ProgramVisibility.Public);
        program.Status.Should().Be(ProgramStatus.Published);
        program.Tests.Should().HaveCount(2);
    }

    [Fact]
    public void AddTest_ToSystemProgram_ThrowsDomainException()
    {
        var program = AssessmentProgram.CreateSystemPublished(
            Guid.NewGuid(), "PERSONALITY_PROFILE", "Shaxsiyat profili", null, 1, [(Guid.NewGuid(), 1)], Now);

        var act = () => program.AddTest(Guid.NewGuid(), 2, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_PROGRAM_LOCKED");
    }

    [Fact]
    public void RemoveTest_FromSystemProgram_ThrowsDomainException()
    {
        var testDefinitionId = Guid.NewGuid();
        var program = AssessmentProgram.CreateSystemPublished(
            Guid.NewGuid(), "PERSONALITY_PROFILE", "Shaxsiyat profili", null, 1, [(testDefinitionId, 1)], Now);

        var act = () => program.RemoveTest(testDefinitionId, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_PROGRAM_LOCKED");
    }

    [Fact]
    public void ReorderTests_OnSystemProgram_ThrowsDomainException()
    {
        var testDefinitionId = Guid.NewGuid();
        var program = AssessmentProgram.CreateSystemPublished(
            Guid.NewGuid(), "PERSONALITY_PROFILE", "Shaxsiyat profili", null, 1, [(testDefinitionId, 1)], Now);

        var act = () => program.ReorderTests([testDefinitionId], Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_PROGRAM_LOCKED");
    }

    [Fact]
    public void SetVisibility_UpdatesVisibilityAndTimestamp()
    {
        var program = CreateCustomProgram();

        program.SetVisibility(ProgramVisibility.Public, Now);

        program.Visibility.Should().Be(ProgramVisibility.Public);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var program = CreateCustomProgram();

        program.Deactivate(Now);
        program.IsActive.Should().BeFalse();

        program.Activate(Now);
        program.IsActive.Should().BeTrue();
    }
}
