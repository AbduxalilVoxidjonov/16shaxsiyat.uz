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

namespace StudentRoadMap.Application.Tests.Admin.Catalog.Testing;

/// <summary>
/// `Application.Admin.Catalog.Questions.*` handler testlari uchun soxta `IAppDbContext`
/// (`FakeAiAppDbContext` bilan bir xil naqsh) — faqat `TestDefinitions`/`Questions`/
/// `QuestionSections`/`TestScales`/`AnswerOptions`/`Answers`/`AuditLogs` xotirada saqlanadi,
/// qolgan hammasi (bu handlerlar TEGMAYDIGAN) `NotSupportedException` otadi.
/// </summary>
internal sealed class FakeCatalogQuestionsAppDbContext : IAppDbContext
{
    public List<TestDefinition> TestDefinitionList { get; } = [];

    public List<Question> QuestionList { get; } = [];

    public List<QuestionSection> QuestionSectionList { get; } = [];

    public List<TestScale> TestScaleList { get; } = [];

    public List<AnswerOption> AnswerOptionList { get; } = [];

    public List<Answer> AnswerList { get; } = [];

    public List<AuditLog> AuditLogList { get; } = [];

    public bool SaveChangesCalled { get; private set; }

    public IQueryable<TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

    public IQueryable<Question> Questions => QuestionList.AsQueryable();

    public IQueryable<QuestionSection> QuestionSections => QuestionSectionList.AsQueryable();

    public IQueryable<TestScale> TestScales => TestScaleList.AsQueryable();

    public IQueryable<AnswerOption> AnswerOptions => AnswerOptionList.AsQueryable();

    public IQueryable<Answer> Answers => AnswerList.AsQueryable();

    public IQueryable<AuditLog> AuditLogs => AuditLogList.AsQueryable();

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public void Add<TEntity>(TEntity entity) where TEntity : class
    {
        switch (entity)
        {
            case AuditLog auditLog:
                AuditLogList.Add(auditLog);
                break;
            case AnswerOption option:
                AnswerOptionList.Add(option);
                break;
            default:
                throw new NotSupportedException($"FakeCatalogQuestionsAppDbContext: '{typeof(TEntity).Name}' qo'shish qo'llab-quvvatlanmaydi.");
        }
    }

    public void Remove<TEntity>(TEntity entity) where TEntity : class =>
        throw new NotSupportedException("Bu testlarda `Remove<TEntity>` chaqirilmasligi kerak (domen kolleksiyadan o'zi olib tashlaydi).");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }

    public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public IQueryable<School> Schools => throw new NotSupportedException();

    public IQueryable<Student> Students => throw new NotSupportedException();

    public IQueryable<Assessment> Assessments => throw new NotSupportedException();

    public IQueryable<AssessmentTest> AssessmentTests => throw new NotSupportedException();

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

    public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw new NotSupportedException();

    public IQueryable<RegistrationCounter> RegistrationCounters => throw new NotSupportedException();

    public IQueryable<SchoolLinkView> SchoolLinkViews => throw new NotSupportedException();

    public IQueryable<AnalysisJob> AnalysisJobs => throw new NotSupportedException();

    public IQueryable<PublicUser> PublicUsers => throw new NotSupportedException();

    public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw new NotSupportedException();
}

/// <summary>LINQ-to-Objects ustida bevosita bajaradi — EF Core/DB yo'q (`SchoolsInlineAsyncQueryExecutor` bilan bir xil naqsh).</summary>
internal sealed class CatalogQuestionsInlineAsyncQueryExecutor : IAsyncQueryExecutor
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
/// `CatalogQuestionsInlineAsyncQueryExecutor`ni o'rab, `ToListAsync` chaqiruvlari sonini
/// hisoblaydi — `CatalogMapping.LoadQuestionIdsWithAnswersAsync` haqiqatan HAM bitta batch
/// so'rov (N+1 EMAS) ekanini tasdiqlash uchun (P52, ADR-11).
/// </summary>
internal sealed class CountingCatalogQuestionsInlineAsyncQueryExecutor : IAsyncQueryExecutor
{
    private readonly CatalogQuestionsInlineAsyncQueryExecutor _inner = new();

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

internal sealed class FakeCatalogDateTime : IDateTime
{
    public FakeCatalogDateTime(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; }
}

internal sealed class FakeCatalogIpHasher : IIpHasher
{
    public string? Hash(string? ipAddress) => ipAddress is null ? null : $"hashed:{ipAddress}";
}
