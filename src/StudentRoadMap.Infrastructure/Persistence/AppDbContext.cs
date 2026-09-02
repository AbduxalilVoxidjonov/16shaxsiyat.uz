using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Application.Common.Events;
using StudentRoadMap.Application.Common.Exceptions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
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

    // --- IAppDbContext: DbSet<T> emas, IQueryable<T> (PM qarori) ---------------------------
    IQueryable<School> IAppDbContext.Schools => Schools;

    IQueryable<Student> IAppDbContext.Students => Students;

    IQueryable<Assessment> IAppDbContext.Assessments => Assessments;

    IQueryable<AssessmentTest> IAppDbContext.AssessmentTests => AssessmentTests;

    IQueryable<Answer> IAppDbContext.Answers => Answers;

    IQueryable<TestResult> IAppDbContext.TestResults => TestResults;

    IQueryable<TestDefinition> IAppDbContext.TestDefinitions => TestDefinitions;

    IQueryable<Question> IAppDbContext.Questions => Questions;

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

    IQueryable<TEntity> IAppDbContext.AsNoTracking<TEntity>(IQueryable<TEntity> query) => query.AsNoTracking();

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
    /// Atomik shartli `UPDATE ... WHERE used_at IS NULL` — QA topilmasi (`docs/13-auth-va-jwt.md`):
    /// ikki bir vaqtdagi so'rov bir xil TOTP zaxira kodi bilan kelsa, `IAppDbContext.TryMarkTotpBackupCodeUsedAsync`
    /// izohidagi kabi faqat BITTASI muvaffaqiyatli bo'lishi shart (`IncrementRegistrationCounterAsync`
    /// bilan bir xil naqsh/sabab).
    /// </summary>
    async Task<bool> IAppDbContext.TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken)
    {
        var affectedRows = await Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE admin_totp_backup_codes SET used_at = {usedAt} WHERE id = {backupCodeId} AND used_at IS NULL",
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

        base.OnModelCreating(modelBuilder);
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
