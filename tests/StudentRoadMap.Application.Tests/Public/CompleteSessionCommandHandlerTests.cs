using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Application.Common.Behaviors;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.CompleteSession;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// `CompleteSessionCommandHandler` — AI tahlilini AVTOMATIK navbatga qo'yish bayrog'i
/// (`Ai:AutoAnalyzeOnCompletion`, `IAppSettings.AutoAnalyzeOnCompletion`, standart `false`;
/// `docs/06` 8-bo'lim, 2026-09-03 loyiha EGASI qarori — har tahlil AI xarajati).
///
/// <list type="bullet">
/// <item>Bayroq O'CHIQ (standart): sessiya yakunlanadi, ballar/ishonchlilik/snapshot avvalgidek
/// hisoblanadi, lekin navbatga HECH NARSA qo'yilmaydi va holat `Completed` bo'lib qoladi.</item>
/// <item>Bayroq YOQILGAN: avvalgi xatti-harakat — `Analyzing` + `IPostCommitActions` orqali
/// KECHIKTIRILGAN navbat (P18-R1: darhol emas, commit'dan keyin).</item>
/// <item>Bayroq YOQILGAN + tranzaksiya rollback: navbatga HECH QACHON qo'yilmaydi (P18-R2) —
/// haqiqiy `TransactionBehavior` bilan tekshiriladi.</item>
/// </list>
/// </summary>
public sealed class CompleteSessionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_AutoAnalyzeDisabled_CompletesSessionButNeverEnqueuesAiAnalysis()
    {
        var world = new World(autoAnalyzeOnCompletion: false);

        var result = await world.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(AssessmentStatus.Completed), "bayroq o'chiq bo'lganda sessiya `Analyzing`ga O'TMAYDI");

        // Xabar ham haqiqatni aytishi shart. Ilgari u holatdan qat'i nazar bitta konstanta
        // ("Natijalaringiz qayta ishlanmoqda.") edi — bayroq o'chiq bo'lganda bu O'QUVCHIGA
        // YOLG'ON: u natija o'zi paydo bo'lishini kutib turadi, aslida hech qanday jarayon
        // yo'q va tahlil admin tugmasi bilan boshlanadi (2026-09-03 qarori).
        result.Value.Message.Should().NotContain("qayta ishlanmoqda");
        world.Assessment.Status.Should().Be(AssessmentStatus.Completed);

        world.JobQueue.EnqueueCallCount.Should().Be(0, "AI tahlili faqat admin panelidagi tugma orqali ishga tushadi");
        world.PostCommitActions.EnqueuedActions.Should().BeEmpty("bayroq o'chiq bo'lganda navbat KO'RSATMASI ham berilmaydi");

        // ⚠️ Yakunlash mantiqi (scoring/ishonchlilik/snapshot) bayroqdan MUSTAQIL — o'zgarmaydi.
        world.Assessment.CompletedAt.Should().NotBeNull();
        world.Assessment.TotalDurationSeconds.Should().NotBeNull();
        world.Assessment.ReliabilityScore.Should().NotBeNull();
        world.Student.CompletedAssessmentCount.Should().Be(1);
        world.Student.LastAssessmentAt.Should().Be(Now);
        world.Context.SaveChangesCalled.Should().BeTrue();
    }

    /// <summary>P18-R1: bayroq yoqilganda navbat DARHOL emas — `IPostCommitActions` orqali.</summary>
    [Fact]
    public async Task Handle_AutoAnalyzeEnabled_MarksAnalyzingAndEnqueuesOnlyAfterPostCommitActionsRun()
    {
        var world = new World(autoAnalyzeOnCompletion: true);

        var result = await world.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(AssessmentStatus.Analyzing));
        result.Value.Message.Should().Contain("qayta ishlanmoqda", "tahlil haqiqatan navbatga qo'yilgan — kutish xabari o'rinli");
        world.Assessment.Status.Should().Be(AssessmentStatus.Analyzing);

        world.JobQueue.EnqueueCallCount.Should().Be(0, "hali `IPostCommitActions.RunAsync` chaqirilmagan — P18-R1");
        world.PostCommitActions.EnqueuedActions.Should().ContainSingle();

        await world.PostCommitActions.RunAsync(CancellationToken.None);

        world.JobQueue.EnqueueCallCount.Should().Be(1);
        world.JobQueue.LastAssessmentId.Should().Be(world.Assessment.Id);
    }

    /// <summary>
    /// P18-R2, bayroq YOQILGAN holat uchun: haqiqiy `TransactionBehavior` + haqiqiy handler —
    /// commit muvaffaqiyatsiz bo'lsa (rollback) navbatga HECH QACHON qo'yilmaydi.
    /// </summary>
    [Fact]
    public async Task Handle_AutoAnalyzeEnabled_TransactionRollsBack_NeverEnqueuesAiAnalysis()
    {
        var world = new World(autoAnalyzeOnCompletion: true);
        world.Context.FailCommit = true;

        var behavior = new TransactionBehavior<CompleteSessionCommand, Domain.Common.Result<CompleteSessionResult>>(
            world.Context,
            world.PostCommitActions,
            NullLogger<TransactionBehavior<CompleteSessionCommand, Domain.Common.Result<CompleteSessionResult>>>.Instance);

        var command = new CompleteSessionCommand(world.Assessment.Id);

        var act = async () => await behavior.Handle(
            command,
            _ => world.Handler.Handle(command, CancellationToken.None),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        world.Context.LastTransaction!.RolledBack.Should().BeTrue();
        world.Context.LastTransaction.Committed.Should().BeFalse();
        world.PostCommitActions.RunAsyncCallCount.Should().Be(0, "rollback bo'lganda `RunAsync` UMUMAN chaqirilmaydi");
        world.JobQueue.EnqueueCallCount.Should().Be(0, "P18-R2: commit bo'lmagan sessiya uchun vazifa navbatda QOLMAYDI");
    }

    /// <summary>
    /// Bitta yakunlangan test bloki, bitta o'quvchi va handler'ning barcha bog'liqliklari —
    /// testlar faqat bayroq qiymati bilan farq qiladi.
    /// </summary>
    private sealed class World
    {
        public World(bool autoAnalyzeOnCompletion)
        {
            var schoolId = Guid.NewGuid();
            Student = Student.Create(
                Guid.NewGuid(),
                schoolId,
                "Egamov Diyorbek Shuxratovich",
                new DateOnly(2010, 1, 1),
                Gender.Male,
                9,
                PhoneNumber.Create("901234567").Value,
                Now.AddMinutes(-30),
                Now.AddMinutes(-30));

            var testDefinition = TestDefinition.Create(
                Guid.NewGuid(),
                "SRVY",
                "Sinov anketasi",
                displayOrder: 1,
                estimatedMinutes: 5,
                scoringStrategyCode: null,
                now: Now.AddMinutes(-30),
                scoringMode: TestScoringMode.Survey);

            Assessment = Assessment.Create(
                Guid.NewGuid(),
                Student.Id,
                schoolId,
                "sessiya-tokeni-1",
                "uz",
                Guid.NewGuid(),
                Now.AddMinutes(-20),
                Now.AddDays(7),
                Now.AddMinutes(-20));

            var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), Assessment.Id, testDefinition.Id, displayOrder: 1, totalCount: 1);
            Assessment.AddTest(assessmentTest);
            Assessment.StartTest(testDefinition.Id, Now.AddMinutes(-20));
            Assessment.CompleteTest(testDefinition.Id, [], Now.AddMinutes(-1));

            Context = new FakeCompleteSessionAppDbContext();
            // P47: makon (maktab) yozuvi kerak — javobdagi `showResultToStudent` GLOBAL
            // bayroq VA `School.ShowResultToStudent` birlashmasi (`ShowResultPolicy`).
            Context.SchoolList.Add(School.Create(
                schoolId,
                "Sinov maktabi",
                "Toshkent",
                "Chilonzor",
                SchoolSlug.Create("sinov-maktabi").Value,
                "access-token-sinov-0123456789abcdef",
                "SNVM2345",
                Now.AddDays(-30)));
            Context.AssessmentList.Add(Assessment);
            Context.AssessmentTestList.Add(assessmentTest);
            Context.TestDefinitionList.Add(testDefinition);
            Context.StudentList.Add(Student);

            JobQueue = new SpyBackgroundJobQueue();
            PostCommitActions = new SpyPostCommitActions();

            Handler = new CompleteSessionCommandHandler(
                Context,
                new InlineAsyncQueryExecutor(),
                new FakeDateTime(Now),
                new FakeCompleteSessionAppSettings(autoAnalyzeOnCompletion),
                JobQueue,
                PostCommitActions);
        }

        public Assessment Assessment { get; }

        public Student Student { get; }

        public FakeCompleteSessionAppDbContext Context { get; }

        public SpyBackgroundJobQueue JobQueue { get; }

        public SpyPostCommitActions PostCommitActions { get; }

        public CompleteSessionCommandHandler Handler { get; }

        public Task<Domain.Common.Result<CompleteSessionResult>> HandleAsync() =>
            Handler.Handle(new CompleteSessionCommand(Assessment.Id), CancellationToken.None);
    }

    private sealed class FakeCompleteSessionAppSettings : IAppSettings
    {
        public FakeCompleteSessionAppSettings(bool autoAnalyzeOnCompletion) => AutoAnalyzeOnCompletion = autoAnalyzeOnCompletion;

        public int SessionLifetimeDays => 7;

        public bool ShowResultToStudent => false;

        public bool AutoAnalyzeOnCompletion { get; }

        public int RefreshTokenDays => 14;

        public string PublicWebBaseUrl => "https://16shaxsiyat.uz";
    }

    private sealed class SpyBackgroundJobQueue : IBackgroundJobQueue
    {
        public int EnqueueCallCount { get; private set; }

        public Guid? LastAssessmentId { get; private set; }

        public Task EnqueueAiAnalysisAsync(Guid assessmentId, AiProvider? provider = null, string? promptVersion = null, CancellationToken cancellationToken = default)
        {
            EnqueueCallCount++;
            LastAssessmentId = assessmentId;
            return Task.CompletedTask;
        }
    }

    private sealed class SpyPostCommitActions : IPostCommitActions
    {
        public List<Func<CancellationToken, Task>> EnqueuedActions { get; } = [];

        public int RunAsyncCallCount { get; private set; }

        public void Enqueue(Func<CancellationToken, Task> action) => EnqueuedActions.Add(action);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            RunAsyncCallCount++;
            foreach (var action in EnqueuedActions)
            {
                await action(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class FakeTransaction : IAppDbContextTransaction
    {
        private readonly bool _failCommit;

        public FakeTransaction(bool failCommit) => _failCommit = failCommit;

        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_failCommit)
            {
                throw new InvalidOperationException("Commit muvaffaqiyatsiz (sinov).");
            }

            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// `CompleteSessionCommandHandler` TEGADIGAN jadvallar xotirada; qolgan hammasi
    /// `NotSupportedException` (chaqirilsa test SINIQ ekanini bildiradi).
    /// </summary>
    private sealed class FakeCompleteSessionAppDbContext : IAppDbContext
    {
        public List<Assessment> AssessmentList { get; } = [];

        public List<AssessmentTest> AssessmentTestList { get; } = [];

        public List<TestResult> TestResultList { get; } = [];

        public List<TestDefinition> TestDefinitionList { get; } = [];

        public List<Question> QuestionList { get; } = [];

        public List<Answer> AnswerList { get; } = [];

        public List<Student> StudentList { get; } = [];

        /// <summary>P47: `CompleteSessionCommandHandler` javobdagi `showResultToStudent` uchun MAKON bayrog'ini o'qiydi.</summary>
        public List<School> SchoolList { get; } = [];

        public bool SaveChangesCalled { get; private set; }

        public bool FailCommit { get; set; }

        public FakeTransaction? LastTransaction { get; private set; }

        public IQueryable<Assessment> Assessments => AssessmentList.AsQueryable();

        public IQueryable<AssessmentTest> AssessmentTests => AssessmentTestList.AsQueryable();

        public IQueryable<TestResult> TestResults => TestResultList.AsQueryable();

        public IQueryable<TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

        public IQueryable<Question> Questions => QuestionList.AsQueryable();

        public IQueryable<Answer> Answers => AnswerList.AsQueryable();

        public IQueryable<Student> Students => StudentList.AsQueryable();

        public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

        public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

        public void Add<TEntity>(TEntity entity) where TEntity : class => throw Unsupported();

        public void Remove<TEntity>(TEntity entity) where TEntity : class => throw Unsupported();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.FromResult(1);
        }

        public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            LastTransaction = new FakeTransaction(FailCommit);
            return Task.FromResult<IAppDbContextTransaction>(LastTransaction);
        }

        private static NotSupportedException Unsupported() => new("CompleteSessionCommandHandlerTests: bu a'zo chaqirilmasligi kerak edi.");

        public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw Unsupported();

        public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw Unsupported();

        public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) => throw Unsupported();

        public IQueryable<School> Schools => SchoolList.AsQueryable();

        public IQueryable<TestScale> TestScales => throw Unsupported();

        public IQueryable<QuestionSection> QuestionSections => throw Unsupported();

        public IQueryable<AnalysisJob> AnalysisJobs => throw Unsupported();

        public IQueryable<PublicUser> PublicUsers => throw Unsupported();

        public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw Unsupported();

        public IQueryable<AssessmentProgram> AssessmentPrograms => throw Unsupported();

        public IQueryable<ProgramTest> ProgramTests => throw Unsupported();

        public IQueryable<SchoolProgram> SchoolPrograms => throw Unsupported();

        public IQueryable<AnswerOption> AnswerOptions => throw Unsupported();

        public IQueryable<TypeCatalogEntry> TypeCatalog => throw Unsupported();

        public IQueryable<CareerMapEntry> CareerMap => throw Unsupported();

        public IQueryable<AiProviderConfig> AiProviderConfigs => throw Unsupported();

        public IQueryable<AiAnalysis> AiAnalyses => throw Unsupported();

        public IQueryable<PromptTemplate> PromptTemplates => throw Unsupported();

        public IQueryable<AdminUser> AdminUsers => throw Unsupported();

        public IQueryable<RefreshToken> RefreshTokens => throw Unsupported();

        public IQueryable<AuditLog> AuditLogs => throw Unsupported();

        public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw Unsupported();

        public IQueryable<RegistrationCounter> RegistrationCounters => throw Unsupported();

        public IQueryable<SchoolLinkView> SchoolLinkViews => throw Unsupported();
    }
}
