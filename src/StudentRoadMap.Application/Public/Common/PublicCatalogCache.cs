using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Ommaviy oqim uchun test katalogini (`TestDefinition` + faol `Question`'lar) 10 daqiqaga
/// keshlaydi (`prompts/11`). Kesh kaliti SESSIYAGA bog'liq EMAS (bir xil `testCode`/
/// `testDefinitionId`+`languageCode` uchun barcha o'quvchilar bitta yozuvni baham ko'radi),
/// lekin TILGA bog'liq — savol so'rovlari kalitiga `languageCode` kiradi (QA topilmasi,
/// 2026-09-02: `docs/07` "scaleLabels tildan olinadi" talabi va `Question.TextRu`/`TextEn`
/// mavjudligi sabab; til aralashtirilsa bitta til birinchi so'ragan navbatda hammaga
/// qaytarilib qolar edi — jimgina, aniqlash qiyin xato). O'quvchiga xos narsa (javob qiymati —
/// `currentValue`) bu klass orqali HECH QACHON o'tmaydi — chaqiruvchi handler uni alohida,
/// keshdan tashqarida DB'dan o'qiydi (`GetTestQuestionsQueryHandler`).
///
/// Invalidatsiya: hozircha (`prompts/11` doirasida) `TestDefinition`/`Question`ni o'zgartiruvchi
/// admin buyruqlari yo'q (P13–P15), shu sabab bu yerda faqat TTL (10 daqiqa) ishlaydi.
/// `Remove` metodi kelajakdagi admin mutatsiya handler'lari uchun tayyor — ular
/// `TestDefinitionCacheKey`/`QuestionsCacheKey` orqali aniq yozuvni bekor qilishi kerak
/// bo'ladi (PM'ga savol: shu paytgacha rasman hujjatlashtirilsinmi?).
/// </summary>
internal sealed class PublicCatalogCache
{
    /// <summary>Til kodi berilmasa/tanilmasa qaytiladigan standart til (`docs/01` MVP — o'zbek).</summary>
    public const string DefaultLanguageCode = "uz";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly ICacheService _cache;

    public PublicCatalogCache(IAppDbContext context, IAsyncQueryExecutor executor, ICacheService cache)
    {
        _context = context;
        _executor = executor;
        _cache = cache;
    }

    public static string TestDefinitionCacheKey(string testCode) => $"public-catalog:test-definition:{testCode}";

    /// <summary>Kesh kaliti tilni o'z ichiga oladi — `NormalizeLanguage` bilan bir xil normalizatsiya.</summary>
    public static string QuestionsCacheKey(Guid testDefinitionId, string? languageCode) =>
        $"public-catalog:questions:{testDefinitionId}:{NormalizeLanguage(languageCode)}";

    /// <summary>Bo'sh/noma'lum til → `uz` (fallback), aks holda kichik harfga tekislanadi (kesh kaliti barqaror bo'lishi uchun).</summary>
    public static string NormalizeLanguage(string? languageCode) =>
        string.IsNullOrWhiteSpace(languageCode) ? DefaultLanguageCode : languageCode.Trim().ToLowerInvariant();

    /// <summary>Faqat nashr qilingan va faol anketani qaytaradi. Topilmasa `null` — bu holat keshlanmaydi.</summary>
    public async Task<CachedTestDefinitionDto?> GetPublishedTestDefinitionAsync(string testCode, CancellationToken cancellationToken)
    {
        var cacheKey = TestDefinitionCacheKey(testCode);
        if (_cache.TryGet<CachedTestDefinitionDto>(cacheKey, out var cached))
        {
            return cached;
        }

        var testDefinition = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions)
                .Where(t => t.Code == testCode && t.Status == TestDefinitionStatus.Published && t.IsActive),
            cancellationToken).ConfigureAwait(false);

        if (testDefinition is null)
        {
            return null;
        }

        var dto = new CachedTestDefinitionDto(
            testDefinition.Id,
            testDefinition.Code,
            testDefinition.PageSize,
            testDefinition.ShuffleQuestions,
            testDefinition.Version,
            testDefinition.ScoringStrategyCode,
            testDefinition.ScoringMode);
        _cache.Set(cacheKey, dto, CacheDuration);
        return dto;
    }

    /// <summary>
    /// Faol savollarni `DisplayOrder` bo'yicha, variantlari bilan qaytaradi (`scale`/`direction`siz).
    /// `languageCode` bo'yicha matn tanlanadi (`ResolveText`) — kesh yozuvi ham shu til uchun
    /// alohida saqlanadi (`QuestionsCacheKey`).
    /// </summary>
    public async Task<IReadOnlyList<CachedQuestionDto>> GetActiveQuestionsAsync(Guid testDefinitionId, string? languageCode, CancellationToken cancellationToken)
    {
        var normalizedLanguage = NormalizeLanguage(languageCode);
        var cacheKey = QuestionsCacheKey(testDefinitionId, normalizedLanguage);
        if (_cache.TryGet<IReadOnlyList<CachedQuestionDto>>(cacheKey, out var cached))
        {
            return cached;
        }

        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => q.TestDefinitionId == testDefinitionId && q.IsActive)
                .OrderBy(q => q.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var questionIds = questions.Select(q => q.Id).ToList();
        var options = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AnswerOptions)
                .Where(o => questionIds.Contains(o.QuestionId))
                .OrderBy(o => o.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var optionsByQuestion = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

        IReadOnlyList<CachedQuestionDto> result = questions
            .Select(q => new CachedQuestionDto(
                q.Id,
                q.Code,
                q.DisplayOrder,
                ResolveText(normalizedLanguage, q.TextUz, q.TextRu, q.TextEn),
                q.QuestionType,
                q.IsRequired,
                (optionsByQuestion.TryGetValue(q.Id, out var qOptions) ? qOptions : [])
                    .Select(o => new CachedAnswerOptionDto(o.Id, o.TextUz, o.Value, o.DisplayOrder))
                    .ToList()))
            .ToList();

        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    /// <summary>
    /// `languageCode` bo'yicha savol matnini tanlaydi — mos til matni bo'lmasa (hozircha `ru`/`en`
    /// hech qachon seed qilinmagan) o'zbekchaga (`textUz`) qaytadi. `textUz` domenda majburiy
    /// (`Question.TextUz` — `null!`), shu sabab fallback har doim natija beradi.
    /// </summary>
    private static string ResolveText(string normalizedLanguage, string textUz, string? textRu, string? textEn) =>
        normalizedLanguage switch
        {
            "ru" when !string.IsNullOrWhiteSpace(textRu) => textRu,
            "en" when !string.IsNullOrWhiteSpace(textEn) => textEn,
            _ => textUz,
        };
}
