using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StudentRoadMap.Application.Common.Events;
using StudentRoadMap.Application.Common.Exceptions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Infrastructure.Persistence;

/// <summary>
/// EF Core `DbContext` — `IAppDbContext` ni amalga oshiradi. Naming convention (`snake_case`)
/// va Npgsql provayder `DependencyInjection.AddInfrastructure` da ulanadi.
/// </summary>
public sealed class AppDbContext : DbContext, IAppDbContext
{
    private readonly IDateTime _dateTime;
    private readonly IPublisher _publisher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTime dateTime, IPublisher publisher)
        : base(options)
    {
        _dateTime = dateTime;
        _publisher = publisher;
    }

    public DbSet<School> Schools => Set<School>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Assessment> Assessments => Set<Assessment>();

    public DbSet<AssessmentTest> AssessmentTests => Set<AssessmentTest>();

    public DbSet<Answer> Answers => Set<Answer>();

    public DbSet<TestResult> TestResults => Set<TestResult>();

    public DbSet<TestDefinition> TestDefinitions => Set<TestDefinition>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<TestScale> TestScales => Set<TestScale>();

    public DbSet<AssessmentProgram> AssessmentPrograms => Set<AssessmentProgram>();

    public DbSet<ProgramTest> ProgramTests => Set<ProgramTest>();

    public DbSet<SchoolProgram> SchoolPrograms => Set<SchoolProgram>();

    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();

    public DbSet<TypeCatalogEntry> TypeCatalog => Set<TypeCatalogEntry>();

    public DbSet<CareerMapEntry> CareerMap => Set<CareerMapEntry>();

    public DbSet<AiProviderConfig> AiProviderConfigs => Set<AiProviderConfig>();

    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();

    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<AdminTotpBackupCode> AdminTotpBackupCodes => Set<AdminTotpBackupCode>();

    public DbSet<RegistrationCounter> RegistrationCounters => Set<RegistrationCounter>();

    public DbSet<SchoolLinkView> SchoolLinkViews => Set<SchoolLinkView>();

    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();

    public DbSet<PublicUser> PublicUsers => Set<PublicUser>();

    public DbSet<PublicRefreshToken> PublicRefreshTokens => Set<PublicRefreshToken>();

    // --- IAppDbContext: DbSet<T> emas, IQueryable<T> (PM qarori) ---------------------------
    IQueryable<School> IAppDbContext.Schools => Schools;

    IQueryable<Student> IAppDbContext.Students => Students;

    IQueryable<Assessment> IAppDbContext.Assessments => Assessments;

    IQueryable<AssessmentTest> IAppDbContext.AssessmentTests => AssessmentTests;

    IQueryable<Answer> IAppDbContext.Answers => Answers;

    IQueryable<TestResult> IAppDbContext.TestResults => TestResults;

    IQueryable<TestDefinition> IAppDbContext.TestDefinitions => TestDefinitions;

    IQueryable<Question> IAppDbContext.Questions => Questions;

    IQueryable<TestScale> IAppDbContext.TestScales => TestScales;

    IQueryable<AssessmentProgram> IAppDbContext.AssessmentPrograms => AssessmentPrograms;

    IQueryable<ProgramTest> IAppDbContext.ProgramTests => ProgramTests;

    IQueryable<SchoolProgram> IAppDbContext.SchoolPrograms => SchoolPrograms;

    IQueryable<AnswerOption> IAppDbContext.AnswerOptions => AnswerOptions;

    IQueryable<TypeCatalogEntry> IAppDbContext.TypeCatalog => TypeCatalog;

    IQueryable<CareerMapEntry> IAppDbContext.CareerMap => CareerMap;

    IQueryable<AiProviderConfig> IAppDbContext.AiProviderConfigs => AiProviderConfigs;

    IQueryable<AiAnalysis> IAppDbContext.AiAnalyses => AiAnalyses;

    IQueryable<PromptTemplate> IAppDbContext.PromptTemplates => PromptTemplates;

    IQueryable<AdminUser> IAppDbContext.AdminUsers => AdminUsers;

    IQueryable<RefreshToken> IAppDbContext.RefreshTokens => RefreshTokens;

    IQueryable<AuditLog> IAppDbContext.AuditLogs => AuditLogs;

    IQueryable<AdminTotpBackupCode> IAppDbContext.AdminTotpBackupCodes => AdminTotpBackupCodes;

    IQueryable<RegistrationCounter> IAppDbContext.RegistrationCounters => RegistrationCounters;

    IQueryable<SchoolLinkView> IAppDbContext.SchoolLinkViews => SchoolLinkViews;

    IQueryable<AnalysisJob> IAppDbContext.AnalysisJobs => AnalysisJobs;

    IQueryable<PublicUser> IAppDbContext.PublicUsers => PublicUsers;

    IQueryable<PublicRefreshToken> IAppDbContext.PublicRefreshTokens => PublicRefreshTokens;

    IQueryable<TEntity> IAppDbContext.AsNoTracking<TEntity>(IQueryable<TEntity> query) => query.AsNoTracking();

    IQueryable<TEntity> IAppDbContext.IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) => query.IgnoreQueryFilters();

    void IAppDbContext.Add<TEntity>(TEntity entity) => Set<TEntity>().Add(entity);

    void IAppDbContext.Remove<TEntity>(TEntity entity) => Set<TEntity>().Remove(entity);

    async Task<IAppDbContextTransaction> IAppDbContext.BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        return new EfAppDbContextTransaction(transaction);
    }

    /// <summary>
    /// Atomik `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` — Postgres va SQLite (sinov
    /// muhiti) ikkalasida ham qo'llab-quvvatlanadi (SQLite 3.35+ `RETURNING`, 3.24+ `ON CONFLICT
    /// DO UPDATE`). `docs/08-auth-va-xavfsizlik.md` 3-bo'lim ruxsati bilan xom SQL — poyga
    /// holatining oldini olish uchun (izoh: `IAppDbContext.IncrementRegistrationCounterAsync`).
    /// </summary>
    async Task<int> IAppDbContext.IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken)
    {
        var rows = await Database.SqlQueryRaw<int>(
                """
                INSERT INTO registration_counters (school_id, date_utc, count)
                VALUES ({0}, {1}, 1)
                ON CONFLICT (school_id, date_utc)
                DO UPDATE SET count = registration_counters.count + 1
                RETURNING count
                """,
                schoolId,
                dateUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows[0];
    }

    /// <summary>
    /// Atomik `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` — `IncrementRegistrationCounterAsync`
    /// bilan BIR XIL naqsh (izohiga qarang), faqat `school_link_views` jadvali uchun
    /// (`prompts/15` vazifa 1, 2026-09-02).
    /// </summary>
    async Task<int> IAppDbContext.IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken)
    {
        var rows = await Database.SqlQueryRaw<int>(
                """
                INSERT INTO school_link_views (school_id, date_utc, count)
                VALUES ({0}, {1}, 1)
                ON CONFLICT (school_id, date_utc)
                DO UPDATE SET count = school_link_views.count + 1
                RETURNING count
                """,
                schoolId,
                dateUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows[0];
    }

    /// <summary>
    /// Atomik shartli `UPDATE ... WHERE used_at IS NULL` — QA topilmasi (`docs/13-auth-va-jwt.md`):
    /// ikki bir vaqtdagi so'rov bir xil TOTP zaxira kodi bilan kelsa, `IAppDbContext.TryMarkTotpBackupCodeUsedAsync`
    /// izohidagi kabi faqat BITTASI muvaffaqiyatli bo'lishi shart (`IncrementRegistrationCounterAsync`
    /// bilan bir xil naqsh/sabab).
    /// </summary>
    async Task<bool> IAppDbContext.TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken)
    {
        // Xom SQL — `ApplySqliteDateTimeOffsetConversion`dagi `ValueConverter` faqat EF Core'ning
        // ODDIY (LINQ/`SaveChanges`) so'rov quvuriga qo'llanadi, xom SQL parametriga AVTOMATIK
        // TA'SIR QILMAYDI — SQLite'da ustun endi `long` (UTC tick) sifatida saqlanadi, lekin bu
        // yerga xom `DateTimeOffset` yuborilsa, SQLite'ning standart (matn) parametr turi bilan
        // yoziladi va keyinroq LINQ orqali o'qishda noto'g'ri talqin qilinadi (QA topilmasi,
        // `prompts/15` davomida: `AdminTotpBackupCodeConcurrencyTests` konvertatsiyadan keyin
        // yiqildi — sabab aynan shu edi). Shu sabab SQLite'da parametr QO'LDA xuddi
        // `ApplySqliteDateTimeOffsetConversion` bilan BIR XIL shaklga (`UtcTicks`) o'giriladi;
        // Postgres'da (`timestamptz`, konvertatsiya YO'Q) xom `DateTimeOffset` ishlatiladi.
        object usedAtParameter = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
            ? usedAt.UtcTicks
            : usedAt;

        var affectedRows = await Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE admin_totp_backup_codes SET used_at = {usedAtParameter} WHERE id = {backupCodeId} AND used_at IS NULL",
                cancellationToken)
            .ConfigureAwait(false);

        return affectedRows > 0;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // `docs/05-database-schema.md` 2-bo'lim: uuid (pgcrypto) va ism trigram qidiruvi (pg_trgm).
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Soft delete global query filter — faqat DDL'da `is_deleted` ustuni bor jadvallar
        // (School/Student: is_deleted + deleted_at; Assessment: faqat is_deleted, `docs/05`).
        modelBuilder.Entity<School>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Student>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Assessment>().HasQueryFilter(a => !a.IsDeleted);

        // `PublicUser` da `is_deleted` ustuni YO'Q — o'chirilganlik `deleted_at`ning
        // to'ldirilganligi bilan aniqlanadi (`PublicUser.DeletedAt` izohi). O'chirilgan
        // (anonimlashtirilgan) akkaunt hech qanday oddiy so'rovda ko'rinmaydi; admin
        // statistikasi kerak bo'lsa `IgnoreQueryFilters` orqali olinadi.
        modelBuilder.Entity<PublicUser>().HasQueryFilter(u => u.DeletedAt == null);

        // Faqat SINOV muhitida (SQLite — Docker/PostgreSQL yo'q joyda `PublicApiTestFactory`/
        // `SqliteAppDbContextFactory` ishlatadi): EF Core'ning SQLite provayderi
        // `DateTimeOffset` ustunida na `ORDER BY`, na oddiy `WHERE x >= @p` taqqoslashni
        // tarjima qila oladi (`System.InvalidOperationException`, `prompts/15` davomida
        // empirik tekshirilgan — hujjatlardagi eski izohlar faqat `ORDER BY`ni aytgan edi,
        // aslida taqqoslash HAM buzilgan). Postgres'da (`Npgsql.EntityFrameworkCore.PostgreSQL`)
        // bu muammo umuman yo'q — shu sabab konvertatsiya FAQAT `Database.ProviderName`
        // SQLite bo'lganda qo'llanadi (`Database.IsSqlite()` emas — bu Sqlite paketining
        // kengaytma metodi, `Infrastructure.csproj`da SHART emas: `ProviderName` satr
        // taqqoslash EF Core yadrosining o'zida bor). Postgres modeliga (`timestamptz`)
        // HECH QANDAY ta'sir qilmaydi — `has-pending-model-changes` bilan tasdiqlangan.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            ApplySqliteDateTimeOffsetConversion(modelBuilder);
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Har bir `DateTimeOffset`/`DateTimeOffset?` ustunni `long` (UTC tick soni) ga
    /// o'giradi — SQLite'da butun son ustuni sifatida saqlanadi, shu bilan `WHERE`/`ORDER BY`
    /// DB darajasida ishlaydi (`ix_students_last_at` "DESC NULLS LAST" mantig'i
    /// `ApplyLastAssessmentAtSort`da xotirada `== null` bo'yicha ALOHIDA emulyatsiya qilinadi
    /// — bu o'zgarmaydi, faqat qiymatni solishtirish endi DB darajasida ishlaydi). `UtcTicks`
    /// (100ns aniqlik) ishlatiladi — `ToUnixTimeMilliseconds()` EMAS, chunki millisekundgacha
    /// yaxlitlash mavjud testlardagi aniq vaqt tengligini (`CreatedAt.Should().Be(now)` kabi)
    /// buzardi. Qayta tiklashda offset har doim UTC (`TimeSpan.Zero`) qilib qo'yiladi —
    /// loyihada barcha `DateTimeOffset` qiymatlar `IDateTime.UtcNow`dan keladi (`CLAUDE.md`
    /// 2-band), shuning uchun bu yo'qotishsiz (`DateTimeOffset.Equals` UTC lahzani solishtiradi,
    /// `Offset`ning o'zini emas).
    /// </summary>
    private static void ApplySqliteDateTimeOffsetConversion(ModelBuilder modelBuilder)
    {
        var nonNullConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        var nullableConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.UtcTicks : null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(nonNullConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableConverter);
                }
            }
        }
    }

    /// <summary>
    /// `CreatedAt`/`UpdatedAt` avtomatik to'ldiriladi (`IDateTime` orqali), so'ng `SaveChanges`
    /// muvaffaqiyatli bo'lgach domen hodisalari MediatR orqali publish qilinadi.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTime.UtcNow;
        UpdateAuditFields(now);

        int result;
        try
        {
            result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // `Application` qatlami EF Core paketiga bog'lanmasligi uchun (`docs/06` 3-bo'lim)
            // portativ istisnoga aylantiriladi — QA topilmasi (`docs/13-auth-va-jwt.md`):
            // `ConcurrencyStamp` (`AdminUserConfiguration`) orqali TOTP asosiy kod poyga holati
            // shu yerda ushlanadi.
            throw new ConcurrencyConflictException(
                "Ma'lumot boshqa so'rov tomonidan bir vaqtda o'zgartirildi. Qaytadan urinib ko'ring.", ex);
        }
        catch (DbUpdateException ex)
        {
            // `P14` (`prompts/14`) MAXSUS DIQQAT #3: DB darajasidagi unique cheklov (masalan
            // `ux_schools_slug`) ChIN bir vaqtdagi poyga holatida buzilishi mumkin —
            // `Application` qatlami EF Core paketiga bog'lanmasligi uchun portativ istisnoga
            // aylantiriladi (`ConcurrencyConflictException` bilan bir xil naqsh). `DbUpdateException`
            // `DbUpdateConcurrencyException`ning bazaviy klassi — shu sabab bu `catch` yuqoridagi
            // aniqrog'idan KEYIN turadi (C# birinchi mos keladigan `catch`ni tanlaydi).
            throw new UniqueConstraintViolationException(
                ProblemCodes.UniqueConstraintConflict,
                "Bu amal boshqa yozuv bilan ziddiyatga keldi (masalan, bir xil havola bandligi). Qaytadan urinib ko'ring.",
                ex);
        }

        await PublishDomainEventsAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    private void UpdateAuditFields(DateTimeOffset now)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Metadata.FindProperty("CreatedAt") is not null)
            {
                entry.Property("CreatedAt").CurrentValue = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified
                && entry.Metadata.FindProperty("UpdatedAt") is not null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }

            // Optimistik konkurentlik (QA topilmasi, `docs/13-auth-va-jwt.md`): `Modified`
            // yozuvlarda `ConcurrencyStamp` bor bo'lsa YANGI qiymatga o'rnatiladi — UPDATE'ning
            // WHERE qismi ESKI (original, SELECT paytida o'qilgan) qiymatni ishlatadi (EF
            // standart xatti-harakati), shu bilan bir vaqtdagi ikki yozuv bir-birini "yutib
            // qo'ymaydi" (lost update) — ikkinchisi `DbUpdateConcurrencyException` oladi.
            // Portativ (SQLite/Postgres) — `xmin` kabi provayderga xos ustundan farqli, oddiy
            // ustun tengligi orqali ishlaydi.
            if (entry.State == EntityState.Modified && entry.Metadata.FindProperty("ConcurrencyStamp") is not null)
            {
                entry.Property("ConcurrencyStamp").CurrentValue = Guid.NewGuid();
            }
        }
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
        {
            return;
        }

        var domainEvents = aggregatesWithEvents.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
            await _publisher.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
