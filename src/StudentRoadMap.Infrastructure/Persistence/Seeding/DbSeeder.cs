using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Seeding;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Ai;

namespace StudentRoadMap.Infrastructure.Persistence.Seeding;

/// <summary>
/// Test bankini (4 tizim metodikasi), tip katalogini, kasb xaritasini va superadminni
/// idempotent tarzda yuklaydigan seeder (`prompts/04-katalog-va-seed-infratuzilma.md`).
/// JSON o'qish/validatsiya/domenga aylantirishning sof qismi <see cref="SeedDataLoader"/>
/// (`Application`) da — bu klass faqat fayl I/O va EF Core upsert'ga javobgar.
/// Seed migratsiyaga qo'yilmaydi (`docs/05` 4-bo'lim) — `--seed` argumenti yoki
/// `App:SeedOnStartup=true` orqali ishga tushiriladi (`Api/Program.cs`).
/// </summary>
public sealed class DbSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly AppDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbSeeder> _logger;
    private readonly string _seedDataRoot;

    public DbSeeder(
        AppDbContext dbContext,
        IDateTime dateTime,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger<DbSeeder> logger,
        string? seedDataRoot = null)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _logger = logger;
        // `seedDataRoot` faqat testlar uchun (vaqtinchalik katalog bilan BR-8/tranzaksiya
        // ssenariylarini soxtalashtirish) — DI orqali chaqirilganda har doim default qiymat
        // ishlatiladi (`string?` uchun mos servis ro'yxatdan o'tilmagani sabab .NET DI shu
        // parametr default qiymatini avtomatik qo'llaydi).
        _seedDataRoot = seedDataRoot ?? Path.Combine(AppContext.BaseDirectory, "SeedData");
    }

    /// <summary>
    /// To'rtta bosqich (test bankiga, tip katalogiga, kasb xaritasiga, superadminga oid) bitta
    /// DB tranzaksiyasida bajariladi — biror bosqich (masalan, BR-8 scale/direction konflikti)
    /// xato bilan to'xtasa, oldingi bosqichlar ham commit qilinmaydi (S8: qisman holat qolmaydi).
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seed boshlandi: {Root}", _seedDataRoot);

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await SeedTestDefinitionsAsync(cancellationToken).ConfigureAwait(false);
            await SeedTypeCatalogAsync(cancellationToken).ConfigureAwait(false);
            await SeedCareerMapAsync(cancellationToken).ConfigureAwait(false);
            await SeedPromptTemplatesAsync(cancellationToken).ConfigureAwait(false);
            await SeedSuperAdminAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }

        _logger.LogInformation("Seed tugadi.");
    }

    private async Task SeedTestDefinitionsAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_seedDataRoot, "test-definitions");
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Test bankiga oid seed katalogi topilmadi: {Directory}", directory);
            return;
        }

        var now = _dateTime.UtcNow;

        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var sourceName = Path.GetFileName(file);
            var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var dto = SeedDataLoader.ParseTestDefinition(json, sourceName);

            var existing = await _dbContext.TestDefinitions
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Code == dto.Code, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                var testDefinitionId = Guid.NewGuid();
                var testDefinition = SeedDataLoader.ToDomainSystemTestDefinition(
                    dto, testDefinitionId, _ => Guid.NewGuid(), now);

                _dbContext.TestDefinitions.Add(testDefinition);
                _logger.LogInformation(
                    "Yangi tizim metodikasi qo'shildi: {Code} ({QuestionCount} savol)", dto.Code, dto.Questions.Count);
                continue;
            }

            UpsertExistingTestDefinition(existing, dto, sourceName, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// BR-8 himoyasi: `Scale`/`Direction`/`Weight` o'zgargan bo'lsa xato bilan to'xtaydi (avval
    /// farqlarni log qiladi). O'zgarish bo'lmasa faqat matn/tartib yangilanadi — shkalaga tegilmaydi.
    /// </summary>
    private void UpsertExistingTestDefinition(TestDefinition existing, TestDefinitionSeedDto dto, string sourceName, DateTimeOffset now)
    {
        var conflicts = SeedDataLoader.DetectScaleConflicts(existing, dto);
        if (conflicts.Count > 0)
        {
            foreach (var conflict in conflicts)
            {
                _logger.LogError(
                    "Scale/direction/weight konflikti — {TestCode}/{QuestionCode}: bazada {ExistingScale}/{ExistingDirection}/{ExistingWeight}, seed faylida {IncomingScale}/{IncomingDirection}/{IncomingWeight}",
                    dto.Code, conflict.QuestionCode, conflict.ExistingScale, conflict.ExistingDirection, conflict.ExistingWeight, conflict.IncomingScale, conflict.IncomingDirection, conflict.IncomingWeight);
            }

            throw new InvalidOperationException(
                $"'{sourceName}' ({dto.Code}) faylida {conflicts.Count} ta savolning scale/direction/weight qiymati bazadagidan farq qiladi — bu BR-8 (tizim metodikasi shkalasi qulfi) buzilishi. Avval oltin test yangilanishi kerak (docs/05, 4-bo'lim).");
        }

        existing.UpdateMetadata(dto.NameUz, dto.DescriptionUz, dto.DisplayOrder, dto.EstimatedMinutes, dto.ShuffleQuestions, dto.PageSize, now);

        var existingQuestionsByCode = existing.Questions.ToDictionary(q => q.Code, StringComparer.Ordinal);
        foreach (var questionDto in dto.Questions)
        {
            if (existingQuestionsByCode.TryGetValue(questionDto.Code, out var question))
            {
                question.UpdateText(questionDto.TextUz, questionDto.TextRu, questionDto.TextEn);
                question.UpdateOrder(questionDto.Order);
            }
            else
            {
                // Tizim metodikasiga runtime'da yangi savol qo'shish BR-8 bo'yicha taqiqlangan —
                // bunday o'zgarish faqat alohida migratsiya + TestDefinition.Version oshirilishi
                // bilan amalga oshiriladi, seeder buni jimgina o'tkazib yuboradi.
                _logger.LogWarning(
                    "'{TestCode}' testida seed faylidagi '{QuestionCode}' savoli bazada yo'q — tizim metodikasiga runtime seed orqali savol qo'shilmaydi (BR-8), o'tkazib yuborildi.",
                    dto.Code, questionDto.Code);
            }
        }

        _logger.LogInformation("Tizim metodikasi yangilandi: {Code}", dto.Code);
    }

    private async Task SeedTypeCatalogAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataRoot, "type-catalog.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Tip katalogi seed fayli topilmadi: {File}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        List<TypeCatalogSeedDto> entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<TypeCatalogSeedDto>>(json, JsonOptions)
                ?? throw new InvalidOperationException("type-catalog.json 'null' sifatida o'qildi.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"type-catalog.json formati noto'g'ri: {ex.Message}", ex);
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Code))
            {
                throw new InvalidOperationException("type-catalog.json: 'code' maydoni bo'sh bo'lgan yozuv topildi.");
            }

            // `TypeCatalogEntry` — o'zgarmas ValueObject (setter yo'q), shu sabab qayta seedlashda
            // eskisi olib tashlanadi va yangi qiymatlar bilan qayta qo'shiladi (natijada qator soni
            // o'zgarmaydi — idempotent).
            var existing = await _dbContext.TypeCatalog
                .FirstOrDefaultAsync(t => t.Code == entry.Code, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                _dbContext.TypeCatalog.Remove(existing);
            }

            var replacement = TypeCatalogEntry.Create(
                entry.Code,
                entry.NameUz,
                entry.ShortDescriptionUz,
                entry.LongDescriptionUz,
                entry.Strengths,
                entry.GrowthAreas,
                entry.CareerHints);

            _dbContext.TypeCatalog.Add(replacement);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Tip katalogi seed qilindi: {Count} tip.", entries.Count);
    }

    private async Task SeedCareerMapAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_seedDataRoot, "career-map.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Kasb xaritasi seed fayli topilmadi: {File}", filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        List<CareerMapSeedDto> entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<CareerMapSeedDto>>(json, JsonOptions)
                ?? throw new InvalidOperationException("career-map.json 'null' sifatida o'qildi.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"career-map.json formati noto'g'ri: {ex.Message}", ex);
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.HollandCode) || string.IsNullOrWhiteSpace(entry.FieldNameUz))
            {
                throw new InvalidOperationException("career-map.json: 'hollandCode'/'fieldNameUz' bo'sh bo'lgan yozuv topildi.");
            }

            // `CareerMapEntry` da ham update metodi yo'q — mos (HollandCode, FieldNameUz) juftligi
            // topilsa o'chirilib qayta qo'shiladi (idempotent yangilanish).
            var existing = await _dbContext.CareerMap
                .FirstOrDefaultAsync(c => c.HollandCode == entry.HollandCode && c.FieldNameUz == entry.FieldNameUz, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                _dbContext.CareerMap.Remove(existing);
            }

            var replacement = CareerMapEntry.Create(
                Guid.NewGuid(),
                entry.HollandCode,
                entry.FieldNameUz,
                entry.RelevanceOrder,
                entry.DescriptionUz,
                entry.ExampleProfessions);

            _dbContext.CareerMap.Add(replacement);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Kasb xaritasi seed qilindi: {Count} yo'nalish.", entries.Count);
    }

    /// <summary>
    /// `prompt_templates`ga `full_analysis`/`v1.0` shablonini yozadi (`prompts/16` vazifa #5,
    /// DoD: "Prompt shabloni seed qilinadi va versiyasi `AiAnalysis`ga yoziladi"). Matn/sxema
    /// `DefaultPromptTemplates`/`AnalysisJsonSchema` bilan bir xil — kodda IKKI joyda mustaqil
    /// yozilgan matn bo'lmasligi uchun (`PromptBuilder`dagi embedded fallback ham shu yerdan).
    /// (`Key`, `Version`) juftligi bo'yicha idempotent — mavjud bo'lsa hech narsa qilinmaydi
    /// (`prompt_templates` versiyalangan — versiya matni RUNTIME'da o'zgartirilmaydi, yangi
    /// matn kerak bo'lsa yangi versiya qo'shiladi).
    /// </summary>
    private async Task SeedPromptTemplatesAsync(CancellationToken cancellationToken)
    {
        var exists = await _dbContext.PromptTemplates
            .AnyAsync(t => t.Key == DefaultPromptTemplates.Key && t.Version == DefaultPromptTemplates.Version, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            _logger.LogInformation("Prompt shabloni allaqachon mavjud: {Key}/{Version} — o'tkazib yuborildi.", DefaultPromptTemplates.Key, DefaultPromptTemplates.Version);
            return;
        }

        var now = _dateTime.UtcNow;
        var template = PromptTemplate.Create(
            Guid.NewGuid(),
            DefaultPromptTemplates.Key,
            DefaultPromptTemplates.Version,
            DefaultPromptTemplates.SystemTextV1,
            DefaultPromptTemplates.UserTextV1,
            AnalysisJsonSchema.RawJson,
            now);
        template.Activate();

        _dbContext.PromptTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Prompt shabloni seed qilindi: {Key}/{Version}.", DefaultPromptTemplates.Key, DefaultPromptTemplates.Version);
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        var username = _configuration["ADMIN_USERNAME"] ?? _configuration["Admin:Username"];
        var password = _configuration["ADMIN_PASSWORD"] ?? _configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("'ADMIN_USERNAME'/'ADMIN_PASSWORD' berilmagan — superadmin seed qilinmadi.");
            return;
        }

        var exists = await _dbContext.AdminUsers
            .AnyAsync(u => u.Username == username, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            _logger.LogInformation("Superadmin '{Username}' allaqachon mavjud — o'tkazib yuborildi.", username);
            return;
        }

        var passwordHash = _passwordHasher.Hash(password);
        var email = _configuration["ADMIN_EMAIL"] ?? _configuration["Admin:Email"] ?? $"{username}@salohiyat.uz";

        var admin = AdminUser.Create(Guid.NewGuid(), username, email, passwordHash, _dateTime.UtcNow);
        _dbContext.AdminUsers.Add(admin);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Superadmin yaratildi: {Username}.", username);
    }
}
