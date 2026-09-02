using FluentAssertions;
using StudentRoadMap.Application.Admin.Catalog.Tests.Publish;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Tests.Admin;

/// <summary>
/// `CatalogPublishValidator` — `docs/03` §6.3 nashr validatsiyasi ("xatolar RO'YXAT sifatida
/// qaytadi", `docs/07` §3.4). DB'siz sof mantiq testi.
/// </summary>
public sealed class CatalogPublishValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static TestDefinition CreateSumTestDefinition() => TestDefinition.Create(
        Guid.NewGuid(), "CUSTOM-1", "Maxsus anketa", 1, 10, "SUM", Now, kind: TestKind.Custom, isSystem: false);

    private static Question CreateQuestion(Guid testDefinitionId, string code, string scale) =>
        Question.Create(Guid.NewGuid(), testDefinitionId, code, 1, "Savol matni", QuestionType.Likert5, scale, 1, 1.0m);

    private static TestScale CreateFullyCoveredScale(Guid testDefinitionId, string code) => TestScale.Create(
        Guid.NewGuid(),
        testDefinitionId,
        code,
        code,
        1,
        [new InterpretationBand(0, 33, "Past"), new InterpretationBand(34, 66, "O'rtacha"), new InterpretationBand(67, 100, "Yuqori")]);

    [Fact]
    public void Validate_NoQuestions_ReturnsTestHasNoQuestionsIssue()
    {
        var test = CreateSumTestDefinition();

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().ContainSingle(i => i.Code == "TEST_HAS_NO_QUESTIONS");
    }

    [Fact]
    public void Validate_SumTestWithoutScales_ReturnsTestHasNoScalesIssue()
    {
        var test = CreateSumTestDefinition();
        test.AddQuestion(CreateQuestion(test.Id, "Q1", "STRESS"), Now);

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().Contain(i => i.Code == "TEST_HAS_NO_SCALES");
    }

    [Fact]
    public void Validate_ScaleWithFewerThanFourQuestions_ReturnsScaleTooFewQuestionsIssue()
    {
        var test = CreateSumTestDefinition();
        test.AddScale(CreateFullyCoveredScale(test.Id, "STRESS"), Now);
        test.AddQuestion(CreateQuestion(test.Id, "Q1", "STRESS"), Now);
        test.AddQuestion(CreateQuestion(test.Id, "Q2", "STRESS"), Now);

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().ContainSingle(i => i.Code == "SCALE_TOO_FEW_QUESTIONS" && i.Scale == "STRESS");
    }

    [Fact]
    public void Validate_QuestionWithUnknownScale_ReturnsQuestionWithoutScaleIssue()
    {
        var test = CreateSumTestDefinition();
        test.AddScale(CreateFullyCoveredScale(test.Id, "STRESS"), Now);
        for (var i = 1; i <= 4; i++)
        {
            test.AddQuestion(CreateQuestion(test.Id, $"S{i}", "STRESS"), Now);
        }

        test.AddQuestion(CreateQuestion(test.Id, "ORPHAN", "UNKNOWN"), Now);

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().Contain(i => i.Code == "QUESTION_WITHOUT_SCALE" && i.QuestionCode == "ORPHAN");
    }

    [Fact]
    public void Validate_BandsWithGap_ReturnsScaleBandGapIssue()
    {
        // PM misoli (2026-09-02): 0-33/35-100 — 34 hech qaysi oralig'iga tushmaydi.
        var test = CreateSumTestDefinition();
        var scale = TestScale.Create(
            Guid.NewGuid(), test.Id, "STRESS", "Stress", 1,
            [new InterpretationBand(0, 33, "Past"), new InterpretationBand(35, 100, "Yuqori")]);
        test.AddScale(scale, Now);
        for (var i = 1; i <= 4; i++)
        {
            test.AddQuestion(CreateQuestion(test.Id, $"S{i}", "STRESS"), Now);
        }

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().Contain(i => i.Code == "SCALE_BAND_GAP" && i.Scale == "STRESS");
    }

    [Fact]
    public void Validate_BandsOverlapping_ReturnsScaleBandOverlapIssue()
    {
        // PM misoli (2026-09-02): 0-50/50-100 — 50 ikkala oraliqqa ham tushadi (to' inklyuziv).
        var test = CreateSumTestDefinition();
        var scale = TestScale.Create(
            Guid.NewGuid(), test.Id, "STRESS", "Stress", 1,
            [new InterpretationBand(0, 50, "Past"), new InterpretationBand(50, 100, "Yuqori")]);
        test.AddScale(scale, Now);
        for (var i = 1; i <= 4; i++)
        {
            test.AddQuestion(CreateQuestion(test.Id, $"S{i}", "STRESS"), Now);
        }

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().Contain(i => i.Code == "SCALE_BAND_OVERLAP" && i.Scale == "STRESS");
    }

    [Fact]
    public void Validate_FractionalBandBoundary_ReturnsScaleBandNotIntegerIssue()
    {
        // PM qarori (2026-09-02, `docs/03` §6.3): kasrli chegara ruxsat etilsa 33.35 ballga ega
        // o'quvchi hech qaysi oraliqqa tushmay qoladi (P09 QA qarzi) — shuning uchun butun son
        // qat'iy talab qilinadi, tolerantlik emas.
        var test = CreateSumTestDefinition();
        var scale = TestScale.Create(
            Guid.NewGuid(), test.Id, "STRESS", "Stress", 1,
            [new InterpretationBand(0, 33.3, "Past"), new InterpretationBand(33.4, 66.6, "O'rtacha"), new InterpretationBand(66.7, 100, "Yuqori")]);
        test.AddScale(scale, Now);
        for (var i = 1; i <= 4; i++)
        {
            test.AddQuestion(CreateQuestion(test.Id, $"S{i}", "STRESS"), Now);
        }

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().ContainSingle(i => i.Code == "SCALE_BAND_NOT_INTEGER" && i.Scale == "STRESS");
    }

    [Fact]
    public void Validate_IncompleteBandCoverage_ReturnsScaleBandIncompleteIssue()
    {
        // PM misoli (2026-09-02): 0-99 — 100 hech qaysi oralig'iga tushmaydi.
        var test = CreateSumTestDefinition();
        var scale = TestScale.Create(
            Guid.NewGuid(), test.Id, "STRESS", "Stress", 1,
            [new InterpretationBand(0, 99, "Yagona")]);
        test.AddScale(scale, Now);
        for (var i = 1; i <= 4; i++)
        {
            test.AddQuestion(CreateQuestion(test.Id, $"S{i}", "STRESS"), Now);
        }

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().ContainSingle(i => i.Code == "SCALE_BAND_INCOMPLETE" && i.Scale == "STRESS");
    }

    [Fact]
    public void Validate_FullyValidSumTest_ReturnsNoIssues()
    {
        var test = CreateSumTestDefinition();
        test.AddScale(CreateFullyCoveredScale(test.Id, "STRESS"), Now);
        for (var i = 1; i <= 4; i++)
        {
            test.AddQuestion(CreateQuestion(test.Id, $"S{i}", "STRESS"), Now);
        }

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Validate_SurveyMode_SkipsScaleRules()
    {
        var test = TestDefinition.Create(
            Guid.NewGuid(), "SURVEY-1", "So'rovnoma", 1, 10, scoringStrategyCode: null, Now, scoringMode: TestScoringMode.Survey);
        test.AddQuestion(CreateQuestion(test.Id, "Q1", "ANY"), Now);

        var issues = CatalogPublishValidator.Validate(test);

        issues.Should().BeEmpty();
    }
}
