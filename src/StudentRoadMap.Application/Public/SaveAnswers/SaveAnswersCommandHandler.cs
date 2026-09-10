using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.SaveAnswers;

/// <summary>
/// `docs/07` 1.6-bo'lim + `docs/04` 2.5-bo'lim (`Answer` upsert invarianti) + `docs/18` §4.2.
///
/// Oqim:
/// 1. Sessiya (AsNoTracking — bu yerda `Assessment.Status`ga tegilmaydi, faqat o'qiladi)
///    topiladi, muddati tekshiriladi (`410 SESSION_EXPIRED`) — "yakunlangan/muddati o'tgan
///    sessiyaga yozish taqiqlanadi" (`prompts/11` 6-band). Sessiya `Completed`/`Analyzing`/…
///    holatida bo'lsa, nishonlangan test bloki ham allaqachon `Completed` bo'ladi — quyidagi
///    `AssessmentTest.UpsertAnswer` domen qo'riqchisi (`ASSESSMENT_TEST_NOT_IN_PROGRESS`,
///    default `409`) bunday yozishni baribir rad etadi.
/// 2. Nishonlangan `AssessmentTest` (tracked) + uning MAVJUD `Answer`lari (tracked, fixup
///    uchun — `StartTestCommandHandler`dagi izohga qarang) SHU DbContext orqali yuklanadi.
///    Fixup bo'lmasa `UpsertAnswer` har doim "yangi javob" deb hisoblab, ikkinchi marta
///    javob berilganda unikal indeks (`ux_answers_test_question`) xatosiga olib keladi.
/// 3. Har bir javob: savol shu testga tegishli/faolligi, shakli (`value`/`text`/`selectedValues`dan
///    AYNAN bittasi, savol turiga mos — aks holda `ANSWER_SHAPE_INVALID`) va turga xos mazmun
///    (`docs/18` §4.2 jadvali — matn uzunligi/shablon, `MultiChoice` tanlovlari) tekshiriladi —
///    BIRINCHI o'tkazib bo'lmagan tekshiruvda hech narsa saqlanmasdan `400` qaytariladi
///    (ikki bosqichli: avval hammasi tekshiriladi, keyin hammasi yoziladi).
/// 4. Barcha elementlar shakl/mazmun bo'yicha o'tgach — `VisibleQuestionResolver` bazadagi
///    joriy javoblar VA shu so'rovdagi (tasdiqlangan) javoblar BIRLASHTIRILGAN holatda
///    chaqiriladi; ko'rinmaydigan savolga yozishga urinish `400 QUESTION_NOT_VISIBLE` bilan
///    rad etiladi (bu ham yozishdan OLDIN, ikki bosqichli naqshning davomi).
/// 5. Tasdiqlangan har bir javob `AssessmentTest.UpsertAnswer` orqali yoziladi — mavjud bo'lsa
///    `RevisionCount++`, yangi bo'lsa `AnsweredCount++` (domen ichida).
/// </summary>
internal sealed class SaveAnswersCommandHandler : IRequestHandler<SaveAnswersCommand, Result<SaveAnswersResult>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly PublicCatalogCache _catalogCache;

    public SaveAnswersCommandHandler(
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

    /// <summary>Bitta tasdiqlangan javob — `AssessmentTest.UpsertAnswer`ga to'g'ridan-to'g'ri uzatiladigan shakl.</summary>
    private sealed record ValidatedAnswer(
        Guid QuestionId,
        int? RawValue,
        string? TextValue,
        IReadOnlyList<int>? SelectedValues,
        Guid? SelectedOptionId,
        int DurationMs);

    public async Task<Result<SaveAnswersResult>> Handle(SaveAnswersCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<SaveAnswersResult>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        if (assessment.ExpiresAt <= now)
        {
            return Result.Failure<SaveAnswersResult>(new Error(ProblemCodes.SessionExpired, "Sessiyaning amal qilish muddati tugagan."));
        }

        var testDefinition = await _catalogCache.GetPublishedTestDefinitionAsync(request.TestCode, cancellationToken).ConfigureAwait(false);
        if (testDefinition is null)
        {
            return Result.Failure<SaveAnswersResult>(new Error(ProblemCodes.NotFound, "Test topilmadi."));
        }

        // Tracked — javob yozish uchun.
        var assessmentTest = await _executor.FirstOrDefaultAsync(
            _context.AssessmentTests.Where(t => t.AssessmentId == assessment.Id && t.TestDefinitionId == testDefinition.Id),
            cancellationToken).ConfigureAwait(false);

        if (assessmentTest is null)
        {
            return Result.Failure<SaveAnswersResult>(new Error(ProblemCodes.NotFound, "Bu test ushbu sessiyaga biriktirilmagan."));
        }

        // Fixup: mavjud javoblarni SHU DbContext orqali tracked holda yuklaymiz — sinf izohiga qarang.
        _ = await _executor.ToListAsync(
            _context.Answers.Where(a => a.AssessmentTestId == assessmentTest.Id),
            cancellationToken).ConfigureAwait(false);

        var questions = await _catalogCache.GetActiveQuestionsAsync(testDefinition.Id, assessment.LanguageCode, cancellationToken).ConfigureAwait(false);
        var sections = await _catalogCache.GetSectionsAsync(testDefinition.Id, cancellationToken).ConfigureAwait(false);
        var questionsById = questions.ToDictionary(q => q.Id);

        var validatedAnswers = new List<ValidatedAnswer>(request.Answers.Count);

        foreach (var item in request.Answers)
        {
            if (!questionsById.TryGetValue(item.QuestionId, out var question))
            {
                return Result.Failure<SaveAnswersResult>(new Error(ProblemCodes.ValidationError, "Ko'rsatilgan savol ushbu testga tegishli emas."));
            }

            var validation = ValidateItem(question, item);
            if (validation.IsFailure)
            {
                return Result.Failure<SaveAnswersResult>(validation.Error);
            }

            validatedAnswers.Add(validation.Value);
        }

        // `docs/18` §4.2: "so'rovdagi savol joriy javoblar (bazadagi + shu so'rovdagi)
        // bo'yicha ko'rinmasa — 400 QUESTION_NOT_VISIBLE". Baza holati + shu so'rovda
        // tasdiqlangan javoblar BIRLASHTIRILADI, so'ng butun anketa bo'yicha ko'rinish
        // qayta hisoblanadi (kaskad boshqa sahifadagi savolga ham bog'liq bo'lishi mumkin,
        // shu sabab `questions`/`sections` — BUTUN test, faqat joriy sahifa emas).
        var visibilityCheck = EnsureAllVisible(assessmentTest, questions, sections, questionsById, validatedAnswers);
        if (visibilityCheck.IsFailure)
        {
            return Result.Failure<SaveAnswersResult>(visibilityCheck.Error);
        }

        // `Answer.Id` `ValueGeneratedOnAdd()` (`HasDefaultValueSql("gen_random_uuid()")`) bilan
        // sozlangan — EF Core'ning graf-fixup orqali (navigatsiyaga qo'shish, `_context.Add`SIZ)
        // aniqlaydigan "Added vs Unchanged" evristikasi klient tomonidan berilgan (bo'sh bo'lmagan)
        // `Guid` kalitini "allaqachon mavjud" deb noto'g'ri talqin qiladi va `INSERT` o'rniga
        // `UPDATE` yuborib, `DbUpdateConcurrencyException` (0 qator) beradi. Shu sabab YANGI
        // javoblar aniq (before/after farqi orqali) `_context.Add`ga uzatiladi — loyihaning
        // boshqa joylarida (`StartSessionCommandHandler`) yangi agregatlar uchun ham xuddi shu
        // aniq `Add` konvensiyasi ishlatiladi.
        var existingAnswerIds = assessmentTest.Answers.Select(a => a.Id).ToHashSet();

        foreach (var answer in validatedAnswers)
        {
            assessmentTest.UpsertAnswer(
                Guid.NewGuid(), answer.QuestionId, answer.RawValue, answer.SelectedOptionId, answer.DurationMs, now,
                answer.TextValue, answer.SelectedValues);
        }

        foreach (var newAnswer in assessmentTest.Answers.Where(a => !existingAnswerIds.Contains(a.Id)))
        {
            _context.Add(newAnswer);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new SaveAnswersResult(validatedAnswers.Count, assessmentTest.AnsweredCount, assessmentTest.TotalCount);

        return Result.Success(result);
    }

    /// <summary>
    /// Bazadagi (fixup orqali tracked) javoblar + shu so'rovda tasdiqlangan javoblarni
    /// savol KODI bo'yicha birlashtirib (`VisibilityEvaluator` savol kodiga tayanadi, B-5),
    /// butun anketa bo'yicha ko'rinish qayta hisoblaydi va har bir yuborilgan savol
    /// ko'rinadigan to'plamda ekanini tekshiradi.
    /// </summary>
    private static Result EnsureAllVisible(
        AssessmentTest assessmentTest,
        IReadOnlyList<CachedQuestionDto> questions,
        IReadOnlyList<CachedSectionDto> sections,
        IReadOnlyDictionary<Guid, CachedQuestionDto> questionsById,
        IReadOnlyList<ValidatedAnswer> validatedAnswers)
    {
        var answersByCode = new Dictionary<string, AnswerSnapshot>(StringComparer.Ordinal);

        foreach (var answer in assessmentTest.Answers)
        {
            if (questionsById.TryGetValue(answer.QuestionId, out var question))
            {
                answersByCode[question.Code] = new AnswerSnapshot(answer.RawValue, answer.TextValue, answer.SelectedValues);
            }
        }

        foreach (var validated in validatedAnswers)
        {
            var code = questionsById[validated.QuestionId].Code;
            answersByCode[code] = new AnswerSnapshot(validated.RawValue, validated.TextValue, validated.SelectedValues ?? []);
        }

        var sectionSnapshots = sections.Select(s => new SectionSnapshot(s.Id, s.Code, s.DisplayOrder, s.Visibility)).ToList();
        var questionSnapshots = questions
            .Select(q => new QuestionSnapshot(q.Id, q.Code, q.DisplayOrder, IsActive: true, q.SectionId, q.Visibility))
            .ToList();

        var visibilityMap = VisibleQuestionResolver.Resolve(sectionSnapshots, questionSnapshots, answersByCode);

        foreach (var validated in validatedAnswers)
        {
            if (!visibilityMap.VisibleQuestionIds.Contains(validated.QuestionId))
            {
                var code = questionsById[validated.QuestionId].Code;
                return Result.Failure(new Error(
                    ProblemCodes.QuestionNotVisible,
                    $"'{code}' savoli hozirgi javoblar bo'yicha ko'rinmaydi — avval bog'liq savolga javob bering."));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Bitta javob elementini savol turiga mos shakl (`ANSWER_SHAPE_INVALID`) va mazmun
    /// (`docs/18` §4.2 jadvali, `VALIDATION_ERROR`) bo'yicha tekshiradi.
    /// </summary>
    private static Result<ValidatedAnswer> ValidateItem(CachedQuestionDto question, SaveAnswerItem item)
    {
        var providedCount = (item.Value is not null ? 1 : 0)
            + (item.Text is not null ? 1 : 0)
            + (item.SelectedValues is not null ? 1 : 0);

        if (providedCount != 1)
        {
            return Result.Failure<ValidatedAnswer>(new Error(
                ProblemCodes.AnswerShapeInvalid,
                $"'{question.Code}' savoli uchun 'value'/'text'/'selectedValues'dan AYNAN bittasi to'ldirilishi kerak."));
        }

        return question.QuestionType switch
        {
            QuestionType.Likert5 or QuestionType.Likert7 or QuestionType.Binary or QuestionType.SingleChoice or QuestionType.ForcedChoice =>
                item.Value is null
                    ? ShapeMismatch(question, "value")
                    : ValidateChoiceLikeValue(question, item),

            QuestionType.ShortText or QuestionType.Phone =>
                item.Text is null ? ShapeMismatch(question, "text") : ValidateShortText(question, item),

            QuestionType.LongText =>
                item.Text is null ? ShapeMismatch(question, "text") : ValidateLongText(question, item),

            QuestionType.MultiChoice =>
                item.SelectedValues is null ? ShapeMismatch(question, "selectedValues") : ValidateMultiChoice(question, item),

            _ => Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, "Noma'lum savol turi.")),
        };
    }

    private static Result<ValidatedAnswer> ShapeMismatch(CachedQuestionDto question, string expectedField) =>
        Result.Failure<ValidatedAnswer>(new Error(
            ProblemCodes.AnswerShapeInvalid,
            $"'{question.Code}' savoli ('{question.QuestionType}') uchun '{expectedField}' maydoni to'ldirilishi kerak."));

    /// <summary>
    /// `RawValue` savol turiga mos ekanini tekshiradi (`prompts/11` 4-band). `SingleChoice`/
    /// `ForcedChoice` uchun qiymat variantlardan biriga TENG bo'lishi shart — mos variant ID'si
    /// `SelectedOptionId` sifatida qaytariladi (`docs/04` 2.5-bo'lim).
    /// </summary>
    private static Result<ValidatedAnswer> ValidateChoiceLikeValue(CachedQuestionDto question, SaveAnswerItem item)
    {
        var value = item.Value!.Value;

        switch (question.QuestionType)
        {
            case QuestionType.Likert5:
                return value is >= 1 and <= 5
                    ? Result.Success(new ValidatedAnswer(item.QuestionId, value, null, null, null, item.DurationMs))
                    : Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun qiymat 1..5 oralig'ida bo'lishi kerak."));

            case QuestionType.Likert7:
                return value is >= 1 and <= 7
                    ? Result.Success(new ValidatedAnswer(item.QuestionId, value, null, null, null, item.DurationMs))
                    : Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun qiymat 1..7 oralig'ida bo'lishi kerak."));

            case QuestionType.Binary:
                return value is 0 or 1
                    ? Result.Success(new ValidatedAnswer(item.QuestionId, value, null, null, null, item.DurationMs))
                    : Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun qiymat 0 yoki 1 bo'lishi kerak."));

            case QuestionType.SingleChoice:
            case QuestionType.ForcedChoice:
                var matchedOption = question.Options.FirstOrDefault(o => o.Value == value);
                return matchedOption is not null
                    ? Result.Success(new ValidatedAnswer(item.QuestionId, value, null, null, matchedOption.Id, item.DurationMs))
                    : Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun noto'g'ri variant qiymati."));

            default:
                return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, "Noma'lum savol turi."));
        }
    }

    /// <summary>`ShortText`/`Phone` — `docs/18` §4.2: bo'sh emas, `MaxLength` (standart 200), `InputPattern` mos.</summary>
    private static Result<ValidatedAnswer> ValidateShortText(CachedQuestionDto question, SaveAnswerItem item)
    {
        var text = item.Text!;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun matn bo'sh bo'lishi mumkin emas."));
        }

        var maxLength = EffectiveMaxLength(question);
        if (text.Length > maxLength)
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun matn {maxLength} belgidan oshmasligi kerak."));
        }

        if (!string.IsNullOrWhiteSpace(question.InputPattern) && !CachedInputPatternMatcher.IsMatch(question.InputPattern, text))
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun matn kutilgan shablonga mos emas."));
        }

        return Result.Success(new ValidatedAnswer(item.QuestionId, null, text, null, null, item.DurationMs));
    }

    /// <summary>`LongText` — `docs/18` §4.2: bo'sh emas, `MaxLength` (standart 2000). `InputPattern` qo'llanmaydi.</summary>
    private static Result<ValidatedAnswer> ValidateLongText(CachedQuestionDto question, SaveAnswerItem item)
    {
        var text = item.Text!;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun matn bo'sh bo'lishi mumkin emas."));
        }

        var maxLength = EffectiveMaxLength(question);
        if (text.Length > maxLength)
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun matn {maxLength} belgidan oshmasligi kerak."));
        }

        return Result.Success(new ValidatedAnswer(item.QuestionId, null, text, null, null, item.DurationMs));
    }

    /// <summary>
    /// `MultiChoice` — `docs/18` §4.2: bo'sh emas, takrorlanmaydi, hammasi variant
    /// qiymatlaridan, `MinSelections ≤ n ≤ MaxSelections`.
    /// </summary>
    private static Result<ValidatedAnswer> ValidateMultiChoice(CachedQuestionDto question, SaveAnswerItem item)
    {
        var selectedValues = item.SelectedValues!;

        if (selectedValues.Count == 0)
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun kamida bitta variant tanlanishi kerak."));
        }

        if (selectedValues.Distinct().Count() != selectedValues.Count)
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun variantlar takrorlanmasligi kerak."));
        }

        var validValues = question.Options.Select(o => o.Value).ToHashSet();
        if (selectedValues.Any(v => !validValues.Contains(v)))
        {
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun noto'g'ri variant qiymati."));
        }

        var min = EffectiveMinSelections(question);
        var max = EffectiveMaxSelections(question);
        if (selectedValues.Count < min || selectedValues.Count > max)
        {
            var maxLabel = max == int.MaxValue ? "cheklovsiz" : max.ToString();
            return Result.Failure<ValidatedAnswer>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun tanlovlar soni {min}..{maxLabel} oralig'ida bo'lishi kerak."));
        }

        return Result.Success(new ValidatedAnswer(item.QuestionId, null, null, selectedValues, null, item.DurationMs));
    }

    /// <summary>`docs/18` §2.3: `MaxLength` berilmasa standart — `ShortText`/`Phone` 200, `LongText` 2000.</summary>
    private static int EffectiveMaxLength(CachedQuestionDto question) =>
        question.MaxLength ?? (question.QuestionType == QuestionType.LongText ? 2000 : 200);

    /// <summary>`docs/18` §2.3: `MinSelections` berilmasa — majburiy savolda 1, ixtiyoriyda 0.</summary>
    private static int EffectiveMinSelections(CachedQuestionDto question) =>
        question.MinSelections ?? (question.IsRequired ? 1 : 0);

    /// <summary>`docs/18` §2.3: `MaxSelections` berilmasa — cheklovsiz.</summary>
    private static int EffectiveMaxSelections(CachedQuestionDto question) =>
        question.MaxSelections ?? int.MaxValue;
}
