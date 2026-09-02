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

    IQueryable<AssessmentProgram> AssessmentPrograms { get; }

    IQueryable<ProgramTest> ProgramTests { get; }

    IQueryable<SchoolProgram> SchoolPrograms { get; }

    IQueryable<AnswerOption> AnswerOptions { get; }

    IQueryable<TypeCatalogEntry> TypeCatalog { get; }

    IQueryable<CareerMapEntry> CareerMap { get; }

    IQueryable<AiProviderConfig> AiProviderConfigs { get; }

    IQueryable<AiAnalysis> AiAnalyses { get; }

    IQueryable<PromptTemplate> PromptTemplates { get; }

    IQueryable<AdminUser> AdminUsers { get; }

    IQueryable<RefreshToken> RefreshTokens { get; }

    IQueryable<AuditLog> AuditLogs { get; }

    IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes { get; }

    IQueryable<RegistrationCounter> RegistrationCounters { get; }

    /// <summary>
    /// So'rovni kuzatilmaydigan (no-tracking) rejimga o'tkazadi — Query handler'lar uchun
    /// (`docs/06-arxitektura.md` 4-bo'lim: "Query'lar READ-ONLY, `AsNoTracking()`"). `Application`
    /// EF Core'ning `AsNoTracking()` kengaytma metodiga (paket sifatida) bevosita bog'lanmasligi
    /// uchun shu abstraksiya orqali beriladi.
    /// </summary>
    IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
        where TEntity : class;

    /// <summary>
    /// Global so'rov filtrini (soft-delete: `School`/`Student`/`Assessment` — `is_deleted`)
    /// e'tiborsiz qoldiradi — `prompts/14` MAXSUS DIQQAT #5: o'quvchini `hard=true` bilan
    /// TO'LIQ o'chirishda avval SOFT o'chirilgan sessiyalar ham (agar bo'lsa) topilib
    /// tozalanishi kerak, aks holda ular yetim (orphan) qolib ketardi.
    /// </summary>
    IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query)
        where TEntity : class;

    /// <summary>Yangi entity'ni o'zgarishlarni kuzatish grafigiga qo'shadi.</summary>
    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>Entity'ni o'chirish uchun belgilaydi (hard delete — soft delete domen metodlari orqali).</summary>
    void Remove<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Aniq (explicit) DB tranzaksiyasini boshlaydi — `TransactionBehavior` Command'larni shu
    /// bilan o'raydi (`docs/06-arxitektura.md` 4-bo'lim: "Command'lar TransactionBehavior
    /// ichida bajariladi").
    /// </summary>
    Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Maktabning kunlik ro'yxatdan o'tish hisoblagichini atomik oshiradi va YANGI qiymatni
    /// qaytaradi — `registration_counters(school_id, date_utc)` bo'yicha `INSERT ... ON CONFLICT
    /// DO UPDATE SET count = count + 1` (poyga holatining oldini olish uchun xom SQL, `docs/08-auth-va-xavfsizlik.md`
    /// 3-bo'lim ruxsati: "atomik `INSERT … ON CONFLICT … +1`"). EF Core LINQ orqali bitta
    /// so'rovda atomik "upsert-va-oshirish" amalga oshirilmaydi — bir nechta parallel so'rov
    /// bir xil kunga bir vaqtda yozsa, LINQ bilan yozilgan "o'qi-tekshir-yoz" mantig'i poyga
    /// holatiga (ikkalasi ham eski qiymatni o'qib, limitdan oshib ketishi mumkin) olib keladi.
    /// </summary>
    Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// TOTP zaxira kodini ATOMIK "ishlatilgan" deb belgilaydi — `UPDATE admin_totp_backup_codes
    /// SET used_at = ... WHERE id = ... AND used_at IS NULL` (xom SQL, `IncrementRegistrationCounterAsync`
    /// bilan bir xil sabab/naqsh: QA topgan poyga holati — ikki bir vaqtdagi so'rov bir xil
    /// zaxira kod bilan kelsa, LINQ "o'qi-tekshir-yoz" mantig'i ikkalasini ham muvaffaqiyatli
    /// deb hisoblardi). `true` — shu chaqiruv kodni muvaffaqiyatli "ishlatilgan" deb belgiladi;
    /// `false` — kod ALLAQACHON ishlatilgan (0 qator ta'sirlandi — boshqa bir vaqtdagi so'rov
    /// ilgari yutib olgan).
    /// </summary>
    Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default);
}
