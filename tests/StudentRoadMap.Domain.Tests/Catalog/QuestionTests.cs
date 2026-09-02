using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>`Question` — `IsSystem = true` savollarda shkala/variant qulfi (BR-8).</summary>
public sealed class QuestionTests
{
    private static Question CreateSystemQuestion(QuestionType type = QuestionType.Likert5) =>
        Question.Create(Guid.NewGuid(), Guid.NewGuid(), "Q1", 1, "Savol matni", type, "EI", 1, 1.0m, isSystem: true);

    private static Question CreateCustomQuestion(QuestionType type = QuestionType.SingleChoice) =>
        Question.Create(Guid.NewGuid(), Guid.NewGuid(), "Q1", 1, "Savol matni", type, "EI", 1, 1.0m, isSystem: false);

    [Theory]
    [InlineData(2)]
    [InlineData(-2)]
    [InlineData(0)]
    public void Create_WithInvalidScaleDirection_ThrowsArgumentOutOfRangeException(int direction)
    {
        var act = () => Question.Create(Guid.NewGuid(), Guid.NewGuid(), "Q1", 1, "Matn", QuestionType.Likert5, "EI", direction, 1.0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateScale_OnSystemQuestion_ThrowsDomainException()
    {
        var question = CreateSystemQuestion();

        var act = () => question.UpdateScale("SN", -1, 2.0m);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void UpdateScale_OnCustomQuestion_UpdatesFields()
    {
        var question = CreateCustomQuestion(QuestionType.Likert5);

        question.UpdateScale("SN", -1, 2.0m);

        question.Scale.Should().Be("SN");
        question.ScaleDirection.Should().Be(-1);
        question.Weight.Should().Be(2.0m);
    }

    [Fact]
    public void AddOption_OnSystemQuestion_ThrowsDomainException()
    {
        var question = CreateSystemQuestion(QuestionType.SingleChoice);
        var option = AnswerOption.Create(Guid.NewGuid(), question.Id, "Variant", 1, 1);

        var act = () => question.AddOption(option);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void AddOption_OnLikertQuestion_ThrowsDomainException()
    {
        var question = CreateCustomQuestion(QuestionType.Likert5);
        var option = AnswerOption.Create(Guid.NewGuid(), question.Id, "Variant", 1, 1);

        var act = () => question.AddOption(option);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("QUESTION_OPTIONS_NOT_ALLOWED");
    }

    [Fact]
    public void AddOption_OnCustomSingleChoiceQuestion_Succeeds()
    {
        var question = CreateCustomQuestion(QuestionType.SingleChoice);
        var option = AnswerOption.Create(Guid.NewGuid(), question.Id, "Variant", 1, 1);

        question.AddOption(option);

        question.Options.Should().ContainSingle();
    }

    [Fact]
    public void RemoveOption_OnSystemQuestion_ThrowsDomainException()
    {
        var question = CreateSystemQuestion(QuestionType.SingleChoice);

        var act = () => question.RemoveOption(Guid.NewGuid());

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    // --- P04 seed infratuzilmasi: UpdateOrder ------------------------------------------------

    [Fact]
    public void UpdateOrder_OnSystemQuestion_UpdatesDisplayOrderOnly()
    {
        // BR-8 faqat Scale/Direction/Weightni qulflaydi — tartib tizim savolida ham
        // seed orqali yangilanishi mumkin (`CLAUDE.md` 9a-qoida).
        var question = CreateSystemQuestion();

        question.UpdateOrder(7);

        question.DisplayOrder.Should().Be(7);
        question.Scale.Should().Be("EI");
        question.ScaleDirection.Should().Be(1);
        question.Weight.Should().Be(1.0m);
        question.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void UpdateOrder_OnCustomQuestion_UpdatesDisplayOrder()
    {
        var question = CreateCustomQuestion(QuestionType.Likert5);

        question.UpdateOrder(3);

        question.DisplayOrder.Should().Be(3);
    }

    [Fact]
    public void UpdateRequired_OnSystemQuestion_Succeeds()
    {
        // BR-8 doirasiga kirmaydi — `docs/07` §3.4 faqat `Scale`/`Direction`/`Weight`ni cheklaydi.
        var question = CreateSystemQuestion();

        question.UpdateRequired(false);

        question.IsRequired.Should().BeFalse();
    }
}
