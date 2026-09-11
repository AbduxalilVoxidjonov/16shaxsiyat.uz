using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Catalog;

/// <summary>
/// `CatalogTestListItemDto`/`CatalogTestDetailDto`/`CatalogQuestionItemDto`/`CatalogScaleItemDto`
/// qurish va agregatni TRACKED holda (buyruq handler'lari uchun) yuklash uchun umumiy mantiq
/// (`ProgramMapping` naqshiga o'xshash, `prompts/37-katalog-crud-backend.md`).
/// </summary>
internal static class CatalogMapping
{
    /// <summary>`Standard` (`IsSystem = true`) → "System", `Custom` → "Custom" — frontend `CatalogTestListItem.kind` shu ikkitasini kutadi, domendagi `TestKind` enum nomlari EMAS.</summary>
    public static string ToKindLabel(TestKind kind) => kind == TestKind.Standard ? "System" : "Custom";

    /// <summary>
    /// <c>questionCount</c> ATAYLAB parametr — `test.QuestionCount` (`_questions.Count`) faqat
    /// agregat TO'LIQ (savollari bilan) yuklangandagina to'g'ri (`LoadTrackedAsync`). Oddiy
    /// `AsNoTracking` so'rovda (ro'yxat/detal query'lari) bu kolleksiya BO'SH bo'lib qoladi —
    /// `docs/06` §8 2026-08-31 qarori: "`question_count` DB ustuni emas ... katalog ro'yxati
    /// so'rovida `Questions.Count()` subquery kerak bo'ladi". Shu QA topilmasini takrorlamaslik
    /// uchun chaqiruvchi HAR DOIM aniq sonni (COUNT so'rovi yoki tracked kolleksiya) beradi.
    /// </summary>
    public static CatalogTestListItemDto ToListItemDto(TestDefinition test, int questionCount, int scaleCount, int usedInProgramCount) => new(
        test.Id,
        test.Code,
        test.NameUz,
        ToKindLabel(test.Kind),
        test.IsSystem,
        test.Status.ToString(),
        test.IsActive,
        test.ScoringMode.ToString(),
        questionCount,
        scaleCount,
        test.EstimatedMinutes,
        test.Version,
        usedInProgramCount);

    /// <summary>`questionCount` haqida `ToListItemDto` izohiga qarang.</summary>
    public static CatalogTestDetailDto ToDetailDto(TestDefinition test, int questionCount, int scaleCount, int usedInProgramCount) => new(
        test.Id,
        test.Code,
        test.NameUz,
        ToKindLabel(test.Kind),
        test.IsSystem,
        test.Status.ToString(),
        test.IsActive,
        test.ScoringMode.ToString(),
        questionCount,
        scaleCount,
        test.EstimatedMinutes,
        test.Version,
        usedInProgramCount,
        test.DescriptionUz,
        test.PageSize,
        test.ShuffleQuestions,
        test.DisplayOrder);

    /// <summary>
    /// <paramref name="scaleNames"/> MAJBURIY parametr — shkala nomi ikki manbadan kelishi mumkin
    /// (`Custom` anketaning o'z `TestScale`i yoki tizim metodikasi uchun `SystemScaleCatalog`),
    /// va u savolning o'zida YO'Q. Ixtiyoriy qilinsa, chaqiruvchi uni unutgan joyda nom jimgina
    /// `null` bo'lib qolardi — shu sabab har bir chaqiruvchi aniq resolver beradi
    /// (nom kerak bo'lmagan joyda <see cref="CatalogScaleNameResolver.None"/>).
    /// </summary>
    /// <summary>
    /// <paramref name="options"/> — chaqiruvchi TASHQARIDAN beradi: `Question.Options` faqat
    /// aggregate TRACKED yuklanganda (`LoadTrackedAsync`) yoki entity yangi yaratilganda to'g'ri
    /// bo'ladi; `AsNoTracking` ro'yxat so'rovlarida (`ListTestQuestionsQueryHandler`) alohida
    /// batch so'rov bilan olib kelinadi (`GetCatalogTestPreviewQueryHandler` naqshi).
    /// `null` — chaqiruvchi variantlarni yuklamagan (masalan tizim savoli, variantsiz tur).
    /// </summary>
    /// <summary><paramref name="hasAnswers"/> — `CatalogQuestionItemDto.HasAnswers` izohiga qarang (P52). Yangi yaratilgan savolda chaqiruvchi bermaydi (standart `false` — to'g'ri, javob hali yo'q).</summary>
    public static CatalogQuestionItemDto ToQuestionDto(Question question, CatalogScaleNameResolver scaleNames, IReadOnlyList<AnswerOption>? options = null, bool hasAnswers = false)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(scaleNames);

