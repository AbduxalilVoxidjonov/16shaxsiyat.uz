using System.Linq.Expressions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Admin.Schools.Testing;

/// <summary>
/// `Application.Admin.Schools` o'qish handerlari uchun soxta `IAppDbContext` — xotiradagi
/// `Schools`/`Students`/`Assessments` ro'yxatlari (`FakeAiAppDbContext` bilan bir xil naqsh:
/// handler TEGMAYDIGAN hamma narsa `NotSupportedException` otadi, shu sabab "handler ortiqcha
/// jadvalga murojaat qilmayapti" ham sinaladi).
///
/// **Global query filtrlar taqlid qilinadi** — haqiqiy `AppDbContext.OnModelCreating` da
/// `School`/`Student`/`Assessment` uchun `HasQueryFilter(x => !x.IsDeleted)` bor, EF Core uni
/// har so'rovga o'zi qo'shadi. Handler kodida qo'lda `IsDeleted` sharti YO'Q (bo'lmasligi ham
/// kerak), shu sabab soxta kontekst filtrni o'zi qo'llaydi — aks holda test haqiqiy xatti-
/// harakatdan chetga chiqib, yolg'on natija berardi. Filtrning o'zi (EF darajasida) esa
/// integratsiya testida (`AdminSchoolStatsEndpointTests`) tasdiqlanadi.
/// </summary>
internal sealed class FakeSchoolsAppDbContext : IAppDbContext
{
    public List<School> SchoolList { get; } = [];

    public List<Student> StudentList { get; } = [];

    public List<Assessment> AssessmentList { get; } = [];

    // ————— Havola sog'ligi (`SchoolLinkHealthEvaluator`) uchun katalog jadvallari —————
    // 2026-09-03: maktab ro'yxati/detali endi "havola ishlaydimi" ni ham hisoblaydi, shu sabab
    // bu to'rtta jadval ham soxta kontekstda mavjud bo'lishi shart.
    public List<AssessmentProgram> ProgramList { get; } = [];

    public List<ProgramTest> ProgramTestList { get; } = [];

    public List<SchoolProgram> SchoolProgramList { get; } = [];

    public List<TestDefinition> TestDefinitionList { get; } = [];

    public List<Question> QuestionList { get; } = [];

    public IQueryable<School> Schools => SchoolList.Where(s => !s.IsDeleted).AsQueryable();

    public IQueryable<Student> Students => StudentList.Where(s => !s.IsDeleted).AsQueryable();

    public IQueryable<Assessment> Assessments => AssessmentList.Where(a => !a.IsDeleted).AsQueryable();

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

    public IQueryable<AssessmentTest> AssessmentTests => throw new NotSupportedException();

    public IQueryable<Answer> Answers => throw new NotSupportedException();

    public IQueryable<TestResult> TestResults => throw new NotSupportedException();

    public IQueryable<TestDefinition> TestDefinitions => TestDefinitionList.AsQueryable();

    public IQueryable<Question> Questions => QuestionList.AsQueryable();

    public IQueryable<TestScale> TestScales => throw new NotSupportedException();

    public IQueryable<AnalysisJob> AnalysisJobs => throw new NotSupportedException();

    public IQueryable<AssessmentProgram> AssessmentPrograms => ProgramList.AsQueryable();

    public IQueryable<ProgramTest> ProgramTests => ProgramTestList.AsQueryable();

    public IQueryable<SchoolProgram> SchoolPrograms => SchoolProgramList.AsQueryable();

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

/// <summary>LINQ-to-Objects ustida bevosita bajaradi — EF Core/DB yo'q.</summary>
internal sealed class SchoolsInlineAsyncQueryExecutor : IAsyncQueryExecutor
{
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.ToList());

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.FirstOrDefault());

    public Task<T> SingleAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.Single());

    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.SingleOrDefault());

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.Count());

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.Any());

    public Task<int> SumAsync(IQueryable<int> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.Sum());

    public Task<int> SumAsync<T>(IQueryable<T> query, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.Sum(selector));

    public Task<decimal> SumAsync<T>(IQueryable<T> query, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) =>
        Task.FromResult(query.Sum(selector));
}

/// <summary>Sinovda QR generatsiyasi ahamiyatsiz — statistika sinaladi.</summary>
internal sealed class FakeQrCodeGenerator : IQrCodeGenerator
{
    public string GeneratePngBase64(string content) => "cXI=";
}

/// <summary>`PublicWebBaseUrl`dan boshqasi bu handerlarda ishlatilmaydi.</summary>
internal sealed class FakeSchoolsAppSettings : IAppSettings
{
    public int SessionLifetimeDays => 7;

    public bool ShowResultToStudent => false;

    public bool AutoAnalyzeOnCompletion => false;

    public int RefreshTokenDays => 14;

    public string PublicWebBaseUrl => "https://16shaxsiyat.uz";
}
