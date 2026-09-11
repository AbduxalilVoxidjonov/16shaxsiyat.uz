using System.Runtime.CompilerServices;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Public.Testing;

/// <summary>
/// P52 (`docs/18`) — `GetTestQuestionsQueryHandler`/`SaveAnswersCommandHandler`/
/// `CompleteTestCommandHandler` sinovlari uchun soxta `IAppDbContext`: faqat shu handlerlar
/// tegadigan jadvallar xotirada saqlanadi (`FakeAiAppDbContext`/`FakeCompleteSessionAppDbContext`
/// bilan bir xil naqsh) — qolgan hammasi chaqirilsa test darhol yiqiladi.
/// </summary>
internal sealed class FakeBranchingSurveyAppDbContext : IAppDbContext
{
    public List<Assessment> AssessmentList { get; } = [];

    public List<AssessmentTest> AssessmentTestList { get; } = [];

    public List<Answer> AnswerList { get; } = [];

    public List<TestDefinition> TestDefinitionList { get; } = [];

    public List<Question> QuestionList { get; } = [];

    public List<QuestionSection> QuestionSectionList { get; } = [];

    public List<AnswerOption> AnswerOptionList { get; } = [];

    public List<TestResult> TestResultList { get; } = [];

    public bool SaveChangesCalled { get; private set; }

    public IQueryable<Assessment> Assessments => AssessmentList.AsQueryable();

    public IQueryable<AssessmentTest> AssessmentTests => AssessmentTestList.AsQueryable();

    public IQueryable<Answer> Answers => AnswerList.AsQueryable();

    public IQueryable<TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

    public IQueryable<Question> Questions => QuestionList.AsQueryable();

    public IQueryable<QuestionSection> QuestionSections => QuestionSectionList.AsQueryable();

    public IQueryable<AnswerOption> AnswerOptions => AnswerOptionList.AsQueryable();

    public IQueryable<TestResult> TestResults => TestResultList.AsQueryable();

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public void Add<TEntity>(TEntity entity) where TEntity : class
    {
        switch (entity)
        {
            case Answer answer:
                AnswerList.Add(answer);
                break;
            case TestResult testResult:
                TestResultList.Add(testResult);
                break;
            default:
                throw Unsupported();
        }
    }

    public void Remove<TEntity>(TEntity entity) where TEntity : class => throw Unsupported();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }

    public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw Unsupported();

    public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw Unsupported();

    public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw Unsupported();

    public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) => throw Unsupported();

    public IQueryable<School> Schools => throw Unsupported();

    public IQueryable<Student> Students => throw Unsupported();

    public IQueryable<TestScale> TestScales => throw Unsupported();

    public IQueryable<AssessmentProgram> AssessmentPrograms => throw Unsupported();

    public IQueryable<ProgramTest> ProgramTests => throw Unsupported();

    public IQueryable<SchoolProgram> SchoolPrograms => throw Unsupported();

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

    public IQueryable<Domain.Settings.RegistrationFormSettings> RegistrationFormSettings => throw Unsupported();

    public IQueryable<AnalysisJob> AnalysisJobs => throw Unsupported();

    public IQueryable<PublicUser> PublicUsers => throw Unsupported();

    public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw Unsupported();

    private static NotSupportedException Unsupported([CallerMemberName] string? member = null) =>
        new($"FakeBranchingSurveyAppDbContext: '{member}' bu testda ishlatilmasligi kerak edi.");
}
