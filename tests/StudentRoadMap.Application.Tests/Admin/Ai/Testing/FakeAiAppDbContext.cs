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

namespace StudentRoadMap.Application.Tests.Admin.Ai.Testing;

/// <summary>
/// `Application.Admin.Ai` handler testlari uchun umumiy soxta `IAppDbContext` — faqat
/// `AiProviderConfigs`/`PromptTemplates`/`AiAnalyses`/`AuditLogs` ro'yxatlarni xotirada saqlaydi
/// (`Add`/`SaveChangesAsync` haqiqiy ro'yxatga qo'shadi), qolgan hammasi (bu handlerlar
/// TEGMAYDIGAN) `NotSupportedException` otadi.
/// </summary>
internal sealed class FakeAiAppDbContext : IAppDbContext
{
    public List<AiProviderConfig> ProviderConfigList { get; } = [];

    public List<PromptTemplate> PromptTemplateList { get; } = [];

    public List<AiAnalysis> AiAnalysisList { get; } = [];

    public List<AuditLog> AuditLogList { get; } = [];

    public List<object> Added { get; } = [];

    public bool SaveChangesCalled { get; private set; }

    public IQueryable<AiProviderConfig> AiProviderConfigs => ProviderConfigList.AsQueryable();

    public IQueryable<PromptTemplate> PromptTemplates => PromptTemplateList.AsQueryable();

    public IQueryable<AiAnalysis> AiAnalyses => AiAnalysisList.AsQueryable();

    public IQueryable<AuditLog> AuditLogs => AuditLogList.AsQueryable();

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

    public void Add<TEntity>(TEntity entity) where TEntity : class
    {
        switch (entity)
        {
            case AiProviderConfig config:
                ProviderConfigList.Add(config);
                break;
            case PromptTemplate template:
                PromptTemplateList.Add(template);
                break;
            case AiAnalysis analysis:
                AiAnalysisList.Add(analysis);
                break;
            case AuditLog auditLog:
                AuditLogList.Add(auditLog);
                break;
            default:
                Added.Add(entity);
                break;
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

    public IQueryable<School> Schools => throw new NotSupportedException();

    public IQueryable<Student> Students => throw new NotSupportedException();

    public IQueryable<Assessment> Assessments => throw new NotSupportedException();

    public IQueryable<AssessmentTest> AssessmentTests => throw new NotSupportedException();

    public IQueryable<Answer> Answers => throw new NotSupportedException();

    public IQueryable<TestResult> TestResults => throw new NotSupportedException();

    public IQueryable<TestDefinition> TestDefinitions => throw new NotSupportedException();

    public IQueryable<Question> Questions => throw new NotSupportedException();

    public IQueryable<TestScale> TestScales => throw new NotSupportedException();

    public IQueryable<AnalysisJob> AnalysisJobs => throw new NotSupportedException();

    public IQueryable<PublicUser> PublicUsers => throw new NotSupportedException();

    public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw new NotSupportedException();

    public IQueryable<AssessmentProgram> AssessmentPrograms => throw new NotSupportedException();

    public IQueryable<ProgramTest> ProgramTests => throw new NotSupportedException();

    public IQueryable<SchoolProgram> SchoolPrograms => throw new NotSupportedException();

    public IQueryable<AnswerOption> AnswerOptions => throw new NotSupportedException();

    public IQueryable<TypeCatalogEntry> TypeCatalog => throw new NotSupportedException();

    public IQueryable<CareerMapEntry> CareerMap => throw new NotSupportedException();

    public IQueryable<AdminUser> AdminUsers => throw new NotSupportedException();

    public IQueryable<RefreshToken> RefreshTokens => throw new NotSupportedException();

    public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw new NotSupportedException();

    public IQueryable<RegistrationCounter> RegistrationCounters => throw new NotSupportedException();

    public IQueryable<SchoolLinkView> SchoolLinkViews => throw new NotSupportedException();
}

internal sealed class InlineAsyncQueryExecutor : IAsyncQueryExecutor
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

internal sealed class FakeDateTime : IDateTime
{
    public FakeDateTime(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; }
}

internal sealed class FakeIpHasher : IIpHasher
{
    public string? Hash(string? ipAddress) => ipAddress is null ? null : $"hashed:{ipAddress}";
}

internal sealed class FakeEncryptionServiceForAiTests : IEncryptionService
{
    public string Encrypt(string plainText) => $"enc:{plainText}";

    public string Decrypt(string cipherText) => cipherText.StartsWith("enc:", StringComparison.Ordinal) ? cipherText[4..] : cipherText;
}
