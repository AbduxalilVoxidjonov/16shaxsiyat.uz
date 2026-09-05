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

namespace StudentRoadMap.Application.Tests.Admin.Students.Testing;

/// <summary>
/// `StudentProfileMapping.BuildTestResultsAsync` uchun soxta `IAppDbContext` — u faqat TO'RTTA
/// jadvalga murojaat qiladi: `AssessmentTests` + `TestDefinitions` (batareya ROLI xaritasi,
/// `PersonalityBatteryRoles.LoadByAssessmentTestIdAsync`), `TypeCatalog` (`typeName`) va
/// `CareerMap` (kasb yo'nalishlari).
///
/// <para>
/// `FakeSchoolsAppDbContext` bilan bir xil naqsh: tegilmasligi kerak bo'lgan HAMMA narsa
/// `NotSupportedException` otadi — shu bilan "xarita bitta so'rov bilan yuklanadi, N+1 yo'q"
/// shartidan chetga chiqish (masalan har natija uchun alohida `TestResults` so'rovi) sinovda
/// darhol ko'rinadi.
/// </para>
/// </summary>
internal sealed class FakeStudentProfileAppDbContext : IAppDbContext
{
    public List<AssessmentTest> AssessmentTestList { get; } = [];

    public List<TestDefinition> TestDefinitionList { get; } = [];

    public List<TypeCatalogEntry> TypeCatalogList { get; } = [];

    public List<CareerMapEntry> CareerMapList { get; } = [];

    public IQueryable<AssessmentTest> AssessmentTests => AssessmentTestList.AsQueryable();

    public IQueryable<TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

    public IQueryable<TypeCatalogEntry> TypeCatalog => TypeCatalogList.AsQueryable();

    public IQueryable<CareerMapEntry> CareerMap => CareerMapList.AsQueryable();

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query;

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query;

    public void Add<TEntity>(TEntity entity)
        where TEntity : class => throw new NotSupportedException("Moslashtirish (mapping) yozish amalini chaqirmasligi kerak.");

    public void Remove<TEntity>(TEntity entity)
        where TEntity : class => throw new NotSupportedException("Moslashtirish (mapping) yozish amalini chaqirmasligi kerak.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Moslashtirish (mapping) `SaveChangesAsync` chaqirmasligi kerak.");

    public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IQueryable<School> Schools => throw new NotSupportedException();

    public IQueryable<Student> Students => throw new NotSupportedException();

    public IQueryable<Assessment> Assessments => throw new NotSupportedException();

    public IQueryable<Answer> Answers => throw new NotSupportedException();

    public IQueryable<TestResult> TestResults => throw new NotSupportedException();

    public IQueryable<Question> Questions => throw new NotSupportedException();

    public IQueryable<TestScale> TestScales => throw new NotSupportedException();

    public IQueryable<AnalysisJob> AnalysisJobs => throw new NotSupportedException();

    public IQueryable<PublicUser> PublicUsers => throw new NotSupportedException();

    public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw new NotSupportedException();

    public IQueryable<AssessmentProgram> AssessmentPrograms => throw new NotSupportedException();

    public IQueryable<ProgramTest> ProgramTests => throw new NotSupportedException();

    public IQueryable<SchoolProgram> SchoolPrograms => throw new NotSupportedException();

    public IQueryable<AnswerOption> AnswerOptions => throw new NotSupportedException();

    public IQueryable<AiProviderConfig> AiProviderConfigs => throw new NotSupportedException();

    public IQueryable<AiAnalysis> AiAnalyses => throw new NotSupportedException();

    public IQueryable<PromptTemplate> PromptTemplates => throw new NotSupportedException();

    public IQueryable<AdminUser> AdminUsers => throw new NotSupportedException();

    public IQueryable<RefreshToken> RefreshTokens => throw new NotSupportedException();

    public IQueryable<AuditLog> AuditLogs => throw new NotSupportedException();

    public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw new NotSupportedException();

    public IQueryable<RegistrationCounter> RegistrationCounters => throw new NotSupportedException();

    public IQueryable<SchoolLinkView> SchoolLinkViews => throw new NotSupportedException();
}

/// <summary>LINQ-to-Objects ustida bevosita bajaradi — EF Core/DB yo'q.</summary>
internal sealed class StudentProfileInlineAsyncQueryExecutor : IAsyncQueryExecutor
{
    /// <summary>Bajarilgan so'rovlar soni — N+1 yo'qligini tasdiqlash uchun.</summary>
    public int QueryCount { get; private set; }

    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.ToList());
    }

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.FirstOrDefault());
    }

    public Task<T> SingleAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.Single());
    }

    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.SingleOrDefault());
    }

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.Count());
    }

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.Any());
    }

    public Task<int> SumAsync(IQueryable<int> query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.Sum());
    }

    public Task<int> SumAsync<T>(IQueryable<T> query, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.Sum(selector));
    }

    public Task<decimal> SumAsync<T>(IQueryable<T> query, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        return Task.FromResult(query.Sum(selector));
    }
}
