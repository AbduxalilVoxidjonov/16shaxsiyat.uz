using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Ma'lumotlar bazasiga EF Core'siz kirish abstraksiyasi (PM qarori — `docs/06-arxitektura.md`
/// 3-bo'limi: `Application` qatlamida `Microsoft.EntityFrameworkCore` paketi bo'lmaydi).
/// Har entity uchun `IQueryable&lt;T&gt;` beriladi — `DbSet&lt;T&gt;` emas. Asinxron materializatsiya
/// uchun <see cref="IAsyncQueryExecutor"/> ishlatiladi.
/// </summary>
public interface IAppDbContext
{
    IQueryable<School> Schools { get; }

    IQueryable<Student> Students { get; }

    IQueryable<Assessment> Assessments { get; }

    IQueryable<AssessmentTest> AssessmentTests { get; }

    IQueryable<Answer> Answers { get; }

    IQueryable<TestResult> TestResults { get; }

    IQueryable<TestDefinition> TestDefinitions { get; }

    IQueryable<Question> Questions { get; }

    IQueryable<AnswerOption> AnswerOptions { get; }

    IQueryable<TypeCatalogEntry> TypeCatalog { get; }

    IQueryable<CareerMapEntry> CareerMap { get; }

    IQueryable<AiProviderConfig> AiProviderConfigs { get; }

    IQueryable<AiAnalysis> AiAnalyses { get; }

    IQueryable<PromptTemplate> PromptTemplates { get; }

    IQueryable<AdminUser> AdminUsers { get; }

    IQueryable<RefreshToken> RefreshTokens { get; }

    /// <summary>Yangi entity'ni o'zgarishlarni kuzatish grafigiga qo'shadi.</summary>
    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>Entity'ni o'chirish uchun belgilaydi (hard delete — soft delete domen metodlari orqali).</summary>
    void Remove<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
