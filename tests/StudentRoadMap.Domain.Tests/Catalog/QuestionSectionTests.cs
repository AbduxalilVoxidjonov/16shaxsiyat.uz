using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Catalog;

/// <summary>
/// `QuestionSection` entity qoidalari va `TestDefinition` agregat metodlari (`AddSection`,
/// `RemoveSection`, `MoveQuestionToSection`, `ReorderSections`) — `docs/18` §2.2.
/// </summary>
public sealed class QuestionSectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static TestDefinition CreateSurveyTestDefinition() => TestDefinition.Create(
        Guid.NewGuid(),
        "SURVEY-1",
        "So'rovnoma",
        1,
        10,
        scoringStrategyCode: null,
        Now,
        kind: TestKind.Custom,
        isSystem: false,
        scoringMode: TestScoringMode.Survey);

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

    private static QuestionSection CreateSection(Guid testDefinitionId, string code = "S1", int order = 1) =>
        QuestionSection.Create(Guid.NewGuid(), testDefinitionId, code, "Bo'lim " + code, order);

    private static Question CreateQuestion(Guid testDefinitionId, string code = "Q1", QuestionType type = QuestionType.ShortText) =>
        Question.Create(Guid.NewGuid(), testDefinitionId, code, 1, "Savol matni", type, "SURVEY", 1, 1.0m);

    [Fact]
    public void AddSection_ToCustomSurveyTestDefinition_Succeeds()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var section = CreateSection(testDefinition.Id);

        testDefinition.AddSection(section, Now);

        testDefinition.Sections.Should().ContainSingle();
    }

    [Fact]
    public void AddSection_ToSystemTestDefinition_ThrowsDomainException()
    {
        var testDefinition = CreateSystemTestDefinition();
        var section = CreateSection(testDefinition.Id);

        var act = () => testDefinition.AddSection(section, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void AddSection_WithDuplicateCode_ThrowsDomainException()
    {
        var testDefinition = CreateSurveyTestDefinition();
        testDefinition.AddSection(CreateSection(testDefinition.Id, "S1"), Now);

        var act = () => testDefinition.AddSection(CreateSection(testDefinition.Id, "S1"), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SECTION_CODE_DUPLICATE");
    }

    [Fact]
    public void RemoveSection_WithQuestionsAssigned_ThrowsDomainException()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var section = CreateSection(testDefinition.Id);
        testDefinition.AddSection(section, Now);
        var question = CreateQuestion(testDefinition.Id);
        testDefinition.AddQuestion(question, Now);
        testDefinition.MoveQuestionToSection(question.Id, section.Id, Now);

        var act = () => testDefinition.RemoveSection(section.Id, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SECTION_IN_USE");
    }

    [Fact]
    public void RemoveSection_WithoutQuestions_Succeeds()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var section = CreateSection(testDefinition.Id);
        testDefinition.AddSection(section, Now);

        testDefinition.RemoveSection(section.Id, Now);

        testDefinition.Sections.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSection_FromSystemTestDefinition_ThrowsDomainException()
    {
        var testDefinition = CreateSystemTestDefinition();

        var act = () => testDefinition.RemoveSection(Guid.NewGuid(), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    // --- `RemoveQuestion` — `docs/18` B-5: boshqa savol/bo'limning ko'rsatish sharti (`VisibilityRule`)
    // shu savolning kodiga tayansa `QUESTION_REFERENCED_BY_VISIBILITY` (P52, 2026-09-11 QA topilmasi:
    // jimgina `500 INTERNAL_ERROR` — `fk_answers_questions_question_id` — o'rniga OLDINDAN aniq xato). ---

    [Fact]
    public void RemoveQuestion_ReferencedByAnotherQuestionVisibility_ThrowsDomainException()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var gate = CreateQuestion(testDefinition.Id, "Q1");
        testDefinition.AddQuestion(gate, Now);
        var rule = new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q1", VisibilityOperator.Answered, [])]);
        var dependent = Question.Create(Guid.NewGuid(), testDefinition.Id, "Q2", 2, "Bog'liq savol", QuestionType.ShortText, "SURVEY", 1, 1.0m, visibilityRule: rule);
        testDefinition.AddQuestion(dependent, Now);

        var act = () => testDefinition.RemoveQuestion(gate.Id, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("QUESTION_REFERENCED_BY_VISIBILITY");
        ex.Message.Should().Contain("Q1").And.Contain("Q2");
    }

    [Fact]
    public void RemoveQuestion_ReferencedBySectionVisibility_ThrowsDomainException()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var gate = CreateQuestion(testDefinition.Id, "Q1");
        testDefinition.AddQuestion(gate, Now);
        var rule = new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q1", VisibilityOperator.Answered, [])]);
        var section = QuestionSection.Create(Guid.NewGuid(), testDefinition.Id, "S1", "Bo'lim", 1, visibilityRule: rule);
        testDefinition.AddSection(section, Now);

        var act = () => testDefinition.RemoveQuestion(gate.Id, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("QUESTION_REFERENCED_BY_VISIBILITY");
        ex.Message.Should().Contain("Q1").And.Contain("S1");
    }

    [Fact]
    public void RemoveQuestion_WithoutVisibilityReferences_Succeeds()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var question = CreateQuestion(testDefinition.Id, "Q1");
        testDefinition.AddQuestion(question, Now);

        testDefinition.RemoveQuestion(question.Id, Now);

        testDefinition.QuestionCount.Should().Be(0);
    }

    [Fact]
    public void MoveQuestionToSection_AssignsSectionId()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var section = CreateSection(testDefinition.Id);
        testDefinition.AddSection(section, Now);
        var question = CreateQuestion(testDefinition.Id);
        testDefinition.AddQuestion(question, Now);

        testDefinition.MoveQuestionToSection(question.Id, section.Id, Now);

        question.SectionId.Should().Be(section.Id);
    }

    [Fact]
    public void MoveQuestionToSection_WithUnknownSection_ThrowsArgumentException()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var question = CreateQuestion(testDefinition.Id);
        testDefinition.AddQuestion(question, Now);

        var act = () => testDefinition.MoveQuestionToSection(question.Id, Guid.NewGuid(), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MoveQuestionToSection_WithNullSectionId_UnassignsSection()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var section = CreateSection(testDefinition.Id);
        testDefinition.AddSection(section, Now);
        var question = CreateQuestion(testDefinition.Id);
        testDefinition.AddQuestion(question, Now);
        testDefinition.MoveQuestionToSection(question.Id, section.Id, Now);

        testDefinition.MoveQuestionToSection(question.Id, null, Now);

        question.SectionId.Should().BeNull();
    }

    [Fact]
    public void ReorderSections_UpdatesDisplayOrder()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var s1 = CreateSection(testDefinition.Id, "S1", 1);
        var s2 = CreateSection(testDefinition.Id, "S2", 2);
        testDefinition.AddSection(s1, Now);
        testDefinition.AddSection(s2, Now);

        testDefinition.ReorderSections([(s1.Id, 2), (s2.Id, 1)], Now);

        s1.DisplayOrder.Should().Be(2);
        s2.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public void ReorderSections_OnSystemTestDefinition_ThrowsDomainException()
    {
        var testDefinition = CreateSystemTestDefinition();

        var act = () => testDefinition.ReorderSections([], Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    // --- QuestionSection value-level qoidalar ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankCode_ThrowsArgumentException(string code)
    {
        var act = () => QuestionSection.Create(Guid.NewGuid(), Guid.NewGuid(), code, "Sarlavha", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidCodeCharacters_ThrowsArgumentException()
    {
        var act = () => QuestionSection.Create(Guid.NewGuid(), Guid.NewGuid(), "S1 A", "Sarlavha", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithTooLongCode_ThrowsArgumentException()
    {
        var act = () => QuestionSection.Create(Guid.NewGuid(), Guid.NewGuid(), new string('A', 21), "Sarlavha", 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidVisibilityRule_ThrowsArgumentException()
    {
        var invalidRule = new VisibilityRule(VisibilityMatch.All, []);

        var act = () => QuestionSection.Create(Guid.NewGuid(), Guid.NewGuid(), "S1", "Sarlavha", 1, visibilityRule: invalidRule);

        act.Should().Throw<ArgumentException>();
    }

    // --- B-1/B-2 (`docs/18` §1): matn/ko'p tanlov turlari va `visibility` faqat `Survey`da ---

    [Fact]
    public void AddQuestion_WithTextTypeInScoredMode_ThrowsDomainException()
    {
        var testDefinition = TestDefinition.Create(Guid.NewGuid(), "SCORED-1", "Ball anketa", 1, 10, "SUM", Now, kind: TestKind.Custom, isSystem: false);
        var question = CreateQuestion(testDefinition.Id, type: QuestionType.ShortText);

        var act = () => testDefinition.AddQuestion(question, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("QUESTION_TYPE_NOT_SCORABLE");
    }

    [Fact]
    public void AddQuestion_WithTextTypeInSurveyMode_Succeeds()
    {
        var testDefinition = CreateSurveyTestDefinition();
        var question = CreateQuestion(testDefinition.Id, type: QuestionType.ShortText);

        testDefinition.AddQuestion(question, Now);

        testDefinition.QuestionCount.Should().Be(1);
    }

    [Fact]
    public void AddQuestion_WithVisibilityRuleInScoredMode_ThrowsDomainException()
    {
        var testDefinition = TestDefinition.Create(Guid.NewGuid(), "SCORED-2", "Ball anketa", 1, 10, "SUM", Now, kind: TestKind.Custom, isSystem: false);
        var rule = new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q0", VisibilityOperator.Equals, [1])]);
        var question = Question.Create(Guid.NewGuid(), testDefinition.Id, "Q1", 2, "Savol", QuestionType.Likert5, "SUM", 1, 1.0m, visibilityRule: rule);

        var act = () => testDefinition.AddQuestion(question, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("BRANCHING_NOT_ALLOWED_IN_SCORED");
    }

    [Fact]
    public void AddSection_WithVisibilityRuleInScoredMode_ThrowsDomainException()
    {
        var testDefinition = TestDefinition.Create(Guid.NewGuid(), "SCORED-3", "Ball anketa", 1, 10, "SUM", Now, kind: TestKind.Custom, isSystem: false);
        var rule = new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q0", VisibilityOperator.Equals, [1])]);
        var section = QuestionSection.Create(Guid.NewGuid(), testDefinition.Id, "S1", "Bo'lim", 1, visibilityRule: rule);

        var act = () => testDefinition.AddSection(section, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("BRANCHING_NOT_ALLOWED_IN_SCORED");
    }

    // --- `Duplicate` — bo'limlar va savol variantlari/shartlari ham nusxalanishi (`docs/18` A1 talabi) ---

    [Fact]
    public void Duplicate_CopiesSectionsAndRemapsQuestionSectionId()
    {
        var original = CreateSurveyTestDefinition();
        var section = CreateSection(original.Id, "S1", 1);
        original.AddSection(section, Now);
        var question = CreateQuestion(original.Id, "Q1");
        original.AddQuestion(question, Now);
        original.MoveQuestionToSection(question.Id, section.Id, Now);

        var copy = original.Duplicate(Guid.NewGuid(), "SURVEY-1-COPY", Now);

        copy.Sections.Should().ContainSingle(s => s.Code == "S1");
        var copiedQuestion = copy.Questions.Should().ContainSingle(q => q.Code == "Q1").Which;
        var copiedSection = copy.Sections.Single();
        copiedQuestion.SectionId.Should().Be(copiedSection.Id);
        copiedSection.Id.Should().NotBe(section.Id);
    }

    [Fact]
    public void Duplicate_CopiesQuestionVisibilityRuleAndOptions()
    {
        var original = CreateSurveyTestDefinition();
        var rule = new VisibilityRule(VisibilityMatch.All, [new VisibilityCondition("Q1", VisibilityOperator.Equals, [1])]);
        var gate = Question.Create(Guid.NewGuid(), original.Id, "Q1", 1, "Filtr", QuestionType.SingleChoice, "SURVEY", 1, 1.0m);
        gate.AddOption(AnswerOption.Create(Guid.NewGuid(), gate.Id, "Ha", 1, 1));
        gate.AddOption(AnswerOption.Create(Guid.NewGuid(), gate.Id, "Yo'q", 2, 2));
        original.AddQuestion(gate, Now);
        var dependent = Question.Create(Guid.NewGuid(), original.Id, "Q2", 2, "Bog'liq savol", QuestionType.ShortText, "SURVEY", 1, 1.0m, visibilityRule: rule);
        original.AddQuestion(dependent, Now);

        var copy = original.Duplicate(Guid.NewGuid(), "SURVEY-1-COPY2", Now);

        var copiedGate = copy.Questions.Single(q => q.Code == "Q1");
        copiedGate.Options.Should().HaveCount(2);
        var copiedDependent = copy.Questions.Single(q => q.Code == "Q2");
        copiedDependent.VisibilityRule.Should().BeEquivalentTo(rule);
    }
}
