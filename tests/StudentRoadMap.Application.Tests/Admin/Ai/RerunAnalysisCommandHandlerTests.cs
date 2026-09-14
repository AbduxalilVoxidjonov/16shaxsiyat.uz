using FluentAssertions;
using StudentRoadMap.Application.Admin.Ai.RerunAnalysis;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Tests.Admin.Ai;

/// <summary>
/// `RerunAnalysisCommandHandler` — `docs/07` §3.3 "POST .../rerun-analysis". Navbatga qo'yish
/// `IPostCommitActions.Enqueue` orqali KECHIKTIRILADI (P18-R1 bilan bir xil naqsh) — bu yerda
/// `Enqueue` chaqirilgani va navbat KO'RSATMASI o'zi (haqiqiy `RunAsync` emas) tekshiriladi;
/// commit'dan keyin haqiqatan bajarilishi `TransactionBehaviorTests`da.
/// </summary>
public sealed class RerunAnalysisCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private sealed class FakeContext : IAppDbContext
    {
        public List<Assessment> AssessmentList { get; } = [];

        public List<AuditLog> AuditLogList { get; } = [];

        /// <summary>Yangi qo'riqchi (2026-09-14) shu ro'yxatlarni `AssessmentTests`/`TestDefinitions` orqali JOIN qiladi.</summary>
        public List<AssessmentTest> AssessmentTestList { get; } = [];

        /// <summary>Yangi qo'riqchi (2026-09-14) shu ro'yxatlarni `AssessmentTests`/`TestDefinitions` orqali JOIN qiladi.</summary>
        public List<Domain.Catalog.TestDefinition> TestDefinitionList { get; } = [];

        public bool SaveChangesCalled { get; private set; }

        public IQueryable<Assessment> Assessments => AssessmentList.AsQueryable();

        public IQueryable<AuditLog> AuditLogs => AuditLogList.AsQueryable();

        public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

        public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is AuditLog log)
            {
                AuditLogList.Add(log);
            }
        }

        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.FromResult(1);
        }

        public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IQueryable<Domain.Schools.School> Schools => throw new NotSupportedException();

        public IQueryable<Domain.Students.Student> Students => throw new NotSupportedException();

        public IQueryable<AssessmentTest> AssessmentTests => AssessmentTestList.AsQueryable();

        public IQueryable<Answer> Answers => throw new NotSupportedException();

        public IQueryable<TestResult> TestResults => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

        public IQueryable<Domain.Catalog.Question> Questions => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.TestScale> TestScales => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.QuestionSection> QuestionSections => throw new NotSupportedException();

        public IQueryable<Domain.Jobs.AnalysisJob> AnalysisJobs => throw new NotSupportedException();

        public IQueryable<Domain.PublicUsers.PublicUser> PublicUsers => throw new NotSupportedException();

        public IQueryable<Domain.PublicUsers.PublicRefreshToken> PublicRefreshTokens => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.AssessmentProgram> AssessmentPrograms => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.ProgramTest> ProgramTests => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.SchoolProgram> SchoolPrograms => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.AnswerOption> AnswerOptions => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.TypeCatalogEntry> TypeCatalog => throw new NotSupportedException();

        public IQueryable<Domain.Catalog.CareerMapEntry> CareerMap => throw new NotSupportedException();

        public IQueryable<AiProviderConfig> AiProviderConfigs => throw new NotSupportedException();

        public IQueryable<AiAnalysis> AiAnalyses => throw new NotSupportedException();

        public IQueryable<PromptTemplate> PromptTemplates => throw new NotSupportedException();

        public IQueryable<AdminUser> AdminUsers => throw new NotSupportedException();

        public IQueryable<RefreshToken> RefreshTokens => throw new NotSupportedException();

        public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw new NotSupportedException();

        public IQueryable<Domain.Schools.RegistrationCounter> RegistrationCounters => throw new NotSupportedException();

        public IQueryable<Domain.Schools.SchoolLinkView> SchoolLinkViews => throw new NotSupportedException();

        public IQueryable<Domain.Settings.RegistrationFormSettings> RegistrationFormSettings => throw new NotSupportedException();
    }

    private sealed class SpyBackgroundJobQueue : IBackgroundJobQueue
    {
        public int EnqueueCallCount { get; private set; }

        public Guid? LastAssessmentId { get; private set; }

        public AiProvider? LastProvider { get; private set; }

        public string? LastPromptVersion { get; private set; }

        public Task EnqueueAiAnalysisAsync(Guid assessmentId, AiProvider? provider = null, string? promptVersion = null, CancellationToken cancellationToken = default)
        {
            EnqueueCallCount++;
            LastAssessmentId = assessmentId;
            LastProvider = provider;
            LastPromptVersion = promptVersion;
            return Task.CompletedTask;
        }
    }

    private sealed class SpyPostCommitActions : IPostCommitActions
    {
        public List<Func<CancellationToken, Task>> EnqueuedActions { get; } = [];

        public void Enqueue(Func<CancellationToken, Task> action) => EnqueuedActions.Add(action);

        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static Assessment CreateAssessmentInStatus(AssessmentStatus targetStatus)
    {
        var assessment = Assessment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "tok-1", "uz", Guid.NewGuid(), Now, Now.AddDays(7), Now);

        var test = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, Guid.NewGuid(), 1, 1);
        assessment.AddTest(test);

        if (targetStatus == AssessmentStatus.Draft)
        {
            return assessment;
        }

        assessment.StartTest(test.TestDefinitionId, Now);
        assessment.CompleteTest(test.TestDefinitionId, [], Now);
        assessment.Complete(Now);

        return assessment;
    }

    /// <summary>
    /// Yangi server-tomoni qo'riqchi (code-review, 2026-09-14) `context.AssessmentTests`/
    /// `TestDefinitions`ga JOIN qiladi — `assessment.Tests`dagi (domen aggregat) yozuvga mos
    /// `TestDefinition` qo'shiladi, `Kind`/`ScoringMode` `PersonalityBattery.Includes`ga mos
    /// keladigan yoki kelmaydigan qilib beriladi.
    /// </summary>
    private static void SeedTestDefinition(
        FakeContext context,
        Assessment assessment,
        StudentRoadMap.Domain.Catalog.TestKind kind,
        StudentRoadMap.Domain.Catalog.TestScoringMode scoringMode)
    {
        foreach (var assessmentTest in assessment.Tests)
        {
            context.AssessmentTestList.Add(assessmentTest);
            context.TestDefinitionList.Add(StudentRoadMap.Domain.Catalog.TestDefinition.Create(
                assessmentTest.TestDefinitionId,
                $"TEST-{assessmentTest.TestDefinitionId:N}",
                "Test nomi",
                displayOrder: 1,
                estimatedMinutes: 5,
                scoringStrategyCode: scoringMode == StudentRoadMap.Domain.Catalog.TestScoringMode.Survey ? null : "SUM",
                now: Now,
                kind: kind,
                isSystem: true,
                scoringMode: scoringMode));
        }
    }

    [Fact]
    public async Task Handle_CompletedAssessment_MarksAnalyzingAndEnqueuesPostCommitAction()
    {
        var context = new FakeContext();
        var assessment = CreateAssessmentInStatus(AssessmentStatus.Completed);
        context.AssessmentList.Add(assessment);
        SeedTestDefinition(context, assessment, StudentRoadMap.Domain.Catalog.TestKind.Standard, StudentRoadMap.Domain.Catalog.TestScoringMode.Scored);

        var jobQueue = new SpyBackgroundJobQueue();
        var postCommitActions = new SpyPostCommitActions();
        var handler = new RerunAnalysisCommandHandler(context, new InlineAsyncQueryExecutor(), new FakeDateTime(Now), new FakeIpHasher(), jobQueue, postCommitActions);

        var result = await handler.Handle(new RerunAnalysisCommand(assessment.Id, AiProvider.Anthropic, "v1.1", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assessment.Status.Should().Be(AssessmentStatus.Analyzing);
        context.SaveChangesCalled.Should().BeTrue();
        context.AuditLogList.Should().ContainSingle(a => a.Action == AuditActions.AssessmentAnalysisRerun);

        // ⚠️ P18-R1 bilan bir xil naqsh: navbat DARHOL emas, `IPostCommitActions.Enqueue` orqali.
        jobQueue.EnqueueCallCount.Should().Be(0, "hali `IPostCommitActions.RunAsync` chaqirilmagan — faqat commit'dan keyin bajarilishi kerak");
        postCommitActions.EnqueuedActions.Should().ContainSingle();

        // Navbatga qo'yilgan amalni QO'LDA bajarib, to'g'ri parametrlar bilan chaqirilganini tekshiramiz.
        await postCommitActions.EnqueuedActions[0](CancellationToken.None);
        jobQueue.EnqueueCallCount.Should().Be(1);
        jobQueue.LastAssessmentId.Should().Be(assessment.Id);
        jobQueue.LastProvider.Should().Be(AiProvider.Anthropic);
        jobQueue.LastPromptVersion.Should().Be("v1.1");
    }

    /// <summary>
    /// ⚠️ Qo'lda ishga tushirish `Ai:AutoAnalyzeOnCompletion` bayrog'idan MUSTAQIL (`docs/06`
    /// 8-bo'lim, 2026-09-03 egasi qarori): bayroq FAQAT avtomatik oqimga
    /// (`CompleteSessionCommandHandler`) tegishli. Buni ikki tomondan qulflaymiz — handler
    /// `IAppSettings`ga UMUMAN bog'lanmaydi (struktura) va har doim navbatga qo'yadi (xatti-harakat).
    /// </summary>
    [Fact]
    public async Task Handle_AutoAnalyzeBayrogidanQatiyNazar_HarDoimNavbatgaQoyadi()
    {
        var constructorParameterTypes = typeof(RerunAnalysisCommandHandler)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(p => p.ParameterType);

        constructorParameterTypes.Should().NotContain(
            typeof(IAppSettings),
            "qo'lda ishga tushirish sozlama bayrog'iga bog'lanmasligi kerak — tugma har doim ishlaydi");

        var context = new FakeContext();
        var assessment = CreateAssessmentInStatus(AssessmentStatus.Completed);
        context.AssessmentList.Add(assessment);
        SeedTestDefinition(context, assessment, StudentRoadMap.Domain.Catalog.TestKind.Standard, StudentRoadMap.Domain.Catalog.TestScoringMode.Scored);

        var jobQueue = new SpyBackgroundJobQueue();
        var postCommitActions = new SpyPostCommitActions();
        var handler = new RerunAnalysisCommandHandler(context, new InlineAsyncQueryExecutor(), new FakeDateTime(Now), new FakeIpHasher(), jobQueue, postCommitActions);

        var result = await handler.Handle(new RerunAnalysisCommand(assessment.Id, null, null, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assessment.Status.Should().Be(AssessmentStatus.Analyzing);
        postCommitActions.EnqueuedActions.Should().ContainSingle();

        await postCommitActions.EnqueuedActions[0](CancellationToken.None);
        jobQueue.EnqueueCallCount.Should().Be(1);
        jobQueue.LastAssessmentId.Should().Be(assessment.Id);
    }

    [Fact]
    public async Task Handle_DraftAssessment_ThrowsDomainException()
    {
        var context = new FakeContext();
        var assessment = CreateAssessmentInStatus(AssessmentStatus.Draft);
        context.AssessmentList.Add(assessment);
        SeedTestDefinition(context, assessment, StudentRoadMap.Domain.Catalog.TestKind.Standard, StudentRoadMap.Domain.Catalog.TestScoringMode.Scored);

        var handler = new RerunAnalysisCommandHandler(context, new InlineAsyncQueryExecutor(), new FakeDateTime(Now), new FakeIpHasher(), new SpyBackgroundJobQueue(), new SpyPostCommitActions());

        var act = async () => await handler.Handle(new RerunAnalysisCommand(assessment.Id, null, null, Guid.NewGuid()), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("ASSESSMENT_INVALID_TRANSITION");
    }

    /// <summary>
    /// Server-tomoni qo'riqchisi (code-review, 2026-09-14): so'rovnoma-only sessiyada
    /// (`Standard`+`Scored` bloki YO'Q) navbatga HECH NARSA qo'shilmaydi, holat o'zgarmaydi —
    /// `MarkAnalyzing`gacha to'xtatiladi.
    /// </summary>
    [Fact]
    public async Task Handle_SorovnomaOnlySessiya_ReturnsAssessmentNoPersonalityBattery_NavbatgaQoshilmaydi()
    {
        var context = new FakeContext();
        var assessment = CreateAssessmentInStatus(AssessmentStatus.Completed);
        context.AssessmentList.Add(assessment);
        SeedTestDefinition(context, assessment, StudentRoadMap.Domain.Catalog.TestKind.Standard, StudentRoadMap.Domain.Catalog.TestScoringMode.Survey);

        var jobQueue = new SpyBackgroundJobQueue();
        var postCommitActions = new SpyPostCommitActions();
        var handler = new RerunAnalysisCommandHandler(context, new InlineAsyncQueryExecutor(), new FakeDateTime(Now), new FakeIpHasher(), jobQueue, postCommitActions);

        var result = await handler.Handle(new RerunAnalysisCommand(assessment.Id, null, null, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(StudentRoadMap.Application.Common.Models.ProblemCodes.AssessmentNoPersonalityBattery);
        assessment.Status.Should().Be(AssessmentStatus.Completed, "holat o'zgarmasligi kerak — `MarkAnalyzing` chaqirilmadi");
        context.SaveChangesCalled.Should().BeFalse("tranzaksiyaga hech narsa yozilmagan");
        context.AuditLogList.Should().BeEmpty();
        postCommitActions.EnqueuedActions.Should().BeEmpty("navbatga HECH NARSA qo'shilmasligi kerak");
        jobQueue.EnqueueCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_AssessmentNotFound_ReturnsNotFound()
    {
        var context = new FakeContext();
        var handler = new RerunAnalysisCommandHandler(context, new InlineAsyncQueryExecutor(), new FakeDateTime(Now), new FakeIpHasher(), new SpyBackgroundJobQueue(), new SpyPostCommitActions());

        var result = await handler.Handle(new RerunAnalysisCommand(Guid.NewGuid(), null, null, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(StudentRoadMap.Application.Common.Models.ProblemCodes.NotFound);
    }
}
