using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.GetSchoolInfo;
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
/// `GetSchoolInfoQueryHandler` — havola ochilishi hisoblagichining FAIL-OPEN shartini sinaydi
/// (PM qarori, 2026-09-02): `IncrementSchoolLinkViewAsync` istisno tashlasa ham landing sahifasi
/// (`GET /api/public/schools/{slug}`) YIQILMASLIGI kerak — telemetriya mahsulotdan muhimroq
/// bo'lib qolmasligi kerak. `Application` DB'siz sinov (`IAppDbContext`/`IAsyncQueryExecutor`
/// qo'lda yozilgan soxta amalga oshirishlar — loyihada mocking kutubxonasi yo'q) —
/// `AdminDashboardMathTests`dagi bilan bir xil ruhda, HTTP/EF Core'siz, tez.
/// </summary>
public sealed class GetSchoolInfoQueryHandlerTests
{
    [Fact]
    public async Task Handle_HisoblagichIstisnoTashlasa_BaribirMuvaffaqiyatliNatijaQaytaradi()
    {
        var now = DateTimeOffset.UtcNow;
        const string token = "access-token-linkview-fail-open-0123456789ab";
        var slug = SchoolSlug.Create("maktab-linkview-fail-open").Value;
        var school = School.Create(Guid.NewGuid(), "Maktab LinkView Fail-Open", "Toshkent", "Chilonzor", slug, token, "FAXM2345", now);

        // 2026-09-03: `GetSchoolInfoQueryHandler` mavjud dastur BO'LMASA `409 NO_PROGRAM_AVAILABLE`
        // qaytaradi, shu sabab bu (hisoblagich haqidagi) test uchun bitta mavjud dastur kerak —
        // aks holda test o'z maqsadidan boshqa sababga ko'ra yiqilardi.
        var program = AssessmentProgram.CreateSystemPublished(
            Guid.NewGuid(), "LINKVIEW_PROGRAM", "Havola hisoblagichi dasturi", null, 1, [(Guid.NewGuid(), 1)], now);

        var context = new ThrowingIncrementFakeDbContext([school], [program]);
        var executor = new LinqToObjectsAsyncQueryExecutor();
        var logger = new RecordingLogger<GetSchoolInfoQueryHandler>();
        var handler = new GetSchoolInfoQueryHandler(context, executor, new FakeDateTime(now), logger);

        var result = await handler.Handle(new GetSchoolInfoQuery(slug.Value, token), CancellationToken.None);

        // Asosiy talab: hisoblagich yozuvi ICHKARIDA (DB xatosi bilan) muvaffaqiyatsiz bo'lsa ham,
        // landing so'rovining o'zi MUVAFFAQIYATLI qaytishi SHART.
        result.IsSuccess.Should().BeTrue("hisoblagich xatosi telemetriya — landing sahifasini yiqitmasligi kerak");
        result.Value.SchoolId.Should().Be(school.Id);

        // Xato JIMGINA yo'qolmasligi kerak — `LogWarning` orqali yozilgan bo'lishi kerak.
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Exception is InvalidOperationException);
    }

    private sealed class FakeDateTime : IDateTime
    {
        public FakeDateTime(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, exception));
    }

    /// <summary>LINQ-to-Objects (`IQueryable&lt;T&gt;.AsQueryable()`) ustida to'g'ridan-to'g'ri bajaradi — EF Core/DB yo'q.</summary>
    private sealed class LinqToObjectsAsyncQueryExecutor : IAsyncQueryExecutor
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

    /// <summary>
    /// `IAppDbContext` soxta amalga oshirilishi — faqat `GetSchoolInfoQueryHandler` (+
    /// `ProgramAvailability`) chindan o'qiydigan xususiyatlar (`Schools`/`TestDefinitions`/
    /// `Questions`/`SchoolPrograms`/`AssessmentPrograms`/`ProgramTests`) bo'sh ro'yxat qaytaradi;
    /// `IncrementSchoolLinkViewAsync` ATAYLAB istisno tashlaydi (sinov maqsadi — "DB band"
    /// simulyatsiyasi). Ishlatilmaydigan yozish metodlari (`Add`/`Remove`/`SaveChangesAsync`/
    /// `BeginTransactionAsync`/boshqa ikkita hisoblagich) chaqirilsa xato — bu handler ularni
    /// TEGMASLIGI kerak.
    /// </summary>
    private sealed class ThrowingIncrementFakeDbContext : IAppDbContext
    {
        private readonly List<School> _schools;
        private readonly List<AssessmentProgram> _programs;

        public ThrowingIncrementFakeDbContext(List<School> schools, List<AssessmentProgram> programs)
        {
            _schools = schools;
            _programs = programs;
        }

        public IQueryable<School> Schools => _schools.AsQueryable();

        public IQueryable<Student> Students => Enumerable.Empty<Student>().AsQueryable();

        public IQueryable<Assessment> Assessments => Enumerable.Empty<Assessment>().AsQueryable();

        public IQueryable<AssessmentTest> AssessmentTests => Enumerable.Empty<AssessmentTest>().AsQueryable();

        public IQueryable<Answer> Answers => Enumerable.Empty<Answer>().AsQueryable();

        public IQueryable<TestResult> TestResults => Enumerable.Empty<TestResult>().AsQueryable();

        public IQueryable<TestDefinition> TestDefinitions => Enumerable.Empty<TestDefinition>().AsQueryable();

        public IQueryable<Question> Questions => Enumerable.Empty<Question>().AsQueryable();

        public IQueryable<TestScale> TestScales => Enumerable.Empty<TestScale>().AsQueryable();

        public IQueryable<QuestionSection> QuestionSections => Enumerable.Empty<QuestionSection>().AsQueryable();

        public IQueryable<AnalysisJob> AnalysisJobs => Enumerable.Empty<AnalysisJob>().AsQueryable();

        public IQueryable<PublicUser> PublicUsers => Enumerable.Empty<PublicUser>().AsQueryable();

        public IQueryable<PublicRefreshToken> PublicRefreshTokens => Enumerable.Empty<PublicRefreshToken>().AsQueryable();

        public IQueryable<AssessmentProgram> AssessmentPrograms => _programs.AsQueryable();

        public IQueryable<ProgramTest> ProgramTests => Enumerable.Empty<ProgramTest>().AsQueryable();

        public IQueryable<SchoolProgram> SchoolPrograms => Enumerable.Empty<SchoolProgram>().AsQueryable();

        public IQueryable<AnswerOption> AnswerOptions => Enumerable.Empty<AnswerOption>().AsQueryable();

        public IQueryable<TypeCatalogEntry> TypeCatalog => Enumerable.Empty<TypeCatalogEntry>().AsQueryable();

        public IQueryable<CareerMapEntry> CareerMap => Enumerable.Empty<CareerMapEntry>().AsQueryable();

        public IQueryable<AiProviderConfig> AiProviderConfigs => Enumerable.Empty<AiProviderConfig>().AsQueryable();

        public IQueryable<AiAnalysis> AiAnalyses => Enumerable.Empty<AiAnalysis>().AsQueryable();

        public IQueryable<PromptTemplate> PromptTemplates => Enumerable.Empty<PromptTemplate>().AsQueryable();

        public IQueryable<AdminUser> AdminUsers => Enumerable.Empty<AdminUser>().AsQueryable();

        public IQueryable<RefreshToken> RefreshTokens => Enumerable.Empty<RefreshToken>().AsQueryable();

        public IQueryable<AuditLog> AuditLogs => Enumerable.Empty<AuditLog>().AsQueryable();

        public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => Enumerable.Empty<AdminTotpBackupCode>().AsQueryable();

        public IQueryable<RegistrationCounter> RegistrationCounters => Enumerable.Empty<RegistrationCounter>().AsQueryable();

        public IQueryable<SchoolLinkView> SchoolLinkViews => Enumerable.Empty<SchoolLinkView>().AsQueryable();

        public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
            where TEntity : class => query;

        public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query)
            where TEntity : class => query;

        public void Add<TEntity>(TEntity entity)
            where TEntity : class => throw new NotSupportedException("Bu testda handler yozish amalini chaqirmasligi kerak.");

        public void Remove<TEntity>(TEntity entity)
            where TEntity : class => throw new NotSupportedException("Bu testda handler yozish amalini chaqirmasligi kerak.");

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Bu testda handler SaveChangesAsync chaqirmasligi kerak (Query, yon ta'sir faqat xom SQL orqali).");

        public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Bu testda handler tranzaksiya boshlamasligi kerak.");

        public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Bu handler ro'yxatdan o'tish hisoblagichiga tegmaydi.");

        /// <summary>Sinov maqsadi — "DB band/deadlock" simulyatsiyasi. Handler buni YUTISHI kerak.</summary>
        public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulyatsiya: school_link_views yozib bo'lmadi (masalan DB band/deadlock).");

        public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Bu handler TOTP zaxira kodlariga tegmaydi.");
    }
}
