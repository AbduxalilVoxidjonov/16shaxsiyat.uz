using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Seeding;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Identity;

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
    /// <summary>
    /// `PERSONALITY_PROFILE` tizim dasturining DETERMINISTIK identifikatori — `prompts/34`
    /// QA tuzatmasi (bloklovchi topilma, 2026-09-02): bu ID `RequireAssessmentProgramId`
    /// migratsiyasidagi xom SQL bilan AYNAN bir xil bo'lishi SHART. Ikkalasi (migratsiya va
    /// seeder) mustaqil `Guid.NewGuid()` bilan yaratsa, ular IKKI XIL dastur hosil qilib
    /// qo'yardi (migratsiya production'da `--migrate` bosqichida, seeder esa keyinroq
    /// `--seed` bosqichida ishga tushadi — `docker-compose.yml`: migrate → seed → api).
    /// </summary>
    public static readonly Guid SystemPersonalityProfileProgramId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public const string SystemPersonalityProfileProgramCode = "PERSONALITY_PROFILE";

    /// <summary>
    /// YAGONA ommaviy makonning (`SchoolKind.PublicSpace`) deterministik identifikatori —
    /// `SystemPersonalityProfileProgramId` bilan bir xil sabab: seeder bir necha muhitda
    /// (migrate → seed konteynerlari) mustaqil ishga tushadi, `Guid.NewGuid()` bo'lsa har
    /// muhitda boshqa ID paydo bo'lardi va "bitta makon" invarianti mantiqiy darajada
    /// buzilardi (DB indeksi buni to'xtatardi, lekin seed xato bilan yiqilardi).
    /// </summary>
    public static readonly Guid PublicSpaceSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    /// <summary>Barqaror slug — ommaviy havola (`/ommaviy`) hech qachon o'zgarmaydi.</summary>
    public const string PublicSpaceSlug = "ommaviy";

    public const string PublicSpaceName = "Ommaviy makon";

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
            await SeedSystemProgramAsync(cancellationToken).ConfigureAwait(false);
            await SeedPublicSpaceAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// `docs/06` 8-bo'lim (2026-09-02 qaror) + `prompts/34` B7-band: mavjud 4 tizim metodikasi
    /// (MBTI16/BIG5/RIASEC/ACTIVITY) bitta tizim dasturiga (`PERSONALITY_PROFILE`, "Shaxsiyat
    /// profili") birlashtiriladi — `Kind = System`, `Visibility = Public`, `IsSystem = true`,
    /// `Status = Published`. Mavjud BARCHA `assessments` (eski, migratsiyadan oldingi sessiyalar)
    /// shu dasturga bog'lanadi — orqaga moslik: eski oqim (tanlov ekranisiz) buzilmaydi.
    ///
    /// **Idempotent** (`SeedTestDefinitionsAsync` naqshiga ergashadi): dastur `Code` bo'yicha
    /// topilsa hech narsa qilinmaydi (tarkib BR-8 bo'yicha runtime'da o'zgartirilmaydi — yangi
    /// tizim metodikasi qo'shilishi alohida migratsiya talab qiladi, xuddi savol qo'shish kabi).
    /// `assessments.program_id IS NULL` bo'lgan qatorlar esa HAR safar (dastur eski/yangi bo'lishidan
    /// qat'i nazar) xom SQL bilan to'ldiriladi — ikki bosqichli migratsiya (`CLAUDE.md` 7-qoida):
    /// ustun HOZIRCHA nullable, ikkinchi migratsiyada `NOT NULL` qilinadi.
    /// </summary>
    private async Task SeedSystemProgramAsync(CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var systemTestCodes = new[] { "MBTI16", "BIG5", "RIASEC", "ACTIVITY" };
        var systemTestDefinitions = await _dbContext.TestDefinitions
            .Where(t => systemTestCodes.Contains(t.Code))
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // `Include(p => p.Tests)` — mavjud (migratsiya yoki oldingi seed orqali yaratilgan)
        // dasturning tarkibi haqiqiy tekshirilishi kerak (pastdagi `EnsureSystemTestsAttached`
        // uchun) — `AsNoTracking` EMAS, chunki `SaveChangesAsync` bilan yozilishi mumkin.
        var existingProgram = await _dbContext.AssessmentPrograms
            .Include(p => p.Tests)
            .FirstOrDefaultAsync(p => p.Code == SystemPersonalityProfileProgramCode, cancellationToken)
            .ConfigureAwait(false);

        if (existingProgram is null)
        {
            if (systemTestDefinitions.Count == 0)
            {
                // Test bankiga oid seed katalogi topilmagan muhitda (yuqoridagi
                // `SeedTestDefinitionsAsync` ogohlantirishi bilan bir xil holat) — tizim
                // dasturi ham yaratilmaydi, jimgina o'tkazib yuboriladi.
                _logger.LogWarning("Tizim metodikalari topilmadi — '{ProgramCode}' tizim dasturi yaratilmadi.", SystemPersonalityProfileProgramCode);
                return;
            }

            // Odatiy holatda bu shoxobcha ISHGA TUSHMAYDI — `RequireAssessmentProgramId`
            // migratsiyasi dasturni allaqachon (xom SQL bilan, xuddi shu deterministik ID
            // bilan) yaratgan bo'ladi. Bu yerda faqat ESKI (bu migratsiyadan oldingi) yoki
            // qo'lda boshqarilgan muhitlar uchun zaxira yo'l sifatida qoladi.
            var program = AssessmentProgram.CreateSystemPublished(
                SystemPersonalityProfileProgramId,
                SystemPersonalityProfileProgramCode,
                "Shaxsiyat profili",
                descriptionUz: "To'rt ilmiy metodikadan iborat yaxlit batareya: shaxsiyat tipi, Big Five, kasb qiziqishlari va aktivlik.",
                displayOrder: 1,
                tests: systemTestDefinitions.Select(t => (t.Id, t.DisplayOrder)).ToList(),
                now: now);

            _dbContext.AssessmentPrograms.Add(program);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Tizim dasturi yaratildi: {Code} ({TestCount} ta test).", SystemPersonalityProfileProgramCode, systemTestDefinitions.Count);
        }
        else
        {
            // ⚠️ Asosiy stsenariy (QA tuzatmasi, 2026-09-02): BIRINCHI deploy'da migratsiya
            // (`RequireAssessmentProgramId`, `--migrate` bosqichi) dasturni test banki HALI
            // seed qilinmagan paytda (0 ta test bilan) yaratgan bo'lishi mumkin — chunki
            // `--migrate` har doim `--seed`dan OLDIN ishga tushadi (`docker-compose.yml`).
            // Endi (`--seed` bosqichida) haqiqiy test banki mavjud — `EnsureSystemTestsAttached`
            // hali bog'lanmagan testlarni bog'laydi (BR-8 qulfini CHETLAB o'tadigan yagona,
            // maxsus seed-vaqtidagi metod — admin API oddiy `AddTest`dan foydalanadi).
            if (systemTestDefinitions.Count > 0)
            {
                var newlyLinked = existingProgram.EnsureSystemTestsAttached(
                    systemTestDefinitions.Select(t => (t.Id, t.DisplayOrder)).ToList(), now);

                // ⚠️ QA topilmasi (`AddProgramTestCommandHandler`dagi bilan bir xil sabab):
                // `existingProgram` SO'ROV orqali (Add() EMAS) tracked qilingan — domen metodi
                // ichida yaratilgan yangi `ProgramTest`larni EF Core'ning avtomatik graf
                // kashfiyoti noto'g'ri holatga ("Modified", "Added" emas) belgilaydi, chunki
                // ularning `Id`si oldindan (client tomonida) o'rnatilgan. Har birini ANIQ
                // `Add()` qilish holatni majburiy ravishda to'g'rilaydi.
                foreach (var programTest in newlyLinked)
                {
                    _dbContext.Add(programTest);
                }

                if (newlyLinked.Count > 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation(
                        "{Count} ta tizim testi '{Code}' dasturiga qayta bog'landi.", newlyLinked.Count, SystemPersonalityProfileProgramCode);
                }
            }

            _logger.LogInformation("Tizim dasturi allaqachon mavjud: {Code} — tarkib tekshirildi.", SystemPersonalityProfileProgramCode);
        }

        // Zaxira/himoya: eski sessiyalar (`program_id IS NULL`) shu dasturga bog'lanadi.
        // Odatiy holatda bu SHART EMAS — asosiy backfill endi `RequireAssessmentProgramId`
        // migratsiyasining O'ZIDA, `SET NOT NULL`dan OLDIN, xom SQL bilan bajariladi (QA
        // tuzatmasi: `--migrate` butun zanjirni `--seed`dan OLDIN, bitta chaqiruvda bajaradi,
        // shu sabab bu yerdagi (seed bosqichidagi) backfill'ga um HECH QACHON tayanib bo'lmaydi
        // — ustun bu paytda allaqachon `NOT NULL`). Shunga qaramay, ustun hali nullable bo'lgan
        // (masalan qo'lda faqat birinchi migratsiya qo'llangan) noodatiy holatlar uchun
        // zararsiz zaxira sifatida qoldirilgan — ustun `NOT NULL` bo'lsa bu so'rov 0 qatorga tegadi.
        var updatedRows = await _dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"UPDATE assessments SET program_id = {SystemPersonalityProfileProgramId} WHERE program_id IS NULL",
                cancellationToken)
            .ConfigureAwait(false);

        if (updatedRows > 0)
        {
            _logger.LogInformation("{Count} ta eski sessiya '{Code}' tizim dasturiga bog'landi.", updatedRows, SystemPersonalityProfileProgramCode);
        }
    }

    /// <summary>
    /// Ommaviy makon (`SchoolKind.PublicSpace`) — Telegram orqali kirgan tashqi
    /// foydalanuvchilarning `Student`/`Assessment` yozuvlari shu makonga tegishli bo'ladi
    /// (`SchoolKind` izohi: nima uchun `SchoolId` nullable qilinmadi).
    ///
    /// **Idempotent:** yozuv `Id` bo'yicha topilsa hech narsa o'zgartirilmaydi — jumladan
    /// `AccessToken` ham QAYTA GENERATSIYA QILINMAYDI (aks holda har seedda mavjud ommaviy
    /// havolalar o'lardi) va `ShowResultToStudent` ham (superadmin uni panelda o'zgartirgan
    /// bo'lishi mumkin). Global query filtri (`!IsDeleted`) chetlab o'tiladi: makonni
    /// o'chirib bo'lmaydi, lekin qo'lda SQL bilan `is_deleted = true` qilingan noodatiy
    /// bazada seed IKKINCHI makon yaratib, `ux_schools_public_space` ni buzmasligi kerak.
    ///
    /// `AccessToken` seed vaqtida TASODIFIY generatsiya qilinadi (kodda qattiq yozilgan
    /// qiymat yo'q — `CLAUDE.md` 4-qoida): ommaviy oqim slug+token juftligiga tayanadi va
    /// token oldindan bilinadigan bo'lsa maktab havolalari bilan bir xil xavfsizlik
    /// modelidan chetga chiqardi.
    /// </summary>
    private async Task SeedPublicSpaceAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Schools
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == PublicSpaceSchoolId || s.Kind == SchoolKind.PublicSpace, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            _logger.LogInformation("Ommaviy makon allaqachon mavjud: {Slug} — o'zgartirilmadi.", existing.Slug.Value);
            return;
        }

        var publicSpace = School.CreatePublicSpace(
            PublicSpaceSchoolId,
            PublicSpaceName,
            SchoolSlug.FromExisting(PublicSpaceSlug),
            accessToken: new TokenGenerator().GenerateUrlSafeToken(32),
            now: _dateTime.UtcNow,
            notes: "Tizim yozuvi: Telegram orqali kirgan tashqi foydalanuvchilar shu makonga tegishli. O'chirilmaydi.");

        _dbContext.Schools.Add(publicSpace);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Ommaviy makon yaratildi: {Slug}.", PublicSpaceSlug);
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
        var email = _configuration["ADMIN_EMAIL"] ?? _configuration["Admin:Email"] ?? $"{username}@16shaxsiyat.uz";

        var admin = AdminUser.Create(Guid.NewGuid(), username, email, passwordHash, _dateTime.UtcNow);
        _dbContext.AdminUsers.Add(admin);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Superadmin yaratildi: {Username}.", username);
    }
}
