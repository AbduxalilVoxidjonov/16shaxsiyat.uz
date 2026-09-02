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

    private static Question CreateQuestion(Guid testDefinitionId, string code = "Q1", bool isSystem = false, string scale = "EI") =>
        Question.Create(Guid.NewGuid(), testDefinitionId, code, 1, "Savol matni", QuestionType.Likert5, scale, 1, 1.0m, isSystem: isSystem);

    private static TestScale CreateScale(Guid testDefinitionId, string code = "STRESS") =>
        TestScale.Create(Guid.NewGuid(), testDefinitionId, code, "Stress", 1);

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

    // --- P04 seed infratuzilmasi: CreateSystemPublished / UpdateMetadata --------------------

    [Fact]
    public void CreateSystemPublished_BuildsPublishedSystemAggregateWithSystemQuestions()
    {
        var testDefinitionId = Guid.NewGuid();
        var question = CreateQuestion(testDefinitionId, "MB-Q01", isSystem: true);

        var testDefinition = TestDefinition.CreateSystemPublished(
            testDefinitionId, "MBTI16", "16 tipli shaxsiyat modeli", "Tavsif", 1, 9, false, 10, "MBTI16",
            [question], Now);

        testDefinition.IsSystem.Should().BeTrue();
        testDefinition.Kind.Should().Be(TestKind.Standard);
        testDefinition.Status.Should().Be(TestDefinitionStatus.Published);
        testDefinition.PublishedAt.Should().Be(Now);
        testDefinition.ScoringStrategyCode.Should().Be("MBTI16");
        testDefinition.QuestionCount.Should().Be(1);
        testDefinition.Questions.Should().OnlyContain(q => q.IsSystem);
    }

    [Fact]
    public void CreateSystemPublished_WithEmptyQuestions_ThrowsArgumentException()
    {
        var testDefinitionId = Guid.NewGuid();

        var act = () => TestDefinition.CreateSystemPublished(
            testDefinitionId, "MBTI16", "16 tipli shaxsiyat modeli", null, 1, 9, false, 10, "MBTI16",
            [], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSystemPublished_WithNonSystemQuestion_ThrowsArgumentException()
    {
        var testDefinitionId = Guid.NewGuid();
        var question = CreateQuestion(testDefinitionId, "MB-Q01", isSystem: false);

        var act = () => TestDefinition.CreateSystemPublished(
            testDefinitionId, "MBTI16", "16 tipli shaxsiyat modeli", null, 1, 9, false, 10, "MBTI16",
            [question], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSystemPublished_ThenAddQuestion_ThrowsDomainException()
    {
        // Muhim: `CreateSystemPublished` orqali qulflangan tizim metodikasiga keyinchalik
        // `AddQuestion` chaqirilsa ham BR-8 qulfi haqiqatan faol bo'lishi kerak (`SYSTEM_TEST_LOCKED`).
        var testDefinitionId = Guid.NewGuid();
        var question = CreateQuestion(testDefinitionId, "MB-Q01", isSystem: true);
        var testDefinition = TestDefinition.CreateSystemPublished(
            testDefinitionId, "MBTI16", "16 tipli shaxsiyat modeli", null, 1, 9, false, 10, "MBTI16",
            [question], Now);

        var newQuestion = CreateQuestion(testDefinitionId, "MB-Q02", isSystem: true);
        var act = () => testDefinition.AddQuestion(newQuestion, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void CreateSystemPublished_ThenRemoveQuestion_ThrowsDomainException()
    {
        var testDefinitionId = Guid.NewGuid();
        var question = CreateQuestion(testDefinitionId, "MB-Q01", isSystem: true);
        var testDefinition = TestDefinition.CreateSystemPublished(
            testDefinitionId, "MBTI16", "16 tipli shaxsiyat modeli", null, 1, 9, false, 10, "MBTI16",
            [question], Now);

        var act = () => testDefinition.RemoveQuestion(question.Id, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void UpdateMetadata_UpdatesTextFieldsOnly_LeavesScaleKindIsSystemStatusUntouched()
    {
        var testDefinitionId = Guid.NewGuid();
        var question = CreateQuestion(testDefinitionId, "MB-Q01", isSystem: true);
        var testDefinition = TestDefinition.CreateSystemPublished(
            testDefinitionId, "MBTI16", "Eski nom", "Eski tavsif", 1, 9, false, 10, "MBTI16",
            [question], Now);

        testDefinition.UpdateMetadata("Yangi nom", "Yangi tavsif", 2, 12, true, 20, Now.AddDays(1));

        testDefinition.NameUz.Should().Be("Yangi nom");
        testDefinition.DescriptionUz.Should().Be("Yangi tavsif");
        testDefinition.DisplayOrder.Should().Be(2);
        testDefinition.EstimatedMinutes.Should().Be(12);
        testDefinition.ShuffleQuestions.Should().BeTrue();
        testDefinition.PageSize.Should().Be(20);
        testDefinition.UpdatedAt.Should().Be(Now.AddDays(1));

        // `UpdateMetadata` — faqat matn/tartib maydonlariga tegadi, BR-8 himoyalagan holatga
        // (Kind/IsSystem/Status/ScoringStrategyCode) va savol shkalasiga tegmaydi.
        testDefinition.Kind.Should().Be(TestKind.Standard);
        testDefinition.IsSystem.Should().BeTrue();
        testDefinition.Status.Should().Be(TestDefinitionStatus.Published);
        testDefinition.ScoringStrategyCode.Should().Be("MBTI16");
        testDefinition.Questions.Single().Scale.Should().Be("EI");
    }

    [Fact]
    public void UpdateMetadata_WithBlankName_ThrowsArgumentException()
    {
        var testDefinition = CreateCustomTestDefinition();

        var act = () => testDefinition.UpdateMetadata(" ", null, 1, 10, false, 10, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateMetadata_WithNonPositivePageSize_ThrowsArgumentOutOfRangeException()
    {
        var testDefinition = CreateCustomTestDefinition();

        var act = () => testDefinition.UpdateMetadata("Nom", null, 1, 10, false, 0, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------- ScoringMode (`docs/06` 8-bo'lim, 2026-09-02 qaror, `prompts/34` A4-band) ----------

    [Fact]
    public void Create_ScoredWithoutStrategyCode_ThrowsArgumentException()
    {
        var act = () => TestDefinition.Create(
            Guid.NewGuid(), "SCORED-1", "Nomi", 1, 10, scoringStrategyCode: null, Now,
            scoringMode: TestScoringMode.Scored);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SurveyWithoutStrategyCode_SucceedsAndScoringStrategyCodeIsNull()
    {
        var testDefinition = TestDefinition.Create(
            Guid.NewGuid(), "SURVEY-1", "So'rovnoma", 1, 10, scoringStrategyCode: null, Now,
            scoringMode: TestScoringMode.Survey);

        testDefinition.ScoringMode.Should().Be(TestScoringMode.Survey);
        testDefinition.ScoringStrategyCode.Should().BeNull();
    }

    [Fact]
    public void Create_SurveyWithStrategyCodeProvided_IgnoresIt()
    {
        // `docs/06` 8-bo'lim: "Survey'da ScoringStrategyCode ishlatilmaydi" — berilgan bo'lsa ham e'tiborsiz qoldiriladi (`null`ga tushadi).
        var testDefinition = TestDefinition.Create(
            Guid.NewGuid(), "SURVEY-2", "So'rovnoma", 1, 10, scoringStrategyCode: "SUM", Now,
            scoringMode: TestScoringMode.Survey);

        testDefinition.ScoringStrategyCode.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutExplicitScoringMode_DefaultsToScored()
    {
        var testDefinition = CreateCustomTestDefinition();

        testDefinition.ScoringMode.Should().Be(TestScoringMode.Scored);
    }

    // --- P37 (`prompts/37-katalog-crud-backend.md`) — TestScale CRUD + BR-8 ---------------------

    [Fact]
    public void AddScale_ToCustomTestDefinition_Succeeds()
    {
        var testDefinition = CreateCustomTestDefinition();
        var scale = CreateScale(testDefinition.Id);

        testDefinition.AddScale(scale, Now);

        testDefinition.Scales.Should().ContainSingle().Which.Code.Should().Be("STRESS");
    }

    [Fact]
    public void AddScale_ToSystemTestDefinition_ThrowsDomainException()
    {
        var testDefinition = CreateSystemTestDefinition();
        var scale = CreateScale(testDefinition.Id);

        var act = () => testDefinition.AddScale(scale, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void AddScale_DuplicateCode_ThrowsDomainException()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.AddScale(CreateScale(testDefinition.Id, "STRESS"), Now);

        var act = () => testDefinition.AddScale(CreateScale(testDefinition.Id, "STRESS"), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SCALE_CODE_DUPLICATE");
    }

    [Fact]
    public void RemoveScale_FromSystemTestDefinition_ThrowsDomainException()
    {
        var testDefinition = CreateSystemTestDefinition();

        var act = () => testDefinition.RemoveScale(Guid.NewGuid(), Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SYSTEM_TEST_LOCKED");
    }

    [Fact]
    public void RemoveScale_WithQuestionsAssigned_ThrowsDomainException()
    {
        var testDefinition = CreateCustomTestDefinition();
        var scale = CreateScale(testDefinition.Id, "STRESS");
        testDefinition.AddScale(scale, Now);
        testDefinition.AddQuestion(CreateQuestion(testDefinition.Id, "Q1", scale: "STRESS"), Now);

        var act = () => testDefinition.RemoveScale(scale.Id, Now);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be("SCALE_IN_USE");
    }

    [Fact]
    public void RemoveScale_WithoutQuestions_Succeeds()
    {
        var testDefinition = CreateCustomTestDefinition();
        var scale = CreateScale(testDefinition.Id, "STRESS");
        testDefinition.AddScale(scale, Now);

        testDefinition.RemoveScale(scale.Id, Now);

        testDefinition.Scales.Should().BeEmpty();
    }

    [Fact]
    public void AddScale_ToPublishedTestDefinition_BumpsVersion()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.AddQuestion(CreateQuestion(testDefinition.Id), Now);
        testDefinition.Publish(Now);
        var versionBeforeScale = testDefinition.Version;

        testDefinition.AddScale(CreateScale(testDefinition.Id), Now);

        testDefinition.Version.Should().Be(versionBeforeScale + 1);
    }

    [Fact]
    public void Duplicate_CopiesScales()
    {
        var testDefinition = CreateCustomTestDefinition();
        testDefinition.AddScale(CreateScale(testDefinition.Id, "STRESS"), Now);

        var copy = testDefinition.Duplicate(Guid.NewGuid(), "CUSTOM-1-COPY", Now);

        copy.Scales.Should().ContainSingle().Which.Code.Should().Be("STRESS");
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var testDefinition = CreateCustomTestDefinition();

        testDefinition.Deactivate(Now);
        testDefinition.IsActive.Should().BeFalse();

        testDefinition.Activate(Now);
        testDefinition.IsActive.Should().BeTrue();
    }
}
