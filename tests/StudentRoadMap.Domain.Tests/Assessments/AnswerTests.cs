using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Assessments;

public sealed class AnswerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithNegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, -1, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_SetsRevisionCountToZero()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        answer.RevisionCount.Should().Be(0);
    }

    [Fact]
    public void UpdateValue_IncrementsRevisionCount()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        answer.UpdateValue(5, null, 1500, Now.AddSeconds(30));
        answer.UpdateValue(2, null, 1800, Now.AddSeconds(60));

        answer.RevisionCount.Should().Be(2);
        answer.RawValue.Should().Be(2);
        answer.DurationMs.Should().Be(1800);
    }

    [Fact]
    public void UpdateValue_WithNegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        var act = () => answer.UpdateValue(3, null, -5, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- `docs/18` §2.7: `Answer` shakl invarianti (uchtadan aynan bittasi) ---

    [Fact]
    public void Create_WithAllThreeShapesEmpty_ThrowsDomainException()
    {
        var act = () => Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), rawValue: null, selectedOptionId: null, durationMs: 1000, answeredAt: Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ANSWER_SHAPE_INVALID");
    }

    [Fact]
    public void Create_WithRawValueAndText_ThrowsDomainException()
    {
        var act = () => Answer.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            rawValue: 3, selectedOptionId: null, durationMs: 1000, answeredAt: Now,
            textValue: "Toshkent");

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ANSWER_SHAPE_INVALID");
    }

    [Fact]
    public void Create_WithTextAndSelectedValues_ThrowsDomainException()
    {
        var act = () => Answer.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            rawValue: null, selectedOptionId: null, durationMs: 1000, answeredAt: Now,
            textValue: "Toshkent", selectedValues: [1, 2]);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ANSWER_SHAPE_INVALID");
    }

    [Fact]
    public void Create_WithWhitespaceOnlyText_ThrowsDomainException()
    {
        // Bo'sh/whitespace matn "javob berilgan" hisoblanmaydi (`AnswerSnapshot.IsAnswered` bilan izchil).
        var act = () => Answer.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            rawValue: null, selectedOptionId: null, durationMs: 1000, answeredAt: Now,
            textValue: "   ");

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ANSWER_SHAPE_INVALID");
    }

    [Fact]
    public void Create_WithOnlyTextValue_Succeeds()
    {
        var answer = Answer.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            rawValue: null, selectedOptionId: null, durationMs: 1000, answeredAt: Now,
            textValue: "Toshkent");

        answer.RawValue.Should().BeNull();
        answer.TextValue.Should().Be("Toshkent");
        answer.SelectedValues.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithOnlySelectedValues_Succeeds()
    {
        var answer = Answer.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            rawValue: null, selectedOptionId: null, durationMs: 1000, answeredAt: Now,
            selectedValues: [1, 3]);

        answer.RawValue.Should().BeNull();
        answer.TextValue.Should().BeNull();
        answer.SelectedValues.Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public void UpdateValue_ChangingShapeFromRawValueToText_Succeeds()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        answer.UpdateValue(rawValue: null, selectedOptionId: null, durationMs: 1200, answeredAt: Now.AddSeconds(10), textValue: "Yangi javob");

        answer.RawValue.Should().BeNull();
        answer.TextValue.Should().Be("Yangi javob");
    }

    [Fact]
    public void UpdateValue_WithInvalidShape_ThrowsDomainException()
    {
        var answer = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        var act = () => answer.UpdateValue(rawValue: null, selectedOptionId: null, durationMs: 1000, answeredAt: Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("ANSWER_SHAPE_INVALID");
    }
}
