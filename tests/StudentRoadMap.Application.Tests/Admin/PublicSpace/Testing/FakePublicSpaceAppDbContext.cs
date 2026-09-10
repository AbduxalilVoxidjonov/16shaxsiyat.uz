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

namespace StudentRoadMap.Application.Tests.Admin.PublicSpace.Testing;

/// <summary>
/// `Admin/PublicSpace/ListUsers` handleri uchun soxta `IAppDbContext` (`FakeSchoolsAppDbContext`
/// bilan bir xil naqsh — loyihada mocking kutubxonasi yo'q). Handler tegadigan 5 jadval
/// xotirada, qolgani `NotSupportedException` — "ortiqcha jadvalga murojaat yo'q" ham sinaladi.
///
/// **Global filtrlar taqlid qilinadi:** `Students`/`Assessments` — `!IsDeleted`. `PublicUsers`
/// FILTRLANMAYDI (2026-09-08): handler HAR DOIM `IgnoreQueryFilters(_context.PublicUsers)`
/// chaqiradi (o'chirilgan akkauntlar ham ro'yxatga kirishi kerak) — real EF'da bu chaqiruv
/// global filtrni olib tashlaydi, shu sabab bu yerda ham boshidanoq filtrsiz.
/// `AssessmentTests` — `Assessment.Tests` navigatsiyasidan yig'iladi (EF'da alohida jadval).
/// </summary>
internal sealed class FakePublicSpaceAppDbContext : IAppDbContext
{
    public List<PublicUser> PublicUserList { get; } = [];

    public List<Student> StudentList { get; } = [];

    public List<Assessment> AssessmentList { get; } = [];

    public List<TestDefinition> TestDefinitionList { get; } = [];

    public IQueryable<PublicUser> PublicUsers => PublicUserList.AsQueryable();

    public IQueryable<Student> Students => StudentList.Where(s => !s.IsDeleted).AsQueryable();

    public IQueryable<Assessment> Assessments => AssessmentList.Where(a => !a.IsDeleted).AsQueryable();

    public IQueryable<AssessmentTest> AssessmentTests => AssessmentList.SelectMany(a => a.Tests).AsQueryable();

    public IQueryable<TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query;

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query;

    public void Add<TEntity>(TEntity entity)
        where TEntity : class => throw new NotSupportedException("O'qish handeri yozish amalini chaqirmasligi kerak.");

    public void Remove<TEntity>(TEntity entity)
        where TEntity : class => throw new NotSupportedException("O'qish handeri yozish amalini chaqirmasligi kerak.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("O'qish handeri `SaveChangesAsync` chaqirmasligi kerak.");

    public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IQueryable<School> Schools => throw new NotSupportedException();

    public IQueryable<Answer> Answers => throw new NotSupportedException();

    public IQueryable<TestResult> TestResults => throw new NotSupportedException();

    public IQueryable<Question> Questions => throw new NotSupportedException();

    public IQueryable<TestScale> TestScales => throw new NotSupportedException();

    public IQueryable<QuestionSection> QuestionSections => throw new NotSupportedException();

    public IQueryable<AnalysisJob> AnalysisJobs => throw new NotSupportedException();

    public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw new NotSupportedException();

    public IQueryable<AssessmentProgram> AssessmentPrograms => throw new NotSupportedException();

    public IQueryable<ProgramTest> ProgramTests => throw new NotSupportedException();

    public IQueryable<SchoolProgram> SchoolPrograms => throw new NotSupportedException();

    public IQueryable<AnswerOption> AnswerOptions => throw new NotSupportedException();

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
}

/// <summary>
/// LINQ-to-Objects ustida bajaradi va HAR materializatsiyani SANAYDI — "N+1 yo'q" sharti
/// shu hisoblagich bilan qulflanadi: so'rovlar soni sahifadagi foydalanuvchilar soniga
/// BOG'LIQ BO'LMASLIGI kerak.
/// </summary>
internal sealed class CountingInlineAsyncQueryExecutor : IAsyncQueryExecutor
{
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

internal sealed class FixedPublicSpaceDateTime(DateTimeOffset now) : IDateTime
{
    public DateTimeOffset UtcNow => now;
}
