using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.SaveAnswers;

/// <summary>
/// `docs/07` 1.6-bo'lim + `docs/04` 2.5-bo'lim (`Answer` upsert invarianti) + `prompts/11`.
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
/// 3. Har bir javob: savol shu testga tegishli/faolligi va qiymati turi bo'yicha (Likert5 →
///    1..5, Likert7 → 1..7, Binary → 0/1, SingleChoice/ForcedChoice → variant qiymatlaridan
///    biri) tekshiriladi — BIRINCHI o'tkazib bo'lmagan tekshiruvda hech narsa saqlanmasdan
///    `400 VALIDATION_ERROR` qaytariladi (ikki bosqichli: avval hammasi tekshiriladi, keyin
///    hammasi yoziladi — qisman yozish bo'lmasligi uchun).
/// 4. Tasdiqlangan har bir javob `AssessmentTest.UpsertAnswer` orqali yoziladi — mavjud bo'lsa
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
        var questionsById = questions.ToDictionary(q => q.Id);

        var validatedAnswers = new List<(Guid QuestionId, int RawValue, Guid? SelectedOptionId, int DurationMs)>(request.Answers.Count);

        foreach (var item in request.Answers)
        {
            if (!questionsById.TryGetValue(item.QuestionId, out var question))
            {
                return Result.Failure<SaveAnswersResult>(new Error(ProblemCodes.ValidationError, "Ko'rsatilgan savol ushbu testga tegishli emas."));
            }

            var validation = ValidateValue(question, item.Value);
            if (validation.IsFailure)
            {
                return Result.Failure<SaveAnswersResult>(validation.Error);
            }

            validatedAnswers.Add((item.QuestionId, item.Value, validation.Value, item.DurationMs));
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
            assessmentTest.UpsertAnswer(Guid.NewGuid(), answer.QuestionId, answer.RawValue, answer.SelectedOptionId, answer.DurationMs, now);
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
    /// `RawValue` savol turiga mos ekanini tekshiradi (`prompts/11` 4-band). `SingleChoice`/
    /// `ForcedChoice` uchun qiymat variantlardan biriga TENG bo'lishi shart — mos variant ID'si
    /// `SelectedOptionId` sifatida qaytariladi (`docs/04` 2.5-bo'lim).
    /// </summary>
    private static Result<Guid?> ValidateValue(CachedQuestionDto question, int value)
    {
        switch (question.QuestionType)
        {
            case QuestionType.Likert5:
                return value is >= 1 and <= 5
                    ? Result.Success<Guid?>(null)
                    : Result.Failure<Guid?>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun qiymat 1..5 oralig'ida bo'lishi kerak."));

            case QuestionType.Likert7:
                return value is >= 1 and <= 7
                    ? Result.Success<Guid?>(null)
                    : Result.Failure<Guid?>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun qiymat 1..7 oralig'ida bo'lishi kerak."));

            case QuestionType.Binary:
                return value is 0 or 1
                    ? Result.Success<Guid?>(null)
                    : Result.Failure<Guid?>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun qiymat 0 yoki 1 bo'lishi kerak."));

            case QuestionType.SingleChoice:
            case QuestionType.ForcedChoice:
                var matchedOption = question.Options.FirstOrDefault(o => o.Value == value);
                return matchedOption is not null
                    ? Result.Success<Guid?>(matchedOption.Id)
                    : Result.Failure<Guid?>(new Error(ProblemCodes.ValidationError, $"'{question.Code}' savoli uchun noto'g'ri variant qiymati."));

            default:
                return Result.Failure<Guid?>(new Error(ProblemCodes.ValidationError, "Noma'lum savol turi."));
        }
    }
}
