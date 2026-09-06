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
    public void Publish_SetsProgramActive()
    {
        var program = CreateCustomProgram();
        program.AddTest(Guid.NewGuid(), 1, Now);

        program.Publish(Now);

        program.IsActive.Should().BeTrue();
        program.State.Should().Be(ProgramState.Active);
    }

    [Fact]
    public void Deactivate_ThenActivate_OnPublishedProgram_TogglesBetweenActiveAndPaused()
    {
        var program = PublishedProgram();

        program.Deactivate(Now);
        program.IsActive.Should().BeFalse();
        program.State.Should().Be(ProgramState.Paused);

        program.Activate(Now);
        program.IsActive.Should().BeTrue();
        program.State.Should().Be(ProgramState.Active);
    }

    // ── Holat o'tishlari: `Activate`/`Deactivate` faqat `Published` doirasida ────────────
    // 2026-09-06: ilgari `Activate()` holatni UMUMAN tekshirmasdi va arxivlangan dasturni
    // faollashtirib, "Arxiv + Faol" ziddiyatini hosil qilish mumkin edi (egasining bazasida
    // aynan shunday qator bor edi).

    [Fact]
    public void Activate_ArchivedProgram_ThrowsDomainException()
    {
        var program = PublishedProgram();
        program.Archive(Now);

        var act = () => program.Activate(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
        program.IsActive.Should().BeFalse();
        program.State.Should().Be(ProgramState.Archived);
    }

    [Fact]
    public void Deactivate_ArchivedProgram_ThrowsDomainException()
    {
        var program = PublishedProgram();
        program.Archive(Now);

        var act = () => program.Deactivate(Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
    }

    [Fact]
    public void Activate_DraftProgram_ThrowsDomainException()
    {
        var program = CreateCustomProgram();

        var act = () => program.Activate(Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
        program.State.Should().Be(ProgramState.Draft);
    }

    [Fact]
    public void Deactivate_DraftProgram_ThrowsDomainException()
    {
        var program = CreateCustomProgram();

        var act = () => program.Deactivate(Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
        program.State.Should().Be(ProgramState.Draft);
    }

    // ── Arxivdan tiklash: `Archived ──Restore()──▶ Paused` (2026-09-06) ─────────────────
    // Egasining asosiy dasturi (`PERSONALITY_PROFILE`) arxivda qolib ketgan edi va uni faqat
    // nusxa olib "tiklash" mumkin edi. Tiklash `Paused` ga qaytaradi, `Active` ga EMAS:
    // `Archive()` maktab biriktirishlarini saqlab qoladi, shu sabab bir bosishda `Active`
    // dasturni o'sha maktablar uchun darhol jonli qilib qo'ygan bo'lardi.

    [Fact]
    public void Restore_ArchivedProgram_BecomesPausedNotActive()
    {
        var program = PublishedProgram();
        program.Archive(Now);

        var later = Now.AddDays(1);
        program.Restore(later);

        program.Status.Should().Be(ProgramStatus.Published);
        program.IsActive.Should().BeFalse();
        program.State.Should().Be(ProgramState.Paused);
        program.UpdatedAt.Should().Be(later);
    }

    /// <summary>Tiklash tarkibni (testlarni) yo'qotmaydi — arxivda ular saqlangan edi.</summary>
    [Fact]
    public void Restore_ArchivedProgram_KeepsTests()
    {
        var program = PublishedProgram();
        var testDefinitionId = program.Tests.Single().TestDefinitionId;
        program.Archive(Now);

        program.Restore(Now);

        program.Tests.Should().ContainSingle(t => t.TestDefinitionId == testDefinitionId);
    }

    /// <summary>Tiklash → faollashtirish — ikki ALOHIDA qaror; ikkinchisi birinchisidan keyin ishlashi shart.</summary>
    [Fact]
    public void Restore_ThenActivate_MakesProgramActiveAgain()
    {
        var program = PublishedProgram();
        program.Archive(Now);
        program.Restore(Now);

        program.Activate(Now);

        program.State.Should().Be(ProgramState.Active);
        program.IsActive.Should().BeTrue();
    }

    /// <summary>Tiklangan dasturni yana arxivlash mumkin — sikl yopiq.</summary>
    [Fact]
    public void Restore_ThenArchive_Succeeds()
    {
        var program = PublishedProgram();
        program.Archive(Now);
        program.Restore(Now);

        program.Archive(Now);

        program.State.Should().Be(ProgramState.Archived);
    }

    [Fact]
    public void Restore_DraftProgram_ThrowsDomainException()
    {
        var program = CreateCustomProgram();

        var act = () => program.Restore(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
        program.State.Should().Be(ProgramState.Draft);
    }

    [Fact]
    public void Restore_ActiveProgram_ThrowsDomainException()
    {
        var program = PublishedProgram();

        var act = () => program.Restore(Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
        program.State.Should().Be(ProgramState.Active);
    }

    [Fact]
    public void Restore_PausedProgram_ThrowsDomainException()
    {
        var program = PublishedProgram();
        program.Deactivate(Now);

        var act = () => program.Restore(Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("PROGRAM_INVALID_TRANSITION");
        program.State.Should().Be(ProgramState.Paused);
    }

    /// <summary>
    /// Eski ziddiyatli qator (`Archived + IsActive = true`) tiklansa ham natija `Paused`:
    /// `Restore()` `IsActive`ni ANIQ `false` qiladi, eski bayroqqa tayanmaydi.
    /// </summary>
    [Fact]
    public void Restore_ArchivedButFlaggedActive_StillBecomesPaused()
    {
        var program = PublishedProgram();
        program.Archive(Now);
        ForceIsActive(program, true);

        program.Restore(Now);

        program.State.Should().Be(ProgramState.Paused);
        program.IsActive.Should().BeFalse();
    }

    // ── Hosila holatning TO'RT kombinatsiyasi ───────────────────────────────────────────

    [Fact]
    public void State_DraftProgram_IsDraft()
    {
        CreateCustomProgram().State.Should().Be(ProgramState.Draft);
    }

    [Fact]
    public void State_PublishedAndActive_IsActive()
    {
        PublishedProgram().State.Should().Be(ProgramState.Active);
    }

    [Fact]
    public void State_PublishedAndNotActive_IsPaused()
    {
        var program = PublishedProgram();
        program.Deactivate(Now);

        program.State.Should().Be(ProgramState.Paused);
    }

    [Fact]
    public void State_ArchivedProgram_IsArchived()
    {
        var program = PublishedProgram();
        program.Archive(Now);

        program.State.Should().Be(ProgramState.Archived);
    }

    /// <summary>
    /// Bazada qolib ketishi mumkin bo'lgan ESKI ziddiyatli qator (`status = 3 AND
    /// is_active = true`) — domen endi bunday holatni hosil qila olmaydi, lekin egasining
    /// bazasida u haqiqatda bor edi. UI yolg'on aytmasligi kerak: `Status` ustuvor.
    /// </summary>
    [Fact]
    public void State_ArchivedButFlaggedActive_StillArchived()
    {
        var program = PublishedProgram();
        program.Archive(Now);
        ForceIsActive(program, true);

        program.State.Should().Be(ProgramState.Archived);
    }

    private static AssessmentProgram PublishedProgram()
    {
        var program = CreateCustomProgram();
        program.AddTest(Guid.NewGuid(), 1, Now);
        program.Publish(Now);
        return program;
    }

    /// <summary>
    /// Domen orqali hosil qilib bo'lmaydigan (lekin bazada uchraydigan) qatorni taqlid
    /// qilish — faqat shu test uchun. Ishlab chiqarish kodida bunday yo'l YO'Q.
    /// </summary>
    private static void ForceIsActive(AssessmentProgram program, bool value) =>
        typeof(AssessmentProgram)
            .GetProperty(nameof(AssessmentProgram.IsActive))!
            .SetValue(program, value);
}
