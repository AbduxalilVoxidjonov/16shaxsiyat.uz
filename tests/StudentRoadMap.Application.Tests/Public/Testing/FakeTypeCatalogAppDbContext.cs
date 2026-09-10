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

namespace StudentRoadMap.Application.Tests.Public.Testing;

/// <summary>
/// `GetTypeCatalogQueryHandler` uchun soxta `IAppDbContext` — `FakeSchoolsAppDbContext` bilan
/// bir xil naqsh: handler TEGMASLIGI kerak bo'lgan hamma narsa `NotSupportedException` otadi.
/// Bu tasodifiy emas — ommaviy, autentifikatsiyasiz endpoint faqat `type_catalog` ni o'qishi
/// SHART; agar kelajakda handlerga (masalan) `Students` yoki `Assessments` so'rovi kirib qolsa,
/// test darhol yiqiladi.
///
/// <see cref="TypeCatalogReadCount"/> — `TypeCatalog` xususiyati necha marta o'qilgani.
/// Keshning CHINDAN ishlashini shu hisoblagich isbotlaydi (ikkinchi chaqiruvda o'smasligi kerak).
/// </summary>
internal sealed class FakeTypeCatalogAppDbContext : IAppDbContext
{
    private readonly List<TypeCatalogEntry> _typeCatalog;

    public FakeTypeCatalogAppDbContext(IEnumerable<TypeCatalogEntry> typeCatalog)
    {
        _typeCatalog = [.. typeCatalog];
    }

    /// <summary>`TypeCatalog` ga murojaatlar soni — kesh sinovining o'lchov nuqtasi.</summary>
    public int TypeCatalogReadCount { get; private set; }

    public IQueryable<TypeCatalogEntry> TypeCatalog
    {
        get
        {
            TypeCatalogReadCount++;
            return _typeCatalog.AsQueryable();
        }
    }

    public IQueryable<School> Schools => throw NotUsed(nameof(Schools));

    public IQueryable<Student> Students => throw NotUsed(nameof(Students));

    public IQueryable<Assessment> Assessments => throw NotUsed(nameof(Assessments));

    public IQueryable<AssessmentTest> AssessmentTests => throw NotUsed(nameof(AssessmentTests));

    public IQueryable<Answer> Answers => throw NotUsed(nameof(Answers));

    public IQueryable<TestResult> TestResults => throw NotUsed(nameof(TestResults));

    public IQueryable<TestDefinition> TestDefinitions => throw NotUsed(nameof(TestDefinitions));

    public IQueryable<Question> Questions => throw NotUsed(nameof(Questions));

    public IQueryable<TestScale> TestScales => throw NotUsed(nameof(TestScales));

    public IQueryable<QuestionSection> QuestionSections => throw NotUsed(nameof(QuestionSections));

    public IQueryable<AssessmentProgram> AssessmentPrograms => throw NotUsed(nameof(AssessmentPrograms));

    public IQueryable<ProgramTest> ProgramTests => throw NotUsed(nameof(ProgramTests));

    public IQueryable<SchoolProgram> SchoolPrograms => throw NotUsed(nameof(SchoolPrograms));

    public IQueryable<AnswerOption> AnswerOptions => throw NotUsed(nameof(AnswerOptions));

    public IQueryable<CareerMapEntry> CareerMap => throw NotUsed(nameof(CareerMap));

    public IQueryable<AiProviderConfig> AiProviderConfigs => throw NotUsed(nameof(AiProviderConfigs));

    public IQueryable<AiAnalysis> AiAnalyses => throw NotUsed(nameof(AiAnalyses));

    public IQueryable<PromptTemplate> PromptTemplates => throw NotUsed(nameof(PromptTemplates));

    public IQueryable<AdminUser> AdminUsers => throw NotUsed(nameof(AdminUsers));

    public IQueryable<RefreshToken> RefreshTokens => throw NotUsed(nameof(RefreshTokens));

    public IQueryable<AuditLog> AuditLogs => throw NotUsed(nameof(AuditLogs));

    public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw NotUsed(nameof(AdminTotpBackupCodes));

    public IQueryable<RegistrationCounter> RegistrationCounters => throw NotUsed(nameof(RegistrationCounters));

    public IQueryable<SchoolLinkView> SchoolLinkViews => throw NotUsed(nameof(SchoolLinkViews));

    public IQueryable<AnalysisJob> AnalysisJobs => throw NotUsed(nameof(AnalysisJobs));

    public IQueryable<PublicUser> PublicUsers => throw NotUsed(nameof(PublicUsers));

    public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw NotUsed(nameof(PublicRefreshTokens));

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query;

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query;

    public void Add<TEntity>(TEntity entity)
        where TEntity : class => throw NotUsed(nameof(Add));

    public void Remove<TEntity>(TEntity entity)
        where TEntity : class => throw NotUsed(nameof(Remove));

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw NotUsed(nameof(SaveChangesAsync));

    public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw NotUsed(nameof(BeginTransactionAsync));

    public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
        throw NotUsed(nameof(IncrementRegistrationCounterAsync));

    public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
        throw NotUsed(nameof(IncrementSchoolLinkViewAsync));

    public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) =>
        throw NotUsed(nameof(TryMarkTotpBackupCodeUsedAsync));

    private static NotSupportedException NotUsed(string member) =>
        new($"Tip katalogi handler'i `{member}` ga tegmasligi kerak — u FAQAT `TypeCatalog` ni o'qiydi.");
}

/// <summary>LINQ-to-Objects ustida bevosita bajaradi — EF Core/DB yo'q (`InlineAsyncQueryExecutor` bilan bir xil).</summary>
internal sealed class TypeCatalogInlineAsyncQueryExecutor : IAsyncQueryExecutor
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
/// Xotiradagi oddiy kesh — `MemoryCacheService` o'rniga (`Application` testida `IMemoryCache`
/// kerak emas). TTL ATAYLAB e'tiborga olinmaydi: bu sinovlar keshning MUDDATINI emas,
/// "ikkinchi chaqiruv DB'ga bormaydi" xatti-harakatini tekshiradi.
/// </summary>
internal sealed class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, object> _entries = [];

    /// <summary>`Set` chaqirilgan kalitlar — kesh yozuvining CHINDAN yozilganini tekshirish uchun.</summary>
    public List<string> WrittenKeys { get; } = [];

    public bool TryGet<T>(string key, out T value)
        where T : class
    {
        if (_entries.TryGetValue(key, out var stored) && stored is T typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan duration)
        where T : class
    {
        _entries[key] = value;
        WrittenKeys.Add(key);
    }

    public void Remove(string key) => _entries.Remove(key);
}
