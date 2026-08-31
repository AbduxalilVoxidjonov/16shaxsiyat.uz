using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>
/// `TestDefinition` nashr oqimi va tizim metodikasi qulfi (BR-8, `docs/04` 2.7-bo'lim).
/// </summary>
public sealed class TestDefinitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static TestDefinition CreateCustomTestDefinition() => TestDefinition.Create(
        Guid.NewGuid(),
        "CUSTOM-1",
        "Maxsus anketa",
        1,
        10,
        "SUM",
        Now,
        kind: TestKind.Custom,
        isSystem: false);

    private static TestDefinition CreateSystemTestDefinition() => TestDefinition.Create(
        Guid.NewGuid(),
        "MBTI16",
        "MBTI-16",
        1,
        20,
        "MBTI16",
        Now,
        kind: TestKind.Standard,
        isSystem: true);

    private static Question CreateQuestion(Guid testDefinitionId, string code = "Q1", bool isSystem = false) =>
        Question.Create(Guid.NewGuid(), testDefinitionId, code, 1, "Savol matni", QuestionType.Likert5, "EI", 1, 1.0m, isSystem: isSystem);

    [Fact]
    public void AddQuestion_ToSystemTestDefinition_ThrowsDomainException()
    {
        var testDefinition = CreateSystemTestDefinition();
        var question = CreateQuestion(testDefinition.Id, isSystem: true);

        var act = () => testDefinition.AddQuestion(question, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void RemoveQuestion_FromSystemTestDefinition_ThrowsDomainException()
    {
        var testDefinition = CreateSystemTestDefinition();

        var act = () => testDefinition.RemoveQuestion(Guid.NewGuid(), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void AddQuestion_ToCustomTestDefinition_Succeeds()
    {
        var testDefinition = CreateCustomTestDefinition();
        var question = CreateQuestion(testDefinition.Id);

        testDefinition.AddQuestion(question, Now);

        testDefinition.QuestionCount.Should().Be(1);
    }

    [Fact]
    public void AddQuestion_WithDuplicateCode_ThrowsDomainException()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.AddQuestion(CreateQuestion(testDefinition.Id, "Q1"), Now);

        var act = () => testDefinition.AddQuestion(CreateQuestion(testDefinition.Id, "Q1"), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("QUESTION_CODE_DUPLICATE");
    }

    [Fact]
    public void Publish_WithoutActiveQuestions_ThrowsDomainException()
    {
        var testDefinition = CreateCustomTestDefinition();

        var act = () => testDefinition.Publish(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("TEST_NOT_PUBLISHABLE");
    }

    [Fact]
    public void Publish_WithActiveQuestion_TransitionsToPublished()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.AddQuestion(CreateQuestion(testDefinition.Id), Now);

        testDefinition.Publish(Now.AddDays(1));

        testDefinition.Status.Should().Be(TestDefinitionStatus.Published);
        testDefinition.PublishedAt.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ThrowsDomainException()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.AddQuestion(CreateQuestion(testDefinition.Id), Now);
        testDefinition.Publish(Now);

        var act = () => testDefinition.Publish(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("TEST_DEFINITION_INVALID_TRANSITION");
    }

    [Fact]
    public void AddQuestion_AfterPublish_BumpsVersion()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.AddQuestion(CreateQuestion(testDefinition.Id, "Q1"), Now);
        testDefinition.Publish(Now);
        testDefinition.Version.Should().Be(1);

        testDefinition.AddQuestion(CreateQuestion(testDefinition.Id, "Q2"), Now);

        testDefinition.Version.Should().Be(2);
    }

    [Fact]
    public void Archive_FromArchived_ThrowsDomainException()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.Archive(Now);

        var act = () => testDefinition.Archive(Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("TEST_DEFINITION_INVALID_TRANSITION");
    }

    [Fact]
    public void Duplicate_AlwaysProducesCustomNonSystemDraftCopy()
    {
        // Eslatma: `IsSystem = true` anketaga `AddQuestion` orqali savol qo'shib bo'lmaydi (BR-8) —
        // savollar seed infratuzilmasi orqali to'g'ridan-to'g'ri materiallashtiriladi. Shu sabab bu
        // yerda savolsiz nusxalash tekshiriladi.
        var testDefinition = CreateSystemTestDefinition();

        var copy = testDefinition.Duplicate(Guid.NewGuid(), "MBTI16-COPY", Now);

        copy.IsSystem.Should().BeFalse();
        copy.Kind.Should().Be(TestKind.Custom);
        copy.Status.Should().Be(TestDefinitionStatus.Draft);
    }
}
