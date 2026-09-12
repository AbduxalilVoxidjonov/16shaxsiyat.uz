using System.Linq.Expressions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Admin.Assessments.Testing;

/// <summary>
/// `GetAssessmentAnswersQueryHandler` testlari uchun soxta `IAppDbContext`
/// (`FakeCatalogQuestionsAppDbContext` bilan bir xil naqsh) — faqat handler o'qiydigan
/// jadvallar xotirada saqlanadi, qolgan hammasi `NotSupportedException` otadi.
/// </summary>
internal sealed class FakeAssessmentAnswersAppDbContext : IAppDbContext
{
    public List<Assessment> AssessmentList { get; } = [];

    public List<AssessmentTest> AssessmentTestList { get; } = [];

    public List<TestDefinition> TestDefinitionList { get; } = [];

    public List<Question> QuestionList { get; } = [];

    public List<TestScale> TestScaleList { get; } = [];

    public List<AnswerOption> AnswerOptionList { get; } = [];

    public List<Answer> AnswerList { get; } = [];

    public IQueryable<Assessment> Assessments => AssessmentList.AsQueryable();

    public IQueryable<AssessmentTest> AssessmentTests => AssessmentTestList.AsQueryable();

    public IQueryable<TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

    public IQueryable<Question> Questions => QuestionList.AsQueryable();

    public IQueryable<TestScale> TestScales => TestScaleList.AsQueryable();

    public IQueryable<AnswerOption> AnswerOptions => AnswerOptionList.AsQueryable();

    public IQueryable<Answer> Answers => AnswerList.AsQueryable();

    public IQueryable<QuestionSection> QuestionSections => throw new NotSupportedException();

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public void Add<TEntity>(TEntity entity) where TEntity : class =>
        throw new NotSupportedException("Bu testlarda `Add<TEntity>` chaqirilmasligi kerak (faqat o'qish).");

    public void Remove<TEntity>(TEntity entity) where TEntity : class =>
        throw new NotSupportedException("Bu testlarda `Remove<TEntity>` chaqirilmasligi kerak (faqat o'qish).");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public IQueryable<School> Schools => throw new NotSupportedException();

    public IQueryable<Student> Students => throw new NotSupportedException();

    public IQueryable<TestResult> TestResults => throw new NotSupportedException();

    public IQueryable<AssessmentProgram> AssessmentPrograms => throw new NotSupportedException();

    public IQueryable<ProgramTest> ProgramTests => throw new NotSupportedException();

    public IQueryable<SchoolProgram> SchoolPrograms => throw new NotSupportedException();

    public IQueryable<TypeCatalogEntry> TypeCatalog => throw new NotSupportedException();

    public IQueryable<CareerMapEntry> CareerMap => throw new NotSupportedException();

    public IQueryable<AiProviderConfig> AiProviderConfigs => throw new NotSupportedException();

    public IQueryable<AiAnalysis> AiAnalyses => throw new NotSupportedException();

    public IQueryable<PromptTemplate> PromptTemplates => throw new NotSupportedException();

    public IQueryable<AdminUser> AdminUsers => throw new NotSupportedException();

    public IQueryable<RefreshToken> RefreshTokens => throw new NotSupportedException();

    public IQueryable<AuditLog> AuditLogs => throw new NotSupportedException();

    public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw new NotSupportedException();

    public IQueryable<RegistrationCounter> RegistrationCounters => throw new NotSupportedException();

    public IQueryable<SchoolLinkView> SchoolLinkViews => throw new NotSupportedException();

    public IQueryable<Domain.Settings.RegistrationFormSettings> RegistrationFormSettings => throw new NotSupportedException();

    public IQueryable<AnalysisJob> AnalysisJobs => throw new NotSupportedException();

    public IQueryable<PublicUser> PublicUsers => throw new NotSupportedException();

    public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw new NotSupportedException();
}

/// <summary>LINQ-to-Objects ustida bevosita bajaradi — EF Core/DB yo'q (`SchoolsInlineAsyncQueryExecutor` bilan bir xil naqsh).</summary>
internal sealed class AssessmentAnswersInlineAsyncQueryExecutor : IAsyncQueryExecutor
{
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => Task.FromResult(query.ToList());

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => Task.FromResult(query.FirstOrDefault());

    public Task<T> SingleAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => Task.FromResult(query.Single());

    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => Task.FromResult(query.SingleOrDefault());

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => Task.FromResult(query.Count());

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => Task.FromResult(query.Any());

    public Task<int> SumAsync(IQueryable<int> query, CancellationToken cancellationToken = default) => Task.FromResult(query.Sum());

    public Task<int> SumAsync<T>(IQueryable<T> query, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default) => Task.FromResult(query.Sum(selector));

    public Task<decimal> SumAsync<T>(IQueryable<T> query, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Task.FromResult(query.Sum(selector));
}

/// <summary>
/// `AssessmentAnswersInlineAsyncQueryExecutor`ni o'rab, `ToListAsync` chaqiruvlari sonini
/// hisoblaydi — `GetAssessmentAnswersQueryHandler.LoadRowsAsync` dagi `MultiChoice` variant
/// matnlari haqiqatan HAM bitta batch so'rov (N+1 EMAS) ekanini tasdiqlash uchun
/// (egasi topgan kamchilik, 2026-09-12, ADR-11).
/// </summary>
internal sealed class CountingAssessmentAnswersInlineAsyncQueryExecutor : IAsyncQueryExecutor
{
    private readonly AssessmentAnswersInlineAsyncQueryExecutor _inner = new();

    public int ToListAsyncCallCount { get; private set; }

    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        ToListAsyncCallCount++;
        return _inner.ToListAsync(query, cancellationToken);
    }

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => _inner.FirstOrDefaultAsync(query, cancellationToken);

    public Task<T> SingleAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => _inner.SingleAsync(query, cancellationToken);

    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => _inner.SingleOrDefaultAsync(query, cancellationToken);

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => _inner.CountAsync(query, cancellationToken);

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) => _inner.AnyAsync(query, cancellationToken);

    public Task<int> SumAsync(IQueryable<int> query, CancellationToken cancellationToken = default) => _inner.SumAsync(query, cancellationToken);

    public Task<int> SumAsync<T>(IQueryable<T> query, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default) => _inner.SumAsync(query, selector, cancellationToken);

    public Task<decimal> SumAsync<T>(IQueryable<T> query, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => _inner.SumAsync(query, selector, cancellationToken);
}
