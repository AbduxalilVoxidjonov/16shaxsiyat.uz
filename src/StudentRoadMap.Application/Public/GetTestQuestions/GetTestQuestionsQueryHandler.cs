using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetTestQuestions;

/// <summary>
/// `docs/07` 1.5-bo'lim. Read-only — `AsNoTracking` (`docs/06` 4-bo'lim konvensiyasi).
///
/// Kesh strategiyasi (`prompts/11`): `TestDefinition`/`Question` ro'yxati `PublicCatalogCache`
/// orqali 10 daqiqaga keshlanadi (sessiyaga bog'liq emas, lekin TILGA bog'liq —
/// `assessment.LanguageCode` kesh kalitiga kiradi, QA topilmasi 2026-09-02). `currentValue`
/// esa HAR DOIM shu so'rov ichida, kesh TASHQARISIDA, joriy sahifadagi savollar uchun DB'dan
/// yangi o'qiladi — bu qiymat o'quvchiga xos, keshga aralashtirib bo'lmaydi (`prompts/11`
/// "nozik joy" ogohlantirishi).
///
/// `ScaleLabels` shu sahifadagi BIRINCHI savolning turi VA `assessment.LanguageCode` asosida
/// hisoblanadi (`docs/07` 1.5-bo'lim: "scaleLabels tildan olinadi"). Hozircha faqat `uz`
/// to'plami mavjud — boshqa til uchun MEXANIZM tayyor (`LabelsByLanguage`), lekin `ru`/`en`
/// matni hali yozilmagan (PM topshirig'i, 2026-09-02: "Ru/en matnini yozma — faqat mexanizm"),
/// shu sabab noma'lum til `uz`ga qaytadi (`ResolveLabelSet`). Aralash turdagi sahifa (masalan,
/// `SingleChoice` bilan `Likert5` bir sahifada) uchun aniq qoida hujjatlanmagan — PM'ga savol.
/// </summary>
internal sealed class GetTestQuestionsQueryHandler : IRequestHandler<GetTestQuestionsQuery, Result<GetTestQuestionsResult>>
{
    private static readonly IReadOnlyList<PublicScaleLabelDto> Likert5LabelsUz =
    [
        new(1, "Umuman qo'shilmayman"),
        new(2, "Qo'shilmayman"),
        new(3, "Bilmadim"),
        new(4, "Qo'shilaman"),
        new(5, "To'liq qo'shilaman"),
    ];

    private static readonly IReadOnlyList<PublicScaleLabelDto> Likert7LabelsUz =
    [
        new(1, "Umuman qo'shilmayman"),
        new(2, "Qo'shilmayman"),
        new(3, "Ozgina qo'shilmayman"),
        new(4, "Bilmadim"),
        new(5, "Ozgina qo'shilaman"),
        new(6, "Qo'shilaman"),
        new(7, "To'liq qo'shilaman"),
    ];

    private static readonly IReadOnlyList<PublicScaleLabelDto> BinaryLabelsUz =
    [
        new(0, "Yo'q"),
        new(1, "Ha"),
    ];

    /// <summary>
    /// Til bo'yicha shkala yorliqlari — hozircha faqat `uz`. Yangi til qo'shilganda shu yerga
    /// kiritiladi (masalan `["ru"] = new Dictionary&lt;...&gt; { ... }`); `ResolveLabelSet`
    /// topilmagan tilni avtomatik `uz`ga qaytaradi, shu sabab kod o'zgarishi shart emas.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<QuestionType, IReadOnlyList<PublicScaleLabelDto>>> LabelsByLanguage =
        new Dictionary<string, IReadOnlyDictionary<QuestionType, IReadOnlyList<PublicScaleLabelDto>>>(StringComparer.Ordinal)
        {
            [PublicCatalogCache.DefaultLanguageCode] = new Dictionary<QuestionType, IReadOnlyList<PublicScaleLabelDto>>
            {
                [QuestionType.Likert5] = Likert5LabelsUz,
                [QuestionType.Likert7] = Likert7LabelsUz,
                [QuestionType.Binary] = BinaryLabelsUz,
            },
        };

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly PublicCatalogCache _catalogCache;

    public GetTestQuestionsQueryHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _catalogCache = catalogCache;
    }

    public async Task<Result<GetTestQuestionsResult>> Handle(GetTestQuestionsQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<GetTestQuestionsResult>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        if (assessment.ExpiresAt <= now)
        {
            return Result.Failure<GetTestQuestionsResult>(new Error(ProblemCodes.SessionExpired, "Sessiyaning amal qilish muddati tugagan."));
        }

        var testDefinition = await _catalogCache.GetPublishedTestDefinitionAsync(request.TestCode, cancellationToken).ConfigureAwait(false);
        if (testDefinition is null)
        {
            return Result.Failure<GetTestQuestionsResult>(new Error(ProblemCodes.NotFound, "Test topilmadi."));
        }

        var assessmentTest = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.AssessmentTests)
                .Where(t => t.AssessmentId == assessment.Id && t.TestDefinitionId == testDefinition.Id),
            cancellationToken).ConfigureAwait(false);

        if (assessmentTest is null)
        {
            return Result.Failure<GetTestQuestionsResult>(new Error(ProblemCodes.NotFound, "Bu test ushbu sessiyaga biriktirilmagan."));
        }

        var questions = await _catalogCache.GetActiveQuestionsAsync(testDefinition.Id, assessment.LanguageCode, cancellationToken).ConfigureAwait(false);

        // Qayta kirganda AYNAN o'sha tartib qaytishi shart (`prompts/11`) — `QuestionOrder`
        // bo'sh bo'lmasa (aralashtirilgan bo'lsa), shu ketma-ketlik bo'yicha saralanadi.
        var orderedQuestions = OrderQuestions(questions, assessmentTest.QuestionOrder);

        var totalQuestions = orderedQuestions.Count;
        var totalPages = totalQuestions == 0 ? 0 : (int)Math.Ceiling(totalQuestions / (double)testDefinition.PageSize);

        var pageItems = orderedQuestions
            .Skip((request.Page - 1) * testDefinition.PageSize)
            .Take(testDefinition.PageSize)
            .ToList();

        var pageQuestionIds = pageItems.Select(q => q.Id).ToList();
        var currentValues = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Answers)
                .Where(a => a.AssessmentTestId == assessmentTest.Id && pageQuestionIds.Contains(a.QuestionId))
                .Select(a => new { a.QuestionId, a.RawValue }),
            cancellationToken).ConfigureAwait(false);

        var currentValueByQuestionId = currentValues.ToDictionary(a => a.QuestionId, a => a.RawValue);

        var questionDtos = pageItems
            .Select(q => new PublicQuestionDto(
                q.Id,
                q.Code,
                q.DisplayOrder,
                q.Text,
                q.QuestionType.ToString(),
                q.IsRequired,
                q.QuestionType is QuestionType.SingleChoice or QuestionType.ForcedChoice
                    ? q.Options.Select(o => new PublicAnswerOptionDto(o.Id, o.TextUz, o.Value, o.DisplayOrder)).ToList()
                    : null,
                currentValueByQuestionId.TryGetValue(q.Id, out var value) ? value : null))
            .ToList();

        var scaleLabels = pageItems.Count > 0 ? BuildScaleLabels(pageItems[0].QuestionType, assessment.LanguageCode) : null;

        var result = new GetTestQuestionsResult(
            testDefinition.Code,
            request.Page,
            testDefinition.PageSize,
            totalPages,
            totalQuestions,
            scaleLabels,
            questionDtos);

        return Result.Success(result);
    }

    private static IReadOnlyList<CachedQuestionDto> OrderQuestions(IReadOnlyList<CachedQuestionDto> questions, IReadOnlyList<Guid> questionOrder)
    {
        if (questionOrder.Count == 0)
        {
            return questions; // `PublicCatalogCache.GetActiveQuestionsAsync` allaqachon `DisplayOrder` bo'yicha.
        }

        var byId = questions.ToDictionary(q => q.Id);
        var ordered = new List<CachedQuestionDto>(questionOrder.Count);
        foreach (var id in questionOrder)
        {
            if (byId.TryGetValue(id, out var question))
            {
                ordered.Add(question);
            }
        }

        return ordered;
    }

    /// <summary>
    /// `languageCode` bo'yicha to'plamni tanlaydi, topilmasa (hozircha `uz`dan boshqa hammasi)
    /// `uz`ga qaytadi (`ResolveLabelSet` — fallback, `docs/07` "scaleLabels tildan olinadi").
    /// </summary>
    private static IReadOnlyList<PublicScaleLabelDto>? BuildScaleLabels(QuestionType questionType, string? languageCode)
    {
        var labelsForLanguage = ResolveLabelSet(languageCode);
        return labelsForLanguage.GetValueOrDefault(questionType);
    }

    private static IReadOnlyDictionary<QuestionType, IReadOnlyList<PublicScaleLabelDto>> ResolveLabelSet(string? languageCode)
    {
        var normalizedLanguage = PublicCatalogCache.NormalizeLanguage(languageCode);
        return LabelsByLanguage.TryGetValue(normalizedLanguage, out var labelsForLanguage)
            ? labelsForLanguage
            : LabelsByLanguage[PublicCatalogCache.DefaultLanguageCode];
    }
}