        var resolvedOptions = options ?? question.Options.ToList();
        var optionDtos = resolvedOptions.Count == 0
            ? null
            : resolvedOptions.OrderBy(o => o.DisplayOrder).Select(o => new CatalogOptionDto(o.Id, o.TextUz, o.Value, o.DisplayOrder)).ToList();

        return new CatalogQuestionItemDto(
            question.Id,
            question.Code,
            question.DisplayOrder,
            question.TextUz,
            question.TextRu,
            question.TextEn,
            question.QuestionType.ToString(),
            question.Scale,
            question.ScaleDirection,
            question.Weight,
            question.IsRequired,
            question.IsActive,
            question.IsSystem,
            scaleNames.Resolve(question.Scale),
            scaleNames.ResolveDescription(question.Scale),
            question.SectionId,
            question.Placeholder,
            question.InputPattern,
            question.MaxLength,
            question.MinSelections,
            question.MaxSelections,
            question.VisibilityRule,
            optionDtos,
            hasAnswers);
    }

    /// <summary>`docs/18` §2.2/§5 — bo'lim DTO'siga xaritalash.</summary>
    public static CatalogSectionItemDto ToSectionDto(QuestionSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new CatalogSectionItemDto(
            section.Id,
            section.TestDefinitionId,
            section.Code,
            section.TitleUz,
            section.DescriptionUz,
            section.DisplayOrder,
            section.VisibilityRule);
    }

    /// <summary>
    /// Anketaning shkalalarini alohida so'rov bilan yuklab, nom resolverini quradi — agregat
    /// TRACKED yuklanmagan (`AsNoTracking` so'rov) joylar uchun, u yerda `test.Scales` bo'sh
    /// bo'ladi (`LoadTrackedAsync` izohidagi bilan bir xil sabab).
    /// </summary>
    public static async Task<CatalogScaleNameResolver> LoadScaleNamesAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        TestDefinition test,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(test);

        var scales = await executor.ToListAsync(
            context.AsNoTracking(context.TestScales).Where(s => s.TestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        return CatalogScaleNameResolver.Create(test.ScoringStrategyCode, scales);
    }

    public static CatalogScaleItemDto ToScaleDto(TestScale scale, int questionCount) => new(
        scale.Id,
        scale.TestDefinitionId,
        scale.Code,
        scale.NameUz,
        scale.DescriptionUz,
        scale.DisplayOrder,
        scale.InterpretationBands.Select(b => new InterpretationBandDto(b.MinInclusive, b.MaxInclusive, b.Level)).ToList(),
        questionCount);

    /// <summary>Berilgan testlar to'plami uchun dasturlarda ishlatilish sonini (`ProgramTest`) bitta batch so'rov bilan hisoblaydi (`docs/06` §8: xotirada agregatsiya taqiqlanadi — bu yerda faqat guruhlash, DB dan kelgan ro'yxat ustida).</summary>
    public static async Task<IReadOnlyDictionary<Guid, int>> CountUsedInProgramsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IReadOnlyCollection<Guid> testDefinitionIds,
        CancellationToken cancellationToken)
    {
        if (testDefinitionIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await executor.ToListAsync(
            context.AsNoTracking(context.ProgramTests)
                .Where(pt => testDefinitionIds.Contains(pt.TestDefinitionId))
                .Select(pt => pt.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        return rows.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>Testlar to'plami uchun FAOL savollar sonini bitta batch so'rov bilan hisoblaydi — `test.QuestionCount` (`_questions.Count`) BARCHA savolni sanaydi, bu esa alohida (jami) ko'rsatkich; ro'yxat/detal DTO'lari uchun ATAYLAB "faol savollar" emas, "barcha savollar" soni kerak (`docs/07` misolidagi `questionCount` — nashr validatsiyasi kabi faqat faolni emas), shu sabab filtrsiz sanaladi.</summary>
    public static async Task<IReadOnlyDictionary<Guid, int>> CountQuestionsAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IReadOnlyCollection<Guid> testDefinitionIds,
        CancellationToken cancellationToken)
    {
        if (testDefinitionIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await executor.ToListAsync(
            context.AsNoTracking(context.Questions)
                .Where(q => testDefinitionIds.Contains(q.TestDefinitionId))
                .Select(q => q.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        return rows.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Berilgan savol ID'lari orasidan JAVOBI BOR bo'lganlarini bitta batch so'rov bilan
    /// aniqlaydi (`CatalogQuestionItemDto.HasAnswers`, P52 — ro'yxat endpointida N+1 bo'lmasin,
    /// ADR-11). `Answers.QuestionId`ni ID bo'yicha filtr qilib `Distinct`langan xotira
    /// to'plamiga aylantiradi — `SELECT DISTINCT question_id FROM answers WHERE question_id IN (...)`.
    /// </summary>
    public static async Task<IReadOnlySet<Guid>> LoadQuestionIdsWithAnswersAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IReadOnlyCollection<Guid> questionIds,
        CancellationToken cancellationToken)
    {
        if (questionIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var rows = await executor.ToListAsync(
            context.AsNoTracking(context.Answers)
                .Where(a => questionIds.Contains(a.QuestionId))
                .Select(a => a.QuestionId)
                .Distinct(),
            cancellationToken).ConfigureAwait(false);

        return rows.ToHashSet();
    }

    /// <summary>Testlar to'plami uchun shkalalar sonini bitta batch so'rov bilan hisoblaydi.</summary>
    public static async Task<IReadOnlyDictionary<Guid, int>> CountScalesAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IReadOnlyCollection<Guid> testDefinitionIds,
        CancellationToken cancellationToken)
    {
        if (testDefinitionIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await executor.ToListAsync(
            context.AsNoTracking(context.TestScales)
                .Where(s => testDefinitionIds.Contains(s.TestDefinitionId))
                .Select(s => s.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        return rows.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// `TestDefinition` agregatini savollari va shkalalari bilan birga TRACKED (kuzatiluvchi)
    /// holda yuklaydi — buyruq handler'lari uchun (`PublishProgramCommandHandler`dagi "fixup"
    /// naqshi: bir xil `DbContext` ichida alohida so'ralgan bola yozuvlar EF Core'ning identity
    /// resolution + FK moslashuvi orqali ota entity'ning navigatsiya kolleksiyasiga (`_questions`/
    /// `_scales`, `PropertyAccessMode.Field`) avtomatik "yopishtiriladi" — `Include()` shart emas,
    /// chunki `IAppDbContext` `IQueryable&lt;T&gt;` beradi, EF Core'ning `Include()` kengaytma
    /// metodi esa `Application`da mavjud emas).
    /// </summary>
    public static async Task<TestDefinition?> LoadTrackedAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid testDefinitionId,
        CancellationToken cancellationToken)
    {
        var test = await executor.FirstOrDefaultAsync(
            context.TestDefinitions.Where(t => t.Id == testDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return null;
        }

        var questions = await executor.ToListAsync(
            context.Questions.Where(q => q.TestDefinitionId == testDefinitionId),
            cancellationToken).ConfigureAwait(false);

        // `docs/18` §5 — `CatalogPublishValidator`/`ToQuestionDto` `question.Options`ga tayanadi
        // (`QUESTION_OPTIONS_REQUIRED`/`QUESTION_OPTION_VALUE_DUPLICATE`/`VISIBILITY_VALUE_UNKNOWN`);
        // `AnswerOption`ning `TestDefinitionId`si yo'q (faqat `QuestionId`), shu sabab avval
        // yuklangan savol ID'lari orqali batch so'rov (`GetCatalogTestPreviewQueryHandler` naqshi,
        // lekin bu yerda TRACKED — EF fixup `question.Options`ni to'ldirishi uchun).
        var questionIds = questions.Select(q => q.Id).ToList();
        _ = await executor.ToListAsync(
            context.AnswerOptions.Where(o => questionIds.Contains(o.QuestionId)),
            cancellationToken).ConfigureAwait(false);

        _ = await executor.ToListAsync(
            context.TestScales.Where(s => s.TestDefinitionId == testDefinitionId),
            cancellationToken).ConfigureAwait(false);

        // `docs/18` §2.2 — bo'limlar ham xuddi shu "fixup" naqshi bilan (bo'lim CRUD
        // handler'lari uchun kerak: `TestDefinition.AddSection`/`RemoveSection`/`ReorderSections`).
        _ = await executor.ToListAsync(
            context.QuestionSections.Where(s => s.TestDefinitionId == testDefinitionId),
            cancellationToken).ConfigureAwait(false);

        return test;
    }
}
