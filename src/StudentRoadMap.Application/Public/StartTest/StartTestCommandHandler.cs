using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.StartTest;

/// <summary>
/// `docs/07` 1.4-bo'lim + `docs/04` 2.3/2.4-bo'lim (holat mashinasi) + `prompts/11`.
///
/// Oqim:
/// 1. Sessiyani (tracked) va shu sessiyaga tegishli BARCHA test bloklarini alohida, tekis
///    so'rovlar bilan yuklaydi — `Include()` `Application`da yo'q (`docs/06` 3-bo'lim: EF Core
///    paketi bu qatlamda taqiqlangan), shu sabab `Assessment.Tests`/`AssessmentTest.Answers`
///    kabi navigatsiyalar EF Core'ning "relationship fixup" mexanizmi orqali to'ldiriladi:
///    ikkala so'rov ham SHU DbContext ichida tracked bo'lsa, EF avtomatik ravishda
///    `AssessmentConfiguration`da `PropertyAccessMode.Field` bilan belgilangan `_tests` maydonini
///    mos `AssessmentTest` yozuvlari bilan bog'laydi — `StartSessionCommandHandler`da yangi
///    yaratilgan agregatlar uchun ishlatilgan naqshning MAVJUD (DB'dan qayta yuklangan)
///    agregatlar uchun analogi.
/// 2. Oldingi (`DisplayOrder` bo'yicha kichikroq) test bloklari hammasi `Completed` emasligini
///    tekshiradi — aks holda `409 TEST_NOT_UNLOCKED`.
/// 3. `Assessment.StartTest` (domen metodi) chaqiriladi — `Draft → InProgress` (yoki idempotent).
/// 4. `ShuffleQuestions` yoqilgan va tartib hali generatsiya qilinmagan bo'lsa — `IQuestionShuffler`
///    orqali BIR MARTA generatsiya qilinib `AssessmentTest.QuestionOrder`ga yoziladi; qayta
///    chaqirilganda mavjud tartib saqlanib qoladi (qayta aralashtirilmaydi).
/// </summary>
internal sealed class StartTestCommandHandler : IRequestHandler<StartTestCommand, Result<StartTestResult>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly PublicCatalogCache _catalogCache;
    private readonly IQuestionShuffler _shuffler;

    public StartTestCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        PublicCatalogCache catalogCache,
        IQuestionShuffler shuffler)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _catalogCache = catalogCache;
        _shuffler = shuffler;
    }

    public async Task<Result<StartTestResult>> Handle(StartTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        // Tracked (AsNoTracking EMAS) — bu Command holatni o'zgartiradi.
        var assessment = await _executor.FirstOrDefaultAsync(
            _context.Assessments.Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<StartTestResult>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        if (assessment.ExpiresAt <= now)
        {
            return Result.Failure<StartTestResult>(new Error(ProblemCodes.SessionExpired, "Sessiyaning amal qilish muddati tugagan."));
        }

        var testDefinition = await _catalogCache.GetPublishedTestDefinitionAsync(request.TestCode, cancellationToken).ConfigureAwait(false);
        if (testDefinition is null)
        {
            return Result.Failure<StartTestResult>(new Error(ProblemCodes.NotFound, "Test topilmadi."));
        }

        // Fixup uchun: shu sessiyaga tegishli barcha test bloklarini SHU DbContext orqali tracked
        // holda yuklaymiz (sinf izohiga qarang) — `assessment.Tests` shu bilan to'ladi.
        var assessmentTests = await _executor.ToListAsync(
            _context.AssessmentTests.Where(t => t.AssessmentId == assessment.Id).OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var targetTest = assessmentTests.FirstOrDefault(t => t.TestDefinitionId == testDefinition.Id);
        if (targetTest is null)
        {
            return Result.Failure<StartTestResult>(new Error(ProblemCodes.NotFound, "Bu test ushbu sessiyaga biriktirilmagan."));
        }

        var isUnlocked = assessmentTests
            .Where(t => t.DisplayOrder < targetTest.DisplayOrder)
            .All(t => t.Status == TestStatus.Completed);

        if (!isUnlocked)
        {
            return Result.Failure<StartTestResult>(new Error(ProblemCodes.TestNotUnlocked, "Oldingi test hali yakunlanmagan."));
        }

        // Domen metodi `_tests` (yuqorida fixup bilan to'lgan) ichidan `targetTest`ni topib
        // uni `Start`laydi va kerak bo'lsa `Assessment.Status`ni `Draft → InProgress`ga o'tkazadi.
        assessment.StartTest(testDefinition.Id, now);

        if (testDefinition.ShuffleQuestions && targetTest.QuestionOrder.Count == 0)
        {
            var activeQuestions = await _catalogCache.GetActiveQuestionsAsync(testDefinition.Id, assessment.LanguageCode, cancellationToken).ConfigureAwait(false);
            var orderedIds = activeQuestions.OrderBy(q => q.DisplayOrder).Select(q => q.Id).ToList();
            var shuffledIds = _shuffler.Shuffle(orderedIds);
            targetTest.SetQuestionOrder(shuffledIds);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var totalPages = targetTest.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(targetTest.TotalCount / (double)testDefinition.PageSize);

        var result = new StartTestResult(testDefinition.Code, targetTest.Status.ToString(), testDefinition.PageSize, totalPages);

        return Result.Success(result);
    }
}
