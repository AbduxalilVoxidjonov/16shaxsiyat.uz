using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>
/// Test dasturi (`AssessmentProgram.CreateForTest`, 2026-09-23 egasi qarori, `docs/18` §9.7):
/// 1:1 tarkib, nom sinxroni va nashr holatining testga ergashishi.
/// </summary>
public sealed class TestProgramTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    private static AssessmentProgram CreateTestProgram(Guid? testId = null) =>
        AssessmentProgram.CreateForTest(Guid.NewGuid(), testId ?? Guid.NewGuid(), "SURVEY-1", "So'rovnoma", "Tavsif", 3, Now);

    [Fact]
    public void CreateForTest_BittaTestli_DraftVaAssigned()
    {
        var testId = Guid.NewGuid();

        var program = CreateTestProgram(testId);

        program.IsTestProgram.Should().BeTrue();
        program.OwnerTestDefinitionId.Should().Be(testId);
        program.Tests.Should().ContainSingle(t => t.TestDefinitionId == testId && t.DisplayOrder == 1);
        program.State.Should().Be(ProgramState.Draft);
        program.Visibility.Should().Be(ProgramVisibility.Assigned);
        program.RegistrationMode.Should().Be(RegistrationMode.Full);
        program.Kind.Should().Be(ProgramKind.Custom);
        program.IsSystem.Should().BeFalse();
        program.NameUz.Should().Be("So'rovnoma");
        program.DisplayOrder.Should().Be(3);
    }

    [Fact]
    public void CreateForTest_BoshTestId_Rad()
    {
        var act = () => AssessmentProgram.CreateForTest(Guid.NewGuid(), Guid.Empty, "X", "X", null, 1, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TestDasturi_TarkibiniOzgartiribBolmaydi()
    {
        var program = CreateTestProgram();

        var add = () => program.AddTest(Guid.NewGuid(), 2, isPersonalityBatteryTest: false, Now);
        var remove = () => program.RemoveTest(program.Tests.Single().TestDefinitionId, Now);

        add.Should().Throw<DomainException>().Which.Code.Should().Be("TEST_PROGRAM_LOCKED");
        remove.Should().Throw<DomainException>().Which.Code.Should().Be("TEST_PROGRAM_LOCKED");
    }

    [Fact]
    public void SyncDetailsFromTest_NomKodTavsifTartibniKochiradi()
    {
        var program = CreateTestProgram();

        var changed = program.SyncDetailsFromTest("SURVEY-1", "Yangi nom", null, 9, Now.AddMinutes(1));

        changed.Should().BeTrue();
        program.NameUz.Should().Be("Yangi nom");
        program.DescriptionUz.Should().BeNull();
        program.DisplayOrder.Should().Be(9);
        program.UpdatedAt.Should().Be(Now.AddMinutes(1));

        program.SyncDetailsFromTest("SURVEY-1", "Yangi nom", null, 9, Now.AddMinutes(2)).Should().BeFalse("o'zgarish yo'q");
        program.UpdatedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void SyncDetailsFromTest_OddiyDasturda_Rad()
    {
        var program = AssessmentProgram.Create(Guid.NewGuid(), "LEGACY", "Eski", Now);

        var act = () => program.SyncDetailsFromTest("X", "Y", null, 1, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("TEST_PROGRAM_REQUIRED");
    }

    [Theory]
    [InlineData(TestDefinitionStatus.Draft, true, ProgramState.Draft)]
    [InlineData(TestDefinitionStatus.Published, true, ProgramState.Active)]
    [InlineData(TestDefinitionStatus.Published, false, ProgramState.Paused)]
    [InlineData(TestDefinitionStatus.Archived, false, ProgramState.Archived)]
    public void SyncStateWithTest_DraftDasturdan(TestDefinitionStatus testStatus, bool testIsActive, ProgramState expected)
    {
        var program = CreateTestProgram();

        program.SyncStateWithTest(testStatus, testIsActive, hasPersonalityBattery: false, Now);

        program.State.Should().Be(expected);
    }

    [Fact]
    public void SyncStateWithTest_ToLiqHayotiySikl()
    {
        var program = CreateTestProgram();

        program.SyncStateWithTest(TestDefinitionStatus.Published, true, false, Now).Should().BeTrue();
        program.State.Should().Be(ProgramState.Active);

        program.SyncStateWithTest(TestDefinitionStatus.Published, true, false, Now).Should().BeFalse("holat allaqachon mos");

        program.SyncStateWithTest(TestDefinitionStatus.Published, false, false, Now);
        program.State.Should().Be(ProgramState.Paused);

        program.SyncStateWithTest(TestDefinitionStatus.Published, true, false, Now);
        program.State.Should().Be(ProgramState.Active);

        program.SyncStateWithTest(TestDefinitionStatus.Archived, false, false, Now);
        program.State.Should().Be(ProgramState.Archived);

        // Arxivdan qayta nashr (masalan test tiklansa) — `Restore` + `Activate`.
        program.SyncStateWithTest(TestDefinitionStatus.Published, true, false, Now);
        program.State.Should().Be(ProgramState.Active);
    }

    [Fact]
    public void SyncStateWithTest_BatareyaliTestdaRoYxatdanOtishsiz_NashrRad()
    {
        var program = CreateTestProgram();
        program.SetRegistrationMode(RegistrationMode.None, hasPersonalityBattery: false, Now);

        var act = () => program.SyncStateWithTest(TestDefinitionStatus.Published, true, hasPersonalityBattery: true, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("REGISTRATION_REQUIRED_FOR_BATTERY");
    }

    [Fact]
    public void SetPublic_VisibilityniAlmashtiradi()
    {
        var program = CreateTestProgram();

        program.SetPublic(true, Now).Should().BeTrue();
        program.Visibility.Should().Be(ProgramVisibility.Public);
        program.SetPublic(true, Now).Should().BeFalse();
        program.SetPublic(false, Now).Should().BeTrue();
        program.Visibility.Should().Be(ProgramVisibility.Assigned);
    }
}
