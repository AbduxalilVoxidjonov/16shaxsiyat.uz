using MediatR;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Application.Common.Events;
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

    void IAppDbContext.Add<TEntity>(TEntity entity) => Set<TEntity>().Add(entity);

    void IAppDbContext.Remove<TEntity>(TEntity entity) => Set<TEntity>().Remove(entity);

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

        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
