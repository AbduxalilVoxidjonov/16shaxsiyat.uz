using System.Reflection;
using FluentAssertions;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>
/// Hosila holat (`ProgramState`) — `Status` + `IsActive` juftligining YAGONA talqini.
///
/// Bu yerdagi asosiy test — <see cref="Filter_MatchesResolve_ForEveryStoredCombination"/>:
/// hisoblash (`Resolve`, xotirada) va filtr (`Filter`, DB darajasida) IKKI marta yozilishga
/// majbur (ifoda daraxtini hisoblanuvchi xossadan qurib bo'lmaydi), shu sabab ularning mos
/// kelishi test bilan QULFLANADI. Aks holda "Faol" filtri "To'xtatilgan" dasturni qaytarib
/// yuborishi mumkin edi va hech kim sezmasdi.
/// </summary>
public sealed class ProgramStateRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ProgramStatus.Draft, true, ProgramState.Draft)]
    [InlineData(ProgramStatus.Draft, false, ProgramState.Draft)]
    [InlineData(ProgramStatus.Published, true, ProgramState.Active)]
    [InlineData(ProgramStatus.Published, false, ProgramState.Paused)]
    [InlineData(ProgramStatus.Archived, false, ProgramState.Archived)]
    [InlineData(ProgramStatus.Archived, true, ProgramState.Archived)]
    public void Resolve_MapsEveryCombination(ProgramStatus status, bool isActive, ProgramState expected)
    {
        ProgramStateRules.Resolve(status, isActive).Should().Be(expected);
    }

    /// <summary>
    /// Bazada uchrashi mumkin bo'lgan HAR BIR (`Status`, `IsActive`) juftligi uchun:
    /// `Filter(state)` predikati aynan `Resolve(...) == state` bo'lgan qatorlarni tanlaydi.
    /// </summary>
    [Theory]
    [InlineData(ProgramState.Draft)]
    [InlineData(ProgramState.Active)]
    [InlineData(ProgramState.Paused)]
    [InlineData(ProgramState.Archived)]
    public void Filter_MatchesResolve_ForEveryStoredCombination(ProgramState state)
    {
        var rows = AllStoredCombinations();

        var selected = rows.AsQueryable().Where(ProgramStateRules.Filter(state)).ToList();
        var expected = rows.Where(p => ProgramStateRules.Resolve(p.Status, p.IsActive) == state).ToList();

        selected.Should().BeEquivalentTo(expected);
        selected.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Paused", ProgramState.Paused)]
    [InlineData("paused", ProgramState.Paused)]
    [InlineData("  ARCHIVED  ", ProgramState.Archived)]
    public void TryParse_AcceptsKnownValuesIgnoringCase(string value, ProgramState expected)
    {
        ProgramStateRules.TryParse(value, out var state).Should().BeTrue();
        state.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Published")] // eski `status` qiymati — endi qabul qilinmaydi
    [InlineData("2")]         // raqamli qiymat ATAYLAB rad etiladi (API shartnomasi — nom)
    public void TryParse_RejectsUnknownValues(string? value)
    {
        ProgramStateRules.TryParse(value, out _).Should().BeFalse();
    }

    /// <summary>
    /// Bazada saqlanishi MUMKIN bo'lgan 6 kombinatsiya. Ulardan ikkitasini domen endi hosil
    /// qila olmaydi (`Draft + !IsActive`, `Archived + IsActive`), lekin eski qatorlar sifatida
    /// uchrashi mumkin — filtr ularni ham to'g'ri joylashtirishi shart.
    /// </summary>
    private static List<AssessmentProgram> AllStoredCombinations()
    {
        var draftActive = Draft();

        var draftInactive = Draft();
        ForceIsActive(draftInactive, false);

        var published = Published();

        var paused = Published();
        paused.Deactivate(Now);

        var archived = Published();
        archived.Archive(Now);

        var archivedButFlaggedActive = Published();
        archivedButFlaggedActive.Archive(Now);
        ForceIsActive(archivedButFlaggedActive, true);

        return [draftActive, draftInactive, published, paused, archived, archivedButFlaggedActive];
    }

    private static AssessmentProgram Draft() =>
        AssessmentProgram.Create(Guid.NewGuid(), $"P-{Guid.NewGuid():N}", "Sinov dasturi", Now);

    private static AssessmentProgram Published()
    {
        var program = Draft();
        program.AddTest(Guid.NewGuid(), 1, Now);
        program.Publish(Now);
        return program;
    }

    /// <summary>Faqat test uchun: domen orqali erishib bo'lmaydigan bazadagi qatorni taqlid qiladi.</summary>
    private static void ForceIsActive(AssessmentProgram program, bool value) =>
        typeof(AssessmentProgram)
            .GetProperty(nameof(AssessmentProgram.IsActive), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(program, value);
}
